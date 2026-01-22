namespace Brine2D.Input;

/// <summary>
///     Defines the standard gamepad button codes using Xbox-style layout conventions.
///     These mappings are cross-platform compatible with PlayStation and other controller types.
/// </summary>
public enum GamepadButton
{
    /// <summary>
    ///     The bottom face button (Xbox A, PlayStation Cross).
    /// </summary>
    A,

    /// <summary>
    ///     The right face button (Xbox B, PlayStation Circle).
    /// </summary>
    B,

    /// <summary>
    ///     The left face button (Xbox X, PlayStation Square).
    /// </summary>
    X,

    /// <summary>
    ///     The top face button (Xbox Y, PlayStation Triangle).
    /// </summary>
    Y,

    /// <summary>
    ///     The back/select button (Xbox Back, PlayStation Select).
    /// </summary>
    Back,

    /// <summary>
    ///     The guide/home button (Xbox Guide, PlayStation PS button).
    /// </summary>
    Guide,

    /// <summary>
    ///     The start/options button (Xbox Start, PlayStation Start).
    /// </summary>
    Start,

    /// <summary>
    ///     The left analog stick button (L3).
    /// </summary>
    LeftStick,

    /// <summary>
    ///     The right analog stick button (R3).
    /// </summary>
    RightStick,

    /// <summary>
    ///     The left shoulder button (LB, L1).
    /// </summary>
    LeftShoulder,

    /// <summary>
    ///     The right shoulder button (RB, R1).
    /// </summary>
    RightShoulder,

    /// <summary>
    ///     The directional pad up button.
    /// </summary>
    DPadUp,

    /// <summary>
    ///     The directional pad down button.
    /// </summary>
    DPadDown,

    /// <summary>
    ///     The directional pad left button.
    /// </summary>
    DPadLeft,

    /// <summary>
    ///     The directional pad right button.
    /// </summary>
    DPadRight
}