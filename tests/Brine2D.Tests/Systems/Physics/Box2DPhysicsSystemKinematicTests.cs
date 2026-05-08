using System.Numerics;
using Box2D.NET.Bindings;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Physics;
using Brine2D.Systems.Physics;

namespace Brine2D.Tests.Systems.Physics;

[Collection("Physics")]
public class Box2DPhysicsSystemKinematicTests : PhysicsTestBase, IDisposable
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

        // Run a couple of ticks at position (0,0) to seed prev-state.
        system.FixedUpdate(world, FixedTime);
        system.FixedUpdate(world, FixedTime);

        var body = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;

        // Disable simulation — prev-state should be cleared.
        body.IsSimulationEnabled = false;
        system.FixedUpdate(world, FixedTime);

        // Move the transform a large distance while disabled, simulating real elapsed time.
        var transform = world.Entities.First().GetComponent<TransformComponent>()!;
        transform.LocalPosition = new Vector2(10_000f, 10_000f);

        // Re-enable simulation — on the first tick the velocity must be zeroed, not derived
        // from the stale pre-disable position vs. the current position.
        body.IsSimulationEnabled = true;
        system.FixedUpdate(world, FixedTime);

        var vel = B2.BodyGetLinearVelocity(body.BodyId);
        Assert.Equal(0f, vel.x, 0.001f);
        Assert.Equal(0f, vel.y, 0.001f);
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

        // Rotate heavily while disabled.
        transform.Rotation = MathF.PI;

        body.IsSimulationEnabled = true;
        system.FixedUpdate(world, FixedTime);

        Assert.Equal(0f, B2.BodyGetAngularVelocity(body.BodyId), 0.001f);
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

        // Seed prev-state at (0,0).
        system.FixedUpdate(world, FixedTime);

        // Move 60 pixels in X over one tick at 60 Hz → expected velocity = 3600 px/s.
        var transform = world.Entities.First().GetComponent<TransformComponent>()!;
        transform.LocalPosition = new Vector2(60f, 0f);
        system.FixedUpdate(world, FixedTime);

        var body = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        var vel = B2.BodyGetLinearVelocity(body.BodyId);
        Assert.True(vel.x > 0f, "Expected positive X velocity from kinematic movement.");
        Assert.Equal(0f, vel.y, 0.001f);
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
        var transform = world.Entities.First().GetComponent<TransformComponent>()!;

        // Teleport the body — must not produce a large velocity.
        transform.LocalPosition = new Vector2(5_000f, 5_000f);
        body.IsTeleporting = true;
        system.FixedUpdate(world, FixedTime);

        var vel = B2.BodyGetLinearVelocity(body.BodyId);
        Assert.Equal(0f, vel.x, 0.001f);
        Assert.Equal(0f, vel.y, 0.001f);
    }
}