using Brine2D.Animation;
using Brine2D.Core;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.Animation;

public class AnimatorComponentTests
{
    [Fact]
    public void HasLayer_ReturnsFalse_WhenNotAdded()
    {
        var comp = new AnimatorComponent();
        comp.HasLayer("upper-body").Should().BeFalse();
    }

    [Fact]
    public void HasLayer_ReturnsTrue_AfterAddLayer()
    {
        var comp = new AnimatorComponent();
        comp.AddLayer("upper-body");
        comp.HasLayer("upper-body").Should().BeTrue();
    }

    [Fact]
    public void HasLayer_ReturnsFalse_AfterRemoveLayer()
    {
        var comp = new AnimatorComponent();
        comp.AddLayer("upper-body");
        comp.RemoveLayer("upper-body");
        comp.HasLayer("upper-body").Should().BeFalse();
    }

    [Fact]
    public void Parameters_IsNotNull_OnConstruction()
    {
        new AnimatorComponent().Parameters.Should().NotBeNull();
    }

    [Fact]
    public void Parameters_IsSameInstance_AcrossMultipleCalls()
    {
        var comp = new AnimatorComponent();
        comp.Parameters.Should().BeSameAs(comp.Parameters);
    }

    [Fact]
    public void Parameters_SetAndGet_RoundTrip()
    {
        var comp = new AnimatorComponent();
        comp.Parameters.SetFloat("speed", 2.5f);
        comp.Parameters.GetFloat("speed").Should().BeApproximately(2.5f, 0.0001f);
    }

    [Fact]
    public void Parameters_CanDriveStateMachineTransition()
    {
        var comp = new AnimatorComponent();
        var idle = new AnimationClip("idle") { PlaybackMode = PlaybackMode.Loop };
        idle.AddFrame(new SpriteFrame(new Rectangle(0, 0, 16, 16), 0.1f));
        var walk = new AnimationClip("walk") { PlaybackMode = PlaybackMode.Loop };
        walk.AddFrame(new SpriteFrame(new Rectangle(16, 0, 16, 16), 0.1f));
        comp.Animator.AddAnimation(idle);
        comp.Animator.AddAnimation(walk);
        comp.Animator.Play("idle");

        comp.StateMachine.AddTransition("idle", "walk",
            () => comp.Parameters.GetFloat("speed") > 0.1f, canInterrupt: true);

        comp.Parameters.SetFloat("speed", 1f);
        comp.StateMachine.Update(0.016f);

        comp.Animator.CurrentAnimation!.Name.Should().Be("walk");
    }

    [Fact]
    public void AddLayer_DefaultPriority_ReturnsLayerWithPriority1()
    {
        var comp = new AnimatorComponent();
        var layer = comp.AddLayer("upper-body");
        layer.Should().NotBeNull();
        layer.Name.Should().Be("upper-body");
        layer.Priority.Should().Be(1);
    }

    [Fact]
    public void AddLayer_ZeroPriority_Throws()
    {
        var comp = new AnimatorComponent();
        var act = () => comp.AddLayer("bad", priority: 0);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("priority");
    }

    [Fact]
    public void AddLayer_NegativePriority_Throws()
    {
        var comp = new AnimatorComponent();
        var act = () => comp.AddLayer("bad", priority: -5);
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("priority");
    }

    [Fact]
    public void AddLayer_MultipleLayers_SortedByPriorityAscending()
    {
        var comp = new AnimatorComponent();
        comp.AddLayer("c", priority: 3);
        comp.AddLayer("a", priority: 1);
        comp.AddLayer("b", priority: 2);

        comp.Layers[0].Priority.Should().Be(1);
        comp.Layers[1].Priority.Should().Be(2);
        comp.Layers[2].Priority.Should().Be(3);
    }

    [Fact]
    public void RemoveLayer_ExistingName_ReturnsTrue_AndRemovesFromList()
    {
        var comp = new AnimatorComponent();
        comp.AddLayer("overlay");
        comp.RemoveLayer("overlay").Should().BeTrue();
        comp.Layers.Should().BeEmpty();
    }

    [Fact]
    public void RemoveLayer_NonExistentName_ReturnsFalse()
    {
        var comp = new AnimatorComponent();
        comp.RemoveLayer("ghost").Should().BeFalse();
    }

    [Fact]
    public void GetLayer_ExistingName_ReturnsLayer()
    {
        var comp = new AnimatorComponent();
        comp.AddLayer("head");
        comp.GetLayer("head").Should().NotBeNull();
    }

    [Fact]
    public void GetLayer_NonExistentName_ReturnsNull()
    {
        var comp = new AnimatorComponent();
        comp.GetLayer("missing").Should().BeNull();
    }

    [Fact]
    public void AnimationLayer_SetPriority_BelowOne_Throws()
    {
        var comp = new AnimatorComponent();
        var layer = comp.AddLayer("fx", priority: 2);
        var act = () => layer.Priority = 0;
        act.Should().Throw<ArgumentOutOfRangeException>().WithParameterName("value");
    }

    [Fact]
    public void AnimationLayer_SetPriority_ValidValue_Updates()
    {
        var comp = new AnimatorComponent();
        var layer = comp.AddLayer("fx", priority: 1);
        layer.Priority = 5;
        layer.Priority.Should().Be(5);
    }
}