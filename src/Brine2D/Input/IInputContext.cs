using System.Numerics;

namespace Brine2D.Input;

/// <summary>
/// Provides input handling for keyboard, mouse, and gamepad.
/// </summary>
public interface IInputContext : IDisposable
{
    /// <summary>
    /// Updates the input state for the current frame.
    /// Must be called exactly once per frame before processing input.
    /// </summary>
    internal void Update();

    /// <summary>
    /// Returns true if the key is currently pressed.
    /// </summary>
    bool IsKeyDown(Key key);

    /// <summary>
    /// Returns true if the key was pressed this frame (not held from previous frame).
    /// </summary>
    bool IsKeyPressed(Key key);

    /// <summary>
    /// Returns true if the key was released this frame.
    /// </summary>
    bool IsKeyReleased(Key key);

    /// <summary>
    /// Returns true if any key was pressed this frame.
    /// Useful for "press any key to continue" screens.
    /// </summary>
    bool IsAnyKeyPressed();

    /// <summary>
    /// Gets the current mouse position in window coordinates.
    /// </summary>
    Vector2 MousePosition { get; }

    /// <summary>
    /// Gets the mouse movement delta since last frame.
    /// </summary>
    Vector2 MouseDelta { get; }

    /// <summary>
    /// Gets the vertical mouse scroll wheel delta.
    /// </summary>
    float ScrollWheelDelta { get; }

    /// <summary>
    /// Gets the horizontal mouse scroll wheel delta.
    /// </summary>
    float ScrollWheelDeltaX { get; }

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

    /// <summary>
    /// Returns true if any mouse button was pressed this frame.
    /// Useful for "press any button to continue" screens.
    /// </summary>
    bool IsAnyMouseButtonPressed();

    /// <summary>
    /// Gets or sets whether the mouse cursor is visible.
    /// </summary>
    bool IsCursorVisible { get; set; }

    /// <summary>
    /// Gets or sets whether relative mouse mode is enabled.
    /// When enabled, the cursor is hidden, the mouse is captured, and only
    /// <see cref="MouseDelta"/> is meaningful (position is not updated).
    /// Useful for first-person cameras and drag operations.
    /// </summary>
    bool IsRelativeMouseMode { get; set; }

    /// <summary>
    /// Returns true if a gamepad is connected at the specified index.
    /// </summary>
    bool IsGamepadConnected(int gamepadIndex = 0);

    /// <summary>
    /// Gets the number of gamepad slots currently occupied by connected gamepads.
    /// Useful for iterating all connected gamepads in multiplayer lobbies.
    /// </summary>
    int ConnectedGamepadCount { get; }

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
    /// Returns true if any gamepad button was pressed this frame on the specified gamepad.
    /// Useful for "press any button to continue" screens.
    /// </summary>
    bool IsAnyGamepadButtonPressed(int gamepadIndex = 0);

    /// <summary>
    /// Returns true if any gamepad button was pressed this frame on any connected gamepad.
    /// Useful for multiplayer "press any button to join" lobbies.
    /// The <paramref name="gamepadIndex"/> output indicates which gamepad pressed a button,
    /// or -1 if none did.
    /// </summary>
    bool IsAnyGamepadButtonPressedOnAny(out int gamepadIndex);

    /// <summary>
    /// Gets the value of a gamepad axis (−1.0 to 1.0 for sticks, 0.0 to 1.0 for triggers).
    /// Returns the raw axis value without deadzone applied.
    /// </summary>
    float GetGamepadAxis(GamepadAxis axis, int gamepadIndex = 0);

    /// <summary>
    /// Returns true if the gamepad axis crossed the deadzone threshold this frame
    /// (was inactive last frame, active now).
    /// </summary>
    bool IsGamepadAxisPressed(GamepadAxis axis, int gamepadIndex = 0);

    /// <summary>
    /// Returns true if the gamepad axis dropped below the deadzone threshold this frame
    /// (was active last frame, inactive now).
    /// </summary>
    bool IsGamepadAxisReleased(GamepadAxis axis, int gamepadIndex = 0);

    /// <summary>
    /// Gets the value of a gamepad trigger (0.0 to 1.0).
    /// Only <see cref="GamepadAxis.LeftTrigger"/> and <see cref="GamepadAxis.RightTrigger"/> are valid.
    /// </summary>
    float GetGamepadTrigger(GamepadAxis trigger, int gamepadIndex = 0);

    /// <summary>
    /// Returns true if the gamepad trigger crossed the deadzone threshold this frame
    /// (was inactive last frame, active now).
    /// </summary>
    bool IsGamepadTriggerPressed(GamepadAxis trigger, int gamepadIndex = 0);

    /// <summary>
    /// Returns true if the gamepad trigger dropped below the deadzone threshold this frame
    /// (was active last frame, inactive now).
    /// </summary>
    bool IsGamepadTriggerReleased(GamepadAxis trigger, int gamepadIndex = 0);

    /// <summary>
    /// Gets or sets the radial deadzone threshold for gamepad sticks (0.0 to 1.0).
    /// Values within the deadzone are reported as zero. The remaining range is
    /// rescaled to 0–1 so that movement begins smoothly at the deadzone edge.
    /// Default is 0.15.
    /// </summary>
    float GamepadDeadzone { get; set; }

    /// <summary>
    /// Gets the left stick position as a vector (-1 to 1 for each axis)
    /// with radial deadzone applied.
    /// </summary>
    Vector2 GetGamepadLeftStick(int gamepadIndex = 0);

    /// <summary>
    /// Gets the right stick position as a vector (-1 to 1 for each axis)
    /// with radial deadzone applied.
    /// </summary>
    Vector2 GetGamepadRightStick(int gamepadIndex = 0);

    /// <summary>
    /// Rumbles the gamepad using the low-frequency and high-frequency motors.
    /// Intensity values are 0.0 (off) to 1.0 (max). Duration of 0 stops rumble.
    /// </summary>
    /// <param name="lowFrequency">Low-frequency (left) motor intensity (0.0–1.0).</param>
    /// <param name="highFrequency">High-frequency (right) motor intensity (0.0–1.0).</param>
    /// <param name="duration">Duration of the rumble effect.</param>
    /// <param name="gamepadIndex">Gamepad slot index.</param>
    /// <returns>True if the gamepad supports rumble and the command succeeded.</returns>
    bool RumbleGamepad(float lowFrequency, float highFrequency, TimeSpan duration, int gamepadIndex = 0);

    /// <summary>
    /// Rumbles the gamepad triggers independently (e.g., Xbox impulse triggers).
    /// Intensity values are 0.0 (off) to 1.0 (max). Duration of 0 stops rumble.
    /// </summary>
    /// <param name="leftTrigger">Left trigger motor intensity (0.0–1.0).</param>
    /// <param name="rightTrigger">Right trigger motor intensity (0.0–1.0).</param>
    /// <param name="duration">Duration of the rumble effect.</param>
    /// <param name="gamepadIndex">Gamepad slot index.</param>
    /// <returns>True if the gamepad supports trigger rumble and the command succeeded.</returns>
    bool RumbleGamepadTriggers(float leftTrigger, float rightTrigger, TimeSpan duration, int gamepadIndex = 0);

    /// <summary>
    /// Starts text input mode. Call this when a text field is focused.
    /// </summary>
    void StartTextInput();

    /// <summary>
    /// Stops text input mode. Call this when text field loses focus.
    /// </summary>
    void StopTextInput();

    /// <summary>
    /// Gets whether text input mode is active.
    /// </summary>
    bool IsTextInputActive { get; }

    /// <summary>
    /// Gets the text that was input this frame (from SDL_EVENT_TEXT_INPUT).
    /// This properly handles Unicode, IME, and keyboard layouts.
    /// </summary>
    string GetTextInput();

    /// <summary>
    /// Returns true if backspace was pressed this frame, including key repeats.
    /// Useful for text editing; works regardless of whether text input mode is active.
    /// Note: unlike <see cref="IsKeyPressed"/>, this fires on held-key repeats
    /// to provide expected text-editing behavior.
    /// </summary>
    bool IsBackspacePressed();

    /// <summary>
    /// Returns true if Enter/Return was pressed this frame, including key repeats.
    /// Useful for text submission; works regardless of whether text input mode is active.
    /// Note: unlike <see cref="IsKeyPressed"/>, this fires on held-key repeats
    /// to provide expected text-editing behavior.
    /// </summary>
    bool IsReturnPressed();

    /// <summary>
    /// Returns true if Delete was pressed this frame, including key repeats.
    /// Useful for text editing; works regardless of whether text input mode is active.
    /// Note: unlike <see cref="IsKeyPressed"/>, this fires on held-key repeats
    /// to provide expected text-editing behavior.
    /// </summary>
    bool IsDeletePressed();
}