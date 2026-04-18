using Brine2D.Input;
using FluentAssertions;
using NSubstitute;

namespace Brine2D.Tests.Input;

public class InputLayerManagerTests
{
    private readonly IInputContext _input = Substitute.For<IInputContext>();

    private IInputLayer CreateLayer(int priority, bool consumeKb = false, bool consumeMouse = false, bool consumeGp = false)
    {
        var layer = Substitute.For<IInputLayer>();
        layer.Priority.Returns(priority);
        layer.ProcessKeyboardInput(Arg.Any<IInputContext>(), Arg.Any<bool>()).Returns(consumeKb);
        layer.ProcessMouseInput(Arg.Any<IInputContext>(), Arg.Any<bool>()).Returns(consumeMouse);
        layer.ProcessGamepadInput(Arg.Any<IInputContext>(), Arg.Any<bool>()).Returns(consumeGp);
        return layer;
    }

    [Fact]
    public void Constructor_NullInput_Throws()
    {
        var act = () => new InputLayerManager(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ProcessInput_NoLayers_NothingConsumed()
    {
        var manager = new InputLayerManager(_input);
        manager.ProcessInput();
        manager.KeyboardConsumed.Should().BeFalse();
        manager.MouseConsumed.Should().BeFalse();
        manager.GamepadConsumed.Should().BeFalse();
    }

    [Fact]
    public void ProcessInput_LayerConsumesKeyboard_KeyboardConsumedIsTrue()
    {
        var manager = new InputLayerManager(_input);
        manager.RegisterLayer(CreateLayer(100, consumeKb: true));
        manager.ProcessInput();
        manager.KeyboardConsumed.Should().BeTrue();
        manager.MouseConsumed.Should().BeFalse();
    }

    [Fact]
    public void ProcessInput_LayerConsumesMouse_MouseConsumedIsTrue()
    {
        var manager = new InputLayerManager(_input);
        manager.RegisterLayer(CreateLayer(100, consumeMouse: true));
        manager.ProcessInput();
        manager.MouseConsumed.Should().BeTrue();
    }

    [Fact]
    public void ProcessInput_LayerConsumesGamepad_GamepadConsumedIsTrue()
    {
        var manager = new InputLayerManager(_input);
        manager.RegisterLayer(CreateLayer(100, consumeGp: true));
        manager.ProcessInput();
        manager.GamepadConsumed.Should().BeTrue();
    }

    [Fact]
    public void ProcessInput_HigherPriorityProcessedFirst()
    {
        var manager = new InputLayerManager(_input);
        var lowLayer = CreateLayer(0);
        var highLayer = CreateLayer(1000, consumeKb: true);

        manager.RegisterLayer(lowLayer);
        manager.RegisterLayer(highLayer);
        manager.ProcessInput();

        Received.InOrder(() =>
        {
            highLayer.ProcessKeyboardInput(_input, false);
            lowLayer.ProcessKeyboardInput(_input, true);
        });
    }

    [Fact]
    public void ProcessInput_AllLayersAlwaysCalled()
    {
        var manager = new InputLayerManager(_input);
        var highLayer = CreateLayer(1000, consumeKb: true, consumeMouse: true, consumeGp: true);
        var lowLayer = CreateLayer(0);

        manager.RegisterLayer(highLayer);
        manager.RegisterLayer(lowLayer);
        manager.ProcessInput();

        lowLayer.Received(1).ProcessKeyboardInput(_input, true);
        lowLayer.Received(1).ProcessMouseInput(_input, true);
        lowLayer.Received(1).ProcessGamepadInput(_input, true);
    }

    [Fact]
    public void ProcessInput_LowerLayerReceivesConsumedTrue()
    {
        var manager = new InputLayerManager(_input);
        var highLayer = CreateLayer(1000, consumeKb: true);
        var lowLayer = CreateLayer(0);

        manager.RegisterLayer(highLayer);
        manager.RegisterLayer(lowLayer);
        manager.ProcessInput();

        lowLayer.Received(1).ProcessKeyboardInput(_input, true);
    }

    [Fact]
    public void ProcessInput_FirstLayerReceivesConsumedFalse()
    {
        var manager = new InputLayerManager(_input);
        var layer = CreateLayer(100);

        manager.RegisterLayer(layer);
        manager.ProcessInput();

        layer.Received(1).ProcessKeyboardInput(_input, false);
        layer.Received(1).ProcessMouseInput(_input, false);
        layer.Received(1).ProcessGamepadInput(_input, false);
    }

    [Fact]
    public void RegisterLayer_DuplicateIgnored()
    {
        var manager = new InputLayerManager(_input);
        var layer = CreateLayer(100, consumeKb: true);

        manager.RegisterLayer(layer);
        manager.RegisterLayer(layer);
        manager.ProcessInput();

        layer.Received(1).ProcessKeyboardInput(Arg.Any<IInputContext>(), Arg.Any<bool>());
    }

    [Fact]
    public void UnregisterLayer_RemovedLayerNotCalled()
    {
        var manager = new InputLayerManager(_input);
        var layer = CreateLayer(100, consumeKb: true);

        manager.RegisterLayer(layer);
        manager.UnregisterLayer(layer);
        manager.ProcessInput();

        layer.DidNotReceive().ProcessKeyboardInput(Arg.Any<IInputContext>(), Arg.Any<bool>());
    }

    [Fact]
    public void UnregisterLayer_NonexistentLayer_NoOp()
    {
        var manager = new InputLayerManager(_input);
        var act = () => manager.UnregisterLayer(CreateLayer(0));
        act.Should().NotThrow();
    }

    [Fact]
    public void ProcessInput_MultipleLayersCanConsumeIndependently()
    {
        var manager = new InputLayerManager(_input);
        var uiLayer = CreateLayer(1000, consumeKb: true, consumeMouse: true);
        var gameLayer = CreateLayer(0, consumeGp: true);

        manager.RegisterLayer(uiLayer);
        manager.RegisterLayer(gameLayer);
        manager.ProcessInput();

        manager.KeyboardConsumed.Should().BeTrue();
        manager.MouseConsumed.Should().BeTrue();
        manager.GamepadConsumed.Should().BeTrue();
    }
}