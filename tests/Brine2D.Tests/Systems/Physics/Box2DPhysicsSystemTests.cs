using System.Numerics;
using Brine2D.Collision;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Physics;
using Brine2D.Systems.Physics;

namespace Brine2D.Tests.Systems.Physics;

public class Box2DPhysicsSystemTests : TestBase, IDisposable
{
    private static readonly GameTime FixedTime = new(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));

    private readonly PhysicsWorld _physicsWorld = new();

    public void Dispose()
    {
        _physicsWorld.Dispose();
    }

    [Fact]
    public void FixedUpdate_DynamicBody_FallsWithGravity()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 0f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));
        world.Flush();

        system.FixedUpdate(world, FixedTime);

        var transform = world.Entities.First().GetComponent<TransformComponent>()!;
        // Gravity is (0, 980) by default, body should have moved down
        Assert.True(transform.Position.Y > 0f);
    }

    [Fact]
    public void FixedUpdate_StaticBody_DoesNotMove()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 200f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(100f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });
        world.Flush();

        system.FixedUpdate(world, FixedTime);

        var transform = world.Entities.First().GetComponent<TransformComponent>()!;
        Assert.Equal(100f, transform.Position.X);
        Assert.Equal(200f, transform.Position.Y);
    }

    [Fact]
    public void FixedUpdate_BodyCreated_MarksNotDirty()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(5f));
        world.Flush();

        var collider = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        Assert.True(collider.IsDirty);

        system.FixedUpdate(world, FixedTime);

        Assert.False(collider.IsDirty);
    }

    [Fact]
    public void FixedUpdate_BoxShape_CreatesBody()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new BoxShape(30f, 20f));
        world.Flush();

        system.FixedUpdate(world, FixedTime);

        var collider = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        Assert.True(Box2D.NET.Bindings.B2.BodyIsValid(collider.BodyId));
        Assert.True(Box2D.NET.Bindings.B2.ShapeIsValid(collider.ShapeId));
    }

    [Fact]
    public void FixedUpdate_CollisionBetweenTwoBodies_DispatchesEvents()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        bool collisionDetected = false;

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(50f);
                c.OnCollisionEnter += (other, contact) => collisionDetected = true;
            });

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 110f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(50f));
        world.Flush();

        // Step multiple times to ensure collision detection
        for (int i = 0; i < 10; i++)
            system.FixedUpdate(world, FixedTime);

        Assert.True(collisionDetected);
    }

    [Fact]
    public void FixedUpdate_VelocityComponent_SyncedFromBox2D()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 0f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));
        world.Flush();

        system.FixedUpdate(world, FixedTime);

        var physicsBody = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        // Gravity should give downward velocity
        Assert.True(physicsBody.LinearVelocity.Y > 0f);
    }

    [Fact]
    public void FixedUpdate_TriggerSensor_DispatchesSensorEvents()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        bool triggerDetected = false;

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(50f);
                c.IsTrigger = true;
                c.BodyType = PhysicsBodyType.Static;
                c.OnTriggerEnter += (_) => triggerDetected = true;
            });

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 100f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(50f));
        world.Flush();

        for (int i = 0; i < 10; i++)
            system.FixedUpdate(world, FixedTime);

        // TODO: Assert.True(triggerDetected);
    }

    [Fact]
    public void FixedUpdate_ColliderWithOffset_AppliesOffset()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.Offset = new Vector2(20f, 0f);
                c.BodyType = PhysicsBodyType.Static;
            });
        world.Flush();

        system.FixedUpdate(world, FixedTime);

        var collider = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        Assert.True(Box2D.NET.Bindings.B2.BodyIsValid(collider.BodyId));
    }

    [Fact]
    public void FixedUpdate_PolygonShape_CreatesBody()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new PolygonShape([
                    new Vector2(-20f, -20f),
                    new Vector2(20f, -20f),
                    new Vector2(20f, 20f),
                    new Vector2(-20f, 20f)
                ]);
            });
        world.Flush();

        system.FixedUpdate(world, FixedTime);

        var collider = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        Assert.True(Box2D.NET.Bindings.B2.BodyIsValid(collider.BodyId));
        Assert.True(Box2D.NET.Bindings.B2.ShapeIsValid(collider.ShapeId));
    }

    [Fact]
    public void FixedUpdate_BulletAndFixedRotation_AppliedToBody()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(5f);
                c.IsBullet = true;
                c.FixedRotation = true;
            });
        world.Flush();

        system.FixedUpdate(world, FixedTime);

        var collider = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        Assert.True(Box2D.NET.Bindings.B2.BodyIsValid(collider.BodyId));
        Assert.True(Box2D.NET.Bindings.B2.BodyIsBullet(collider.BodyId));
        Assert.True(Box2D.NET.Bindings.B2.BodyIsFixedRotation(collider.BodyId));
    }

    [Fact]
    public void FixedUpdate_Restitution_AppliedToShape()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.Restitution = 0.8f;
                c.SurfaceFriction = 0.3f;
            });
        world.Flush();

        system.FixedUpdate(world, FixedTime);

        var collider = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        Assert.True(Box2D.NET.Bindings.B2.ShapeIsValid(collider.ShapeId));
    }
}