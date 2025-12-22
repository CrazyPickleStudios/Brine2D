using Brine2D.Input;
using Microsoft.Extensions.Logging;
using System.Numerics;

namespace Brine2D.Input.SDL;

/// <summary>
/// SDL3 implementation of the input service.
/// </summary>
public class SDL3InputService : IInputService
{
    private readonly ILogger<SDL3InputService> _logger;

    private readonly HashSet<Keys> _keysDown = new();
    private readonly HashSet<Keys> _keysPressed = new();
    private readonly HashSet<Keys> _keysReleased = new();

    private Vector2 _mousePosition;
    private Vector2 _mouseDelta;
    private float _scrollWheelDelta;
    private readonly HashSet<MouseButton> _mouseButtonsDown = new();
    private readonly HashSet<MouseButton> _mouseButtonsPressed = new();
    private readonly HashSet<MouseButton> _mouseButtonsReleased = new();

    private readonly Dictionary<uint, nint> _gamepads = new();
    private readonly Dictionary<uint, HashSet<GamepadButton>> _gamepadButtonsDown = new();
    private readonly Dictionary<uint, HashSet<GamepadButton>> _gamepadButtonsPressed = new();
    private readonly Dictionary<uint, HashSet<GamepadButton>> _gamepadButtonsReleased = new();

    public Vector2 MousePosition => _mousePosition;
    public Vector2 MouseDelta => _mouseDelta;
    public float ScrollWheelDelta => _scrollWheelDelta;
    public bool IsQuitRequested { get; private set; }

    public SDL3InputService(ILogger<SDL3InputService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        
        EnumerateGamepads();
    }

    private void EnumerateGamepads()
    {
        var gamepadIds = SDL3.SDL.GetGamepads(out int numGamepads);
        
        if (gamepadIds != null && numGamepads > 0)
        {
            _logger.LogInformation("Found {Count} gamepad(s)", numGamepads);
            
            for (int i = 0; i < numGamepads; i++)
            {
                var deviceId = gamepadIds[i];
                var gamepad = SDL3.SDL.OpenGamepad(deviceId);
                
                if (gamepad != IntPtr.Zero)
                {
                    _gamepads[deviceId] = gamepad;
                    _gamepadButtonsDown[deviceId] = new HashSet<GamepadButton>();
                    _gamepadButtonsPressed[deviceId] = new HashSet<GamepadButton>();
                    _gamepadButtonsReleased[deviceId] = new HashSet<GamepadButton>();
                    
                    var name = SDL3.SDL.GetGamepadName(gamepad);
                    _logger.LogInformation("Gamepad {DeviceId} opened: {Name}", deviceId, name);
                }
                else
                {
                    _logger.LogWarning("Failed to open gamepad {DeviceId}", deviceId);
                }
            }
        }
        else
        {
            _logger.LogInformation("No gamepads found");
        }
    }

    /// <summary>
    /// Called at the beginning of each frame to poll SDL events and update input state.
    /// </summary>
    public void Update()
    {
        // Clear per-frame state
        _keysPressed.Clear();
        _keysReleased.Clear();
        _mouseButtonsPressed.Clear();
        _mouseButtonsReleased.Clear();
        _mouseDelta = Vector2.Zero;
        _scrollWheelDelta = 0;

        foreach (var set in _gamepadButtonsPressed.Values)
            set.Clear();
        foreach (var set in _gamepadButtonsReleased.Values)
            set.Clear();

        // Poll all SDL events
        while (SDL3.SDL.PollEvent(out var evt))
        {
            ProcessEvent(evt);
        }

        // Update mouse position
        SDL3.SDL.GetMouseState(out float mx, out float my);
        var newMousePos = new Vector2(mx, my);
        _mouseDelta = newMousePos - _mousePosition;
        _mousePosition = newMousePos;
    }

    private void ProcessEvent(SDL3.SDL.Event evt)
    {
        switch ((SDL3.SDL.EventType)evt.Type)
        {
            case SDL3.SDL.EventType.Quit:
                IsQuitRequested = true;
                _logger.LogInformation("Quit event received");
                break;
            case SDL3.SDL.EventType.KeyDown:
                HandleKeyDown(evt.Key);
                break;
            case SDL3.SDL.EventType.KeyUp:
                HandleKeyUp(evt.Key);
                break;
            case SDL3.SDL.EventType.MouseButtonDown:
                HandleMouseButtonDown(evt.Button);
                break;
            case SDL3.SDL.EventType.MouseButtonUp:
                HandleMouseButtonUp(evt.Button);
                break;
            case SDL3.SDL.EventType.MouseWheel:
                HandleMouseWheel(evt.Wheel);
                break;
            case SDL3.SDL.EventType.GamepadButtonDown:
                HandleGamepadButtonDown(evt.GButton);
                break;
            case SDL3.SDL.EventType.GamepadButtonUp:
                HandleGamepadButtonUp(evt.GButton);
                break;
            case SDL3.SDL.EventType.GamepadAdded:
                HandleGamepadAdded(evt.GDevice);
                break;
            case SDL3.SDL.EventType.GamepadRemoved:
                HandleGamepadRemoved(evt.GDevice);
                break;
        }
    }

    private void HandleKeyDown(SDL3.SDL.KeyboardEvent keyEvent)
    {
        var key = MapSDLKeyToKeys(keyEvent.Key);
        if (key != Keys.Unknown && !_keysDown.Contains(key))
        {
            _keysDown.Add(key);
            _keysPressed.Add(key);
        }
    }

    private void HandleKeyUp(SDL3.SDL.KeyboardEvent keyEvent)
    {
        var key = MapSDLKeyToKeys(keyEvent.Key);
        if (key != Keys.Unknown)
        {
            _keysDown.Remove(key);
            _keysReleased.Add(key);
        }
    }

    private void HandleMouseButtonDown(SDL3.SDL.MouseButtonEvent mouseEvent)
    {
        var button = MapSDLMouseButton(mouseEvent.Button);
        if (!_mouseButtonsDown.Contains(button))
        {
            _mouseButtonsDown.Add(button);
            _mouseButtonsPressed.Add(button);
        }
    }

    private void HandleMouseButtonUp(SDL3.SDL.MouseButtonEvent mouseEvent)
    {
        var button = MapSDLMouseButton(mouseEvent.Button);
        _mouseButtonsDown.Remove(button);
        _mouseButtonsReleased.Add(button);
    }

    private void HandleMouseWheel(SDL3.SDL.MouseWheelEvent wheelEvent)
    {
        _scrollWheelDelta = wheelEvent.Y;
    }

    private void HandleGamepadButtonDown(SDL3.SDL.GamepadButtonEvent buttonEvent)
    {
        var deviceId = buttonEvent.Which;
        
        // Ensure we have dictionaries for this device
        if (!_gamepadButtonsDown.ContainsKey(deviceId))
        {
            _logger.LogWarning("Received button event for unknown gamepad device {DeviceId}", deviceId);
            return;
        }
        
        var button = MapSDLGamepadButton((SDL3.SDL.GamepadButton)buttonEvent.Button);

        if (!_gamepadButtonsDown[deviceId].Contains(button))
        {
            _gamepadButtonsDown[deviceId].Add(button);
            _gamepadButtonsPressed[deviceId].Add(button);
            _logger.LogTrace("Gamepad {DeviceId} button {Button} pressed", deviceId, button);
        }
    }

    private void HandleGamepadButtonUp(SDL3.SDL.GamepadButtonEvent buttonEvent)
    {
        var deviceId = buttonEvent.Which;
        
        if (!_gamepadButtonsDown.ContainsKey(deviceId))
            return;
        
        var button = MapSDLGamepadButton((SDL3.SDL.GamepadButton)buttonEvent.Button);

        _gamepadButtonsDown[deviceId].Remove(button);
        _gamepadButtonsReleased[deviceId].Add(button);
    }

    private void HandleGamepadAdded(SDL3.SDL.GamepadDeviceEvent deviceEvent)
    {
        var deviceId = deviceEvent.Which;
        var gamepad = SDL3.SDL.OpenGamepad(deviceId);

        if (gamepad != IntPtr.Zero)
        {
            _gamepads[deviceId] = gamepad;
            _gamepadButtonsDown[deviceId] = new HashSet<GamepadButton>();
            _gamepadButtonsPressed[deviceId] = new HashSet<GamepadButton>();
            _gamepadButtonsReleased[deviceId] = new HashSet<GamepadButton>();
            
            var name = SDL3.SDL.GetGamepadName(gamepad);
            _logger.LogInformation("Gamepad {DeviceId} connected: {Name}", deviceId, name);
        }
    }

    private void HandleGamepadRemoved(SDL3.SDL.GamepadDeviceEvent deviceEvent)
    {
        var deviceId = deviceEvent.Which;

        if (_gamepads.TryGetValue(deviceId, out var gamepad))
        {
            SDL3.SDL.CloseGamepad(gamepad);
            _gamepads.Remove(deviceId);
            _gamepadButtonsDown.Remove(deviceId);
            _gamepadButtonsPressed.Remove(deviceId);
            _gamepadButtonsReleased.Remove(deviceId);
            _logger.LogInformation("Gamepad {DeviceId} disconnected", deviceId);
        }
    }
    
    public bool IsKeyDown(Keys key) => _keysDown.Contains(key);
    public bool IsKeyPressed(Keys key) => _keysPressed.Contains(key);
    public bool IsKeyReleased(Keys key) => _keysReleased.Contains(key);
    
    public bool IsMouseButtonDown(MouseButton button) => _mouseButtonsDown.Contains(button);
    public bool IsMouseButtonPressed(MouseButton button) => _mouseButtonsPressed.Contains(button);
    public bool IsMouseButtonReleased(MouseButton button) => _mouseButtonsReleased.Contains(button);
    
    public bool IsGamepadConnected(int gamepadIndex = 0)
    {
        // Use the first connected gamepad for index 0
        return _gamepads.Count > gamepadIndex;
    }

    public bool IsGamepadButtonDown(GamepadButton button, int gamepadIndex = 0)
    {
        var deviceId = GetDeviceIdByIndex(gamepadIndex);
        if (deviceId == null) return false;
        
        return _gamepadButtonsDown.TryGetValue(deviceId.Value, out var buttons) && buttons.Contains(button);
    }

    public bool IsGamepadButtonPressed(GamepadButton button, int gamepadIndex = 0)
    {
        var deviceId = GetDeviceIdByIndex(gamepadIndex);
        if (deviceId == null) return false;
        
        return _gamepadButtonsPressed.TryGetValue(deviceId.Value, out var buttons) && buttons.Contains(button);
    }

    public bool IsGamepadButtonReleased(GamepadButton button, int gamepadIndex = 0)
    {
        var deviceId = GetDeviceIdByIndex(gamepadIndex);
        if (deviceId == null) return false;
        
        return _gamepadButtonsReleased.TryGetValue(deviceId.Value, out var buttons) && buttons.Contains(button);
    }

    public float GetGamepadAxis(GamepadAxis axis, int gamepadIndex = 0)
    {
        var deviceId = GetDeviceIdByIndex(gamepadIndex);
        if (deviceId == null) return 0f;
        
        if (!_gamepads.TryGetValue(deviceId.Value, out var gamepad))
            return 0f;

        var sdlAxis = MapGamepadAxisToSDL(axis);
        var value = SDL3.SDL.GetGamepadAxis(gamepad, sdlAxis);

        // Normalize from -32768 to 32767 range to -1.0 to 1.0
        return value / 32767f;
    }

    public Vector2 GetGamepadLeftStick(int gamepadIndex = 0)
    {
        return new Vector2(
            GetGamepadAxis(GamepadAxis.LeftX, gamepadIndex),
            GetGamepadAxis(GamepadAxis.LeftY, gamepadIndex)
        );
    }

    public Vector2 GetGamepadRightStick(int gamepadIndex = 0)
    {
        return new Vector2(
            GetGamepadAxis(GamepadAxis.RightX, gamepadIndex),
            GetGamepadAxis(GamepadAxis.RightY, gamepadIndex)
        );
    }

    private uint? GetDeviceIdByIndex(int index)
    {
        if (index < 0 || index >= _gamepads.Count)
            return null;
        
        return _gamepads.Keys.ElementAtOrDefault(index);
    }
    
    private Keys MapSDLKeyToKeys(SDL3.SDL.Keycode sdlKey)
    {
        return sdlKey switch
        {
            // Letters
            SDL3.SDL.Keycode.A => Keys.A,
            SDL3.SDL.Keycode.B => Keys.B,
            SDL3.SDL.Keycode.C => Keys.C,
            SDL3.SDL.Keycode.D => Keys.D,
            SDL3.SDL.Keycode.E => Keys.E,
            SDL3.SDL.Keycode.F => Keys.F,
            SDL3.SDL.Keycode.G => Keys.G,
            SDL3.SDL.Keycode.H => Keys.H,
            SDL3.SDL.Keycode.I => Keys.I,
            SDL3.SDL.Keycode.J => Keys.J,
            SDL3.SDL.Keycode.K => Keys.K,
            SDL3.SDL.Keycode.L => Keys.L,
            SDL3.SDL.Keycode.M => Keys.M,
            SDL3.SDL.Keycode.N => Keys.N,
            SDL3.SDL.Keycode.O => Keys.O,
            SDL3.SDL.Keycode.P => Keys.P,
            SDL3.SDL.Keycode.Q => Keys.Q,
            SDL3.SDL.Keycode.R => Keys.R,
            SDL3.SDL.Keycode.S => Keys.S,
            SDL3.SDL.Keycode.T => Keys.T,
            SDL3.SDL.Keycode.U => Keys.U,
            SDL3.SDL.Keycode.V => Keys.V,
            SDL3.SDL.Keycode.W => Keys.W,
            SDL3.SDL.Keycode.X => Keys.X,
            SDL3.SDL.Keycode.Y => Keys.Y,
            SDL3.SDL.Keycode.Z => Keys.Z,

            // Numbers
            SDL3.SDL.Keycode.Alpha0 => Keys.D0,
            SDL3.SDL.Keycode.Alpha1 => Keys.D1,
            SDL3.SDL.Keycode.Alpha2 => Keys.D2,
            SDL3.SDL.Keycode.Alpha3 => Keys.D3,
            SDL3.SDL.Keycode.Alpha4 => Keys.D4,
            SDL3.SDL.Keycode.Alpha5 => Keys.D5,
            SDL3.SDL.Keycode.Alpha6 => Keys.D6,
            SDL3.SDL.Keycode.Alpha7 => Keys.D7,
            SDL3.SDL.Keycode.Alpha8 => Keys.D8,
            SDL3.SDL.Keycode.Alpha9 => Keys.D9,

            // Function keys
            SDL3.SDL.Keycode.F1 => Keys.F1,
            SDL3.SDL.Keycode.F2 => Keys.F2,
            SDL3.SDL.Keycode.F3 => Keys.F3,
            SDL3.SDL.Keycode.F4 => Keys.F4,
            SDL3.SDL.Keycode.F5 => Keys.F5,
            SDL3.SDL.Keycode.F6 => Keys.F6,
            SDL3.SDL.Keycode.F7 => Keys.F7,
            SDL3.SDL.Keycode.F8 => Keys.F8,
            SDL3.SDL.Keycode.F9 => Keys.F9,
            SDL3.SDL.Keycode.F10 => Keys.F10,
            SDL3.SDL.Keycode.F11 => Keys.F11,
            SDL3.SDL.Keycode.F12 => Keys.F12,

            // Arrow keys
            SDL3.SDL.Keycode.Left => Keys.Left,
            SDL3.SDL.Keycode.Right => Keys.Right,
            SDL3.SDL.Keycode.Up => Keys.Up,
            SDL3.SDL.Keycode.Down => Keys.Down,

            // Special keys
            SDL3.SDL.Keycode.Space => Keys.Space,
            SDL3.SDL.Keycode.Return => Keys.Enter,
            SDL3.SDL.Keycode.Escape => Keys.Escape,
            SDL3.SDL.Keycode.Tab => Keys.Tab,
            SDL3.SDL.Keycode.Backspace => Keys.Backspace,
            SDL3.SDL.Keycode.Delete => Keys.Delete,
            
            // Modifiers
            SDL3.SDL.Keycode.LShift => Keys.LeftShift,
            SDL3.SDL.Keycode.RShift => Keys.RightShift,
            SDL3.SDL.Keycode.LCtrl => Keys.LeftControl,
            SDL3.SDL.Keycode.RCtrl => Keys.RightControl,
            SDL3.SDL.Keycode.LAlt => Keys.LeftAlt,
            SDL3.SDL.Keycode.RAlt => Keys.RightAlt,
            SDL3.SDL.Keycode.LGUI => Keys.LeftSuper,
            SDL3.SDL.Keycode.RGUI => Keys.RightSuper,
            SDL3.SDL.Keycode.Capslock => Keys.CapsLock,
            SDL3.SDL.Keycode.NumLockClear => Keys.NumLock,
            SDL3.SDL.Keycode.ScrollLock => Keys.ScrollLock,

            // Editing keys
            SDL3.SDL.Keycode.Insert => Keys.Insert,
            SDL3.SDL.Keycode.Home => Keys.Home,
            SDL3.SDL.Keycode.End => Keys.End,
            SDL3.SDL.Keycode.Pageup => Keys.PageUp,
            SDL3.SDL.Keycode.Pagedown => Keys.PageDown,

            // Punctuation
            SDL3.SDL.Keycode.Minus => Keys.Minus,
            SDL3.SDL.Keycode.Equals => Keys.Equals,
            SDL3.SDL.Keycode.LeftBracket => Keys.LeftBracket,
            SDL3.SDL.Keycode.RightBracket => Keys.RightBracket,
            SDL3.SDL.Keycode.Backslash => Keys.Backslash,
            SDL3.SDL.Keycode.Semicolon => Keys.Semicolon,
            SDL3.SDL.Keycode.Apostrophe => Keys.Apostrophe,
            SDL3.SDL.Keycode.Grave => Keys.Grave,
            SDL3.SDL.Keycode.Comma => Keys.Comma,
            SDL3.SDL.Keycode.Period => Keys.Period,
            SDL3.SDL.Keycode.Slash => Keys.Slash,

            // Numpad
            SDL3.SDL.Keycode.Kp0 => Keys.Numpad0,
            SDL3.SDL.Keycode.Kp1 => Keys.Numpad1,
            SDL3.SDL.Keycode.Kp2 => Keys.Numpad2,
            SDL3.SDL.Keycode.Kp3 => Keys.Numpad3,
            SDL3.SDL.Keycode.Kp4 => Keys.Numpad4,
            SDL3.SDL.Keycode.Kp5 => Keys.Numpad5,
            SDL3.SDL.Keycode.Kp6 => Keys.Numpad6,
            SDL3.SDL.Keycode.Kp7 => Keys.Numpad7,
            SDL3.SDL.Keycode.Kp8 => Keys.Numpad8,
            SDL3.SDL.Keycode.Kp9 => Keys.Numpad9,
            SDL3.SDL.Keycode.KpEnter => Keys.NumpadEnter,
            SDL3.SDL.Keycode.KpPlus => Keys.NumpadPlus,
            SDL3.SDL.Keycode.KpMinus => Keys.NumpadMinus,
            SDL3.SDL.Keycode.KpMultiply => Keys.NumpadMultiply,
            SDL3.SDL.Keycode.KpDivide => Keys.NumpadDivide,
            SDL3.SDL.Keycode.KpPeriod => Keys.NumpadPeriod,

            _ => Keys.Unknown
        };
    }

    private MouseButton MapSDLMouseButton(byte sdlButton)
    {
        return sdlButton switch
        {
            1 => MouseButton.Left,
            2 => MouseButton.Middle,
            3 => MouseButton.Right,
            4 => MouseButton.X1,
            5 => MouseButton.X2,
            _ => MouseButton.Left
        };
    }

    private GamepadButton MapSDLGamepadButton(SDL3.SDL.GamepadButton sdlButton)
    {
        return sdlButton switch
        {
            SDL3.SDL.GamepadButton.South => GamepadButton.A,
            SDL3.SDL.GamepadButton.East => GamepadButton.B,
            SDL3.SDL.GamepadButton.West => GamepadButton.X,
            SDL3.SDL.GamepadButton.North => GamepadButton.Y,
            SDL3.SDL.GamepadButton.Back => GamepadButton.Back,
            SDL3.SDL.GamepadButton.Guide => GamepadButton.Guide,
            SDL3.SDL.GamepadButton.Start => GamepadButton.Start,
            SDL3.SDL.GamepadButton.LeftStick => GamepadButton.LeftStick,
            SDL3.SDL.GamepadButton.RightStick => GamepadButton.RightStick,
            SDL3.SDL.GamepadButton.LeftShoulder => GamepadButton.LeftShoulder,
            SDL3.SDL.GamepadButton.RightShoulder => GamepadButton.RightShoulder,
            SDL3.SDL.GamepadButton.DPadUp => GamepadButton.DPadUp,
            SDL3.SDL.GamepadButton.DPadDown => GamepadButton.DPadDown,
            SDL3.SDL.GamepadButton.DPadLeft => GamepadButton.DPadLeft,
            SDL3.SDL.GamepadButton.DPadRight => GamepadButton.DPadRight,
            _ => GamepadButton.A
        };
    }

    private SDL3.SDL.GamepadAxis MapGamepadAxisToSDL(GamepadAxis axis)
    {
        return axis switch
        {
            GamepadAxis.LeftX => SDL3.SDL.GamepadAxis.LeftX,
            GamepadAxis.LeftY => SDL3.SDL.GamepadAxis.LeftY,
            GamepadAxis.RightX => SDL3.SDL.GamepadAxis.RightX,
            GamepadAxis.RightY => SDL3.SDL.GamepadAxis.RightY,
            GamepadAxis.LeftTrigger => SDL3.SDL.GamepadAxis.LeftTrigger,
            GamepadAxis.RightTrigger => SDL3.SDL.GamepadAxis.RightTrigger,
            _ => SDL3.SDL.GamepadAxis.LeftX
        };
    }
}