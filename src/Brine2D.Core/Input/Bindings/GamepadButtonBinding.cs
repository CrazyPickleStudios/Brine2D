namespace Brine2D.Core.Input.Bindings;

/// <summary>
///     Binds an action to a specific gamepad button, optionally requiring additional buttons to be held (a combo).
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description>The binding evaluates against a single logical <see cref="IGamepad" /> instance provided to <see cref="IsDown" />.</description></item>
///         <item><description>If <see cref="With" /> contains buttons, all must be down together with <see cref="Button" /> (duplicates have no extra effect).</description></item>
///         <item><description><see cref="PadIndex" /> is metadata for higher-level systems to select a device; <see cref="IsDown" /> uses the <see cref="IGamepad" /> supplied by the caller.</description></item>
///     </list>
/// </remarks>
/// <example>
///     <code>
///     // Simple binding:
///     var jump = new GamepadButtonBinding(GamepadButton.A);
///
///     // Combo binding (LB + Y):
///     var ult = new GamepadButtonBinding(GamepadButton.Y, with: new[] { GamepadButton.LeftShoulder });
///     </code>
/// </example>
public sealed class GamepadButtonBinding : IActionBinding
{
    /// <summary>
    ///     Creates an empty binding. Set <see cref="Button" /> before use.
    /// </summary>
    public GamepadButtonBinding()
    {
    }

    /// <summary>
    ///     Creates a binding to a gamepad button with optional fixed pad index and combo buttons.
    /// </summary>
    /// <param name="button">The primary button required to consider the binding down.</param>
    /// <param name="padIndex">
    ///     Optional fixed gamepad index in the input system. When <c>null</c>, a higher-level resolver
    ///     is expected to provide the appropriate <see cref="IGamepad" /> to <see cref="IsDown" />.
    /// </param>
    /// <param name="with">
    ///     Optional additional buttons that must also be held (e.g., LB + Y).
    ///     Duplicates are allowed but have no additional effect.
    /// </param>
    public GamepadButtonBinding(GamepadButton button, int? padIndex = null, IEnumerable<GamepadButton>? with = null)
    {
        Button = button;
        PadIndex = padIndex;
        if (with is not null)
        {
            With.AddRange(with);
        }
    }

    /// <summary>
    ///     The primary gamepad button required for this binding.
    /// </summary>
    public GamepadButton Button { get; set; }

    /// <summary>
    ///     Optional fixed gamepad index. When <c>null</c>, input systems typically resolve to
    ///     the active/primary player and pass that <see cref="IGamepad" /> to <see cref="IsDown" />.
    /// </summary>
    /// <value>Defaults to <c>null</c> (use caller-resolved pad).</value>
    public int? PadIndex { get; set; }

    /// <summary>
    ///     Additional buttons that must be held together with <see cref="Button" /> (combo).
    ///     For example: require LeftShoulder + Y.
    /// </summary>
    /// <value>Defaults to an empty list.</value>
    public List<GamepadButton> With { get; set; } = new();

    /// <summary>
    ///     Returns true while the underlying <paramref name="pad" /> is connected and all required buttons
    ///     (the primary <see cref="Button" /> and any in <see cref="With" />) are currently held down.
    /// </summary>
    /// <param name="kb">Ignored for this binding.</param>
    /// <param name="mods">Ignored for this binding.</param>
    /// <param name="mouse">Ignored for this binding.</param>
    /// <param name="pad">
    ///     The gamepad instance to evaluate. If <c>null</c> or not connected, the binding is not down.
    /// </param>
    /// <returns>
    ///     True if the pad is connected, <see cref="Button" /> is down, and all <see cref="With" /> buttons are down;
    ///     otherwise, false.
    /// </returns>
    /// <remarks>This is a level (held) query, not an edge event.</remarks>
    public bool IsDown(IKeyboard kb, KeyboardModifiers mods, IMouse mouse, IGamepad? pad)
    {
        if (pad is null || !pad.IsConnected)
        {
            return false;
        }

        if (!pad.IsButtonDown(Button))
        {
            return false;
        }

        for (var i = 0; i < With.Count; i++)
        {
            if (!pad.IsButtonDown(With[i]))
            {
                return false;
            }
        }

        return true;
    }
}