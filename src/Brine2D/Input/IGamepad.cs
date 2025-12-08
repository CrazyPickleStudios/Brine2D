namespace Brine2D.Input;

/// <summary>
///     Represents a gamepad device abstraction providing access to axes and button states.
/// </summary>
public interface IGamepad
{
    /// <summary>
    ///     Gets the zero-based index of the gamepad, typically matching the system or input manager slot.
    /// </summary>
    int Index { get; }

    /// <summary>
    ///     Gets the current value of the specified gamepad axis.
    /// </summary>
    /// <param name="axis">The axis to query.</param>
    /// <returns>
    ///     A float representing the axis value. Typical range is [-1, 1], but may vary by implementation.
    /// </returns>
    float Axis(GamepadAxis axis);

    /// <summary>
    ///     Determines whether the specified button is currently held down.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns><c>true</c> if the button is down; otherwise, <c>false</c>.</returns>
    bool IsDown(GamepadButton button);

    /// <summary>
    ///     Determines whether the specified button transitioned to pressed state during the current frame/tick.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns><c>true</c> if the button was pressed this frame; otherwise, <c>false</c>.</returns>
    bool WasPressed(GamepadButton button);

    /// <summary>
    ///     Determines whether the specified button transitioned to released state during the current frame/tick.
    /// </summary>
    /// <param name="button">The button to check.</param>
    /// <returns><c>true</c> if the button was released this frame; otherwise, <c>false</c>.</returns>
    bool WasReleased(GamepadButton button);
}