using System.Numerics;
using Box2D.NET.Bindings;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Physics;
using Brine2D.Systems.Physics;

namespace Brine2D.Tests.Systems.Physics;

[Collection("Physics")]
public class Box2DPhysicsSystemBugFixTests : TestBase, IDisposable
{
    private static readonly GameTime FixedTime = new(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));
    private readonly PhysicsWorld _physicsWorld = new();

    public void Dispose() => _physicsWorld.Dispose();

    // -------------------------------------------------------------------------
    // Fix 1 – ChainShape double-offset
    // -------------------------------------------------------------------------

    [Fact]
    public void ChainShape_WithColliderOffset_BodyPositionMatchesTransformPlusOffset()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        var offset = new Vector2(50f, 0f);
        var origin = new Vector2(100f, 200f);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = origin)
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new ChainShape([
                    new Vector2(-100f, 0f),
                    new Vector2(100f, 0f)
                ]);
                c.BodyType = PhysicsBodyType.Static;
                c.Offset = offset;
            });
        world.Flush();
        system.FixedUpdate(world, FixedTime);

        var body = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        var bodyPos = B2.BodyGetPosition(body.BodyId);

        // The Box2D body origin must be at transform.Position + collider.Offset — not double that.
        Assert.Equal(origin.X + offset.X, bodyPos.x, 0.1f);
        Assert.Equal(origin.Y + offset.Y, bodyPos.y, 0.1f);
    }

    [Fact]
    public void ChainShape_ZeroOffset_BodyPositionMatchesTransform()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        var origin = new Vector2(80f, 120f);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = origin)
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new ChainShape([
                    new Vector2(-50f, 0f),
                    new Vector2(50f, 0f)
                ]);
                c.BodyType = PhysicsBodyType.Static;
            });
        world.Flush();
        system.FixedUpdate(world, FixedTime);

        var body = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        var bodyPos = B2.BodyGetPosition(body.BodyId);

        Assert.Equal(origin.X, bodyPos.x, 0.1f);
        Assert.Equal(origin.Y, bodyPos.y, 0.1f);
    }

    // -------------------------------------------------------------------------
    // Fix 2 – Kinematic FreezePosition axes: velocity must be zero on frozen axis
    // -------------------------------------------------------------------------

    [Fact]
    public void KinematicBody_FreezePositionX_VelocityXIsZeroAfterSync()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
                c.FreezePositionX = true;
            });
        world.Flush();
        system.FixedUpdate(world, FixedTime);

        var transform = entity.GetComponent<TransformComponent>()!;
        var body = entity.GetComponent<PhysicsBodyComponent>()!;

        // Move along both axes next tick.
        transform.Position = new Vector2(200f, 200f);
        system.FixedUpdate(world, FixedTime);

        var vel = B2.BodyGetLinearVelocity(body.BodyId);
        Assert.Equal(0f, vel.x, 0.01f);
        Assert.True(vel.y != 0f, "Y velocity must be non-zero because Y is not frozen.");
    }

    [Fact]
    public void KinematicBody_FreezePositionY_VelocityYIsZeroAfterSync()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
                c.FreezePositionY = true;
            });
        world.Flush();
        system.FixedUpdate(world, FixedTime);

        var transform = entity.GetComponent<TransformComponent>()!;
        var body = entity.GetComponent<PhysicsBodyComponent>()!;

        transform.Position = new Vector2(200f, 200f);
        system.FixedUpdate(world, FixedTime);

        var vel = B2.BodyGetLinearVelocity(body.BodyId);
        Assert.Equal(0f, vel.y, 0.01f);
        Assert.True(vel.x != 0f, "X velocity must be non-zero because X is not frozen.");
    }

    // -------------------------------------------------------------------------
    // Fix 3 – No double-decrement of _shouldCollideCount on component removal
    // -------------------------------------------------------------------------

    [Fact]
    public void RemovingBodyComponent_WithShouldCollide_DoesNotBreakFilterForNewBodies()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        // Create a body with ShouldCollide so _shouldCollideCount becomes 1.
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Static;
                c.ShouldCollide = _ => true;
            });
        world.Flush();
        system.FixedUpdate(world, FixedTime);

        // Remove the component — the system's DestroyBody runs, then OnRemoved fires.
        // Before fix-3, OnBodyDestroyed could fire twice, decrementing _shouldCollideCount to -1.
        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();
        system.FixedUpdate(world, FixedTime);

        // Now add a second body with ShouldCollide.  The filter callback must be installed
        // (the count must be 1, not 0 from underflow masking a -1 + 2 = 1 coincidence).
        bool filterCalled = false;
        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Static;
                c.ShouldCollide = other =>
                {
                    filterCalled = true;
                    return true;
                };
            });

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 5f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new BoxShape(20f, 20f));
        world.Flush();

        for (int i = 0; i < 5; i++)
            system.FixedUpdate(world, FixedTime);

        Assert.True(filterCalled, "ShouldCollide filter must be called — the system filter must still be installed.");
    }

    [Fact]
    public void RemovingBodyComponent_WithShouldCollide_CountNeverGoesNegative()
    {
        var world = CreateTestWorld();
        var system = new Box2DPhysicsSystem(_physicsWorld);

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Static;
                c.ShouldCollide = _ => true;
            });
        world.Flush();
        system.FixedUpdate(world, FixedTime);

        entity.RemoveComponent<PhysicsBodyComponent>();
        world.Flush();

        // If the double-decrement bug is present, the next FixedUpdate would have thrown
        // a Debug.Fail / Trace.TraceWarning for count-below-zero.
        // Running without exception confirms the count stayed at 0 (not -1).
        var ex = Record.Exception(() => system.FixedUpdate(world, FixedTime));
        Assert.Null(ex);
    }
}