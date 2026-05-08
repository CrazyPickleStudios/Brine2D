//using System.Numerics;
//using Brine2D.Collision;
//using Brine2D.Core;
//using Brine2D.ECS;
//using Brine2D.ECS.Components;
//using Brine2D.Physics;
//using Brine2D.Systems.Physics;

//namespace Brine2D.Tests.Systems.Physics;

//[Collection("Physics")]
//public class PhysicsBodyComponentBehaviorTests : PhysicsTestBase
//{
//    // ── Collision events ──────────────────────────────────────────────────────

//    [Fact]
//    public void OnCollisionEnter_TwoBodiesOverlapping_Fires()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);
//        bool fired = false;

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(40f);
//                c.BodyType = PhysicsBodyType.Static;
//                c.OnCollisionEnter += (_, _) => fired = true;
//            });

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 10f))
//            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(40f));

//        world.Flush();
//        for (int i = 0; i < 5; i++)
//            system.FixedUpdate(world, FixedTime);

//        Assert.True(fired);
//    }

//    [Fact]
//    public void OnCollisionExit_AfterBodyRemoved_Fires()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);
//        bool exitFired = false;

//        var entityA = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(40f);
//                c.BodyType = PhysicsBodyType.Static;
//                c.OnCollisionExit += _ => exitFired = true;
//            });

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 10f))
//            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(40f));

//        world.Flush();
//        for (int i = 0; i < 3; i++)
//            system.FixedUpdate(world, FixedTime);

//        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
//        Assert.NotEmpty(bodyA.ActiveContactPairs);

//        entityA.RemoveComponent<PhysicsBodyComponent>();
//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        Assert.True(exitFired);
//    }

//    [Fact]
//    public void OnTriggerEnter_TriggerAndSolidOverlapping_Fires()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);
//        bool fired = false;

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(40f);
//                c.IsTrigger = true;
//                c.BodyType = PhysicsBodyType.Static;
//                c.OnTriggerEnter += _ => fired = true;
//            });

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 5f))
//            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));

//        world.Flush();
//        for (int i = 0; i < 5; i++)
//            system.FixedUpdate(world, FixedTime);

//        Assert.True(fired);
//    }

//    [Fact]
//    public void OnTriggerExit_AfterBodyRemoved_Fires()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);
//        bool exitFired = false;

//        var trigger = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(40f);
//                c.IsTrigger = true;
//                c.BodyType = PhysicsBodyType.Static;
//                c.OnTriggerExit += _ => exitFired = true;
//            });

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 5f))
//            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));

//        world.Flush();
//        for (int i = 0; i < 3; i++)
//            system.FixedUpdate(world, FixedTime);

//        var triggerBody = trigger.GetComponent<PhysicsBodyComponent>()!;
//        Assert.NotEmpty(triggerBody.ActiveSensorPairs);

//        trigger.RemoveComponent<PhysicsBodyComponent>();
//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        Assert.True(exitFired);
//    }

//    [Fact]
//    public void CollidingEntities_AfterBothExitEvents_IsEmpty()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        var entityA = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(40f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        var moverEntity = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 5f))
//            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(40f));

//        world.Flush();
//        for (int i = 0; i < 3; i++)
//            system.FixedUpdate(world, FixedTime);

//        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
//        Assert.NotEmpty(bodyA.CollidingEntities);

//        moverEntity.Destroy();
//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        Assert.Empty(bodyA.CollidingEntities);
//    }

//    // ── IgnoreCollision recycled index ────────────────────────────────────────

//    [Fact]
//    public void IgnoreCollision_AfterBodyDestroyed_PairPurged()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        var entityA = world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));
//        var entityB = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(500f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));

//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
//        var bodyB = entityB.GetComponent<PhysicsBodyComponent>()!;
//        PhysicsWorld.IgnoreCollision(bodyA, bodyB);
//        Assert.True(PhysicsWorld.IsCollisionIgnored(bodyA.BodyId.index1, bodyB.BodyId.index1));

//        entityA.Destroy();
//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        Assert.False(PhysicsWorld.IsCollisionIgnored(bodyA.BodyId.index1, bodyB.BodyId.index1));
//    }

//    // ── Live body-type switch ─────────────────────────────────────────────────

//    [Fact]
//    public void BodyType_LiveSwitch_StaticToDynamic_BodyBecomesAffectedByGravity()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        var entity = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var body = entity.GetComponent<PhysicsBodyComponent>()!;
//        var transform = entity.GetComponent<TransformComponent>()!;
//        float yBefore = transform.Position.Y;

//        body.BodyType = PhysicsBodyType.Dynamic;
//        for (int i = 0; i < 5; i++)
//            system.FixedUpdate(world, FixedTime);

//        Assert.True(transform.Position.Y > yBefore);
//    }

//    [Fact]
//    public void BodyType_LiveSwitch_FlushesContactPairs()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);
//        int exitCount = 0;

//        var entityA = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(40f);
//                c.BodyType = PhysicsBodyType.Static;
//                c.OnCollisionExit += _ => exitCount++;
//            });

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 10f))
//            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(40f));

//        world.Flush();
//        for (int i = 0; i < 3; i++)
//            system.FixedUpdate(world, FixedTime);

//        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
//        Assert.NotEmpty(bodyA.ActiveContactPairs);

//        bodyA.BodyType = PhysicsBodyType.Kinematic;
//        system.FixedUpdate(world, FixedTime);

//        Assert.True(exitCount > 0);
//    }

//    // ── IsBullet live toggle ──────────────────────────────────────────────────

//    [Fact]
//    public void IsBullet_LiveToggle_RebuildPreservesVelocity()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        var entity = world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));

//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var body = entity.GetComponent<PhysicsBodyComponent>()!;
//        var velocityBefore = body.LinearVelocity;

//        body.IsBullet = true;
//        system.FixedUpdate(world, FixedTime);

//        Assert.True(Box2D.NET.Bindings.B2.BodyIsBullet(body.BodyId));
//        Assert.Equal(velocityBefore.Y, body.LinearVelocity.Y, precision: 0);
//    }

//    // ── GravityOverride ───────────────────────────────────────────────────────

//    [Fact]
//    public void GravityOverride_OppositeToWorldGravity_BodyMovesUp()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        var entity = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 500f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.GravityOverride = new Vector2(0f, -980f);
//            });

//        world.Flush();
//        for (int i = 0; i < 5; i++)
//            system.FixedUpdate(world, FixedTime);

//        var transform = entity.GetComponent<TransformComponent>()!;
//        Assert.True(transform.Position.Y < 500f);
//    }

//    [Fact]
//    public void GravityOverride_SetToNull_RestoresWorldGravity()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        var entity = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.GravityOverride = new Vector2(0f, -980f);
//            });

//        world.Flush();
//        for (int i = 0; i < 3; i++)
//            system.FixedUpdate(world, FixedTime);

//        var body = entity.GetComponent<PhysicsBodyComponent>()!;
//        var transform = entity.GetComponent<TransformComponent>()!;
//        body.GravityOverride = null;
//        float yAfterReset = transform.Position.Y;

//        for (int i = 0; i < 5; i++)
//            system.FixedUpdate(world, FixedTime);

//        Assert.True(transform.Position.Y > yAfterReset);
//    }

//    // ── FreezePosition ────────────────────────────────────────────────────────

//    [Fact]
//    public void FreezePositionY_Dynamic_YAxisStaysFixed()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        var entity = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 200f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.FreezePositionY = true;
//            });

//        world.Flush();
//        float yBefore = entity.GetComponent<TransformComponent>()!.Position.Y;

//        for (int i = 0; i < 10; i++)
//            system.FixedUpdate(world, FixedTime);

//        Assert.Equal(yBefore, entity.GetComponent<TransformComponent>()!.Position.Y, precision: 1);
//    }

//    [Fact]
//    public void FreezePositionX_Dynamic_XAxisStaysFixed()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        var entity = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.FreezePositionX = true;
//            });

//        world.Flush();
//        float xBefore = entity.GetComponent<TransformComponent>()!.Position.X;

//        for (int i = 0; i < 10; i++)
//            system.FixedUpdate(world, FixedTime);

//        Assert.Equal(xBefore, entity.GetComponent<TransformComponent>()!.Position.X, precision: 1);
//    }

//    // ── One-way platform ──────────────────────────────────────────────────────

//    [Fact]
//    public void IsOneWayPlatform_BodyFallingFromAbove_Collides()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        // Platform at y=200, solid from above (normal (0,-1) in Y-down space).
//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 200f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(200f, 10f);
//                c.BodyType = PhysicsBodyType.Static;
//                c.IsOneWayPlatform = true;
//                c.PlatformNormalDirection = new Vector2(0f, -1f);
//            });

//        var fallerEntity = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
//            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new BoxShape(20f, 20f));

//        world.Flush();
//        for (int i = 0; i < 30; i++)
//            system.FixedUpdate(world, FixedTime);

//        var fallerTransform = fallerEntity.GetComponent<TransformComponent>()!;
//        Assert.True(fallerTransform.Position.Y < 220f, $"Expected body to land on platform, Y was {fallerTransform.Position.Y}");
//    }

//    // ── ShouldCollide filter ──────────────────────────────────────────────────

//    [Fact(Skip = "ShouldCollide uses [UnmanagedCallersOnly] with non-blittable bool return - crashes JIT in CI")]
//    public void ShouldCollide_ReturningFalse_VetoesContact()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);
//        bool enterFired = false;

//        var entityA = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(40f);
//                c.BodyType = PhysicsBodyType.Static;
//                c.OnCollisionEnter += (_, _) => enterFired = true;
//            });

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 10f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(40f);
//                c.ShouldCollide = _ => false;
//            });

//        world.Flush();
//        for (int i = 0; i < 5; i++)
//            system.FixedUpdate(world, FixedTime);

//        Assert.False(enterFired);
//        _ = entityA;
//    }

//    // ── ChainShape ────────────────────────────────────────────────────────────

//    [Fact]
//    public void ChainShape_StaticBody_CreatesValidBody()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.BodyType = PhysicsBodyType.Static;
//                c.Shape = new ChainShape(
//                [
//                    new Vector2(-200f, 0f),
//                    new Vector2(-100f, 50f),
//                    new Vector2(0f, 0f),
//                    new Vector2(100f, 50f),
//                    new Vector2(200f, 0f)
//                ]);
//            });

//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var body = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
//        Assert.True(Box2D.NET.Bindings.B2.BodyIsValid(body.BodyId));
//        Assert.True(Box2D.NET.Bindings.B2.ChainIsValid(body.ChainId));
//    }

//    [Fact]
//    public void ChainShape_DynamicBodyType_ThrowsOnAssign()
//    {
//        var body = new PhysicsBodyComponent();

//        Assert.Throws<InvalidOperationException>(() => body.Shape = new ChainShape(
//        [
//            new Vector2(0f, 0f),
//            new Vector2(100f, 0f),
//            new Vector2(200f, 0f)
//        ]));
//    }

//    // ── Raycasting ────────────────────────────────────────────────────────────

//    [Fact]
//    public void RaycastClosest_AgainstStaticBox_ReturnsHit()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 200f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(100f, 20f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var hit = PhysicsWorld.RaycastClosest(new Vector2(0f, 0f), new Vector2(0f, 1f), 500f);

//        Assert.NotNull(hit);
//        Assert.True(hit.Value.Distance > 0f);
//        Assert.NotNull(hit.Value.Component);
//    }

//    [Fact]
//    public void RaycastClosest_NoBodyInPath_ReturnsNull()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(1000f, 1000f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(10f, 10f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var hit = PhysicsWorld.RaycastClosest(new Vector2(0f, 0f), new Vector2(0f, 1f), 100f);

//        Assert.Null(hit);
//    }

//    [Fact]
//    public void RaycastAll_MultipleTargets_ReturnsAllHits()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        for (int i = 1; i <= 3; i++)
//        {
//            world.CreateEntity()
//                .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, i * 100f))
//                .AddComponent<PhysicsBodyComponent>(c =>
//                {
//                    c.Shape = new BoxShape(20f, 10f);
//                    c.BodyType = PhysicsBodyType.Static;
//                });
//        }

//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var results = new RaycastHit[10];
//        int count = PhysicsWorld.RaycastAll(new Vector2(0f, 0f), new Vector2(0f, 1f), 500f, results);

//        Assert.Equal(3, count);
//    }

//    // ── MultiWorld PPM guard ──────────────────────────────────────────────────

//    [Fact]
//    public void Constructor_SecondWorldDifferentPPM_Throws()
//    {
//        using var first = new PhysicsWorld(Vector2.Zero, 100f);

//        Assert.Throws<InvalidOperationException>(() =>
//        {
//            using var second = new PhysicsWorld(Vector2.Zero, 200f);
//        });
//    }
//}