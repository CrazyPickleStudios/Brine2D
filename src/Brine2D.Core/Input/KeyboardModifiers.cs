namespace Brine2D.Core.Input;

/// <summary>
///     Keyboard modifier state as a bitmask. Use bitwise operations to test combinations.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>
///                 Combined modifiers (<see cref="Shift" />, <see cref="Control" />, <see cref="Alt" />,
///                 <see cref="Super" />) are conceptual bits. An input backend may set these and the corresponding
///                 side-specific bits together.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Side-specific modifiers (<see cref="LeftShift" />, <see cref="RightShift" />, etc.) enable
///                 fine-grained checks when the platform provides that detail.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Lock keys (<see cref="CapsLock" />, <see cref="NumLock" />) indicate toggle state at the time
///                 of the query/event.
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <example>
///     <code>
///     // "At least these modifiers" (e.g., require Shift + Control, allow others):
///     bool hasShiftCtrl = (mods & (KeyboardModifiers.Shift | KeyboardModifiers.Control))
///                     == (KeyboardModifiers.Shift | KeyboardModifiers.Control);
/// 
///     // Exact match (only Shift + Control, no others):
///     bool exactShiftCtrl = mods == (KeyboardModifiers.Shift | KeyboardModifiers.Control);
/// 
///     // Side-specific check:
///     bool isLeftShiftOnly = (mods & KeyboardModifiers.LeftShift) == KeyboardModifiers.LeftShift
///                        && (mods & (KeyboardModifiers.RightShift | KeyboardModifiers.Control | KeyboardModifiers.Alt | KeyboardModifiers.Super)) == 0;
///     </code>
/// </example>
[Flags]
public enum KeyboardModifiers
{
    /// <summary>No modifiers are active.</summary>
    None = 0,

    /// <summary>
    ///     A Shift modifier is active (left or right).
    /// </summary>
    Shift = 1 << 0,

    /// <summary>
    ///     A Control modifier is active (left or right).
    /// </summary>
    Control = 1 << 1,

    /// <summary>
    ///     An Alt/Option modifier is active (left or right).
    /// </summary>
    Alt = 1 << 2,

    /// <summary>
    ///     A Super/Meta/Windows/Command modifier is active (left or right).
    /// </summary>
    Super = 1 << 3,

    /// <summary>
    ///     Caps Lock is engaged.
    /// </summary>
    CapsLock = 1 << 4,

    /// <summary>
    ///     Num Lock is engaged.
    /// </summary>
    NumLock = 1 << 5,

    /// <summary>
    ///     Left Shift is active.
    /// </summary>
    LeftShift = 1 << 6,

    /// <summary>
    ///     Right Shift is active.
    /// </summary>
    RightShift = 1 << 7,

    /// <summary>
    ///     Left Control is active.
    /// </summary>
    LeftControl = 1 << 8,

    /// <summary>
    ///     Right Control is active.
    /// </summary>
    RightControl = 1 << 9,

    /// <summary>
    ///     Left Alt/Option is active.
    /// </summary>
    LeftAlt = 1 << 10,

    /// <summary>
    ///     Right Alt/Option (often AltGr) is active.
    /// </summary>
    RightAlt = 1 << 11,

    /// <summary>
    ///     Left Super/Meta/Windows/Command is active.
    /// </summary>
    LeftSuper = 1 << 12,

    /// <summary>
    ///     Right Super/Meta/Windows/Command is active.
    /// </summary>
    RightSuper = 1 << 13
}