using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using System.Text;
using Brine2D.Common;
using Brine2D.Events;
using Microsoft.Extensions.Logging;

namespace Brine2D.Input;

/// <summary>
/// SDL3 implementation of the input context.
/// Subscribes to internal SDL events and maintains per-frame input state.
/// Publishes high-level, framework-agnostic events for user code.
/// </summary>
/// <remarks>
/// The SDL3-touching members (<see cref="EnumerateGamepads"/>, <see cref="PollMousePosition"/>,
/// <see cref="GetGamepadAxis"/>, <see cref="StartTextInput"/>, <see cref="StopTextInput"/>,
/// gamepad connect/disconnect handlers, and <see cref="Dispose"/>) are excluded from coverage
/// as they require a live SDL3 context. All remaining state-machine logic and
/// <see cref="ClearFrameState"/> are covered by unit tests via <c>InternalsVisibleTo</c>.
/// </remarks>
internal sealed class InputContext : IInputContext, IDisposable
{
    private readonly ILogger<InputContext> _logger;
    private readonly IEventBus _publicEventBus;
    private readonly IEventBus _internalEventBus;
    private readonly ISDL3WindowProvider? _windowProvider;

    private readonly List<IDisposable> _subscriptions = [];

    // ── Keyboard state ───────────────────────────────────────────────────────
    private readonly HashSet<Key> _keysDown           = new();
    private readonly HashSet<Key> _keysPressed        = new();
    private readonly HashSet<Key> _keysReleased       = new();

    // ── Mouse state ──────────────────────────────────────────────────────────
    private Vector2 _mousePosition;
    private Vector2 _mouseDelta;
    private float   _scrollWheelDelta;
    private readonly HashSet<MouseButton> _mouseButtonsDown     = new();
    private readonly HashSet<MouseButton> _mouseButtonsPressed  = new();
    private readonly HashSet<MouseButton> _mouseButtonsReleased = new();

    // ── Gamepad state ────────────────────────────────────────────────────────
    private readonly Dictionary<uint, nint>               _gamepads              = new();
    private readonly Dictionary<uint, HashSet<GamepadButton>> _gamepadButtonsDown     = new();
    private readonly Dictionary<uint, HashSet<GamepadButton>> _gamepadButtonsPressed  = new();
    private readonly Dictionary<uint, HashSet<GamepadButton>> _gamepadButtonsReleased = new();

    // ── Text input state ─────────────────────────────────────────────────────
    private bool            _textInputActive;
    private readonly StringBuilder _textInputBuffer        = new();
    private bool            _backspacePressedThisFrame;
    private bool            _returnPressedThisFrame;
    private bool            _deletePressedThisFrame;

    // ── IInputContext properties ─────────────────────────────────────────────
    public Vector2 MousePosition    => _mousePosition;
    public Vector2 MouseDelta       => _mouseDelta;
    public float   ScrollWheelDelta => _scrollWheelDelta;
    public bool    IsTextInputActive => _textInputActive;

    public InputContext(
        ILogger<InputContext> logger,
        IEventBus publicEventBus,
        IEventBus internalEventBus,
        ISDL3WindowProvider? windowProvider = null)
    {
        _logger           = logger           ?? throw new ArgumentNullException(nameof(logger));
        _publicEventBus   = publicEventBus   ?? throw new ArgumentNullException(nameof(publicEventBus));
        _internalEventBus = internalEventBus ?? throw new ArgumentNullException(nameof(internalEventBus));
        _windowProvider   = windowProvider;

        SubscribeToInternalEvents();
        EnumerateGamepads();
    }

    // ── Lifecycle ────────────────────────────────────────────────────────────

    void IInputContext.Update()
    {
        ClearFrameState();
        PollMousePosition();
    }

    /// <summary>
    /// Clears all per-frame input state. Pure — no SDL3 calls.
    /// Called by <see cref="IInputContext.Update"/> and directly by unit tests.
    /// </summary>
    internal void ClearFrameState()
    {
        _keysPressed.Clear();
        _keysReleased.Clear();
        _mouseButtonsPressed.Clear();
        _mouseButtonsReleased.Clear();
        _mouseDelta           = Vector2.Zero;
        _scrollWheelDelta     = 0f;
        _textInputBuffer.Clear();
        _backspacePressedThisFrame = false;
        _returnPressedThisFrame    = false;
        _deletePressedThisFrame    = false;

        foreach (var set in _gamepadButtonsPressed.Values)
            set.Clear();
        foreach (var set in _gamepadButtonsReleased.Values)
            set.Clear();
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 mouse device.")]
    private void PollMousePosition()
    {
        SDL3.SDL.GetMouseState(out float mx, out float my);
        var newMousePos = new Vector2(mx, my);
        _mouseDelta    = newMousePos - _mousePosition;
        _mousePosition = newMousePos;
    }

    // ── Internal event subscriptions ─────────────────────────────────────────

    private void SubscribeToInternalEvents()
    {
        _subscriptions.Add(_internalEventBus.Subscribe<SDL3KeyDownEvent>(OnKeyDown));
        _subscriptions.Add(_internalEventBus.Subscribe<SDL3KeyUpEvent>(OnKeyUp));
        _subscriptions.Add(_internalEventBus.Subscribe<SDL3MouseButtonDownEvent>(OnMouseButtonDown));
        _subscriptions.Add(_internalEventBus.Subscribe<SDL3MouseButtonUpEvent>(OnMouseButtonUp));
        _subscriptions.Add(_internalEventBus.Subscribe<SDL3MouseWheelEvent>(OnMouseWheel));
        _subscriptions.Add(_internalEventBus.Subscribe<SDL3MouseMotionEvent>(OnMouseMotion));
        _subscriptions.Add(_internalEventBus.Subscribe<SDL3TextInputEvent>(OnTextInput));
        _subscriptions.Add(_internalEventBus.Subscribe<SDL3GamepadButtonDownEvent>(OnGamepadButtonDown));
        _subscriptions.Add(_internalEventBus.Subscribe<SDL3GamepadButtonUpEvent>(OnGamepadButtonUp));
        _subscriptions.Add(_internalEventBus.Subscribe<SDL3GamepadAddedEvent>(OnGamepadAdded));
        _subscriptions.Add(_internalEventBus.Subscribe<SDL3GamepadRemovedEvent>(OnGamepadRemoved));
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 gamepad subsystem.")]
    private void EnumerateGamepads()
    {
        var gamepadIds = SDL3.SDL.GetGamepads(out int numGamepads);

        if (gamepadIds != null && numGamepads > 0)
        {
            _logger.LogInformation("Found {Count} gamepad(s)", numGamepads);

            for (int i = 0; i < numGamepads; i++)
            {
                var deviceId = gamepadIds[i];
                var gamepad  = SDL3.SDL.OpenGamepad(deviceId);

                if (gamepad != IntPtr.Zero)
                {
                    _gamepads[deviceId]              = gamepad;
                    _gamepadButtonsDown[deviceId]     = new HashSet<GamepadButton>();
                    _gamepadButtonsPressed[deviceId]  = new HashSet<GamepadButton>();
                    _gamepadButtonsReleased[deviceId] = new HashSet<GamepadButton>();

                    _logger.LogInformation("Gamepad {DeviceId} opened: {Name}",
                        deviceId, SDL3.SDL.GetGamepadName(gamepad));
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

    // ── Internal event handlers ───────────────────────────────────────────────

    private void OnKeyDown(SDL3KeyDownEvent evt)
    {
        var key = InputMapping.ToKey(evt.KeyEvent.Key);
        if (key == Key.Unknown) return;

        _keysDown.Add(key);

        if (!evt.KeyEvent.Repeat)
        {
            _keysPressed.Add(key);
            _publicEventBus.Publish(new KeyPressedEvent(key, IsRepeat: false));
        }

        if (_textInputActive)
        {
            if (key == Key.Backspace && !_backspacePressedThisFrame) _backspacePressedThisFrame = true;
            if (key == Key.Enter     && !_returnPressedThisFrame)    _returnPressedThisFrame    = true;
            if (key == Key.Delete    && !_deletePressedThisFrame)    _deletePressedThisFrame    = true;
        }
    }

    private void OnKeyUp(SDL3KeyUpEvent evt)
    {
        var key = InputMapping.ToKey(evt.KeyEvent.Key);
        if (key == Key.Unknown) return;

        _keysDown.Remove(key);
        _keysReleased.Add(key);

        _publicEventBus.Publish(new KeyReleasedEvent(key));
    }

    private void OnMouseButtonDown(SDL3MouseButtonDownEvent evt)
    {
        var button = InputMapping.ToMouseButton(evt.ButtonEvent.Button);
        if (button == MouseButton.Unknown) return;

        _mouseButtonsDown.Add(button);
        _mouseButtonsPressed.Add(button);

        _publicEventBus.Publish(new MouseButtonPressedEvent(button, _mousePosition));
    }

    private void OnMouseButtonUp(SDL3MouseButtonUpEvent evt)
    {
        var button = InputMapping.ToMouseButton(evt.ButtonEvent.Button);
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
        var delta = new Vector2(evt.MotionEvent.XRel, evt.MotionEvent.YRel);
        _publicEventBus.Publish(new MouseMovedEvent(_mousePosition, delta));
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 text input and PointerToString.")]
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
        var button   = InputMapping.ToGamepadButton((SDL3.SDL.GamepadButton)evt.ButtonEvent.Button);

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
        var button   = InputMapping.ToGamepadButton((SDL3.SDL.GamepadButton)evt.ButtonEvent.Button);

        if (!_gamepadButtonsDown.ContainsKey(deviceId)) return;

        _gamepadButtonsDown[deviceId].Remove(button);
        _gamepadButtonsReleased[deviceId].Add(button);

        _publicEventBus.Publish(new GamepadButtonReleasedEvent(button, (int)deviceId));
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 gamepad subsystem.")]
    private void OnGamepadAdded(SDL3GamepadAddedEvent evt)
    {
        var deviceId = evt.DeviceEvent.Which;
        var gamepad  = SDL3.SDL.OpenGamepad(deviceId);

        if (gamepad != IntPtr.Zero)
        {
            _gamepads[deviceId]              = gamepad;
            _gamepadButtonsDown[deviceId]     = new HashSet<GamepadButton>();
            _gamepadButtonsPressed[deviceId]  = new HashSet<GamepadButton>();
            _gamepadButtonsReleased[deviceId] = new HashSet<GamepadButton>();

            var name = SDL3.SDL.GetGamepadName(gamepad);
            _logger.LogInformation("Gamepad {DeviceId} connected: {Name}", deviceId, name);
            _publicEventBus.Publish(new GamepadConnectedEvent((int)deviceId, name ?? "Unknown"));
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 gamepad subsystem.")]
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

    // ── Query methods ─────────────────────────────────────────────────────────

    public bool IsKeyDown(Key key)     => _keysDown.Contains(key);
    public bool IsKeyPressed(Key key)  => _keysPressed.Contains(key);
    public bool IsKeyReleased(Key key) => _keysReleased.Contains(key);

    public bool IsMouseButtonDown(MouseButton button)     => _mouseButtonsDown.Contains(button);
    public bool IsMouseButtonPressed(MouseButton button)  => _mouseButtonsPressed.Contains(button);
    public bool IsMouseButtonReleased(MouseButton button) => _mouseButtonsReleased.Contains(button);

    public bool IsGamepadConnected(int gamepadIndex = 0) => _gamepads.Count > gamepadIndex;

    public bool IsGamepadButtonDown(GamepadButton button, int gamepadIndex = 0)
    {
        var deviceId = GetDeviceIdByIndex(gamepadIndex);
        return deviceId != null
            && _gamepadButtonsDown.TryGetValue(deviceId.Value, out var buttons)
            && buttons.Contains(button);
    }

    public bool IsGamepadButtonPressed(GamepadButton button, int gamepadIndex = 0)
    {
        var deviceId = GetDeviceIdByIndex(gamepadIndex);
        return deviceId != null
            && _gamepadButtonsPressed.TryGetValue(deviceId.Value, out var buttons)
            && buttons.Contains(button);
    }

    public bool IsGamepadButtonReleased(GamepadButton button, int gamepadIndex = 0)
    {
        var deviceId = GetDeviceIdByIndex(gamepadIndex);
        return deviceId != null
            && _gamepadButtonsReleased.TryGetValue(deviceId.Value, out var buttons)
            && buttons.Contains(button);
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 gamepad axis polling.")]
    public float GetGamepadAxis(GamepadAxis axis, int gamepadIndex = 0)
    {
        var deviceId = GetDeviceIdByIndex(gamepadIndex);
        if (deviceId == null) return 0f;
        if (!_gamepads.TryGetValue(deviceId.Value, out var gamepad)) return 0f;

        var value = SDL3.SDL.GetGamepadAxis(gamepad, InputMapping.ToSDLAxis(axis));
        return value / 32767f;
    }

    public Vector2 GetGamepadLeftStick(int gamepadIndex = 0) => new(
        GetGamepadAxis(GamepadAxis.LeftX, gamepadIndex),
        GetGamepadAxis(GamepadAxis.LeftY, gamepadIndex));

    public Vector2 GetGamepadRightStick(int gamepadIndex = 0) => new(
        GetGamepadAxis(GamepadAxis.RightX, gamepadIndex),
        GetGamepadAxis(GamepadAxis.RightY, gamepadIndex));

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 text input subsystem.")]
    public void StartTextInput()
    {
        if (_textInputActive) return;
        SDL3.SDL.StartTextInput(_windowProvider?.Window ?? IntPtr.Zero);
        _textInputActive = true;
        _logger.LogDebug("Text input started");
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 text input subsystem.")]
    public void StopTextInput()
    {
        if (!_textInputActive) return;
        SDL3.SDL.StopTextInput(_windowProvider?.Window ?? IntPtr.Zero);
        _textInputActive = false;
        _logger.LogDebug("Text input stopped");
    }

    public string GetTextInput()       => _textInputBuffer.ToString();
    public bool IsBackspacePressed()   => _backspacePressedThisFrame;
    public bool IsReturnPressed()      => _returnPressedThisFrame;
    public bool IsDeletePressed()      => _deletePressedThisFrame;

    // ── Helpers ───────────────────────────────────────────────────────────────

    private uint? GetDeviceIdByIndex(int index)
    {
        if (index < 0 || index >= _gamepads.Count) return null;
        return _gamepads.Keys.ElementAtOrDefault(index);
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 gamepad subsystem.")]
    public void Dispose()
    {
        foreach (var subscription in _subscriptions)
            subscription.Dispose();
        _subscriptions.Clear();

        foreach (var gamepad in _gamepads.Values)
            SDL3.SDL.CloseGamepad(gamepad);
        _gamepads.Clear();
    }
}