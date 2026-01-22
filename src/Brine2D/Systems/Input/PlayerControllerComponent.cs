using System.Numerics;
using Brine2D.ECS;

namespace Brine2D.Systems.Input;

/// <summary>
/// Component for player input control.
/// Lives in Brine2D.Input.ECS because it's input-specific.
/// Converts keyboard/gamepad input into movement velocity.
/// </summary>
public class PlayerControllerComponent : Component
{
    /// <summary>
    /// Movement speed in units per second.
    /// </summary>
    public float MoveSpeed { get; set; } = 200f;

    /// <summary>
    /// Input mode (keyboard, gamepad, or both).
    /// </summary>
    public InputMode InputMode { get; set; } = InputMode.KeyboardAndGamepad;

    /// <summary>
    /// Gamepad index (for local multiplayer).
    /// </summary>
    public int GamepadIndex { get; set; } = 0;

    /// <summary>
    /// Whether to normalize diagonal movement (prevents faster diagonal movement).
    /// </summary>
    public bool NormalizeDiagonals { get; set; } = true;

    /// <summary>
    /// Current input direction this frame (calculated by system).
    /// </summary>
    public Vector2 InputDirection { get; internal set; }

    /// <summary>
    /// Whether the player is currently moving (has input).
    /// </summary>
    public bool IsMoving => InputDirection != Vector2.Zero;
}

/// <summary>
/// Input control modes.
/// </summary>
public enum InputMode
{
    /// <summary>
    /// Only keyboard input (WASD or arrows).
    /// </summary>
    Keyboard,

    /// <summary>
    /// Only gamepad input (left stick).
    /// </summary>
    Gamepad,

    /// <summary>
    /// Both keyboard and gamepad (gamepad overrides keyboard).
    /// </summary>
    KeyboardAndGamepad
}
