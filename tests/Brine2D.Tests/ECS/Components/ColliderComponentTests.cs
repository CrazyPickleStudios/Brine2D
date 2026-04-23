using System.Numerics;
using Brine2D.Collision;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Physics;

namespace Brine2D.Tests.ECS.Components;

public class ColliderComponentTests : TestBase
{
    [Fact]
    public void Constructor_DefaultValues_AreCorrect()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        Assert.Equal(0, collider.Layer);
        Assert.Equal(ulong.MaxValue, collider.CollisionMask);
        Assert.False(collider.IsTrigger);
        Assert.Equal(Vector2.Zero, collider.Offset);
        Assert.Empty(collider.CollidingEntities);
        Assert.Equal(PhysicsBodyType.Dynamic, collider.BodyType);
    }

    [Fact]
    public void SetCircle_ConfiguresCircleShape()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        collider.Shape = new CircleShape(50f);

        Assert.IsType<CircleShape>(collider.Shape);
        Assert.Equal(50f, ((CircleShape)collider.Shape).Radius);
    }

    [Fact]
    public void SetBox_ConfiguresBoxShape()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        collider.Shape = new BoxShape(100f, 50f);

        Assert.IsType<BoxShape>(collider.Shape);
        Assert.Equal(100f, ((BoxShape)collider.Shape).Width);
        Assert.Equal(50f, ((BoxShape)collider.Shape).Height);
    }

    [Fact]
    public void SetCircle_CanChangeFromBox()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;
        collider.Shape = new BoxShape(100f, 50f);

        collider.Shape = new CircleShape(25f);

        Assert.IsType<CircleShape>(collider.Shape);
        Assert.Equal(25f, ((CircleShape)collider.Shape).Radius);
    }

    [Fact]
    public void SetBox_CanChangeFromCircle()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;
        collider.Shape = new CircleShape(50f);

        collider.Shape = new BoxShape(80f, 60f);

        Assert.IsType<BoxShape>(collider.Shape);
        Assert.Equal(80f, ((BoxShape)collider.Shape).Width);
        Assert.Equal(60f, ((BoxShape)collider.Shape).Height);
    }

    [Fact]
    public void Layer_SetAndGet_WorksCorrectly()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        collider.Layer = 5;

        Assert.Equal(5, collider.Layer);
    }

    [Fact]
    public void Layer_CanBeSetToZero()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        collider.Layer = 0;

        Assert.Equal(0, collider.Layer);
    }

    [Fact]
    public void Layer_CanBeSetTo31()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        collider.Layer = 31;

        Assert.Equal(31, collider.Layer);
    }

    [Fact]
    public void CollisionMask_SetAndGet_WorksCorrectly()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        collider.CollisionMask = 0x00000001;

        Assert.Equal(0x00000001u, collider.CollisionMask);
    }

    [Fact]
    public void CollisionMask_DefaultValue_CollidesWithAllLayers()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        Assert.Equal(ulong.MaxValue, collider.CollisionMask);
    }

    [Fact]
    public void IsTrigger_SetAndGet_WorksCorrectly()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        collider.IsTrigger = true;

        Assert.True(collider.IsTrigger);
    }

    [Fact]
    public void IsTrigger_CanBeToggled()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        collider.IsTrigger = true;
        Assert.True(collider.IsTrigger);

        collider.IsTrigger = false;
        Assert.False(collider.IsTrigger);
    }

    [Fact]
    public void Offset_SetAndGet_WorksCorrectly()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        collider.Offset = new Vector2(10, 20);

        Assert.Equal(new Vector2(10, 20), collider.Offset);
    }

    [Fact]
    public void Offset_DefaultIsZero()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        Assert.Equal(Vector2.Zero, collider.Offset);
    }

    [Theory]
    [InlineData(PhysicsBodyType.Dynamic)]
    [InlineData(PhysicsBodyType.Static)]
    [InlineData(PhysicsBodyType.Kinematic)]
    public void BodyType_CanBeSetToAllValues(PhysicsBodyType bodyType)
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PhysicsBodyComponent>();
        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        collider.BodyType = bodyType;

        Assert.Equal(bodyType, collider.BodyType);
    }

    [Fact]
    public void OnCollisionEnter_Trigger_InvokesTriggerEvent()
    {
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c => c.IsTrigger = true);

        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();

        var collider1 = entity1.GetComponent<PhysicsBodyComponent>()!;
        var collider2 = entity2.GetComponent<PhysicsBodyComponent>()!;

        var eventFired = false;
        collider1.OnTriggerEnter += (_) => eventFired = true;

        collider1.NotifyTriggerEnter(collider2, null, null);

        Assert.True(eventFired);
    }

    [Fact]
    public void OnCollisionEnter_NonTrigger_InvokesCollisionEvent()
    {
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();

        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();

        var collider1 = entity1.GetComponent<PhysicsBodyComponent>()!;
        var collider2 = entity2.GetComponent<PhysicsBodyComponent>()!;

        var eventFired = false;
        collider1.OnCollisionEnter += (_, _) => eventFired = true;

        collider1.NotifyCollisionEnter(collider2, CollisionContact.Empty, null, null);

        Assert.True(eventFired);
    }

    [Fact]
    public void OnCollisionExit_Trigger_InvokesTriggerExitEvent()
    {
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c => c.IsTrigger = true);

        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();

        var collider1 = entity1.GetComponent<PhysicsBodyComponent>()!;
        var collider2 = entity2.GetComponent<PhysicsBodyComponent>()!;

        var eventFired = false;
        collider1.OnTriggerExit += _ => eventFired = true;

        collider1.NotifyTriggerExit(collider2);

        Assert.True(eventFired);
    }

    [Fact]
    public void OnCollisionExit_NonTrigger_InvokesCollisionExitEvent()
    {
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();

        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();

        var collider1 = entity1.GetComponent<PhysicsBodyComponent>()!;
        var collider2 = entity2.GetComponent<PhysicsBodyComponent>()!;

        var eventFired = false;
        collider1.OnCollisionExit += _ => eventFired = true;

        collider1.NotifyCollisionExit(collider2);

        Assert.True(eventFired);
    }

    [Fact]
    public void NotifyCollisionEnter_AddsToCollidingEntities()
    {
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();

        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();

        var collider1 = entity1.GetComponent<PhysicsBodyComponent>()!;
        var collider2 = entity2.GetComponent<PhysicsBodyComponent>()!;

        collider1.NotifyCollisionEnter(collider2, CollisionContact.Empty, null, null);

        Assert.Contains(entity2, collider1.CollidingEntities);
    }

    [Fact]
    public void NotifyCollisionExit_RemovesFromCollidingEntities()
    {
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();

        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();

        var collider1 = entity1.GetComponent<PhysicsBodyComponent>()!;
        var collider2 = entity2.GetComponent<PhysicsBodyComponent>()!;

        collider1.NotifyCollisionEnter(collider2, CollisionContact.Empty, null, null);
        collider1.NotifyCollisionExit(collider2);

        Assert.DoesNotContain(entity2, collider1.CollidingEntities);
    }

    [Fact]
    public void ColliderComponent_CompleteSetup_WorksCorrectly()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 100))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c. Shape = new CircleShape(50f);
                c.Layer = 3;
                c.CollisionMask = 0x00000008;
                c.IsTrigger = true;
                c.Offset = new Vector2(10, 10);
                c.BodyType = PhysicsBodyType.Static;
            });

        var collider = entity.GetComponent<PhysicsBodyComponent>()!;

        Assert.IsType<CircleShape>(collider.Shape);
        Assert.Equal(50f, ((CircleShape)collider.Shape).Radius);
        Assert.Equal(3, collider.Layer);
        Assert.Equal(0x00000008u, collider.CollisionMask);
        Assert.True(collider.IsTrigger);
        Assert.Equal(new Vector2(10, 10), collider.Offset);
        Assert.Equal(PhysicsBodyType.Static, collider.BodyType);
    }
}