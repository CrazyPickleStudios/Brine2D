using Brine2D.Input;
using FluentAssertions;
using NSubstitute;

namespace Brine2D.Tests.Input;

public class InputActionTests
{
    private readonly IInputContext _input = Substitute.For<IInputContext>();

    [Fact]
    public void Constructor_NullName_Throws()
    {
        var act = () => new InputAction(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WhitespaceName_Throws()
    {
        var act = () => new InputAction("  ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Name_ReturnsConstructorValue()
    {
        new InputAction("Jump").Name.Should().Be("Jump");
    }

    [Fact]
    public void Bindings_ReturnsSnapshot()
    {
        var action = new InputAction("Jump", new KeyBinding(Key.Space));
        action.Bindings.Should().HaveCount(1);
    }

    [Fact]
    public void IsDown_NoBindings_ReturnsFalse()
    {
        new InputAction("Jump").IsDown(_input).Should().BeFalse();
    }

    [Fact]
    public void IsDown_AnyBindingDown_ReturnsTrue()
    {
        _input.IsKeyDown(Key.Space).Returns(true);
        var action = new InputAction("Jump", new KeyBinding(Key.Space), new KeyBinding(Key.W));
        action.IsDown(_input).Should().BeTrue();
    }

    [Fact]
    public void IsPressed_AnyBindingPressed_ReturnsTrue()
    {
        _input.IsKeyPressed(Key.W).Returns(true);
        var action = new InputAction("Jump", new KeyBinding(Key.Space), new KeyBinding(Key.W));
        action.IsPressed(_input).Should().BeTrue();
    }

    [Fact]
    public void IsReleased_AnyBindingReleased_ReturnsTrue()
    {
        _input.IsKeyReleased(Key.Space).Returns(true);
        var action = new InputAction("Jump", new KeyBinding(Key.Space));
        action.IsReleased(_input).Should().BeTrue();
    }

    [Fact]
    public void ReadValue_ReturnsLargestMagnitude()
    {
        _input.IsKeyDown(Key.Space).Returns(true);
        _input.IsKeyDown(Key.D).Returns(true);
        _input.IsKeyDown(Key.A).Returns(true);

        var action = new InputAction("Move",
            new KeyBinding(Key.Space),
            new KeyAxisBinding(Key.D, Key.A));

        action.ReadValue(_input).Should().Be(1f);
    }

    [Fact]
    public void ReadValue_PrefersLargerAbsoluteValue()
    {
        _input.GetGamepadAxis(GamepadAxis.LeftX, 0).Returns(-0.8f);
        _input.GamepadDeadzone.Returns(0.15f);
        _input.IsKeyDown(Key.D).Returns(false);
        _input.IsKeyDown(Key.A).Returns(false);

        var action = new InputAction("Move",
            new KeyAxisBinding(Key.D, Key.A),
            new GamepadAxisBinding(GamepadAxis.LeftX));

        action.ReadValue(_input).Should().BeNegative();
    }

    [Fact]
    public void AddBinding_IncreasesCount()
    {
        var action = new InputAction("Jump");
        action.AddBinding(new KeyBinding(Key.Space));
        action.Bindings.Should().HaveCount(1);
    }

    [Fact]
    public void AddBinding_Null_Throws()
    {
        var action = new InputAction("Jump");
        var act = () => action.AddBinding(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void RemoveBinding_ExistingBinding_ReturnsTrue()
    {
        var binding = new KeyBinding(Key.Space);
        var action = new InputAction("Jump", binding);
        action.RemoveBinding(binding).Should().BeTrue();
        action.Bindings.Should().BeEmpty();
    }

    [Fact]
    public void RemoveBinding_NonexistentBinding_ReturnsFalse()
    {
        var action = new InputAction("Jump", new KeyBinding(Key.Space));
        action.RemoveBinding(new KeyBinding(Key.W)).Should().BeFalse();
    }

    [Fact]
    public void RemoveBinding_CompositeKeyBinding_ValueEquality_Works()
    {
        var action = new InputAction("Save", new CompositeKeyBinding(Key.LeftControl, Key.S));
        var toRemove = new CompositeKeyBinding(Key.LeftControl, Key.S);
        action.RemoveBinding(toRemove).Should().BeTrue();
        action.Bindings.Should().BeEmpty();
    }

    [Fact]
    public void ClearBindings_RemovesAll()
    {
        var action = new InputAction("Jump", new KeyBinding(Key.Space), new KeyBinding(Key.W));
        action.ClearBindings();
        action.Bindings.Should().BeEmpty();
    }

    [Fact]
    public void Bindings_SnapshotInvalidatedAfterAdd()
    {
        var action = new InputAction("Jump");
        var before = action.Bindings;
        action.AddBinding(new KeyBinding(Key.Space));
        var after = action.Bindings;
        after.Should().HaveCount(1);
        before.Should().NotBeSameAs(after);
    }
}