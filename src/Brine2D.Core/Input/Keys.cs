namespace Brine2D.Core.Input;

/// <summary>
/// Keyboard key codes.
/// </summary>
public enum Keys
{
    Unknown = 0,
    
    // Letters
    A, B, C, D, E, F, G, H, I, J, K, L, M,
    N, O, P, Q, R, S, T, U, V, W, X, Y, Z,
    
    // Numbers
    D0, D1, D2, D3, D4, D5, D6, D7, D8, D9,
    
    // Function keys
    F1, F2, F3, F4, F5, F6, F7, F8, F9, F10, F11, F12,
    
    // Arrow keys
    Left, Right, Up, Down,
    
    // Special keys
    Space, Enter, Escape, Tab, Backspace, Delete,
    LeftShift, RightShift, LeftControl, RightControl,
    LeftAlt, RightAlt, CapsLock,
    
    // Numpad
    Numpad0, Numpad1, Numpad2, Numpad3, Numpad4,
    Numpad5, Numpad6, Numpad7, Numpad8, Numpad9,
    NumpadEnter, NumpadPlus, NumpadMinus,
    NumpadMultiply, NumpadDivide, NumpadPeriod,
    
    // Navigation
    Home, End, PageUp, PageDown, Insert,
    
    // Other
    PrintScreen, ScrollLock, Pause
}