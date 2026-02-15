using Brine2D.Events;
using Brine2D.Input;
using Brine2D.SDL.Common;
using Brine2D.SDL.Common.Events;
using Microsoft.Extensions.Logging;
using System.Numerics;
using System.Text;
using Brine2D.SDL.Events;

namespace Brine2D.SDL.Input;

/// <summary>
/// SDL3 implementation of the input service.
/// Subscribes to internal SDL events and maintains input state.
/// Publishes high-level, framework-agnostic input events for user code.
/// </summary>
public class SDL3InputContext : IInputContext, IDisposable
{
    private readonly ILogger<SDL3InputContext> _logger;
    private readonly EventBus _publicEventBus;
    private readonly EventBus _internalEventBus;
    private readonly ISDL3WindowProvider? _windowProvider;

    private readonly HashSet<Key> _keysDown = new();
    private readonly HashSet<Key> _keysPressed = new();
    private readonly HashSet<Key> _keysReleased = new();
    private readonly HashSet<Key> _keysPressedThisFrame = new();

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

    private bool _textInputActive;
    private readonly StringBuilder _textInputBuffer = new();
    private bool _backspacePressedThisFrame;
    private bool _returnPressedThisFrame;
    private bool _deletePressedThisFrame;

    public Vector2 MousePosition => _mousePosition;
    public Vector2 MouseDelta => _mouseDelta;
    public float ScrollWheelDelta => _scrollWheelDelta;
    public bool IsTextInputActive => _textInputActive;

    public SDL3InputContext(
        ILogger<SDL3InputContext> logger,
        EventBus publicEventBus,
        EventBus internalEventBus,
        ISDL3WindowProvider? windowProvider = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _publicEventBus = publicEventBus ?? throw new ArgumentNullException(nameof(publicEventBus));
        _internalEventBus = internalEventBus ?? throw new ArgumentNullException(nameof(internalEventBus));
        _windowProvider = windowProvider;

        SubscribeToInternalEvents();
        EnumerateGamepads();
    }

    private void SubscribeToInternalEvents()
    {
        _internalEventBus.Subscribe<SDL3KeyDownEvent>(OnKeyDown);
        _internalEventBus.Subscribe<SDL3KeyUpEvent>(OnKeyUp);
        _internalEventBus.Subscribe<SDL3MouseButtonDownEvent>(OnMouseButtonDown);
        _internalEventBus.Subscribe<SDL3MouseButtonUpEvent>(OnMouseButtonUp);
        _internalEventBus.Subscribe<SDL3MouseWheelEvent>(OnMouseWheel);
        _internalEventBus.Subscribe<SDL3MouseMotionEvent>(OnMouseMotion);
        _internalEventBus.Subscribe<SDL3TextInputEvent>(OnTextInput);
        _internalEventBus.Subscribe<SDL3GamepadButtonDownEvent>(OnGamepadButtonDown);
        _internalEventBus.Subscribe<SDL3GamepadButtonUpEvent>(OnGamepadButtonUp);
        _internalEventBus.Subscribe<SDL3GamepadAddedEvent>(OnGamepadAdded);
        _internalEventBus.Subscribe<SDL3GamepadRemovedEvent>(OnGamepadRemoved);
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

    public void Update()
    {
        // Clear per-frame state
        _keysPressed.Clear();
        _keysReleased.Clear();
        _mouseButtonsPressed.Clear();
        _mouseButtonsReleased.Clear();
        _mouseDelta = Vector2.Zero;
        _scrollWheelDelta = 0f;
        _textInputBuffer.Clear();
        _backspacePressedThisFrame = false;
        _returnPressedThisFrame = false;
        _deletePressedThisFrame = false;
        _keysPressedThisFrame.Clear();

        foreach (var set in _gamepadButtonsPressed.Values)
            set.Clear();
        foreach (var set in _gamepadButtonsReleased.Values)
            set.Clear();

        // Update mouse position directly (not event-based for polling queries)
        SDL3.SDL.GetMouseState(out float mx, out float my);
        var newMousePos = new Vector2(mx, my);
        _mouseDelta = newMousePos - _mousePosition;
        _mousePosition = newMousePos;
    }

    // ===== Internal Event Handlers =====

    private void OnKeyDown(SDL3KeyDownEvent evt)
    {
        var key = MapSDLKeyToKeys(evt.KeyEvent.Key);
        if (key == Key.Unknown) return;

        bool isRepeat = evt.KeyEvent.Repeat;

        _keysDown.Add(key);

        if (!isRepeat)
        {
            _keysPressed.Add(key);
            _publicEventBus.Publish(new KeyPressedEvent(key, IsRepeat: false));
        }

        if (_textInputActive)
        {
            if (key == Key.Backspace && !_backspacePressedThisFrame)
            {
                _backspacePressedThisFrame = true;
            }
            if (key == Key.Enter && !_returnPressedThisFrame)
            {
                _returnPressedThisFrame = true;
            }
            if (key == Key.Delete && !_deletePressedThisFrame)
            {
                _deletePressedThisFrame = true;
            }
        }
    }

    private void OnKeyUp(SDL3KeyUpEvent evt)
    {
        var key = MapSDLKeyToKeys(evt.KeyEvent.Key);
        if (key == Key.Unknown) return;

        _keysDown.Remove(key);
        _keysReleased.Add(key);

        _publicEventBus.Publish(new KeyReleasedEvent(key));
    }

    private void OnMouseButtonDown(SDL3MouseButtonDownEvent evt)
    {
        var button = MapSDLMouseButton(evt.ButtonEvent.Button);
        if (button == MouseButton.Unknown) return;

        _mouseButtonsDown.Add(button);
        _mouseButtonsPressed.Add(button);

        _publicEventBus.Publish(new MouseButtonPressedEvent(button, _mousePosition));
    }

    private void OnMouseButtonUp(SDL3MouseButtonUpEvent evt)
    {
        var button = MapSDLMouseButton(evt.ButtonEvent.Button);
        if (button == MouseButton.Unknown) return;

        _mouseButtonsDown.Remove(button);
        _mouseButtonsReleased.Add(button);

        _publicEventBus.Publish(new MouseButtonReleasedEvent(button, _mousePosition));
    }

    private void OnMouseWheel(SDL3MouseWheelEvent evt)
    {
        _scrollWheelDelta = evt.WheelEvent.Y;
        _publicEventBus.Publish(new MouseScrolledEvent(evt.WheelEvent.X, evt.WheelEvent.Y));
    }

    private void OnMouseMotion(SDL3MouseMotionEvent evt)
    {
        // Position is updated in Update() method for polling
        var delta = new Vector2(evt.MotionEvent.XRel, evt.MotionEvent.YRel);
        _publicEventBus.Publish(new MouseMovedEvent(_mousePosition, delta));
    }

    private void OnTextInput(SDL3TextInputEvent evt)
    {
        if (_textInputActive && !string.IsNullOrEmpty(SDL3.SDL.PointerToString(evt.TextEvent.Text)))
        {
            _textInputBuffer.Append(evt.TextEvent.Text);
            _logger.LogTrace("Text input: {Text}", evt.TextEvent.Text);
        }
    }

    private void OnGamepadButtonDown(SDL3GamepadButtonDownEvent evt)
    {
        var deviceId = evt.ButtonEvent.Which;
        var button = MapSDLGamepadButton((SDL3.SDL.GamepadButton)evt.ButtonEvent.Button);

        if (!_gamepadButtonsDown.ContainsKey(deviceId))
        {
            _logger.LogWarning("Received button event for unknown gamepad device {DeviceId}", deviceId);
            return;
        }

        _gamepadButtonsDown[deviceId].Add(button);
        _gamepadButtonsPressed[deviceId].Add(button);

        _publicEventBus.Publish(new GamepadButtonPressedEvent(button, (int)deviceId));
        _logger.LogTrace("Gamepad {DeviceId} button {Button} pressed", deviceId, button);
    }

    private void OnGamepadButtonUp(SDL3GamepadButtonUpEvent evt)
    {
        var deviceId = evt.ButtonEvent.Which;
        var button = MapSDLGamepadButton((SDL3.SDL.GamepadButton)evt.ButtonEvent.Button);

        if (!_gamepadButtonsDown.ContainsKey(deviceId))
            return;

        _gamepadButtonsDown[deviceId].Remove(button);
        _gamepadButtonsReleased[deviceId].Add(button);

        _publicEventBus.Publish(new GamepadButtonReleasedEvent(button, (int)deviceId));
    }

    private void OnGamepadAdded(SDL3GamepadAddedEvent evt)
    {
        var deviceId = evt.DeviceEvent.Which;
        var gamepad = SDL3.SDL.OpenGamepad(deviceId);

        if (gamepad != IntPtr.Zero)
        {
            _gamepads[deviceId] = gamepad;
            _gamepadButtonsDown[deviceId] = new HashSet<GamepadButton>();
            _gamepadButtonsPressed[deviceId] = new HashSet<GamepadButton>();
            _gamepadButtonsReleased[deviceId] = new HashSet<GamepadButton>();

            var name = SDL3.SDL.GetGamepadName(gamepad);
            _logger.LogInformation("Gamepad {DeviceId} connected: {Name}", deviceId, name);

            _publicEventBus.Publish(new GamepadConnectedEvent((int)deviceId, name ?? "Unknown"));
        }
    }

    private void OnGamepadRemoved(SDL3GamepadRemovedEvent evt)
    {
        var deviceId = evt.DeviceEvent.Which;

        if (_gamepads.TryGetValue(deviceId, out var gamepad))
        {
            SDL3.SDL.CloseGamepad(gamepad);
            _gamepads.Remove(deviceId);
            _gamepadButtonsDown.Remove(deviceId);
            _gamepadButtonsPressed.Remove(deviceId);
            _gamepadButtonsReleased.Remove(deviceId);

            _logger.LogInformation("Gamepad {DeviceId} disconnected", deviceId);

            _publicEventBus.Publish(new GamepadDisconnectedEvent((int)deviceId));
        }
    }

    // ===== Input Query Methods =====

    public bool IsKeyDown(Key key) => _keysDown.Contains(key);
    public bool IsKeyPressed(Key key) => _keysPressed.Contains(key);
    public bool IsKeyReleased(Key key) => _keysReleased.Contains(key);

    public bool IsMouseButtonDown(MouseButton button) => _mouseButtonsDown.Contains(button);
    public bool IsMouseButtonPressed(MouseButton button) => _mouseButtonsPressed.Contains(button);
    public bool IsMouseButtonReleased(MouseButton button) => _mouseButtonsReleased.Contains(button);

    public bool IsGamepadConnected(int gamepadIndex = 0)
    {
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

    public void StartTextInput()
    {
        if (!_textInputActive)
        {
            var window = _windowProvider?.Window ?? IntPtr.Zero;
            SDL3.SDL.StartTextInput(window);
            _textInputActive = true;
            _logger.LogDebug("Text input started");
        }
    }

    public void StopTextInput()
    {
        if (_textInputActive)
        {
            var window = _windowProvider?.Window ?? IntPtr.Zero;
            SDL3.SDL.StopTextInput(window);
            _textInputActive = false;
            _logger.LogDebug("Text input stopped");
        }
    }

    public string GetTextInput() => _textInputBuffer.ToString();

    public bool IsBackspacePressed() => _backspacePressedThisFrame;
    public bool IsReturnPressed() => _returnPressedThisFrame;
    public bool IsDeletePressed() => _deletePressedThisFrame;

    // ===== Helper Methods =====

    private uint? GetDeviceIdByIndex(int index)
    {
        if (index < 0 || index >= _gamepads.Count)
            return null;
        return _gamepads.Keys.ElementAtOrDefault(index);
    }

    private static Key MapSDLKeyToKeys(SDL3.SDL.Keycode key)
    {
        return key switch
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
            SDL3.SDL.Keycode.F1 => Key.F1,
            SDL3.SDL.Keycode.F2 => Key.F2,
            SDL3.SDL.Keycode.F3 => Key.F3,
            SDL3.SDL.Keycode.F4 => Key.F4,
            SDL3.SDL.Keycode.F5 => Key.F5,
            SDL3.SDL.Keycode.F6 => Key.F6,
            SDL3.SDL.Keycode.F7 => Key.F7,
            SDL3.SDL.Keycode.F8 => Key.F8,
            SDL3.SDL.Keycode.F9 => Key.F9,
            SDL3.SDL.Keycode.F10 => Key.F10,
            SDL3.SDL.Keycode.F11 => Key.F11,
            SDL3.SDL.Keycode.F12 => Key.F12,

            // Arrow keys
            SDL3.SDL.Keycode.Left => Key.Left,
            SDL3.SDL.Keycode.Right => Key.Right,
            SDL3.SDL.Keycode.Up => Key.Up,
            SDL3.SDL.Keycode.Down => Key.Down,

            // Special keys
            SDL3.SDL.Keycode.Space => Key.Space,
            SDL3.SDL.Keycode.Return => Key.Enter,
            SDL3.SDL.Keycode.Escape => Key.Escape,
            SDL3.SDL.Keycode.Tab => Key.Tab,
            SDL3.SDL.Keycode.Backspace => Key.Backspace,
            SDL3.SDL.Keycode.Delete => Key.Delete,

            // Modifiers
            SDL3.SDL.Keycode.LShift => Key.LeftShift,
            SDL3.SDL.Keycode.RShift => Key.RightShift,
            SDL3.SDL.Keycode.LCtrl => Key.LeftControl,
            SDL3.SDL.Keycode.RCtrl => Key.RightControl,
            SDL3.SDL.Keycode.LAlt => Key.LeftAlt,
            SDL3.SDL.Keycode.RAlt => Key.RightAlt,
            SDL3.SDL.Keycode.LGUI => Key.LeftSuper,
            SDL3.SDL.Keycode.RGUI => Key.RightSuper,
            SDL3.SDL.Keycode.Capslock => Key.CapsLock,
            SDL3.SDL.Keycode.NumLockClear => Key.NumLock,
            SDL3.SDL.Keycode.ScrollLock => Key.ScrollLock,

            // Editing keys
            SDL3.SDL.Keycode.Insert => Key.Insert,
            SDL3.SDL.Keycode.Home => Key.Home,
            SDL3.SDL.Keycode.End => Key.End,
            SDL3.SDL.Keycode.Pageup => Key.PageUp,
            SDL3.SDL.Keycode.Pagedown => Key.PageDown,

            // Punctuation
            SDL3.SDL.Keycode.Minus => Key.Minus,
            SDL3.SDL.Keycode.Equals => Key.Equals,
            SDL3.SDL.Keycode.LeftBracket => Key.LeftBracket,
            SDL3.SDL.Keycode.RightBracket => Key.RightBracket,
            SDL3.SDL.Keycode.Backslash => Key.Backslash,
            SDL3.SDL.Keycode.Semicolon => Key.Semicolon,
            SDL3.SDL.Keycode.Apostrophe => Key.Apostrophe,
            SDL3.SDL.Keycode.Grave => Key.Grave,
            SDL3.SDL.Keycode.Comma => Key.Comma,
            SDL3.SDL.Keycode.Period => Key.Period,
            SDL3.SDL.Keycode.Slash => Key.Slash,

            // Numpad
            SDL3.SDL.Keycode.Kp0 => Key.Numpad0,
            SDL3.SDL.Keycode.Kp1 => Key.Numpad1,
            SDL3.SDL.Keycode.Kp2 => Key.Numpad2,
            SDL3.SDL.Keycode.Kp3 => Key.Numpad3,
            SDL3.SDL.Keycode.Kp4 => Key.Numpad4,
            SDL3.SDL.Keycode.Kp5 => Key.Numpad5,
            SDL3.SDL.Keycode.Kp6 => Key.Numpad6,
            SDL3.SDL.Keycode.Kp7 => Key.Numpad7,
            SDL3.SDL.Keycode.Kp8 => Key.Numpad8,
            SDL3.SDL.Keycode.Kp9 => Key.Numpad9,
            SDL3.SDL.Keycode.KpEnter => Key.NumpadEnter,
            SDL3.SDL.Keycode.KpPlus => Key.NumpadPlus,
            SDL3.SDL.Keycode.KpMinus => Key.NumpadMinus,
            SDL3.SDL.Keycode.KpMultiply => Key.NumpadMultiply,
            SDL3.SDL.Keycode.KpDivide => Key.NumpadDivide,
            SDL3.SDL.Keycode.KpPeriod => Key.NumpadPeriod,

            _ => Key.Unknown
        };
    }

    private static MouseButton MapSDLMouseButton(byte button)
    {
        return button switch
        {
            1 => MouseButton.Left,
            2 => MouseButton.Middle,
            3 => MouseButton.Right,
            4 => MouseButton.X1,
            5 => MouseButton.X2,
            _ => MouseButton.Unknown
        };
    }

    private static GamepadButton MapSDLGamepadButton(SDL3.SDL.GamepadButton button)
    {
        return button switch
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

    private static SDL3.SDL.GamepadAxis MapGamepadAxisToSDL(GamepadAxis axis)
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

    public void Dispose()
    {
        foreach (var gamepad in _gamepads.Values)
        {
            SDL3.SDL.CloseGamepad(gamepad);
        }
        _gamepads.Clear();
    }
}