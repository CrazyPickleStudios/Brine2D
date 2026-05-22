namespace Brine2D.Animation;

public partial class AnimationStateMachine
{
    /// <summary>
    /// Captures the current runtime state into an immutable <see cref="AnimationStateMachineSnapshot"/>.
    /// Use with <see cref="RestoreSnapshot"/> for save/load and rollback systems.
    /// Registered transitions, callbacks, and state history are not included.
    /// </summary>
    public AnimationStateMachineSnapshot CaptureSnapshot() =>
        new(_stateTimer, _previousStateName, _currentStateName, _defaultState, IsEnabled);

    /// <summary>
    /// Restores runtime state from a previously captured <see cref="AnimationStateMachineSnapshot"/>.
    /// Does not fire <see cref="OnStateChanged"/>, <see cref="OnStateEnter"/>, or
    /// <see cref="OnStateExit"/>. Pair with <see cref="SpriteAnimator.RestorePlaybackSnapshot"/>
    /// to perform a full animation restore.
    /// </summary>
    public void RestoreSnapshot(AnimationStateMachineSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        _stateTimer = snapshot.StateTimer;
        _previousStateName = snapshot.PreviousState;
        _currentStateName = snapshot.CurrentStateName;
        _defaultState = snapshot.DefaultState;
        IsEnabled = snapshot.IsEnabled;
    }
}