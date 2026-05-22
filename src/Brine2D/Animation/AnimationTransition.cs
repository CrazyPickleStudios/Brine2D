namespace Brine2D.Animation;

/// <summary>
/// Represents a single conditional transition between two animation states.
/// </summary>
/// <param name="From">The animation name this transition originates from, or null for AnyState.</param>
/// <param name="To">The animation name to transition to.</param>
/// <param name="Condition">
/// The condition that must be true for the transition to fire. When <see cref="OnComplete"/> is
/// <c>true</c>, <see cref="AnimationStateMachine.AddOnCompleteTransition"/> wraps this with a
/// guaranteed <c>() =&gt; true</c> fallback — pass your own condition to make an on-complete
/// transition conditional.
/// </param>
/// <param name="CanInterrupt">Whether this transition can interrupt a non-looping clip mid-play.</param>
/// <param name="CrossFadeDuration">
/// When greater than zero the state machine fires
/// <see cref="SpriteAnimator.PlayWithCrossFade"/> instead of <see cref="SpriteAnimator.Play"/>,
/// blending from the outgoing to the incoming clip over this many seconds. Zero means hard cut.
/// </param>
/// <param name="MinStateDuration">
/// Minimum time in seconds the source animation must have been playing before this transition
/// can fire. Zero means no minimum.
/// </param>
/// <param name="MinNormalizedTime">
/// Minimum normalized playback position [0, 1] the source animation must have reached before
/// this transition can fire. Zero means no minimum. Use this for exit-time transitions —
/// for example, 0.8 means "don't allow this transition until the clip is 80% complete".
/// </param>
/// <param name="OnComplete">
/// When <c>true</c>, this transition is evaluated only after a non-looping source clip reaches
/// its natural end. <see cref="Condition"/> is still evaluated — use
/// <see cref="AnimationStateMachine.AddOnCompleteTransition"/> with <c>null</c> condition for an
/// unconditional on-complete transition.
/// </param>
/// <param name="Priority">
/// Evaluation order within the same transition list. Higher values are evaluated first.
/// Transitions with the same priority are evaluated in insertion order.
/// Defaults to <c>0</c>. Use positive values for high-priority overrides and negative values
/// for low-priority fallbacks.
/// </param>
/// <param name="RestartSelf">
/// When <c>true</c> and <c>To == From</c> (or an AnyState transition whose target is currently
/// active), the transition is allowed to fire and restarts the clip from the beginning instead
/// of being silently skipped. Defaults to <c>false</c>.
/// </param>
public sealed record AnimationTransition(
    string? From,
    string To,
    Func<bool> Condition,
    bool CanInterrupt,
    float CrossFadeDuration = 0f,
    float MinStateDuration = 0f,
    float MinNormalizedTime = 0f,
    bool OnComplete = false,
    int Priority = 0,
    bool RestartSelf = false)
{
    /// <summary>
    /// Optional action invoked immediately after this transition fires (after the new clip has
    /// started). Use this to perform side-effects that should only happen when the transition
    /// actually fires — for example, consuming an <see cref="AnimationParameters"/> trigger
    /// without risking early consumption from short-circuit evaluation in
    /// <see cref="Condition"/>.
    /// <para>
    /// See <see cref="AnimationStateMachine.AddTriggerTransition"/> for a ready-made helper
    /// that wires this up automatically for trigger-based transitions.
    /// </para>
    /// </summary>
    public Action? OnFired { get; init; }
}