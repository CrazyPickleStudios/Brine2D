using System.Numerics;
using Brine2D.Input;
using FluentAssertions;

namespace Brine2D.Tests.Input;

public class HeadlessInputContextTests
{
    private readonly HeadlessInputContext _ctx = new();

    [Fact]
    public void MousePosition_ReturnsZero() => _ctx.MousePosition.Should().Be(Vector2.Zero);

    [Fact]
    public void MouseDelta_ReturnsZero() => _ctx.MouseDelta.Should().Be(Vector2.Zero);

    [Fact]
    public void ScrollWheelDelta_ReturnsZero() => _ctx.ScrollWheelDelta.Should().Be(0f);

    [Fact]
    public void ScrollWheelDeltaX_ReturnsZero() => _ctx.ScrollWheelDeltaX.Should().Be(0f);

    [Fact]
    public void IsKeyDown_ReturnsFalse() => _ctx.IsKeyDown(Key.Space).Should().BeFalse();

    [Fact]
    public void IsKeyPressed_ReturnsFalse() => _ctx.IsKeyPressed(Key.Space).Should().BeFalse();

    [Fact]
    public void IsKeyReleased_ReturnsFalse() => _ctx.IsKeyReleased(Key.Space).Should().BeFalse();

    [Fact]
    public void IsMouseButtonDown_ReturnsFalse() => _ctx.IsMouseButtonDown(MouseButton.Left).Should().BeFalse();

    [Fact]
    public void IsMouseButtonPressed_ReturnsFalse() => _ctx.IsMouseButtonPressed(MouseButton.Left).Should().BeFalse();

    [Fact]
    public void IsMouseButtonReleased_ReturnsFalse() => _ctx.IsMouseButtonReleased(MouseButton.Left).Should().BeFalse();

    [Fact]
    public void IsGamepadConnected_ReturnsFalse() => _ctx.IsGamepadConnected().Should().BeFalse();

    [Fact]
    public void IsGamepadButtonDown_ReturnsFalse() => _ctx.IsGamepadButtonDown(GamepadButton.A).Should().BeFalse();

    [Fact]
    public void IsGamepadButtonPressed_ReturnsFalse() => _ctx.IsGamepadButtonPressed(GamepadButton.A).Should().BeFalse();

    [Fact]
    public void IsGamepadButtonReleased_ReturnsFalse() => _ctx.IsGamepadButtonReleased(GamepadButton.A).Should().BeFalse();

    [Fact]
    public void GetGamepadAxis_ReturnsZero() => _ctx.GetGamepadAxis(GamepadAxis.LeftX).Should().Be(0f);

    [Fact]
    public void IsGamepadAxisPressed_ReturnsFalse() => _ctx.IsGamepadAxisPressed(GamepadAxis.LeftX).Should().BeFalse();

    [Fact]
    public void IsGamepadAxisReleased_ReturnsFalse() => _ctx.IsGamepadAxisReleased(GamepadAxis.LeftX).Should().BeFalse();

    [Fact]
    public void GetGamepadTrigger_ReturnsZero() => _ctx.GetGamepadTrigger(GamepadAxis.LeftTrigger).Should().Be(0f);

    [Fact]
    public void IsGamepadTriggerPressed_ReturnsFalse() => _ctx.IsGamepadTriggerPressed(GamepadAxis.LeftTrigger).Should().BeFalse();

    [Fact]
    public void IsGamepadTriggerReleased_ReturnsFalse() => _ctx.IsGamepadTriggerReleased(GamepadAxis.LeftTrigger).Should().BeFalse();

    [Fact]
    public void GetGamepadLeftStick_ReturnsZero() => _ctx.GetGamepadLeftStick().Should().Be(Vector2.Zero);

    [Fact]
    public void GetGamepadRightStick_ReturnsZero() => _ctx.GetGamepadRightStick().Should().Be(Vector2.Zero);

    [Fact]
    public void IsTextInputActive_ReturnsFalse() => _ctx.IsTextInputActive.Should().BeFalse();

    [Fact]
    public void GetTextInput_ReturnsEmpty() => _ctx.GetTextInput().Should().BeEmpty();

    [Fact]
    public void IsBackspacePressed_ReturnsFalse() => _ctx.IsBackspacePressed().Should().BeFalse();

    [Fact]
    public void IsReturnPressed_ReturnsFalse() => _ctx.IsReturnPressed().Should().BeFalse();

    [Fact]
    public void IsDeletePressed_ReturnsFalse() => _ctx.IsDeletePressed().Should().BeFalse();

    [Fact]
    public void GamepadDeadzone_DefaultValue() => _ctx.GamepadDeadzone.Should().Be(0.15f);

    [Fact]
    public void GamepadDeadzone_CanBeSet()
    {
        _ctx.GamepadDeadzone = 0.25f;
        _ctx.GamepadDeadzone.Should().Be(0.25f);
    }

    [Fact]
    public void IsCursorVisible_DefaultTrue() => _ctx.IsCursorVisible.Should().BeTrue();

    [Fact]
    public void IsCursorVisible_CanBeSet()
    {
        _ctx.IsCursorVisible = false;
        _ctx.IsCursorVisible.Should().BeFalse();
    }

    [Fact]
    public void IsRelativeMouseMode_DefaultFalse() => _ctx.IsRelativeMouseMode.Should().BeFalse();

    [Fact]
    public void StartTextInput_DoesNotThrow()
    {
        var act = () => _ctx.StartTextInput();
        act.Should().NotThrow();
    }

    [Fact]
    public void StopTextInput_DoesNotThrow()
    {
        var act = () => _ctx.StopTextInput();
        act.Should().NotThrow();
    }

    [Fact]
    public void Update_DoesNotThrow()
    {
        var act = () => ((IInputContext)_ctx).Update();
        act.Should().NotThrow();
    }
}