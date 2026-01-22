using Microsoft.Extensions.Logging;

namespace Brine2D.Animation;

/// <summary>
/// Animates sprites by playing animation clips.
/// </summary>
public class SpriteAnimator
{
    private readonly ILogger<SpriteAnimator>? _logger;
    private readonly Dictionary<string, AnimationClip> _animations = new();
    
    private AnimationClip? _currentAnimation;
    private int _currentFrameIndex;
    private float _frameTimer;
    private bool _isPlaying;

    /// <summary>
    /// Gets the currently playing animation clip.
    /// </summary>
    public AnimationClip? CurrentAnimation => _currentAnimation;

    /// <summary>
    /// Gets the current frame.
    /// </summary>
    public SpriteFrame? CurrentFrame => 
        _currentAnimation != null && _currentFrameIndex < _currentAnimation.Frames.Count 
            ? _currentAnimation.Frames[_currentFrameIndex] 
            : null;

    /// <summary>
    /// Gets whether an animation is currently playing.
    /// </summary>
    public bool IsPlaying => _isPlaying;

    /// <summary>
    /// Gets or sets the playback speed multiplier.
    /// </summary>
    public float Speed { get; set; } = 1.0f;

    public SpriteAnimator(ILogger<SpriteAnimator>? logger = null)
    {
        _logger = logger;
    }

    /// <summary>
    /// Adds an animation clip.
    /// </summary>
    public void AddAnimation(AnimationClip clip)
    {
        if (clip == null) throw new ArgumentNullException(nameof(clip));
        
        _animations[clip.Name] = clip;
        _logger?.LogDebug("Added animation: {Name} ({FrameCount} frames)", clip.Name, clip.Frames.Count);
    }

    /// <summary>
    /// Plays an animation by name.
    /// </summary>
    /// <param name="animationName">Name of the animation to play.</param>
    /// <param name="restart">If true, restarts the animation even if it's already playing.</param>
    public void Play(string animationName, bool restart = false)
    {
        if (!_animations.TryGetValue(animationName, out var animation))
        {
            _logger?.LogWarning("Animation not found: {Name}", animationName);
            return;
        }

        if (_currentAnimation == animation && !restart)
        {
            if (!_isPlaying)
            {
                _isPlaying = true;
            }
            return;
        }

        _currentAnimation = animation;
        _currentFrameIndex = 0;
        _frameTimer = 0;
        _isPlaying = true;

        _logger?.LogDebug("Playing animation: {Name}", animationName);
    }

    /// <summary>
    /// Pauses the current animation.
    /// </summary>
    public void Pause()
    {
        _isPlaying = false;
    }

    /// <summary>
    /// Resumes the current animation.
    /// </summary>
    public void Resume()
    {
        _isPlaying = true;
    }

    /// <summary>
    /// Stops the current animation and resets to the first frame.
    /// </summary>
    public void Stop()
    {
        _isPlaying = false;
        _currentFrameIndex = 0;
        _frameTimer = 0;
    }

    /// <summary>
    /// Updates the animation.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (!_isPlaying || _currentAnimation == null || _currentAnimation.Frames.Count == 0)
            return;

        _frameTimer += deltaTime * Speed;

        var currentFrame = _currentAnimation.Frames[_currentFrameIndex];
        
        while (_frameTimer >= currentFrame.Duration)
        {
            _frameTimer -= currentFrame.Duration;
            _currentFrameIndex++;

            // Check if we've reached the end
            if (_currentFrameIndex >= _currentAnimation.Frames.Count)
            {
                if (_currentAnimation.Loop)
                {
                    _currentFrameIndex = 0;
                }
                else
                {
                    _currentFrameIndex = _currentAnimation.Frames.Count - 1;
                    _isPlaying = false;
                    _logger?.LogDebug("Animation finished: {Name}", _currentAnimation.Name);
                    break;
                }
            }

            currentFrame = _currentAnimation.Frames[_currentFrameIndex];
        }
    }

    /// <summary>
    /// Gets an animation by name.
    /// </summary>
    public AnimationClip? GetAnimation(string name)
    {
        _animations.TryGetValue(name, out var animation);
        return animation;
    }

    /// <summary>
    /// Checks if an animation exists.
    /// </summary>
    public bool HasAnimation(string name) => _animations.ContainsKey(name);
}