using System.Diagnostics;
using Brine2D.Core.Input;
using SDL;
// + for KeyChord

namespace Brine2D.SDL.Input;

internal sealed class SdlKeyboard : IKeyboard
{
    private static readonly Stopwatch _sw = Stopwatch.StartNew();

    private readonly Key[] _allKeys = Enum.GetValues<Key>();
    private readonly bool[] _down;
    private readonly double[] _downSince;
    private readonly double[] _lastPress; // for double-tap
    private readonly bool[] _pressed;
    private readonly bool[] _released;

    // Text input buffer (per-frame) and IME composition
    private readonly List<string> _typed = new();

    // Optional custom scancode mapper
    private Func<SDL_Scancode, Key>? _customMap;

    private bool _suppressKeyRepeat = true;

    public SdlKeyboard()
    {
        var n = _allKeys.Length;
        _down = new bool[n];
        _pressed = new bool[n];
        _released = new bool[n];
        _downSince = new double[n];
        _lastPress = new double[n];
    }

    public int CompositionLength { get; private set; }
    public int CompositionStart { get; private set; }

    public string CompositionText { get; private set; } = string.Empty;

    // Helpers
    public bool IsAnyKeyDown { get; private set; }

    public bool IsComposing => CompositionText.Length > 0;

    // Modifiers
    public KeyboardModifiers Modifiers { get; private set; }

    // When true, repeated SDL keydown events generate WasKeyPressed edges.
    public bool TreatRepeatAsPressed { get; set; }
    public string TypedAsString => _typed.Count == 0 ? string.Empty : string.Concat(_typed);
    public IReadOnlyList<string> TypedThisFrame => _typed;
    public bool WasAnyKeyPressedThisFrame { get; private set; }

    public bool WasAnyKeyReleasedThisFrame { get; private set; }

    private double _now => _sw.Elapsed.TotalSeconds;

    // Identify pure modifier keys (useful for rebind UIs)
    public static bool IsModifierKey(Key k)
    {
        return k is Key.LeftShift or Key.RightShift
            or Key.LeftControl or Key.RightControl
            or Key.LeftAlt or Key.RightAlt
            or Key.LeftSuper or Key.RightSuper;
    }

    public int GetDownKeys(Span<Key> buffer)
    {
        var count = 0;
        for (var i = 0; i < _down.Length && count < buffer.Length; i++)
        {
            if (_down[i])
            {
                buffer[count++] = (Key)i;
            }
        }

        return count;
    }

    public double GetHeldSeconds(Key key)
    {
        var idx = (int)key;
        if (idx < 0 || idx >= _down.Length || !_down[idx])
        {
            return 0;
        }

        return _now - _downSince[idx];
    }

    public int GetPressedThisFrame(Span<Key> buffer)
    {
        var count = 0;
        for (var i = 0; i < _pressed.Length && count < buffer.Length; i++)
        {
            if (_pressed[i])
            {
                buffer[count++] = (Key)i;
            }
        }

        return count;
    }

    public int GetReleasedThisFrame(Span<Key> buffer)
    {
        var count = 0;
        for (var i = 0; i < _released.Length && count < buffer.Length; i++)
        {
            if (_released[i])
            {
                buffer[count++] = (Key)i;
            }
        }

        return count;
    }

    public bool IsChordDown(Key key, KeyboardModifiers required)
    {
        return IsKeyDown(key) && (Modifiers & required) == required;
    }

    // IInput
    public bool IsKeyDown(Key key)
    {
        return key is >= 0 && (int)key < _down.Length && _down[(int)key];
    }

    // Long-press detection while held
    public bool IsLongPress(Key key, double thresholdSeconds)
    {
        return IsKeyDown(key) && GetHeldSeconds(key) >= thresholdSeconds;
    }

    public void SetKeyMapping(Func<SDL_Scancode, Key>? mapper)
    {
        _customMap = mapper;
    }

    public void SetSuppressKeyRepeat(bool enabled)
    {
        _suppressKeyRepeat = enabled;
    }

    // Rebinding helper: returns the first key pressed this frame as a chord with current modifiers.
    // ignorePureModifiers: when true, ignores presses that are only modifiers.
    public bool TryGetPressedChord(out KeyChord chord, bool ignorePureModifiers = true)
    {
        // Prefer non-modifier keys
        for (var i = 0; i < _pressed.Length; i++)
        {
            if (!_pressed[i])
            {
                continue;
            }

            var key = (Key)i;
            if (ignorePureModifiers && IsModifierKey(key))
            {
                continue;
            }

            chord = new KeyChord(key, Modifiers);
            return true;
        }

        // Fall back to modifiers if requested
        if (!ignorePureModifiers)
        {
            for (var i = 0; i < _pressed.Length; i++)
            {
                if (!_pressed[i])
                {
                    continue;
                }

                var key = (Key)i;
                if (IsModifierKey(key))
                {
                    chord = new KeyChord(key, Modifiers);
                    return true;
                }
            }
        }

        chord = default;
        return false;
    }

    public bool WasChordPressed(Key key, KeyboardModifiers required)
    {
        return WasKeyPressed(key) && (Modifiers & required) == required;
    }

    // Double-tap: true if pressed this frame and previous press within thresholdSeconds
    public bool WasDoublePressed(Key key, double thresholdSeconds = 0.3)
    {
        var idx = (int)key;
        if (idx < 0 || idx >= _pressed.Length || !_pressed[idx])
        {
            return false;
        }

        // Compare with prior press time (set on keydown)
        var dt = _now - _lastPress[idx];
        return dt > 0 && dt <= thresholdSeconds;
    }

    public bool WasKeyPressed(Key key)
    {
        return key is >= 0 && (int)key < _pressed.Length && _pressed[(int)key];
    }

    public bool WasKeyReleased(Key key)
    {
        return key is >= 0 && (int)key < _released.Length && _released[(int)key];
    }

    // Long-press released this frame (held long enough, then released)
    public bool WasLongPressReleased(Key key, double thresholdSeconds)
    {
        var idx = (int)key;
        if (idx < 0 || idx >= _down.Length)
        {
            return false;
        }

        return _released[idx] && _now - _downSince[idx] >= thresholdSeconds;
    }

    // Per-frame reset
    internal void BeginFrame()
    {
        Array.Clear(_pressed, 0, _pressed.Length);
        Array.Clear(_released, 0, _released.Length);
        WasAnyKeyPressedThisFrame = false;
        WasAnyKeyReleasedThisFrame = false;
        _typed.Clear();
        // Composition persists until editing or committed (OnTextEditing/OnTextInput)
    }

    internal void OnFocusGained()
    {
        // Defensive clear to avoid “stuck keys” after alt-tab on some platforms
        OnFocusLost();
    }

    internal void OnFocusLost()
    {
        Array.Clear(_down, 0, _down.Length);
        Array.Clear(_pressed, 0, _pressed.Length);
        Array.Clear(_released, 0, _released.Length);
        IsAnyKeyDown = false;
        WasAnyKeyPressedThisFrame = false;
        WasAnyKeyReleasedThisFrame = false;
        _typed.Clear();
        CompositionText = string.Empty;
        CompositionStart = CompositionLength = 0;
        Modifiers = KeyboardModifiers.None;
    }

    internal void OnKeyDown(SDL_Scancode scancode, bool isRepeat)
    {
        if (_suppressKeyRepeat && isRepeat)
        {
            return;
        }

        var k = Map(scancode);
        if (k == Key.Unknown)
        {
            return;
        }

        var idx = (int)k;
        if (!_down[idx])
        {
            _down[idx] = true;
            _downSince[idx] = _now;

            _pressed[idx] = true;
            WasAnyKeyPressedThisFrame = true;
            IsAnyKeyDown = true;

            // track last press for double-tap
            _lastPress[idx] = _now;

            RecomputeModifiersOn(k, true);
        }
        else if (isRepeat && TreatRepeatAsPressed)
        {
            _pressed[idx] = true;
            WasAnyKeyPressedThisFrame = true;
        }
    }

    internal void OnKeyUp(SDL_Scancode scancode)
    {
        var k = Map(scancode);
        if (k == Key.Unknown)
        {
            return;
        }

        var idx = (int)k;
        if (_down[idx])
        {
            _down[idx] = false;
            _released[idx] = true;
            WasAnyKeyReleasedThisFrame = true;

            // Recompute any-down cheaply if we just turned off the last one.
            if (IsAnyKeyDown)
            {
                IsAnyKeyDown = false;
                for (var i = 0; i < _down.Length; i++)
                {
                    if (_down[i])
                    {
                        IsAnyKeyDown = true;
                        break;
                    }
                }
            }

            RecomputeModifiersOn(k, false);
        }
    }

    internal void OnTextEditing(string text, int start, int length)
    {
        CompositionText = text ?? string.Empty;
        CompositionStart = start;
        CompositionLength = length;
    }

    internal void OnTextInput(string text)
    {
        if (!string.IsNullOrEmpty(text))
        {
            _typed.Add(text);
        }

        // IME: committed text should clear composition state
        CompositionText = string.Empty;
        CompositionStart = CompositionLength = 0;
    }

    // Keep Modifiers accurate using SDL’s mod state (called by host on key events)
    internal void SyncModifiersFromSDL(SDL_Keymod sdlMods)
    {
        KeyboardModifiers mods = 0;

        bool has(SDL_Keymod m)
        {
            return (sdlMods & m) != 0;
        }

        // Combined
        if (has(SDL_Keymod.SDL_KMOD_SHIFT))
        {
            mods |= KeyboardModifiers.Shift;
        }

        if (has(SDL_Keymod.SDL_KMOD_CTRL))
        {
            mods |= KeyboardModifiers.Control;
        }

        if (has(SDL_Keymod.SDL_KMOD_ALT))
        {
            mods |= KeyboardModifiers.Alt;
        }

        if (has(SDL_Keymod.SDL_KMOD_GUI))
        {
            mods |= KeyboardModifiers.Super;
        }

        // Sides
        if (has(SDL_Keymod.SDL_KMOD_LSHIFT))
        {
            mods |= KeyboardModifiers.LeftShift;
        }

        if (has(SDL_Keymod.SDL_KMOD_RSHIFT))
        {
            mods |= KeyboardModifiers.RightShift;
        }

        if (has(SDL_Keymod.SDL_KMOD_LCTRL))
        {
            mods |= KeyboardModifiers.LeftControl;
        }

        if (has(SDL_Keymod.SDL_KMOD_RCTRL))
        {
            mods |= KeyboardModifiers.RightControl;
        }

        if (has(SDL_Keymod.SDL_KMOD_LALT))
        {
            mods |= KeyboardModifiers.LeftAlt;
        }

        if (has(SDL_Keymod.SDL_KMOD_RALT))
        {
            mods |= KeyboardModifiers.RightAlt;
        }

        if (has(SDL_Keymod.SDL_KMOD_LGUI))
        {
            mods |= KeyboardModifiers.LeftSuper;
        }

        if (has(SDL_Keymod.SDL_KMOD_RGUI))
        {
            mods |= KeyboardModifiers.RightSuper;
        }

        // Locks
        if (has(SDL_Keymod.SDL_KMOD_CAPS))
        {
            mods |= KeyboardModifiers.CapsLock;
        }

        if (has(SDL_Keymod.SDL_KMOD_NUM))
        {
            mods |= KeyboardModifiers.NumLock;
        }

        Modifiers = mods;
    }

    private Key Map(SDL_Scancode sc)
    {
        if (_customMap is not null)
        {
            try
            {
                var k = _customMap(sc);
                if (k != Key.Unknown)
                {
                    return k;
                }
            }
            catch
            {
                /* ignore mapping exceptions */
            }
        }

        return sc switch
        {
            // Navigation / editing
            SDL_Scancode.SDL_SCANCODE_ESCAPE => Key.Escape,
            SDL_Scancode.SDL_SCANCODE_TAB => Key.Tab,
            SDL_Scancode.SDL_SCANCODE_RETURN => Key.Enter,
            SDL_Scancode.SDL_SCANCODE_BACKSPACE => Key.Backspace,
            SDL_Scancode.SDL_SCANCODE_INSERT => Key.Insert,
            SDL_Scancode.SDL_SCANCODE_DELETE => Key.Delete,
            SDL_Scancode.SDL_SCANCODE_HOME => Key.Home,
            SDL_Scancode.SDL_SCANCODE_END => Key.End,
            SDL_Scancode.SDL_SCANCODE_PAGEUP => Key.PageUp,
            SDL_Scancode.SDL_SCANCODE_PAGEDOWN => Key.PageDown,

            // Arrows
            SDL_Scancode.SDL_SCANCODE_LEFT => Key.Left,
            SDL_Scancode.SDL_SCANCODE_RIGHT => Key.Right,
            SDL_Scancode.SDL_SCANCODE_UP => Key.Up,
            SDL_Scancode.SDL_SCANCODE_DOWN => Key.Down,

            // Whitespace
            SDL_Scancode.SDL_SCANCODE_SPACE => Key.Space,

            // Digits
            SDL_Scancode.SDL_SCANCODE_0 => Key.D0,
            SDL_Scancode.SDL_SCANCODE_1 => Key.D1,
            SDL_Scancode.SDL_SCANCODE_2 => Key.D2,
            SDL_Scancode.SDL_SCANCODE_3 => Key.D3,
            SDL_Scancode.SDL_SCANCODE_4 => Key.D4,
            SDL_Scancode.SDL_SCANCODE_5 => Key.D5,
            SDL_Scancode.SDL_SCANCODE_6 => Key.D6,
            SDL_Scancode.SDL_SCANCODE_7 => Key.D7,
            SDL_Scancode.SDL_SCANCODE_8 => Key.D8,
            SDL_Scancode.SDL_SCANCODE_9 => Key.D9,

            // Letters
            SDL_Scancode.SDL_SCANCODE_A => Key.A,
            SDL_Scancode.SDL_SCANCODE_B => Key.B,
            SDL_Scancode.SDL_SCANCODE_C => Key.C,
            SDL_Scancode.SDL_SCANCODE_D => Key.D,
            SDL_Scancode.SDL_SCANCODE_E => Key.E,
            SDL_Scancode.SDL_SCANCODE_F => Key.F,
            SDL_Scancode.SDL_SCANCODE_G => Key.G,
            SDL_Scancode.SDL_SCANCODE_H => Key.H,
            SDL_Scancode.SDL_SCANCODE_I => Key.I,
            SDL_Scancode.SDL_SCANCODE_J => Key.J,
            SDL_Scancode.SDL_SCANCODE_K => Key.K,
            SDL_Scancode.SDL_SCANCODE_L => Key.L,
            SDL_Scancode.SDL_SCANCODE_M => Key.M,
            SDL_Scancode.SDL_SCANCODE_N => Key.N,
            SDL_Scancode.SDL_SCANCODE_O => Key.O,
            SDL_Scancode.SDL_SCANCODE_P => Key.P,
            SDL_Scancode.SDL_SCANCODE_Q => Key.Q,
            SDL_Scancode.SDL_SCANCODE_R => Key.R,
            SDL_Scancode.SDL_SCANCODE_S => Key.S,
            SDL_Scancode.SDL_SCANCODE_T => Key.T,
            SDL_Scancode.SDL_SCANCODE_U => Key.U,
            SDL_Scancode.SDL_SCANCODE_V => Key.V,
            SDL_Scancode.SDL_SCANCODE_W => Key.W,
            SDL_Scancode.SDL_SCANCODE_X => Key.X,
            SDL_Scancode.SDL_SCANCODE_Y => Key.Y,
            SDL_Scancode.SDL_SCANCODE_Z => Key.Z,

            // Function keys
            SDL_Scancode.SDL_SCANCODE_F1 => Key.F1,
            SDL_Scancode.SDL_SCANCODE_F2 => Key.F2,
            SDL_Scancode.SDL_SCANCODE_F3 => Key.F3,
            SDL_Scancode.SDL_SCANCODE_F4 => Key.F4,
            SDL_Scancode.SDL_SCANCODE_F5 => Key.F5,
            SDL_Scancode.SDL_SCANCODE_F6 => Key.F6,
            SDL_Scancode.SDL_SCANCODE_F7 => Key.F7,
            SDL_Scancode.SDL_SCANCODE_F8 => Key.F8,
            SDL_Scancode.SDL_SCANCODE_F9 => Key.F9,
            SDL_Scancode.SDL_SCANCODE_F10 => Key.F10,
            SDL_Scancode.SDL_SCANCODE_F11 => Key.F11,
            SDL_Scancode.SDL_SCANCODE_F12 => Key.F12,

            // Locks
            SDL_Scancode.SDL_SCANCODE_CAPSLOCK => Key.CapsLock,
            SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR => Key.NumLock,
            SDL_Scancode.SDL_SCANCODE_SCROLLLOCK => Key.ScrollLock,

            // Print/Pause
            SDL_Scancode.SDL_SCANCODE_PRINTSCREEN => Key.PrintScreen,
            SDL_Scancode.SDL_SCANCODE_PAUSE => Key.Pause,

            // Punctuation / symbols
            SDL_Scancode.SDL_SCANCODE_GRAVE => Key.Grave,
            SDL_Scancode.SDL_SCANCODE_MINUS => Key.Minus,
            SDL_Scancode.SDL_SCANCODE_EQUALS => Key.Equals,
            SDL_Scancode.SDL_SCANCODE_LEFTBRACKET => Key.LeftBracket,
            SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET => Key.RightBracket,
            SDL_Scancode.SDL_SCANCODE_BACKSLASH => Key.Backslash,
            SDL_Scancode.SDL_SCANCODE_SEMICOLON => Key.Semicolon,
            SDL_Scancode.SDL_SCANCODE_APOSTROPHE => Key.Apostrophe,
            SDL_Scancode.SDL_SCANCODE_COMMA => Key.Comma,
            SDL_Scancode.SDL_SCANCODE_PERIOD => Key.Period,
            SDL_Scancode.SDL_SCANCODE_SLASH => Key.Slash,

            // Modifiers
            SDL_Scancode.SDL_SCANCODE_LSHIFT => Key.LeftShift,
            SDL_Scancode.SDL_SCANCODE_RSHIFT => Key.RightShift,
            SDL_Scancode.SDL_SCANCODE_LCTRL => Key.LeftControl,
            SDL_Scancode.SDL_SCANCODE_RCTRL => Key.RightControl,
            SDL_Scancode.SDL_SCANCODE_LALT => Key.LeftAlt,
            SDL_Scancode.SDL_SCANCODE_RALT => Key.RightAlt,
            SDL_Scancode.SDL_SCANCODE_LGUI => Key.LeftSuper,
            SDL_Scancode.SDL_SCANCODE_RGUI => Key.RightSuper,
            SDL_Scancode.SDL_SCANCODE_APPLICATION => Key.Menu,

            // Numpad
            SDL_Scancode.SDL_SCANCODE_KP_0 => Key.Numpad0,
            SDL_Scancode.SDL_SCANCODE_KP_1 => Key.Numpad1,
            SDL_Scancode.SDL_SCANCODE_KP_2 => Key.Numpad2,
            SDL_Scancode.SDL_SCANCODE_KP_3 => Key.Numpad3,
            SDL_Scancode.SDL_SCANCODE_KP_4 => Key.Numpad4,
            SDL_Scancode.SDL_SCANCODE_KP_5 => Key.Numpad5,
            SDL_Scancode.SDL_SCANCODE_KP_6 => Key.Numpad6,
            SDL_Scancode.SDL_SCANCODE_KP_7 => Key.Numpad7,
            SDL_Scancode.SDL_SCANCODE_KP_8 => Key.Numpad8,
            SDL_Scancode.SDL_SCANCODE_KP_9 => Key.Numpad9,
            SDL_Scancode.SDL_SCANCODE_KP_DIVIDE => Key.NumpadDivide,
            SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY => Key.NumpadMultiply,
            SDL_Scancode.SDL_SCANCODE_KP_MINUS => Key.NumpadMinus,
            SDL_Scancode.SDL_SCANCODE_KP_PLUS => Key.NumpadPlus,
            SDL_Scancode.SDL_SCANCODE_KP_ENTER => Key.NumpadEnter,
            SDL_Scancode.SDL_SCANCODE_KP_PERIOD => Key.NumpadPeriod,

            // Media
            SDL_Scancode.SDL_SCANCODE_MEDIA_PLAY => Key.MediaPlayPause,
            SDL_Scancode.SDL_SCANCODE_MEDIA_STOP => Key.MediaStop,
            SDL_Scancode.SDL_SCANCODE_MEDIA_NEXT_TRACK => Key.MediaNextTrack,
            SDL_Scancode.SDL_SCANCODE_MEDIA_PREVIOUS_TRACK => Key.MediaPrevTrack,
            SDL_Scancode.SDL_SCANCODE_MUTE => Key.VolumeMute,
            SDL_Scancode.SDL_SCANCODE_VOLUMEUP => Key.VolumeUp,
            SDL_Scancode.SDL_SCANCODE_VOLUMEDOWN => Key.VolumeDown,

            _ => Key.Unknown
        };
    }

    private void RecomputeModifiersOn(Key k, bool isDown)
    {
        // Update side-specific flags first
        var mods = Modifiers;

        void Set(KeyboardModifiers flag, bool on)
        {
            if (on)
            {
                mods |= flag;
            }
            else
            {
                mods &= ~flag;
            }
        }

        switch (k)
        {
            case Key.LeftShift: Set(KeyboardModifiers.LeftShift, isDown); break;
            case Key.RightShift: Set(KeyboardModifiers.RightShift, isDown); break;
            case Key.LeftControl: Set(KeyboardModifiers.LeftControl, isDown); break;
            case Key.RightControl: Set(KeyboardModifiers.RightControl, isDown); break;
            case Key.LeftAlt: Set(KeyboardModifiers.LeftAlt, isDown); break;
            case Key.RightAlt: Set(KeyboardModifiers.RightAlt, isDown); break;
            case Key.LeftSuper: Set(KeyboardModifiers.LeftSuper, isDown); break;
            case Key.RightSuper: Set(KeyboardModifiers.RightSuper, isDown); break;

            case Key.CapsLock:
            case Key.NumLock:
                // Locks toggled by OS; combined state comes from SyncModifiersFromSDL
                break;
        }

        // Derive combined flags from side flags and current downs (best effort; host sync wins)
        var lShift = _down[(int)Key.LeftShift];
        var rShift = _down[(int)Key.RightShift];
        var lCtrl = _down[(int)Key.LeftControl];
        var rCtrl = _down[(int)Key.RightControl];
        var lAlt = _down[(int)Key.LeftAlt];
        var rAlt = _down[(int)Key.RightAlt];
        var lSup = _down[(int)Key.LeftSuper];
        var rSup = _down[(int)Key.RightSuper];

        if (lShift || rShift)
        {
            mods |= KeyboardModifiers.Shift;
        }
        else
        {
            mods &= ~KeyboardModifiers.Shift;
        }

        if (lCtrl || rCtrl)
        {
            mods |= KeyboardModifiers.Control;
        }
        else
        {
            mods &= ~KeyboardModifiers.Control;
        }

        if (lAlt || rAlt)
        {
            mods |= KeyboardModifiers.Alt;
        }
        else
        {
            mods &= ~KeyboardModifiers.Alt;
        }

        if (lSup || rSup)
        {
            mods |= KeyboardModifiers.Super;
        }
        else
        {
            mods &= ~KeyboardModifiers.Super;
        }

        Modifiers = mods;
    }
}