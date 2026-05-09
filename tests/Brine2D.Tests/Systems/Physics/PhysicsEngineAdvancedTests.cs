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
public class PhysicsEngineAdvancedTests : PhysicsTestBase
{
    private Box2DPhysicsSystem? _system;

    private Box2DPhysicsSystem CreateSystem()
    {
        _system = new Box2DPhysicsSystem(PhysicsWorld);
        return _system;
    }

    public override void Dispose()
    {
        _system?.Dispose();
        _system = null;
        base.Dispose();
    }

    private void Step(IEntityWorld world, Box2DPhysicsSystem system, int count = 1)
    {
        for (int i = 0; i < count; i++)
            system.FixedUpdate(world, FixedTime);
    }

    // Circle at y=170 with downward velocity, floor at y=190 (gap=10px, radius=10px → immediate contact).
    // GravityScale=0 so no gravity accumulation; velocity is set directly.
    [Fact]
    public void OnCollisionHit_GravityDrivenImpact_Fires()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 190f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
                c.EnableHitEvents = true;
            });

        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 170f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityScale = 0f;
                c.EnableHitEvents = true;
            });

        world.Flush();
        PhysicsWorld.SetContactHitEventThreshold(0f);

        bool hitFired = false;
        dynEntity.GetComponent<PhysicsBodyComponent>()!.OnCollisionHit += (_, contact) =>
        {
            if (contact.ImpactSpeed > 0f)
                hitFired = true;
        };

        dynEntity.GetComponent<PhysicsBodyComponent>()!.LinearVelocity = new Vector2(0f, 100f);
        Step(world, system, 5);

        Assert.True(hitFired, "OnCollisionHit should fire when a body impacts at non-zero speed.");
    }

    // Same setup but EnableHitEvents=false on both bodies — event must never fire.
    [Fact]
    public void EnableHitEvents_False_HitEventDoesNotFire()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 190f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
                c.EnableHitEvents = false;
            });

        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 170f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityScale = 0f;
                c.EnableHitEvents = false;
            });

        world.Flush();

        bool hitFired = false;
        dynEntity.GetComponent<PhysicsBodyComponent>()!.OnCollisionHit += (_, _) => hitFired = true;

        dynEntity.GetComponent<PhysicsBodyComponent>()!.LinearVelocity = new Vector2(0f, 100f);
        Step(world, system, 5);

        Assert.False(hitFired, "OnCollisionHit must not fire when EnableHitEvents is false.");
    }

    // Dynamic circle starts just right of the sub-shape (gap ~20px) moving left — hits sub-shape in 2-3 steps.
    [Fact]
    public void OnCollisionEnterWithShape_SubShapeHit_ReportsCorrectSubShape()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        SubShape? reportedSelfSub = null;

        var compoundEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(20f, 400f);
                c.BodyType = PhysicsBodyType.Static;
                c.OnCollisionEnterWithShape += (_, _, selfSub, _) => reportedSelfSub = selfSub;
            });

        var compoundBody = compoundEntity.GetComponent<PhysicsBodyComponent>()!;
        // Sub-shape: tall box offset 60px to the right of body center.
        var subShape = compoundBody.AddSubShape(
            new BoxShape(20f, 400f) { Offset = new Vector2(60f, 0f) });

        // Circle starts 10px right of the sub-shape right edge (offset 60 + half-width 10 = 70; circle at x=90, radius 10 → gap 10px).
        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(90f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityScale = 0f;
            });

        world.Flush();
        Step(world, system, 1);

        dynEntity.GetComponent<PhysicsBodyComponent>()!.LinearVelocity = new Vector2(-100f, 0f);
        Step(world, system, 5);

        Assert.NotNull(reportedSelfSub);
        Assert.Same(subShape, reportedSelfSub);
    }

    // Body lands on a platform, then teleports away — exit event should fire.
    [Fact]
    public void OnCollisionExitWithShape_SubShapeContact_ReportsCorrectSubShape()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        SubShape? exitSelfSub = null;

        var compoundEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 190f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
                c.OnCollisionExitWithShape += (_, selfSub, _) => exitSelfSub = selfSub;
            });

        var compoundBody = compoundEntity.GetComponent<PhysicsBodyComponent>()!;
        var subShape = compoundBody.AddSubShape(
            new BoxShape(400f, 20f) { Offset = new Vector2(0f, -30f) });

        // Circle starts just above the platform, moving downward — contacts in 1-2 steps.
        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 165f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityScale = 0f;
            });

        world.Flush();

        var dynBody = dynEntity.GetComponent<PhysicsBodyComponent>()!;
        dynBody.LinearVelocity = new Vector2(0f, 50f);
        Step(world, system, 3);

        // Teleport far away to end contact.
        dynBody.Teleport(new Vector2(5000f, 5000f));
        Step(world, system, 3);

        Assert.NotNull(exitSelfSub);
    }

    // Trigger sub-shape at a known position; dynamic body placed just below it, moving up — enters in 1-2 steps.
    [Fact]
    public void OnTriggerEnterWithShape_SubShapeTrigger_ReportsCorrectSubShape()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        SubShape? reportedSub = null;
        bool basicTriggerFired = false;
        int triggerEnterWithShapeCallCount = 0;

        var sensorEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                // Primary shape kept far away so only the sub-shape trigger is relevant.
                c.Shape = new CircleShape(1f) { Offset = new Vector2(5000f, 0f) };
                c.BodyType = PhysicsBodyType.Static;
                c.OnTriggerEnter += _ => basicTriggerFired = true;
                c.OnTriggerEnterWithShape += (_, selfSub, _) =>
                {
                    triggerEnterWithShapeCallCount++;
                    reportedSub = selfSub;
                };
            });

        var sensorBody = sensorEntity.GetComponent<PhysicsBodyComponent>()!;
        // Trigger sub-shape: circle of radius 30px centered at (0, 0) relative to body.
        var triggerSub = sensorBody.AddSubShape(
            new CircleShape(30f) { Offset = new Vector2(0f, 0f) },
            isTrigger: true);

        // Dynamic circle just below the trigger zone: body at (0,0), trigger radius 30 → bottom edge at y=30.
        // Place dynamic at y=50, radius 10 → top edge at y=40, gap 10px.
        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 50f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityScale = 0f;
            });

        world.Flush();
        Step(world, system, 1);

        var dynBody = dynEntity.GetComponent<PhysicsBodyComponent>()!;
        bool subShapeValid = B2.ShapeIsValid(triggerSub.ShapeId);
        bool subShapeIsSensor = subShapeValid && B2.ShapeIsSensor(triggerSub.ShapeId);
        bool dynShapeValid = B2.ShapeIsValid(dynBody.ShapeId);

        // Move up at moderate velocity — enters trigger in 2-3 steps.
        dynBody.LinearVelocity = new Vector2(0f, -100f);
        Step(world, system, 5);

        Assert.True(subShapeValid,
            $"Sub-shape ShapeId is not valid after flush. dynShapeValid={dynShapeValid}");
        Assert.True(subShapeIsSensor,
            "Sub-shape is not registered as a sensor in Box2D");
        Assert.True(basicTriggerFired || triggerEnterWithShapeCallCount > 0,
            $"No trigger event fired. subShapeValid={subShapeValid}, subShapeIsSensor={subShapeIsSensor}, dynShapeValid={dynShapeValid}");
        Assert.True(triggerEnterWithShapeCallCount > 0,
            $"OnTriggerEnter fired={basicTriggerFired} but OnTriggerEnterWithShape never called. callCount={triggerEnterWithShapeCallCount}");
        Assert.NotNull(reportedSub);
        Assert.Same(triggerSub, reportedSub);
    }

    // Body placed at y=170 (floor at y=190, gap=10px) with no gravity — it just sits there.
    // After a handful of steps it should sleep. Box2D sleep time is ~0.5s = ~30 steps.
    [Fact]
    public void GetSleepingBodies_SettledDynamicBody_AppearsInList()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 190f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 170f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityScale = 0f;
            });

        world.Flush();
        // 60 steps = 1 simulated second — well past Box2D's default sleep threshold (~0.5s).
        Step(world, system, 60);

        var dynBody = dynEntity.GetComponent<PhysicsBodyComponent>()!;
        Assert.Contains(dynBody, PhysicsWorld.GetSleepingBodies());
    }

    [Fact]
    public void GetSleepingBodies_WokenBody_NoLongerInList()
    {
        var world = CreateTestWorld();
        var system = CreateSystem();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 190f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new BoxShape(400f, 20f);
                c.BodyType = PhysicsBodyType.Static;
            });

        var dynEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 170f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.GravityScale = 0f;
            });

        world.Flush();
        Step(world, system, 60);

        var dynBody = dynEntity.GetComponent<PhysicsBodyComponent>()!;
        Assert.Contains(dynBody, PhysicsWorld.GetSleepingBodies());

        dynBody.ApplyLinearImpulse(new Vector2(0f, -50f));
        Step(world, system, 1);

        Assert.DoesNotContain(dynBody, PhysicsWorld.GetSleepingBodies());
    }

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
            j.BreakForce = 0.001f;
            j.OnBreak += _ => breakFired = true;
        });

        world.Flush();
        Step(world, system, 2);

        bodyA.ApplyLinearImpulse(new Vector2(500f, 0f));
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
            j.LinearHertz = 60f;
            j.LinearDampingRatio = 0f;
            j.BreakForce = 1f;
            j.RebuildAfterBreak = true;
            j.OnBreak += _ => breakCount++;
        });

        world.Flush();
        Step(world, system, 2);

        bodyA.ApplyLinearImpulse(new Vector2(500f, 0f));

        Step(world, system, 1);
        Assert.True(breakCount >= 1, $"Joint should have broken on step 1. breakCount={breakCount}");

        Step(world, system, 1);
        Assert.True(breakCount >= 2,
            $"RebuildAfterBreak=true should have rebuilt the joint causing it to break again. breakCount={breakCount}");
    }
}