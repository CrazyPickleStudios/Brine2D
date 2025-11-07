namespace Brine2D.Core.Input.Bindings;

/// <summary>
///     Defines a single contributor to a logical axis.
/// </summary>
/// <remarks>
///     Implementations read from one or more input sources (keyboard, mouse, gamepad) and return a raw contribution
///     ideally in the normalized range [-1, 1]. An input mapping system may then compose multiple bindings
///     (e.g., sum, average, max, or prioritize) and clamp the final value to a valid range.
///     Bindings may be stateful (e.g., smoothing, acceleration), but should avoid side effects on the input devices.
/// </remarks>
public interface IAxisBinding
{
    /// <summary>
    ///     Computes the raw contribution of this binding for the current frame.
    /// </summary>
    /// <param name="kb">Keyboard input accessor for level/edge queries.</param>
    /// <param name="mouse">Mouse input accessor for deltas and buttons.</param>
    /// <param name="pad">
    ///     Optional gamepad accessor. May be <c>null</c> when no gamepad is available or when this binding
    ///     does not require a gamepad.
    /// </param>
    /// <param name="dt">Delta time in seconds since the last input update.</param>
    /// <returns>
    ///     A raw, unclamped contribution ideally in the range [-1, 1]. Consumers may clamp/compose this value.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>Implementations should be deterministic for the same input and internal state.</description></item>
    ///         <item><description>Prefer frame rate–independent behavior when time-based smoothing is applied (use <paramref name="dt" />).</description></item>
    ///         <item><description>Do not mutate device state (e.g., do not change cursor position or rumble) from within a binding.</description></item>
    ///     </list>
    /// </remarks>
    float Get(IKeyboard kb, IMouse mouse, IGamepad? pad, double dt);
}