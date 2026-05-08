//using System.Numerics;
//using Brine2D.ECS.Components;
//using Brine2D.Physics;
//using Brine2D.Systems.Physics;
//using Brine2D.Core;

//namespace Brine2D.Tests.Physics;

//[Collection("Physics")]
//public class OverlapBodyDeduplicationTests : PhysicsTestBase
//{
//    public OverlapBodyDeduplicationTests() : base(gravity: Vector2.Zero) { }

//    [Fact]
//    public void OverlapBodyAll_MultipleOverlappingBodies_ReturnsDistinctResults()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = Vector2.Zero)
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.BodyType = PhysicsBodyType.Static;
//                c.Shape = new CircleShape(10f);
//            });

//        for (int i = 0; i < 3; i++)
//        {
//            world.CreateEntity()
//                .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(i * 2f, 0f))
//                .AddComponent<PhysicsBodyComponent>(c =>
//                {
//                    c.BodyType = PhysicsBodyType.Static;
//                    c.Shape = new CircleShape(10f);
//                });
//        }

//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var queryBody = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
//        var buffer = new OverlapHit[8];
//        int count = PhysicsWorld.OverlapBody(queryBody, buffer, out bool wasTruncated);

//        Assert.False(wasTruncated);
//        Assert.Equal(3, count);

//        var bodyIds = buffer[..count].Select(h => h.Component).Distinct().ToList();
//        Assert.Equal(3, bodyIds.Count);
//    }

//    [Fact]
//    public void OverlapBodyAll_CompoundQueryBody_DeduplicatesTargetsAcrossShapes()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = Vector2.Zero)
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.BodyType = PhysicsBodyType.Static;
//                c.Shape = new CircleShape(30f);
//                c.AddSubShape(new CircleShape(30f));
//            });

//        for (int i = 0; i < 2; i++)
//        {
//            world.CreateEntity()
//                .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(i * 5f, 0f))
//                .AddComponent<PhysicsBodyComponent>(c =>
//                {
//                    c.BodyType = PhysicsBodyType.Static;
//                    c.Shape = new CircleShape(20f);
//                });
//        }

//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var queryBody = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
//        var buffer = new OverlapHit[8];
//        int count = PhysicsWorld.OverlapBody(queryBody, buffer, out bool wasTruncated);

//        Assert.False(wasTruncated);

//        var distinct = buffer[..count].Select(h => h.Component).Distinct().ToList();
//        Assert.Equal(count, distinct.Count);
//        Assert.Equal(2, count);
//    }

//    [Fact]
//    public void OverlapBodyAll_BufferSmallerThanOverlapCount_SetsTruncated()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = Vector2.Zero)
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.BodyType = PhysicsBodyType.Static;
//                c.Shape = new CircleShape(10f);
//            });

//        for (int i = 0; i < 4; i++)
//        {
//            world.CreateEntity()
//                .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(i * 2f, 0f))
//                .AddComponent<PhysicsBodyComponent>(c =>
//                {
//                    c.BodyType = PhysicsBodyType.Static;
//                    c.Shape = new CircleShape(10f);
//                });
//        }

//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var queryBody = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
//        var buffer = new OverlapHit[2];
//        int count = PhysicsWorld.OverlapBody(queryBody, buffer, out bool wasTruncated);

//        Assert.True(wasTruncated);
//        Assert.Equal(2, count);

//        var distinct = buffer[..count].Select(h => h.Component).Distinct().ToList();
//        Assert.Equal(2, distinct.Count);
//    }

//    [Fact]
//    public void OverlapBodyAll_ExactBufferSize_NoTruncation()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = Vector2.Zero)
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.BodyType = PhysicsBodyType.Static;
//                c.Shape = new CircleShape(10f);
//            });

//        for (int i = 0; i < 3; i++)
//        {
//            world.CreateEntity()
//                .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(i * 2f, 0f))
//                .AddComponent<PhysicsBodyComponent>(c =>
//                {
//                    c.BodyType = PhysicsBodyType.Static;
//                    c.Shape = new CircleShape(10f);
//                });
//        }

//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var queryBody = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
//        var buffer = new OverlapHit[3];
//        int count = PhysicsWorld.OverlapBody(queryBody, buffer, out bool wasTruncated);

//        Assert.False(wasTruncated);
//        Assert.Equal(3, count);
//    }
//}