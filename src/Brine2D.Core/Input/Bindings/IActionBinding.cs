namespace Brine2D.Core.Input.Bindings;

/// <summary>
///     Represents a logical input binding that maps one or more low-level inputs
///     (keyboard, mouse, gamepad) into a higher-level action.
/// </summary>
/// <remarks>
///     Implementations decide how to evaluate the binding each frame (e.g., key held, mouse button pressed,
///     gamepad button, trigger threshold, stick direction with deadzone, or combinations).
///     Use <see cref="IsDown(IKeyboard, KeyboardModifiers, IMouse, IGamepad?)" /> for level (held) queries.
/// </remarks>
/// <example>
///     // Typical usage inside a frame loop:
///     // if (jumpBinding.IsDown(kb, mods, mouse, pad)) Jump();
/// </example>
public interface IActionBinding
{
    /// <summary>
    ///     Returns true while the binding evaluates to an "active/held" state during this frame.
    /// </summary>
    /// <param name="kb">Keyboard input source for this frame.</param>
    /// <param name="mods">Snapshot of currently active keyboard modifiers.</param>
    /// <param name="mouse">Mouse input source for this frame.</param>
    /// <param name="pad">Optional gamepad input source for this frame; <c>null</c> if not available.</param>
    /// <returns>
    ///     <c>true</c> while the binding is logically down (active) for this frame; otherwise, <c>false</c>.
    /// </returns>
    /// <remarks>
    ///     This is a level query. Implementations that need edge behavior should use the input source's
    ///     edge helpers (e.g., <see cref="IKeyboard.WasKeyPressed(Key)" />,
    ///     <see cref="IGamepad.WasButtonPressed(GamepadButton)" />)
    ///     internally and expose an appropriate level result for the action.
    /// </remarks>
    bool IsDown(IKeyboard kb, KeyboardModifiers mods, IMouse mouse, IGamepad? pad);
}