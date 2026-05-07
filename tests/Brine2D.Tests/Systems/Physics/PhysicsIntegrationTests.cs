using System.Numerics;
using Box2D.NET.Bindings;
using Brine2D.Collision;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Components.Joints;
using Brine2D.Physics;
using Brine2D.Systems.Physics;

namespace Brine2D.Tests.Systems.Physics;

[Collection("Physics")]
public class PhysicsIntegrationTests : TestBase, IDisposable
{
    private static readonly GameTime FixedTime = new(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));

    private readonly PhysicsWorld _physicsWorld = new();

    public void Dispose()
    {
        _physicsWorld.Dispose();
    }

    private Box2DPhysicsSystem CreateSystem() => new(_physicsWorld);

    private void Step(IEntityWorld world, Box2DPhysicsSystem system, int count = 1)
    {
        for (int i = 0; i < count; i++)
            system.FixedUpdate(world, FixedTime);
    }

    // ── Collision events ──────────────────────────────────────────────────────

    [Fact]
    public void OnCollisionEnter_ThenExit_FiresInOrder()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        bool entered = false;
        bool exited = false;

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(40f);
                c.BodyType = PhysicsBodyType.Static;
                c.OnCollisionEnter += (_, _) => entered = true;
                c.OnCollisionExit += _ => exited = true;
            });

        var movingEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(50f, 0f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(40f));
        world.Flush();

        Step(world, system, 3);
        Assert.True(entered);

        // Move away so the bodies separate.
        movingEntity.GetComponent<TransformComponent>()!.LocalPosition = new Vector2(5000f, 0f);
        movingEntity.GetComponent<PhysicsBodyComponent>()!.IsDirty = true;
        world.Flush();
        Step(world, system, 3);

        Assert.True(exited);
    }

    [Fact]
    public void OnCollisionStay_TwoBodiesOverlapping_Fires()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        bool stayFired = false;

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(40f);
                c.BodyType = PhysicsBodyType.Static;
                c.OnCollisionStay += (_, _) => stayFired = true;
            });

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(50f, 0f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(40f));
        world.Flush();

        Step(world, system, 5);

        Assert.True(stayFired);
    }

    // ── IgnoreCollision / RestoreCollision ────────────────────────────────────

    [Fact]
    public void IgnoreCollision_SuppressesContactEvents()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        bool collisionFired = false;

        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(40f);
                c.BodyType = PhysicsBodyType.Static;
                c.OnCollisionEnter += (_, _) => collisionFired = true;
            });

        var entityB = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(50f, 0f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(40f));
        world.Flush();

        Step(world, system);

        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
        var bodyB = entityB.GetComponent<PhysicsBodyComponent>()!;
        _physicsWorld.IgnoreCollision(bodyA, bodyB);

        collisionFired = false;
        Step(world, system, 5);

        Assert.False(collisionFired);
    }

    [Fact]
    public void RestoreCollision_AfterIgnore_ResumesContactEvents()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        bool collisionFired = false;

        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(40f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var entityB = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(50f, 0f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(40f));
        world.Flush();

        Step(world, system);

        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
        var bodyB = entityB.GetComponent<PhysicsBodyComponent>()!;
        _physicsWorld.IgnoreCollision(bodyA, bodyB);
        Step(world, system, 3);

        // Separate, then restore and re-approach.
        entityB.GetComponent<TransformComponent>()!.LocalPosition = new Vector2(5000f, 0f);
        bodyB.IsDirty = true;
        world.Flush();
        Step(world, system, 3);

        _physicsWorld.RestoreCollision(bodyA, bodyB);

        entityB.GetComponent<TransformComponent>()!.LocalPosition = new Vector2(50f, 0f);
        bodyB.IsDirty = true;
        world.Flush();

        bodyA.OnCollisionEnter += (_, _) => collisionFired = true;
        Step(world, system, 5);

        Assert.True(collisionFired);
    }

    [Fact]
    public void IgnoreCollision_BodyDestroyed_NewBodyAtSameSlot_DoesNotInheritIgnore()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(40f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var entityB = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(5000f, 0f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(40f));
        world.Flush();
        Step(world, system);

        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
        var bodyB = entityB.GetComponent<PhysicsBodyComponent>()!;
        _physicsWorld.IgnoreCollision(bodyA, bodyB);

        // Destroy B — PurgeIgnoredPairsForBody fires in DestroyBody (Bug #2 fix).
        entityB.Destroy();
        world.Flush();
        Step(world, system);

        bool newCollision = false;
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(50f, 0f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(40f));
        world.Flush();

        bodyA.OnCollisionEnter += (_, _) => newCollision = true;
        Step(world, system, 5);

        Assert.True(newCollision, "New body at recycled slot inherited stale IgnoreCollision entry (Bug #2).");
    }

    // ── ShouldCollide per-body filter ─────────────────────────────────────────
    // Note: ShouldCollide uses a Box2D pre-solve callback with [UnmanagedCallersOnly].
    // When called on the JIT interpreter (no NativeAOT/ReadyToRun), Box2D.NET.Bindings
    // throws InvalidProgramException due to non-blittable bool return. These tests
    // are skipped until the upstream binding is updated.

    [Fact]
    public void ShouldCollide_ReturnsFalse_SuppressesContact()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();
        bool collisionFired = false;

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(40f);
                c.BodyType = PhysicsBodyType.Static;
                c.ShouldCollide = _ => false;
                c.OnCollisionEnter += (_, _) => collisionFired = true;
            });

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(50f, 0f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(40f));
        world.Flush();

        Step(world, system, 5);

        Assert.False(collisionFired);
    }

    [Fact]
    public void SetCustomCollisionFilter_AffectsAllPairs()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();
        bool collisionFired = false;

        _physicsWorld.SetCustomCollisionFilter((_, _) => false);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(40f);
                c.BodyType = PhysicsBodyType.Static;
                c.OnCollisionEnter += (_, _) => collisionFired = true;
            });

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(50f, 0f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(40f));
        world.Flush();

        Step(world, system, 5);

        Assert.False(collisionFired);
        _physicsWorld.SetCustomCollisionFilter(null);
    }

    // ── GravityOverride ───────────────────────────────────────────────────────

    [Fact]
    public void GravityOverride_Body_IgnoresWorldGravity()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.GravityOverride = Vector2.Zero;
            });

        var entityB = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(200f, 0f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));
        world.Flush();

        Step(world, system, 5);

        var posA = entityA.GetComponent<TransformComponent>()!.Position.Y;
        var posB = entityB.GetComponent<TransformComponent>()!.Position.Y;

        Assert.True(posA < posB, "Body with GravityOverride=0 should fall less than body under world gravity.");
    }

    // ── FreezePositionX / FreezePositionY ────────────────────────────────────

    [Fact]
    public void FreezePositionX_DynamicBody_DoesNotMoveHorizontally()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.FreezePositionX = true;
            });
        world.Flush();

        Step(world, system, 5);

        var x = entity.GetComponent<TransformComponent>()!.Position.X;
        Assert.Equal(100f, x, precision: 1);
    }

    [Fact]
    public void FreezePositionY_DynamicBody_DoesNotMoveVertically()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.FreezePositionY = true;
            });
        world.Flush();

        Step(world, system, 5);

        // Box2D needs ~1 step to read back the lock; allow 2px tolerance.
        var y = entity.GetComponent<TransformComponent>()!.Position.Y;
        Assert.True(MathF.Abs(y - 100f) < 2f, $"Expected Y≈100 but got {y}");
    }

    // ── Teleport suppresses phantom velocity ──────────────────────────────────

    [Fact]
    public void Teleport_KinematicBody_SuppressesPhantomVelocity()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Kinematic;
            });
        world.Flush();
        Step(world, system);

        var body = entity.GetComponent<PhysicsBodyComponent>()!;
        body.Teleport(new Vector2(5000f, 5000f));
        world.Flush();
        Step(world, system);

        var vel = B2.BodyGetLinearVelocity(body.BodyId);
        float speed = MathF.Sqrt(vel.x * vel.x + vel.y * vel.y);
        Assert.Equal(0f, speed, precision: 1);
    }

    // ── Raycast ───────────────────────────────────────────────────────────────

    [Fact]
    public void RaycastClosest_HitsLiveBody_ReturnsHit()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(500f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(40f, 40f);
                c.BodyType = PhysicsBodyType.Static;
            });
        world.Flush();
        Step(world, system);

        var hit = _physicsWorld.RaycastClosest(new Vector2(0f, 0f), new Vector2(1f, 0f), 1000f);

        Assert.NotNull(hit);
        Assert.NotNull(hit.Value.Component);
    }

    [Fact]
    public void RaycastAll_ReturnsMultipleHits()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        for (int i = 1; i <= 3; i++)
        {
            world.CreateEntity()
                .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(i * 150f, 0f))
                .AddComponent<PhysicsBodyComponent>(c =>
                {
                    c.Shape = new BoxShape(20f, 200f);
                    c.BodyType = PhysicsBodyType.Static;
                });
        }
        world.Flush();
        Step(world, system);

        var buffer = new RaycastHit[8];
        int count = _physicsWorld.RaycastAll(new Vector2(0f, 0f), new Vector2(1f, 0f), 1000f, buffer);

        Assert.True(count >= 3);
    }

    // ── ShapeCast ─────────────────────────────────────────────────────────────

    [Fact]
    public void ShapeCastClosest_HitsBody_ReturnsHit()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(300f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(40f, 40f);
                c.BodyType = PhysicsBodyType.Static;
            });
        world.Flush();
        Step(world, system);

        var hit = _physicsWorld.ShapeCastClosest(new Vector2(0f, 0f), 10f, new Vector2(1f, 0f), 500f);

        Assert.NotNull(hit);
    }

    // ── Overlap queries ───────────────────────────────────────────────────────

    [Fact]
    public void OverlapCircle_LiveBody_ReturnsHit()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(40f, 40f);
                c.BodyType = PhysicsBodyType.Static;
            });
        world.Flush();
        Step(world, system);

        var buffer = new OverlapHit[8];
        int count = _physicsWorld.OverlapCircle(Vector2.Zero, 100f, buffer);

        Assert.True(count >= 1);
    }

    [Fact]
    public void OverlapAABB_LiveBody_ReturnsHit()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(40f, 40f);
                c.BodyType = PhysicsBodyType.Static;
            });
        world.Flush();
        Step(world, system);

        var buffer = new OverlapHit[8];
        int count = _physicsWorld.OverlapAABB(new Vector2(-100f, -100f), new Vector2(100f, 100f), buffer);

        Assert.True(count >= 1);
    }

    // ── GetContacts ───────────────────────────────────────────────────────────

    [Fact]
    public void GetContactsAll_TwoBodiesInContact_ReturnsPairs()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(40f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var movingEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(50f, 0f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(40f));
        world.Flush();

        Step(world, system, 3);

        var body = movingEntity.GetComponent<PhysicsBodyComponent>()!;
        var contacts = new List<ContactPair>();
        _physicsWorld.GetContactsAll(body, contacts);

        Assert.NotEmpty(contacts);
    }

    // ── Joints ────────────────────────────────────────────────────────────────

    [Fact]
    public void RevoluteJoint_Created_IsLive()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
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
                c.BodyType = PhysicsBodyType.Static;
            });
        world.Flush();
        Step(world, system);

        var bodyB = entityB.GetComponent<PhysicsBodyComponent>()!;

        entityA.AddComponent<RevoluteJointComponent>(j => j.ConnectedBody = bodyB);
        world.Flush();
        Step(world, system);

        var joint = entityA.GetComponent<RevoluteJointComponent>()!;
        Assert.True(joint.IsLive);
    }

    [Fact]
    public void DistanceJoint_BreakForce_FiresOnBreakCallback()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        bool broke = false;

        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.Mass = 1f;
            });

        var entityB = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(50f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Static;
            });
        world.Flush();
        Step(world, system);

        var bodyB = entityB.GetComponent<PhysicsBodyComponent>()!;
        entityA.AddComponent<DistanceJointComponent>(j =>
        {
            j.ConnectedBody = bodyB;
            j.Length = 50f;
            j.BreakForce = 0.001f;
            j.OnBreak += _ => broke = true;
        });
        world.Flush();

        Step(world, system, 10);

        Assert.True(broke);
    }

    // ── Body rebuild preserves velocity ──────────────────────────────────────

    [Fact]
    public void RebuildBody_WhileLive_PreservesLinearVelocity()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));
        world.Flush();

        Step(world, system, 5);

        var body = entity.GetComponent<PhysicsBodyComponent>()!;
        float velBefore = B2.BodyGetLinearVelocity(body.BodyId).y;
        Assert.True(velBefore > 0f);

        body.IsDirty = true;
        Step(world, system);

        float velAfter = B2.BodyGetLinearVelocity(body.BodyId).y;
        Assert.True(velAfter > 0f, "Velocity should be preserved across body rebuild.");
    }

    // ── Body type change ─────────────────────────────────────────────────────

    [Fact]
    public void BodyTypeChange_DynamicToStatic_StopsMoving()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));
        world.Flush();

        Step(world, system, 3);

        var body = entity.GetComponent<PhysicsBodyComponent>()!;
        var transform = entity.GetComponent<TransformComponent>()!;

        float yBefore = transform.Position.Y;

        body.BodyType = PhysicsBodyType.Static;
        Step(world, system, 5);

        float yAfter = transform.Position.Y;
        Assert.Equal(yBefore, yAfter, precision: 1);
    }

    // ── One-way platform ─────────────────────────────────────────────────────

    [Fact]
    public void IsOneWayPlatform_BodyPassesThroughFromBelow_DoesNotCollide()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        bool collisionFired = false;

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(200f, 10f);
                c.BodyType = PhysicsBodyType.Static;
                c.IsOneWayPlatform = true;
                c.OnCollisionEnter += (_, _) => collisionFired = true;
            });

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.InitialLinearVelocity = new Vector2(0f, -500f);
            });
        world.Flush();

        Step(world, system, 5);

        Assert.False(collisionFired);
    }
}