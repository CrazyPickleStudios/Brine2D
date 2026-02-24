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
    public bool IsTextInputActive => false;

    void IInputContext.Update() { }

    public bool IsKeyDown(Key key) => false;
    public bool IsKeyPressed(Key key) => false;
    public bool IsKeyReleased(Key key) => false;

    public bool IsMouseButtonDown(MouseButton button) => false;
    public bool IsMouseButtonPressed(MouseButton button) => false;
    public bool IsMouseButtonReleased(MouseButton button) => false;

    public bool IsGamepadConnected(int gamepadIndex = 0) => false;
    public bool IsGamepadButtonDown(GamepadButton button, int gamepadIndex = 0) => false;
    public bool IsGamepadButtonPressed(GamepadButton button, int gamepadIndex = 0) => false;
    public bool IsGamepadButtonReleased(GamepadButton button, int gamepadIndex = 0) => false;
    public float GetGamepadAxis(GamepadAxis axis, int gamepadIndex = 0) => 0f;
    public Vector2 GetGamepadLeftStick(int gamepadIndex = 0) => Vector2.Zero;
    public Vector2 GetGamepadRightStick(int gamepadIndex = 0) => Vector2.Zero;

    public void StartTextInput() { }
    public void StopTextInput() { }
    public string GetTextInput() => string.Empty;
    public bool IsBackspacePressed() => false;
    public bool IsReturnPressed() => false;
    public bool IsDeletePressed() => false;
}