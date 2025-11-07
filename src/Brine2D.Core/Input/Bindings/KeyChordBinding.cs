namespace Brine2D.Core.Input.Bindings;

/// <summary>
///     Maps an action to a specific <see cref="KeyChord" />.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description>Keyboard-only; mouse and gamepad parameters are ignored.</description></item>
///         <item><description>Delegates to <see cref="KeyChord.IsDown(IKeyboard, KeyboardModifiers)" /> for level (held) queries.</description></item>
///         <item><description>For edge semantics, use <see cref="KeyChord.WasPressed(IKeyboard, KeyboardModifiers)" /> or <see cref="KeyChord.WasReleased(IKeyboard, KeyboardModifiers)" />.</description></item>
///     </list>
/// </remarks>
public sealed class KeyChordBinding : IActionBinding
{
    /// <summary>
    ///     Creates a new binding with a default-initialized <see cref="Chord" />.
    /// </summary>
    /// <remarks>Useful for serializers or DI containers that set properties after construction.</remarks>
    public KeyChordBinding()
    {
    }

    /// <summary>
    ///     Creates a new binding for the specified <paramref name="chord" />.
    /// </summary>
    /// <param name="chord">The key and modifiers that represent the binding.</param>
    public KeyChordBinding(KeyChord chord)
    {
        Chord = chord;
    }

    /// <summary>
    ///     The key chord this binding responds to.
    /// </summary>
    /// <value>Defaults to <c>default</c> (no modifiers and the default key).</value>
    public KeyChord Chord { get; set; }

    /// <summary>
    ///     Returns true while the underlying <see cref="Chord" /> is considered down for the given keyboard and modifiers.
    /// </summary>
    /// <param name="kb">Keyboard input source.</param>
    /// <param name="mods">The current keyboard modifiers state.</param>
    /// <param name="mouse">Ignored for this binding.</param>
    /// <param name="pad">Ignored for this binding.</param>
    /// <returns>True when the chord is down; otherwise, false.</returns>
    /// <remarks>This is a level (held) query.</remarks>
    public bool IsDown(IKeyboard kb, KeyboardModifiers mods, IMouse mouse, IGamepad? pad)
    {
        return Chord.IsDown(kb, mods);
    }
}