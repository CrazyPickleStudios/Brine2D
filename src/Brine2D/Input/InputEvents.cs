using System.Numerics;
using Brine2D.Input;

namespace Brine2D.Input;

/// <summary>
/// Raised when a key is pressed.
/// </summary>
public record KeyPressedEvent(Key Key, bool IsRepeat = false);

/// <summary>
/// Raised when a key is released.
/// </summary>
public record KeyReleasedEvent(Key Key);

/// <summary>
/// Raised when a mouse button is pressed.
/// </summary>
public record MouseButtonPressedEvent(MouseButton Button, Vector2 Position);

/// <summary>
/// Raised when a mouse button is released.
/// </summary>
public record MouseButtonReleasedEvent(MouseButton Button, Vector2 Position);

/// <summary>
/// Raised when the mouse is moved.
/// </summary>
public record MouseMovedEvent(Vector2 Position, Vector2 Delta);

/// <summary>
/// Raised when the mouse wheel is scrolled.
/// </summary>
public record MouseScrolledEvent(float DeltaX, float DeltaY);

/// <summary>
/// Raised when a gamepad button is pressed.
/// </summary>
public record GamepadButtonPressedEvent(GamepadButton Button, int GamepadIndex);

/// <summary>
/// Raised when a gamepad button is released.
/// </summary>
public record GamepadButtonReleasedEvent(GamepadButton Button, int GamepadIndex);

/// <summary>
/// Raised when a gamepad is connected.
/// </summary>
public record GamepadConnectedEvent(int GamepadIndex, string Name);

/// <summary>
/// Raised when a gamepad is disconnected.
/// </summary>
public record GamepadDisconnectedEvent(int GamepadIndex);