namespace Brine2D.Input;

/// <summary>
/// Defines keyboard key codes for input handling.
/// </summary>
/// <remarks>
/// This enumeration provides a comprehensive set of keyboard keys including:
/// <list type="bullet">
/// <item><description>Alphabetic keys (A-Z)</description></item>
/// <item><description>Numeric keys (0-9) on the top row</description></item>
/// <item><description>Function keys (F1-F24)</description></item>
/// <item><description>Arrow keys for directional navigation</description></item>
/// <item><description>Modifier keys (Shift, Control, Alt, Super/Windows/Command)</description></item>
/// <item><description>Special editing keys (Insert, Home, End, PageUp, PageDown)</description></item>
/// <item><description>Punctuation and symbol keys</description></item>
/// <item><description>Numpad keys including operators</description></item>
/// <item><description>System keys (PrintScreen, Pause, Menu)</description></item>
/// <item><description>Media control keys (Play, Stop, Volume)</description></item>
/// <item><description>Browser navigation keys</description></item>
/// <item><description>International and language-specific keys</description></item>
/// </list>
/// </remarks>
public enum Keys
{
    /// <summary>
    /// Represents an unknown or unmapped key.
    /// </summary>
    Unknown = 0,

    /// <summary>Letter A key.</summary>
    A,
    /// <summary>Letter B key.</summary>
    B,
    /// <summary>Letter C key.</summary>
    C,
    /// <summary>Letter D key.</summary>
    D,
    /// <summary>Letter E key.</summary>
    E,
    /// <summary>Letter F key.</summary>
    F,
    /// <summary>Letter G key.</summary>
    G,
    /// <summary>Letter H key.</summary>
    H,
    /// <summary>Letter I key.</summary>
    I,
    /// <summary>Letter J key.</summary>
    J,
    /// <summary>Letter K key.</summary>
    K,
    /// <summary>Letter L key.</summary>
    L,
    /// <summary>Letter M key.</summary>
    M,
    /// <summary>Letter N key.</summary>
    N,
    /// <summary>Letter O key.</summary>
    O,
    /// <summary>Letter P key.</summary>
    P,
    /// <summary>Letter Q key.</summary>
    Q,
    /// <summary>Letter R key.</summary>
    R,
    /// <summary>Letter S key.</summary>
    S,
    /// <summary>Letter T key.</summary>
    T,
    /// <summary>Letter U key.</summary>
    U,
    /// <summary>Letter V key.</summary>
    V,
    /// <summary>Letter W key.</summary>
    W,
    /// <summary>Letter X key.</summary>
    X,
    /// <summary>Letter Y key.</summary>
    Y,
    /// <summary>Letter Z key.</summary>
    Z,

    /// <summary>Number 0 key on the top row.</summary>
    D0,
    /// <summary>Number 1 key on the top row.</summary>
    D1,
    /// <summary>Number 2 key on the top row.</summary>
    D2,
    /// <summary>Number 3 key on the top row.</summary>
    D3,
    /// <summary>Number 4 key on the top row.</summary>
    D4,
    /// <summary>Number 5 key on the top row.</summary>
    D5,
    /// <summary>Number 6 key on the top row.</summary>
    D6,
    /// <summary>Number 7 key on the top row.</summary>
    D7,
    /// <summary>Number 8 key on the top row.</summary>
    D8,
    /// <summary>Number 9 key on the top row.</summary>
    D9,

    /// <summary>Function key F1.</summary>
    F1,
    /// <summary>Function key F2.</summary>
    F2,
    /// <summary>Function key F3.</summary>
    F3,
    /// <summary>Function key F4.</summary>
    F4,
    /// <summary>Function key F5.</summary>
    F5,
    /// <summary>Function key F6.</summary>
    F6,
    /// <summary>Function key F7.</summary>
    F7,
    /// <summary>Function key F8.</summary>
    F8,
    /// <summary>Function key F9.</summary>
    F9,
    /// <summary>Function key F10.</summary>
    F10,
    /// <summary>Function key F11.</summary>
    F11,
    /// <summary>Function key F12.</summary>
    F12,
    /// <summary>Function key F13.</summary>
    F13,
    /// <summary>Function key F14.</summary>
    F14,
    /// <summary>Function key F15.</summary>
    F15,
    /// <summary>Function key F16.</summary>
    F16,
    /// <summary>Function key F17.</summary>
    F17,
    /// <summary>Function key F18.</summary>
    F18,
    /// <summary>Function key F19.</summary>
    F19,
    /// <summary>Function key F20.</summary>
    F20,
    /// <summary>Function key F21.</summary>
    F21,
    /// <summary>Function key F22.</summary>
    F22,
    /// <summary>Function key F23.</summary>
    F23,
    /// <summary>Function key F24.</summary>
    F24,

    /// <summary>Left arrow key.</summary>
    Left,
    /// <summary>Right arrow key.</summary>
    Right,
    /// <summary>Up arrow key.</summary>
    Up,
    /// <summary>Down arrow key.</summary>
    Down,

    /// <summary>Space bar key.</summary>
    Space,
    /// <summary>Enter (Return) key.</summary>
    Enter,
    /// <summary>Escape key.</summary>
    Escape,
    /// <summary>Tab key.</summary>
    Tab,
    /// <summary>Backspace key.</summary>
    Backspace,
    /// <summary>Delete key.</summary>
    Delete,
    /// <summary>Left Shift modifier key.</summary>
    LeftShift,
    /// <summary>Right Shift modifier key.</summary>
    RightShift,
    /// <summary>Left Control modifier key.</summary>
    LeftControl,
    /// <summary>Right Control modifier key.</summary>
    RightControl,
    /// <summary>Left Alt modifier key.</summary>
    LeftAlt,
    /// <summary>Right Alt modifier key.</summary>
    RightAlt,
    /// <summary>Left Super key (Windows key on Windows, Command key on macOS).</summary>
    LeftSuper,
    /// <summary>Right Super key (Windows key on Windows, Command key on macOS).</summary>
    RightSuper,
    /// <summary>Caps Lock toggle key.</summary>
    CapsLock,
    /// <summary>Num Lock toggle key.</summary>
    NumLock,
    /// <summary>Scroll Lock toggle key.</summary>
    ScrollLock,

    /// <summary>Insert key.</summary>
    Insert,
    /// <summary>Home key.</summary>
    Home,
    /// <summary>End key.</summary>
    End,
    /// <summary>Page Up key.</summary>
    PageUp,
    /// <summary>Page Down key.</summary>
    PageDown,

    /// <summary>Minus/Hyphen key.</summary>
    Minus,
    /// <summary>Equals key.</summary>
    Equals,
    /// <summary>Left bracket key ([).</summary>
    LeftBracket,
    /// <summary>Right bracket key (]).</summary>
    RightBracket,
    /// <summary>Backslash key (\).</summary>
    Backslash,
    /// <summary>Semicolon key (;).</summary>
    Semicolon,
    /// <summary>Apostrophe/Quote key (').</summary>
    Apostrophe,
    /// <summary>Grave/Backtick key (`).</summary>
    Grave,
    /// <summary>Comma key (,).</summary>
    Comma,
    /// <summary>Period key (.).</summary>
    Period,
    /// <summary>Slash key (/).</summary>
    Slash,

    /// <summary>Numpad 0 key.</summary>
    Numpad0,
    /// <summary>Numpad 1 key.</summary>
    Numpad1,
    /// <summary>Numpad 2 key.</summary>
    Numpad2,
    /// <summary>Numpad 3 key.</summary>
    Numpad3,
    /// <summary>Numpad 4 key.</summary>
    Numpad4,
    /// <summary>Numpad 5 key.</summary>
    Numpad5,
    /// <summary>Numpad 6 key.</summary>
    Numpad6,
    /// <summary>Numpad 7 key.</summary>
    Numpad7,
    /// <summary>Numpad 8 key.</summary>
    Numpad8,
    /// <summary>Numpad 9 key.</summary>
    Numpad9,
    /// <summary>Numpad Enter key.</summary>
    NumpadEnter,
    /// <summary>Numpad Plus key (+).</summary>
    NumpadPlus,
    /// <summary>Numpad Minus key (-).</summary>
    NumpadMinus,
    /// <summary>Numpad Multiply key (*).</summary>
    NumpadMultiply,
    /// <summary>Numpad Divide key (/).</summary>
    NumpadDivide,
    /// <summary>Numpad Period/Decimal key (.).</summary>
    NumpadPeriod,
    /// <summary>Numpad Equals key (=).</summary>
    NumpadEquals,
    /// <summary>Numpad Comma key (,).</summary>
    NumpadComma,

    /// <summary>Print Screen key.</summary>
    PrintScreen,
    /// <summary>Pause/Break key.</summary>
    Pause,
    /// <summary>Menu key.</summary>
    Menu,
    /// <summary>Application/Context menu key.</summary>
    Application,

    /// <summary>Volume Up key.</summary>
    VolumeUp,
    /// <summary>Volume Down key.</summary>
    VolumeDown,
    /// <summary>Mute/Unmute key.</summary>
    Mute,
    /// <summary>Media Play/Pause key.</summary>
    MediaPlay,
    /// <summary>Media Stop key.</summary>
    MediaStop,
    /// <summary>Media Previous Track key.</summary>
    MediaPrevious,
    /// <summary>Media Next Track key.</summary>
    MediaNext,

    /// <summary>Browser Back navigation key.</summary>
    BrowserBack,
    /// <summary>Browser Forward navigation key.</summary>
    BrowserForward,
    /// <summary>Browser Refresh key.</summary>
    BrowserRefresh,
    /// <summary>Browser Stop key.</summary>
    BrowserStop,
    /// <summary>Browser Search key.</summary>
    BrowserSearch,
    /// <summary>Browser Favorites key.</summary>
    BrowserFavorites,
    /// <summary>Browser Home key.</summary>
    BrowserHome,

    /// <summary>Clear key.</summary>
    Clear,
    /// <summary>Help key.</summary>
    Help,
    /// <summary>Select key.</summary>
    Select,
    /// <summary>Execute key.</summary>
    Execute,
    /// <summary>Undo key.</summary>
    Undo,
    /// <summary>Redo key.</summary>
    Redo,
    /// <summary>Find key.</summary>
    Find,
    /// <summary>Cut key.</summary>
    Cut,
    /// <summary>Copy key.</summary>
    Copy,
    /// <summary>Paste key.</summary>
    Paste,

    /// <summary>International key 1.</summary>
    International1,
    /// <summary>International key 2.</summary>
    International2,
    /// <summary>International key 3.</summary>
    International3,
    /// <summary>International key 4.</summary>
    International4,
    /// <summary>International key 5.</summary>
    International5,
    /// <summary>International key 6.</summary>
    International6,
    /// <summary>International key 7.</summary>
    International7,
    /// <summary>International key 8.</summary>
    International8,
    /// <summary>International key 9.</summary>
    International9,

    /// <summary>Language key 1.</summary>
    Lang1,
    /// <summary>Language key 2.</summary>
    Lang2,
    /// <summary>Language key 3.</summary>
    Lang3,
    /// <summary>Language key 4.</summary>
    Lang4,
    /// <summary>Language key 5.</summary>
    Lang5
}