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

    [Fact]
    public void OnDisabled_CalledWhenIsEnabledSetToFalse()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TrackingComponent>();
        world.Flush();
        var component = entity.GetComponent<TrackingComponent>()!;

        component.IsEnabled = false;

        Assert.True(component.DisabledCalled);
    }

    [Fact]
    public void OnEnabled_CalledWhenIsEnabledSetToTrue()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TrackingComponent>();
        world.Flush();
        var component = entity.GetComponent<TrackingComponent>()!;

        component.IsEnabled = false;
        component.EnabledCalled = false;
        component.IsEnabled = true;

        Assert.True(component.EnabledCalled);
    }

    [Fact]
    public void OnEnabled_NotCalledWhenAlreadyEnabled()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TrackingComponent>();
        world.Flush();
        var component = entity.GetComponent<TrackingComponent>()!;

        component.IsEnabled = true;

        Assert.False(component.EnabledCalled);
    }

    [Fact]
    public void OnDisabled_NotCalledWhenAlreadyDisabled()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TrackingComponent>();
        world.Flush();
        var component = entity.GetComponent<TrackingComponent>()!;

        component.IsEnabled = false;
        component.DisabledCalled = false;
        component.IsEnabled = false;

        Assert.False(component.DisabledCalled);
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

    #region IsActive Propagation

    [Fact]
    public void IsActive_SetToFalse_CallsOnDisabledOnEnabledComponents()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TrackingComponent>();
        world.Flush();
        var component = entity.GetComponent<TrackingComponent>()!;

        entity.IsActive = false;

        Assert.True(component.DisabledCalled);
    }

    [Fact]
    public void IsActive_SetToTrue_CallsOnEnabledOnEnabledComponents()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TrackingComponent>();
        world.Flush();
        var component = entity.GetComponent<TrackingComponent>()!;

        entity.IsActive = false;
        component.EnabledCalled = false;
        entity.IsActive = true;

        Assert.True(component.EnabledCalled);
    }

    [Fact]
    public void IsActive_SetToFalse_DoesNotCallOnDisabledOnAlreadyDisabledComponents()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TrackingComponent>();
        world.Flush();
        var component = entity.GetComponent<TrackingComponent>()!;
        component.IsEnabled = false;
        component.DisabledCalled = false;

        entity.IsActive = false;

        Assert.False(component.DisabledCalled);
    }

    [Fact]
    public void IsActive_SetToTrue_DoesNotCallOnEnabledOnAlreadyDisabledComponents()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TrackingComponent>();
        world.Flush();
        var component = entity.GetComponent<TrackingComponent>()!;
        component.IsEnabled = false;
        entity.IsActive = false;
        component.EnabledCalled = false;

        entity.IsActive = true;

        Assert.False(component.EnabledCalled);
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

    private class TrackingComponent : Component
    {
        public bool EnabledCalled { get; set; }
        public bool DisabledCalled { get; set; }

        protected internal override void OnEnabled() => EnabledCalled = true;
        protected internal override void OnDisabled() => DisabledCalled = true;
    }

    #endregion
}

public class BehaviorLifecycleTests : TestBase
{
    [Fact]
    public void Behavior_OnDisabled_CalledWhenIsEnabledSetToFalse()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TrackingBehavior>();

        var behavior = entity.GetBehavior<TrackingBehavior>()!;
        behavior.IsEnabled = false;

        Assert.True(behavior.DisabledCalled);
    }

    [Fact]
    public void Behavior_OnEnabled_CalledWhenIsEnabledSetToTrue()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TrackingBehavior>();

        var behavior = entity.GetBehavior<TrackingBehavior>()!;
        behavior.IsEnabled = false;
        behavior.EnabledCalled = false;
        behavior.IsEnabled = true;

        Assert.True(behavior.EnabledCalled);
    }

    [Fact]
    public void Behavior_OnEnabled_NotCalledWhenAlreadyEnabled()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TrackingBehavior>();

        var behavior = entity.GetBehavior<TrackingBehavior>()!;
        behavior.IsEnabled = true;

        Assert.False(behavior.EnabledCalled);
    }

    [Fact]
    public void Behavior_OnDisabled_NotCalledWhenAlreadyDisabled()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TrackingBehavior>();

        var behavior = entity.GetBehavior<TrackingBehavior>()!;
        behavior.IsEnabled = false;
        behavior.DisabledCalled = false;
        behavior.IsEnabled = false;

        Assert.False(behavior.DisabledCalled);
    }

    private class TrackingBehavior : Behavior
    {
        public bool EnabledCalled { get; set; }
        public bool DisabledCalled { get; set; }

        protected internal override void OnEnabled() => EnabledCalled = true;
        protected internal override void OnDisabled() => DisabledCalled = true;
    }

    [Fact]
    public void IsActive_SetToFalse_CallsOnDisabledOnEnabledBehaviors()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TrackingBehavior>();

        var behavior = entity.GetBehavior<TrackingBehavior>()!;
        entity.IsActive = false;

        Assert.True(behavior.DisabledCalled);
    }

    [Fact]
    public void IsActive_SetToTrue_CallsOnEnabledOnEnabledBehaviors()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TrackingBehavior>();
        entity.IsActive = false;

        var behavior = entity.GetBehavior<TrackingBehavior>()!;
        behavior.EnabledCalled = false;
        entity.IsActive = true;

        Assert.True(behavior.EnabledCalled);
    }

    [Fact]
    public void IsActive_SetToFalse_DoesNotCallOnDisabledOnAlreadyDisabledBehaviors()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TrackingBehavior>();

        var behavior = entity.GetBehavior<TrackingBehavior>()!;
        behavior.IsEnabled = false;
        behavior.DisabledCalled = false;
        entity.IsActive = false;

        Assert.False(behavior.DisabledCalled);
    }

    [Fact]
    public void IsActive_SetToTrue_DoesNotCallOnEnabledOnAlreadyDisabledBehaviors()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TrackingBehavior>();
        entity.IsActive = false;

        var behavior = entity.GetBehavior<TrackingBehavior>()!;
        behavior.IsEnabled = false;
        behavior.EnabledCalled = false;
        entity.IsActive = true;

        Assert.False(behavior.EnabledCalled);
    }
}
