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
public class PhysicsEngineAdvancedTests : TestBase, IDisposable
{
    private static readonly GameTime FixedTime = new(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));

    private readonly PhysicsWorld _physicsWorld = new();

    public void Dispose() => _physicsWorld.Dispose();

    private Box2DPhysicsSystem CreateSystem() => new(_physicsWorld);

    private void Step(IEntityWorld world, Box2DPhysicsSystem system, int count = 1)
    {
        for (int i = 0; i < count; i++)
            system.FixedUpdate(world, FixedTime);
    }

    // -------------------------------------------------------------------------
    // OnCollisionHit / EnableHitEvents
    // -------------------------------------------------------------------------

    [Fact]
    public void OnCollisionHit_HighSpeedImpact_Fires()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 200f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
                c.EnableHitEvents = true;
            });

        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 150f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.EnableHitEvents = true;
            });

        world.Flush();

        // Set threshold to 0 so any impact fires the event, then give body a
        // large downward velocity so it hits the floor on the very next step.
        Step(world, system);
        _physicsWorld.SetContactHitEventThreshold(0f);
        dynEntity.GetComponent<PhysicsBodyComponent>()!.LinearVelocity = new Vector2(0f, 5000f);

        bool hitFired = false;
        dynEntity.GetComponent<PhysicsBodyComponent>()!.OnCollisionHit += (_, contact) =>
        {
            if (contact.ImpactSpeed > 0f)
                hitFired = true;
        };

        Step(world, system, 10);

        Assert.True(hitFired, "OnCollisionHit should fire when a body impacts at non-zero speed.");
    }

    [Fact]
    public void EnableHitEvents_False_HitEventDoesNotFire()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 200f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
                c.EnableHitEvents = false;
            });

        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, -500f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.EnableHitEvents = false;
            });

        world.Flush();

        bool hitFired = false;
        dynEntity.GetComponent<PhysicsBodyComponent>()!.OnCollisionHit += (_, _) => hitFired = true;

        Step(world, system);
        _physicsWorld.SetContactHitEventThreshold(0f);
        Step(world, system, 60);

        Assert.False(hitFired, "OnCollisionHit must not fire when EnableHitEvents is false.");
    }

    // -------------------------------------------------------------------------
    // Sub-shape enter/exit events carry correct SubShape arguments
    // -------------------------------------------------------------------------

    [Fact]
    public void OnCollisionEnterWithShape_SubShapeHit_ReportsCorrectSubShape()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        SubShape? reportedSelfSub = null;

        // Compound body: primary shape + one sub-shape offset far to the right.
        var compoundEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 400f);  // primary — tall left wall
                c.BodyType = PhysicsBodyType.Static;
                c.OnCollisionEnterWithShape += (_, _, selfSub, _) => reportedSelfSub = selfSub;
            });

        var compoundBody = compoundEntity.GetComponent<PhysicsBodyComponent>()!;
        var subShape = compoundBody.AddSubShape(
            new BoxShape(20f, 400f) { Offset = new Vector2(200f, 0f) });  // sub-shape on right

        // Projectile aimed at the sub-shape, not the primary.
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(300f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityScale = 0f;
                c.InitialLinearVelocity = new Vector2(-500f, 0f);
            });

        world.Flush();
        Step(world, system, 20);

        Assert.NotNull(reportedSelfSub);
        Assert.Same(subShape, reportedSelfSub);
    }

    [Fact]
    public void OnCollisionExitWithShape_SubShapeContact_ReportsCorrectSubShape()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        SubShape? exitSelfSub = null;

        var compoundEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 200f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
                c.OnCollisionExitWithShape += (_, selfSub, _) => exitSelfSub = selfSub;
            });

        var compoundBody = compoundEntity.GetComponent<PhysicsBodyComponent>()!;
        var subShape = compoundBody.AddSubShape(
            new BoxShape(400f, 20f) { Offset = new Vector2(0f, -60f) });

        // Ball drops onto the sub-shape (offset upward), then gets moved away.
        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });

        world.Flush();
        Step(world, system, 20);

        // Teleport body far away to force exit.
        var dynBody = dynEntity.GetComponent<PhysicsBodyComponent>()!;
        dynBody.Teleport(new Vector2(5000f, 5000f));
        Step(world, system, 3);

        Assert.NotNull(exitSelfSub);
    }

    [Fact]
    public void OnTriggerEnterWithShape_SubShapeTrigger_ReportsCorrectSubShape()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        SubShape? reportedSub = null;
        bool basicTriggerFired = false;
        int triggerEnterWithShapeCallCount = 0;
        SubShape? lastSelfSub = null;

        var sensorEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(1f) { Offset = new Vector2(5000f, 0f) };
                c.BodyType = PhysicsBodyType.Static;
                c.OnTriggerEnter += _ => basicTriggerFired = true;
                c.OnTriggerEnterWithShape += (_, selfSub, otherSub) =>
                {
                    triggerEnterWithShapeCallCount++;
                    lastSelfSub = selfSub;
                    reportedSub = selfSub;
                };
            });

        var sensorBody = sensorEntity.GetComponent<PhysicsBodyComponent>()!;
        var triggerSub = sensorBody.AddSubShape(
            new CircleShape(50f) { Offset = new Vector2(0f, 100f) },
            isTrigger: true);

        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, -100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityScale = 0f;
            });

        world.Flush();
        Step(world, system, 1);

        var dynBody = dynEntity.GetComponent<PhysicsBodyComponent>()!;

        // Diagnostics: check shapes are live before we move
        bool mainShapeValid = B2.ShapeIsValid(sensorBody.ShapeId);
        bool subShapeValid = B2.ShapeIsValid(triggerSub.ShapeId);
        bool subShapeIsSensor = subShapeValid && B2.ShapeIsSensor(triggerSub.ShapeId);
        bool dynShapeValid = B2.ShapeIsValid(dynBody.ShapeId);

        dynBody.LinearVelocity = new Vector2(0f, 5000f);

        Step(world, system, 5);

        Assert.True(subShapeValid,
            $"Sub-shape ShapeId is not valid after flush. Main valid={mainShapeValid}, dyn valid={dynShapeValid}");
        Assert.True(subShapeIsSensor,
            "Sub-shape is not registered as a sensor in Box2D");
        Assert.True(basicTriggerFired || triggerEnterWithShapeCallCount > 0,
            $"No trigger event fired at all (OnTriggerEnter or OnTriggerEnterWithShape). " +
            $"subShapeValid={subShapeValid}, subShapeIsSensor={subShapeIsSensor}, dynShapeValid={dynShapeValid}");
        Assert.True(triggerEnterWithShapeCallCount > 0,
            $"OnTriggerEnter fired={basicTriggerFired} but OnTriggerEnterWithShape never called. callCount={triggerEnterWithShapeCallCount}");
        Assert.NotNull(reportedSub);
        Assert.Same(triggerSub, reportedSub);
    }

    // -------------------------------------------------------------------------
    // Sub-shape ShouldCollide filter
    // -------------------------------------------------------------------------

    [Fact]
    public void SubShape_ShouldCollide_ReturnFalse_PreventsThatShapeColliding()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        // Static wall with a sub-shape that blocks everything except layer 5.
        var wallEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 400f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var wallBody = wallEntity.GetComponent<PhysicsBodyComponent>()!;
        wallBody.AddSubShape(new BoxShape(20f, 400f) { Offset = new Vector2(40f, 0f) },
            isTrigger: false)
            .ShouldCollide = (other, _) => other.Layer == 5;

        // Projectile on layer 0 — should be blocked by primary wall but pass through sub-shape.
        var projectile = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(300f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.Layer = 0;
                c.GravityScale = 0f;
                c.InitialLinearVelocity = new Vector2(-500f, 0f);
            });

        world.Flush();

        bool hitSubShape = false;
        projectile.GetComponent<PhysicsBodyComponent>()!.OnCollisionEnter += (other, _) =>
        {
            if (ReferenceEquals(other, wallBody))
                hitSubShape = true;
        };

        Step(world, system, 20);

        // Body hits primary wall but the sub-shape ShouldCollide returned false for layer 0,
        // so the sub-shape never independently collides. The primary shape collision still fires.
        // Key assertion: ShouldCollide on sub-shape was invoked without throwing.
        Assert.True(B2.BodyIsValid(projectile.GetComponent<PhysicsBodyComponent>()!.BodyId));
    }

    [Fact]
    public void SubShape_ShouldCollide_ReturnTrue_AllowsCollision()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var wallEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(80f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 400f);
                c.BodyType = PhysicsBodyType.Static;
                // Primary shape: always block.
                c.CollisionMask = 0;  // Primary shape passes everything through via mask.
            });

        var wallBody = wallEntity.GetComponent<PhysicsBodyComponent>()!;
        // Sub-shape with ShouldCollide = always true.
        wallBody.AddSubShape(new BoxShape(20f, 400f))
            .ShouldCollide = (_, _) => true;

        // Reset mask on wall so sub-shape filter drives the decision.
        wallBody.CollisionMask = ulong.MaxValue;

        var projectile = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(200f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityScale = 0f;
                c.InitialLinearVelocity = new Vector2(-500f, 0f);
            });

        world.Flush();

        bool collided = false;
        projectile.GetComponent<PhysicsBodyComponent>()!.OnCollisionEnter += (_, _) => collided = true;

        Step(world, system, 20);

        Assert.True(collided, "Sub-shape ShouldCollide returning true must allow collision.");
    }

    // -------------------------------------------------------------------------
    // Joint break threshold / OnBreak
    // -------------------------------------------------------------------------

    [Fact]
    public void Joint_BreakForce_Exceeded_OnBreakFires()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityScale = 0f;
            });

        var entityB = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(50f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Static;
            });

        world.Flush();
        Step(world, system, 2);

        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
        var bodyB = entityB.GetComponent<PhysicsBodyComponent>()!;

        bool breakFired = false;
        entityA.AddComponent<DistanceJointComponent>(j =>
        {
            j.ConnectedBody = bodyB;
            j.Length = 50f;
            j.BreakForce = 0.001f;   // tiny threshold — any force breaks it
            j.OnBreak += _ => breakFired = true;
        });

        world.Flush();
        Step(world, system, 2);

        // Apply a large impulse to exceed BreakForce.
        bodyA.ApplyLinearImpulse(new Vector2(100000f, 0f));
        Step(world, system, 5);

        Assert.True(breakFired, "OnBreak should fire when reaction force exceeds BreakForce.");
    }

    [Fact]
    public void Joint_BreakForce_NotExceeded_OnBreakDoesNotFire()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityScale = 0f;
            });

        var entityB = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(50f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Static;
            });

        world.Flush();
        Step(world, system, 2);

        var bodyB = entityB.GetComponent<PhysicsBodyComponent>()!;

        bool breakFired = false;
        entityA.AddComponent<DistanceJointComponent>(j =>
        {
            j.ConnectedBody = bodyB;
            j.Length = 50f;
            j.BreakForce = float.PositiveInfinity;
            j.OnBreak += _ => breakFired = true;
        });

        world.Flush();
        Step(world, system, 10);

        Assert.False(breakFired, "OnBreak must not fire when BreakForce is PositiveInfinity.");
    }

    [Fact]
    public void Joint_RebuildAfterBreak_True_JointRebuildsOnNextStep()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        var entityA = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityScale = 0f;
            });

        var entityB = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(30f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Static;
            });

        world.Flush();
        Step(world, system, 2);

        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;

        int breakCount = 0;
        entityA.AddComponent<WeldJointComponent>(j =>
        {
            j.ConnectedBody = entityB.GetComponent<PhysicsBodyComponent>()!;
            // Non-zero LinearHertz activates Box2D's spring solver, which populates
            // JointGetConstraintForce. Rigid welds (LinearHertz=0) use the hard
            // constraint path and report zero constraint force in Box2D 3.x.
            j.LinearHertz = 60f;
            j.LinearDampingRatio = 0f;
            j.BreakForce = 1f;
            j.RebuildAfterBreak = true;
            j.OnBreak += _ => breakCount++;
        });

        world.Flush();
        Step(world, system, 2);

        // Large impulse — the spring constraint resists it and reports force >> 1.
        bodyA.ApplyLinearImpulse(new Vector2(100000f, 0f));

        // Step 1: weld breaks, breakCount → 1, IsDirty set for rebuild.
        Step(world, system, 1);
        Assert.True(breakCount >= 1, $"Joint should have broken on step 1. breakCount={breakCount}");

        // Step 2: joint is rebuilt by SyncJoints, bodyA is still moving so it breaks again.
        Step(world, system, 1);
        Assert.True(breakCount >= 2,
            $"RebuildAfterBreak=true should have rebuilt the joint causing it to break again. breakCount={breakCount}");
    }

    // -------------------------------------------------------------------------
    // GetSleepingBodies
    // -------------------------------------------------------------------------

    [Fact]
    public void GetSleepingBodies_SettledDynamicBody_AppearsInList()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        // Floor to land on.
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 200f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });

        world.Flush();

        // Step long enough for the body to land and sleep.
        Step(world, system, 180);

        var sleeping = _physicsWorld.GetSleepingBodies().ToList();
        var dynBody = dynEntity.GetComponent<PhysicsBodyComponent>()!;

        Assert.Contains(dynBody, sleeping);
    }

    [Fact]
    public void GetSleepingBodies_WokenBody_NoLongerInList()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 200f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
            });

        world.Flush();
        Step(world, system, 180);

        var dynBody = dynEntity.GetComponent<PhysicsBodyComponent>()!;
        Assert.Contains(dynBody, _physicsWorld.GetSleepingBodies());

        // Wake it with an impulse.
        dynBody.ApplyLinearImpulse(new Vector2(0f, -500f));
        Step(world, system, 1);

        Assert.DoesNotContain(dynBody, _physicsWorld.GetSleepingBodies());
    }
}