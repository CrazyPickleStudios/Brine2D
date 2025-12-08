namespace Brine2D.Input;

/// <summary>
///     Represents the set of gamepad buttons supported by the input system.
/// </summary>
public enum GamepadButton
{
    /// <summary>
    ///     The primary face button (commonly labeled "A").
    /// </summary>
    A,

    /// <summary>
    ///     The secondary face button (commonly labeled "B").
    /// </summary>
    B,

    /// <summary>
    ///     The tertiary face button (commonly labeled "X").
    /// </summary>
    X,

    /// <summary>
    ///     The quaternary face button (commonly labeled "Y").
    /// </summary>
    Y,

    /// <summary>
    ///     The back/select/system secondary button.
    /// </summary>
    Back,

    /// <summary>
    ///     The start/options/system primary button.
    /// </summary>
    Start,

    /// <summary>
    ///     The guide/system home button.
    /// </summary>
    Guide,

    /// <summary>
    ///     The left shoulder bumper button.
    /// </summary>
    LeftShoulder,

    /// <summary>
    ///     The right shoulder bumper button.
    /// </summary>
    RightShoulder,

    /// <summary>
    ///     The left stick press (L3).
    /// </summary>
    LeftStick,

    /// <summary>
    ///     The right stick press (R3).
    /// </summary>
    RightStick,

    /// <summary>
    ///     The D-pad up button.
    /// </summary>
    DpadUp,

    /// <summary>
    ///     The D-pad down button.
    /// </summary>
    DpadDown,

    /// <summary>
    ///     The D-pad left button.
    /// </summary>
    DpadLeft,

    /// <summary>
    ///     The D-pad right button.
    /// </summary>
    DpadRight
}