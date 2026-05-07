using System.Numerics;
using Brine2D.Collision;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Physics;
using Brine2D.Systems.Physics;

namespace Brine2D.Tests.Systems.Physics;

/// <summary>
/// Integration tests for PhysicsWorld overlap query methods.
/// Each test wires up a Box2DPhysicsSystem so ComponentResolver and AllBodiesResolver
/// are registered, then steps the world once so bodies are live in Box2D.
/// </summary>
[Collection("Physics")]
public class PhysicsWorldOverlapTests : TestBase, IDisposable
{
    private static readonly GameTime OneStep = new(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0));

    private readonly PhysicsWorld _physicsWorld = new();

    public void Dispose() => _physicsWorld.Dispose();

    private (IEntityWorld world, Box2DPhysicsSystem system) Setup() =>
        (CreateTestWorld(), new Box2DPhysicsSystem(_physicsWorld));

    private (IEntityWorld world, Box2DPhysicsSystem system, PhysicsBodyComponent body)
        SetupWithStaticCircleAt(Vector2 position, float radius = 30f)
    {
        var (world, system) = Setup();
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = position)
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(radius);
                c.BodyType = PhysicsBodyType.Static;
            });
        world.Flush();
        system.FixedUpdate(world, OneStep);
        var body = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        return (world, system, body);
    }

    private (IEntityWorld world, Box2DPhysicsSystem system, PhysicsBodyComponent solid, PhysicsBodyComponent sensor)
        SetupWithSolidAndSensorCircle(Vector2 position, float radius = 30f)
    {
        var (world, system) = Setup();
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = position)
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(radius);
                c.BodyType = PhysicsBodyType.Static;
            });
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = position)
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(radius);
                c.BodyType = PhysicsBodyType.Static;
                c.IsTrigger = true;
            });
        world.Flush();
        system.FixedUpdate(world, OneStep);
        var entities = world.Entities.ToArray();
        var solid = entities[0].GetComponent<PhysicsBodyComponent>()!;
        var sensor = entities[1].GetComponent<PhysicsBodyComponent>()!;
        return (world, system, solid, sensor);
    }
    
    [Fact]
    public void OverlapPointFirst_BodyAtPoint_ReturnsComponent()
    {
        var (_, _, body) = SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);

        var result = _physicsWorld.OverlapPointFirst(new Vector2(200f, 200f));

        Assert.NotNull(result);
        Assert.Same(body, result.Value.Component);
    }

    [Fact]
    public void OverlapPointFirst_NoBodyAtPoint_ReturnsNull()
    {
        SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);

        var result = _physicsWorld.OverlapPointFirst(new Vector2(9000f, 9000f));

        Assert.Null(result);
    }

    [Fact]
    public void OverlapPointFirstHit_BodyAtPoint_ReturnsHitWithComponent()
    {
        var (_, _, body) = SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);

        var hit = _physicsWorld.OverlapPointFirst(new Vector2(200f, 200f));

        Assert.NotNull(hit);
        Assert.Same(body, hit.Value.Component);
    }

    [Fact]
    public void OverlapPointFirstHit_PrimaryShape_SubShapeIsNull()
    {
        var (world, system) = Setup();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(300f, 300f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(30f);
                c.BodyType = PhysicsBodyType.Static;
                c.AddSubShape(new CircleShape(10f) { Offset = new Vector2(50f, 0f) });
            });

        world.Flush();
        system.FixedUpdate(world, OneStep);

        var hit = _physicsWorld.OverlapPointFirst(new Vector2(300f, 300f));

        Assert.NotNull(hit);
        Assert.Null(hit.Value.SubShape);
    }

    [Fact]
    public void OverlapPointFirstHit_SubShapeHit_SubShapeReturned()
    {
        var (world, system) = Setup();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(200f, 200f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Static;
                c.AddSubShape(new CircleShape(20f) { Offset = new Vector2(80f, 0f) });
            });

        world.Flush();
        system.FixedUpdate(world, OneStep);

        var hit = _physicsWorld.OverlapPointFirst(new Vector2(280f, 200f));

        Assert.NotNull(hit);
        Assert.NotNull(hit.Value.SubShape);
    }
    
    [Fact]
    public void OverlapCircleFirst_ExcludeSensors_SkipsSensor()
    {
        var (_, _, solid, _) = SetupWithSolidAndSensorCircle(new Vector2(200f, 200f), 30f);

        var result = _physicsWorld.OverlapCircleFirst(
            new Vector2(200f, 200f), 50f, PhysicsQueryFilter.SolidOnly);

        Assert.NotNull(result);
        Assert.Same(solid, result.Value.Component);
    }

    [Fact]
    public void OverlapCircleFirst_NoExcludeSensors_ReturnsSomething()
    {
        SetupWithSolidAndSensorCircle(new Vector2(200f, 200f), 30f);

        var result = _physicsWorld.OverlapCircleFirst(new Vector2(200f, 200f), 50f);

        Assert.NotNull(result);
    }

    [Fact]
    public void OverlapCircle_ExcludeSensors_ExcludesSensorBody()
    {
        SetupWithSolidAndSensorCircle(new Vector2(200f, 200f), 30f);

        var results = new OverlapHit[8];

        int count = _physicsWorld.OverlapCircle(new Vector2(200f, 200f), 50f, results, PhysicsQueryFilter.SolidOnly);

        Assert.Equal(1, count);
        Assert.False(results[0].Component!.IsTrigger);
    }

    [Fact]
    public void OverlapBoxFirst_ExcludeSensors_SkipsSensor()
    {
        var (_, _, solid, _) = SetupWithSolidAndSensorCircle(new Vector2(200f, 200f), 30f);

        var result = _physicsWorld.OverlapBoxFirst(new Vector2(200f, 200f), 60f, 60f, 0f, PhysicsQueryFilter.SolidOnly);

        Assert.NotNull(result);
        Assert.Same(solid, result.Value.Component);
    }

    [Fact]
    public void OverlapCapsuleFirst_ExcludeSensors_SkipsSensor()
    {
        var (_, _, solid, _) = SetupWithSolidAndSensorCircle(new Vector2(200f, 200f), 30f);

        var result = _physicsWorld.OverlapCapsuleFirst(new Vector2(200f, 150f), new Vector2(200f, 250f), 40f, PhysicsQueryFilter.SolidOnly);

        Assert.NotNull(result);
        Assert.Same(solid, result.Value.Component);
    }

    [Fact]
    public void OverlapPolygonFirst_ExcludeSensors_SkipsSensor()
    {
        var (_, _, solid, _) = SetupWithSolidAndSensorCircle(new Vector2(200f, 200f), 30f);

        Vector2[] verts =
        [
            new Vector2(150f, 150f),
            new Vector2(250f, 150f),
            new Vector2(250f, 250f),
            new Vector2(150f, 250f)
        ];

        var result = _physicsWorld.OverlapPolygonFirst(verts, PhysicsQueryFilter.SolidOnly);

        Assert.NotNull(result);
        Assert.Same(solid, result.Value.Component);
    }
    
    [Fact]
    public void SubStepCount_SetValidValue_UpdatesProperty()
    {
        _physicsWorld.SubStepCount = 8;

        Assert.Equal(8, _physicsWorld.SubStepCount);
    }

    [Fact]
    public void SubStepCount_SetToZero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _physicsWorld.SubStepCount = 0);
    }

    [Fact]
    public void SubStepCount_SetToNegative_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => _physicsWorld.SubStepCount = -1);
    }
    
    [Fact]
    public void IsBullet_ChangeOnLiveBody_DoesNotThrow()
    {
        var (world, system) = Setup();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(200f, 200f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.IsBullet = false;
            });

        world.Flush();
        system.FixedUpdate(world, OneStep);

        var body = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        body.IsBullet = true;

        var ex = Record.Exception(() => system.FixedUpdate(world, OneStep));
        Assert.Null(ex);
        Assert.True(body.IsBullet);
    }

    [Fact]
    public void IsBullet_SetSameValue_DoesNotMarkDirty()
    {
        var (world, system) = Setup();

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(200f, 200f))
            .AddComponent<PhysicsBodyComponent>(c =>
            {
                c.Shape = new CircleShape(10f);
                c.BodyType = PhysicsBodyType.Dynamic;
                c.IsBullet = true;
            });

        world.Flush();
        system.FixedUpdate(world, OneStep);

        var body = world.Entities.First().GetComponent<PhysicsBodyComponent>()!;
        body.IsBullet = true;

        Assert.False(body.IsBulletDirty);
    }
    
    [Fact]
    public void OverlapAABBFirst_BodyInAABB_ReturnsComponent()
    {
        var (_, _, body) = SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);

        var result = _physicsWorld.OverlapAABBFirst(new Vector2(150f, 150f), new Vector2(250f, 250f));

        Assert.NotNull(result);
        Assert.Same(body, result.Value.Component);
    }

    [Fact]
    public void OverlapAABBFirst_NoBodyInAABB_ReturnsNull()
    {
        SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);

        var result = _physicsWorld.OverlapAABBFirst(new Vector2(9000f, 9000f), new Vector2(9100f, 9100f));

        Assert.Null(result);
    }

    [Fact]
    public void OverlapAABBFirstHit_BodyInAABB_ReturnsHit()
    {
        var (_, _, body) = SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);

        var hit = _physicsWorld.OverlapAABBFirst(new Vector2(150f, 150f), new Vector2(250f, 250f));

        Assert.NotNull(hit);
        Assert.Same(body, hit.Value.Component);
    }

    [Fact]
    public void OverlapAABBFirstHit_NoBodyInAABB_ReturnsNull()
    {
        SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);

        var hit = _physicsWorld.OverlapAABBFirst(new Vector2(9000f, 9000f), new Vector2(9100f, 9100f));

        Assert.Null(hit);
    }

    [Fact]
    public void OverlapAABB_BodyInAABB_ReturnsOne()
    {
        var (_, _, body) = SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);
        var results = new OverlapHit[8];

        int count = _physicsWorld.OverlapAABB(new Vector2(150f, 150f), new Vector2(250f, 250f), results);

        Assert.Equal(1, count);
        Assert.Same(body, results[0].Component);
    }

    [Fact]
    public void OverlapAABB_EmptyBuffer_ReturnsZero()
    {
        SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);

        int count = _physicsWorld.OverlapAABB(new Vector2(150f, 150f), new Vector2(250f, 250f), Span<OverlapHit>.Empty);

        Assert.Equal(0, count);
    }

    [Fact]
    public void OverlapAABBShapes_BodyInAABB_ReturnsHit()
    {
        SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);
        var results = new OverlapHit[8];

        int count = _physicsWorld.OverlapAABBShapes(new Vector2(150f, 150f), new Vector2(250f, 250f), results);

        Assert.Equal(1, count);
    }

    [Fact]
    public void OverlapAABB_TwoBodies_ReturnsBothDeduplicatedByBody()
    {
        var (world, system) = Setup();

        for (int i = 0; i < 2; i++)
        {
            world.CreateEntity()
                .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(200f + i * 50f, 200f))
                .AddComponent<PhysicsBodyComponent>(c =>
                {
                    c.Shape = new CircleShape(10f);
                    c.BodyType = PhysicsBodyType.Static;
                });
        }

        world.Flush();
        system.FixedUpdate(world, OneStep);

        var results = new OverlapHit[8];
        int count = _physicsWorld.OverlapAABB(new Vector2(100f, 100f), new Vector2(400f, 400f), results);

        Assert.Equal(2, count);
    }
    
    [Fact]
    public void OverlapCircleFirst_BodyInRadius_ReturnsComponent()
    {
        var (_, _, body) = SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);

        var result = _physicsWorld.OverlapCircleFirst(new Vector2(200f, 200f), 50f);

        Assert.NotNull(result);
        Assert.Same(body, result.Value.Component);
    }

    [Fact]
    public void OverlapCircleFirst_NoBodyInRadius_ReturnsNull()
    {
        SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);

        var result = _physicsWorld.OverlapCircleFirst(new Vector2(9000f, 9000f), 50f);

        Assert.Null(result);
    }

    [Fact]
    public void OverlapCircleFirstHit_BodyInRadius_ReturnsHit()
    {
        var (_, _, body) = SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);

        var hit = _physicsWorld.OverlapCircleFirst(new Vector2(200f, 200f), 50f);

        Assert.NotNull(hit);
        Assert.Same(body, hit.Value.Component);
    }

    [Fact]
    public void OverlapCircle_BodyInRadius_ReturnsOne()
    {
        var (_, _, body) = SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);
        var results = new OverlapHit[8];

        int count = _physicsWorld.OverlapCircle(new Vector2(200f, 200f), 50f, results);

        Assert.Equal(1, count);
        Assert.Same(body, results[0]!.Component);
    }

    [Fact]
    public void OverlapCircle_EmptyBuffer_ReturnsZero()
    {
        SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);

        int count = _physicsWorld.OverlapCircle(new Vector2(200f, 200f), 50f, Span<OverlapHit>.Empty);

        Assert.Equal(0, count);
    }

    [Fact]
    public void OverlapCircleShapes_BodyInRadius_ReturnsHit()
    {
        SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);
        var results = new OverlapHit[8];

        int count = _physicsWorld.OverlapCircleShapes(new Vector2(200f, 200f), 50f, results);

        Assert.Equal(1, count);
    }

    [Fact]
    public void OverlapCircle_BodyOutsideRadius_ReturnsZero()
    {
        SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);
        var results = new OverlapHit[8];

        int count = _physicsWorld.OverlapCircle(new Vector2(9000f, 9000f), 1f, results);

        Assert.Equal(0, count);
    }
    
    [Fact]
    public void OverlapCapsuleFirst_BodyInCapsule_ReturnsComponent()
    {
        var (_, _, body) = SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);

        var result = _physicsWorld.OverlapCapsuleFirst(new Vector2(200f, 150f), new Vector2(200f, 250f), 40f);

        Assert.NotNull(result);
        Assert.Same(body, result.Value.Component);
    }

    [Fact]
    public void OverlapCapsule_BodyInCapsule_ReturnsOne()
    {
        var (_, _, body) = SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);
        var results = new OverlapHit[8];

        int count = _physicsWorld.OverlapCapsule(new Vector2(200f, 150f), new Vector2(200f, 250f), 40f, results);

        Assert.Equal(1, count);
        Assert.Same(body, results[0]!.Component);
    }

    [Fact]
    public void OverlapCapsuleShapes_BodyInCapsule_ReturnsHit()
    {
        SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);
        var results = new OverlapHit[8];

        int count = _physicsWorld.OverlapCapsuleShapes(
            new Vector2(200f, 150f), new Vector2(200f, 250f), 40f, results);

        Assert.Equal(1, count);
    }
    
    [Fact]
    public void OverlapBoxFirst_BodyInBox_ReturnsComponent()
    {
        var (_, _, body) = SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);

        var result = _physicsWorld.OverlapBoxFirst(new Vector2(200f, 200f), 60f, 60f, 0f);

        Assert.NotNull(result);
        Assert.Same(body, result.Value.Component);
    }

    [Fact]
    public void OverlapBox_BodyInBox_ReturnsOne()
    {
        var (_, _, body) = SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);
        var results = new OverlapHit[8];

        int count = _physicsWorld.OverlapBox(new Vector2(200f, 200f), 60f, 60f, 0f, results);

        Assert.Equal(1, count);
        Assert.Same(body, results[0]!.Component);
    }

    [Fact]
    public void OverlapBoxShapes_BodyInBox_ReturnsHit()
    {
        SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);
        var results = new OverlapHit[8];

        int count = _physicsWorld.OverlapBoxShapes(new Vector2(200f, 200f), 60f, 60f, 0f, results);

        Assert.Equal(1, count);
    }

    [Fact]
    public void OverlapBox_BodyOutsideBox_ReturnsZero()
    {
        SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);
        var results = new OverlapHit[8];

        int count = _physicsWorld.OverlapBox(new Vector2(9000f, 9000f), 10f, 10f, 0f, results);

        Assert.Equal(0, count);
    }
    
    [Fact]
    public void OverlapPolygonFirst_BodyInPolygon_ReturnsComponent()
    {
        var (_, _, body) = SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);

        Vector2[] verts =
        [
            new Vector2(150f, 150f),
            new Vector2(250f, 150f),
            new Vector2(250f, 250f),
            new Vector2(150f, 250f)
        ];

        var result = _physicsWorld.OverlapPolygonFirst(verts);

        Assert.NotNull(result);
        Assert.Same(body, result.Value.Component);
    }

    [Fact]
    public void OverlapPolygon_BodyInPolygon_ReturnsOne()
    {
        var (_, _, body) = SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);

        Vector2[] verts =
        [
            new Vector2(150f, 150f),
            new Vector2(250f, 150f),
            new Vector2(250f, 250f),
            new Vector2(150f, 250f)
        ];

        var results = new OverlapHit[8];

        int count = _physicsWorld.OverlapPolygon(verts, results);

        Assert.Equal(1, count);
        Assert.Same(body, results[0]!.Component);
    }

    [Fact]
    public void OverlapPolygonShapes_BodyInPolygon_ReturnsHit()
    {
        SetupWithStaticCircleAt(new Vector2(200f, 200f), 30f);

        Vector2[] verts =
        [
            new Vector2(150f, 150f),
            new Vector2(250f, 150f),
            new Vector2(250f, 250f),
            new Vector2(150f, 250f)
        ];

        var results = new OverlapHit[8];

        int count = _physicsWorld.OverlapPolygonShapes(verts, results);

        Assert.Equal(1, count);
    }

    [Fact]
    public void OverlapPolygon_TooFewVertices_Throws()
    {
        var (_, _) = Setup();
        Vector2[] verts = [new Vector2(0f, 0f), new Vector2(10f, 0f)];
        var results = new OverlapHit[8];

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _physicsWorld.OverlapPolygon(verts, results));
    }
}