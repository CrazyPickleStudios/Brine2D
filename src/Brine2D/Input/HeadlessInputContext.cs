using System.Numerics;

namespace Brine2D.Input;

/// <summary>
/// No-op input context for headless mode (servers, testing).
/// All input queries return false/zero - allows game logic to run without SDL.
/// </summary>
internal sealed class HeadlessInputContext : IInputContext
{
    public Vector2 MousePosition => Vector2.Zero;
    public Vector2 MouseDelta => Vector2.Zero;
    public float ScrollWheelDelta => 0f;
    public float ScrollWheelDeltaX => 0f;
    public bool IsTextInputActive => false;
    public float GamepadDeadzone { get; set; } = 0.15f;
    public bool IsCursorVisible { get; set; } = true;
    public bool IsRelativeMouseMode { get; set; }

    void IInputContext.Update() { }
    public void Dispose() { }

    public bool IsKeyDown(Key key) => false;
    public bool IsKeyPressed(Key key) => false;
    public bool IsKeyReleased(Key key) => false;
    public bool IsAnyKeyPressed() => false;

    public bool IsMouseButtonDown(MouseButton button) => false;
    public bool IsMouseButtonPressed(MouseButton button) => false;
    public bool IsMouseButtonReleased(MouseButton button) => false;
    public bool IsAnyMouseButtonPressed() => false;

    public bool IsGamepadConnected(int gamepadIndex = 0) => false;
    public int ConnectedGamepadCount => 0;
    public bool IsGamepadButtonDown(GamepadButton button, int gamepadIndex = 0) => false;
    public bool IsGamepadButtonPressed(GamepadButton button, int gamepadIndex = 0) => false;
    public bool IsGamepadButtonReleased(GamepadButton button, int gamepadIndex = 0) => false;
    public bool IsAnyGamepadButtonPressed(int gamepadIndex = 0) => false;

    public bool IsAnyGamepadButtonPressedOnAny(out int gamepadIndex)
    {
        gamepadIndex = -1;
        return false;
    }

    public float GetGamepadAxis(GamepadAxis axis, int gamepadIndex = 0) => 0f;
    public bool IsGamepadAxisPressed(GamepadAxis axis, int gamepadIndex = 0) => false;
    public bool IsGamepadAxisReleased(GamepadAxis axis, int gamepadIndex = 0) => false;

    public float GetGamepadTrigger(GamepadAxis trigger, int gamepadIndex = 0)
    {
        if (trigger is not (GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger))
            throw new ArgumentException("Only LeftTrigger and RightTrigger are valid.", nameof(trigger));
        return 0f;
    }

    public bool IsGamepadTriggerPressed(GamepadAxis trigger, int gamepadIndex = 0)
    {
        if (trigger is not (GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger))
            throw new ArgumentException("Only LeftTrigger and RightTrigger are valid.", nameof(trigger));
        return false;
    }

    public bool IsGamepadTriggerReleased(GamepadAxis trigger, int gamepadIndex = 0)
    {
        if (trigger is not (GamepadAxis.LeftTrigger or GamepadAxis.RightTrigger))
            throw new ArgumentException("Only LeftTrigger and RightTrigger are valid.", nameof(trigger));
        return false;
    }

    public Vector2 GetGamepadLeftStick(int gamepadIndex = 0) => Vector2.Zero;
    public Vector2 GetGamepadRightStick(int gamepadIndex = 0) => Vector2.Zero;
    public bool RumbleGamepad(float lowFrequency, float highFrequency, TimeSpan duration, int gamepadIndex = 0) => false;
    public bool RumbleGamepadTriggers(float leftTrigger, float rightTrigger, TimeSpan duration, int gamepadIndex = 0) => false;

    public void StartTextInput() { }
    public void StopTextInput() { }
    public string GetTextInput() => string.Empty;
    public bool IsBackspacePressed() => false;
    public bool IsReturnPressed() => false;
    public bool IsDeletePressed() => false;
}