using System.Numerics;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Physics;
using Brine2D.Systems.Physics;

namespace Brine2D.Tests.Systems.Physics;

public class PhysicsBodyComponentOnRemovedTests : PhysicsTestBase
{
    [Fact]
    public void OnRemoved_BodyId_ResetToDefault()
    {
        var (world, _, entity, body) = CreateLiveBody();

        // Body was registered — confirm it has a non-default id before removal.
        Assert.NotEqual(default, body.BodyId);

        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        Assert.Equal(default, body.BodyId);
    }

    [Fact]
    public void OnRemoved_BodyType_ResetToDynamic()
    {
        var (world, _, entity, body) = CreateLiveBody(c => c.BodyType = PhysicsBodyType.Static);

        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        Assert.Equal(PhysicsBodyType.Dynamic, body.BodyType);
    }

    [Fact]
    public void OnRemoved_CollidingEntities_Cleared()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(PhysicsWorld);

        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(40f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var entityB = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 5f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(40f));

        world.Flush();
        for (var i = 0; i < 5; i++)
        {
            system.FixedUpdate(world, FixedTime);
        }

        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
        Assert.NotEmpty(bodyA.CollidingEntities);

        entityA.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        Assert.Empty(bodyA.CollidingEntities);
    }

    [Fact]
    public void OnRemoved_CollisionMask_ResetToULongMax()
    {
        var (world, _, entity, body) = CreateLiveBody(c => c.CollisionMask = 0b11UL);

        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        Assert.Equal(ulong.MaxValue, body.CollisionMask);
    }

    [Fact]
    public void OnRemoved_EnableHitEvents_ResetToTrue()
    {
        var (world, _, entity, body) = CreateLiveBody(c => c.EnableHitEvents = false);

        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        Assert.True(body.EnableHitEvents);
    }

    [Fact]
    public void OnRemoved_Events_HandlersNotFiredAfterRemoval()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(PhysicsWorld);

        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(40f);
                c.BodyType = PhysicsBodyType.Static;
                c.EnableHitEvents = true;
            });

        var entityB = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 5f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(40f);
                c.EnableHitEvents = true;
            });

        world.Flush();
        for (var i = 0; i < 3; i++)
            system.FixedUpdate(world, FixedTime);

        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;

        var firedAfterRemoval = false;
        bodyA.OnCollisionEnter += (_, _) => firedAfterRemoval = true;
        bodyA.OnCollisionExit += _ => firedAfterRemoval = true;
        bodyA.OnCollisionHit += (_, _) => firedAfterRemoval = true;
        bodyA.OnTriggerEnter += _ => firedAfterRemoval = true;
        bodyA.OnTriggerExit += _ => firedAfterRemoval = true;
        bodyA.OnBodySleep += _ => firedAfterRemoval = true;
        bodyA.OnBodyWake += _ => firedAfterRemoval = true;

        entityA.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        // world.Flush() legitimately fires OnCollisionExit during body destruction cleanup —
        // that is tested separately. Reset here to verify no further dispatch occurs.
        firedAfterRemoval = false;

        // One final step to confirm Box2D does not dispatch to the removed component.
        system.FixedUpdate(world, FixedTime);

        Assert.False(firedAfterRemoval);
    }

    [Fact]
    public void OnRemoved_FixedRotation_ResetToFalse()
    {
        var (world, _, entity, body) = CreateLiveBody(c => c.FixedRotation = true);

        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        Assert.False(body.FixedRotation);
    }

    [Fact]
    public void OnRemoved_FreezePositionX_ResetToFalse()
    {
        var (world, _, entity, body) = CreateLiveBody(c => c.FreezePositionX = true);

        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        Assert.False(body.FreezePositionX);
    }

    [Fact]
    public void OnRemoved_FreezePositionY_ResetToFalse()
    {
        var (world, _, entity, body) = CreateLiveBody(c => c.FreezePositionY = true);

        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        Assert.False(body.FreezePositionY);
    }

    [Fact]
    public void OnRemoved_GravityScale_ResetToOne()
    {
        var (world, _, entity, body) = CreateLiveBody(c => c.GravityScale = 0f);

        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        Assert.Equal(1f, body.GravityScale);
    }

    [Fact]
    public void OnRemoved_IgnoredPairs_PurgedOnRecycle()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(PhysicsWorld);

        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));
        var entityB = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(500f, 0f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));

        world.Flush();
        system.FixedUpdate(world, FixedTime);

        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
        var bodyB = entityB.GetComponent<PhysicsBodyComponent>()!;
        PhysicsWorld.IgnoreCollision(bodyA, bodyB);
        Assert.True(PhysicsWorld.IsCollisionIgnored(bodyA.BodyId.index1, bodyB.BodyId.index1));

        entityA.Destroy();
        world.Flush();
        system.FixedUpdate(world, FixedTime);

        Assert.False(PhysicsWorld.IsCollisionIgnored(0, bodyB.BodyId.index1));
    }

    [Fact]
    public void OnRemoved_IsBullet_ResetToFalse()
    {
        var (world, _, entity, body) = CreateLiveBody(c => c.IsBullet = true);

        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        Assert.False(body.IsBullet);
    }

    [Fact]
    public void OnRemoved_IsOneWayPlatformDirty_ResetToFalse()
    {
        // CreateLiveBody runs FixedUpdate, which clears IsOneWayPlatformDirty.
        // After removal, confirm the flag remains false (reset did not re-dirty it).
        var (world, _, entity, body) = CreateLiveBody(c => c.IsOneWayPlatform = true);
        Assert.False(body.IsOneWayPlatformDirty, "FixedUpdate should have cleared the dirty flag");

        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        Assert.False(body.IsOneWayPlatformDirty);
    }

    [Fact]
    public void OnRemoved_IsSimulationEnabled_ResetToTrue()
    {
        var (world, system, entity, body) = CreateLiveBody();
        body.IsSimulationEnabled = false;
        system.FixedUpdate(world, FixedTime);

        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        Assert.True(body.IsSimulationEnabled);
    }

    [Fact]
    public void OnRemoved_IsTrigger_ResetToFalse()
    {
        var (world, _, entity, body) = CreateLiveBody(c =>
        {
            c.IsTrigger = true;
            c.BodyType = PhysicsBodyType.Static;
        });

        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        Assert.False(body.IsTrigger);
    }

    [Fact]
    public void OnRemoved_Layer_ResetToZero()
    {
        var (world, _, entity, body) = CreateLiveBody(c => c.Layer = 5);

        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        Assert.Equal(0, body.Layer);
    }

    [Fact]
    public void OnRemoved_Mass_ResetToOne()
    {
        var (world, _, entity, body) = CreateLiveBody(c => c.Mass = 42f);

        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        Assert.Equal(1f, body.Mass);
    }

    [Fact]
    public void OnRemoved_Offset_ResetToZero()
    {
        var (world, _, entity, body) = CreateLiveBody(c => c.Offset = new Vector2(50f, 30f));

        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        Assert.Equal(Vector2.Zero, body.Offset);
    }

    [Fact]
    public void OnRemoved_Shape_ClearedToNull()
    {
        var (world, _, entity, body) = CreateLiveBody();

        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        Assert.Null(body.Shape);
    }

    [Fact]
    public void OnRemoved_SubShapes_Cleared()
    {
        var (world, _, entity, body) = CreateLiveBody(c =>
        {
            c.AddSubShape(new CircleShape(10f));
            c.AddSubShape(new CircleShape(15f));
        });

        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        Assert.Empty(body.SubShapes);
    }

    private (IEntityWorld world, Box2DPhysicsSystem system, Entity entity, PhysicsBodyComponent body) CreateLiveBody(
        Action<PhysicsBodyComponent>? configure = null)
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(PhysicsWorld);
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                configure?.Invoke(c);
            });
        world.Flush();
        system.FixedUpdate(world, FixedTime);
        return (world, system, entity, entity.GetComponent<PhysicsBodyComponent>()!);
    }
}