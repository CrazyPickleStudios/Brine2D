//using Box2D.NET.Bindings;
//using Brine2D.Collision;
//using Brine2D.Core;
//using Brine2D.ECS;
//using Brine2D.ECS.Components;
//using Brine2D.ECS.Components.Joints;
//using Brine2D.Physics;
//using Brine2D.Systems.Physics;
//using Microsoft.Extensions.Logging.Abstractions;
//using System.Numerics;

//namespace Brine2D.Tests.Systems.Physics;

//[Collection("Physics")]
//public class Box2DPhysicsSystemExtendedTests : PhysicsTestBase
//{
//    private Box2DPhysicsSystem CreateSystem() => new(PhysicsWorld);

//    private void Step(IEntityWorld world, Box2DPhysicsSystem system, int count = 1)
//    {
//        for (int i = 0; i < count; i++)
//            system.FixedUpdate(world, FixedTime);
//    }

//    // -------------------------------------------------------------------------
//    // IgnoreCollision / RestoreCollision
//    // -------------------------------------------------------------------------

//    [Fact]
//    public void IgnoreCollision_TwoDynamicBodies_DoNotCollide()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        // Two circles overlapping on creation — they would normally collide.
//        var entityA = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(20f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        var entityB = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 5f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(20f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//                c.GravityScale = 0f;
//            });

//        world.Flush();
//        Step(world, system);

//        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
//        var bodyB = entityB.GetComponent<PhysicsBodyComponent>()!;

//        bool collisionFired = false;
//        bodyB.OnCollisionEnter += (_, _) => collisionFired = true;

//        PhysicsWorld.IgnoreCollision(bodyA, bodyB);

//        Step(world, system, 5);

//        Assert.False(collisionFired, "IgnoreCollision should suppress contact events.");
//    }

//    [Fact]
//    public void RestoreCollision_AfterIgnore_CollisionResumes()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        var entityA = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(400f, 20f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        var entityB = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, -50f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//            });

//        world.Flush();
//        Step(world, system);

//        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
//        var bodyB = entityB.GetComponent<PhysicsBodyComponent>()!;

//        PhysicsWorld.IgnoreCollision(bodyA, bodyB);
//        Step(world, system, 5);

//        bool collisionFired = false;
//        bodyB.OnCollisionEnter += (_, _) => collisionFired = true;

//        PhysicsWorld.RestoreCollision(bodyA, bodyB);
//        Step(world, system, 30);

//        Assert.True(collisionFired, "After RestoreCollision the bodies should collide again.");
//    }

//    [Fact]
//    public void IgnoreCollision_DestroyBody_PurgesIgnoredPairSoRecycledSlotIsClean()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        var entityA = world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        var entityB = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        world.Flush();
//        Step(world, system);

//        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
//        var bodyB = entityB.GetComponent<PhysicsBodyComponent>()!;

//        PhysicsWorld.IgnoreCollision(bodyA, bodyB);
//        Assert.True(PhysicsWorld.HasIgnoredPairs);

//        entityB.Destroy();
//        world.Flush();
//        Step(world, system);

//        Assert.False(PhysicsWorld.HasIgnoredPairs,
//            "Destroying a body must purge its ignored pairs so the recycled slot starts clean.");
//    }

//    // -------------------------------------------------------------------------
//    // IsOneWayPlatform — install / uninstall filter and basic pass-through
//    // -------------------------------------------------------------------------

//    [Fact]
//    public void IsOneWayPlatform_BodyApproachingFromSolidSide_Collides()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        // Platform at Y=100, normal pointing up (0,-1) in Y-down space.
//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(400f, 20f);
//                c.BodyType = PhysicsBodyType.Static;
//                c.IsOneWayPlatform = true;
//                c.PlatformNormalDirection = new Vector2(0f, -1f);
//            });

//        var dynEntity = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 50f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//            });

//        world.Flush();

//        bool landed = false;
//        dynEntity.GetComponent<PhysicsBodyComponent>()!.OnCollisionEnter += (_, _) => landed = true;

//        Step(world, system, 30);

//        Assert.True(landed, "Body falling onto the solid side should collide with the one-way platform.");
//    }

//    [Fact]
//    public void IsOneWayPlatform_BodyApproachingFromPassThroughSide_PassesThrough()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        var platformEntity = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(400f, 20f);
//                c.BodyType = PhysicsBodyType.Static;
//                c.IsOneWayPlatform = true;
//                c.PlatformNormalDirection = new Vector2(0f, -1f);
//            });

//        var dynEntity = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 50f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//                c.GravityScale = 0f;
//                c.InitialLinearVelocity = new Vector2(0f, -300f);
//            });

//        world.Flush();

//        var preSolveCalls = new System.Collections.Generic.List<(bool aIsOWP, bool bIsOWP, float normalX, float normalY)>();
//        PhysicsWorld.SetPreSolveFilter(c =>
//        {
//            preSolveCalls.Add((c.BodyA.IsOneWayPlatform, c.BodyB.IsOneWayPlatform, c.Normal.X, c.Normal.Y));
//            return true;
//        });

//        Step(world, system, 15);

//        PhysicsWorld.SetPreSolveFilter(null);

//        var platformBody = platformEntity.GetComponent<PhysicsBodyComponent>()!;
//        var finalY = dynEntity.GetComponent<TransformComponent>()!.Position.Y;

//        var report = preSolveCalls.Count == 0
//            ? "Pre-solve NEVER fired (filter not installed or OWP count=0)"
//            : string.Join("; ", preSolveCalls.Select(p =>
//                $"aOWP={p.aIsOWP} bOWP={p.bIsOWP} n=({p.normalX:F2},{p.normalY:F2})"));

//        Assert.True(finalY < -10f,
//            $"Body should have passed through but was blocked. FinalY={finalY:F1}. " +
//            $"platformIsOWP={platformBody.IsOneWayPlatform} preSolve: {report}");
//    }

//    [Fact]
//    public void IsOneWayPlatform_ToggleOffThenOn_CountNeverGoesNegative()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        var entity = world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(100f, 20f);
//                c.BodyType = PhysicsBodyType.Static;
//                c.IsOneWayPlatform = true;
//            });

//        world.Flush();
//        Step(world, system);

//        var body = entity.GetComponent<PhysicsBodyComponent>()!;

//        // Rapid toggle in same tick before SyncToBox2D processes it.
//        body.IsOneWayPlatform = false;
//        body.IsOneWayPlatform = true;

//        // Should not throw and should not leave filter in broken state.
//        Step(world, system, 2);

//        // A second body with OWP should still work — filter count must be >= 1.
//        var entity2 = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(500f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(100f, 20f);
//                c.BodyType = PhysicsBodyType.Static;
//                c.IsOneWayPlatform = true;
//            });

//        world.Flush();
//        Step(world, system);

//        // Both entities have IsOneWayPlatform=true — disabling one must leave filter active.
//        body.IsOneWayPlatform = false;
//        Step(world, system);

//        // The second body still has OWP; a body approaching from solid side should still land.
//        var dynEntity = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(500f, -50f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//            });
//        world.Flush();

//        bool landed = false;
//        dynEntity.GetComponent<PhysicsBodyComponent>()!.OnCollisionEnter += (_, _) => landed = true;

//        Step(world, system, 30);

//        Assert.True(landed, "OWP filter must remain installed while at least one body has IsOneWayPlatform=true.");
//    }

//    // -------------------------------------------------------------------------
//    // FreezePositionX / FreezePositionY
//    // -------------------------------------------------------------------------

//    [Fact]
//    public void FreezePositionX_DynamicBodyReceivesHorizontalImpulse_XVelocityRemainsZero()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        var entity = world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//                c.GravityScale = 0f;
//                c.FreezePositionX = true;
//            });

//        world.Flush();
//        Step(world, system);

//        var body = entity.GetComponent<PhysicsBodyComponent>()!;
//        body.ApplyLinearImpulse(new Vector2(1000f, 0f));

//        Step(world, system, 5);

//        var vel = B2.BodyGetLinearVelocity(body.BodyId);
//        Assert.Equal(0f, vel.x, 0.01f);
//    }

//    [Fact]
//    public void FreezePositionY_DynamicBodyUnderGravity_YVelocityRemainsZero()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        var entity = world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//                c.FreezePositionY = true;
//            });

//        world.Flush();
//        Step(world, system, 10);

//        var body = entity.GetComponent<PhysicsBodyComponent>()!;
//        var vel = B2.BodyGetLinearVelocity(body.BodyId);
//        Assert.Equal(0f, vel.y, 0.01f);
//    }

//    // -------------------------------------------------------------------------
//    // GravityOverride
//    // -------------------------------------------------------------------------

//    [Fact]
//    public void GravityOverride_BodyMovesInOverrideDirection_NotWorldGravity()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        // World gravity is (0, 980) — downward.
//        // Override to (0, -980) — upward.
//        var overrideEntity = world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//                c.GravityOverride = new Vector2(0f, -980f);
//            });

//        var normalEntity = world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//            });

//        world.Flush();
//        Step(world, system, 10);

//        var overrideTransform = overrideEntity.GetComponent<TransformComponent>()!;
//        var normalTransform = normalEntity.GetComponent<TransformComponent>()!;

//        Assert.True(overrideTransform.Position.Y < 0f,
//            "Body with upward GravityOverride should move up.");
//        Assert.True(normalTransform.Position.Y > 0f,
//            "Body with normal gravity should move down.");
//    }

//    [Fact]
//    public void GravityOverride_SetToNull_RestoresWorldGravity()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        var entity = world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//                c.GravityOverride = new Vector2(0f, -980f);
//            });

//        world.Flush();
//        Step(world, system, 5);

//        var body = entity.GetComponent<PhysicsBodyComponent>()!;
//        var transform = entity.GetComponent<TransformComponent>()!;
//        float yAfterOverride = transform.Position.Y;

//        body.GravityOverride = null;
//        Step(world, system, 10);

//        Assert.True(transform.Position.Y > yAfterOverride,
//            "After clearing GravityOverride the body should fall downward under world gravity.");
//    }

//    [Fact]
//    public void GravityScale_Zero_BodyDoesNotFall()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        var entity = world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//                c.GravityScale = 0f;
//            });

//        world.Flush();
//        Step(world, system, 10);

//        var transform = entity.GetComponent<TransformComponent>()!;
//        Assert.Equal(0f, transform.Position.Y, 0.01f);
//    }

//    // -------------------------------------------------------------------------
//    // IsSimulationEnabled toggle
//    // -------------------------------------------------------------------------

//    [Fact]
//    public void IsSimulationEnabled_False_BodyStopsMoving()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        var entity = world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//            });

//        world.Flush();
//        Step(world, system, 3);

//        var transform = entity.GetComponent<TransformComponent>()!;
//        var body = entity.GetComponent<PhysicsBodyComponent>()!;

//        body.IsSimulationEnabled = false;
//        Step(world, system);

//        float yWhenDisabled = transform.Position.Y;
//        Step(world, system, 5);

//        Assert.Equal(yWhenDisabled, transform.Position.Y, 0.01f);
//    }

//    [Fact]
//    public void IsSimulationEnabled_ReEnable_ContactEnterFiresAgain()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        // Static floor.
//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(400f, 20f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        var dynEntity = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 50f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//            });

//        world.Flush();

//        int enterCount = 0;
//        dynEntity.GetComponent<PhysicsBodyComponent>()!.OnCollisionEnter += (_, _) => enterCount++;

//        // Land and establish contact.
//        Step(world, system, 20);
//        int countAfterLanding = enterCount;
//        Assert.True(countAfterLanding >= 1, "Should have entered collision at least once.");

//        var body = dynEntity.GetComponent<PhysicsBodyComponent>()!;
//        var transform = dynEntity.GetComponent<TransformComponent>()!;

//        // Disable simulation — contact exit fires, body freezes.
//        body.IsSimulationEnabled = false;
//        Step(world, system, 3);

//        // Reposition above floor and re-enable.
//        transform.Position = new Vector2(0f, 50f);
//        body.IsSimulationEnabled = true;
//        Step(world, system, 20);

//        Assert.True(enterCount > countAfterLanding,
//            "OnCollisionEnter should fire again after re-enabling simulation and landing.");
//    }

//    [Fact]
//    public void IsSimulationEnabled_Disable_FiresCollisionExit()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(400f, 20f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        var dynEntity = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 50f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//            });

//        world.Flush();
//        Step(world, system, 20);

//        bool exitFired = false;
//        dynEntity.GetComponent<PhysicsBodyComponent>()!.OnCollisionExit += _ => exitFired = true;

//        dynEntity.GetComponent<PhysicsBodyComponent>()!.IsSimulationEnabled = false;
//        Step(world, system, 2);

//        Assert.True(exitFired, "Disabling simulation should flush contact pairs and fire OnCollisionExit.");
//    }

//    // -------------------------------------------------------------------------
//    // Body type change on live body
//    // -------------------------------------------------------------------------

//    [Fact]
//    public void BodyType_DynamicToStatic_OnLiveBody_BodyStopsMoving()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        var entity = world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//            });

//        world.Flush();
//        Step(world, system, 5);

//        var body = entity.GetComponent<PhysicsBodyComponent>()!;
//        var transform = entity.GetComponent<TransformComponent>()!;

//        body.BodyType = PhysicsBodyType.Static;
//        Step(world, system);

//        float yFrozen = transform.Position.Y;
//        Step(world, system, 5);

//        Assert.Equal(yFrozen, transform.Position.Y, 0.01f);
//    }

//    [Fact]
//    public void BodyType_StaticToDynamic_OnLiveBody_BodyFallsWithGravity()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        var entity = world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        world.Flush();
//        Step(world, system, 3);

//        var body = entity.GetComponent<PhysicsBodyComponent>()!;
//        var transform = entity.GetComponent<TransformComponent>()!;

//        float yBefore = transform.Position.Y;
//        body.BodyType = PhysicsBodyType.Dynamic;
//        Step(world, system, 10);

//        Assert.True(transform.Position.Y > yBefore,
//            "Body switched to Dynamic should fall under gravity.");
//    }

//    [Fact]
//    public void BodyType_DynamicToKinematic_OnLiveBody_BodyPreservesPosition()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        var entity = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 50f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//                c.GravityScale = 0f;
//            });

//        world.Flush();
//        Step(world, system, 3);

//        var body = entity.GetComponent<PhysicsBodyComponent>()!;
//        var transform = entity.GetComponent<TransformComponent>()!;

//        float yBefore = transform.Position.Y;
//        body.BodyType = PhysicsBodyType.Kinematic;
//        Step(world, system, 3);

//        Assert.Equal(yBefore, transform.Position.Y, 0.5f);
//    }

//    // -------------------------------------------------------------------------
//    // MoveAndCollide — actual wall hit
//    // -------------------------------------------------------------------------

//    [Fact]
//    public void MoveAndCollide_HitsWall_LastHitSetAndRemainderCorrect()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();
//        var pre = new KinematicCharacterSystem(PhysicsWorld, isPostStep: false, NullLogger<KinematicCharacterSystem>.Instance);
//        var post = new KinematicCharacterSystem(PhysicsWorld, isPostStep: true, NullLogger<KinematicCharacterSystem>.Instance);

//        // Wall at X=100, width=20 → left edge X=90.
//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(20f, 400f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        var charEntity = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(20f, 20f);
//                c.BodyType = PhysicsBodyType.Kinematic;
//            })
//            .AddComponent<KinematicCharacterBody>();

//        world.Flush();

//        // Settle body.
//        pre.FixedUpdate(world, FixedTime);
//        system.FixedUpdate(world, FixedTime);
//        post.FixedUpdate(world, FixedTime);

//        var character = charEntity.GetComponent<KinematicCharacterBody>()!;

//        // MoveAndCollide with a vector that would overshoot the wall.
//        character.MoveAndCollide(new Vector2(200f, 0f));

//        pre.FixedUpdate(world, FixedTime);
//        system.FixedUpdate(world, FixedTime);
//        post.FixedUpdate(world, FixedTime);

//        Assert.NotNull(character.LastMoveAndCollideHit);
//        Assert.True(character.MotionRemainder.X > 0f,
//            "There should be leftover motion after hitting the wall.");
//    }

//    // -------------------------------------------------------------------------
//    // GetContacts / GetContactsAll integration
//    // -------------------------------------------------------------------------

//    [Fact]
//    public void GetContacts_TwoBodiesInContact_ReturnsNonZero()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(200f, 20f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        var dynEntity = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 50f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//            });

//        world.Flush();
//        Step(world, system, 25);

//        var body = dynEntity.GetComponent<PhysicsBodyComponent>()!;
//        var buffer = new ContactPair[8];
//        int count = PhysicsWorld.GetContacts(body, buffer, out _);

//        Assert.True(count > 0, "GetContacts should return at least one contact when resting on a surface.");
//    }

//    [Fact]
//    public void GetContactsAll_TwoBodiesInContact_ListNonEmpty()
//    {
//        var world = CreateTestWorld();
//        var system = CreateSystem();

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 100f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(200f, 20f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        var dynEntity = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 50f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//            });

//        world.Flush();
//        Step(world, system, 25);

//        var body = dynEntity.GetComponent<PhysicsBodyComponent>()!;
//        var results = new List<ContactPair>();
//        PhysicsWorld.GetContactsAll(body, results);

//        Assert.NotEmpty(results);
//    }
//}