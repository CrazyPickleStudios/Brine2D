namespace Brine2D.Input;

/// <summary>
///     Represents the primary input facade for the engine, providing access to all
///     supported input devices and input modalities (gamepads, keyboard, mouse, text input, and touch).
/// </summary>
public interface IInput
{
    /// <summary>
    ///     Gets the gamepad input manager providing access to connected gamepads and their state.
    /// </summary>
    IGamepads Gamepads { get; }

    /// <summary>
    ///     Gets the keyboard input manager for key state and events.
    /// </summary>
    IKeyboard Keyboard { get; }

    /// <summary>
    ///     Gets the mouse input manager for cursor position, button state, and wheel input.
    /// </summary>
    IMouse Mouse { get; }

    /// <summary>
    ///     Gets the text input handler for high-level text composition and character input.
    /// </summary>
    ITextInput TextInput { get; }

    /// <summary>
    ///     Gets the touch input manager for multi-touch gestures and touch points.
    /// </summary>
    ITouch Touch { get; }
}