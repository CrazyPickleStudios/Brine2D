namespace Brine2D.UI;

/// <summary>
/// Loop behaviour for a <see cref="UITween"/>.
/// </summary>
public enum UITweenLoopMode
{
    /// <summary>Run once then stop.</summary>
    Once,
    /// <summary>Restart from the beginning on each completion.</summary>
    Loop,
    /// <summary>Ping-pong between start and end values.</summary>
    PingPong
}

/// <summary>
/// Animates a single <c>float</c> property from a start value to an end value over time
/// via a setter delegate.
/// </summary>
/// <remarks>
/// Advance manually by calling <see cref="Update"/> each frame, or register with
/// <see cref="UICanvas.StartTween"/> for automatic updates.
/// </remarks>
/// <example>
/// <code>
/// // Slide a panel 200 px to the right over 0.4 s with a cubic ease-out.
/// var tween = new UITween(
///     from: panel.Position.X,
///     to: panel.Position.X + 200f,
///     duration: 0.4f,
///     setter: v => panel.Position = panel.Position with { X = v },
///     easing: UIEasing.CubicOut);
/// canvas.StartTween(tween);
/// </code>
/// </example>
public class UITween
{
    private readonly float _from;
    private readonly float _to;
    private readonly Action<float> _setter;
    private readonly Func<float, float> _easing;
    private float _elapsed;
    private bool _forward = true;

    // ── Configuration ────────────────────────────────────────────────────────

    /// <summary>Total animation duration in seconds.</summary>
    public float Duration { get; }

    /// <summary>Seconds to wait before the animation begins.</summary>
    public float Delay { get; set; }

    /// <summary>Determines what happens when the tween completes.</summary>
    public UITweenLoopMode LoopMode { get; set; } = UITweenLoopMode.Once;

    // ── State ────────────────────────────────────────────────────────────────

    /// <summary>Whether the tween has finished (always false for looping tweens).</summary>
    public bool IsComplete { get; private set; }

    /// <summary>Whether the tween is paused. Paused tweens ignore <see cref="Update"/> calls.</summary>
    public bool IsPaused { get; set; }

    /// <summary>Normalised progress in [0, 1] (before easing is applied).</summary>
    public float Progress => Duration > 0f ? Math.Clamp(_elapsed / Duration, 0f, 1f) : 1f;

    /// <summary>
    /// Time elapsed beyond <see cref="Duration"/> on the frame the tween completed.
    /// Zero while the tween is still running or before it has been updated.
    /// </summary>
    internal float OvershootTime { get; private set; }

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>Fired each frame the tween advances, with the current eased value.</summary>
    public event Action<float>? OnUpdate;

    /// <summary>Fired once when a <see cref="UITweenLoopMode.Once"/> tween reaches its end.</summary>
    public event Action? OnComplete;

    // ── Construction ─────────────────────────────────────────────────────────

    /// <summary>
    /// Creates a new tween.
    /// </summary>
    /// <param name="from">Start value.</param>
    /// <param name="to">End value.</param>
    /// <param name="duration">Duration in seconds. Must be &gt; 0.</param>
    /// <param name="setter">Called each frame with the current animated value.</param>
    /// <param name="easing">
    /// Easing function accepting normalised time [0,1] and returning normalised progress.
    /// Any method from <see cref="UIEasing"/> or a custom lambda works.
    /// Defaults to <see cref="UIEasing.Linear"/> when <c>null</c>.
    /// </param>
    public UITween(float from, float to, float duration, Action<float> setter,
        Func<float, float>? easing = null)
    {
        ArgumentNullException.ThrowIfNull(setter);
        if (duration <= 0f) throw new ArgumentOutOfRangeException(nameof(duration), "Duration must be greater than zero.");

        _from = from;
        _to = to;
        Duration = duration;
        _setter = setter;
        _easing = easing ?? UIEasing.Linear;
    }

    // ── Public API ───────────────────────────────────────────────────────────

    /// <summary>Advances the tween by <paramref name="deltaTime"/> seconds.</summary>
    public void Update(float deltaTime)
    {
        if (IsComplete || IsPaused) return;

        // Delay handling
        if (Delay > 0f)
        {
            Delay -= deltaTime;
            if (Delay > 0f) return;
            deltaTime = -Delay;
            Delay = 0f;
        }

        _elapsed += deltaTime;

        if (_elapsed >= Duration)
        {
            switch (LoopMode)
            {
                case UITweenLoopMode.Once:
                    OvershootTime = _elapsed - Duration;
                    _elapsed = Duration;
                    Apply();
                    IsComplete = true;
                    OnComplete?.Invoke();
                    return;

                case UITweenLoopMode.Loop:
                    _elapsed -= Duration;
                    break;

                case UITweenLoopMode.PingPong:
                    _elapsed -= Duration;
                    _forward = !_forward;
                    break;
            }
        }

        Apply();
    }

    /// <summary>Immediately jumps to the end value and marks the tween complete.</summary>
    public void Complete()
    {
        if (IsComplete) return;
        _elapsed = Duration;
        Apply();
        IsComplete = true;
        OnComplete?.Invoke();
    }

    /// <summary>Resets the tween to the start value and marks it not complete.</summary>
    public void Reset()
    {
        _elapsed = 0f;
        _forward = true;
        IsComplete = false;
        IsPaused = false;
        Delay = 0f;
        OvershootTime = 0f;
        Apply();
    }

    // ── Internal ─────────────────────────────────────────────────────────────

    private void Apply()
    {
        float t = Duration > 0f ? _elapsed / Duration : 1f;
        t = Math.Clamp(t, 0f, 1f);
        if (!_forward) t = 1f - t;
        float eased = _easing(t);
        float value = _from + (_to - _from) * eased;
        _setter(value);
        OnUpdate?.Invoke(value);
    }
}
