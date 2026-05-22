namespace Brine2D.Animation;

/// <summary>
/// Lightweight code-driven animation state machine.
/// Evaluates transitions each frame and calls <see cref="SpriteAnimator.Play"/> automatically.
/// </summary>
/// <remarks>
/// Transitions are evaluated in descending <see cref="AnimationTransition.Priority"/> order.
/// Within the same priority, they fire in the order they were added. The first passing
/// condition wins.
/// <para>
/// Non-looping clips block outgoing transitions until they finish unless
/// <see cref="AnimationTransition.CanInterrupt"/> is <c>true</c>.
/// </para>
/// <para>
/// <see cref="StateTimer"/> only advances while <see cref="SpriteAnimator.IsPlaying"/> is
/// <c>true</c>, so pausing the animator also freezes the dwell clock.
/// </para>
/// <para>
/// AnyState transitions are evaluated after all regular transitions for the current state.
/// </para>
/// <para>
/// If the default state is a non-looping clip, it restarts every time it completes with no
/// matching transition. To suppress this, add an explicit on-complete transition from it to
/// itself with a condition that returns <c>false</c>, or use a looping clip as the default.
/// </para>
/// </remarks>
public partial class AnimationStateMachine : IDisposable
{
    private readonly SpriteAnimator _animator;
    private readonly List<AnimationTransition> _transitions = new();
    private readonly List<AnimationTransition> _anyTransitions = new();
    private readonly List<string> _stateHistory = new(StateHistoryCapacity + 1);
    private readonly Dictionary<string, List<Action<string?>>> _stateEnterCallbacks = new();
    private readonly Dictionary<string, List<Action<string?>>> _stateExitCallbacks = new();
    private string? _defaultState;
    private float _stateTimer;
    private string? _currentStateName;
    private string? _previousStateName;
    private int _suppressDepth;
    private bool _disposed;

    /// <summary>Maximum number of state names retained in <see cref="StateHistory"/>.</summary>
    public const int StateHistoryCapacity = 16;

    /// <summary>Gets the name of the currently active animation, or <c>null</c> if none.</summary>
    public string? CurrentState => _animator.CurrentAnimation?.Name;

    /// <summary>Gets the animation that was active before the current one, or <c>null</c>.</summary>
    public string? PreviousState => _previousStateName;

    /// <summary>
    /// Gets the elapsed time in seconds the current state has been active.
    /// Only advances while <see cref="SpriteAnimator.IsPlaying"/> is <c>true</c>, so pausing the
    /// animator also freezes this timer. Resets whenever a new animation starts (including via
    /// direct <see cref="SpriteAnimator.Play"/> calls).
    /// </summary>
    public float StateTimer => _stateTimer;

    /// <summary>Returns <c>true</c> when a default state has been configured.</summary>
    public bool HasDefaultState => _defaultState != null;

    /// <summary>Gets the total number of registered transitions (regular + AnyState).</summary>
    public int TransitionCount => _transitions.Count + _anyTransitions.Count;

    /// <summary>Read-only view of all regular (source-specific) transitions, in evaluation order.</summary>
    public IReadOnlyList<AnimationTransition> Transitions => _transitions;

    /// <summary>Read-only view of all AnyState transitions, in evaluation order.</summary>
    public IReadOnlyList<AnimationTransition> AnyTransitions => _anyTransitions;

    /// <summary>
    /// Chronological log of the last <see cref="StateHistoryCapacity"/> state names entered.
    /// Index 0 is oldest. Cleared by <see cref="ClearStateHistory"/>.
    /// </summary>
    public IReadOnlyList<string> StateHistory => _stateHistory;

    /// <summary>
    /// When <c>false</c>, transition evaluation is suspended each frame — the animator keeps
    /// playing its current clip normally but no automatic transitions fire and the default-state
    /// kickoff is skipped. <see cref="ForceState"/> and <see cref="ForceStop"/> still work
    /// regardless of this flag. Defaults to <c>true</c>.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets whether this instance has been disposed. Useful for external lifetime guards when
    /// a reference to the state machine outlives its owning entity or component.
    /// </summary>
    public bool IsDisposed => _disposed;

    /// <summary>
    /// Raised whenever the active state changes — both via automatic transitions,
    /// <see cref="ForceState"/>, and direct <see cref="SpriteAnimator.Play"/> calls.
    /// Provides the previous state name (or <c>null</c>) and the new state name (or <c>null</c>
    /// when the animator is stopped via <see cref="ForceStop"/>).
    /// </summary>
    public event Action<string?, string?>? OnStateChanged;

    public AnimationStateMachine(SpriteAnimator animator)
    {
        _animator = animator;
        _animator.OnAnimationStart += OnAnimatorAnimationStart;
        _animator.OnAnimationComplete += OnClipComplete;
        _animator.OnStopped += OnAnimatorStopped;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_disposed)
            return;

        _animator.OnAnimationStart -= OnAnimatorAnimationStart;
        _animator.OnAnimationComplete -= OnClipComplete;
        _animator.OnStopped -= OnAnimatorStopped;
        _stateEnterCallbacks.Clear();
        _stateExitCallbacks.Clear();
        _disposed = true;
        GC.SuppressFinalize(this);
    }

    /// <summary>
    /// Evaluates all transitions and fires the first valid one.
    /// Called automatically by <see cref="Brine2D.Systems.Animation.AnimationSystem"/> each tick.
    /// No-op when <see cref="IsEnabled"/> is <c>false</c>.
    /// </summary>
    public void Update(float deltaTime)
    {
        if (_disposed || !IsEnabled)
            return;

        var current = _animator.CurrentAnimation;

        if (current == null)
        {
            if (_defaultState != null)
            {
                _suppressDepth++;
                try { _animator.Play(_defaultState, restart: true); }
                finally { _suppressDepth--; }

                if (_animator.CurrentAnimation?.Name == _defaultState)
                    OnStateChanged?.Invoke(null, _defaultState);
            }
            return;
        }

        if (_animator.IsPlaying)
            _stateTimer += deltaTime;

        bool isFinishedNonLoop = current is { Loop: false } && _animator.IsFinished;
        bool isBlockedByNonLoop = current is { Loop: false } && _animator.IsPlaying && !isFinishedNonLoop;
        var normalizedTime = _animator.NormalizedTime;

        foreach (var t in _transitions)
        {
            if (t.From != current.Name) continue;
            if (t.OnComplete) continue;
            if (t.To == current.Name && !t.RestartSelf) continue;
            if (isBlockedByNonLoop && !t.CanInterrupt) continue;
            if (_stateTimer < t.MinStateDuration) continue;
            if (normalizedTime < t.MinNormalizedTime) continue;
            if (!t.Condition()) continue;

            FireTransition(t, current.Name);
            return;
        }

        foreach (var t in _anyTransitions)
        {
            if (t.OnComplete) continue;
            if (t.To == current.Name && !t.RestartSelf) continue;
            if (isBlockedByNonLoop && !t.CanInterrupt) continue;
            if (_stateTimer < t.MinStateDuration) continue;
            if (normalizedTime < t.MinNormalizedTime) continue;
            if (!t.Condition()) continue;

            FireTransition(t, current.Name);
            return;
        }
    }

    /// <summary>
    /// Returns <c>true</c> when the currently active animation matches <paramref name="stateName"/>.
    /// </summary>
    public bool IsInState(string stateName) => _animator.CurrentAnimation?.Name == stateName;

    /// <summary>
    /// Returns <c>true</c> if a transition to <paramref name="targetState"/> is eligible based
    /// on loop-block rules, <see cref="AnimationTransition.MinStateDuration"/>, and
    /// <see cref="AnimationTransition.MinNormalizedTime"/>. Conditions and on-complete transitions
    /// are not evaluated.
    /// </summary>
    public bool CanTransitionTo(string targetState)
    {
        if (_disposed)
            return false;

        var current = _animator.CurrentAnimation;
        if (current == null)
            return false;

        bool isBlockedByNonLoop = current is { Loop: false } && _animator.IsPlaying && !_animator.IsFinished;
        var normalizedTime = _animator.NormalizedTime;

        foreach (var t in _transitions)
        {
            if (t.From != current.Name) continue;
            if (t.To != targetState) continue;
            if (t.OnComplete) continue;
            if (isBlockedByNonLoop && !t.CanInterrupt) continue;
            if (_stateTimer < t.MinStateDuration) continue;
            if (normalizedTime < t.MinNormalizedTime) continue;
            return true;
        }

        foreach (var t in _anyTransitions)
        {
            if (t.To != targetState) continue;
            if (t.OnComplete) continue;
            if (isBlockedByNonLoop && !t.CanInterrupt) continue;
            if (_stateTimer < t.MinStateDuration) continue;
            if (normalizedTime < t.MinNormalizedTime) continue;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Returns the names of all destination states reachable via non-complete transitions
    /// from the active state (including AnyState transitions). Conditions are not evaluated.
    /// </summary>
    public IReadOnlyList<string> GetAvailableTransitions()
    {
        var result = new List<string>();
        if (_disposed)
            return result;

        var current = _animator.CurrentAnimation;
        if (current == null)
            return result;

        bool isBlockedByNonLoop = current is { Loop: false } && _animator.IsPlaying && !_animator.IsFinished;
        var normalizedTime = _animator.NormalizedTime;

        foreach (var t in _transitions)
        {
            if (t.From != current.Name) continue;
            if (t.OnComplete) continue;
            if (isBlockedByNonLoop && !t.CanInterrupt) continue;
            if (_stateTimer < t.MinStateDuration) continue;
            if (normalizedTime < t.MinNormalizedTime) continue;
            if (!result.Contains(t.To))
                result.Add(t.To);
        }

        foreach (var t in _anyTransitions)
        {
            if (t.OnComplete) continue;
            if (isBlockedByNonLoop && !t.CanInterrupt) continue;
            if (_stateTimer < t.MinStateDuration) continue;
            if (normalizedTime < t.MinNormalizedTime) continue;
            if (!result.Contains(t.To))
                result.Add(t.To);
        }

        return result;
    }

    /// <summary>
    /// Checks that every <c>From</c> and <c>To</c> name in all registered transitions (and the
    /// default state) refers to a clip that has actually been added to the underlying animator.
    /// Returns a list of human-readable issue strings. An empty list means everything is valid.
    /// </summary>
    public IReadOnlyList<string> ValidateTransitions()
    {
        var issues = new List<string>();

        foreach (var t in _transitions)
        {
            if (t.From != null && !_animator.HasAnimation(t.From))
                issues.Add($"Transition From='{t.From}' To='{t.To}': source clip '{t.From}' not found in animator.");
            if (!_animator.HasAnimation(t.To))
                issues.Add($"Transition From='{t.From}' To='{t.To}': destination clip '{t.To}' not found in animator.");
        }

        foreach (var t in _anyTransitions)
        {
            if (!_animator.HasAnimation(t.To))
                issues.Add($"AnyState transition To='{t.To}': destination clip '{t.To}' not found in animator.");
        }

        if (_defaultState != null && !_animator.HasAnimation(_defaultState))
            issues.Add($"Default state '{_defaultState}' not found in animator.");

        return issues;
    }

    private void OnAnimatorStopped(AnimationClip? stopped)
    {
        if (_animator.IsPlaying || _animator.IsPaused)
            return;

        var leaving = _currentStateName;
        _previousStateName = null;
        _currentStateName = null;

        if (leaving != null)
        {
            if (_stateExitCallbacks.TryGetValue(leaving, out var exitList))
            {
                foreach (var cb in exitList)
                    cb(null);
            }

            if (_suppressDepth == 0)
                OnStateChanged?.Invoke(leaving, null);
        }
    }

    private void OnAnimatorAnimationStart(AnimationClip clip)
    {
        var entering = clip.Name;
        var leaving = _currentStateName;

        _stateTimer = 0f;
        PushStateHistory(entering);

        _previousStateName = leaving;
        _currentStateName = entering;

        if (leaving != null && _stateExitCallbacks.TryGetValue(leaving, out var exitList))
        {
            foreach (var cb in exitList)
                cb(entering);
        }

        if (_suppressDepth == 0)
            OnStateChanged?.Invoke(leaving, entering);

        if (_stateEnterCallbacks.TryGetValue(entering, out var enterList))
        {
            foreach (var cb in enterList)
                cb(leaving);
        }
    }

    private void OnClipComplete(AnimationClip completed)
    {
        foreach (var t in _transitions)
        {
            if (t.From != completed.Name) continue;
            if (!t.OnComplete) continue;
            if (t.To == completed.Name && !t.RestartSelf) continue;
            if (_stateTimer < t.MinStateDuration) continue;
            if (!t.Condition()) continue;

            FireTransition(t, completed.Name);
            return;
        }

        foreach (var t in _anyTransitions)
        {
            if (!t.OnComplete) continue;
            if (t.To == completed.Name && !t.RestartSelf) continue;
            if (_stateTimer < t.MinStateDuration) continue;
            if (!t.Condition()) continue;

            FireTransition(t, completed.Name);
            return;
        }

        if (_defaultState != null)
        {
            var previous = completed.Name;
            _suppressDepth++;
            try { _animator.Play(_defaultState, restart: true); }
            finally { _suppressDepth--; }

            if (_animator.CurrentAnimation?.Name == _defaultState)
                OnStateChanged?.Invoke(previous, _defaultState);
        }
    }

    private void PushStateHistory(string stateName)
    {
        _stateHistory.Add(stateName);
        if (_stateHistory.Count > StateHistoryCapacity)
            _stateHistory.RemoveAt(0);
    }

    private void FireTransition(AnimationTransition t, string previous)
    {
        _suppressDepth++;
        try
        {
            if (t.CrossFadeDuration > 0f)
                _animator.PlayWithCrossFade(t.To, t.CrossFadeDuration);
            else
                _animator.Play(t.To, restart: t.RestartSelf);
        }
        finally
        {
            _suppressDepth--;
        }

        if (_animator.CurrentAnimation?.Name != t.To)
            return;

        t.OnFired?.Invoke();
        OnStateChanged?.Invoke(previous, t.To);
    }
}