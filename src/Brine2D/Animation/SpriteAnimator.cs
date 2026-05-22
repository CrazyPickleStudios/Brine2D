using Microsoft.Extensions.Logging;

namespace Brine2D.Animation;

/// <summary>
/// Animates sprites by playing animation clips.
/// </summary>
public partial class SpriteAnimator : IDisposable
{
    private readonly ILogger<SpriteAnimator>? _logger;
    private readonly Dictionary<string, AnimationClip> _animations = new();
    private readonly Queue<(string Name, float FadeDuration, AnimationClip? DirectClip)> _animationQueue = new();

    private AnimationClip? _currentAnimation;
    private int _currentFrameIndex;
    private float _frameTimer;
    private float _clipTime;
    private bool _isPlaying;
    private bool _isPaused;
    private float _speed = 1.0f;
    private bool _reversed;
    private bool _pingPongForward = true;
    private bool _pingPongFirstPassDone;
    private int _loopCountRemaining;
    private float _crossFadeAlpha = 1f;
    private float _crossFadeTimer;
    private float _crossFadeDuration;

    private const int MaxFrameAdvanceIterations = 1000;

    /// <summary>
    /// Maximum queue depth across all <c>PlayQueued*</c> overloads. Entries beyond this limit
    /// are dropped with a warning. Defaults to <c>32</c>.
    /// </summary>
    public int MaxQueueDepth { get; set; } = 32;

    /// <summary>
    /// The sprite tint alpha [0,1] captured at the moment a cross-fade starts.
    /// Consumed and written by <see cref="Brine2D.Systems.Animation.AnimationSystem"/>.
    /// </summary>
    public float CrossFadeBaseAlpha { get; set; } = 1f;

    /// <summary>Gets the currently playing animation clip.</summary>
    public AnimationClip? CurrentAnimation => _currentAnimation;

    /// <summary>
    /// Gets the zero-based index of the current frame within the active clip,
    /// or -1 if no animation is active.
    /// </summary>
    public int CurrentFrameIndex => _currentAnimation != null ? _currentFrameIndex : -1;

    /// <summary>
    /// Gets the current frame, or <c>null</c> if no animation is active or the animator is stopped.
    /// </summary>
    public SpriteFrame? CurrentFrame =>
        _currentAnimation != null && _currentFrameIndex < _currentAnimation.Frames.Count
            ? _currentAnimation.Frames[_currentFrameIndex]
            : null;

    /// <summary>
    /// Elapsed playback time of the current clip in seconds, or <c>0</c> if nothing is active.
    /// For PingPong modes, returns the position within the current pass, not the full cycle time.
    /// </summary>
    public float CurrentTime
    {
        get
        {
            if (_currentAnimation == null)
                return 0f;
            if (_currentAnimation.PlaybackMode is PlaybackMode.PingPong or PlaybackMode.PingPongOnce)
            {
                var total = _currentAnimation.TotalDuration;
                if (total <= 0f)
                    return 0f;
                var cycleDuration = total * 2f;
                var posInCycle = ((_clipTime % cycleDuration) + cycleDuration) % cycleDuration;
                return posInCycle <= total ? posInCycle : cycleDuration - posInCycle;
            }
            return _clipTime;
        }
    }

    /// <summary>
    /// Gets the normalised playback position [0, 1]. Returns 0 if no animation is active or
    /// the total duration is zero.
    /// </summary>
    public float NormalizedTime
    {
        get
        {
            if (_currentAnimation == null)
                return 0f;
            var total = _currentAnimation.TotalDuration;
            return total > 0f ? CurrentTime / total : 0f;
        }
    }

    /// <summary>
    /// Remaining playback time in seconds for non-looping clips. Returns <c>0</c> for looping
    /// clips or when nothing is active.
    /// </summary>
    public float TimeRemaining
    {
        get
        {
            if (_currentAnimation == null || _currentAnimation.Loop)
                return 0f;
            return MathF.Max(0f, _currentAnimation.TotalDuration - CurrentTime);
        }
    }

    /// <summary>
    /// <c>true</c> when a non-looping clip has finished. <c>false</c> for looping clips, when
    /// nothing is active, or when paused.
    /// </summary>
    public bool IsFinished => _currentAnimation != null && !_isPlaying && !_isPaused;

    /// <summary>Gets whether an animation is currently playing (not paused and not finished).</summary>
    public bool IsPlaying => _isPlaying;

    /// <summary>
    /// Gets whether the animator has been explicitly paused via <see cref="Pause"/>.
    /// A paused animator still holds a valid <see cref="CurrentFrame"/> and can be resumed.
    /// </summary>
    public bool IsPaused => _isPaused;

    /// <summary>
    /// Gets or sets the playback speed multiplier. Clamped to zero on the low end.
    /// A value of <c>0</c> effectively freezes playback without losing clip state. Use
    /// <see cref="Pause"/> for an explicit pause that also sets <see cref="IsPaused"/>.
    /// </summary>
    public float Speed
    {
        get => _speed;
        set => _speed = MathF.Max(value, 0f);
    }

    /// <summary>
    /// Plays the clip in reverse. For PingPong modes, controls the starting direction only;
    /// the alternation itself is unaffected.
    /// </summary>
    public bool Reversed
    {
        get => _reversed;
        set => _reversed = value;
    }

    /// <summary>Returns a read-only collection of all registered animation names.</summary>
    public IReadOnlyCollection<string> AnimationNames => _animations.Keys;

    /// <summary>Returns a read-only collection of all registered animation clips.</summary>
    public IReadOnlyCollection<AnimationClip> Clips => _animations.Values;

    /// <summary>Gets the normalised cross-fade alpha [0, 1].</summary>
    public float CrossFadeAlpha => _crossFadeAlpha;

    /// <summary>Gets the outgoing clip during a cross-fade, or <c>null</c>.</summary>
    public AnimationClip? CrossFadeOutgoingClip { get; private set; }

    /// <summary>Gets the outgoing frame during a cross-fade, or <c>null</c>.</summary>
    public SpriteFrame? CrossFadeOutgoingFrame { get; private set; }

    /// <summary>Gets the name of the first queued animation, or <c>null</c>.</summary>
    public string? QueuedAnimation => _animationQueue.Count > 0 ? _animationQueue.Peek().Name : null;

    /// <summary>Gets the number of animations currently in the queue.</summary>
    public int AnimationQueueCount => _animationQueue.Count;

    /// <summary>Raised when a new animation begins playing.</summary>
    public event Action<AnimationClip>? OnAnimationStart;

    /// <summary>Raised when a non-looping animation reaches its terminal frame.</summary>
    public event Action<AnimationClip>? OnAnimationComplete;

    /// <summary>
    /// Raised each time a looping animation wraps, each time a ping-pong animation reverses
    /// direction, or when a <see cref="AnimationClip.RepeatCount"/> clip completes a pass.
    /// </summary>
    public event Action<AnimationClip>? OnLoopComplete;

    /// <summary>Raised each time the active frame index changes.</summary>
    public event Action<SpriteFrame>? OnFrameChanged;

    /// <summary>
    /// Raised when <see cref="Stop"/> is called or a <see cref="PlaybackMode.OnceStop"/> clip
    /// finishes. Receives the clip that was active, or <c>null</c>. Always fires regardless of
    /// the <c>fireCallbacks</c> parameter on <see cref="Stop"/>.
    /// </summary>
    public event Action<AnimationClip?>? OnStopped;

    public SpriteAnimator(ILogger<SpriteAnimator>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Releases all event subscriptions and clears playback state without firing any callbacks.
    /// </summary>
    public void Dispose()
    {
        _isPlaying = false;
        _isPaused = false;
        _currentAnimation = null;
        _currentFrameIndex = 0;
        _frameTimer = 0f;
        _clipTime = 0f;
        _crossFadeAlpha = 1f;
        _crossFadeDuration = 0f;
        CrossFadeOutgoingClip = null;
        CrossFadeOutgoingFrame = null;
        _animationQueue.Clear();
        OnAnimationStart = null;
        OnAnimationComplete = null;
        OnLoopComplete = null;
        OnFrameChanged = null;
        OnStopped = null;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Adds an animation clip. Replaces any existing clip with the same name and logs a warning.
    /// </summary>
    public void AddAnimation(AnimationClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (_animations.ContainsKey(clip.Name))
            _logger?.LogWarning("AddAnimation('{Name}'): replacing an existing clip with the same name.", clip.Name);
        _animations[clip.Name] = clip;
        _logger?.LogDebug("Added animation: {Name} ({FrameCount} frames)", clip.Name, clip.Frames.Count);
    }

    /// <summary>
    /// Removes an animation clip by name. If the clip is currently active the animator is stopped.
    /// Any queued entries for this clip are also removed.
    /// </summary>
    public bool RemoveAnimation(string name)
    {
        if (!_animations.Remove(name))
            return false;

        if (_currentAnimation?.Name == name)
        {
            _logger?.LogWarning("RemoveAnimation('{Name}'): clip was active — animator stopped.", name);
            Stop();
        }

        RemoveFromQueue(name);

        _logger?.LogDebug("Removed animation: {Name}", name);
        return true;
    }

    /// <summary>Removes all animation clips and stops the animator.</summary>
    public void ClearAnimations()
    {
        _animations.Clear();
        _animationQueue.Clear();
        Stop();
        _logger?.LogDebug("Cleared all animations");
    }

    /// <summary>
    /// Plays an animation by name from the beginning.
    /// </summary>
    /// <param name="animationName">Name of the animation to play.</param>
    /// <param name="restart">
    /// When <c>false</c> (default): if the same clip is already the active animation and it is
    /// still playing or paused, the call is a no-op. A finished clip is always restarted even
    /// when <c>restart</c> is <c>false</c>. Use <c>true</c> to force a restart unconditionally
    /// (including while playing or paused).
    /// </param>
    /// <exception cref="InvalidOperationException">Thrown if the clip has no frames.</exception>
    public void Play(string animationName, bool restart = false)
    {
        if (!_animations.TryGetValue(animationName, out var animation))
        {
            _logger?.LogWarning("Animation not found: {Name}", animationName);
            return;
        }

        if (animation.Frames.Count == 0)
            throw new InvalidOperationException($"Animation '{animationName}' has no frames.");

        if (_currentAnimation == animation && !restart && !IsFinished)
            return;

        var startFrame = _reversed ? animation.Frames.Count - 1 : 0;
        StartClip(animation, startFrame);
        _logger?.LogDebug("Playing animation: {Name}", animationName);
    }

    /// <summary>
    /// Plays an animation by name, beginning at a specific frame index.
    /// </summary>
    public void PlayFromFrame(string animationName, int startFrame, bool restart = false)
    {
        if (!_animations.TryGetValue(animationName, out var animation))
        {
            _logger?.LogWarning("Animation not found: {Name}", animationName);
            return;
        }

        if (animation.Frames.Count == 0)
            throw new InvalidOperationException($"Animation '{animationName}' has no frames.");

        if (_currentAnimation == animation && !restart && !IsFinished)
            return;

        var clamped = Math.Clamp(startFrame, 0, animation.Frames.Count - 1);
        StartClip(animation, clamped);
        _logger?.LogDebug("Playing animation: {Name} from frame {Frame}", animationName, clamped);
    }

    /// <summary>
    /// Plays an animation by name, beginning at a normalised time [0, 1].
    /// </summary>
    public void PlayFromNormalizedTime(string animationName, float normalizedTime, bool restart = false)
    {
        if (!_animations.TryGetValue(animationName, out var animation))
        {
            _logger?.LogWarning("Animation not found: {Name}", animationName);
            return;
        }

        if (animation.Frames.Count == 0)
            throw new InvalidOperationException($"Animation '{animationName}' has no frames.");

        if (_currentAnimation == animation && !restart && !IsFinished)
            return;

        var (frameIndex, frameTimer) = ResolveTimeToFrame(animation, normalizedTime * animation.TotalDuration);
        StartClip(animation, frameIndex, frameTimer);
        _logger?.LogDebug("Playing animation: {Name} from normalized time {Time}", animationName, normalizedTime);
    }

    /// <summary>
    /// Plays an animation by name with a cross-fade over <paramref name="fadeDuration"/> seconds.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="fadeDuration"/> is not positive.</exception>
    public void PlayWithCrossFade(string animationName, float fadeDuration, bool restart = false)
    {
        if (fadeDuration <= 0f)
            throw new ArgumentOutOfRangeException(nameof(fadeDuration), fadeDuration, "Fade duration must be greater than zero.");

        var outgoingClip = _currentAnimation;
        var outgoingFrame = CurrentFrame;
        var prevAnimation = _currentAnimation;
        var wasPlaying = _isPlaying;

        Play(animationName, restart);

        if (_currentAnimation == null || (_currentAnimation == prevAnimation && !restart && wasPlaying))
            return;

        if (_currentAnimation.Name != animationName)
            return;

        CrossFadeOutgoingClip = outgoingClip;
        CrossFadeOutgoingFrame = outgoingFrame;
        _crossFadeAlpha = 0f;
        _crossFadeTimer = 0f;
        _crossFadeDuration = fadeDuration;
    }

    /// <summary>
    /// Plays an animation by name beginning at a specific frame index, with a cross-fade over
    /// <paramref name="fadeDuration"/> seconds from the currently playing clip.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="fadeDuration"/> is not positive.</exception>
    public void PlayFromFrameWithCrossFade(string animationName, int startFrame, float fadeDuration, bool restart = false)
    {
        if (fadeDuration <= 0f)
            throw new ArgumentOutOfRangeException(nameof(fadeDuration), fadeDuration, "Fade duration must be greater than zero.");

        if (!_animations.TryGetValue(animationName, out var animation))
        {
            _logger?.LogWarning("Animation not found: {Name}", animationName);
            return;
        }

        if (animation.Frames.Count == 0)
            throw new InvalidOperationException($"Animation '{animationName}' has no frames.");

        if (_currentAnimation == animation && !restart && !IsFinished)
            return;

        var outgoingClip = _currentAnimation;
        var outgoingFrame = CurrentFrame;
        var prevAnimation = _currentAnimation;
        var wasPlaying = _isPlaying;

        var clamped = Math.Clamp(startFrame, 0, animation.Frames.Count - 1);
        StartClip(animation, clamped);

        if (_currentAnimation == null || (_currentAnimation == prevAnimation && !restart && wasPlaying))
            return;

        CrossFadeOutgoingClip = outgoingClip;
        CrossFadeOutgoingFrame = outgoingFrame;
        _crossFadeAlpha = 0f;
        _crossFadeTimer = 0f;
        _crossFadeDuration = fadeDuration;
        _logger?.LogDebug("Playing animation (cross-fade {Duration}s from frame {Frame}): {Name}", fadeDuration, clamped, animationName);
    }

    /// <summary>
    /// Plays an animation by name beginning at a normalised time [0, 1], with a cross-fade over
    /// <paramref name="fadeDuration"/> seconds from the currently playing clip.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="fadeDuration"/> is not positive.</exception>
    public void PlayFromNormalizedTimeWithCrossFade(string animationName, float normalizedTime, float fadeDuration, bool restart = false)
    {
        if (fadeDuration <= 0f)
            throw new ArgumentOutOfRangeException(nameof(fadeDuration), fadeDuration, "Fade duration must be greater than zero.");

        if (!_animations.TryGetValue(animationName, out var animation))
        {
            _logger?.LogWarning("Animation not found: {Name}", animationName);
            return;
        }

        if (animation.Frames.Count == 0)
            throw new InvalidOperationException($"Animation '{animationName}' has no frames.");

        if (_currentAnimation == animation && !restart && !IsFinished)
            return;

        var outgoingClip = _currentAnimation;
        var outgoingFrame = CurrentFrame;
        var prevAnimation = _currentAnimation;
        var wasPlaying = _isPlaying;

        var (frameIndex, frameTimer) = ResolveTimeToFrame(animation, normalizedTime * animation.TotalDuration);
        StartClip(animation, frameIndex, frameTimer);

        if (_currentAnimation == null || (_currentAnimation == prevAnimation && !restart && wasPlaying))
            return;

        CrossFadeOutgoingClip = outgoingClip;
        CrossFadeOutgoingFrame = outgoingFrame;
        _crossFadeAlpha = 0f;
        _crossFadeTimer = 0f;
        _crossFadeDuration = fadeDuration;
        _logger?.LogDebug("Playing animation (cross-fade {Duration}s from normalized time {Time}): {Name}", fadeDuration, normalizedTime, animationName);
    }

    /// <summary>
    /// Plays the named animation in reverse. Equivalent to setting <see cref="Reversed"/> to
    /// <c>true</c> then calling <see cref="Play"/>. After the clip ends, <see cref="Reversed"/>
    /// remains <c>true</c> — set it back to <c>false</c> explicitly if subsequent clips should
    /// play forward.
    /// </summary>
    public void PlayReversed(string animationName, bool restart = false)
    {
        Reversed = true;
        Play(animationName, restart);
    }

    /// <summary>
    /// Queues an animation to play after the current non-looping clip finishes (or immediately
    /// if nothing is playing). Multiple calls append to the queue; animations play in order.
    /// Indefinitely-looping clips (no <see cref="AnimationClip.RepeatCount"/>) cannot be queued
    /// behind because they never complete; a warning is logged and the call is ignored.
    /// The queue depth is capped at <see cref="MaxQueueDepth"/>; attempts to exceed it are
    /// ignored with a warning.
    /// </summary>
    public void PlayQueued(string animationName)
    {
        ArgumentNullException.ThrowIfNull(animationName);

        if (!_animations.ContainsKey(animationName))
        {
            _logger?.LogWarning("PlayQueued('{Name}'): animation not found.", animationName);
            return;
        }

        if (_currentAnimation == null || !_isPlaying)
        {
            Play(animationName);
            return;
        }

        if (_currentAnimation.Loop && _currentAnimation.RepeatCount == 0)
        {
            _logger?.LogWarning(
                "PlayQueued('{Queued}') ignored: '{Current}' is a {Mode} clip that never completes naturally.",
                animationName, _currentAnimation.Name, _currentAnimation.PlaybackMode);
            return;
        }

        if (_animationQueue.Count >= MaxQueueDepth)
        {
            _logger?.LogWarning(
                "PlayQueued('{Name}'): queue is full ({Depth}/{Max}). Entry dropped.",
                animationName, _animationQueue.Count, MaxQueueDepth);
            return;
        }

        _animationQueue.Enqueue((animationName, 0f, null));
        _logger?.LogDebug("Queued animation: {Name} (queue depth {Depth})", animationName, _animationQueue.Count);
    }

    /// <summary>
    /// Queues an animation to play with a cross-fade after the current non-looping clip finishes
    /// (or immediately if nothing is playing). Multiple calls append to the queue.
    /// The queue depth is capped at <see cref="MaxQueueDepth"/>; attempts to exceed it are
    /// ignored with a warning.
    /// </summary>
    public void PlayQueuedWithCrossFade(string animationName, float fadeDuration)
    {
        ArgumentNullException.ThrowIfNull(animationName);
        if (fadeDuration <= 0f)
            throw new ArgumentOutOfRangeException(nameof(fadeDuration), fadeDuration, "Fade duration must be greater than zero.");

        if (!_animations.ContainsKey(animationName))
        {
            _logger?.LogWarning("PlayQueuedWithCrossFade('{Name}'): animation not found.", animationName);
            return;
        }

        if (_currentAnimation == null || !_isPlaying)
        {
            PlayWithCrossFade(animationName, fadeDuration);
            return;
        }

        if (_currentAnimation.Loop && _currentAnimation.RepeatCount == 0)
        {
            _logger?.LogWarning(
                "PlayQueuedWithCrossFade('{Queued}') ignored: '{Current}' is a {Mode} clip that never completes naturally.",
                animationName, _currentAnimation.Name, _currentAnimation.PlaybackMode);
            return;
        }

        if (_animationQueue.Count >= MaxQueueDepth)
        {
            _logger?.LogWarning(
                "PlayQueuedWithCrossFade('{Name}'): queue is full ({Depth}/{Max}). Entry dropped.",
                animationName, _animationQueue.Count, MaxQueueDepth);
            return;
        }

        _animationQueue.Enqueue((animationName, fadeDuration, null));
        _logger?.LogDebug("Queued animation (cross-fade {Duration}s): {Name} (queue depth {Depth})", fadeDuration, animationName, _animationQueue.Count);
    }

    /// <summary>
    /// Queues a clip instance to play directly after the current non-looping clip finishes
    /// (or immediately if nothing is playing). The clip does not need to be registered via
    /// <see cref="AddAnimation"/>. Behaviour is otherwise identical to <see cref="PlayQueued"/>.
    /// </summary>
    public void PlayDirectQueued(AnimationClip clip)
    {
        ArgumentNullException.ThrowIfNull(clip);

        if (clip.Frames.Count == 0)
            throw new InvalidOperationException($"Animation '{clip.Name}' has no frames.");

        if (_currentAnimation == null || !_isPlaying)
        {
            PlayDirect(clip);
            return;
        }

        if (_currentAnimation.Loop && _currentAnimation.RepeatCount == 0)
        {
            _logger?.LogWarning(
                "PlayDirectQueued('{Queued}') ignored: '{Current}' is a {Mode} clip that never completes naturally.",
                clip.Name, _currentAnimation.Name, _currentAnimation.PlaybackMode);
            return;
        }

        if (_animationQueue.Count >= MaxQueueDepth)
        {
            _logger?.LogWarning(
                "PlayDirectQueued('{Name}'): queue is full ({Depth}/{Max}). Entry dropped.",
                clip.Name, _animationQueue.Count, MaxQueueDepth);
            return;
        }

        _animationQueue.Enqueue((clip.Name, 0f, clip));
        _logger?.LogDebug("Queued animation (direct): {Name} (queue depth {Depth})", clip.Name, _animationQueue.Count);
    }

    /// <summary>
    /// Queues a clip instance to play directly with a cross-fade after the current non-looping
    /// clip finishes (or immediately if nothing is playing). The clip does not need to be
    /// registered via <see cref="AddAnimation"/>. Behaviour is otherwise identical to
    /// <see cref="PlayQueuedWithCrossFade"/>.
    /// </summary>
    public void PlayDirectQueuedWithCrossFade(AnimationClip clip, float fadeDuration)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (fadeDuration <= 0f)
            throw new ArgumentOutOfRangeException(nameof(fadeDuration), fadeDuration, "Fade duration must be greater than zero.");

        if (clip.Frames.Count == 0)
            throw new InvalidOperationException($"Animation '{clip.Name}' has no frames.");

        if (_currentAnimation == null || !_isPlaying)
        {
            PlayDirectWithCrossFade(clip, fadeDuration);
            return;
        }

        if (_currentAnimation.Loop && _currentAnimation.RepeatCount == 0)
        {
            _logger?.LogWarning(
                "PlayDirectQueuedWithCrossFade('{Queued}') ignored: '{Current}' is a {Mode} clip that never completes naturally.",
                clip.Name, _currentAnimation.Name, _currentAnimation.PlaybackMode);
            return;
        }

        if (_animationQueue.Count >= MaxQueueDepth)
        {
            _logger?.LogWarning(
                "PlayDirectQueuedWithCrossFade('{Name}'): queue is full ({Depth}/{Max}). Entry dropped.",
                clip.Name, _animationQueue.Count, MaxQueueDepth);
            return;
        }

        _animationQueue.Enqueue((clip.Name, fadeDuration, clip));
        _logger?.LogDebug("Queued animation (direct cross-fade {Duration}s): {Name} (queue depth {Depth})", fadeDuration, clip.Name, _animationQueue.Count);
    }

    /// <summary>Cancels all queued animations.</summary>
    public void CancelQueued()
    {
        _animationQueue.Clear();
    }

    /// <summary>
    /// Pauses playback without resetting position. Sets <see cref="IsPaused"/> to <c>true</c>.
    /// </summary>
    public void Pause()
    {
        if (_currentAnimation == null)
            return;
        _isPlaying = false;
        _isPaused = true;
    }

    /// <summary>
    /// Resumes a paused animation. Clears <see cref="IsPaused"/>. No-op if not paused.
    /// </summary>
    public void Resume()
    {
        if (!_isPaused || _currentAnimation == null)
            return;
        _isPlaying = true;
        _isPaused = false;
    }

    /// <summary>
    /// Stops and clears the current animation and the entire animation queue.
    /// <see cref="CurrentFrame"/> returns <c>null</c> after this call.
    /// </summary>
    /// <param name="fireCallbacks">
    /// When <c>true</c>, fires <see cref="SpriteFrame.OnExit"/> and
    /// <see cref="AnimationClip.OnExit"/> before clearing state.
    /// <see cref="OnStopped"/> always fires regardless of this parameter.
    /// </param>
    public void Stop(bool fireCallbacks = false)
    {
        if (fireCallbacks && _currentAnimation != null)
        {
            if (_currentFrameIndex < _currentAnimation.Frames.Count)
                _currentAnimation.Frames[_currentFrameIndex].RaiseOnExit();
            _currentAnimation.RaiseOnExit();
        }

        var stoppedClip = _currentAnimation;

        _isPlaying = false;
        _isPaused = false;
        _currentAnimation = null;
        _currentFrameIndex = 0;
        _frameTimer = 0f;
        _clipTime = 0f;
        _animationQueue.Clear();
        _crossFadeAlpha = 1f;
        _crossFadeTimer = 0f;
        _crossFadeDuration = 0f;
        CrossFadeBaseAlpha = 1f;
        CrossFadeOutgoingClip = null;
        CrossFadeOutgoingFrame = null;

        OnStopped?.Invoke(stoppedClip);
    }

    /// <summary>
    /// Immediately jumps to a specific frame index within the active clip.
    /// </summary>
    public void SetFrame(int frameIndex)
    {
        if (_currentAnimation == null || _currentAnimation.Frames.Count == 0)
            return;

        var clamped = Math.Clamp(frameIndex, 0, _currentAnimation.Frames.Count - 1);
        if (clamped == _currentFrameIndex)
            return;

        _currentAnimation.Frames[_currentFrameIndex].RaiseOnExit();
        _currentFrameIndex = clamped;
        _frameTimer = 0f;

        float t = 0f;
        for (int i = 0; i < _currentFrameIndex; i++)
            t += _currentAnimation.Frames[i].Duration;
        _clipTime = t;

        _currentAnimation.Frames[_currentFrameIndex].RaiseOnEnter();
        OnFrameChanged?.Invoke(_currentAnimation.Frames[_currentFrameIndex]);
    }

    /// <summary>
    /// Seeks the animation to a specific time in seconds.
    /// </summary>
    /// <param name="time">Target playback time in seconds.</param>
    /// <param name="fireEvents">
    /// When <c>true</c>, fires any <see cref="ClipEvent"/> markers whose time falls in the window
    /// between the previous clip time and the new clip time.
    /// </param>
    public void SeekToTime(float time, bool fireEvents = false)
    {
        if (_currentAnimation == null || _currentAnimation.Frames.Count == 0)
            return;

        var prevClipTime = _clipTime;
        var total = _currentAnimation.TotalDuration;

        bool prevPingPongForward = _pingPongForward;

        float frameResolveTime = time;
        bool newPingPongForward = _pingPongForward;
        bool isPingPongVariant = _currentAnimation.PlaybackMode is PlaybackMode.PingPong or PlaybackMode.PingPongOnce;
        if (isPingPongVariant && total > 0f)
        {
            var cycleDuration = total * 2f;
            var posInCycle = ((time % cycleDuration) + cycleDuration) % cycleDuration;
            newPingPongForward = posInCycle < total;
            frameResolveTime = newPingPongForward ? posInCycle : total - (posInCycle - total);
        }

        var (targetIndex, targetTimer) = ResolveTimeToFrame(_currentAnimation, frameResolveTime);

        if (targetIndex != _currentFrameIndex)
        {
            _currentAnimation.Frames[_currentFrameIndex].RaiseOnExit();
            _currentFrameIndex = targetIndex;
            _currentAnimation.Frames[_currentFrameIndex].RaiseOnEnter();
            OnFrameChanged?.Invoke(_currentAnimation.Frames[_currentFrameIndex]);
        }

        _frameTimer = targetTimer;
        _pingPongForward = newPingPongForward;

        _clipTime = _currentAnimation.PlaybackMode switch
        {
            PlaybackMode.PingPong or PlaybackMode.PingPongOnce =>
                total > 0f ? ((time % (total * 2f)) + (total * 2f)) % (total * 2f) : 0f,
            PlaybackMode.OnceHoldLast or PlaybackMode.OnceHoldFirst or PlaybackMode.OnceStop =>
                Math.Clamp(time, 0f, total),
            _ => total > 0f ? ((time % total) + total) % total : 0f
        };

        if (fireEvents && _currentAnimation.Events.Count > 0)
            FireClipEvents(prevClipTime, _clipTime, prevPingPongForward);
    }

    /// <summary>
    /// Updates the animation state. Called once per frame by
    /// <see cref="Brine2D.Systems.Animation.AnimationSystem"/>.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (_crossFadeDuration > 0f && _crossFadeAlpha < 1f)
        {
            _crossFadeTimer += deltaTime;
            _crossFadeAlpha = Math.Clamp(_crossFadeTimer / _crossFadeDuration, 0f, 1f);
            if (_crossFadeAlpha >= 1f)
            {
                _crossFadeDuration = 0f;
                CrossFadeOutgoingClip = null;
                CrossFadeOutgoingFrame = null;
            }
        }

        if (!_isPlaying || _currentAnimation == null || _currentAnimation.Frames.Count == 0)
            return;

        if (_currentFrameIndex >= _currentAnimation.Frames.Count)
            _currentFrameIndex = _currentAnimation.Frames.Count - 1;

        var prevClipTime = _clipTime;
        bool prevPingPongForward = _pingPongForward;

        var clipBeforeAdvance = _currentAnimation;

        bool isPingPongVariant = _currentAnimation.PlaybackMode is PlaybackMode.PingPong or PlaybackMode.PingPongOnce;

        float advance = deltaTime * _speed;

        if (isPingPongVariant)
        {
            _clipTime += advance;
        }
        else if (_reversed)
        {
            _clipTime -= advance;
            if (_currentAnimation.PlaybackMode is not (PlaybackMode.OnceHoldLast or PlaybackMode.OnceHoldFirst or PlaybackMode.OnceStop))
            {
                var total = _currentAnimation.TotalDuration;
                if (total > 0f)
                    _clipTime = ((_clipTime % total) + total) % total;
            }
            else
            {
                _clipTime = MathF.Max(_clipTime, 0f);
            }
        }
        else
        {
            _clipTime += advance;
        }

        _frameTimer += advance;

        var newClipTime = _clipTime;

        switch (clipBeforeAdvance.PlaybackMode)
        {
            case PlaybackMode.PingPong:
            case PlaybackMode.PingPongOnce:
                AdvancePingPong();
                break;
            default:
                if (_reversed)
                    AdvanceReversed();
                else
                    AdvanceForward();
                break;
        }

        if (_currentAnimation != null && ReferenceEquals(_currentAnimation, clipBeforeAdvance) && _currentAnimation.Events.Count > 0)
            FireClipEvents(prevClipTime, newClipTime, prevPingPongForward);

        _currentAnimation?.RaiseOnUpdate(CurrentTime);
    }

    /// <summary>
    /// Notifies the animator that the active clip's frame list has been structurally mutated
    /// (frames removed or cleared) while this animator was playing it. The current frame index
    /// is clamped to valid bounds; if all frames were removed the animator is stopped.
    /// Call this after any <see cref="AnimationClip.RemoveFrame"/> or
    /// <see cref="AnimationClip.ClearFrames"/> on the <see cref="CurrentAnimation"/>.
    /// </summary>
    public void NotifyClipMutated()
    {
        if (_currentAnimation == null)
            return;

        if (_currentAnimation.Frames.Count == 0)
        {
            Stop();
            return;
        }

        if (_currentFrameIndex >= _currentAnimation.Frames.Count)
            _currentFrameIndex = _currentAnimation.Frames.Count - 1;
    }

    /// <summary>
    /// Returns the total duration of the currently set animation clip in seconds, or 0 if no
    /// animation is active.
    /// </summary>
    public float GetCurrentAnimationDuration() => _currentAnimation?.TotalDuration ?? 0f;

    /// <summary>
    /// Returns the total number of frames in the currently set animation clip, or 0 if no
    /// animation is active.
    /// </summary>
    public int GetCurrentAnimationFrameCount() => _currentAnimation?.Frames.Count ?? 0;

    /// <summary>
    /// Returns the frame duration at the given index within the currently set animation clip,
    /// or 0 if the index is out of bounds or no animation is active.
    /// </summary>
    public float GetCurrentAnimationFrameDuration(int frameIndex)
    {
        if (_currentAnimation == null || frameIndex < 0 || frameIndex >= _currentAnimation.Frames.Count)
            return 0f;
        return _currentAnimation.Frames[frameIndex].Duration;
    }

    /// <summary>
    /// Returns the start time of the given frame index in the current clip, in seconds.
    /// Returns <c>0</c> if out of bounds or nothing is active.
    /// </summary>
    public float GetCurrentAnimationSampleTime(int frameIndex)
    {
        if (_currentAnimation == null || frameIndex < 0 || frameIndex >= _currentAnimation.Frames.Count)
            return 0f;

        float sampleTime = 0f;
        for (int i = 0; i < frameIndex; i++)
            sampleTime += _currentAnimation.Frames[i].Duration;
        return sampleTime;
    }

    /// <summary>
    /// Sets the normalized time [0, 1] of the currently playing animation clip.
    /// </summary>
    public void SetCurrentAnimationNormalizedTime(float normalizedTime)
    {
        if (_currentAnimation == null)
            return;
        var clamped = Math.Clamp(normalizedTime, 0f, 1f);
        SeekToTime(clamped * _currentAnimation.TotalDuration);
    }

    /// <summary>
    /// Forces the currently playing animation clip to the given time in seconds.
    /// </summary>
    public void SetCurrentAnimationTime(float time)
    {
        if (_currentAnimation == null)
            return;
        SeekToTime(time);
    }

    /// <summary>
    /// Skips forward to the next frame in the currently playing animation clip,
    /// respecting playback direction (including ping-pong).
    /// </summary>
    public void SkipToNextFrame()
    {
        if (_currentAnimation == null)
            return;

        if (_pingPongForward)
        {
            if (_currentFrameIndex + 1 < _currentAnimation.Frames.Count)
                SetFrame(_currentFrameIndex + 1);
        }
        else
        {
            if (_currentFrameIndex - 1 >= 0)
                SetFrame(_currentFrameIndex - 1);
        }
    }

    /// <summary>
    /// Skips back to the previous frame in the currently playing animation clip,
    /// respecting playback direction (including ping-pong).
    /// </summary>
    public void SkipToPreviousFrame()
    {
        if (_currentAnimation == null)
            return;

        if (_pingPongForward)
        {
            if (_currentFrameIndex - 1 >= 0)
                SetFrame(_currentFrameIndex - 1);
        }
        else
        {
            if (_currentFrameIndex + 1 < _currentAnimation.Frames.Count)
                SetFrame(_currentFrameIndex + 1);
        }
    }

    /// <summary>
    /// Forces the animator to the logical start frame of the current clip, respecting
    /// <see cref="Reversed"/>: frame 0 for forward playback, last frame for reversed.
    /// </summary>
    public void ForceResetCurrentAnimation()
    {
        if (_currentAnimation == null || _currentAnimation.Frames.Count == 0)
            return;

        var targetIndex = _reversed ? _currentAnimation.Frames.Count - 1 : 0;

        if (targetIndex != _currentFrameIndex)
        {
            _currentAnimation.Frames[_currentFrameIndex].RaiseOnExit();
            _currentFrameIndex = targetIndex;
            _currentAnimation.Frames[_currentFrameIndex].RaiseOnEnter();
            OnFrameChanged?.Invoke(_currentAnimation.Frames[_currentFrameIndex]);
        }

        _frameTimer = 0f;
        _clipTime = _reversed ? _currentAnimation.TotalDuration : 0f;
    }

    /// <summary>
    /// Forces the animator to the logical end frame of the current clip, respecting
    /// <see cref="Reversed"/>: last frame for forward playback, frame 0 for reversed.
    /// </summary>
    public void ForceResetCurrentAnimationToEnd()
    {
        if (_currentAnimation == null || _currentAnimation.Frames.Count == 0)
            return;

        var targetIndex = _reversed ? 0 : _currentAnimation.Frames.Count - 1;

        if (targetIndex != _currentFrameIndex)
        {
            _currentAnimation.Frames[_currentFrameIndex].RaiseOnExit();
            _currentFrameIndex = targetIndex;
            _currentAnimation.Frames[_currentFrameIndex].RaiseOnEnter();
            OnFrameChanged?.Invoke(_currentAnimation.Frames[_currentFrameIndex]);
        }

        _frameTimer = 0f;
        _clipTime = _reversed ? 0f : _currentAnimation.TotalDuration;
    }

    /// <summary>
    /// Checks if the given animation clip is currently set as the active animation.
    /// </summary>
    public bool IsAnimationActive(AnimationClip clip) => _currentAnimation == clip;

    /// <summary>
    /// Plays the animation if it is not already active. Returns <c>false</c> if it was already
    /// playing.
    /// </summary>
    public bool PlayIfNotPlaying(string animationName, bool restart = false)
    {
        if (!_animations.ContainsKey(animationName))
        {
            _logger?.LogWarning("Animation not found: {Name}", animationName);
            return false;
        }

        if (_currentAnimation?.Name == animationName && !restart && _isPlaying)
            return false;

        Play(animationName, restart);
        return true;
    }

    /// <summary>
    /// Plays an <see cref="AnimationClip"/> instance directly without requiring it to be
    /// pre-registered via <see cref="AddAnimation"/>. Useful for procedurally generated or
    /// temporary clips.
    /// </summary>
    /// <param name="clip">The clip to play.</param>
    /// <param name="restart">
    /// When <c>false</c> (default): no-op if this exact clip instance is already active and
    /// playing or paused. When <c>true</c>, restarts unconditionally.
    /// </param>
    /// <exception cref="InvalidOperationException">Thrown if the clip has no frames.</exception>
    public void PlayDirect(AnimationClip clip, bool restart = false)
    {
        ArgumentNullException.ThrowIfNull(clip);

        if (clip.Frames.Count == 0)
            throw new InvalidOperationException($"Animation '{clip.Name}' has no frames.");

        if (_currentAnimation == clip && !restart && !IsFinished)
            return;

        var startFrame = _reversed ? clip.Frames.Count - 1 : 0;
        StartClip(clip, startFrame);
        _logger?.LogDebug("Playing animation (direct): {Name}", clip.Name);
    }

    /// <summary>
    /// Plays an <see cref="AnimationClip"/> instance directly with a cross-fade, without
    /// requiring it to be pre-registered via <see cref="AddAnimation"/>.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="fadeDuration"/> is not positive.</exception>
    public void PlayDirectWithCrossFade(AnimationClip clip, float fadeDuration, bool restart = false)
    {
        ArgumentNullException.ThrowIfNull(clip);
        if (fadeDuration <= 0f)
            throw new ArgumentOutOfRangeException(nameof(fadeDuration), fadeDuration, "Fade duration must be greater than zero.");

        if (clip.Frames.Count == 0)
            throw new InvalidOperationException($"Animation '{clip.Name}' has no frames.");

        var outgoingClip = _currentAnimation;
        var outgoingFrame = CurrentFrame;

        if (_currentAnimation == clip && !restart && !IsFinished)
            return;

        var prevAnimation = _currentAnimation;
        var wasPlaying = _isPlaying;

        var startFrame = _reversed ? clip.Frames.Count - 1 : 0;
        StartClip(clip, startFrame);

        if (_currentAnimation == null || (_currentAnimation == prevAnimation && !restart && wasPlaying))
            return;

        CrossFadeOutgoingClip = outgoingClip;
        CrossFadeOutgoingFrame = outgoingFrame;
        _crossFadeAlpha = 0f;
        _crossFadeTimer = 0f;
        _crossFadeDuration = fadeDuration;
        _logger?.LogDebug("Playing animation (direct cross-fade {Duration}s): {Name}", fadeDuration, clip.Name);
    }

    /// <summary>
    /// Returns <c>true</c> if an animation clip with the given name has been added.
    /// </summary>
    public bool HasAnimation(string name) => _animations.ContainsKey(name);

    /// <summary>
    /// Returns the registered clip with the given name, or <c>null</c> if not found.
    /// </summary>
    public AnimationClip? GetAnimation(string name) =>
        _animations.TryGetValue(name, out var clip) ? clip : null;

    /// <summary>
    /// Attempts to retrieve a registered animation clip by name.
    /// </summary>
    public bool TryGetAnimation(string name, out AnimationClip? clip) =>
        _animations.TryGetValue(name, out clip);
}