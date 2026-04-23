using System.Numerics;
using Brine2D.Physics;

namespace Brine2D.Tests.Physics;

public class PhysicsWorldTests
{
    [Fact]
    public void Constructor_Default_UsesDownwardGravityAndDefaultPPM()
    {
        using var world = new PhysicsWorld();

        Assert.Equal(100f, world.PixelsPerMeter);
    }

    [Fact]
    public void Constructor_CustomGravityAndPPM_StoresValues()
    {
        using var world = new PhysicsWorld(new Vector2(0f, 500f), 50f);

        Assert.Equal(50f, world.PixelsPerMeter);
    }

    [Fact]
    public void Constructor_ZeroPPM_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PhysicsWorld(Vector2.Zero, 0f));
    }

    [Fact]
    public void Constructor_NegativePPM_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new PhysicsWorld(Vector2.Zero, -1f));
    }

    [Fact]
    public void Dispose_ThenAccessWorldId_ThrowsObjectDisposed()
    {
        var world = new PhysicsWorld();
        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => _ = world.WorldId);
    }

    [Fact]
    public void Dispose_CalledTwice_DoesNotThrow()
    {
        var world = new PhysicsWorld();
        world.Dispose();
        world.Dispose();
    }

    [Fact]
    public void Step_AfterDispose_ThrowsObjectDisposed()
    {
        var world = new PhysicsWorld();
        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => world.Step(1f / 60f));
    }

    [Fact]
    public void Step_ValidWorld_DoesNotThrow()
    {
        using var world = new PhysicsWorld();

        world.Step(1f / 60f);
    }

    [Fact]
    public void PPM_ResetsAfterAllInstancesDisposed()
    {
        var world1 = new PhysicsWorld(Vector2.Zero, 100f);
        world1.Dispose();

        using var world2 = new PhysicsWorld(Vector2.Zero, 200f);
        Assert.Equal(200f, world2.PixelsPerMeter);
    }

    [Fact]
    public void PPM_ConcurrentSameValue_DoesNotThrow()
    {
        using var world1 = new PhysicsWorld(Vector2.Zero, 100f);
        using var world2 = new PhysicsWorld(Vector2.Zero, 100f);

        Assert.Equal(100f, world1.PixelsPerMeter);
        Assert.Equal(100f, world2.PixelsPerMeter);
    }

    [Fact]
    public void PPM_ConcurrentDifferentValue_Throws()
    {
        using var world1 = new PhysicsWorld(Vector2.Zero, 100f);

        Assert.Throws<InvalidOperationException>(() => new PhysicsWorld(Vector2.Zero, 200f));
    }

    [Fact]
    public void RaycastClosest_EmptyWorld_ReturnsNull()
    {
        using var world = new PhysicsWorld();

        var hit = world.RaycastClosest(Vector2.Zero, Vector2.UnitX, 1000f);

        Assert.Null(hit);
    }

    [Fact]
    public void RaycastAll_EmptyWorld_ReturnsZero()
    {
        using var world = new PhysicsWorld();
        Span<RaycastHit> buffer = stackalloc RaycastHit[8];

        var count = world.RaycastAll(Vector2.Zero, Vector2.UnitX, 1000f, buffer);

        Assert.Equal(0, count);
    }

    [Fact]
    public void RaycastAll_ZeroLengthBuffer_ReturnsZero()
    {
        using var world = new PhysicsWorld();

        var count = world.RaycastAll(Vector2.Zero, Vector2.UnitX, 1000f, Span<RaycastHit>.Empty);

        Assert.Equal(0, count);
    }

    [Fact]
    public void ShapeCastClosest_EmptyWorld_ReturnsNull()
    {
        using var world = new PhysicsWorld();

        var hit = world.ShapeCastClosest(Vector2.Zero, 10f, Vector2.UnitX, 1000f);

        Assert.Null(hit);
    }

    [Fact]
    public void OverlapAABB_EmptyWorld_ReturnsZero()
    {
        using var world = new PhysicsWorld();
        Span<Box2D.NET.Bindings.B2.ShapeId> buffer = stackalloc Box2D.NET.Bindings.B2.ShapeId[8];

        var count = world.OverlapAABB(new Vector2(-100, -100), new Vector2(100, 100), buffer);

        Assert.Equal(0, count);
    }

    [Fact]
    public void OverlapCircle_EmptyWorld_ReturnsZero()
    {
        using var world = new PhysicsWorld();
        Span<Box2D.NET.Bindings.B2.ShapeId> buffer = stackalloc Box2D.NET.Bindings.B2.ShapeId[8];

        var count = world.OverlapCircle(Vector2.Zero, 50f, buffer);

        Assert.Equal(0, count);
    }

    [Fact]
    public void RaycastClosest_AfterDispose_ThrowsObjectDisposed()
    {
        var world = new PhysicsWorld();
        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => world.RaycastClosest(Vector2.Zero, Vector2.UnitX, 100f));
    }

    [Fact]
    public void RaycastAll_AfterDispose_ThrowsObjectDisposed()
    {
        var world = new PhysicsWorld();
        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
        {
            Span<RaycastHit> buffer = stackalloc RaycastHit[4];
            world.RaycastAll(Vector2.Zero, Vector2.UnitX, 100f, buffer);
        });
    }

    [Fact]
    public void ShapeCastClosest_AfterDispose_ThrowsObjectDisposed()
    {
        var world = new PhysicsWorld();
        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => world.ShapeCastClosest(Vector2.Zero, 10f, Vector2.UnitX, 100f));
    }

    [Fact]
    public void SetCustomCollisionFilter_AfterDispose_ThrowsObjectDisposed()
    {
        var world = new PhysicsWorld();
        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => world.SetCustomCollisionFilter((_, _) => true));
    }

    [Fact]
    public void SetCustomCollisionFilter_SetAndClear_DoesNotThrow()
    {
        using var world = new PhysicsWorld();

        world.SetCustomCollisionFilter((_, _) => true);
        world.SetCustomCollisionFilter(null);
    }
}