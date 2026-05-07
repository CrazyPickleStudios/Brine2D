using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Physics;
using Brine2D.Systems.Physics;

namespace Brine2D.Tests.Physics;

[Collection("Physics")]
public class PhysicsIgnoreCollisionTests : TestBase, IDisposable
{
    private static readonly GameTime FixedTime = new(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));

    private readonly PhysicsWorld _physicsWorld = new();

    public void Dispose() => _physicsWorld.Dispose();

    private Box2DPhysicsSystem CreatePhysicsSystem() => new(_physicsWorld);

    private void Step(IEntityWorld world, Box2DPhysicsSystem physics, int count = 1)
    {
        for (int i = 0; i < count; i++)
            physics.FixedUpdate(world, FixedTime);
    }

    [Fact]
    public void IgnoreCollision_TwoBodiesDoNotCollide()
    {
        var world = CreateTestWorld();
        var physics = CreatePhysicsSystem();

        // Floor at Y=100 (half-height 10 → top edge Y=90).
        // Body A: dynamic circle falling onto floor.
        var floorEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var bodyEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 50f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });

        world.Flush();
        Step(world, physics, 2);

        var floorBody = floorEntity.GetComponent<PhysicsBodyComponent>()!;
        var dynBody = bodyEntity.GetComponent<PhysicsBodyComponent>()!;

        _physicsWorld.IgnoreCollision(floorBody, dynBody);

        var transformBefore = bodyEntity.GetComponent<TransformComponent>()!.Position;
        Step(world, physics, 20);
        var transformAfter = bodyEntity.GetComponent<TransformComponent>()!.Position;

        // Body should have passed through the floor (Y increased past 90).
        Assert.True(transformAfter.Y > 90f, $"Expected body to pass through ignored floor, Y={transformAfter.Y}");
    }

    [Fact]
    public void RestoreCollision_BodiesCollideAgain()
    {
        var world = CreateTestWorld();
        var physics = CreatePhysicsSystem();

        var floorEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 200f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var bodyEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 50f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });

        world.Flush();
        Step(world, physics, 2);

        var floorBody = floorEntity.GetComponent<PhysicsBodyComponent>()!;
        var dynBody = bodyEntity.GetComponent<PhysicsBodyComponent>()!;

        _physicsWorld.IgnoreCollision(floorBody, dynBody);
        Step(world, physics, 5);
        _physicsWorld.RestoreCollision(floorBody, dynBody);

        // After restore, stepping again should stop the body at the floor.
        Step(world, physics, 30);

        var yAfter = bodyEntity.GetComponent<TransformComponent>()!.Position.Y;
        Assert.True(yAfter < 210f, $"Expected body to rest on floor after collision restored, Y={yAfter}");
    }

    [Fact]
    public void IgnoreCollision_PurgedOnBodyDestroy_NewBodyAtSameSlotCollides()
    {
        var world = CreateTestWorld();
        var physics = CreatePhysicsSystem();

        // Floor that we will ignore, then destroy and recreate.
        var floorEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 80f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 30f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });

        world.Flush();
        Step(world, physics, 2);

        var floorBody = floorEntity.GetComponent<PhysicsBodyComponent>()!;
        var dynBody = dynEntity.GetComponent<PhysicsBodyComponent>()!;
        _physicsWorld.IgnoreCollision(floorBody, dynBody);

        // Destroy the floor entity — should purge the ignored pair.
        floorEntity.Destroy();
        world.Flush();
        Step(world, physics, 2);

        // Create a new floor — may reuse the recycled index1 slot.
        var newFloorEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 80f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        world.Flush();

        // Reset the dynamic body above the new floor.
        var dynTransform = dynEntity.GetComponent<TransformComponent>()!;
        dynBody.Teleport(new Vector2(0f, 30f));
        Step(world, physics, 30);

        var yAfter = dynEntity.GetComponent<TransformComponent>()!.Position.Y;
        Assert.True(yAfter < 90f, $"New floor should collide (no stale ignore). Y={yAfter}");
    }

    [Fact]
    public void IgnoreCollision_SafeToCallMultipleTimes_SamePair()
    {
        var world = CreateTestWorld();
        var physics = CreatePhysicsSystem();

        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });

        var entityB = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });

        world.Flush();
        Step(world, physics);

        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
        var bodyB = entityB.GetComponent<PhysicsBodyComponent>()!;

        var ex = Record.Exception(() =>
        {
            _physicsWorld.IgnoreCollision(bodyA, bodyB);
            _physicsWorld.IgnoreCollision(bodyA, bodyB);
            _physicsWorld.IgnoreCollision(bodyA, bodyB);
        });

        Assert.Null(ex);
    }

    [Fact]
    public void RestoreCollision_WhenNotIgnored_DoesNotThrow()
    {
        var world = CreateTestWorld();
        var physics = CreatePhysicsSystem();

        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });

        var entityB = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });

        world.Flush();
        Step(world, physics);

        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
        var bodyB = entityB.GetComponent<PhysicsBodyComponent>()!;

        var ex = Record.Exception(() => _physicsWorld.RestoreCollision(bodyA, bodyB));
        Assert.Null(ex);
    }
}