using Brine2D.Core;
using Brine2D.Rendering;
using Brine2D.Rendering.TextureAtlas;

namespace Brine2D.Animation;

/// <summary>
/// Represents an animation clip with multiple frames.
/// </summary>
public class AnimationClip
{
    private readonly List<SpriteFrame> _frames = new();
    private readonly List<ClipEvent> _events = new();
    private float _cachedTotalDuration;
    private bool _totalDurationDirty = true;

    /// <summary>
    /// Name of the animation (e.g., "walk", "jump", "idle").
    /// </summary>
    public string Name { get; init; }

    /// <summary>
    /// Controls how the clip loops. Defaults to <see cref="PlaybackMode.Loop"/>.
    /// </summary>
    public PlaybackMode PlaybackMode { get; set; } = PlaybackMode.Loop;

    /// <summary>
    /// Shorthand for <see cref="PlaybackMode"/>. Setting to <c>false</c> maps the current mode
    /// to its non-looping equivalent: <see cref="PlaybackMode.Loop"/> becomes
    /// <see cref="PlaybackMode.Once"/>; <see cref="PlaybackMode.PingPong"/> becomes
    /// <see cref="PlaybackMode.PingPongOnce"/>. Setting to <c>true</c> always sets
    /// <see cref="PlaybackMode.Loop"/> — use <see cref="PlaybackMode"/> directly for ping-pong.
    /// </summary>
    public bool Loop
    {
        get => PlaybackMode == PlaybackMode.Loop || PlaybackMode == PlaybackMode.PingPong;
        set
        {
            if (value)
            {
                PlaybackMode = PlaybackMode.Loop;
            }
            else if (PlaybackMode == PlaybackMode.PingPong)
            {
                PlaybackMode = PlaybackMode.PingPongOnce;
            }
            else if (PlaybackMode == PlaybackMode.Loop)
            {
                PlaybackMode = PlaybackMode.OnceHoldLast;
            }
        }
    }

    /// <summary>
    /// Number of full passes before firing <see cref="SpriteAnimator.OnAnimationComplete"/>.
    /// Only meaningful for <see cref="PlaybackMode.Loop"/> and <see cref="PlaybackMode.PingPong"/>.
    /// <c>0</c> (default) loops indefinitely. <see cref="SpriteAnimator.OnLoopComplete"/> fires
    /// on each pass regardless.
    /// </summary>
    public int RepeatCount { get; set; }

    /// <summary>
    /// Read-only view of frames in this animation. Use <see cref="AddFrame"/>,
    /// <see cref="InsertFrame"/>, <see cref="RemoveFrame"/>, and <see cref="ClearFrames"/> to mutate.
    /// </summary>
    public IReadOnlyList<SpriteFrame> Frames => _frames;

    /// <summary>
    /// Read-only view of clip event markers. Use <see cref="AddEvent"/> and
    /// <see cref="RemoveEvent(string)"/> to mutate.
    /// </summary>
    public IReadOnlyList<ClipEvent> Events => _events;

    /// <summary>
    /// Total duration in seconds. Cached; invalidated automatically when frames are added, removed,
    /// or a frame's <see cref="SpriteFrame.Duration"/> changes while it belongs to this clip.
    /// </summary>
    public float TotalDuration
    {
        get
        {
            if (!_totalDurationDirty)
                return _cachedTotalDuration;

            float total = 0f;
            foreach (var frame in _frames)
                total += frame.Duration;
            _cachedTotalDuration = total;
            _totalDurationDirty = false;
            return _cachedTotalDuration;
        }
    }

    /// <summary>
    /// Optional clip-level texture path. Written to <see cref="Brine2D.Systems.Rendering.SpriteComponent.TexturePath"/>
    /// each frame. A per-frame <see cref="SpriteFrame.TexturePath"/> takes priority.
    /// </summary>
    public string? TexturePath { get; set; }

    /// <summary>
    /// Optional clip-level pre-loaded texture. Written to <see cref="Brine2D.Systems.Rendering.SpriteComponent.Texture"/>
    /// each frame. A per-frame <see cref="SpriteFrame.Texture"/> takes priority.
    /// </summary>
    public ITexture? Texture { get; set; }

    /// <summary>
    /// Optional tint applied to <see cref="Brine2D.Systems.Rendering.SpriteComponent.Tint"/> while
    /// this clip is active. A per-frame <see cref="SpriteFrame.Tint"/> takes priority.
    /// </summary>
    public Color? ClipTint { get; set; }

    /// <summary>
    /// Arbitrary per-clip payload. Use this to attach game-specific data (e.g. clip categories,
    /// tags, metadata) without subclassing. Not consumed by the animation system.
    /// </summary>
    public object? UserData { get; set; }

    /// <summary>Raised once when this clip becomes the active clip via <see cref="SpriteAnimator.Play"/>.</summary>
    public event Action? OnEnter;

    /// <summary>Raised once when this clip is replaced by a different clip via <see cref="SpriteAnimator.Play"/>.</summary>
    public event Action? OnExit;

    /// <summary>
    /// Raised every tick while this clip is the active clip and the animator is playing.
    /// Receives the elapsed clip time in seconds.
    /// </summary>
    public event Action<float>? OnUpdate;

    public AnimationClip(string name)
    {
        ArgumentNullException.ThrowIfNull(name);
        Name = name;
    }

    internal void RaiseOnEnter() => OnEnter?.Invoke();
    internal void RaiseOnExit() => OnExit?.Invoke();
    internal void RaiseOnUpdate(float time) => OnUpdate?.Invoke(time);

    /// <summary>
    /// Appends a frame to the end of the clip.
    /// </summary>
    public AnimationClip AddFrame(SpriteFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        frame.RegisterOwningClip(this);
        _frames.Add(frame);
        _totalDurationDirty = true;
        return this;
    }

    /// <summary>
    /// Inserts a frame at the specified index.
    /// </summary>
    public AnimationClip InsertFrame(int index, SpriteFrame frame)
    {
        ArgumentNullException.ThrowIfNull(frame);
        frame.RegisterOwningClip(this);
        _frames.Insert(index, frame);
        _totalDurationDirty = true;
        return this;
    }

    /// <summary>
    /// Removes a specific frame from the clip.
    /// </summary>
    public bool RemoveFrame(SpriteFrame frame)
    {
        var removed = _frames.Remove(frame);
        if (removed)
        {
            frame.UnregisterOwningClip(this);
            _totalDurationDirty = true;
        }
        return removed;
    }

    /// <summary>
    /// Removes all frames from the clip.
    /// </summary>
    public AnimationClip ClearFrames()
    {
        foreach (var frame in _frames)
            frame.UnregisterOwningClip(this);
        _frames.Clear();
        _cachedTotalDuration = 0f;
        _totalDurationDirty = false;
        return this;
    }

    /// <summary>
    /// Marks the <see cref="TotalDuration"/> cache as dirty, forcing a recompute on next access.
    /// Also re-resolves the times of any events registered via <see cref="AddEventAtFrame"/> so
    /// that frame-index-based events remain accurate after a frame's
    /// <see cref="SpriteFrame.Duration"/> is mutated.
    /// </summary>
    public void InvalidateDurationCache()
    {
        _totalDurationDirty = true;
        ResolveFrameIndexedEventTimes();
    }

    /// <summary>
    /// Adds a named event marker that fires when playback crosses the given time offset.
    /// Events are stored sorted by time.
    /// </summary>
    public AnimationClip AddEvent(string name, float time, Action<ClipEventArgs> callback, bool fireBothDirections = false)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(callback);
        _events.Add(new ClipEvent(name, time, callback, fireBothDirections));
        _events.Sort(static (a, b) => a.Time.CompareTo(b.Time));
        return this;
    }

    /// <summary>
    /// Adds a named event marker that fires when playback reaches the given zero-based frame index.
    /// Unlike <see cref="AddEvent"/>, the resolved time is automatically kept up-to-date when any
    /// frame's <see cref="SpriteFrame.Duration"/> changes while it belongs to this clip.
    /// </summary>
    /// <param name="name">Identifier for this event.</param>
    /// <param name="frameIndex">Zero-based index of the frame at which the event should fire.</param>
    /// <param name="callback">The action to invoke when the event fires.</param>
    /// <param name="fireBothDirections">
    /// When <c>true</c> and the owning clip uses <see cref="PlaybackMode.PingPong"/>, the event
    /// also fires during the backward sweep.
    /// </param>
    /// <exception cref="ArgumentOutOfRangeException">
    /// Thrown if <paramref name="frameIndex"/> is negative or exceeds the current frame count.
    /// </exception>
    public AnimationClip AddEventAtFrame(string name, int frameIndex, Action<ClipEventArgs> callback, bool fireBothDirections = false)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(callback);
        ArgumentOutOfRangeException.ThrowIfNegative(frameIndex);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(frameIndex, _frames.Count);

        var time = ComputeFrameStartTime(frameIndex);
        _events.Add(new ClipEvent(name, time, callback, fireBothDirections, FrameIndex: frameIndex));
        _events.Sort(static (a, b) => a.Time.CompareTo(b.Time));
        return this;
    }

    /// <summary>
    /// Removes the first event with the given name.
    /// </summary>
    public bool RemoveEvent(string name)
    {
        var index = _events.FindIndex(e => e.Name == name);
        if (index < 0)
            return false;
        _events.RemoveAt(index);
        return true;
    }

    /// <summary>
    /// Removes a specific event instance. Use this when multiple events share the same name.
    /// </summary>
    public bool RemoveEvent(ClipEvent clipEvent) => _events.Remove(clipEvent);

    /// <summary>
    /// Removes all event markers from this clip.
    /// </summary>
    public AnimationClip ClearEvents()
    {
        _events.Clear();
        return this;
    }

    /// <summary>
    /// Captures a snapshot of this clip's mutable runtime state. Frame lists and events are not
    /// included; use <see cref="Clone"/> for a full structural copy.
    /// </summary>
    public AnimationClipSnapshot CaptureSnapshot() =>
        new(PlaybackMode, RepeatCount, TexturePath, Texture, UserData, ClipTint);

    /// <summary>Restores a previously captured snapshot.</summary>
    public void RestoreSnapshot(AnimationClipSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        PlaybackMode = snapshot.PlaybackMode;
        RepeatCount = snapshot.RepeatCount;
        TexturePath = snapshot.TexturePath;
        Texture = snapshot.Texture;
        UserData = snapshot.UserData;
        ClipTint = snapshot.ClipTint;
    }

    /// <summary>
    /// Creates a shallow copy under a new name. Frames are shared, not deep copied. Event
    /// callbacks are not copied.
    /// </summary>
    /// <param name="newName">Name for the cloned clip.</param>
    public AnimationClip Clone(string newName)
    {
        ArgumentNullException.ThrowIfNull(newName);
        var clone = new AnimationClip(newName)
        {
            PlaybackMode = PlaybackMode,
            RepeatCount = RepeatCount,
            TexturePath = TexturePath,
            Texture = Texture,
            ClipTint = ClipTint,
            UserData = UserData,
        };

        foreach (var frame in _frames)
            clone.AddFrame(frame);

        foreach (var e in _events)
            clone._events.Add(e);

        clone._events.Sort(static (a, b) => a.Time.CompareTo(b.Time));
        return clone;
    }

    /// <summary>
    /// Creates an animation from a sprite sheet with uniform frame sizes.
    /// </summary>
    public static AnimationClip FromSpriteSheet(
        string name,
        int frameCount,
        int frameWidth,
        int frameHeight,
        int columns,
        int startX = 0,
        int startY = 0,
        float frameDuration = 0.1f,
        PlaybackMode playbackMode = PlaybackMode.Loop,
        string? texturePath = null,
        ITexture? texture = null)
    {
        var clip = new AnimationClip(name)
        {
            PlaybackMode = playbackMode,
            TexturePath = texturePath,
            Texture = texture
        };

        for (int i = 0; i < frameCount; i++)
        {
            int col = i % columns;
            int row = i / columns;
            var rect = new Rectangle(
                startX + col * frameWidth,
                startY + row * frameHeight,
                frameWidth,
                frameHeight);
            clip.AddFrame(new SpriteFrame(rect, frameDuration));
        }

        return clip;
    }

    /// <summary>
    /// Creates an animation clip from a sequence of <see cref="Brine2D.Rendering.TextureAtlas.AtlasRegion"/>s.
    /// Each region becomes one frame; the frame's <see cref="SpriteFrame.Texture"/> is set to
    /// the region's <see cref="Brine2D.Rendering.TextureAtlas.AtlasRegion.AtlasTexture"/> and
    /// <see cref="SpriteFrame.SourceRect"/> is set to the region's
    /// <see cref="Brine2D.Rendering.TextureAtlas.AtlasRegion.SourceRect"/>.
    /// </summary>
    public static AnimationClip FromAtlasRegions(
        string name,
        IReadOnlyList<AtlasRegion> regions,
        float frameDuration = 0.1f,
        PlaybackMode playbackMode = PlaybackMode.Loop)
    {
        ArgumentNullException.ThrowIfNull(regions);
        if (regions.Count == 0)
            throw new ArgumentException("At least one region is required.", nameof(regions));

        var clip = new AnimationClip(name) { PlaybackMode = playbackMode };

        foreach (var region in regions)
        {
            var frame = new SpriteFrame(region.SourceRect, frameDuration)
            {
                Texture = region.AtlasTexture
            };
            clip.AddFrame(frame);
        }

        return clip;
    }

    private float ComputeFrameStartTime(int frameIndex)
    {
        float t = 0f;
        for (int i = 0; i < frameIndex && i < _frames.Count; i++)
            t += _frames[i].Duration;
        return t;
    }

    private void ResolveFrameIndexedEventTimes()
    {
        for (int i = 0; i < _events.Count; i++)
        {
            var e = _events[i];
            if (e.FrameIndex.HasValue)
            {
                var newTime = ComputeFrameStartTime(e.FrameIndex.Value);
                _events[i] = e with { Time = newTime };
            }
        }
        _events.Sort(static (a, b) => a.Time.CompareTo(b.Time));
    }
}