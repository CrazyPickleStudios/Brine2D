namespace Brine2D.Input;

/// <summary>
///     Represents scan codes for keyboard and media keys.
///     Values map to physical keys regardless of keyboard layout.
/// </summary>
public enum ScanKey
{
    /// <summary>
    ///     Unrecognized or no key.
    /// </summary>
    Unknown,

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

    /// <summary>
    ///     Number row 1 key.
    /// </summary>
    Alpha1,

    /// <summary>
    ///     Number row 2 key.
    /// </summary>
    Alpha2,

    /// <summary>
    ///     Number row 3 key.
    /// </summary>
    Alpha3,

    /// <summary>
    ///     Number row 4 key.
    /// </summary>
    Alpha4,

    /// <summary>
    ///     Number row 5 key.
    /// </summary>
    Alpha5,

    /// <summary>
    ///     Number row 6 key.
    /// </summary>
    Alpha6,

    /// <summary>
    ///     Number row 7 key.
    /// </summary>
    Alpha7,

    /// <summary>
    ///     Number row 8 key.
    /// </summary>
    Alpha8,

    /// <summary>
    ///     Number row 9 key.
    /// </summary>
    Alpha9,

    /// <summary>
    ///     Number row 0 key.
    /// </summary>
    Alpha0,

    /// <summary>
    ///     Enter/Return key.
    /// </summary>
    Return,

    /// <summary>
    ///     Escape key.
    /// </summary>
    Escape,

    /// <summary>
    ///     Backspace key.
    /// </summary>
    Backspace,

    /// <summary>
    ///     Tab key.
    /// </summary>
    Tab,

    /// <summary>
    ///     Space bar.
    /// </summary>
    Space,

    /// <summary>
    ///     Minus/Hyphen key.
    /// </summary>
    Minus,

    /// <summary>
    ///     Equals key.
    /// </summary>
    Equals,

    /// <summary>
    ///     Left bracket '[' key.
    /// </summary>
    LeftBracket,

    /// <summary>
    ///     Right bracket ']' key.
    /// </summary>
    RightBracket,

    /// <summary>
    ///     Backslash '\' key.
    /// </summary>
    Backslash,

    /// <summary>
    ///     Non-US hash/tilde key.
    /// </summary>
    NonUsHash,

    /// <summary>
    ///     Semicolon ';' key.
    /// </summary>
    Semicolon,

    /// <summary>
    ///     Apostrophe '\'' key.
    /// </summary>
    Apostrophe,

    /// <summary>
    ///     Grave/Backtick '`' key.
    /// </summary>
    Grave,

    /// <summary>
    ///     Comma ',' key.
    /// </summary>
    Comma,

    /// <summary>
    ///     Period '.' key.
    /// </summary>
    Period,

    /// <summary>
    ///     Slash '/' key.
    /// </summary>
    Slash,

    /// <summary>
    ///     Caps Lock key.
    /// </summary>
    Capslock,

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

    /// <summary>
    ///     Print Screen key.
    /// </summary>
    PrintScreen,

    /// <summary>
    ///     Scroll Lock key.
    /// </summary>
    ScrollLock,

    /// <summary>
    ///     Pause/Break key.
    /// </summary>
    Pause,

    /// <summary>
    ///     Insert key.
    /// </summary>
    Insert,

    /// <summary>
    ///     Home key.
    /// </summary>
    Home,

    /// <summary>
    ///     Page Up key.
    /// </summary>
    PageUp,

    /// <summary>
    ///     Delete key.
    /// </summary>
    Delete,

    /// <summary>
    ///     End key.
    /// </summary>
    End,

    /// <summary>
    ///     Page Down key.
    /// </summary>
    PageDown,

    /// <summary>
    ///     Right arrow key.
    /// </summary>
    Right,

    /// <summary>
    ///     Left arrow key.
    /// </summary>
    Left,

    /// <summary>
    ///     Down arrow key.
    /// </summary>
    Down,

    /// <summary>
    ///     Up arrow key.
    /// </summary>
    Up,

    /// <summary>
    ///     Num Lock/Clear key.
    /// </summary>
    NumLockClear,

    /// <summary>
    ///     Keypad divide key.
    /// </summary>
    KpDivide,

    /// <summary>
    ///     Keypad multiply key.
    /// </summary>
    KpMultiply,

    /// <summary>
    ///     Keypad minus key.
    /// </summary>
    KpMinus,

    /// <summary>
    ///     Keypad plus key.
    /// </summary>
    KpPlus,

    /// <summary>
    ///     Keypad enter key.
    /// </summary>
    KpEnter,

    /// <summary>
    ///     Keypad 1 key.
    /// </summary>
    Kp1,

    /// <summary>
    ///     Keypad 2 key.
    /// </summary>
    Kp2,

    /// <summary>
    ///     Keypad 3 key.
    /// </summary>
    Kp3,

    /// <summary>
    ///     Keypad 4 key.
    /// </summary>
    Kp4,

    /// <summary>
    ///     Keypad 5 key.
    /// </summary>
    Kp5,

    /// <summary>
    ///     Keypad 6 key.
    /// </summary>
    Kp6,

    /// <summary>
    ///     Keypad 7 key.
    /// </summary>
    Kp7,

    /// <summary>
    ///     Keypad 8 key.
    /// </summary>
    Kp8,

    /// <summary>
    ///     Keypad 9 key.
    /// </summary>
    Kp9,

    /// <summary>
    ///     Keypad 0 key.
    /// </summary>
    Kp0,

    /// <summary>
    ///     Keypad period '.' key.
    /// </summary>
    KpPeriod,

    /// <summary>
    ///     Non-US backslash key.
    /// </summary>
    NonUsBackSlash,

    /// <summary>
    ///     Application/Menu key.
    /// </summary>
    Application,

    /// <summary>
    ///     Power key.
    /// </summary>
    Power,

    /// <summary>
    ///     Keypad equals key.
    /// </summary>
    KpEquals,

    /// <summary>
    ///     Function key F13.
    /// </summary>
    F13,

    /// <summary>
    ///     Function key F14.
    /// </summary>
    F14,

    /// <summary>
    ///     Function key F15.
    /// </summary>
    F15,

    /// <summary>
    ///     Function key F16.
    /// </summary>
    F16,

    /// <summary>
    ///     Function key F17.
    /// </summary>
    F17,

    /// <summary>
    ///     Function key F18.
    /// </summary>
    F18,

    /// <summary>
    ///     Function key F19.
    /// </summary>
    F19,

    /// <summary>
    ///     Function key F20.
    /// </summary>
    F20,

    /// <summary>
    ///     Function key F21.
    /// </summary>
    F21,

    /// <summary>
    ///     Function key F22.
    /// </summary>
    F22,

    /// <summary>
    ///     Function key F23.
    /// </summary>
    F23,

    /// <summary>
    ///     Function key F24.
    /// </summary>
    F24,

    /// <summary>
    ///     Execute key.
    /// </summary>
    Execute,

    /// <summary>
    ///     Help key.
    /// </summary>
    Help,

    /// <summary>
    ///     Menu key.
    /// </summary>
    Menu,

    /// <summary>
    ///     Select key.
    /// </summary>
    Select,

    /// <summary>
    ///     Stop key.
    /// </summary>
    Stop,

    /// <summary>
    ///     Again/Redo key.
    /// </summary>
    Again,

    /// <summary>
    ///     Undo key.
    /// </summary>
    Undo,

    /// <summary>
    ///     Cut key.
    /// </summary>
    Cut,

    /// <summary>
    ///     Copy key.
    /// </summary>
    Copy,

    /// <summary>
    ///     Paste key.
    /// </summary>
    Paste,

    /// <summary>
    ///     Find key.
    /// </summary>
    Find,

    /// <summary>
    ///     Mute key.
    /// </summary>
    Mute,

    /// <summary>
    ///     Volume up key.
    /// </summary>
    VolumeUp,

    /// <summary>
    ///     Volume down key.
    /// </summary>
    VolumeDown,

    /// <summary>
    ///     Keypad comma ',' key.
    /// </summary>
    KpComma,

    /// <summary>
    ///     Keypad equals (AS/400) key.
    /// </summary>
    KpEqualsAs400,

    /// <summary>
    ///     International key 1.
    /// </summary>
    International1,

    /// <summary>
    ///     International key 2.
    /// </summary>
    International2,

    /// <summary>
    ///     International key 3.
    /// </summary>
    International3,

    /// <summary>
    ///     International key 4.
    /// </summary>
    International4,

    /// <summary>
    ///     International key 5.
    /// </summary>
    International5,

    /// <summary>
    ///     International key 6.
    /// </summary>
    International6,

    /// <summary>
    ///     International key 7.
    /// </summary>
    International7,

    /// <summary>
    ///     International key 8.
    /// </summary>
    International8,

    /// <summary>
    ///     International key 9.
    /// </summary>
    International9,

    /// <summary>
    ///     Language key 1.
    /// </summary>
    Lang1,

    /// <summary>
    ///     Language key 2.
    /// </summary>
    Lang2,

    /// <summary>
    ///     Language key 3.
    /// </summary>
    Lang3,

    /// <summary>
    ///     Language key 4.
    /// </summary>
    Lang4,

    /// <summary>
    ///     Language key 5.
    /// </summary>
    Lang5,

    /// <summary>
    ///     Language key 6.
    /// </summary>
    Lang6,

    /// <summary>
    ///     Language key 7.
    /// </summary>
    Lang7,

    /// <summary>
    ///     Language key 8.
    /// </summary>
    Lang8,

    /// <summary>
    ///     Language key 9.
    /// </summary>
    Lang9,

    /// <summary>
    ///     Alternate erase key.
    /// </summary>
    AltErase,

    /// <summary>
    ///     System request key.
    /// </summary>
    SysReq,

    /// <summary>
    ///     Cancel key.
    /// </summary>
    Cancel,

    /// <summary>
    ///     Clear key.
    /// </summary>
    Clear,

    /// <summary>
    ///     Prior key.
    /// </summary>
    Prior,

    /// <summary>
    ///     Enter (alternative) key.
    /// </summary>
    Return2,

    /// <summary>
    ///     Separator key.
    /// </summary>
    Separator,

    /// <summary>
    ///     Out key.
    /// </summary>
    Out,

    /// <summary>
    ///     Oper key.
    /// </summary>
    Oper,

    /// <summary>
    ///     Clear again key.
    /// </summary>
    ClearAgain,

    /// <summary>
    ///     CrSel key.
    /// </summary>
    CrSel,

    /// <summary>
    ///     ExSel key.
    /// </summary>
    ExSel,

    /// <summary>
    ///     Keypad 00 key.
    /// </summary>
    Kp00,

    /// <summary>
    ///     Keypad 000 key.
    /// </summary>
    Kp000,

    /// <summary>
    ///     Thousands separator key.
    /// </summary>
    ThousandsSeparator,

    /// <summary>
    ///     Decimal separator key.
    /// </summary>
    DecimalSeparator,

    /// <summary>
    ///     Currency unit key.
    /// </summary>
    CurrencyUnit,

    /// <summary>
    ///     Currency subunit key.
    /// </summary>
    CurrencySubunit,

    /// <summary>
    ///     Keypad left parenthesis '(' key.
    /// </summary>
    KpLeftParen,

    /// <summary>
    ///     Keypad right parenthesis ')' key.
    /// </summary>
    KpRightParen,

    /// <summary>
    ///     Keypad left brace '{' key.
    /// </summary>
    KpLeftBrace,

    /// <summary>
    ///     Keypad right brace '}' key.
    /// </summary>
    KpRightBrace,

    /// <summary>
    ///     Keypad Tab key.
    /// </summary>
    KpTab,

    /// <summary>
    ///     Keypad Backspace key.
    /// </summary>
    KpBackspace,

    /// <summary>
    ///     Keypad A key.
    /// </summary>
    KpA,

    /// <summary>
    ///     Keypad B key.
    /// </summary>
    KpB,

    /// <summary>
    ///     Keypad C key.
    /// </summary>
    KpC,

    /// <summary>
    ///     Keypad D key.
    /// </summary>
    KpD,

    /// <summary>
    ///     Keypad E key.
    /// </summary>
    KpE,

    /// <summary>
    ///     Keypad F key.
    /// </summary>
    KpF,

    /// <summary>
    ///     Keypad XOR key.
    /// </summary>
    KpXor,

    /// <summary>
    ///     Keypad power key.
    /// </summary>
    KpPower,

    /// <summary>
    ///     Keypad percent '%' key.
    /// </summary>
    KpPercent,

    /// <summary>
    ///     Keypad less-than '&lt;' key.
    /// </summary>
    KpLess,

    /// <summary>
    ///     Keypad greater-than '&gt;' key.
    /// </summary>
    KpGreater,

    /// <summary>
    ///     Keypad ampersand '&amp;' key.
    /// </summary>
    KpAmpersand,

    /// <summary>
    ///     Keypad double ampersand '&&' key.
    /// </summary>
    KpDblAmpersand,

    /// <summary>
    ///     Keypad vertical bar '|' key.
    /// </summary>
    KpVerticalBar,

    /// <summary>
    ///     Keypad double vertical bar '||' key.
    /// </summary>
    KpDblVerticalBar,

    /// <summary>
    ///     Keypad colon ':' key.
    /// </summary>
    KpColon,

    /// <summary>
    ///     Keypad hash '#' key.
    /// </summary>
    KpHash,

    /// <summary>
    ///     Keypad space key.
    /// </summary>
    KpSpace,

    /// <summary>
    ///     Keypad at '@' key.
    /// </summary>
    KpAt,

    /// <summary>
    ///     Keypad exclamation '!' key.
    /// </summary>
    KpExClam,

    /// <summary>
    ///     Keypad memory store key.
    /// </summary>
    KpMemStore,

    /// <summary>
    ///     Keypad memory recall key.
    /// </summary>
    KpMemRecall,

    /// <summary>
    ///     Keypad memory clear key.
    /// </summary>
    KpMemClear,

    /// <summary>
    ///     Keypad memory add key.
    /// </summary>
    KpMemAdd,

    /// <summary>
    ///     Keypad memory subtract key.
    /// </summary>
    KpMemSubtract,

    /// <summary>
    ///     Keypad memory multiply key.
    /// </summary>
    KpMemMultiply,

    /// <summary>
    ///     Keypad memory divide key.
    /// </summary>
    KpMemDivide,

    /// <summary>
    ///     Keypad plus/minus key.
    /// </summary>
    KpPlusMinus,

    /// <summary>
    ///     Keypad clear key.
    /// </summary>
    KpClear,

    /// <summary>
    ///     Keypad clear entry key.
    /// </summary>
    KpClearEntry,

    /// <summary>
    ///     Keypad binary mode key.
    /// </summary>
    KpBinary,

    /// <summary>
    ///     Keypad octal mode key.
    /// </summary>
    KpOctal,

    /// <summary>
    ///     Keypad decimal mode key.
    /// </summary>
    KpDecimal,

    /// <summary>
    ///     Keypad hexadecimal mode key.
    /// </summary>
    KpHexadecimal,

    /// <summary>
    ///     Left Control key.
    /// </summary>
    LCtrl,

    /// <summary>
    ///     Left Shift key.
    /// </summary>
    LShift,

    /// <summary>
    ///     Left Alt key.
    /// </summary>
    LAlt,

    /// <summary>
    ///     Left GUI/Windows key.
    /// </summary>
    LGUI,

    /// <summary>
    ///     Right Control key.
    /// </summary>
    RCtrl,

    /// <summary>
    ///     Right Shift key.
    /// </summary>
    RShift,

    /// <summary>
    ///     Right Alt key.
    /// </summary>
    RAlt,

    /// <summary>
    ///     Right GUI/Windows key.
    /// </summary>
    RGUI,

    /// <summary>
    ///     Mode switch key.
    /// </summary>
    Mode,

    /// <summary>
    ///     Sleep key.
    /// </summary>
    Sleep,

    /// <summary>
    ///     Wake key.
    /// </summary>
    Wake,

    /// <summary>
    ///     Channel increment key.
    /// </summary>
    ChannelIncrement,

    /// <summary>
    ///     Channel decrement key.
    /// </summary>
    ChannelDecrement,

    /// <summary>
    ///     Media Play key.
    /// </summary>
    MediaPlay,

    /// <summary>
    ///     Media Pause key.
    /// </summary>
    MediaPause,

    /// <summary>
    ///     Media Record key.
    /// </summary>
    MediaRecord,

    /// <summary>
    ///     Media Fast Forward key.
    /// </summary>
    MediaFastForward,

    /// <summary>
    ///     Media Rewind key.
    /// </summary>
    MediaRewind,

    /// <summary>
    ///     Media Next Track key.
    /// </summary>
    MediaNextTrack,

    /// <summary>
    ///     Media Previous Track key.
    /// </summary>
    MediaPreviousTrack,

    /// <summary>
    ///     Media Stop key.
    /// </summary>
    MediaStop,

    /// <summary>
    ///     Media Eject key.
    /// </summary>
    MediaEject,

    /// <summary>
    ///     Media Play/Pause toggle key.
    /// </summary>
    MediaPlayPause,

    /// <summary>
    ///     Media Select key.
    /// </summary>
    MediaSelect,

    /// <summary>
    ///     App Command New.
    /// </summary>
    ACNew,

    /// <summary>
    ///     App Command Open.
    /// </summary>
    ACOpen,

    /// <summary>
    ///     App Command Close.
    /// </summary>
    ACClose,

    /// <summary>
    ///     App Command Exit.
    /// </summary>
    ACExit,

    /// <summary>
    ///     App Command Save.
    /// </summary>
    ACSave,

    /// <summary>
    ///     App Command Print.
    /// </summary>
    ACPrint,

    /// <summary>
    ///     App Command Properties.
    /// </summary>
    ACProperties,

    /// <summary>
    ///     App Command Search.
    /// </summary>
    ACSearch,

    /// <summary>
    ///     App Command Home.
    /// </summary>
    ACHome,

    /// <summary>
    ///     App Command Back.
    /// </summary>
    ACBack,

    /// <summary>
    ///     App Command Forward.
    /// </summary>
    ACForward,

    /// <summary>
    ///     App Command Stop.
    /// </summary>
    ACStop,

    /// <summary>
    ///     App Command Refresh.
    /// </summary>
    ACRefresh,

    /// <summary>
    ///     App Command Bookmarks.
    /// </summary>
    ACBookmarks,

    /// <summary>
    ///     Soft Left key (mobile).
    /// </summary>
    SoftLeft,

    /// <summary>
    ///     Soft Right key (mobile).
    /// </summary>
    SoftRight,

    /// <summary>
    ///     Call/Answer key (mobile).
    /// </summary>
    Call,

    /// <summary>
    ///     End Call key (mobile).
    /// </summary>
    EndCall
}