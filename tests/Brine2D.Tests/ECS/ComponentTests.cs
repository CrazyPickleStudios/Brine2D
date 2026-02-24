using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Xunit;

namespace Brine2D.Tests.ECS;

public class ComponentTests : TestBase
{
    #region Attachment

    [Fact]
    public void Entity_WhenNotAttached_IsNull()
    {
        var component = new TestComponent();

        Assert.Null(component.Entity);
    }

    [Fact]
    public void Entity_WhenAttached_IsSet()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();

        // Assert
        var component = entity.GetComponent<TestComponent>()!;
        Assert.Equal(entity, component.Entity);
    }

    #endregion

    #region IsEnabled

    [Fact]
    public void IsEnabled_DefaultsToTrue()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();

        // Assert
        Assert.True(entity.GetComponent<TestComponent>()!.IsEnabled);
    }

    [Fact]
    public void IsEnabled_CanBeSetToFalse()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act
        component.IsEnabled = false;

        // Assert
        Assert.False(component.IsEnabled);
    }

    [Fact]
    public void IsEnabled_CanBeToggled()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        component.IsEnabled = false;
        Assert.False(component.IsEnabled);

        component.IsEnabled = true;
        Assert.True(component.IsEnabled);
    }

    #endregion

    #region Lifecycle

    [Fact]
    public void OnAdded_CalledWhenComponentAdded()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act
        entity.AddComponent<TestComponent>();
        world.Flush();

        // Assert
        Assert.True(entity.GetComponent<TestComponent>()!.OnAddedCalled);
    }

    [Fact]
    public void OnRemoved_CalledWhenComponentRemoved()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act
        entity.RemoveComponent<TestComponent>();

        // Assert
        Assert.True(component.OnRemovedCalled);
    }

    #endregion

    #region Test Helper

    private class TestComponent : Component
    {
        public bool OnAddedCalled { get; private set; }
        public bool OnRemovedCalled { get; private set; }

        protected internal override void OnAdded() => OnAddedCalled = true;
        protected internal override void OnRemoved() => OnRemovedCalled = true;
    }

    #endregion
}