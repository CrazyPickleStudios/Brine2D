namespace Brine2D.UI;

/// <summary>
/// Runs a list of <see cref="UITween"/> instances sequentially, each starting only
/// after the previous one completes.
/// </summary>
/// <remarks>
/// Register with <see cref="UICanvas.StartTween(UITweenSequence)"/> for automatic
/// updates, or call <see cref="Update"/> manually.
/// </remarks>
public class UITweenSequence
{
    private readonly List<UITween> _tweens = [];
    private int _currentIndex;

    // ── State ────────────────────────────────────────────────────────────────

    /// <summary>Whether every tween in the sequence has completed.</summary>
    public bool IsComplete { get; private set; }

    /// <summary>Whether the sequence is paused. Paused sequences ignore <see cref="Update"/> calls.</summary>
    public bool IsPaused { get; set; }

    /// <summary>The tween currently being animated, or <c>null</c> when the sequence is empty or complete.</summary>
    public UITween? Current =>
        _currentIndex < _tweens.Count ? _tweens[_currentIndex] : null;

    // ── Events ───────────────────────────────────────────────────────────────

    /// <summary>Fired once when all tweens have finished.</summary>
    public event Action? OnComplete;

    // ── Builder API ──────────────────────────────────────────────────────────

    /// <summary>
    /// Appends a tween to the sequence and returns <c>this</c> for chaining.
    /// </summary>
    public UITweenSequence Then(UITween tween)
    {
        ArgumentNullException.ThrowIfNull(tween);
        _tweens.Add(tween);
        return this;
    }

    /// <summary>
    /// Convenience overload — constructs and appends a tween in one call.
    /// </summary>
    public UITweenSequence Then(float from, float to, float duration, Action<float> setter,
        Func<float, float>? easing = null)
        => Then(new UITween(from, to, duration, setter, easing));

    // ── Lifecycle ────────────────────────────────────────────────────────────

    /// <summary>Advances the currently active tween by <paramref name="deltaTime"/> seconds.</summary>
    public void Update(float deltaTime)
    {
        if (IsComplete || IsPaused || _tweens.Count == 0) return;

        while (_currentIndex < _tweens.Count)
        {
            var current = _tweens[_currentIndex];
            current.Update(deltaTime);

            if (!current.IsComplete) break;

            // Carry the overshoot time into the next tween so the sequence
            // stays temporally accurate across frame boundaries.
            float overshoot = current.OvershootTime;
            _currentIndex++;

            if (_currentIndex >= _tweens.Count)
            {
                IsComplete = true;
                OnComplete?.Invoke();
                return;
            }

            deltaTime = overshoot;
        }
    }

    /// <summary>
    /// Immediately completes all remaining tweens in the sequence and fires <see cref="OnComplete"/>.
    /// </summary>
    public void CompleteAll()
    {
        if (IsComplete) return;
        while (_currentIndex < _tweens.Count)
        {
            _tweens[_currentIndex].Complete();
            _currentIndex++;
        }
        IsComplete = true;
        OnComplete?.Invoke();
    }

    /// <summary>Resets all tweens and restarts from the first one.</summary>
    public void Reset()
    {
        foreach (var t in _tweens)
            t.Reset();
        _currentIndex = 0;
        IsComplete = false;
        IsPaused = false;
    }
}
