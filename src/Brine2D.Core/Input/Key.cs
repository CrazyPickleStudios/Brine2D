namespace Brine2D.Core.Input;

/// <summary>
///     Represents a physical keyboard key identifier used by the input system.
/// </summary>
public enum Key
{
    /// <summary>
    ///     An unknown or unmapped key.
    /// </summary>
    Unknown = 0,

    // Navigation

    /// <summary>
    ///     Escape (Esc) key.
    /// </summary>
    Escape,

    /// <summary>
    ///     Tab key.
    /// </summary>
    Tab,

    /// <summary>
    ///     Enter/Return key.
    /// </summary>
    Enter,

    /// <summary>
    ///     Backspace key.
    /// </summary>
    Backspace,

    /// <summary>
    ///     Insert (Ins) key.
    /// </summary>
    Insert,

    /// <summary>
    ///     Delete (Del) key.
    /// </summary>
    Delete,

    /// <summary>
    ///     Home key.
    /// </summary>
    Home,

    /// <summary>
    ///     End key.
    /// </summary>
    End,

    /// <summary>
    ///     Page Up (PgUp) key.
    /// </summary>
    PageUp,

    /// <summary>
    ///     Page Down (PgDn) key.
    /// </summary>
    PageDown,

    // Arrows

    /// <summary>
    ///     Left Arrow key.
    /// </summary>
    Left,

    /// <summary>
    ///     Right Arrow key.
    /// </summary>
    Right,

    /// <summary>
    ///     Up Arrow key.
    /// </summary>
    Up,

    /// <summary>
    ///     Down Arrow key.
    /// </summary>
    Down,

    // Whitespace

    /// <summary>
    ///     Spacebar key.
    /// </summary>
    Space,

    // Digits (top row)

    /// <summary>
    ///     Digit 0 key (top row, not numpad).
    /// </summary>
    D0,

    /// <summary>
    ///     Digit 1 key (top row, not numpad).
    /// </summary>
    D1,

    /// <summary>
    ///     Digit 2 key (top row, not numpad).
    /// </summary>
    D2,

    /// <summary>
    ///     Digit 3 key (top row, not numpad).
    /// </summary>
    D3,

    /// <summary>
    ///     Digit 4 key (top row, not numpad).
    /// </summary>
    D4,

    /// <summary>
    ///     Digit 5 key (top row, not numpad).
    /// </summary>
    D5,

    /// <summary>
    ///     Digit 6 key (top row, not numpad).
    /// </summary>
    D6,

    /// <summary>
    ///     Digit 7 key (top row, not numpad).
    /// </summary>
    D7,

    /// <summary>
    ///     Digit 8 key (top row, not numpad).
    /// </summary>
    D8,

    /// <summary>
    ///     Digit 9 key (top row, not numpad).
    /// </summary>
    D9,

    // Letters

    /// <summary>
    ///     Letter A key.
    /// </summary>
    A,

    /// <summary>
    ///     Letter B key.
    /// </summary>
    B,

    /// <summary>
    ///     Letter C key.
    /// </summary>
    C,

    /// <summary>
    ///     Letter D key.
    /// </summary>
    D,

    /// <summary>
    ///     Letter E key.
    /// </summary>
    E,

    /// <summary>
    ///     Letter F key.
    /// </summary>
    F,

    /// <summary>
    ///     Letter G key.
    /// </summary>
    G,

    /// <summary>
    ///     Letter H key.
    /// </summary>
    H,

    /// <summary>
    ///     Letter I key.
    /// </summary>
    I,

    /// <summary>
    ///     Letter J key.
    /// </summary>
    J,

    /// <summary>
    ///     Letter K key.
    /// </summary>
    K,

    /// <summary>
    ///     Letter L key.
    /// </summary>
    L,

    /// <summary>
    ///     Letter M key.
    /// </summary>
    M,

    /// <summary>
    ///     Letter N key.
    /// </summary>
    N,

    /// <summary>
    ///     Letter O key.
    /// </summary>
    O,

    /// <summary>
    ///     Letter P key.
    /// </summary>
    P,

    /// <summary>
    ///     Letter Q key.
    /// </summary>
    Q,

    /// <summary>
    ///     Letter R key.
    /// </summary>
    R,

    /// <summary>
    ///     Letter S key.
    /// </summary>
    S,

    /// <summary>
    ///     Letter T key.
    /// </summary>
    T,

    /// <summary>
    ///     Letter U key.
    /// </summary>
    U,

    /// <summary>
    ///     Letter V key.
    /// </summary>
    V,

    /// <summary>
    ///     Letter W key.
    /// </summary>
    W,

    /// <summary>
    ///     Letter X key.
    /// </summary>
    X,

    /// <summary>
    ///     Letter Y key.
    /// </summary>
    Y,

    /// <summary>
    ///     Letter Z key.
    /// </summary>
    Z,

    // Function keys

    /// <summary>
    ///     Function key F1.
    /// </summary>
    F1,

    /// <summary>
    ///     Function key F2.
    /// </summary>
    F2,

    /// <summary>
    ///     Function key F3.
    /// </summary>
    F3,

    /// <summary>
    ///     Function key F4.
    /// </summary>
    F4,

    /// <summary>
    ///     Function key F5.
    /// </summary>
    F5,

    /// <summary>
    ///     Function key F6.
    /// </summary>
    F6,

    /// <summary>
    ///     Function key F7.
    /// </summary>
    F7,

    /// <summary>
    ///     Function key F8.
    /// </summary>
    F8,

    /// <summary>
    ///     Function key F9.
    /// </summary>
    F9,

    /// <summary>
    ///     Function key F10.
    /// </summary>
    F10,

    /// <summary>
    ///     Function key F11.
    /// </summary>
    F11,

    /// <summary>
    ///     Function key F12.
    /// </summary>
    F12,

    // Lock keys

    /// <summary>
    ///     Caps Lock key.
    /// </summary>
    CapsLock,

    /// <summary>
    ///     Num Lock key.
    /// </summary>
    NumLock,

    /// <summary>
    ///     Scroll Lock key.
    /// </summary>
    ScrollLock,

    // Print/Pause

    /// <summary>
    ///     Print Screen (PrtSc) key.
    /// </summary>
    PrintScreen,

    /// <summary>
    ///     Pause/Break key.
    /// </summary>
    Pause,

    // Editing symbols

    /// <summary>
    ///     Grave accent / backtick (`) key (often shares tilde ~).
    /// </summary>
    Grave, // `

    /// <summary>
    ///     Minus (-) / underscore (_) key.
    /// </summary>
    Minus, // -

    /// <summary>
    ///     Equals (=) / plus (+) key.
    /// </summary>
    Equals, // =

    /// <summary>
    ///     Left bracket ([) key.
    /// </summary>
    LeftBracket, // [

    /// <summary>
    ///     Right bracket (]) key.
    /// </summary>
    RightBracket, // ]

    /// <summary>
    ///     Backslash (\) / pipe (|) key.
    /// </summary>
    Backslash, // \

    /// <summary>
    ///     Semicolon (;) / colon (:) key.
    /// </summary>
    Semicolon, // ;

    /// <summary>
    ///     Apostrophe (') / quote (") key.
    /// </summary>
    Apostrophe, // '

    /// <summary>
    ///     Comma (,) / less-than (<) key.
    /// </summary>
    Comma, // ,

    /// <summary>
    ///     Period (.) / greater-than (>) key.
    /// </summary>
    Period, // .

    /// <summary>
    ///     Slash (/) / question mark (?) key.
    /// </summary>
    Slash, // /

    // Modifiers (left/right)

    /// <summary>
    ///     Left Shift modifier key.
    /// </summary>
    LeftShift,

    /// <summary>
    ///     Right Shift modifier key.
    /// </summary>
    RightShift,

    /// <summary>
    ///     Left Control (Ctrl) modifier key.
    /// </summary>
    LeftControl,

    /// <summary>
    ///     Right Control (Ctrl) modifier key.
    /// </summary>
    RightControl,

    /// <summary>
    ///     Left Alt (Option) modifier key.
    /// </summary>
    LeftAlt,

    /// <summary>
    ///     Right Alt (AltGr/Option) modifier key.
    /// </summary>
    RightAlt,

    /// <summary>
    ///     Left Super key (Windows/Command).
    /// </summary>
    LeftSuper, // Win/Command

    /// <summary>
    ///     Right Super key (Windows/Command).
    /// </summary>
    RightSuper, // Win/Command

    // Menu (Applications)

    /// <summary>
    ///     Application/Menu key.
    /// </summary>
    Menu,

    // Numpad cluster

    /// <summary>
    ///     Numeric keypad 0 key.
    /// </summary>
    Numpad0,

    /// <summary>
    ///     Numeric keypad 1 key.
    /// </summary>
    Numpad1,

    /// <summary>
    ///     Numeric keypad 2 key.
    /// </summary>
    Numpad2,

    /// <summary>
    ///     Numeric keypad 3 key.
    /// </summary>
    Numpad3,

    /// <summary>
    ///     Numeric keypad 4 key.
    /// </summary>
    Numpad4,

    /// <summary>
    ///     Numeric keypad 5 key.
    /// </summary>
    Numpad5,

    /// <summary>
    ///     Numeric keypad 6 key.
    /// </summary>
    Numpad6,

    /// <summary>
    ///     Numeric keypad 7 key.
    /// </summary>
    Numpad7,

    /// <summary>
    ///     Numeric keypad 8 key.
    /// </summary>
    Numpad8,

    /// <summary>
    ///     Numeric keypad 9 key.
    /// </summary>
    Numpad9,

    /// <summary>
    ///     Numeric keypad divide (/) key.
    /// </summary>
    NumpadDivide, // KP /

    /// <summary>
    ///     Numeric keypad multiply (*) key.
    /// </summary>
    NumpadMultiply, // KP *

    /// <summary>
    ///     Numeric keypad minus (-) key.
    /// </summary>
    NumpadMinus, // KP -

    /// <summary>
    ///     Numeric keypad plus (+) key.
    /// </summary>
    NumpadPlus, // KP +

    /// <summary>
    ///     Numeric keypad Enter key.
    /// </summary>
    NumpadEnter, // KP Enter

    /// <summary>
    ///     Numeric keypad period (.) / decimal separator key.
    /// </summary>
    NumpadPeriod, // KP .

    // Media keys (optional)

    /// <summary>
    ///     Volume Up media key.
    /// </summary>
    VolumeUp,

    /// <summary>
    ///     Volume Down media key.
    /// </summary>
    VolumeDown,

    /// <summary>
    ///     Volume Mute media key.
    /// </summary>
    VolumeMute,

    /// <summary>
    ///     Media Play/Pause key.
    /// </summary>
    MediaPlayPause,

    /// <summary>
    ///     Media Next Track key.
    /// </summary>
    MediaNextTrack,

    /// <summary>
    ///     Media Previous Track key.
    /// </summary>
    MediaPrevTrack,

    /// <summary>
    ///     Media Stop key.
    /// </summary>
    MediaStop
}