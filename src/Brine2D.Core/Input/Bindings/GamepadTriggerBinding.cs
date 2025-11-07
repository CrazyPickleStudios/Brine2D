namespace Brine2D.Core.Input.Bindings;

/// <summary>
///     Action binding that treats a gamepad trigger as a digital action with hysteresis.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description>Uses a latch with press/release thresholds to avoid flicker near the threshold.</description></item>
///         <item><description>If the <see cref="IGamepad" /> implementation already applies trigger hysteresis, this acts as a lightweight fallback.</description></item>
///         <item><description>Ignores keyboard/mouse and evaluates only the provided <see cref="IGamepad" /> instance.</description></item>
///         <item><description><see cref="PadIndex" /> is metadata for higher-level routing and is not enforced here.</description></item>
///     </list>
/// </remarks>
public sealed class GamepadTriggerBinding : IActionBinding
{
    // Internal latched state to provide hysteresis at the binding level as fallback
    private bool _latched;

    /// <summary>
    ///     Initializes a new binding with default thresholds.
    /// </summary>
    /// <value><see cref="PressThreshold" /> = 0.5; <see cref="ReleaseThreshold" /> = 0.45.</value>
    public GamepadTriggerBinding()
    {
    }

    /// <summary>
    ///     Initializes a new binding for the specified trigger and thresholds.
    /// </summary>
    /// <param name="axis">
    ///     The trigger axis to monitor. Expected values are <see cref="GamepadAxis.LeftTrigger" /> or
    ///     <see cref="GamepadAxis.RightTrigger" />.
    /// </param>
    /// <param name="press">Press threshold in [0, 1]. When the value rises to or above this level, the binding latches on.</param>
    /// <param name="release">Release threshold in [0, 1]. When the value falls below this level, the binding unlatches.</param>
    /// <param name="padIndex">
    ///     Optional logical gamepad index for higher-level routing. Not used by this type directly; the caller
    ///     is expected to provide the correct <see cref="IGamepad" /> instance to <see cref="IsDown" />.
    /// </param>
    /// <remarks>
    ///     For stable hysteresis, <paramref name="release" /> should be less than <paramref name="press" />.
    /// </remarks>
    public GamepadTriggerBinding(GamepadAxis axis, float press = 0.5f, float release = 0.45f, int? padIndex = null)
    {
        Axis = axis;
        PressThreshold = press;
        ReleaseThreshold = release;
        PadIndex = padIndex;
    }

    /// <summary>
    ///     The trigger axis to monitor. Use <see cref="GamepadAxis.LeftTrigger" /> or <see cref="GamepadAxis.RightTrigger" />.
    /// </summary>
    public GamepadAxis Axis { get; set; } // LeftTrigger or RightTrigger

    /// <summary>
    ///     Optional logical gamepad index this binding conceptually targets.
    ///     Note: This type does not select gamepads; the caller supplies the <see cref="IGamepad" /> to query.
    /// </summary>
    /// <value>Defaults to <c>null</c> (use caller-resolved pad).</value>
    public int? PadIndex { get; set; }

    /// <summary>
    ///     Threshold in [0, 1] at or above which the binding becomes pressed (latched).
    /// </summary>
    /// <value>Defaults to 0.5.</value>
    public float PressThreshold { get; set; } = 0.5f;

    /// <summary>
    ///     Threshold in [0, 1] below which the binding becomes released (unlatched).
    ///     Should be less than <see cref="PressThreshold" /> to avoid flicker.
    /// </summary>
    /// <value>Defaults to 0.45.</value>
    public float ReleaseThreshold { get; set; } = 0.45f;

    /// <summary>
    ///     Returns whether the trigger is logically down this frame using hysteresis.
    /// </summary>
    /// <param name="kb">Ignored.</param>
    /// <param name="mods">Ignored.</param>
    /// <param name="mouse">Ignored.</param>
    /// <param name="pad">
    ///     The gamepad to sample. If <c>null</c> or not connected, this returns false.
    /// </param>
    /// <returns>
    ///     True if the trigger value has crossed and latched above <see cref="PressThreshold" /> and has not yet
    ///     fallen below <see cref="ReleaseThreshold" />; otherwise, false.
    /// </returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>Samples the raw axis via <see cref="IGamepad.GetAxis(GamepadAxis)" /> in [0, 1].</description></item>
    ///         <item><description>Level query with hysteresis (not an edge event).</description></item>
    ///     </list>
    /// </remarks>
    public bool IsDown(IKeyboard kb, KeyboardModifiers mods, IMouse mouse, IGamepad? pad)
    {
        if (pad is null || !pad.IsConnected)
        {
            return false;
        }

        var v = pad.GetAxis(Axis);

        // Rising edge: latch when value crosses or meets the press threshold.
        if (!_latched && v >= PressThreshold)
        {
            _latched = true;
        }
        // Falling edge: unlatch when value falls below the release threshold.
        else if (_latched && v < ReleaseThreshold)
        {
            _latched = false;
        }

        return _latched;
    }
}