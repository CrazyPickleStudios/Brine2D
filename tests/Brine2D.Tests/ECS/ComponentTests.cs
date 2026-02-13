using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Rendering;
using NSubstitute;

namespace Brine2D.Tests.ECS;

public class ComponentTests : TestBase
{
    #region Attachment State

    [Fact]
    public void IsAttached_WhenNotAttached_ReturnsFalse()
    {
        // Arrange
        var component = new TestComponent();

        // Act & Assert
        Assert.False(component.IsAttached);
        Assert.Null(component.Entity);
    }

    [Fact]
    public void IsAttached_WhenAttached_ReturnsTrue()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act & Assert
        Assert.True(component.IsAttached);
        Assert.NotNull(component.Entity);
        Assert.Equal(entity, component.Entity);
    }

    [Fact]
    public void Entity_AfterAttachment_IsSet()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity("TestEntity");
        entity.AddComponent<TestComponent>();
        world.Flush();

        // Act
        var component = entity.GetComponent<TestComponent>()!;

        // Assert
        Assert.Equal(entity, component.Entity);
    }

    #endregion

    #region IsEnabled Property

    [Fact]
    public void IsEnabled_DefaultValue_IsTrue()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act & Assert
        Assert.True(component.IsEnabled);
    }

    [Fact]
    public void IsEnabled_SetToFalse_CallsOnDisabled()
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
        Assert.True(component.OnDisabledCalled);
    }

    [Fact]
    public void IsEnabled_SetToTrue_CallsOnEnabled()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;
        component.IsEnabled = false;
        component.OnEnabledCalled = false; // Reset

        // Act
        component.IsEnabled = true;

        // Assert
        Assert.True(component.IsEnabled);
        Assert.True(component.OnEnabledCalled);
    }

    [Fact]
    public void IsEnabled_SetToSameValue_DoesNotCallLifecycleMethods()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;
        component.OnEnabledCalled = false; // Reset from initial state

        // Act
        component.IsEnabled = true; // Already true

        // Assert
        Assert.False(component.OnEnabledCalled); // Should not be called again
    }

    [Fact]
    public void IsEnabled_ToggleMultipleTimes_CallsLifecycleMethods()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act & Assert - Toggle off
        component.IsEnabled = false;
        Assert.True(component.OnDisabledCalled);
        Assert.False(component.IsEnabled);

        component.OnDisabledCalled = false;
        component.OnEnabledCalled = false;

        // Toggle on
        component.IsEnabled = true;
        Assert.True(component.OnEnabledCalled);
        Assert.True(component.IsEnabled);

        component.OnDisabledCalled = false;
        component.OnEnabledCalled = false;

        // Toggle off again
        component.IsEnabled = false;
        Assert.True(component.OnDisabledCalled);
        Assert.False(component.IsEnabled);
    }

    #endregion

    #region Convenience Properties

    [Fact]
    public void EntityName_WhenAttached_ReturnsEntityName()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity("MyEntity");
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act
        var name = component.EntityName;

        // Assert
        Assert.Equal("MyEntity", name);
    }

    [Fact]
    public void EntityName_WhenNotAttached_ReturnsEmptyString()
    {
        // Arrange
        var component = new TestComponent();

        // Act
        var name = component.EntityName;

        // Assert
        Assert.Equal(string.Empty, name);
    }

    [Fact]
    public void EntityTags_WhenAttached_ReturnsEntityTags()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddTags("Player", "Controllable");
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act
        var tags = component.EntityTags;

        // Assert
        Assert.Contains("Player", tags);
        Assert.Contains("Controllable", tags);
    }

    [Fact]
    public void EntityTags_WhenNotAttached_ReturnsEmptySet()
    {
        // Arrange
        var component = new TestComponent();

        // Act
        var tags = component.EntityTags;

        // Assert
        Assert.Empty(tags);
    }

    [Fact]
    public void Transform_WhenEntityHasTransform_ReturnsComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act
        var transform = component.Transform;

        // Assert
        Assert.NotNull(transform);
        Assert.IsType<TransformComponent>(transform);
    }

    [Fact]
    public void Transform_WhenEntityLacksTransform_ReturnsNull()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act
        var transform = component.Transform;

        // Assert
        Assert.Null(transform);
    }

    #endregion

    #region GetComponent Methods

    [Fact]
    public void GetComponent_SiblingExists_ReturnsComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act
        var transform = component.GetComponent<TransformComponent>();

        // Assert
        Assert.NotNull(transform);
    }

    [Fact]
    public void GetComponent_SiblingDoesNotExist_ReturnsNull()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act
        var transform = component.GetComponent<TransformComponent>();

        // Assert
        Assert.Null(transform);
    }

    [Fact]
    public void GetRequiredComponent_SiblingExists_ReturnsComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act
        var transform = component.GetRequiredComponent<TransformComponent>();

        // Assert
        Assert.NotNull(transform);
    }

    [Fact]
    public void GetRequiredComponent_SiblingDoesNotExist_ThrowsException()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            component.GetRequiredComponent<TransformComponent>());
    }

    [Fact]
    public void GetRequiredComponent_NotAttached_ThrowsException()
    {
        // Arrange
        var component = new TestComponent();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            component.GetRequiredComponent<TransformComponent>());
    }

    [Fact]
    public void TryGetComponent_SiblingExists_ReturnsTrueAndComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act
        var result = component.TryGetComponent<TransformComponent>(out var transform);

        // Assert
        Assert.True(result);
        Assert.NotNull(transform);
    }

    [Fact]
    public void TryGetComponent_SiblingDoesNotExist_ReturnsFalse()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act
        var result = component.TryGetComponent<TransformComponent>(out var transform);

        // Assert
        Assert.False(result);
        Assert.Null(transform);
    }

    #endregion

    #region Hierarchy Component Queries

    [Fact]
    public void GetComponentInChildren_OnSelf_ReturnsComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act
        var transform = component.GetComponentInChildren<TransformComponent>();

        // Assert
        Assert.NotNull(transform);
    }

    [Fact]
    public void GetComponentInChildren_OnChild_ReturnsComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        parent.AddComponent<TestComponent>();
        
        var child = world.CreateEntity();
        child.AddComponent<TransformComponent>();
        child.SetParent(parent);
        
        world.Flush();
        var component = parent.GetComponent<TestComponent>()!;

        // Act
        var transform = component.GetComponentInChildren<TransformComponent>();

        // Assert
        Assert.NotNull(transform);
    }

    [Fact]
    public void GetComponentInChildren_NotFound_ReturnsNull()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act
        var transform = component.GetComponentInChildren<TransformComponent>();

        // Assert
        Assert.Null(transform);
    }

    [Fact]
    public void GetComponentInParent_OnSelf_ReturnsComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act
        var transform = component.GetComponentInParent<TransformComponent>();

        // Assert
        Assert.NotNull(transform);
    }

    [Fact]
    public void GetComponentInParent_OnParent_ReturnsComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        parent.AddComponent<TransformComponent>();
        
        var child = world.CreateEntity();
        child.AddComponent<TestComponent>();
        child.SetParent(parent);
        
        world.Flush();
        var component = child.GetComponent<TestComponent>()!;

        // Act
        var transform = component.GetComponentInParent<TransformComponent>();

        // Assert
        Assert.NotNull(transform);
    }

    [Fact]
    public void GetComponentInParent_NotFound_ReturnsNull()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act
        var transform = component.GetComponentInParent<TransformComponent>();

        // Assert
        Assert.Null(transform);
    }

    #endregion

    #region Destroy Method

    [Fact]
    public void Destroy_RemovesComponentFromEntity()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Act
        component.Destroy();
        world.Flush();

        // Assert
        Assert.False(entity.HasComponent<TestComponent>());
    }

    [Fact]
    public void Destroy_NotAttached_ThrowsException()
    {
        // Arrange
        var component = new TestComponent();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => component.Destroy());
    }

    #endregion

    #region Lifecycle Methods

    [Fact]
    public void OnAdded_CalledWhenComponentAdded()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        // Assert
        Assert.True(component.OnAddedCalled);
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
        world.Flush();

        // Assert
        Assert.True(component.OnRemovedCalled);
    }

    [Fact]
    public void OnUpdate_CalledDuringWorldUpdate()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

        // Act
        world.Update(gameTime);

        // Assert
        Assert.True(component.OnUpdateCalled);
    }

    [Fact]
    public void OnUpdate_NotCalledWhenDisabled()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;
        component.IsEnabled = false;

        var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

        // Act
        world.Update(gameTime);

        // Assert
        Assert.False(component.OnUpdateCalled);
    }

    [Fact]
    public void OnRender_CalledDuringWorldRender()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;
        var mockRenderer = Substitute.For<IRenderer>();

        // Act
        world.Render(mockRenderer);

        // Assert
        Assert.True(component.OnRenderCalled);
    }

    [Fact]
    public void OnRender_NotCalledWhenDisabled()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;
        component.IsEnabled = false;
        var mockRenderer = Substitute.For<IRenderer>();

        // Act
        world.Render(mockRenderer);

        // Assert
        Assert.False(component.OnRenderCalled);
    }

    #endregion

    #region Test Helper Component

    private class TestComponent : Component
    {
        public bool OnAddedCalled { get; set; }
        public bool OnRemovedCalled { get; set; }
        public bool OnEnabledCalled { get; set; }
        public bool OnDisabledCalled { get; set; }
        public bool OnUpdateCalled { get; set; }
        public bool OnRenderCalled { get; set; }

        protected internal override void OnAdded()
        {
            OnAddedCalled = true;
        }

        protected internal override void OnRemoved()
        {
            OnRemovedCalled = true;
        }

        protected internal override void OnEnabled()
        {
            OnEnabledCalled = true;
        }

        protected internal override void OnDisabled()
        {
            OnDisabledCalled = true;
        }

        protected internal override void OnUpdate(GameTime gameTime)
        {
            OnUpdateCalled = true;
        }

        protected internal override void OnRender(IRenderer renderer)
        {
            OnRenderCalled = true;
        }
    }

    #endregion
}