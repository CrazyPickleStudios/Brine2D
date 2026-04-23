using System.Numerics;
using Brine2D.Physics;

namespace Brine2D.Tests.Systems.Physics;

public class PhysicsWorldTests : IDisposable
{
    private PhysicsWorld? _world;

    public void Dispose()
    {
        _world?.Dispose();
    }

    [Fact]
    public void Constructor_Default_CreatesWorld()
    {
        _world = new PhysicsWorld();
        Assert.Equal(100f, _world.PixelsPerMeter);
        _ = _world.WorldId; // should not throw
    }

    [Fact]
    public void Constructor_CustomGravityAndScale()
    {
        _world = new PhysicsWorld(new Vector2(0f, 500f), 100f);
        Assert.Equal(100f, _world.PixelsPerMeter);
    }

    [Fact]
    public void Constructor_ZeroPixelsPerMeter_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _world = new PhysicsWorld(Vector2.Zero, 0f));
    }

    [Fact]
    public void Constructor_NegativePixelsPerMeter_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _world = new PhysicsWorld(Vector2.Zero, -1f));
    }

    [Fact]
    public void Step_DoesNotThrow()
    {
        _world = new PhysicsWorld();
        _world.Step(1f / 60f);
    }

    [Fact]
    public void Dispose_ThenWorldId_ThrowsObjectDisposed()
    {
        _world = new PhysicsWorld();
        _world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = _world.WorldId);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        _world = new PhysicsWorld();
        _world.Dispose();
        _world.Dispose();
    }

    [Fact]
    public void Step_AfterDispose_ThrowsObjectDisposed()
    {
        _world = new PhysicsWorld();
        _world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _world.Step(1f / 60f));
    }

    [Fact]
    public void GetContactEvents_AfterDispose_ThrowsObjectDisposed()
    {
        _world = new PhysicsWorld();
        _world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _world.GetContactEvents());
    }

    [Fact]
    public void GetSensorEvents_AfterDispose_ThrowsObjectDisposed()
    {
        _world = new PhysicsWorld();
        _world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _world.GetSensorEvents());
    }

    [Fact]
    public void GetBodyEvents_AfterDispose_ThrowsObjectDisposed()
    {
        _world = new PhysicsWorld();
        _world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _world.GetBodyEvents());
    }

    [Fact]
    public unsafe void CreateBody_ReturnsValidBodyId()
    {
        _world = new PhysicsWorld();

        var bodyDef = Box2D.NET.Bindings.B2.DefaultBodyDef();
        bodyDef.type = Box2D.NET.Bindings.B2.BodyType.dynamicBody;
        var bodyId = _world.CreateBody(&bodyDef);

        Assert.True(Box2D.NET.Bindings.B2.BodyIsValid(bodyId));
    }

    [Fact]
    public unsafe void CreateCircleShape_ReturnsValidShapeId()
    {
        _world = new PhysicsWorld();

        var bodyDef = Box2D.NET.Bindings.B2.DefaultBodyDef();
        bodyDef.type = Box2D.NET.Bindings.B2.BodyType.dynamicBody;
        var bodyId = _world.CreateBody(&bodyDef);

        var shapeDef = Box2D.NET.Bindings.B2.DefaultShapeDef();
        var circle = new Box2D.NET.Bindings.B2.Circle { radius = 10f };
        var shapeId = _world.CreateCircleShape(bodyId, &shapeDef, &circle);

        Assert.True(Box2D.NET.Bindings.B2.ShapeIsValid(shapeId));
    }

    [Fact]
    public unsafe void CreatePolygonShape_ReturnsValidShapeId()
    {
        _world = new PhysicsWorld();

        var bodyDef = Box2D.NET.Bindings.B2.DefaultBodyDef();
        bodyDef.type = Box2D.NET.Bindings.B2.BodyType.dynamicBody;
        var bodyId = _world.CreateBody(&bodyDef);

        var shapeDef = Box2D.NET.Bindings.B2.DefaultShapeDef();
        var box = Box2D.NET.Bindings.B2.MakeBox(20f, 10f);
        var shapeId = _world.CreatePolygonShape(bodyId, &shapeDef, &box);

        Assert.True(Box2D.NET.Bindings.B2.ShapeIsValid(shapeId));
    }

    [Fact]
    public unsafe void GetBodyEvents_ReturnsEvents()
    {
        _world = new PhysicsWorld();

        var bodyDef = Box2D.NET.Bindings.B2.DefaultBodyDef();
        bodyDef.type = Box2D.NET.Bindings.B2.BodyType.dynamicBody;
        var bodyId = _world.CreateBody(&bodyDef);

        var shapeDef = Box2D.NET.Bindings.B2.DefaultShapeDef();
        var circle = new Box2D.NET.Bindings.B2.Circle { radius = 5f };
        _world.CreateCircleShape(bodyId, &shapeDef, &circle);

        _world.Step(1f / 60f);

        var events = _world.GetBodyEvents();
        // Dynamic body with gravity will move, so we expect at least one move event
        Assert.True(events.moveCount >= 1);
    }
}