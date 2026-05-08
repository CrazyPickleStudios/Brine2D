//using System.Numerics;
//using Brine2D.Collision;
//using Brine2D.ECS.Components;
//using Brine2D.Physics;
//using Brine2D.Systems.Physics;
//using Brine2D.Core;

//namespace Brine2D.Tests.Systems.Physics;

//[Collection("Physics")]
//public class PolygonShapeOffsetIntegrationTests : PhysicsTestBase
//{
//    private static Vector2[] SquareVerts(float halfSize) =>
//    [
//        new(-halfSize, -halfSize), new(halfSize, -halfSize),
//        new(halfSize, halfSize),   new(-halfSize, halfSize)
//    ];

//    [Fact]
//    public void PolygonShape_WithOffset_BuildsValidLiveBody()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(500f, 500f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.BodyType = PhysicsBodyType.Static;
//                c.Shape = new PolygonShape(SquareVerts(20f)) { Offset = new Vector2(50f, 0f) };
//            });
//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var body = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
//        Assert.True(Box2D.NET.Bindings.B2.BodyIsValid(body.BodyId));
//        Assert.True(Box2D.NET.Bindings.B2.ShapeIsValid(body.ShapeId));
//    }

//    [Fact]
//    public void PolygonShape_WithOffset_AabbCenterIsShifted()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);
//        const float offsetX = 100f;

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(500f, 500f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.BodyType = PhysicsBodyType.Static;
//                c.Shape = new PolygonShape(SquareVerts(20f)) { Offset = new Vector2(offsetX, 0f) };
//            });

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(500f, 500f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.BodyType = PhysicsBodyType.Static;
//                c.Shape = new PolygonShape(SquareVerts(20f));
//            });

//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var entities = world.Entities.ToList();
//        var withOffset = entities[0].GetComponent<PhysicsBodyComponent>()!;
//        var noOffset = entities[1].GetComponent<PhysicsBodyComponent>()!;

//        withOffset.TryGetAABB(out var offsetMin, out var offsetMax);
//        noOffset.TryGetAABB(out var baseMin, out var baseMax);

//        float offsetCenterX = (offsetMin.X + offsetMax.X) / 2f;
//        float baseCenterX = (baseMin.X + baseMax.X) / 2f;

//        Assert.Equal(offsetX, offsetCenterX - baseCenterX, precision: 1);
//    }

//    [Fact]
//    public void PolygonShape_SubShape_WithOffset_BuildsValidBody()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(500f, 500f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.BodyType = PhysicsBodyType.Static;
//                c.Shape = new BoxShape(40f, 40f);
//                c.AddSubShape(new PolygonShape(SquareVerts(15f)) { Offset = new Vector2(60f, 0f) });
//            });
//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var body = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
//        Assert.True(Box2D.NET.Bindings.B2.BodyIsValid(body.BodyId));
//        Assert.True(Box2D.NET.Bindings.B2.ShapeIsValid(body.ShapeId));
//    }

//    [Fact]
//    public void PolygonShape_ZeroOffset_MatchesNoOffsetShape()
//    {
//        var world = CreateTestWorld();
//        var system = new Box2DPhysicsSystem(PhysicsWorld);

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(500f, 500f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.BodyType = PhysicsBodyType.Static;
//                c.Shape = new PolygonShape(SquareVerts(20f)) { Offset = Vector2.Zero };
//            });

//        world.CreateEntity()
//            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(500f, 500f))
//            .AddComponent<PhysicsBodyComponent>(c =>
//            {
//                c.BodyType = PhysicsBodyType.Static;
//                c.Shape = new PolygonShape(SquareVerts(20f));
//            });

//        world.Flush();
//        system.FixedUpdate(world, FixedTime);

//        var entities = world.Entities.ToList();
//        entities[0].GetComponent<PhysicsBodyComponent>()!.TryGetAABB(out var minA, out var maxA);
//        entities[1].GetComponent<PhysicsBodyComponent>()!.TryGetAABB(out var minB, out var maxB);

//        Assert.Equal(minA.X, minB.X, precision: 1);
//        Assert.Equal(minA.Y, minB.Y, precision: 1);
//        Assert.Equal(maxA.X, maxB.X, precision: 1);
//        Assert.Equal(maxA.Y, maxB.Y, precision: 1);
//    }
//}