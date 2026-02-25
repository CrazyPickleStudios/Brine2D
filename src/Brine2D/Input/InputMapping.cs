namespace Brine2D.Input;

/// <summary>
/// Pure, stateless mapping tables between SDL3 input types and Brine2D input types.
/// No dependencies — fully unit testable without SDL3 initialised.
/// </summary>
internal static class InputMapping
{
    internal static Key ToKey(SDL3.SDL.Keycode key) => key switch
    {
        // Letters
        SDL3.SDL.Keycode.A => Key.A,
        SDL3.SDL.Keycode.B => Key.B,
        SDL3.SDL.Keycode.C => Key.C,
        SDL3.SDL.Keycode.D => Key.D,
        SDL3.SDL.Keycode.E => Key.E,
        SDL3.SDL.Keycode.F => Key.F,
        SDL3.SDL.Keycode.G => Key.G,
        SDL3.SDL.Keycode.H => Key.H,
        SDL3.SDL.Keycode.I => Key.I,
        SDL3.SDL.Keycode.J => Key.J,
        SDL3.SDL.Keycode.K => Key.K,
        SDL3.SDL.Keycode.L => Key.L,
        SDL3.SDL.Keycode.M => Key.M,
        SDL3.SDL.Keycode.N => Key.N,
        SDL3.SDL.Keycode.O => Key.O,
        SDL3.SDL.Keycode.P => Key.P,
        SDL3.SDL.Keycode.Q => Key.Q,
        SDL3.SDL.Keycode.R => Key.R,
        SDL3.SDL.Keycode.S => Key.S,
        SDL3.SDL.Keycode.T => Key.T,
        SDL3.SDL.Keycode.U => Key.U,
        SDL3.SDL.Keycode.V => Key.V,
        SDL3.SDL.Keycode.W => Key.W,
        SDL3.SDL.Keycode.X => Key.X,
        SDL3.SDL.Keycode.Y => Key.Y,
        SDL3.SDL.Keycode.Z => Key.Z,

        // Numbers
        SDL3.SDL.Keycode.Alpha0 => Key.D0,
        SDL3.SDL.Keycode.Alpha1 => Key.D1,
        SDL3.SDL.Keycode.Alpha2 => Key.D2,
        SDL3.SDL.Keycode.Alpha3 => Key.D3,
        SDL3.SDL.Keycode.Alpha4 => Key.D4,
        SDL3.SDL.Keycode.Alpha5 => Key.D5,
        SDL3.SDL.Keycode.Alpha6 => Key.D6,
        SDL3.SDL.Keycode.Alpha7 => Key.D7,
        SDL3.SDL.Keycode.Alpha8 => Key.D8,
        SDL3.SDL.Keycode.Alpha9 => Key.D9,

        // Function keys
        SDL3.SDL.Keycode.F1  => Key.F1,
        SDL3.SDL.Keycode.F2  => Key.F2,
        SDL3.SDL.Keycode.F3  => Key.F3,
        SDL3.SDL.Keycode.F4  => Key.F4,
        SDL3.SDL.Keycode.F5  => Key.F5,
        SDL3.SDL.Keycode.F6  => Key.F6,
        SDL3.SDL.Keycode.F7  => Key.F7,
        SDL3.SDL.Keycode.F8  => Key.F8,
        SDL3.SDL.Keycode.F9  => Key.F9,
        SDL3.SDL.Keycode.F10 => Key.F10,
        SDL3.SDL.Keycode.F11 => Key.F11,
        SDL3.SDL.Keycode.F12 => Key.F12,

        // Arrow keys
        SDL3.SDL.Keycode.Left  => Key.Left,
        SDL3.SDL.Keycode.Right => Key.Right,
        SDL3.SDL.Keycode.Up    => Key.Up,
        SDL3.SDL.Keycode.Down  => Key.Down,

        // Special keys
        SDL3.SDL.Keycode.Space     => Key.Space,
        SDL3.SDL.Keycode.Return    => Key.Enter,
        SDL3.SDL.Keycode.Escape    => Key.Escape,
        SDL3.SDL.Keycode.Tab       => Key.Tab,
        SDL3.SDL.Keycode.Backspace => Key.Backspace,
        SDL3.SDL.Keycode.Delete    => Key.Delete,

        // Modifiers
        SDL3.SDL.Keycode.LShift      => Key.LeftShift,
        SDL3.SDL.Keycode.RShift      => Key.RightShift,
        SDL3.SDL.Keycode.LCtrl       => Key.LeftControl,
        SDL3.SDL.Keycode.RCtrl       => Key.RightControl,
        SDL3.SDL.Keycode.LAlt        => Key.LeftAlt,
        SDL3.SDL.Keycode.RAlt        => Key.RightAlt,
        SDL3.SDL.Keycode.LGUI        => Key.LeftSuper,
        SDL3.SDL.Keycode.RGUI        => Key.RightSuper,
        SDL3.SDL.Keycode.Capslock    => Key.CapsLock,
        SDL3.SDL.Keycode.NumLockClear => Key.NumLock,
        SDL3.SDL.Keycode.ScrollLock  => Key.ScrollLock,

        // Editing keys
        SDL3.SDL.Keycode.Insert   => Key.Insert,
        SDL3.SDL.Keycode.Home     => Key.Home,
        SDL3.SDL.Keycode.End      => Key.End,
        SDL3.SDL.Keycode.Pageup   => Key.PageUp,
        SDL3.SDL.Keycode.Pagedown => Key.PageDown,

        // Punctuation
        SDL3.SDL.Keycode.Minus        => Key.Minus,
        SDL3.SDL.Keycode.Equals       => Key.Equals,
        SDL3.SDL.Keycode.LeftBracket  => Key.LeftBracket,
        SDL3.SDL.Keycode.RightBracket => Key.RightBracket,
        SDL3.SDL.Keycode.Backslash    => Key.Backslash,
        SDL3.SDL.Keycode.Semicolon    => Key.Semicolon,
        SDL3.SDL.Keycode.Apostrophe   => Key.Apostrophe,
        SDL3.SDL.Keycode.Grave        => Key.Grave,
        SDL3.SDL.Keycode.Comma        => Key.Comma,
        SDL3.SDL.Keycode.Period       => Key.Period,
        SDL3.SDL.Keycode.Slash        => Key.Slash,

        // Numpad
        SDL3.SDL.Keycode.Kp0        => Key.Numpad0,
        SDL3.SDL.Keycode.Kp1        => Key.Numpad1,
        SDL3.SDL.Keycode.Kp2        => Key.Numpad2,
        SDL3.SDL.Keycode.Kp3        => Key.Numpad3,
        SDL3.SDL.Keycode.Kp4        => Key.Numpad4,
        SDL3.SDL.Keycode.Kp5        => Key.Numpad5,
        SDL3.SDL.Keycode.Kp6        => Key.Numpad6,
        SDL3.SDL.Keycode.Kp7        => Key.Numpad7,
        SDL3.SDL.Keycode.Kp8        => Key.Numpad8,
        SDL3.SDL.Keycode.Kp9        => Key.Numpad9,
        SDL3.SDL.Keycode.KpEnter    => Key.NumpadEnter,
        SDL3.SDL.Keycode.KpPlus     => Key.NumpadPlus,
        SDL3.SDL.Keycode.KpMinus    => Key.NumpadMinus,
        SDL3.SDL.Keycode.KpMultiply => Key.NumpadMultiply,
        SDL3.SDL.Keycode.KpDivide   => Key.NumpadDivide,
        SDL3.SDL.Keycode.KpPeriod   => Key.NumpadPeriod,

        _ => Key.Unknown
    };

    internal static MouseButton ToMouseButton(byte button) => button switch
    {
        1 => MouseButton.Left,
        2 => MouseButton.Middle,
        3 => MouseButton.Right,
        4 => MouseButton.X1,
        5 => MouseButton.X2,
        _ => MouseButton.Unknown
    };

    internal static GamepadButton ToGamepadButton(SDL3.SDL.GamepadButton button) => button switch
    {
        SDL3.SDL.GamepadButton.South         => GamepadButton.A,
        SDL3.SDL.GamepadButton.East          => GamepadButton.B,
        SDL3.SDL.GamepadButton.West          => GamepadButton.X,
        SDL3.SDL.GamepadButton.North         => GamepadButton.Y,
        SDL3.SDL.GamepadButton.Back          => GamepadButton.Back,
        SDL3.SDL.GamepadButton.Guide         => GamepadButton.Guide,
        SDL3.SDL.GamepadButton.Start         => GamepadButton.Start,
        SDL3.SDL.GamepadButton.LeftStick     => GamepadButton.LeftStick,
        SDL3.SDL.GamepadButton.RightStick    => GamepadButton.RightStick,
        SDL3.SDL.GamepadButton.LeftShoulder  => GamepadButton.LeftShoulder,
        SDL3.SDL.GamepadButton.RightShoulder => GamepadButton.RightShoulder,
        SDL3.SDL.GamepadButton.DPadUp        => GamepadButton.DPadUp,
        SDL3.SDL.GamepadButton.DPadDown      => GamepadButton.DPadDown,
        SDL3.SDL.GamepadButton.DPadLeft      => GamepadButton.DPadLeft,
        SDL3.SDL.GamepadButton.DPadRight     => GamepadButton.DPadRight,
        _                                    => GamepadButton.A
    };

    internal static SDL3.SDL.GamepadAxis ToSDLAxis(GamepadAxis axis) => axis switch
    {
        GamepadAxis.LeftX        => SDL3.SDL.GamepadAxis.LeftX,
        GamepadAxis.LeftY        => SDL3.SDL.GamepadAxis.LeftY,
        GamepadAxis.RightX       => SDL3.SDL.GamepadAxis.RightX,
        GamepadAxis.RightY       => SDL3.SDL.GamepadAxis.RightY,
        GamepadAxis.LeftTrigger  => SDL3.SDL.GamepadAxis.LeftTrigger,
        GamepadAxis.RightTrigger => SDL3.SDL.GamepadAxis.RightTrigger,
        _                        => SDL3.SDL.GamepadAxis.LeftX
    };
}