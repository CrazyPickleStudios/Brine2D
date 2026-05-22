namespace Brine2D.Animation;

/// <summary>
/// Immutable snapshot of <see cref="AnimationStateMachine"/> runtime state.
/// Use with <see cref="AnimationStateMachine.CaptureSnapshot"/> and
/// <see cref="AnimationStateMachine.RestoreSnapshot"/> for save/load and rollback systems.
/// </summary>
public sealed record AnimationStateMachineSnapshot(
    float StateTimer,
    string? PreviousState,
    string? CurrentStateName,
    string? DefaultState,
    bool IsEnabled);