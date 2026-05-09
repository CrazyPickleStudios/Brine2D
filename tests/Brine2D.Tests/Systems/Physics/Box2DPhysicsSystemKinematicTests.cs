using System.Numerics;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Physics;
using Brine2D.Systems.Physics;

namespace Brine2D.Tests.Systems.Physics;

public class Box2DPhysicsSystemKinematicTests : PhysicsTestBase
{
    [Fact]
    public void KinematicBody_AfterSimulationDisableAndReEnable_HasZeroLinearVelocity()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(PhysicsWorld);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Kinematic;
            });
        world.Flush();

        system.FixedUpdate(world, FixedTime);
        system.FixedUpdate(world, FixedTime);

        var body = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;

        body.IsSimulationEnabled = false;
        system.FixedUpdate(world, FixedTime);

        var transform = world.Entities.First().GetComponent<TransformComponent>()!;
        transform.LocalPosition = new Vector2(10_000f, 10_000f);

        body.IsSimulationEnabled = true;
        system.FixedUpdate(world, FixedTime);

        Assert.Equal(0f, body.LinearVelocity.X, 0.001f);
        Assert.Equal(0f, body.LinearVelocity.Y, 0.001f);
    }

    [Fact]
    public void KinematicBody_AfterSimulationDisableAndReEnable_HasZeroAngularVelocity()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(PhysicsWorld);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Kinematic;
            });
        world.Flush();

        system.FixedUpdate(world, FixedTime);
        system.FixedUpdate(world, FixedTime);

        var body = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        var transform = world.Entities.First().GetComponent<TransformComponent>()!;

        body.IsSimulationEnabled = false;
        system.FixedUpdate(world, FixedTime);

        transform.Rotation = MathF.PI;

        body.IsSimulationEnabled = true;
        system.FixedUpdate(world, FixedTime);

        Assert.Equal(0f, body.AngularVelocity, 0.001f);
    }

    [Fact]
    public void KinematicBody_NormalMovement_DrivesExpectedLinearVelocity()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(PhysicsWorld);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Kinematic;
            });
        world.Flush();

        system.FixedUpdate(world, FixedTime);

        var transform = world.Entities.First().GetComponent<TransformComponent>()!;
        transform.LocalPosition = new Vector2(60f, 0f);
        system.FixedUpdate(world, FixedTime);

        var body = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        Assert.True(body.LinearVelocity.X > 0f, "Expected positive X velocity from kinematic movement.");
        Assert.Equal(0f, body.LinearVelocity.Y, 0.001f);
    }

    [Fact]
    public void KinematicBody_Teleport_DoesNotProducePhantomVelocity()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(PhysicsWorld);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Kinematic;
            });
        world.Flush();

        system.FixedUpdate(world, FixedTime);

        var body = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;

        body.Teleport(new Vector2(5_000f, 5_000f));
        system.FixedUpdate(world, FixedTime);

        Assert.Equal(0f, body.LinearVelocity.X, 0.001f);
        Assert.Equal(0f, body.LinearVelocity.Y, 0.001f);
    }
}