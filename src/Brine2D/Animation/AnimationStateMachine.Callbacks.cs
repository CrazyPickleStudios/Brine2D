namespace Brine2D.Animation;

public partial class AnimationStateMachine
{
    /// <summary>
    /// Registers a callback to invoke whenever the named state is entered.
    /// The callback receives the name of the previous state (or <c>null</c> if this is the
    /// first state entered). Multiple callbacks per state are supported; each call appends to
    /// the subscription list. Use <see cref="RemoveStateEnterCallback"/> with the exact delegate
    /// to remove a specific subscription.
    /// </summary>
    public AnimationStateMachine OnStateEnter(string stateName, Action<string?> callback)
    {
        ArgumentNullException.ThrowIfNull(stateName);
        ArgumentNullException.ThrowIfNull(callback);
        if (!_stateEnterCallbacks.TryGetValue(stateName, out var list))
        {
            list = new List<Action<string?>>();
            _stateEnterCallbacks[stateName] = list;
        }
        list.Add(callback);
        return this;
    }

    /// <summary>
    /// Registers a callback to invoke whenever the named state is exited.
    /// The callback receives the name of the state being entered next, or <c>null</c> when
    /// the animator is stopped rather than transitioning to another state.
    /// Multiple callbacks per state are supported.
    /// </summary>
    public AnimationStateMachine OnStateExit(string stateName, Action<string?> callback)
    {
        ArgumentNullException.ThrowIfNull(stateName);
        ArgumentNullException.ThrowIfNull(callback);
        if (!_stateExitCallbacks.TryGetValue(stateName, out var list))
        {
            list = new List<Action<string?>>();
            _stateExitCallbacks[stateName] = list;
        }
        list.Add(callback);
        return this;
    }

    /// <summary>
    /// Removes a specific enter callback previously registered for <paramref name="stateName"/>.
    /// Returns <c>true</c> if the exact delegate was found and removed.
    /// </summary>
    public bool RemoveStateEnterCallback(string stateName, Action<string?> callback)
    {
        ArgumentNullException.ThrowIfNull(stateName);
        ArgumentNullException.ThrowIfNull(callback);
        return _stateEnterCallbacks.TryGetValue(stateName, out var list) && list.Remove(callback);
    }

    /// <summary>
    /// Removes all enter callbacks registered for <paramref name="stateName"/>.
    /// Returns <c>true</c> if any were registered.
    /// </summary>
    public bool RemoveStateEnterCallback(string stateName)
    {
        ArgumentNullException.ThrowIfNull(stateName);
        return _stateEnterCallbacks.Remove(stateName);
    }

    /// <summary>
    /// Removes a specific exit callback previously registered for <paramref name="stateName"/>.
    /// Returns <c>true</c> if the exact delegate was found and removed.
    /// </summary>
    public bool RemoveStateExitCallback(string stateName, Action<string?> callback)
    {
        ArgumentNullException.ThrowIfNull(stateName);
        ArgumentNullException.ThrowIfNull(callback);
        return _stateExitCallbacks.TryGetValue(stateName, out var list) && list.Remove(callback);
    }

    /// <summary>
    /// Removes all exit callbacks registered for <paramref name="stateName"/>.
    /// Returns <c>true</c> if any were registered.
    /// </summary>
    public bool RemoveStateExitCallback(string stateName)
    {
        ArgumentNullException.ThrowIfNull(stateName);
        return _stateExitCallbacks.Remove(stateName);
    }

    /// <summary>
    /// Sets the animation that plays automatically when the animator has no active animation,
    /// or when a non-looping clip finishes and no transition fires.
    /// Pass <c>null</c> to clear the default state.
    /// </summary>
    public AnimationStateMachine SetDefaultState(string? animationName)
    {
        _defaultState = animationName;
        return this;
    }

    /// <summary>Resets <see cref="StateTimer"/> to zero without triggering a transition.</summary>
    public void ResetStateTimer() => _stateTimer = 0f;

    /// <summary>Clears the <see cref="StateHistory"/> log.</summary>
    public void ClearStateHistory() => _stateHistory.Clear();
}