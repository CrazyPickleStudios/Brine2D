namespace Brine2D.Core.Input;

/// <summary>
///     Keyboard chord consisting of a key and a set of modifier flags.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description>Matching semantics are “at least these modifiers”: a chord matches when the key is active and all bits in <see cref="Modifiers" /> are present in the current modifier mask; extra modifiers do not invalidate the match.</description></item>
///         <item><description><see cref="KeyboardModifiers" /> may include combined bits (e.g., <see cref="KeyboardModifiers.Shift" />) and/or side-specific bits (e.g., <see cref="KeyboardModifiers.LeftShift" />). Your input backend may set both for convenience.</description></item>
///         <item><description>Edge helpers (<see cref="WasPressed" />, <see cref="WasReleased" />) depend on the <see cref="IKeyboard" /> implementation’s edge tracking and should be called once per frame after the device is updated.</description></item>
///     </list>
/// </remarks>
/// <example>
///     <code><![CDATA[
/// var save = new KeyChord(Key.S, KeyboardModifiers.Control | KeyboardModifiers.Shift);
/// if (save.WasPressed(keyboard, currentMods))
/// {
///     SaveDocument();
/// }
/// ]]></code>
/// </example>
public readonly record struct KeyChord(Key Key, KeyboardModifiers Modifiers)
{
    /// <summary>
    ///     Returns whether the chord is currently held: the key is down and all required modifiers are present.
    /// </summary>
    /// <param name="input">Keyboard state source.</param>
    /// <param name="currentMods">Current modifier state for this frame.</param>
    /// <returns><c>true</c> if the key is down and the required modifiers are set; otherwise, <c>false</c>.</returns>
    public bool IsDown(IKeyboard input, KeyboardModifiers currentMods)
    {
        return input.IsKeyDown(Key) && (currentMods & Modifiers) == Modifiers;
    }

    /// <summary>
    ///     Returns <c>true</c> only on the frame the key transitioned from up to down and the required modifiers are present.
    /// </summary>
    /// <param name="input">Keyboard state source.</param>
    /// <param name="currentMods">Current modifier state for this frame.</param>
    /// <returns><c>true</c> on the first frame of the press with matching modifiers; otherwise, <c>false</c>.</returns>
    public bool WasPressed(IKeyboard input, KeyboardModifiers currentMods)
    {
        return input.WasKeyPressed(Key) && (currentMods & Modifiers) == Modifiers;
    }

    /// <summary>
    ///     Returns <c>true</c> only on the frame the key transitioned from down to up and the required modifiers are present.
    /// </summary>
    /// <param name="input">Keyboard state source.</param>
    /// <param name="currentMods">Current modifier state for this frame.</param>
    /// <returns><c>true</c> on the first frame of the release with matching modifiers; otherwise, <c>false</c>.</returns>
    public bool WasReleased(IKeyboard input, KeyboardModifiers currentMods)
    {
        return input.WasKeyReleased(Key) && (currentMods & Modifiers) == Modifiers;
    }
}

/// <summary>
///     Formatting and parsing helpers for <see cref="KeyChord" /> that use compact, OS-agnostic strings
///     (for example, "Ctrl+Shift+S" or "Alt+Num5").
/// </summary>
public static class KeyChordFormat
{
    /// <summary>
    ///     Formats a chord into a compact, OS-agnostic string like "Ctrl+Shift+S".
    /// </summary>
    /// <param name="chord">The chord to format.</param>
    /// <param name="includeLocks">
    ///     When <c>true</c>, includes lock modifiers (Caps/Num) if present in the chord's modifier mask.
    /// </param>
    /// <returns>The formatted string.</returns>
    /// <example>
    ///     <code>
    ///     var text = KeyChordFormat.Format(new KeyChord(Key.S, KeyboardModifiers.Control | KeyboardModifiers.Shift));
    ///     // "Ctrl+Shift+S"
    ///     </code>
    /// </example>
    public static string Format(KeyChord chord, bool includeLocks = false)
    {
        var parts = new List<string>(6);

        var m = chord.Modifiers;
        if ((m & KeyboardModifiers.Control) != 0)
        {
            parts.Add("Ctrl");
        }

        if ((m & KeyboardModifiers.Shift) != 0)
        {
            parts.Add("Shift");
        }

        if ((m & KeyboardModifiers.Alt) != 0)
        {
            parts.Add("Alt");
        }

        if ((m & KeyboardModifiers.Super) != 0)
        {
            parts.Add("Super");
        }

        if (includeLocks && (m & KeyboardModifiers.CapsLock) != 0)
        {
            parts.Add("Caps");
        }

        if (includeLocks && (m & KeyboardModifiers.NumLock) != 0)
        {
            parts.Add("Num");
        }

        parts.Add(FormatKeyName(chord.Key));
        return string.Join('+', parts);
    }

    /// <summary>
    ///     Attempts to parse a chord string of the form "Ctrl+Shift+S" into a <see cref="KeyChord" />.
    ///     Parsing is case-insensitive for tokens and supports a variety of aliases.
    /// </summary>
    /// <param name="s">The input string to parse.</param>
    /// <param name="chord">When successful, receives the parsed chord.</param>
    /// <returns><c>true</c> if parsed; otherwise, <c>false</c>.</returns>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>Accepts modifiers: ctrl/control, shift, alt, super/win/cmd/command, caps/capslock, num/numlock, plus side-specific forms (lshift, rctrl, etc.).</description></item>
    ///         <item><description>Accepts keys: letters/digits, punctuation (`-=[]\;',./), aliases (esc, enter/return, pgdn, prtsc…), arrow names, and numpad variants (num5, numplus, kp_5, kp_plus, numenter).</description></item>
    ///         <item><description>Requires exactly one non-modifier key token in the input.</description></item>
    ///     </list>
    /// </remarks>
    /// <example>
    ///     <code><![CDATA[
    /// KeyChordFormat.TryParse("Ctrl+Shift+S", out var chord);  // true
    /// KeyChordFormat.TryParse("alt+num5", out var chord2);     // true (Numpad5)
    /// ]]></code>
    /// </example>
    public static bool TryParse(string s, out KeyChord chord)
    {
        chord = default;
        if (string.IsNullOrWhiteSpace(s))
        {
            return false;
        }

        var mods = KeyboardModifiers.None;
        Key? key = null;

        var tokens = s.Split('+', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        foreach (var raw in tokens)
        {
            var t = raw.Trim();
            if (TryParseModifier(t, ref mods))
            {
                continue;
            }

            if (TryParseKey(t, out var k))
            {
                key = k;
                continue;
            }

            return false;
        }

        if (key is null)
        {
            return false;
        }

        chord = new KeyChord(key.Value, mods);
        return true;
    }

    /// <summary>
    ///     Formats a single <see cref="Key" /> to a compact display name where possible,
    ///     or falls back to the enum name.
    /// </summary>
    /// <param name="k">The key to format.</param>
    /// <returns>A short display string for the key.</returns>
    private static string FormatKeyName(Key k)
    {
        // Try to display compact names; fallback to enum if needed.
        return k switch
        {
            // Numpad
            Key.Numpad0 => "Num0",
            Key.Numpad1 => "Num1",
            Key.Numpad2 => "Num2",
            Key.Numpad3 => "Num3",
            Key.Numpad4 => "Num4",
            Key.Numpad5 => "Num5",
            Key.Numpad6 => "Num6",
            Key.Numpad7 => "Num7",
            Key.Numpad8 => "Num8",
            Key.Numpad9 => "Num9",
            Key.NumpadPlus => "Num+",
            Key.NumpadMinus => "Num-",
            Key.NumpadMultiply => "Num*",
            Key.NumpadDivide => "Num/",
            Key.NumpadEnter => "NumEnter",
            Key.NumpadPeriod => "Num.",

            // Navigation / editing
            Key.Escape => "Esc",
            Key.Enter => "Enter",
            Key.Backspace => "Backspace",
            Key.Tab => "Tab",
            Key.Space => "Space",
            Key.Left => "Left",
            Key.Right => "Right",
            Key.Up => "Up",
            Key.Down => "Down",
            Key.Delete => "Delete",
            Key.Insert => "Insert",
            Key.Home => "Home",
            Key.End => "End",
            Key.PageUp => "PageUp",
            Key.PageDown => "PageDown",
            Key.PrintScreen => "PrintScreen",
            Key.Pause => "Pause",

            // Media (short names)
            Key.VolumeUp => "VolUp",
            Key.VolumeDown => "VolDown",
            Key.VolumeMute => "Mute",
            Key.MediaPlayPause => "Play/Pause",
            Key.MediaNextTrack => "Next",
            Key.MediaPrevTrack => "Prev",
            Key.MediaStop => "Stop",

            _ => k.ToString()
        };
    }

    /// <summary>
    ///     Tries to interpret a token as a concrete <see cref="Key" /> (non-modifier).
    ///     Supports single characters (letters, digits, punctuation), common aliases, arrow names,
    ///     numpad variants, and falls back to enum parsing (case-insensitive).
    /// </summary>
    /// <param name="token">The token to classify.</param>
    /// <param name="key">The resolved key if successful.</param>
    /// <returns><c>true</c> if token is a key; otherwise, <c>false</c>.</returns>
    private static bool TryParseKey(string token, out Key key)
    {
        // Single letters or digits
        if (token.Length == 1)
        {
            var c = token[0];
            if (c is >= 'A' and <= 'Z')
            {
                key = (Key)Enum.Parse(typeof(Key), c.ToString());
                return true;
            }

            if (c is >= 'a' and <= 'z')
            {
                c = char.ToUpperInvariant(c);
                key = (Key)Enum.Parse(typeof(Key), c.ToString());
                return true;
            }

            if (char.IsAsciiDigit(c))
            {
                key = c switch
                {
                    '0' => Key.D0,
                    '1' => Key.D1,
                    '2' => Key.D2,
                    '3' => Key.D3,
                    '4' => Key.D4,
                    '5' => Key.D5,
                    '6' => Key.D6,
                    '7' => Key.D7,
                    '8' => Key.D8,
                    '9' => Key.D9,
                    _ => Key.Unknown
                };
                return key != Key.Unknown;
            }

            // Punctuation symbols as keys
            key = c switch
            {
                '`' => Key.Grave,
                '-' => Key.Minus,
                '=' => Key.Equals,
                '[' => Key.LeftBracket,
                ']' => Key.RightBracket,
                '\\' => Key.Backslash,
                ';' => Key.Semicolon,
                '\'' => Key.Apostrophe,
                ',' => Key.Comma,
                '.' => Key.Period,
                '/' => Key.Slash,
                _ => Key.Unknown
            };
            if (key != Key.Unknown)
            {
                return true;
            }
        }

        var t = token.Trim();
        var tl = t.ToLowerInvariant();

        // Numpad aliases
        if (TryParseNumpad(tl, out key))
        {
            return true;
        }

        // Common aliases and shorthands
        switch (tl)
        {
            // Navigation/Editing
            case "esc":
                key = Key.Escape;
                return true;
            case "ret":
            case "return":
            case "enter":
                key = Key.Enter;
                return true;
            case "bksp":
            case "back":
            case "backspace":
                key = Key.Backspace;
                return true;
            case "ins":
                key = Key.Insert;
                return true;
            case "del":
                key = Key.Delete;
                return true;

            case "pgup":
            case "pageup":
                key = Key.PageUp;
                return true;
            case "pgdn":
            case "pagedn":
            case "pagedown":
                key = Key.PageDown;
                return true;

            case "prtsc":
            case "printscr":
            case "print":
            case "printscreen":
                key = Key.PrintScreen;
                return true;

            case "break":
            case "pause":
                key = Key.Pause;
                return true;

            // Arrows
            case "leftarrow":
                key = Key.Left;
                return true;
            case "rightarrow":
                key = Key.Right;
                return true;
            case "uparrow":
                key = Key.Up;
                return true;
            case "downarrow":
                key = Key.Down;
                return true;

            // Space/Tab
            case "space":
                key = Key.Space;
                return true;
            case "tab":
                key = Key.Tab;
                return true;

            // Side-specific modifiers as keys (not just modifiers)
            case "lshift":
                key = Key.LeftShift;
                return true;
            case "rshift":
                key = Key.RightShift;
                return true;
            case "lctrl":
            case "lcontrol":
                key = Key.LeftControl;
                return true;
            case "rctrl":
            case "rcontrol":
                key = Key.RightControl;
                return true;
            case "lalt":
                key = Key.LeftAlt;
                return true;
            case "ralt":
                key = Key.RightAlt;
                return true;
            case "lsuper":
            case "lwin":
            case "lcmd":
                key = Key.LeftSuper;
                return true;
            case "rsuper":
            case "rwin":
            case "rcmd":
                key = Key.RightSuper;
                return true;

            // Menu
            case "apps":
            case "app":
            case "context":
            case "contextmenu":
            case "menu":
                key = Key.Menu;
                return true;

            // Locks (as keys)
            case "caps":
            case "capslock":
                key = Key.CapsLock;
                return true;
            case "num":
            case "numlock":
                key = Key.NumLock;
                return true;
            case "scrolllock":
            case "scroll":
                key = Key.ScrollLock;
                return true;
        }

        // Enum name fallback, case-insensitive: e.g., "F5", "PageDown", "Numpad5"
        if (Enum.TryParse(t, true, out key))
        {
            return true;
        }

        key = Key.Unknown;
        return false;
    }

    /// <summary>
    ///     Tries to interpret a token as a modifier and OR it into <paramref name="mods" />.
    ///     Accepts combined and side-specific modifier names.
    /// </summary>
    /// <param name="token">The token to classify.</param>
    /// <param name="mods">The running modifier mask to update.</param>
    /// <returns><c>true</c> if token is a modifier; otherwise, <c>false</c>.</returns>
    private static bool TryParseModifier(string token, ref KeyboardModifiers mods)
    {
        switch (token.ToLowerInvariant())
        {
            case "ctrl":
            case "control":
                mods |= KeyboardModifiers.Control;
                return true;
            case "shift":
                mods |= KeyboardModifiers.Shift;
                return true;
            case "alt":
                mods |= KeyboardModifiers.Alt;
                return true;
            case "super":
            case "win":
            case "cmd":
            case "command":
                mods |= KeyboardModifiers.Super;
                return true;

            case "caps":
            case "capslock":
                mods |= KeyboardModifiers.CapsLock;
                return true;
            case "num":
            case "numlock":
                mods |= KeyboardModifiers.NumLock;
                return true;

            // Side-specific (optional if a user wants exact sides)
            case "lctrl":
                mods |= KeyboardModifiers.LeftControl;
                mods |= KeyboardModifiers.Control;
                return true;
            case "rctrl":
                mods |= KeyboardModifiers.RightControl;
                mods |= KeyboardModifiers.Control;
                return true;
            case "lshift":
                mods |= KeyboardModifiers.LeftShift;
                mods |= KeyboardModifiers.Shift;
                return true;
            case "rshift":
                mods |= KeyboardModifiers.RightShift;
                mods |= KeyboardModifiers.Shift;
                return true;
            case "lalt":
                mods |= KeyboardModifiers.LeftAlt;
                mods |= KeyboardModifiers.Alt;
                return true;
            case "ralt":
                mods |= KeyboardModifiers.RightAlt;
                mods |= KeyboardModifiers.Alt;
                return true;
            case "lsuper":
            case "lwin":
            case "lcmd":
                mods |= KeyboardModifiers.LeftSuper;
                mods |= KeyboardModifiers.Super;
                return true;
            case "rsuper":
            case "rwin":
            case "rcmd":
                mods |= KeyboardModifiers.RightSuper;
                mods |= KeyboardModifiers.Super;
                return true;

            default: return false;
        }
    }

    /// <summary>
    ///     Parses numpad-specific tokens. Accepts prefixes "num", "numpad", and "kp", with optional separators.
    ///     Supports digits 0–9 and operators (plus, minus, multiply, divide, period, enter).
    /// </summary>
    /// <param name="tl">Lower-cased token to parse.</param>
    /// <param name="key">Resolved numpad key if successful.</param>
    /// <returns><c>true</c> if token maps to a numpad key; otherwise, <c>false</c>.</returns>
    private static bool TryParseNumpad(string tl, out Key key)
    {
        // Variants: "num5", "numpad5", "kp5", "kp_5"
        // Operators: "numplus"/"numpadadd"/"kp_plus", "num-" => minus, etc.
        key = Key.Unknown;

        // Normalize prefixes
        var hasNum = tl.StartsWith("num");
        var hasNumpad = tl.StartsWith("numpad");
        var hasKp = tl.StartsWith("kp");

        if (!(hasNum || hasNumpad || hasKp))
        {
            return false;
        }

        var s = tl.AsSpan();
        var idx = hasNumpad ? "numpad".Length : hasNum ? "num".Length : "kp".Length;

        // skip optional separator
        if (idx < s.Length && (s[idx] == '_' || s[idx] == ' '))
        {
            idx++;
        }

        // Digits 0..9
        if (idx < s.Length && char.IsAsciiDigit(s[idx]) && idx + 1 == s.Length)
        {
            key = s[idx] switch
            {
                '0' => Key.Numpad0,
                '1' => Key.Numpad1,
                '2' => Key.Numpad2,
                '3' => Key.Numpad3,
                '4' => Key.Numpad4,
                '5' => Key.Numpad5,
                '6' => Key.Numpad6,
                '7' => Key.Numpad7,
                '8' => Key.Numpad8,
                '9' => Key.Numpad9,
                _ => Key.Unknown
            };
            return key != Key.Unknown;
        }

        // Names/aliases
        var rest = s[idx..].ToString();

        switch (rest)
        {
            case "+":
            case "plus":
            case "add":
            case "kp+":
            case "kp_plus":
                key = Key.NumpadPlus;
                return true;

            case "-":
            case "minus":
            case "sub":
            case "subtract":
            case "kp-":
            case "kp_minus":
                key = Key.NumpadMinus;
                return true;

            case "*":
            case "mul":
            case "multiply":
            case "kp*":
            case "kp_multiply":
                key = Key.NumpadMultiply;
                return true;

            case "/":
            case "div":
            case "divide":
            case "kp/":
            case "kp_divide":
                key = Key.NumpadDivide;
                return true;

            case ".":
            case "decimal":
            case "period":
            case "dot":
            case "kp.":
            case "kp_period":
                key = Key.NumpadPeriod;
                return true;

            case "enter":
            case "ret":
            case "return":
            case "kp_enter":
                key = Key.NumpadEnter;
                return true;
        }

        // Explicit long names without prefix ambiguity
        switch (tl)
        {
            case "numpadplus":
            case "numplus":
                key = Key.NumpadPlus;
                return true;
            case "numpadminus":
            case "numminus":
                key = Key.NumpadMinus;
                return true;
            case "numpadmultiply":
            case "nummultiply":
                key = Key.NumpadMultiply;
                return true;
            case "numpaddivide":
            case "numdivide":
                key = Key.NumpadDivide;
                return true;
            case "numpadperiod":
            case "numperiod":
            case "numpaddecimal":
            case "numdecimal":
                key = Key.NumpadPeriod;
                return true;
            case "numpadenter":
            case "numenter":
                key = Key.NumpadEnter;
                return true;
        }

        return false;
    }
}