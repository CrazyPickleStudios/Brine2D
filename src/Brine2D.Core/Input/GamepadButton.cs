namespace Brine2D.Core.Input;

/// <summary>
///     Represents logical gamepad buttons across common controllers.
/// </summary>
/// <remarks>
///     Names follow Xbox labeling for familiarity. Mappings to PlayStation and Nintendo are provided
///     to clarify intent when adapting to different physical layouts.
/// </remarks>
public enum GamepadButton
{
    /// <summary>
    ///     Bottom face button.
    ///     Xbox: A, PlayStation: Cross (X), Nintendo: B.
    /// </summary>
    A,

    /// <summary>
    ///     Right face button.
    ///     Xbox: B, PlayStation: Circle (O), Nintendo: A.
    /// </summary>
    B,

    /// <summary>
    ///     Left face button.
    ///     Xbox: X, PlayStation: Square (□), Nintendo: Y.
    /// </summary>
    X,

    /// <summary>
    ///     Top face button.
    ///     Xbox: Y, PlayStation: Triangle (△), Nintendo: X.
    /// </summary>
    Y,

    /// <summary>
    ///     View/Back/Select.
    ///     Xbox: View (Back), PlayStation: Share, Nintendo: Minus (-).
    /// </summary>
    Back, // View/Back/Select

    /// <summary>
    ///     Menu/Start/Options.
    ///     Xbox: Menu (Start), PlayStation: Options, Nintendo: Plus (+).
    /// </summary>
    Start, // Menu/Start/Options

    /// <summary>
    ///     Platform guide button.
    ///     Xbox: Xbox button, PlayStation: PS button, General: Home.
    /// </summary>
    Guide, // Xbox/Home/PS

    /// <summary>
    ///     Left shoulder button.
    ///     Xbox: LB, PlayStation: L1.
    /// </summary>
    LeftShoulder,

    /// <summary>
    ///     Right shoulder button.
    ///     Xbox: RB, PlayStation: R1.
    /// </summary>
    RightShoulder,

    /// <summary>
    ///     Left stick press (L3).
    /// </summary>
    LeftStick, // L3

    /// <summary>
    ///     Right stick press (R3).
    /// </summary>
    RightStick, // R3

    /// <summary>
    ///     Directional pad up.
    /// </summary>
    DPadUp,

    /// <summary>
    ///     Directional pad down.
    /// </summary>
    DPadDown,

    /// <summary>
    ///     Directional pad left.
    /// </summary>
    DPadLeft,

    /// <summary>
    ///     Directional pad right.
    /// </summary>
    DPadRight
}