using Brine2D.Input;
using FluentAssertions;
using NSubstitute;

namespace Brine2D.Tests.Input;

public class InputBindingTests
{
    private readonly IInputContext _input = Substitute.For<IInputContext>();

    public class KeyBindingTests : InputBindingTests
    {
        [Fact]
        public void IsDown_WhenKeyDown_ReturnsTrue()
        {
            _input.IsKeyDown(Key.Space).Returns(true);
            new KeyBinding(Key.Space).IsDown(_input).Should().BeTrue();
        }

        [Fact]
        public void IsDown_WhenKeyUp_ReturnsFalse()
        {
            _input.IsKeyDown(Key.Space).Returns(false);
            new KeyBinding(Key.Space).IsDown(_input).Should().BeFalse();
        }

        [Fact]
        public void IsPressed_DelegatesToContext()
        {
            _input.IsKeyPressed(Key.A).Returns(true);
            new KeyBinding(Key.A).IsPressed(_input).Should().BeTrue();
        }

        [Fact]
        public void IsReleased_DelegatesToContext()
        {
            _input.IsKeyReleased(Key.A).Returns(true);
            new KeyBinding(Key.A).IsReleased(_input).Should().BeTrue();
        }

        [Fact]
        public void ReadValue_ReturnsOneWhenDown()
        {
            _input.IsKeyDown(Key.W).Returns(true);
            new KeyBinding(Key.W).ReadValue(_input).Should().Be(1f);
        }

        [Fact]
        public void ReadValue_ReturnsZeroWhenUp()
        {
            _input.IsKeyDown(Key.W).Returns(false);
            new KeyBinding(Key.W).ReadValue(_input).Should().Be(0f);
        }

        [Fact]
        public void Equality_SameKey_AreEqual()
        {
            var a = new KeyBinding(Key.Space);
            var b = new KeyBinding(Key.Space);
            a.Should().Be(b);
        }

        [Fact]
        public void Equality_DifferentKey_AreNotEqual()
        {
            new KeyBinding(Key.A).Should().NotBe(new KeyBinding(Key.B));
        }
    }

    public class KeyAxisBindingTests : InputBindingTests
    {
        [Fact]
        public void ReadValue_PositiveOnly_ReturnsOne()
        {
            _input.IsKeyDown(Key.D).Returns(true);
            _input.IsKeyDown(Key.A).Returns(false);
            new KeyAxisBinding(Key.D, Key.A).ReadValue(_input).Should().Be(1f);
        }

        [Fact]
        public void ReadValue_NegativeOnly_ReturnsNegativeOne()
        {
            _input.IsKeyDown(Key.D).Returns(false);
            _input.IsKeyDown(Key.A).Returns(true);
            new KeyAxisBinding(Key.D, Key.A).ReadValue(_input).Should().Be(-1f);
        }

        [Fact]
        public void ReadValue_BothDown_ReturnsZero()
        {
            _input.IsKeyDown(Key.D).Returns(true);
            _input.IsKeyDown(Key.A).Returns(true);
            new KeyAxisBinding(Key.D, Key.A).ReadValue(_input).Should().Be(0f);
        }

        [Fact]
        public void ReadValue_NeitherDown_ReturnsZero()
        {
            _input.IsKeyDown(Key.D).Returns(false);
            _input.IsKeyDown(Key.A).Returns(false);
            new KeyAxisBinding(Key.D, Key.A).ReadValue(_input).Should().Be(0f);
        }

        [Fact]
        public void IsDown_EitherKey_ReturnsTrue()
        {
            _input.IsKeyDown(Key.D).Returns(false);
            _input.IsKeyDown(Key.A).Returns(true);
            new KeyAxisBinding(Key.D, Key.A).IsDown(_input).Should().BeTrue();
        }

        [Fact]
        public void IsDown_NeitherKey_ReturnsFalse()
        {
            new KeyAxisBinding(Key.D, Key.A).IsDown(_input).Should().BeFalse();
        }

        [Fact]
        public void IsPressed_EitherKey_ReturnsTrue()
        {
            _input.IsKeyPressed(Key.A).Returns(true);
            new KeyAxisBinding(Key.D, Key.A).IsPressed(_input).Should().BeTrue();
        }

        [Fact]
        public void IsReleased_EitherKey_ReturnsTrue()
        {
            _input.IsKeyReleased(Key.D).Returns(true);
            new KeyAxisBinding(Key.D, Key.A).IsReleased(_input).Should().BeTrue();
        }
    }

    public class CompositeKeyBindingTests : InputBindingTests
    {
        [Fact]
        public void Constructor_LessThanTwoKeys_Throws()
        {
            var act = () => new CompositeKeyBinding(Key.A);
            act.Should().Throw<ArgumentException>();
        }

        [Fact]
        public void Constructor_NullKeys_Throws()
        {
            var act = () => new CompositeKeyBinding(null!);
            act.Should().Throw<ArgumentNullException>();
        }

        [Fact]
        public void IsDown_AllKeysDown_ReturnsTrue()
        {
            _input.IsKeyDown(Key.LeftControl).Returns(true);
            _input.IsKeyDown(Key.S).Returns(true);
            new CompositeKeyBinding(Key.LeftControl, Key.S).IsDown(_input).Should().BeTrue();
        }

        [Fact]
        public void IsDown_OneKeyUp_ReturnsFalse()
        {
            _input.IsKeyDown(Key.LeftControl).Returns(true);
            _input.IsKeyDown(Key.S).Returns(false);
            new CompositeKeyBinding(Key.LeftControl, Key.S).IsDown(_input).Should().BeFalse();
        }

        [Fact]
        public void IsPressed_FinalKeyCompletesCombo_ReturnsTrue()
        {
            _input.IsKeyDown(Key.LeftControl).Returns(true);
            _input.IsKeyPressed(Key.LeftControl).Returns(false);
            _input.IsKeyDown(Key.S).Returns(true);
            _input.IsKeyPressed(Key.S).Returns(true);
            new CompositeKeyBinding(Key.LeftControl, Key.S).IsPressed(_input).Should().BeTrue();
        }

        [Fact]
        public void IsPressed_NoKeyPressedThisFrame_ReturnsFalse()
        {
            _input.IsKeyDown(Key.LeftControl).Returns(true);
            _input.IsKeyDown(Key.S).Returns(true);
            new CompositeKeyBinding(Key.LeftControl, Key.S).IsPressed(_input).Should().BeFalse();
        }

        [Fact]
        public void IsPressed_KeyPressedButOtherNotDown_ReturnsFalse()
        {
            _input.IsKeyPressed(Key.S).Returns(true);
            _input.IsKeyDown(Key.LeftControl).Returns(false);
            new CompositeKeyBinding(Key.LeftControl, Key.S).IsPressed(_input).Should().BeFalse();
        }

        [Fact]
        public void IsReleased_OneKeyReleasedWhileOtherDown_ReturnsTrue()
        {
            _input.IsKeyReleased(Key.S).Returns(true);
            _input.IsKeyDown(Key.LeftControl).Returns(true);
            new CompositeKeyBinding(Key.LeftControl, Key.S).IsReleased(_input).Should().BeTrue();
        }

        [Fact]
        public void IsReleased_KeyReleasedButOtherAlsoUp_ReturnsFalse()
        {
            _input.IsKeyReleased(Key.S).Returns(true);
            _input.IsKeyDown(Key.LeftControl).Returns(false);
            new CompositeKeyBinding(Key.LeftControl, Key.S).IsReleased(_input).Should().BeFalse();
        }

        [Fact]
        public void ReadValue_AllDown_ReturnsOne()
        {
            _input.IsKeyDown(Key.LeftControl).Returns(true);
            _input.IsKeyDown(Key.S).Returns(true);
            new CompositeKeyBinding(Key.LeftControl, Key.S).ReadValue(_input).Should().Be(1f);
        }

        [Fact]
        public void ReadValue_NotAllDown_ReturnsZero()
        {
            _input.IsKeyDown(Key.LeftControl).Returns(true);
            new CompositeKeyBinding(Key.LeftControl, Key.S).ReadValue(_input).Should().Be(0f);
        }

        [Fact]
        public void Equality_SameKeys_AreEqual()
        {
            var a = new CompositeKeyBinding(Key.LeftControl, Key.S);
            var b = new CompositeKeyBinding(Key.LeftControl, Key.S);
            a.Should().Be(b);
            a.GetHashCode().Should().Be(b.GetHashCode());
        }

        [Fact]
        public void Equality_DifferentKeys_AreNotEqual()
        {
            var a = new CompositeKeyBinding(Key.LeftControl, Key.S);
            var b = new CompositeKeyBinding(Key.LeftControl, Key.A);
            a.Should().NotBe(b);
        }

        [Fact]
        public void Equality_DifferentOrder_AreNotEqual()
        {
            var a = new CompositeKeyBinding(Key.LeftControl, Key.S);
            var b = new CompositeKeyBinding(Key.S, Key.LeftControl);
            a.Should().NotBe(b);
        }

        [Fact]
        public void Equality_Null_IsNotEqual()
        {
            var a = new CompositeKeyBinding(Key.LeftControl, Key.S);
            a.Equals((CompositeKeyBinding?)null).Should().BeFalse();
        }
    }

    public class MouseButtonBindingTests : InputBindingTests
    {
        [Fact]
        public void IsDown_DelegatesToContext()
        {
            _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
            new MouseButtonBinding(MouseButton.Left).IsDown(_input).Should().BeTrue();
        }

        [Fact]
        public void IsPressed_DelegatesToContext()
        {
            _input.IsMouseButtonPressed(MouseButton.Right).Returns(true);
            new MouseButtonBinding(MouseButton.Right).IsPressed(_input).Should().BeTrue();
        }

        [Fact]
        public void IsReleased_DelegatesToContext()
        {
            _input.IsMouseButtonReleased(MouseButton.Middle).Returns(true);
            new MouseButtonBinding(MouseButton.Middle).IsReleased(_input).Should().BeTrue();
        }

        [Fact]
        public void ReadValue_ReturnsOneWhenDown()
        {
            _input.IsMouseButtonDown(MouseButton.Left).Returns(true);
            new MouseButtonBinding(MouseButton.Left).ReadValue(_input).Should().Be(1f);
        }
    }

    public class GamepadButtonBindingTests : InputBindingTests
    {
        [Fact]
        public void IsDown_DelegatesToContext()
        {
            _input.IsGamepadButtonDown(GamepadButton.A, 0).Returns(true);
            new GamepadButtonBinding(GamepadButton.A).IsDown(_input).Should().BeTrue();
        }

        [Fact]
        public void IsDown_UsesGamepadIndex()
        {
            _input.IsGamepadButtonDown(GamepadButton.A, 1).Returns(true);
            new GamepadButtonBinding(GamepadButton.A, 1).IsDown(_input).Should().BeTrue();
        }

        [Fact]
        public void ReadValue_ReturnsOneWhenDown()
        {
            _input.IsGamepadButtonDown(GamepadButton.A, 0).Returns(true);
            new GamepadButtonBinding(GamepadButton.A).ReadValue(_input).Should().Be(1f);
        }
    }

    public class GamepadAxisBindingTests : InputBindingTests
    {
        [Fact]
        public void IsDown_AboveDeadzone_ReturnsTrue()
        {
            _input.GetGamepadAxis(GamepadAxis.LeftX, 0).Returns(0.5f);
            _input.GamepadDeadzone.Returns(0.15f);
            new GamepadAxisBinding(GamepadAxis.LeftX).IsDown(_input).Should().BeTrue();
        }

        [Fact]
        public void IsDown_BelowDeadzone_ReturnsFalse()
        {
            _input.GetGamepadAxis(GamepadAxis.LeftX, 0).Returns(0.1f);
            _input.GamepadDeadzone.Returns(0.15f);
            new GamepadAxisBinding(GamepadAxis.LeftX).IsDown(_input).Should().BeFalse();
        }

        [Fact]
        public void ReadValue_BelowDeadzone_ReturnsZero()
        {
            _input.GetGamepadAxis(GamepadAxis.LeftX, 0).Returns(0.1f);
            _input.GamepadDeadzone.Returns(0.15f);
            new GamepadAxisBinding(GamepadAxis.LeftX).ReadValue(_input).Should().Be(0f);
        }

        [Fact]
        public void ReadValue_FullPositive_ReturnsOne()
        {
            _input.GetGamepadAxis(GamepadAxis.LeftX, 0).Returns(1f);
            _input.GamepadDeadzone.Returns(0.15f);
            new GamepadAxisBinding(GamepadAxis.LeftX).ReadValue(_input).Should().Be(1f);
        }

        [Fact]
        public void ReadValue_FullNegative_ReturnsNegativeOne()
        {
            _input.GetGamepadAxis(GamepadAxis.LeftX, 0).Returns(-1f);
            _input.GamepadDeadzone.Returns(0.15f);
            new GamepadAxisBinding(GamepadAxis.LeftX).ReadValue(_input).Should().Be(-1f);
        }

        [Fact]
        public void ReadValue_MidRange_RescalesCorrectly()
        {
            _input.GamepadDeadzone.Returns(0.2f);
            _input.GetGamepadAxis(GamepadAxis.LeftX, 0).Returns(0.6f);

            float expected = (0.6f - 0.2f) / (1f - 0.2f);
            new GamepadAxisBinding(GamepadAxis.LeftX).ReadValue(_input).Should().BeApproximately(expected, 0.001f);
        }

        [Fact]
        public void ReadValue_NegativeMidRange_RescalesWithSign()
        {
            _input.GamepadDeadzone.Returns(0.2f);
            _input.GetGamepadAxis(GamepadAxis.LeftX, 0).Returns(-0.6f);

            float expected = -((0.6f - 0.2f) / (1f - 0.2f));
            new GamepadAxisBinding(GamepadAxis.LeftX).ReadValue(_input).Should().BeApproximately(expected, 0.001f);
        }

        [Fact]
        public void IsPressed_DelegatesToContext()
        {
            _input.IsGamepadAxisPressed(GamepadAxis.LeftX, 0).Returns(true);
            new GamepadAxisBinding(GamepadAxis.LeftX).IsPressed(_input).Should().BeTrue();
        }

        [Fact]
        public void IsReleased_DelegatesToContext()
        {
            _input.IsGamepadAxisReleased(GamepadAxis.LeftX, 0).Returns(true);
            new GamepadAxisBinding(GamepadAxis.LeftX).IsReleased(_input).Should().BeTrue();
        }
    }

    public class MouseScrollBindingTests : InputBindingTests
    {
        [Fact]
        public void IsDown_WhenScrolled_ReturnsTrue()
        {
            _input.ScrollWheelDelta.Returns(1.0f);
            new MouseScrollBinding().IsDown(_input).Should().BeTrue();
        }

        [Fact]
        public void IsDown_WhenNotScrolled_ReturnsFalse()
        {
            _input.ScrollWheelDelta.Returns(0f);
            new MouseScrollBinding().IsDown(_input).Should().BeFalse();
        }

        [Fact]
        public void ReadValue_ReturnsScrollDelta()
        {
            _input.ScrollWheelDelta.Returns(-2.5f);
            new MouseScrollBinding().ReadValue(_input).Should().Be(-2.5f);
        }

        [Fact]
        public void IsReleased_AlwaysFalse()
        {
            new MouseScrollBinding().IsReleased(_input).Should().BeFalse();
        }
    }

    public class MouseDeltaBindingTests : InputBindingTests
    {
        [Fact]
        public void ReadValue_X_ReturnsHorizontalDelta()
        {
            _input.MouseDelta.Returns(new System.Numerics.Vector2(5.5f, -3.0f));
            new MouseDeltaBinding(MouseDeltaAxis.X).ReadValue(_input).Should().Be(5.5f);
        }

        [Fact]
        public void ReadValue_Y_ReturnsVerticalDelta()
        {
            _input.MouseDelta.Returns(new System.Numerics.Vector2(5.5f, -3.0f));
            new MouseDeltaBinding(MouseDeltaAxis.Y).ReadValue(_input).Should().Be(-3.0f);
        }

        [Fact]
        public void IsDown_WhenDeltaNonZero_ReturnsTrue()
        {
            _input.MouseDelta.Returns(new System.Numerics.Vector2(1.0f, 0f));
            new MouseDeltaBinding(MouseDeltaAxis.X).IsDown(_input).Should().BeTrue();
        }

        [Fact]
        public void IsDown_WhenDeltaZero_ReturnsFalse()
        {
            _input.MouseDelta.Returns(System.Numerics.Vector2.Zero);
            new MouseDeltaBinding(MouseDeltaAxis.X).IsDown(_input).Should().BeFalse();
        }

        [Fact]
        public void IsReleased_AlwaysFalse()
        {
            new MouseDeltaBinding(MouseDeltaAxis.X).IsReleased(_input).Should().BeFalse();
        }
    }
}