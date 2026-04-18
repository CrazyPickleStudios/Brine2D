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
/// Thread-safe: all mutable state is guarded by <see cref="_stateLock"/>.
/// The lock is never held while publishing events to avoid deadlocks when
/// user handlers query input state re-entrantly.
/// </remarks>
internal sealed class InputContext : IInputContext, IDisposable
{
    private static readonly GamepadAxis[] AllAxes =
    [
        GamepadAxis.LeftX, GamepadAxis.LeftY,
        GamepadAxis.RightX, GamepadAxis.RightY,
        GamepadAxis.LeftTrigger, GamepadAxis.RightTrigger,
    ];

    private readonly ILogger<InputContext> _logger;
    private readonly IEventBus _publicEventBus;
    private readonly IEventBus _internalEventBus;
    private readonly ISDL3WindowProvider? _windowProvider;

    private readonly Lock _stateLock = new();
    private readonly List<IDisposable> _subscriptions = [];

    private readonly HashSet<Key> _keysDown           = new();
    private readonly HashSet<Key> _keysPressed        = new();
    private readonly HashSet<Key> _keysReleased       = new();

    private Vector2 _mousePosition;
    private Vector2 _mouseDelta;
    private float   _scrollWheelDelta;
    private float   _scrollWheelDeltaX;
    private readonly HashSet<MouseButton> _mouseButtonsDown     = new();
    private readonly HashSet<MouseButton> _mouseButtonsPressed  = new();
    private readonly HashSet<MouseButton> _mouseButtonsReleased = new();

    private readonly Dictionary<uint, nint>               _gamepads              = new();
    private readonly List<uint?>                           _gamepadSlots          = [];
    private readonly Dictionary<uint, HashSet<GamepadButton>> _gamepadButtonsDown     = new();
    private readonly Dictionary<uint, HashSet<GamepadButton>> _gamepadButtonsPressed  = new();
    private readonly Dictionary<uint, HashSet<GamepadButton>> _gamepadButtonsReleased = new();

    private readonly Dictionary<(uint DeviceId, GamepadAxis Axis), bool> _previousAxisActive = new();
    private readonly Dictionary<(uint DeviceId, GamepadAxis Axis), bool> _currentAxisActive = new();

    private bool            _textInputActive;
    private readonly StringBuilder _textInputBuffer        = new();
    private bool            _backspacePressedThisFrame;
    private bool            _returnPressedThisFrame;
    private bool            _deletePressedThisFrame;

    private float _gamepadDeadzone = 0.15f;
    
    public Vector2 MousePosition { get { lock (_stateLock) return _mousePosition; } }
    public Vector2 MouseDelta { get { lock (_stateLock) return _mouseDelta; } }
    public float ScrollWheelDelta { get { lock (_stateLock) return _scrollWheelDelta; } }
    public float ScrollWheelDeltaX { get { lock (_stateLock) return _scrollWheelDeltaX; } }
    public bool IsTextInputActive { get { lock (_stateLock) return _textInputActive; } }

    public float GamepadDeadzone
    {
        get { lock (_stateLock) return _gamepadDeadzone; }
        set { lock (_stateLock) _gamepadDeadzone = Math.Clamp(value, 0f, 1f); }
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 cursor subsystem.")]
    public bool IsCursorVisible
    {
        get => SDL3.SDL.CursorVisible();
        set
        {
            if (value)
                SDL3.SDL.ShowCursor();
            else
                SDL3.SDL.HideCursor();
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 mouse subsystem.")]
    public bool IsRelativeMouseMode
    {
        get => SDL3.SDL.GetWindowRelativeMouseMode(_windowProvider?.Window ?? IntPtr.Zero);
        set => SDL3.SDL.SetWindowRelativeMouseMode(_windowProvider?.Window ?? IntPtr.Zero, value);
    }

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
        InitializeMousePosition();
    }

    void IInputContext.Update()
    {
        SnapshotAxisState();
        ClearPerFrameBuffers();
    }

    /// <summary>
    /// Promotes the previous frame's current axis state to the previous buffer, then
    /// captures a fresh snapshot of live axis values into the current buffer.
    /// Called at frame start so that <see cref="IsGamepadAxisPressed"/> and
    /// <see cref="IsGamepadAxisReleased"/> can compare last frame's state against
    /// this frame's live values.
    /// </summary>
    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 gamepad axis polling.")]
    private void SnapshotAxisState()
    {
        lock (_stateLock)
        {
            var deadzone = _gamepadDeadzone;

            _previousAxisActive.Clear();
            foreach (var kvp in _currentAxisActive)
                _previousAxisActive[kvp.Key] = kvp.Value;
            _currentAxisActive.Clear();

            for (int g = 0; g < _gamepadSlots.Count; g++)
            {
                var slot = _gamepadSlots[g];
                if (slot == null)
                    continue;

                var deviceId = slot.Value;
                if (!_gamepads.TryGetValue(deviceId, out var gamepad))
                    continue;

                for (int a = 0; a < AllAxes.Length; a++)
                {
                    var axis = AllAxes[a];
                    var raw = SDL3.SDL.GetGamepadAxis(gamepad, InputMapping.ToSDLAxis(axis));
                    var value = Math.Clamp(raw / 32767f, -1f, 1f);
                    var isActive = axis is GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger
                        ? value > deadzone
                        : MathF.Abs(value) > deadzone;

                    _currentAxisActive[(deviceId, axis)] = isActive;
                }
            }
        }
    }

    /// <summary>
    /// Clears all per-frame input state.
    /// Called directly by unit tests to simulate a new frame boundary.
    /// </summary>
    internal void ClearFrameState() => ClearPerFrameBuffers();

    private void ClearPerFrameBuffers()
    {
        lock (_stateLock)
        {
            _keysPressed.Clear();
            _keysReleased.Clear();
            _mouseButtonsPressed.Clear();
            _mouseButtonsReleased.Clear();
            _mouseDelta = Vector2.Zero;
            _scrollWheelDelta = 0f;
            _scrollWheelDeltaX = 0f;
            _textInputBuffer.Clear();
            _backspacePressedThisFrame = false;
            _returnPressedThisFrame = false;
            _deletePressedThisFrame = false;

            foreach (var set in _gamepadButtonsPressed.Values)
                set.Clear();
            foreach (var set in _gamepadButtonsReleased.Values)
                set.Clear();
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 mouse device.")]
    private void InitializeMousePosition()
    {
        SDL3.SDL.GetMouseState(out float mx, out float my);
        _mousePosition = new Vector2(mx, my);
    }

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
                    lock (_stateLock)
                    {
                        _gamepads[deviceId] = gamepad;
                        _gamepadSlots.Add(deviceId);
                        _gamepadButtonsDown[deviceId]     = new HashSet<GamepadButton>();
                        _gamepadButtonsPressed[deviceId]  = new HashSet<GamepadButton>();
                        _gamepadButtonsReleased[deviceId] = new HashSet<GamepadButton>();
                    }

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

    private void OnKeyDown(SDL3KeyDownEvent evt)
    {
        var key = InputMapping.ToKey(evt.KeyEvent.Key);
        if (key == Key.Unknown) return;

        bool isRepeat = evt.KeyEvent.Repeat;

        lock (_stateLock)
        {
            _keysDown.Add(key);

            if (!isRepeat)
                _keysPressed.Add(key);

            if (key == Key.Backspace) _backspacePressedThisFrame = true;
            if (key == Key.Enter)     _returnPressedThisFrame    = true;
            if (key == Key.Delete)    _deletePressedThisFrame    = true;
        }

        _publicEventBus.Publish(new KeyPressedEvent(key, IsRepeat: isRepeat));
    }

    private void OnKeyUp(SDL3KeyUpEvent evt)
    {
        var key = InputMapping.ToKey(evt.KeyEvent.Key);
        if (key == Key.Unknown) return;

        lock (_stateLock)
        {
            _keysDown.Remove(key);
            _keysReleased.Add(key);
        }

        _publicEventBus.Publish(new KeyReleasedEvent(key));
    }

    private void OnMouseButtonDown(SDL3MouseButtonDownEvent evt)
    {
        var button = InputMapping.ToMouseButton(evt.ButtonEvent.Button);
        if (button == MouseButton.Unknown) return;

        Vector2 position;
        lock (_stateLock)
        {
            _mouseButtonsDown.Add(button);
            _mouseButtonsPressed.Add(button);
            position = _mousePosition;
        }

        _publicEventBus.Publish(new MouseButtonPressedEvent(button, position));
    }

    private void OnMouseButtonUp(SDL3MouseButtonUpEvent evt)
    {
        var button = InputMapping.ToMouseButton(evt.ButtonEvent.Button);
        if (button == MouseButton.Unknown) return;

        Vector2 position;
        lock (_stateLock)
        {
            _mouseButtonsDown.Remove(button);
            _mouseButtonsReleased.Add(button);
            position = _mousePosition;
        }

        _publicEventBus.Publish(new MouseButtonReleasedEvent(button, position));
    }

    private void OnMouseWheel(SDL3MouseWheelEvent evt)
    {
        lock (_stateLock)
        {
            _scrollWheelDelta  += evt.WheelEvent.Y;
            _scrollWheelDeltaX += evt.WheelEvent.X;
        }

        _publicEventBus.Publish(new MouseScrolledEvent(evt.WheelEvent.X, evt.WheelEvent.Y));
    }

    private void OnMouseMotion(SDL3MouseMotionEvent evt)
    {
        Vector2 position;
        Vector2 delta = new(evt.MotionEvent.XRel, evt.MotionEvent.YRel);

        lock (_stateLock)
        {
            _mousePosition = new Vector2(evt.MotionEvent.X, evt.MotionEvent.Y);
            _mouseDelta   += delta;
            position       = _mousePosition;
        }

        _publicEventBus.Publish(new MouseMovedEvent(position, delta));
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 text input and PointerToString.")]
    private void OnTextInput(SDL3TextInputEvent evt)
    {
        var text = SDL3.SDL.PointerToString(evt.TextEvent.Text);

        if (!string.IsNullOrEmpty(text))
        {
            lock (_stateLock)
            {
                if (_textInputActive)
                {
                    _textInputBuffer.Append(text);
                }
            }

            _logger.LogTrace("Text input: {Text}", text);
        }
    }

    private void OnGamepadButtonDown(SDL3GamepadButtonDownEvent evt)
    {
        var deviceId = evt.ButtonEvent.Which;
        var button   = InputMapping.ToGamepadButton((SDL3.SDL.GamepadButton)evt.ButtonEvent.Button);

        if (button == GamepadButton.Unknown) return;

        int gamepadIndex;
        lock (_stateLock)
        {
            if (!_gamepadButtonsDown.ContainsKey(deviceId))
            {
                _logger.LogWarning("Received button event for unknown gamepad device {DeviceId}", deviceId);
                return;
            }

            _gamepadButtonsDown[deviceId].Add(button);
            _gamepadButtonsPressed[deviceId].Add(button);
            gamepadIndex = IndexOfDevice(deviceId);
        }

        _publicEventBus.Publish(new GamepadButtonPressedEvent(button, gamepadIndex));
        _logger.LogTrace("Gamepad {DeviceId} button {Button} pressed", deviceId, button);
    }

    private void OnGamepadButtonUp(SDL3GamepadButtonUpEvent evt)
    {
        var deviceId = evt.ButtonEvent.Which;
        var button   = InputMapping.ToGamepadButton((SDL3.SDL.GamepadButton)evt.ButtonEvent.Button);

        if (button == GamepadButton.Unknown) return;

        int gamepadIndex;
        lock (_stateLock)
        {
            if (!_gamepadButtonsDown.ContainsKey(deviceId)) return;

            _gamepadButtonsDown[deviceId].Remove(button);
            _gamepadButtonsReleased[deviceId].Add(button);
            gamepadIndex = IndexOfDevice(deviceId);
        }

        _publicEventBus.Publish(new GamepadButtonReleasedEvent(button, gamepadIndex));
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 gamepad subsystem.")]
    private void OnGamepadAdded(SDL3GamepadAddedEvent evt)
    {
        var deviceId = evt.DeviceEvent.Which;
        var gamepad  = SDL3.SDL.OpenGamepad(deviceId);

        if (gamepad != IntPtr.Zero)
        {
            int gamepadIndex;
            lock (_stateLock)
            {
                _gamepads[deviceId] = gamepad;
                gamepadIndex = AllocateSlot(deviceId);
                _gamepadButtonsDown[deviceId]     = new HashSet<GamepadButton>();
                _gamepadButtonsPressed[deviceId]  = new HashSet<GamepadButton>();
                _gamepadButtonsReleased[deviceId] = new HashSet<GamepadButton>();
            }

            var name = SDL3.SDL.GetGamepadName(gamepad);
            _logger.LogInformation("Gamepad {DeviceId} connected at slot {Index}: {Name}", deviceId, gamepadIndex, name);
            _publicEventBus.Publish(new GamepadConnectedEvent(gamepadIndex, name ?? "Unknown"));
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 gamepad subsystem.")]
    private void OnGamepadRemoved(SDL3GamepadRemovedEvent evt)
    {
        var deviceId = evt.DeviceEvent.Which;

        nint gamepad;
        int gamepadIndex;
        lock (_stateLock)
        {
            if (!_gamepads.TryGetValue(deviceId, out gamepad))
                return;

            gamepadIndex = IndexOfDevice(deviceId);
            if (gamepadIndex >= 0)
                _gamepadSlots[gamepadIndex] = null;

            _gamepads.Remove(deviceId);
            _gamepadButtonsDown.Remove(deviceId);
            _gamepadButtonsPressed.Remove(deviceId);
            _gamepadButtonsReleased.Remove(deviceId);

            foreach (var axis in AllAxes)
            {
                _previousAxisActive.Remove((deviceId, axis));
                _currentAxisActive.Remove((deviceId, axis));
            }
        }

        SDL3.SDL.CloseGamepad(gamepad);
        _logger.LogInformation("Gamepad {DeviceId} disconnected from slot {Index}", deviceId, gamepadIndex);
        _publicEventBus.Publish(new GamepadDisconnectedEvent(gamepadIndex));
    }

    public bool IsKeyDown(Key key)     { lock (_stateLock) return _keysDown.Contains(key); }
    public bool IsKeyPressed(Key key)  { lock (_stateLock) return _keysPressed.Contains(key); }
    public bool IsKeyReleased(Key key) { lock (_stateLock) return _keysReleased.Contains(key); }
    public bool IsAnyKeyPressed()      { lock (_stateLock) return _keysPressed.Count > 0; }

    public bool IsMouseButtonDown(MouseButton button)     { lock (_stateLock) return _mouseButtonsDown.Contains(button); }
    public bool IsMouseButtonPressed(MouseButton button)  { lock (_stateLock) return _mouseButtonsPressed.Contains(button); }
    public bool IsMouseButtonReleased(MouseButton button) { lock (_stateLock) return _mouseButtonsReleased.Contains(button); }
    public bool IsAnyMouseButtonPressed()                 { lock (_stateLock) return _mouseButtonsPressed.Count > 0; }

    public bool IsGamepadConnected(int gamepadIndex = 0)
    {
        lock (_stateLock)
            return gamepadIndex >= 0
                && gamepadIndex < _gamepadSlots.Count
                && _gamepadSlots[gamepadIndex] != null;
    }

    public bool IsGamepadButtonDown(GamepadButton button, int gamepadIndex = 0)
    {
        lock (_stateLock)
        {
            var deviceId = GetDeviceIdByIndex(gamepadIndex);
            return deviceId != null
                && _gamepadButtonsDown.TryGetValue(deviceId.Value, out var buttons)
                && buttons.Contains(button);
        }
    }

    public bool IsGamepadButtonPressed(GamepadButton button, int gamepadIndex = 0)
    {
        lock (_stateLock)
        {
            var deviceId = GetDeviceIdByIndex(gamepadIndex);
            return deviceId != null
                && _gamepadButtonsPressed.TryGetValue(deviceId.Value, out var buttons)
                && buttons.Contains(button);
        }
    }

    public bool IsGamepadButtonReleased(GamepadButton button, int gamepadIndex = 0)
    {
        lock (_stateLock)
        {
            var deviceId = GetDeviceIdByIndex(gamepadIndex);
            return deviceId != null
                && _gamepadButtonsReleased.TryGetValue(deviceId.Value, out var buttons)
                && buttons.Contains(button);
        }
    }

    public bool IsAnyGamepadButtonPressed(int gamepadIndex = 0)
    {
        lock (_stateLock)
        {
            var deviceId = GetDeviceIdByIndex(gamepadIndex);
            return deviceId != null
                && _gamepadButtonsPressed.TryGetValue(deviceId.Value, out var buttons)
                && buttons.Count > 0;
        }
    }

    public int ConnectedGamepadCount
    {
        get
        {
            lock (_stateLock)
            {
                int count = 0;
                for (int i = 0; i < _gamepadSlots.Count; i++)
                {
                    if (_gamepadSlots[i] != null)
                        count++;
                }
                return count;
            }
        }
    }

    public bool IsAnyGamepadButtonPressedOnAny(out int gamepadIndex)
    {
        lock (_stateLock)
        {
            for (int i = 0; i < _gamepadSlots.Count; i++)
            {
                var slot = _gamepadSlots[i];
                if (slot == null)
                    continue;

                if (_gamepadButtonsPressed.TryGetValue(slot.Value, out var buttons) && buttons.Count > 0)
                {
                    gamepadIndex = i;
                    return true;
                }
            }
        }

        gamepadIndex = -1;
        return false;
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 gamepad axis polling.")]
    public float GetGamepadAxis(GamepadAxis axis, int gamepadIndex = 0)
    {
        nint gamepad;
        lock (_stateLock)
        {
            var deviceId = GetDeviceIdByIndex(gamepadIndex);
            if (deviceId == null) return 0f;
            if (!_gamepads.TryGetValue(deviceId.Value, out gamepad)) return 0f;
        }

        var value = SDL3.SDL.GetGamepadAxis(gamepad, InputMapping.ToSDLAxis(axis));
        return Math.Clamp(value / 32767f, -1f, 1f);
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 gamepad axis polling.")]
    public bool IsGamepadAxisPressed(GamepadAxis axis, int gamepadIndex = 0)
    {
        lock (_stateLock)
        {
            var deviceId = GetDeviceIdByIndex(gamepadIndex);
            if (deviceId == null) return false;

            var isActive = _currentAxisActive.TryGetValue((deviceId.Value, axis), out var cur) && cur;
            var wasPreviouslyActive = _previousAxisActive.TryGetValue((deviceId.Value, axis), out var prev) && prev;
            return isActive && !wasPreviouslyActive;
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 gamepad axis polling.")]
    public bool IsGamepadAxisReleased(GamepadAxis axis, int gamepadIndex = 0)
    {
        lock (_stateLock)
        {
            var deviceId = GetDeviceIdByIndex(gamepadIndex);
            if (deviceId == null) return false;

            var isActive = _currentAxisActive.TryGetValue((deviceId.Value, axis), out var cur) && cur;
            var wasPreviouslyActive = _previousAxisActive.TryGetValue((deviceId.Value, axis), out var prev) && prev;
            return !isActive && wasPreviouslyActive;
        }
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 gamepad axis polling.")]
    public float GetGamepadTrigger(GamepadAxis trigger, int gamepadIndex = 0)
    {
        if (trigger is not (GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger))
            throw new ArgumentException("Only LeftTrigger and RightTrigger are valid.", nameof(trigger));

        return Math.Clamp(GetGamepadAxis(trigger, gamepadIndex), 0f, 1f);
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 gamepad axis polling.")]
    public bool IsGamepadTriggerPressed(GamepadAxis trigger, int gamepadIndex = 0)
    {
        if (trigger is not (GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger))
            throw new ArgumentException("Only LeftTrigger and RightTrigger are valid.", nameof(trigger));

        return IsGamepadAxisPressed(trigger, gamepadIndex);
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 gamepad axis polling.")]
    public bool IsGamepadTriggerReleased(GamepadAxis trigger, int gamepadIndex = 0)
    {
        if (trigger is not (GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger))
            throw new ArgumentException("Only LeftTrigger and RightTrigger are valid.", nameof(trigger));

        return IsGamepadAxisReleased(trigger, gamepadIndex);
    }

    public Vector2 GetGamepadLeftStick(int gamepadIndex = 0) =>
        ApplyRadialDeadzone(new Vector2(
            GetGamepadAxis(GamepadAxis.LeftX, gamepadIndex),
            GetGamepadAxis(GamepadAxis.LeftY, gamepadIndex)));

    public Vector2 GetGamepadRightStick(int gamepadIndex = 0) =>
        ApplyRadialDeadzone(new Vector2(
            GetGamepadAxis(GamepadAxis.RightX, gamepadIndex),
            GetGamepadAxis(GamepadAxis.RightY, gamepadIndex)));

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 gamepad rumble subsystem.")]
    public bool RumbleGamepad(float lowFrequency, float highFrequency, TimeSpan duration, int gamepadIndex = 0)
    {
        nint gamepad;
        lock (_stateLock)
        {
            var deviceId = GetDeviceIdByIndex(gamepadIndex);
            if (deviceId == null) return false;
            if (!_gamepads.TryGetValue(deviceId.Value, out gamepad)) return false;
        }

        var lowU16 = (ushort)(Math.Clamp(lowFrequency, 0f, 1f) * 0xFFFF);
        var highU16 = (ushort)(Math.Clamp(highFrequency, 0f, 1f) * 0xFFFF);
        var durationMs = (uint)Math.Clamp(duration.TotalMilliseconds, 0, uint.MaxValue);
        return SDL3.SDL.RumbleGamepad(gamepad, lowU16, highU16, durationMs);
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 gamepad rumble subsystem.")]
    public bool RumbleGamepadTriggers(float leftTrigger, float rightTrigger, TimeSpan duration, int gamepadIndex = 0)
    {
        nint gamepad;
        lock (_stateLock)
        {
            var deviceId = GetDeviceIdByIndex(gamepadIndex);
            if (deviceId == null) return false;
            if (!_gamepads.TryGetValue(deviceId.Value, out gamepad)) return false;
        }

        var leftU16 = (ushort)(Math.Clamp(leftTrigger, 0f, 1f) * 0xFFFF);
        var rightU16 = (ushort)(Math.Clamp(rightTrigger, 0f, 1f) * 0xFFFF);
        var durationMs = (uint)Math.Clamp(duration.TotalMilliseconds, 0, uint.MaxValue);
        return SDL3.SDL.RumbleGamepadTriggers(gamepad, leftU16, rightU16, durationMs);
    }

    private Vector2 ApplyRadialDeadzone(Vector2 stick)
    {
        float deadzone;
        lock (_stateLock) deadzone = _gamepadDeadzone;

        var magnitude = stick.Length();
        if (magnitude < deadzone)
            return Vector2.Zero;

        var direction = stick / magnitude;
        var rescaled = (magnitude - deadzone) / (1f - deadzone);
        return direction * Math.Min(rescaled, 1f);
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 text input subsystem.")]
    public void StartTextInput()
    {
        lock (_stateLock)
        {
            if (_textInputActive) return;
            _textInputActive = true;
        }

        SDL3.SDL.StartTextInput(_windowProvider?.Window ?? IntPtr.Zero);
        _logger.LogDebug("Text input started");
    }

    [ExcludeFromCodeCoverage(Justification = "Requires SDL3 text input subsystem.")]
    public void StopTextInput()
    {
        lock (_stateLock)
        {
            if (!_textInputActive) return;
            _textInputActive = false;
        }

        SDL3.SDL.StopTextInput(_windowProvider?.Window ?? IntPtr.Zero);
        _logger.LogDebug("Text input stopped");
    }

    public string GetTextInput()     { lock (_stateLock) return _textInputBuffer.ToString(); }
    public bool IsBackspacePressed() { lock (_stateLock) return _backspacePressedThisFrame; }
    public bool IsReturnPressed()    { lock (_stateLock) return _returnPressedThisFrame; }
    public bool IsDeletePressed()    { lock (_stateLock) return _deletePressedThisFrame; }

    private uint? GetDeviceIdByIndex(int index)
    {
        if (index < 0 || index >= _gamepadSlots.Count) return null;
        return _gamepadSlots[index];
    }

    private int IndexOfDevice(uint deviceId)
    {
        for (int i = 0; i < _gamepadSlots.Count; i++)
        {
            if (_gamepadSlots[i] == deviceId)
                return i;
        }
        return -1;
    }

    private int AllocateSlot(uint deviceId)
    {
        for (int i = 0; i < _gamepadSlots.Count; i++)
        {
            if (_gamepadSlots[i] == null)
            {
                _gamepadSlots[i] = deviceId;
                return i;
            }
        }
        _gamepadSlots.Add(deviceId);
        return _gamepadSlots.Count - 1;
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
        _gamepadSlots.Clear();
        _gamepadButtonsDown.Clear();
        _gamepadButtonsPressed.Clear();
        _gamepadButtonsReleased.Clear();
        _previousAxisActive.Clear();
        _currentAxisActive.Clear();
    }
}