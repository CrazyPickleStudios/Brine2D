using System.Numerics;
using Brine2D.ECS;
using Brine2D.ECS.Components;

namespace Brine2D.Tests.ECS.Components;

public class ColliderComponentTests : TestBase
{
    #region Default Values

    [Fact]
    public void Constructor_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>(); // Required by ColliderComponent
        entity.AddComponent<ColliderComponent>();
        var collider = entity.GetComponent<ColliderComponent>()!;

        // Assert
        Assert.Equal(0, collider.Layer);
        Assert.Equal(0xFFFFFFFF, collider.CollisionMask);
        Assert.False(collider.IsTrigger);
        Assert.Equal(Vector2.Zero, collider.Offset);
        Assert.Empty(collider.CollidingEntities);
    }

    #endregion

    #region Shape Configuration

    [Fact]
    public void SetCircle_ConfiguresCircleShape()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ColliderComponent>();
        var collider = entity.GetComponent<ColliderComponent>()!;

        // Act
        collider.SetCircle(50f);

        // Assert
        Assert.Equal(CollisionShapeType.Circle, collider.ShapeType);
        Assert.Equal(50f, collider.Radius);
    }

    [Fact]
    public void SetBox_ConfiguresBoxShape()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ColliderComponent>();
        var collider = entity.GetComponent<ColliderComponent>()!;

        // Act
        collider.SetBox(100f, 50f);

        // Assert
        Assert.Equal(CollisionShapeType.Box, collider.ShapeType);
        Assert.Equal(100f, collider.Width);
        Assert.Equal(50f, collider.Height);
    }

    [Fact]
    public void SetCircle_CanChangeFromBox()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ColliderComponent>();
        var collider = entity.GetComponent<ColliderComponent>()!;
        collider.SetBox(100f, 50f);

        // Act
        collider.SetCircle(25f);

        // Assert
        Assert.Equal(CollisionShapeType.Circle, collider.ShapeType);
        Assert.Equal(25f, collider.Radius);
    }

    [Fact]
    public void SetBox_CanChangeFromCircle()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ColliderComponent>();
        var collider = entity.GetComponent<ColliderComponent>()!;
        collider.SetCircle(50f);

        // Act
        collider.SetBox(80f, 60f);

        // Assert
        Assert.Equal(CollisionShapeType.Box, collider.ShapeType);
        Assert.Equal(80f, collider.Width);
        Assert.Equal(60f, collider.Height);
    }

    #endregion

    #region Layer and Mask

    [Fact]
    public void Layer_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ColliderComponent>();
        var collider = entity.GetComponent<ColliderComponent>()!;

        // Act
        collider.Layer = 5;

        // Assert
        Assert.Equal(5, collider.Layer);
    }

    [Fact]
    public void Layer_CanBeSetToZero()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ColliderComponent>();
        var collider = entity.GetComponent<ColliderComponent>()!;

        // Act
        collider.Layer = 0;

        // Assert
        Assert.Equal(0, collider.Layer);
    }

    [Fact]
    public void Layer_CanBeSetTo31()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ColliderComponent>();
        var collider = entity.GetComponent<ColliderComponent>()!;

        // Act
        collider.Layer = 31;

        // Assert
        Assert.Equal(31, collider.Layer);
    }

    [Fact]
    public void CollisionMask_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ColliderComponent>();
        var collider = entity.GetComponent<ColliderComponent>()!;

        // Act
        collider.CollisionMask = 0x00000001; // Only layer 0

        // Assert
        Assert.Equal(0x00000001u, collider.CollisionMask);
    }

    [Fact]
    public void CollisionMask_DefaultValue_CollidesWithAllLayers()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ColliderComponent>();
        var collider = entity.GetComponent<ColliderComponent>()!;

        // Act & Assert
        Assert.Equal(0xFFFFFFFF, collider.CollisionMask);
    }

    #endregion

    #region Trigger

    [Fact]
    public void IsTrigger_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ColliderComponent>();
        var collider = entity.GetComponent<ColliderComponent>()!;

        // Act
        collider.IsTrigger = true;

        // Assert
        Assert.True(collider.IsTrigger);
    }

    [Fact]
    public void IsTrigger_CanBeToggled()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ColliderComponent>();
        var collider = entity.GetComponent<ColliderComponent>()!;

        // Act & Assert
        collider.IsTrigger = true;
        Assert.True(collider.IsTrigger);

        collider.IsTrigger = false;
        Assert.False(collider.IsTrigger);
    }

    #endregion

    #region Offset

    [Fact]
    public void Offset_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ColliderComponent>();
        var collider = entity.GetComponent<ColliderComponent>()!;

        // Act
        collider.Offset = new Vector2(10, 20);

        // Assert
        Assert.Equal(new Vector2(10, 20), collider.Offset);
    }

    [Fact]
    public void Offset_DefaultIsZero()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ColliderComponent>();
        var collider = entity.GetComponent<ColliderComponent>()!;

        // Assert
        Assert.Equal(Vector2.Zero, collider.Offset);
    }

    #endregion

    #region WorldCenter

    [Fact]
    public void WorldCenter_WithNoOffset_ReturnsTransformPosition()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 200))
            .AddComponent<ColliderComponent>();

        var collider = entity.GetComponent<ColliderComponent>()!;

        // Act
        var worldCenter = collider.WorldCenter;

        // Assert
        Assert.Equal(new Vector2(100, 200), worldCenter);
    }

    [Fact]
    public void WorldCenter_WithOffset_ReturnsTransformPlusOffset()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 200))
            .AddComponent<ColliderComponent>(c => c.Offset = new Vector2(10, 20));

        var collider = entity.GetComponent<ColliderComponent>()!;

        // Act
        var worldCenter = collider.WorldCenter;

        // Assert
        Assert.Equal(new Vector2(110, 220), worldCenter);
    }

    [Fact]
    public void WorldCenter_UpdatesWhenTransformChanges()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 200))
            .AddComponent<ColliderComponent>();

        var transform = entity.GetComponent<TransformComponent>()!;
        var collider = entity.GetComponent<ColliderComponent>()!;

        // Act
        transform.LocalPosition = new Vector2(300, 400);
        var worldCenter = collider.WorldCenter;

        // Assert
        Assert.Equal(new Vector2(300, 400), worldCenter);
    }

    #endregion

    #region Collision Events

    [Fact]
    public void OnCollisionEnter_Trigger_InvokesTriggerEvent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<ColliderComponent>(c => c.IsTrigger = true);

        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<ColliderComponent>();

        var collider1 = entity1.GetComponent<ColliderComponent>()!;
        var collider2 = entity2.GetComponent<ColliderComponent>()!;

        var eventFired = false;
        collider1.OnTriggerEnter += (other) => eventFired = true;

        // Act
        collider1.NotifyCollisionEnter(collider2);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void OnCollisionEnter_NonTrigger_InvokesCollisionEvent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<ColliderComponent>();

        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<ColliderComponent>();

        var collider1 = entity1.GetComponent<ColliderComponent>()!;
        var collider2 = entity2.GetComponent<ColliderComponent>()!;

        var eventFired = false;
        collider1.OnCollisionEnter += (other) => eventFired = true;

        // Act
        collider1.NotifyCollisionEnter(collider2);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void OnCollisionExit_Trigger_InvokesTriggerExitEvent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<ColliderComponent>(c => c.IsTrigger = true);

        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<ColliderComponent>();

        var collider1 = entity1.GetComponent<ColliderComponent>()!;
        var collider2 = entity2.GetComponent<ColliderComponent>()!;

        var eventFired = false;
        collider1.OnTriggerExit += (other) => eventFired = true;

        // Act
        collider1.NotifyCollisionExit(collider2);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void OnCollisionExit_NonTrigger_InvokesCollisionExitEvent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<ColliderComponent>();

        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<ColliderComponent>();

        var collider1 = entity1.GetComponent<ColliderComponent>()!;
        var collider2 = entity2.GetComponent<ColliderComponent>()!;

        var eventFired = false;
        collider1.OnCollisionExit += (other) => eventFired = true;

        // Act
        collider1.NotifyCollisionExit(collider2);

        // Assert
        Assert.True(eventFired);
    }

    [Fact]
    public void NotifyCollisionEnter_AddsToCollidingEntities()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<ColliderComponent>();

        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<ColliderComponent>();

        var collider1 = entity1.GetComponent<ColliderComponent>()!;
        var collider2 = entity2.GetComponent<ColliderComponent>()!;

        // Act
        collider1.NotifyCollisionEnter(collider2);

        // Assert
        Assert.Contains(entity2, collider1.CollidingEntities);
    }

    [Fact]
    public void NotifyCollisionExit_RemovesFromCollidingEntities()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<ColliderComponent>();

        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<ColliderComponent>();

        var collider1 = entity1.GetComponent<ColliderComponent>()!;
        var collider2 = entity2.GetComponent<ColliderComponent>()!;

        collider1.NotifyCollisionEnter(collider2);

        // Act
        collider1.NotifyCollisionExit(collider2);

        // Assert
        Assert.DoesNotContain(entity2, collider1.CollidingEntities);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void ColliderComponent_CompleteSetup_WorksCorrectly()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 100))
            .AddComponent<ColliderComponent>(c =>
            {
                c.SetCircle(50f);
                c.Layer = 3;
                c.CollisionMask = 0x00000008; // Layer 3 only
                c.IsTrigger = true;
                c.Offset = new Vector2(10, 10);
            });

        var collider = entity.GetComponent<ColliderComponent>()!;

        // Assert
        Assert.Equal(CollisionShapeType.Circle, collider.ShapeType);
        Assert.Equal(50f, collider.Radius);
        Assert.Equal(3, collider.Layer);
        Assert.Equal(0x00000008u, collider.CollisionMask);
        Assert.True(collider.IsTrigger);
        Assert.Equal(new Vector2(10, 10), collider.Offset);
        Assert.Equal(new Vector2(110, 110), collider.WorldCenter);
    }

    #endregion
}