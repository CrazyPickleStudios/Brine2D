using Brine2D.Collision;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Components.Joints;
using Brine2D.Physics;
using Brine2D.Systems.Physics;
using Microsoft.Extensions.Logging.Abstractions;
using System.Numerics;

namespace Brine2D.Tests.Systems.Physics;

public class PhysicsIntegrationTests : PhysicsTestBase
{
    private Box2DPhysicsSystem CreateSystem() => new(PhysicsWorld);

    private void Step(IEntityWorld world, Box2DPhysicsSystem system, int count = 1)
    {
        for (int i = 0; i < count; i++)
            system.FixedUpdate(world, FixedTime);
    }

    private (IEntityWorld world, Box2DPhysicsSystem physics, KinematicCharacterSystem pre, KinematicCharacterSystem post) CreateKinematicSystems()
    {
        var world = CreateTestWorld();
        var physics = new Box2DPhysicsSystem(PhysicsWorld);
        var pre = new KinematicCharacterSystem(PhysicsWorld, isPostStep: false, NullLogger<KinematicCharacterSystem>.Instance);
        var post = new KinematicCharacterSystem(PhysicsWorld, isPostStep: true, NullLogger<KinematicCharacterSystem>.Instance);
        return (world, physics, pre, post);
    }

    private void Step(IEntityWorld world, Box2DPhysicsSystem physics,
        KinematicCharacterSystem pre, KinematicCharacterSystem post, int steps = 1)
    {
        for (int i = 0; i < steps; i++)
        {
            pre.FixedUpdate(world, FixedTime);
            physics.FixedUpdate(world, FixedTime);
            post.FixedUpdate(world, FixedTime);
        }
    }

    // Collision events

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

    // IgnoreCollision / RestoreCollision

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
        PhysicsWorld.IgnoreCollision(bodyA, bodyB);

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
        PhysicsWorld.IgnoreCollision(bodyA, bodyB);
        Step(world, system, 3);

        entityB.GetComponent<TransformComponent>()!.LocalPosition = new Vector2(5000f, 0f);
        bodyB.IsDirty = true;
        world.Flush();
        Step(world, system, 3);

        PhysicsWorld.RestoreCollision(bodyA, bodyB);

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
        PhysicsWorld.IgnoreCollision(bodyA, bodyB);

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

        Assert.True(newCollision, "New body at recycled slot should not inherit stale IgnoreCollision entry.");
    }

    // Custom filters — skipped due to [UnmanagedCallersOnly] non-blittable bool return crashing JIT in CI

    [Fact(Skip = "ShouldCollide uses [UnmanagedCallersOnly] with non-blittable bool return - crashes JIT in CI")]
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

    [Fact(Skip = "SetCustomCollisionFilter uses [UnmanagedCallersOnly] with non-blittable bool return - crashes JIT in CI")]
    public void SetCustomCollisionFilter_AffectsAllPairs()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();
        bool collisionFired = false;

        PhysicsWorld.SetCustomCollisionFilter((_, _) => false);

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
        PhysicsWorld.SetCustomCollisionFilter(null);
    }

    // Gravity

    [Fact]
    public void GravityOverride_BodyMovesInOverrideDirection_NotWorldGravity()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var overrideEntity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityOverride = new Vector2(0f, -980f);
            });

        var normalEntity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });

        world.Flush();
        Step(world, system, 10);

        Assert.True(overrideEntity.GetComponent<TransformComponent>()!.Position.Y < 0f,
            "Body with upward GravityOverride should move up.");
        Assert.True(normalEntity.GetComponent<TransformComponent>()!.Position.Y > 0f,
            "Body under world gravity should move down.");
    }

    [Fact]
    public void GravityOverride_SetToNull_RestoresWorldGravity()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityOverride = new Vector2(0f, -980f);
            });

        world.Flush();
        Step(world, system, 5);

        var body = entity.GetComponent<PhysicsBodyComponent>()!;
        var transform = entity.GetComponent<TransformComponent>()!;
        float yAfterOverride = transform.Position.Y;

        body.GravityOverride = null;
        Step(world, system, 10);

        Assert.True(transform.Position.Y > yAfterOverride,
            "After clearing GravityOverride the body should fall under world gravity.");
    }

    [Fact]
    public void GravityScale_Zero_BodyDoesNotFall()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityScale = 0f;
            });

        world.Flush();
        Step(world, system, 10);

        Assert.Equal(0f, entity.GetComponent<TransformComponent>()!.Position.Y, 0.01f);
    }

    // Freeze Position

    [Fact]
    public void FreezePositionX_DynamicBodyReceivesHorizontalImpulse_XVelocityRemainsZero()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityScale = 0f;
                c.FreezePositionX = true;
            });

        world.Flush();
        Step(world, system);

        var body = entity.GetComponent<PhysicsBodyComponent>()!;
        body.ApplyLinearImpulse(new Vector2(1000f, 0f));
        Step(world, system, 5);

        Assert.Equal(0f, body.LinearVelocity.X, 0.01f);
    }

    [Fact]
    public void FreezePositionY_DynamicBodyUnderGravity_YVelocityRemainsZero()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.FreezePositionY = true;
            });

        world.Flush();
        Step(world, system, 10);

        var body = entity.GetComponent<PhysicsBodyComponent>()!;
        Assert.Equal(0f, body.LinearVelocity.Y, 0.01f);
    }

    // IsSimulationEnabled

    [Fact]
    public void IsSimulationEnabled_False_BodyStopsMoving()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });

        world.Flush();
        Step(world, system, 3);

        var transform = entity.GetComponent<TransformComponent>()!;
        var body = entity.GetComponent<PhysicsBodyComponent>()!;

        body.IsSimulationEnabled = false;
        Step(world, system);

        float yWhenDisabled = transform.Position.Y;
        Step(world, system, 5);

        Assert.Equal(yWhenDisabled, transform.Position.Y, 0.01f);
    }

    [Fact]
    public void IsSimulationEnabled_Disable_FiresCollisionExit()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 50f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });

        world.Flush();
        Step(world, system, 20);

        bool exitFired = false;
        dynEntity.GetComponent<PhysicsBodyComponent>()!.OnCollisionExit += _ => exitFired = true;
        dynEntity.GetComponent<PhysicsBodyComponent>()!.IsSimulationEnabled = false;
        Step(world, system, 2);

        Assert.True(exitFired, "Disabling simulation should flush contact pairs and fire OnCollisionExit.");
    }

    [Fact]
    public void IsSimulationEnabled_ReEnable_ContactEnterFiresAgain()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 50f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });

        world.Flush();

        int enterCount = 0;
        dynEntity.GetComponent<PhysicsBodyComponent>()!.OnCollisionEnter += (_, _) => enterCount++;

        Step(world, system, 20);
        int countAfterLanding = enterCount;
        Assert.True(countAfterLanding >= 1);

        var body = dynEntity.GetComponent<PhysicsBodyComponent>()!;
        var transform = dynEntity.GetComponent<TransformComponent>()!;

        body.IsSimulationEnabled = false;
        Step(world, system, 3);

        transform.Position = new Vector2(0f, 50f);
        body.IsSimulationEnabled = true;
        Step(world, system, 20);

        Assert.True(enterCount > countAfterLanding,
            "OnCollisionEnter should fire again after re-enabling simulation and landing.");
    }

    [Fact]
    public void PhysicsWorld_Pause_SuppressesSimulation()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });
        world.Flush();

        Step(world, system, 2);

        var transform = entity.GetComponent<TransformComponent>()!;
        var posBefore = transform.Position;

        PhysicsWorld.Pause();
        Step(world, system, 2);

        Assert.Equal(posBefore, transform.Position);

        PhysicsWorld.Resume();
        Step(world, system, 2);

        Assert.True(transform.Position.Y > posBefore.Y, "Body should have moved after Resume.");
    }

    // Teleport

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

        Assert.Equal(0f, body.LinearVelocity.Length(), 0.1f);
    }

    // Body type changes

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

        Assert.Equal(yBefore, transform.Position.Y, precision: 1);
    }

    [Fact]
    public void BodyType_StaticToDynamic_OnLiveBody_BodyFallsWithGravity()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Static;
            });

        world.Flush();
        Step(world, system, 3);

        var body = entity.GetComponent<PhysicsBodyComponent>()!;
        var transform = entity.GetComponent<TransformComponent>()!;
        float yBefore = transform.Position.Y;

        body.BodyType = PhysicsBodyType.Dynamic;
        Step(world, system, 10);

        Assert.True(transform.Position.Y > yBefore, "Body switched to Dynamic should fall under gravity.");
    }

    [Fact]
    public void BodyType_DynamicToKinematic_OnLiveBody_BodyPreservesPosition()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 50f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityScale = 0f;
            });

        world.Flush();
        Step(world, system, 3);

        var body = entity.GetComponent<PhysicsBodyComponent>()!;
        var transform = entity.GetComponent<TransformComponent>()!;
        float yBefore = transform.Position.Y;

        body.BodyType = PhysicsBodyType.Kinematic;
        Step(world, system, 3);

        Assert.Equal(yBefore, transform.Position.Y, 0.5f);
    }

    // One-way platform

    [Fact]
    public void IsOneWayPlatform_BodyApproachingFromSolidSide_Collides()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
                c.IsOneWayPlatform = true;
                c.PlatformNormalDirection = new Vector2(0f, -1f);
            });

        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 50f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });

        world.Flush();

        bool landed = false;
        dynEntity.GetComponent<PhysicsBodyComponent>()!.OnCollisionEnter += (_, _) => landed = true;

        Step(world, system, 30);

        Assert.True(landed, "Body falling onto the solid side should collide with the one-way platform.");
    }

    [Fact]
    public void IsOneWayPlatform_BodyApproachingFromPassThroughSide_PassesThrough()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var platformEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
                c.IsOneWayPlatform = true;
                c.PlatformNormalDirection = new Vector2(0f, -1f);
            });

        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 50f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityScale = 0f;
                c.InitialLinearVelocity = new Vector2(0f, -300f);
            });

        world.Flush();
        Step(world, system, 15);

        var platformBody = platformEntity.GetComponent<PhysicsBodyComponent>()!;
        var finalY = dynEntity.GetComponent<TransformComponent>()!.Position.Y;

        Assert.True(finalY < -10f,
            $"Body should have passed through the one-way platform from below. " +
            $"FinalY={finalY:F1}, platformIsOWP={platformBody.IsOneWayPlatform}");
    }

    [Fact]
    public void IsOneWayPlatform_ToggleOffThenOn_CountNeverGoesNegative()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(100f, 20f);
                c.BodyType = PhysicsBodyType.Static;
                c.IsOneWayPlatform = true;
            });

        world.Flush();
        Step(world, system);

        var body = entity.GetComponent<PhysicsBodyComponent>()!;
        body.IsOneWayPlatform = false;
        body.IsOneWayPlatform = true;
        Step(world, system, 2);

        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(500f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(100f, 20f);
                c.BodyType = PhysicsBodyType.Static;
                c.IsOneWayPlatform = true;
            });

        world.Flush();
        Step(world, system);

        body.IsOneWayPlatform = false;
        Step(world, system);

        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(500f, -50f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });
        world.Flush();

        bool landed = false;
        dynEntity.GetComponent<PhysicsBodyComponent>()!.OnCollisionEnter += (_, _) => landed = true;

        Step(world, system, 30);

        Assert.True(landed, "OWP filter must remain installed while at least one body has IsOneWayPlatform=true.");
    }

    // Queries

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

        var hit = PhysicsWorld.RaycastClosest(new Vector2(0f, 0f), new Vector2(1f, 0f), 1000f);

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
        int count = PhysicsWorld.RaycastAll(new Vector2(0f, 0f), new Vector2(1f, 0f), 1000f, buffer);

        Assert.True(count >= 3);
    }

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

        var hit = PhysicsWorld.ShapeCastClosest(new Vector2(0f, 0f), 10f, new Vector2(1f, 0f), 500f);

        Assert.NotNull(hit);
    }

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
        int count = PhysicsWorld.OverlapCircle(Vector2.Zero, 100f, buffer);

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
        int count = PhysicsWorld.OverlapAABB(new Vector2(-100f, -100f), new Vector2(100f, 100f), buffer);

        Assert.True(count >= 1);
    }

    // Contacts

    [Fact]
    public void GetContacts_TwoBodiesInContact_ReturnsNonZero()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(200f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 50f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });

        world.Flush();
        Step(world, system, 25);

        var body = dynEntity.GetComponent<PhysicsBodyComponent>()!;
        var buffer = new ContactPair[8];
        int count = PhysicsWorld.GetContacts(body, buffer, out _);

        Assert.True(count > 0, "GetContacts should return at least one contact when resting on a surface.");
    }

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
        PhysicsWorld.GetContactsAll(body, contacts);

        Assert.NotEmpty(contacts);
    }

    // Joints

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

    // Body rebuild

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
        Assert.True(body.LinearVelocity.Y > 0f);

        body.IsDirty = true;
        Step(world, system);

        Assert.True(body.LinearVelocity.Y > 0f, "Velocity should be preserved across body rebuild.");
    }

    // MoveAndCollide with KinematicCharacterSystem

    [Fact]
    public void MoveAndCollide_HitsWall_LastHitSetAndRemainderCorrect()
    {
        var (world, physics, pre, post) = CreateKinematicSystems();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 400f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var charEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 20f);
                c.BodyType = PhysicsBodyType.Kinematic;
            })
            .AddComponent<KinematicCharacterBody>();

        world.Flush();
        Step(world, physics, pre, post);

        var character = charEntity.GetComponent<KinematicCharacterBody>()!;
        character.MoveAndCollide(new Vector2(200f, 0f));
        Step(world, physics, pre, post);

        Assert.NotNull(character.LastMoveAndCollideHit);
        Assert.True(character.MotionRemainder.X > 0f,
            "There should be leftover motion after hitting the wall.");
    }
}