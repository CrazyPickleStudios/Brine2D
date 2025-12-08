namespace Brine2D.Input;

/// <summary>
///     Represents the axes available on a gamepad device.
///     Values typically range from -1.0 to 1.0 for sticks, and 0.0 to 1.0 for triggers.
/// </summary>
public enum GamepadAxis
{
    /// <summary>
    ///     Horizontal axis of the left analog stick.
    ///     -1.0 is full left, 1.0 is full right.
    /// </summary>
    LeftX,

    /// <summary>
    ///     Vertical axis of the left analog stick.
    ///     -1.0 is up, 1.0 is down (may vary by platform).
    /// </summary>
    LeftY,

    /// <summary>
    ///     Horizontal axis of the right analog stick.
    ///     -1.0 is full left, 1.0 is full right.
    /// </summary>
    RightX,

    /// <summary>
    ///     Vertical axis of the right analog stick.
    ///     -1.0 is up, 1.0 is down (may vary by platform).
    /// </summary>
    RightY,

    /// <summary>
    ///     Left trigger axis.
    ///     0.0 is released, 1.0 is fully pressed.
    /// </summary>
    LeftTrigger,

    /// <summary>
    ///     Right trigger axis.
    ///     0.0 is released, 1.0 is fully pressed.
    /// </summary>
    RightTrigger
}