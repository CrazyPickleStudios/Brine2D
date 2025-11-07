namespace Brine2D.Core.Input.Bindings;

/// <summary>
///     Simple action binding that treats a single <see cref="MouseButton" /> as the action source.
/// </summary>
/// <remarks>
///     This binding performs a level query against the current mouse state. It is "down" while the
///     configured button is physically held. For edge-triggered behavior, prefer using
///     <see cref="IMouse.WasButtonPressed(MouseButton)" /> or <see cref="IMouse.WasButtonReleased(MouseButton)" />
///     in a specialized binding.
/// </remarks>
public sealed class MouseButtonBinding : IActionBinding
{
    /// <summary>
    ///     Initializes a new instance of <see cref="MouseButtonBinding" /> with the default button value.
    /// </summary>
    /// <remarks>
    ///     After constructing with this overload, set <see cref="Button" /> before use.
    /// </remarks>
    public MouseButtonBinding()
    {
    }

    /// <summary>
    ///     Initializes a new instance of <see cref="MouseButtonBinding" /> with the specified mouse button.
    /// </summary>
    /// <param name="button">The mouse button to monitor for the action.</param>
    public MouseButtonBinding(MouseButton button)
    {
        Button = button;
    }

    /// <summary>
    ///     Gets or sets the mouse button that activates this binding.
    /// </summary>
    public MouseButton Button { get; set; }

    /// <summary>
    ///     Returns true while the configured mouse <see cref="Button" /> is currently held down.
    /// </summary>
    /// <param name="kb">Keyboard input source for this frame (unused).</param>
    /// <param name="mods">Currently active keyboard modifiers (unused).</param>
    /// <param name="mouse">Mouse input source for this frame.</param>
    /// <param name="pad">Optional gamepad input source for this frame (unused).</param>
    /// <returns>
    ///     <c>true</c> while the configured mouse button is down; otherwise, <c>false</c>.
    /// </returns>
    public bool IsDown(IKeyboard kb, KeyboardModifiers mods, IMouse mouse, IGamepad? pad)
    {
        return mouse.IsButtonDown(Button);
    }
}