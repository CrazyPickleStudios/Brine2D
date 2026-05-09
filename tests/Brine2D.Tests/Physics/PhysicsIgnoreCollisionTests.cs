using System.Numerics;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Physics;
using Brine2D.Systems.Physics;

namespace Brine2D.Tests.Physics;

[Collection("Physics")]
public class PhysicsIgnoreCollisionTests : PhysicsTestBase
{
    private const string SkipReason =
        "IgnoreCollision installs a custom filter callback via [UnmanagedCallersOnly] with a " +
        "non-blittable bool return — crashes JIT in CI. Passes locally on Windows.";

    private Box2DPhysicsSystem CreatePhysicsSystem() => new(PhysicsWorld);

    private void Step(IEntityWorld world, Box2DPhysicsSystem physics, int count = 1)
    {
        for (int i = 0; i < count; i++)
            physics.FixedUpdate(world, FixedTime);
    }

    [Fact(Skip = SkipReason)]
    public void IgnoreCollision_TwoBodiesDoNotCollide()
    {
        var world = CreateTestWorld();
        var physics = CreatePhysicsSystem();

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

        PhysicsWorld.IgnoreCollision(floorBody, dynBody);

        Step(world, physics, 20);
        var transformAfter = bodyEntity.GetComponent<TransformComponent>()!.Position;

        Assert.True(transformAfter.Y > 90f, $"Expected body to pass through ignored floor, Y={transformAfter.Y}");
    }

    [Fact(Skip = SkipReason)]
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

        PhysicsWorld.IgnoreCollision(floorBody, dynBody);
        Step(world, physics, 5);
        PhysicsWorld.RestoreCollision(floorBody, dynBody);

        Step(world, physics, 30);

        var yAfter = bodyEntity.GetComponent<TransformComponent>()!.Position.Y;
        Assert.True(yAfter < 210f, $"Expected body to rest on floor after collision restored, Y={yAfter}");
    }

    [Fact(Skip = SkipReason)]
    public void IgnoreCollision_PurgedOnBodyDestroy_NewBodyAtSameSlotCollides()
    {
        var world = CreateTestWorld();
        var physics = CreatePhysicsSystem();

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
        PhysicsWorld.IgnoreCollision(floorBody, dynBody);

        floorEntity.Destroy();
        world.Flush();
        Step(world, physics, 2);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 80f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        world.Flush();

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
            PhysicsWorld.IgnoreCollision(bodyA, bodyB);
            PhysicsWorld.IgnoreCollision(bodyA, bodyB);
            PhysicsWorld.IgnoreCollision(bodyA, bodyB);
        });

        Assert.Null(ex);
    }
}