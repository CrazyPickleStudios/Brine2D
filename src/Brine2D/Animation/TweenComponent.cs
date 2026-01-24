using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;

namespace Brine2D.Animation;

/// <summary>
/// Component for simple interpolation animations (tweening).
/// Supports position, scale, and rotation tweening.
/// </summary>
public class TweenComponent : Component
{
    /// <summary>
    /// Type of tween to perform.
    /// </summary>
    public TweenType Type { get; set; } = TweenType.Position;

    /// <summary>
    /// Duration of the tween in seconds.
    /// </summary>
    public float Duration { get; set; } = 1f;

    /// <summary>
    /// Current time elapsed in the tween.
    /// </summary>
    public float ElapsedTime { get; set; }

    /// <summary>
    /// Whether the tween is currently playing.
    /// </summary>
    public bool IsPlaying { get; set; } = true;

    /// <summary>
    /// Whether to loop the tween.
    /// </summary>
    public bool Loop { get; set; } = false;

    /// <summary>
    /// Whether to ping-pong (reverse) the tween.
    /// </summary>
    public bool PingPong { get; set; } = false;

    /// <summary>
    /// Easing function to use.
    /// </summary>
    public EasingType Easing { get; set; } = EasingType.Linear;

    // Position tween
    public Vector2 StartPosition { get; set; }
    public Vector2 EndPosition { get; set; }

    // Scale tween
    public Vector2 StartScale { get; set; } = Vector2.One;
    public Vector2 EndScale { get; set; } = Vector2.One;

    // Rotation tween
    public float StartRotation { get; set; }
    public float EndRotation { get; set; }

    /// <summary>
    /// Event fired when tween completes.
    /// </summary>
    public event Action? OnComplete;

    private bool _isReversed;

    protected internal override void OnUpdate(GameTime gameTime)
    {
        if (!IsPlaying || !IsEnabled)
            return;

        var transform = Entity?.GetComponent<TransformComponent>();
        if (transform == null)
            return;

        ElapsedTime += (float)gameTime.DeltaTime;
        
        bool completed = ElapsedTime >= Duration;

        if (completed)
        {
            // Tween completed
            if (PingPong)
            {
                _isReversed = !_isReversed;
                ElapsedTime = 0;
            }
            else if (Loop)
            {
                ElapsedTime = 0;
            }
            else
            {
                ElapsedTime = Duration; // Clamp to duration
            }
        }

        // Calculate progress (0 to 1)
        float t = Math.Clamp(ElapsedTime / Duration, 0f, 1f);

        // Reverse if ping-ponging
        if (_isReversed)
            t = 1f - t;

        // Apply easing
        float easedT = ApplyEasing(t, Easing);

        // Apply tween based on type
        switch (Type)
        {
            case TweenType.Position:
                transform.Position = Vector2.Lerp(StartPosition, EndPosition, easedT);
                break;

            case TweenType.Scale:
                transform.Scale = Vector2.Lerp(StartScale, EndScale, easedT);
                break;

            case TweenType.Rotation:
                transform.Rotation = MathHelper.Lerp(StartRotation, EndRotation, easedT);
                break;
        }

        if (completed && !Loop && !PingPong)
        {
            IsPlaying = false;
            OnComplete?.Invoke();
        }
    }

    /// <summary>
    /// Starts or restarts the tween.
    /// </summary>
    public void Play()
    {
        ElapsedTime = 0;
        IsPlaying = true;
        _isReversed = false;
    }

    /// <summary>
    /// Pauses the tween.
    /// </summary>
    public void Pause()
    {
        IsPlaying = false;
    }

    /// <summary>
    /// Resumes the tween.
    /// </summary>
    public void Resume()
    {
        IsPlaying = true;
    }

    /// <summary>
    /// Applies easing function to interpolation value.
    /// </summary>
    private static float ApplyEasing(float t, EasingType easing)
    {
        return easing switch
        {
            EasingType.Linear => t,
            EasingType.EaseInQuad => t * t,
            EasingType.EaseOutQuad => t * (2f - t),
            EasingType.EaseInOutQuad => t < 0.5f ? 2f * t * t : -1f + (4f - 2f * t) * t,
            EasingType.EaseInCubic => t * t * t,
            EasingType.EaseOutCubic => (--t) * t * t + 1f,
            EasingType.Bounce => BounceEaseOut(t),
            _ => t
        };
    }

    private static float BounceEaseOut(float t)
    {
        if (t < 1f / 2.75f)
            return 7.5625f * t * t;
        if (t < 2f / 2.75f)
            return 7.5625f * (t -= 1.5f / 2.75f) * t + 0.75f;
        if (t < 2.5f / 2.75f)
            return 7.5625f * (t -= 2.25f / 2.75f) * t + 0.9375f;
        return 7.5625f * (t -= 2.625f / 2.75f) * t + 0.984375f;
    }
}

/// <summary>
/// Type of property to tween.
/// </summary>
public enum TweenType
{
    Position,
    Scale,
    Rotation
}

/// <summary>
/// Easing function type for tweens.
/// </summary>
public enum EasingType
{
    Linear,
    EaseInQuad,
    EaseOutQuad,
    EaseInOutQuad,
    EaseInCubic,
    EaseOutCubic,
    Bounce
}