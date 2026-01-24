using Brine2D.ECS;
using Brine2D.ECS.Components;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.ECS;

public class EntityTests
{
    [Fact]
    public void ShouldAddComponentAndRetrieveIt()
    {
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var component = entity.AddComponent(new TestComponent());

        entity.HasComponent<TestComponent>().Should().BeTrue();
        entity.GetComponent<TestComponent>().Should().Be(component);
    }

    [Fact]
    public void ShouldNotAddDuplicateComponent()
    {
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var comp1 = entity.AddComponent(new TestComponent());
        var comp2 = entity.AddComponent(new TestComponent());

        comp2.Should().Be(comp1);
        entity.GetAllComponents().OfType<TestComponent>().Count().Should().Be(1);
    }

    [Fact]
    public void ShouldAddPreconfiguredComponentInstance()
    {
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var comp = new TestComponent();
        entity.AddComponent(comp);

        entity.GetComponent<TestComponent>().Should().Be(comp);
    }

    [Fact]
    public void ShouldRemoveComponentAndNotRetrieveIt()
    {
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        entity.AddComponent(new TestComponent());

        entity.RemoveComponent<TestComponent>().Should().BeTrue();
        entity.HasComponent<TestComponent>().Should().BeFalse();
    }

    [Fact]
    public void ShouldReturnAllComponents()
    {
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        entity.AddComponent(new TestComponent());
        entity.AddComponent(new AnotherComponent());

        entity.GetAllComponents().Count.Should().Be(2);
    }

    [Fact]
    public void ShouldCheckForComponentExistenceByType()
    {
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        entity.AddComponent(new TestComponent());

        entity.HasComponent<TestComponent>().Should().BeTrue();
        entity.HasComponent<AnotherComponent>().Should().BeFalse();
    }

    [Fact]
    public void ShouldCheckForComponentExistenceByTypeObject()
    {
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        entity.AddComponent(new TestComponent());

        entity.HasComponent(typeof(TestComponent)).Should().BeTrue();
        entity.HasComponent(typeof(AnotherComponent)).Should().BeFalse();
    }

    [Fact]
    public void ShouldFireOnComponentAddedEventWhenComponentAdded()
    {
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        var eventFired = false;
        entity.OnComponentAdded += (_, _) => eventFired = true;

        entity.AddComponent(new TestComponent());

        eventFired.Should().BeTrue();
    }

    [Fact]
    public void ShouldFireOnComponentRemovedEventWhenComponentRemoved()
    {
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        entity.AddComponent(new TestComponent());
        var eventFired = false;
        entity.OnComponentRemoved += (_, _) => eventFired = true;

        entity.RemoveComponent<TestComponent>();

        eventFired.Should().BeTrue();
    }

    [Fact]
    public void ShouldAddAndCheckTags()
    {
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        entity.Tags.Add("player");
        entity.Tags.Contains("player").Should().BeTrue();
    }

    [Fact]
    public void ShouldHaveUniqueIdForEachEntity()
    {
        var world = new EntityWorld();
        var e1 = world.CreateEntity();
        var e2 = world.CreateEntity();
        e1.Id.Should().NotBe(e2.Id);
    }

    [Fact]
    public void ShouldSetAndGetNameProperty()
    {
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        entity.Name = "Player";
        entity.Name.Should().Be("Player");
    }

    [Fact]
    public void ToStringShouldReturnNameAndId()
    {
        var world = new EntityWorld();
        var entity = world.CreateEntity();
        entity.Name = "Test";
        entity.ToString().Should().Contain("Test");
        entity.ToString().Should().Contain(entity.Id.ToString());
    }

    // Add more tests for activation, deactivation, OnDestroy, World integration, etc.

    public class TestComponent : Component { }
    public class AnotherComponent : Component { }
}