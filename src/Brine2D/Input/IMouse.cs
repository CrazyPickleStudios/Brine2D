namespace Brine2D.Input;

/// <summary>
///     Defines a mouse input source, providing wheel movement, cursor position,
///     and button state queries for the current frame.
/// </summary>
public interface IMouse
{
    /// <summary>
    ///     Gets the horizontal wheel delta since the last frame.
    ///     Positive values indicate scrolling to the right.
    /// </summary>
    float WheelX { get; }

    /// <summary>
    ///     Gets the vertical wheel delta since the last frame.
    ///     Positive values indicate scrolling upward.
    /// </summary>
    float WheelY { get; }

    /// <summary>
    ///     Gets the current mouse cursor X position in pixels.
    /// </summary>
    float X { get; }

    /// <summary>
    ///     Gets the current mouse cursor Y position in pixels.
    /// </summary>
    float Y { get; }

    /// <summary>
    ///     Returns whether the specified mouse button is currently held down.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button is down; otherwise, false.</returns>
    bool IsDown(MouseButton button);

    /// <summary>
    ///     Returns whether the specified mouse button transitioned to pressed
    ///     during the current frame.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button was pressed this frame; otherwise, false.</returns>
    bool WasPressed(MouseButton button);

    /// <summary>
    ///     Returns whether the specified mouse button transitioned to released
    ///     during the current frame.
    /// </summary>
    /// <param name="button">The mouse button to check.</param>
    /// <returns>True if the button was released this frame; otherwise, false.</returns>
    bool WasReleased(MouseButton button);
}