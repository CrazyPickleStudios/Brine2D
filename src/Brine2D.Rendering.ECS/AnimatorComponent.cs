using Brine2D.Core;
using Brine2D.Core.Animation;
using Brine2D.ECS;

namespace Brine2D.Rendering.ECS;

/// <summary>
/// Component for sprite animation.
/// Lives in Brine2D.Rendering.ECS because it's rendering-specific.
/// Bridges SpriteAnimator (core logic) with SpriteComponent (rendering).
/// </summary>
public class AnimatorComponent : Component
{
    private SpriteAnimator? _animator;
    private SpriteComponent? _sprite;

    /// <summary>
    /// Path to the sprite sheet texture.
    /// </summary>
    public string SpriteSheetPath { get; set; } = string.Empty;

    /// <summary>
    /// Current animation name.
    /// </summary>
    public string CurrentAnimation { get; private set; } = string.Empty;

    /// <summary>
    /// Whether the animator is currently playing.
    /// </summary>
    public bool IsPlaying => _animator?.IsPlaying ?? false;

    /// <summary>
    /// Playback speed multiplier (1.0 = normal speed).
    /// </summary>
    public float Speed
    {
        get => _animator?.Speed ?? 1.0f;
        set
        {
            if (_animator != null)
                _animator.Speed = value;
        }
    }

    /// <summary>
    /// Current frame from the animation.
    /// </summary>
    public SpriteFrame? CurrentFrame => _animator?.CurrentFrame;

    protected override void OnAdded()
    {
        base.OnAdded();

        // Get or add sprite component via Entity
        _sprite = Entity?.GetComponent<SpriteComponent>();
        if (_sprite == null)
        {
            _sprite = Entity?.AddComponent<SpriteComponent>();
        }
    }

    /// <summary>
    /// Initializes the animator (called by AnimationSystem or user).
    /// </summary>
    public void Initialize(SpriteAnimator animator)
    {
        _animator = animator;
    }

    /// <summary>
    /// Adds an animation clip.
    /// </summary>
    public void AddAnimation(AnimationClip clip)
    {
        _animator?.AddAnimation(clip);
    }

    /// <summary>
    /// Plays an animation by name.
    /// </summary>
    public void Play(string animationName, bool restart = false)
    {
        if (_animator == null) return;

        _animator.Play(animationName, restart);
        CurrentAnimation = animationName;

        // Update sprite component with current frame
        UpdateSpriteFromAnimation();
    }

    /// <summary>
    /// Pauses the current animation.
    /// </summary>
    public void Pause()
    {
        _animator?.Pause();
    }

    /// <summary>
    /// Resumes the current animation.
    /// </summary>
    public void Resume()
    {
        _animator?.Resume();
    }

    /// <summary>
    /// Stops the current animation.
    /// </summary>
    public void Stop()
    {
        _animator?.Stop();
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        base.OnUpdate(gameTime);

        _animator?.Update((float)gameTime.DeltaTime);

        // Sync animation frame to sprite component
        UpdateSpriteFromAnimation();
    }

    private void UpdateSpriteFromAnimation()
    {
        if (_sprite == null || _animator?.CurrentFrame == null) return;

        var frame = _animator.CurrentFrame;
        _sprite.SourceRect = frame.SourceRect;
    }
}