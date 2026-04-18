using Brine2D.Input;
using FluentAssertions;
using NSubstitute;

namespace Brine2D.Tests.Input;

public class InputActionMapTests
{
    private readonly IInputContext _input = Substitute.For<IInputContext>();

    [Fact]
    public void Constructor_NullName_Throws()
    {
        var act = () => new InputActionMap(null!);
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Constructor_WhitespaceName_Throws()
    {
        var act = () => new InputActionMap("  ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Name_ReturnsConstructorValue()
    {
        new InputActionMap("Player").Name.Should().Be("Player");
    }

    [Fact]
    public void Enabled_DefaultsToTrue()
    {
        new InputActionMap("Player").Enabled.Should().BeTrue();
    }

    [Fact]
    public void AddAction_Null_Throws()
    {
        var map = new InputActionMap("Player");
        var act = () => map.AddAction(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddAction_DuplicateName_Throws()
    {
        var map = new InputActionMap("Player");
        map.AddAction(new InputAction("Jump"));
        var act = () => map.AddAction(new InputAction("Jump"));
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void Indexer_ExistingAction_ReturnsIt()
    {
        var map = new InputActionMap("Player");
        var jump = new InputAction("Jump");
        map.AddAction(jump);
        map["Jump"].Should().BeSameAs(jump);
    }

    [Fact]
    public void Indexer_MissingAction_ThrowsKeyNotFound()
    {
        var map = new InputActionMap("Player");
        var act = () => { _ = map["Nonexistent"]; };
        act.Should().Throw<KeyNotFoundException>();
    }

    [Fact]
    public void TryGetAction_Found_ReturnsTrue()
    {
        var map = new InputActionMap("Player");
        map.AddAction(new InputAction("Jump"));
        map.TryGetAction("Jump", out var action).Should().BeTrue();
        action.Should().NotBeNull();
    }

    [Fact]
    public void TryGetAction_NotFound_ReturnsFalse()
    {
        var map = new InputActionMap("Player");
        map.TryGetAction("Jump", out _).Should().BeFalse();
    }

    [Fact]
    public void RemoveAction_Existing_ReturnsTrue()
    {
        var map = new InputActionMap("Player");
        map.AddAction(new InputAction("Jump"));
        map.RemoveAction("Jump").Should().BeTrue();
    }

    [Fact]
    public void RemoveAction_Nonexistent_ReturnsFalse()
    {
        var map = new InputActionMap("Player");
        map.RemoveAction("Jump").Should().BeFalse();
    }

    [Fact]
    public void Actions_ReturnsSnapshot()
    {
        var map = new InputActionMap("Player");
        map.AddAction(new InputAction("Jump"));
        map.Actions.Should().ContainKey("Jump");
    }

    [Fact]
    public void IsDown_WhenEnabled_DelegatesToAction()
    {
        _input.IsKeyDown(Key.Space).Returns(true);
        var map = new InputActionMap("Player");
        map.AddAction(new InputAction("Jump", new KeyBinding(Key.Space)));
        map.IsDown("Jump", _input).Should().BeTrue();
    }

    [Fact]
    public void IsDown_WhenDisabled_ReturnsFalse()
    {
        _input.IsKeyDown(Key.Space).Returns(true);
        var map = new InputActionMap("Player") { Enabled = false };
        map.AddAction(new InputAction("Jump", new KeyBinding(Key.Space)));
        map.IsDown("Jump", _input).Should().BeFalse();
    }

    [Fact]
    public void IsPressed_WhenEnabled_DelegatesToAction()
    {
        _input.IsKeyPressed(Key.Space).Returns(true);
        var map = new InputActionMap("Player");
        map.AddAction(new InputAction("Jump", new KeyBinding(Key.Space)));
        map.IsPressed("Jump", _input).Should().BeTrue();
    }

    [Fact]
    public void IsPressed_WhenDisabled_ReturnsFalse()
    {
        _input.IsKeyPressed(Key.Space).Returns(true);
        var map = new InputActionMap("Player") { Enabled = false };
        map.AddAction(new InputAction("Jump", new KeyBinding(Key.Space)));
        map.IsPressed("Jump", _input).Should().BeFalse();
    }

    [Fact]
    public void IsReleased_WhenEnabled_DelegatesToAction()
    {
        _input.IsKeyReleased(Key.Space).Returns(true);
        var map = new InputActionMap("Player");
        map.AddAction(new InputAction("Jump", new KeyBinding(Key.Space)));
        map.IsReleased("Jump", _input).Should().BeTrue();
    }

    [Fact]
    public void IsReleased_WhenDisabled_ReturnsFalse()
    {
        _input.IsKeyReleased(Key.Space).Returns(true);
        var map = new InputActionMap("Player") { Enabled = false };
        map.AddAction(new InputAction("Jump", new KeyBinding(Key.Space)));
        map.IsReleased("Jump", _input).Should().BeFalse();
    }

    [Fact]
    public void ReadValue_WhenEnabled_DelegatesToAction()
    {
        _input.IsKeyDown(Key.Space).Returns(true);
        var map = new InputActionMap("Player");
        map.AddAction(new InputAction("Jump", new KeyBinding(Key.Space)));
        map.ReadValue("Jump", _input).Should().Be(1f);
    }

    [Fact]
    public void ReadValue_WhenDisabled_ReturnsZero()
    {
        _input.IsKeyDown(Key.Space).Returns(true);
        var map = new InputActionMap("Player") { Enabled = false };
        map.AddAction(new InputAction("Jump", new KeyBinding(Key.Space)));
        map.ReadValue("Jump", _input).Should().Be(0f);
    }

    [Fact]
    public void Enabled_CanBeToggled()
    {
        var map = new InputActionMap("Player");
        map.Enabled = false;
        map.Enabled.Should().BeFalse();
        map.Enabled = true;
        map.Enabled.Should().BeTrue();
    }
}