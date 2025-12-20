using System.Numerics;

namespace Brine2D.Core.Input;

/// <summary>
/// Provides input handling for keyboard, mouse, and gamepad.
/// </summary>
public interface IInputService
{
    // === System ===
    
    /// <summary>
    /// Updates the input state for the current frame.
    /// This should be called once per frame before processing input.
    /// </summary>
    void Update();
    
    /// <summary>
    /// Gets whether a quit/exit event has been requested (e.g., window close button).
    /// </summary>
    bool IsQuitRequested { get; }
    
    // === Keyboard ===
    
    /// <summary>
    /// Returns true if the key is currently pressed.
    /// </summary>
    bool IsKeyDown(Keys key);
    
    /// <summary>
    /// Returns true if the key was pressed this frame (not held from previous frame).
    /// </summary>
    bool IsKeyPressed(Keys key);
    
    /// <summary>
    /// Returns true if the key was released this frame.
    /// </summary>
    bool IsKeyReleased(Keys key);
    
    // === Mouse ===
    
    /// <summary>
    /// Gets the current mouse position in window coordinates.
    /// </summary>
    Vector2 MousePosition { get; }
    
    /// <summary>
    /// Gets the mouse movement delta since last frame.
    /// </summary>
    Vector2 MouseDelta { get; }
    
    /// <summary>
    /// Gets the mouse scroll wheel delta.
    /// </summary>
    float ScrollWheelDelta { get; }
    
    /// <summary>
    /// Returns true if the mouse button is currently pressed.
    /// </summary>
    bool IsMouseButtonDown(MouseButton button);
    
    /// <summary>
    /// Returns true if the mouse button was pressed this frame.
    /// </summary>
    bool IsMouseButtonPressed(MouseButton button);
    
    /// <summary>
    /// Returns true if the mouse button was released this frame.
    /// </summary>
    bool IsMouseButtonReleased(MouseButton button);
    
    // === Gamepad ===
    
    /// <summary>
    /// Returns true if a gamepad is connected at the specified index.
    /// </summary>
    bool IsGamepadConnected(int gamepadIndex = 0);
    
    /// <summary>
    /// Returns true if the gamepad button is currently pressed.
    /// </summary>
    bool IsGamepadButtonDown(GamepadButton button, int gamepadIndex = 0);
    
    /// <summary>
    /// Returns true if the gamepad button was pressed this frame.
    /// </summary>
    bool IsGamepadButtonPressed(GamepadButton button, int gamepadIndex = 0);
    
    /// <summary>
    /// Returns true if the gamepad button was released this frame.
    /// </summary>
    bool IsGamepadButtonReleased(GamepadButton button, int gamepadIndex = 0);
    
    /// <summary>
    /// Gets the value of a gamepad axis (-1.0 to 1.0).
    /// </summary>
    float GetGamepadAxis(GamepadAxis axis, int gamepadIndex = 0);
    
    /// <summary>
    /// Gets the left stick position as a vector (-1 to 1 for each axis).
    /// </summary>
    Vector2 GetGamepadLeftStick(int gamepadIndex = 0);
    
    /// <summary>
    /// Gets the right stick position as a vector (-1 to 1 for each axis).
    /// </summary>
    Vector2 GetGamepadRightStick(int gamepadIndex = 0);
}