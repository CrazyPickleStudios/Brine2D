//using System.Numerics;
//using Brine2D.Collision;
//using Brine2D.Core;
//using Brine2D.ECS;
//using Brine2D.ECS.Components;
//using Brine2D.ECS.Components.Joints;
//using Brine2D.Physics;
//using Brine2D.Systems.Physics;

//namespace Brine2D.Tests.Systems.Physics;

//[Collection("Physics")]
//public class Box2DPhysicsSystemTests : PhysicsTestBase, IDisposable
//{
//    [Fact]
//    public void FixedUpdate_DynamicBody_FallsWithGravity()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));
//        world.Flush();

//        system.FixedUpdate(world, FixedTime);

//        var transform = world.Entities.First().GetComponent<TransformComponent>()!;
//        // Gravity is (0, 980) by default, body should have moved down
//        Assert.True(transform.Position.Y > 0f);
//    }

//    [Fact]
//    public void FixedUpdate_StaticBody_DoesNotMove()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 200f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(100f, 20f);
//                c.BodyType = PhysicsBodyType.Static;
//            });
//        world.Flush();

//        system.FixedUpdate(world, FixedTime);

//        var transform = world.Entities.First().GetComponent<TransformComponent>()!;
//        Assert.Equal(100f, transform.Position.X);
//        Assert.Equal(200f, transform.Position.Y);
//    }

//    [Fact]
//    public void FixedUpdate_BodyCreated_MarksNotDirty()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(5f));
//        world.Flush();

//        var collider = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
//        Assert.True(collider.IsDirty);

//        system.FixedUpdate(world, FixedTime);

//        Assert.False(collider.IsDirty);
//    }

//    [Fact]
//    public void FixedUpdate_BoxShape_CreatesBody()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new BoxShape(30f, 20f));
//        world.Flush();

//        system.FixedUpdate(world, FixedTime);

//        var collider = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
//        Assert.True(Box2D.NET.Bindings.B2.BodyIsValid(collider.BodyId));
//        Assert.True(Box2D.NET.Bindings.B2.ShapeIsValid(collider.ShapeId));
//    }

//    [Fact]
//    public void FixedUpdate_CollisionBetweenTwoBodies_DispatchesEvents()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        bool collisionDetected = false;

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 100f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(50f);
//                c.OnCollisionEnter += (other, contact) => collisionDetected = true;
//            });

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 110f))
//            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(50f));
//        world.Flush();

//        // Step multiple times to ensure collision detection
//        for (int i = 0; i < 10; i++)
//            system.FixedUpdate(world, FixedTime);

//        Assert.True(collisionDetected);
//    }

//    [Fact]
//    public void FixedUpdate_DynamicBody_AcquiresDownwardVelocityFromGravity()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(10f));
//        world.Flush();

//        system.FixedUpdate(world, FixedTime);

//        var physicsBody = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
//        Assert.True(physicsBody.LinearVelocity.Y > 0f);
//    }

//    [Fact]
//    public void FixedUpdate_TriggerSensor_DispatchesSensorEvents()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        bool triggerDetected = false;

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 100f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(50f);
//                c.IsTrigger = true;
//                c.BodyType = PhysicsBodyType.Static;
//                c.OnTriggerEnter += (_) => triggerDetected = true;
//            });

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 100f))
//            .AddComponent<PhysicsBodyComponent>(c => c.Shape = new CircleShape(50f));
//        world.Flush();

//        for (int i = 0; i < 10; i++)
//            system.FixedUpdate(world, FixedTime);

//        Assert.True(triggerDetected);
//    }

//    [Fact]
//    public void FixedUpdate_ColliderWithOffset_AppliesOffset()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 100f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.Offset = new Vector2(20f, 0f);
//                c.BodyType = PhysicsBodyType.Static;
//            });
//        world.Flush();

//        system.FixedUpdate(world, FixedTime);

//        var collider = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
//        Assert.True(Box2D.NET.Bindings.B2.BodyIsValid(collider.BodyId));
//    }

//    [Fact]
//    public void FixedUpdate_PolygonShape_CreatesBody()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new PolygonShape([
//                    new Vector2(-20f, -20f),
//                    new Vector2(20f, -20f),
//                    new Vector2(20f, 20f),
//                    new Vector2(-20f, 20f)
//                ]);
//            });
//        world.Flush();

//        system.FixedUpdate(world, FixedTime);

//        var collider = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
//        Assert.True(Box2D.NET.Bindings.B2.BodyIsValid(collider.BodyId));
//        Assert.True(Box2D.NET.Bindings.B2.ShapeIsValid(collider.ShapeId));
//    }

//    [Fact]
//    public void FixedUpdate_BulletAndFixedRotation_AppliedToBody()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(5f);
//                c.IsBullet = true;
//                c.FixedRotation = true;
//            });
//        world.Flush();

//        system.FixedUpdate(world, FixedTime);

//        var collider = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
//        Assert.True(Box2D.NET.Bindings.B2.BodyIsValid(collider.BodyId));
//        Assert.True(Box2D.NET.Bindings.B2.BodyIsBullet(collider.BodyId));
//        Assert.True(Box2D.NET.Bindings.B2.BodyIsFixedRotation(collider.BodyId));
//    }

//    [Fact]
//    public void FixedUpdate_Restitution_AppliedToShape()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.Restitution = 0.8f;
//                c.SurfaceFriction = 0.3f;
//            });
//        world.Flush();

//        system.FixedUpdate(world, FixedTime);

//        var collider = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
//        Assert.True(Box2D.NET.Bindings.B2.ShapeIsValid(collider.ShapeId));
//    }

//    // ----- Bug #3: ApplyMass — mixed solid + trigger sub-shapes -----

//    [Fact]
//    public void ApplyMass_MixedSolidAndTriggerSubShapes_MassMatchesConfigured()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        const float targetMass = 5f;

//        world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(40f, 40f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//                c.Mass = targetMass;
//                c.AddSubShape(new CircleShape(20f) { Offset = new Vector2(50f, 0f) }, isTrigger: true);
//            });
//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var collider = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
//        var massData = Box2D.NET.Bindings.B2.BodyGetMassData(collider.BodyId);

//        Assert.Equal(targetMass, massData.mass, precision: 2);
//    }

//    [Fact]
//    public void ApplyMass_AllTriggerBody_MassMatchesConfigured()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        const float targetMass = 3f;

//        world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(20f);
//                c.IsTrigger = true;
//                c.BodyType = PhysicsBodyType.Dynamic;
//                c.Mass = targetMass;
//            });
//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var collider = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
//        var massData = Box2D.NET.Bindings.B2.BodyGetMassData(collider.BodyId);

//        Assert.Equal(targetMass, massData.mass, precision: 2);
//    }

//    // ----- Missing #7: GetAllBodies / GetSleepingBodies -----

//    [Fact]
//    public void GetAllBodies_AfterFixedUpdate_ReturnsAllRegisteredBodies()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        for (int i = 0; i < 3; i++)
//        {
//            world.CreateEntity()
//                .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(i * 200f, 0f))
//                .AddComponent<PhysicsBodyComponent>(c =>
//                {
//                    c.Shape = new CircleShape(10f);
//                    c.BodyType = PhysicsBodyType.Static;
//                });
//        }
//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var bodies = PhysicsWorld.GetAllBodies().ToList();

//        Assert.Equal(3, bodies.Count);
//    }

//    [Fact]
//    public void GetAllBodies_BeforeSystemInitialized_ReturnsEmpty()
//    {
//        var bodies = PhysicsWorld.GetAllBodies().ToList();

//        Assert.Empty(bodies);
//    }

//    [Fact]
//    public void GetSleepingBodies_AllBodiesAwake_ReturnsEmpty()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>()
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//            });
//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        // Body just started moving under gravity — it is awake.
//        var sleeping = PhysicsWorld.GetSleepingBodies().ToList();

//        Assert.Empty(sleeping);
//    }

//    [Fact]
//    public void GetSleepingBodies_BeforeSystemInitialized_ReturnsEmpty()
//    {
//        var sleeping = PhysicsWorld.GetSleepingBodies().ToList();

//        Assert.Empty(sleeping);
//    }

//    // ----- Bug #6: GetLiveContact — stay events fire for touching bodies -----

//    [Fact]
//    public void DispatchStayEvents_TwoBodiesInContact_FiresCollisionStay()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        bool stayFired = false;

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(40f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 50f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(40f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//                c.OnCollisionStay += (_, _) => stayFired = true;
//            });
//        world.Flush();

//        // First step registers enter; subsequent steps fire stay.
//        for (int i = 0; i < 3; i++)
//            system.FixedUpdate(world, FixedTime);

//        Assert.True(stayFired);
//    }

//    [Fact]
//    public void OverlapBody_OverlappingBodies_ReturnsOtherBody()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        // Two boxes placed so their AABBs overlap.
//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(50f, 50f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(10f, 10f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(50f, 50f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var bodyA = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
//        var results = new List<OverlapHit>();
//        PhysicsWorld.OverlapBodyAll(bodyA, results);

//        Assert.Single(results);
//        Assert.NotEqual(bodyA, results[0].Component);
//    }

//    [Fact]
//    public void OverlapBodyFirst_NonOverlappingBodies_ReturnsNull()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(10f, 10f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(5000f, 5000f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new BoxShape(10f, 10f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var bodyA = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
//        var result = PhysicsWorld.OverlapBodyFirst(bodyA);

//        Assert.Null(result);
//    }

//    [Fact]
//    public void UnregisterJointsForBody_AfterBodyDestroyed_NoStaleJointsOnOtherBody()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        var entityA = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Dynamic;
//            });

//        var entityB = world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100f, 0f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.Shape = new CircleShape(10f);
//                c.BodyType = PhysicsBodyType.Static;
//            });

//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var bodyA = entityA.GetComponent<PhysicsBodyComponent>()!;
//        var bodyB = entityB.GetComponent<PhysicsBodyComponent>()!;

//        // Connect them with a distance joint.
//        entityA.AddComponent<DistanceJointComponent>(j =>
//        {
//            j.ConnectedBody = bodyB;
//            j.Length = 100f;
//        });
//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        // Verify the joint is live from both sides before destroying.
//        var jointsOnB = new JointComponent[4];
//        int countBefore = PhysicsWorld.GetJoints(bodyB, jointsOnB);
//        Assert.Equal(1, countBefore);

//        // Destroy entity A — this triggers UnregisterJointsForBody for body A's index.
//        entityA.Destroy();
//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        // Body B's joint registry must now be empty — no stale dead joint.
//        int countAfter = PhysicsWorld.GetJoints(bodyB, jointsOnB);
//        Assert.Equal(0, countAfter);
//    }
//}