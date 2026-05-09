using System.Numerics;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Physics;
using Brine2D.Systems.Physics;

namespace Brine2D.Tests.Systems.Physics;

public class PhysicsBodyComponentBehaviorTests : PhysicsTestBase
{
    [Fact]
    public void OnCollisionExit_AfterBodyRemoved_Fires()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(PhysicsWorld);
        bool exitFired = false;

        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(40f);
                c.BodyType = PhysicsBodyType.Static;
                c.OnCollisionExit += _ => exitFired = true;
            });

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 10f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(40f));

        world.Flush();
        for (int i = 0; i < 3; i++)
            system.FixedUpdate(world, FixedTime);

        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
        Assert.NotEmpty(bodyA.ActiveContactPairs);

        entityA.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();
        system.FixedUpdate(world, FixedTime);

        Assert.True(exitFired);
    }

    [Fact]
    public void OnTriggerEnter_TriggerAndSolidOverlapping_Fires()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(PhysicsWorld);
        bool fired = false;

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(40f);
                c.IsTrigger = true;
                c.BodyType = PhysicsBodyType.Static;
                c.OnTriggerEnter += _ => fired = true;
            });

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 5f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));

        world.Flush();
        for (int i = 0; i < 5; i++)
            system.FixedUpdate(world, FixedTime);

        Assert.True(fired);
    }

    [Fact]
    public void OnTriggerExit_AfterBodyRemoved_Fires()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(PhysicsWorld);
        bool exitFired = false;

        var trigger = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(40f);
                c.IsTrigger = true;
                c.BodyType = PhysicsBodyType.Static;
                c.OnTriggerExit += _ => exitFired = true;
            });

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 5f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));

        world.Flush();
        for (int i = 0; i < 3; i++)
            system.FixedUpdate(world, FixedTime);

        var triggerBody = trigger.GetComponent<PhysicsBodyComponent>()!;
        Assert.NotEmpty(triggerBody.ActiveSensorPairs);

        trigger.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();
        system.FixedUpdate(world, FixedTime);

        Assert.True(exitFired);
    }

    [Fact]
    public void CollidingEntities_AfterBothExitEvents_IsEmpty()
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

        var moverEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 5f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(40f));

        world.Flush();
        for (int i = 0; i < 3; i++)
            system.FixedUpdate(world, FixedTime);

        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
        Assert.NotEmpty(bodyA.CollidingEntities);

        moverEntity.Destroy();
        world.Flush();
        system.FixedUpdate(world, FixedTime);

        Assert.Empty(bodyA.CollidingEntities);
    }

    [Fact]
    public void IgnoreCollision_AfterBodyDestroyed_PairPurged()
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

        Assert.False(PhysicsWorld.IsCollisionIgnored(bodyA.BodyId.index1, bodyB.BodyId.index1));
    }

    [Fact]
    public void BodyType_LiveSwitch_FlushesContactPairs()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(PhysicsWorld);
        int exitCount = 0;

        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(40f);
                c.BodyType = PhysicsBodyType.Static;
                c.OnCollisionExit += _ => exitCount++;
            });

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 10f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(40f));

        world.Flush();
        for (int i = 0; i < 3; i++)
            system.FixedUpdate(world, FixedTime);

        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
        Assert.NotEmpty(bodyA.ActiveContactPairs);

        bodyA.BodyType = PhysicsBodyType.Kinematic;
        system.FixedUpdate(world, FixedTime);

        Assert.True(exitCount > 0);
    }

    [Fact]
    public void IsBullet_LiveToggle_RebuildPreservesVelocity()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(PhysicsWorld);

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));

        world.Flush();
        system.FixedUpdate(world, FixedTime);

        var body = entity.GetComponent<PhysicsBodyComponent>()!;

        body.IsBullet = true;
        system.FixedUpdate(world, FixedTime);

        Assert.True(body.IsBullet);
        Assert.NotEqual(0f, body.LinearVelocity.Y);
    }

    [Fact]
    public void FreezePositionY_Dynamic_YAxisStaysFixed()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(PhysicsWorld);

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 200f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.FreezePositionY = true;
            });

        world.Flush();
        float yBefore = entity.GetComponent<TransformComponent>()!.Position.Y;

        for (int i = 0; i < 10; i++)
            system.FixedUpdate(world, FixedTime);

        float yAfter = entity.GetComponent<TransformComponent>()!.Position.Y;
        Assert.InRange(yAfter, yBefore - 5f, yBefore + 5f);
    }

    [Fact]
    public void FreezePositionX_Dynamic_XAxisStaysFixed()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(PhysicsWorld);

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.FreezePositionX = true;
            });

        world.Flush();
        float xBefore = entity.GetComponent<TransformComponent>()!.Position.X;

        for (int i = 0; i < 10; i++)
            system.FixedUpdate(world, FixedTime);

        Assert.Equal(xBefore, entity.GetComponent<TransformComponent>()!.Position.X, precision: 1);
    }

    [Fact]
    public void ChainShape_StaticBody_CreatesValidBody()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(PhysicsWorld);

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.BodyType = PhysicsBodyType.Static;
                c.Shape = new ChainShape(
                [
                    new Vector2(-200f, 0f),
                    new Vector2(-100f, 50f),
                    new Vector2(0f, 0f),
                    new Vector2(100f, 50f),
                    new Vector2(200f, 0f)
                ]);
            });

        world.Flush();
        system.FixedUpdate(world, FixedTime);

        var body = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        // B2.BodyIsValid / B2.ChainIsValid are used here intentionally —
        // no Brine2D API exposes ChainId validity.
        Assert.True(Box2D.NET.Bindings.B2.BodyIsValid(body.BodyId));
        Assert.True(Box2D.NET.Bindings.B2.ChainIsValid(body.ChainId));
    }

    [Fact]
    public void ChainShape_DynamicBodyType_ThrowsOnAssign()
    {
        var body = new PhysicsBodyComponent();

        Assert.Throws<InvalidOperationException>(() => body.Shape = new ChainShape(
        [
            new Vector2(0f, 0f),
            new Vector2(100f, 0f),
            new Vector2(200f, 0f)
        ]));
    }

    [Fact]
    public void RaycastClosest_NoBodyInPath_ReturnsNull()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(PhysicsWorld);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(1000f, 1000f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(10f, 10f);
                c.BodyType = PhysicsBodyType.Static;
            });

        world.Flush();
        system.FixedUpdate(world, FixedTime);

        var hit = PhysicsWorld.RaycastClosest(new Vector2(0f, 0f), new Vector2(0f, 1f), 100f);

        Assert.Null(hit);
    }
}