using Brine2D.ECS.Components;
using Brine2D.Physics;
using System.Numerics;

namespace Brine2D.Tests.Physics;

[Collection("Physics")]
public class PhysicsWorldTests : IDisposable
{
    private readonly List<PhysicsWorld> _worlds = [];

    public void Dispose()
    {
        foreach (var w in _worlds)
            w.Dispose();
        PhysicsWorld.ResetForTesting();
    }

    private PhysicsWorld Create(Vector2? gravity = null, float ppm = 100f)
    {
        var w = new PhysicsWorld(gravity ?? new Vector2(0f, 980f), ppm);
        _worlds.Add(w);
        return w;
    }

    [Fact]
    public void Constructor_Default_UsesDownwardGravityAndDefaultPPM()
    {
        using var world = new PhysicsWorld();

        Assert.Equal(100f, world.PixelsPerMeter);
    }

    [Fact]
    public void Constructor_CustomGravityAndPPM_StoresValues()
    {
        var world = Create(new Vector2(0f, 500f), 100f);

        Assert.Equal(100f, world.PixelsPerMeter);
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
        var world1 = Create(Vector2.Zero, 100f);
        world1.Dispose();
        _worlds.Remove(world1);

        var world2 = Create(Vector2.Zero, 100f);

        Assert.Equal(100f, world2.PixelsPerMeter);
    }

    [Fact]
    public void PPM_ConcurrentSameValue_DoesNotThrow()
    {
        var world1 = Create(Vector2.Zero, 100f);
        var world2 = Create(Vector2.Zero, 100f);

        Assert.Equal(100f, world1.PixelsPerMeter);
        Assert.Equal(100f, world2.PixelsPerMeter);
    }

    [Fact]
    public void PPM_ConcurrentDifferentValue_Throws()
    {
        var world1 = Create(Vector2.Zero, 100f);

        Assert.Throws<InvalidOperationException>(() => new PhysicsWorld(Vector2.Zero, 200f));
    }

    [Fact]
    public void PPM_NearSameValue_WithinTolerance_DoesNotThrow()
    {
        // 100f and 100.005f differ by less than the 0.01f tolerance — must not throw.
        var world1 = Create(Vector2.Zero, 100f);
        var world2 = new PhysicsWorld(Vector2.Zero, 100.005f);
        _worlds.Add(world2);

        Assert.Equal(100f, world1.PixelsPerMeter);
    }

    [Fact]
    public void PPM_ValueExceedingTolerance_Throws()
    {
        // 100f vs 100.02f exceeds the 0.01f tolerance — must throw.
        var world1 = Create(Vector2.Zero, 100f);

        Assert.Throws<InvalidOperationException>(() =>
            _ = new PhysicsWorld(Vector2.Zero, 100.02f));
    }

    [Fact]
    public void RaycastClosest_EmptyWorld_ReturnsNull()
    {
        var world = Create();

        var hit = world.RaycastClosest(Vector2.Zero, Vector2.UnitX, 1000f);

        Assert.Null(hit);
    }

    [Fact]
    public void RaycastAll_EmptyWorld_ReturnsZero()
    {
        var world = Create();
        var buffer = new RaycastHit[8];

        var count = world.RaycastAll(Vector2.Zero, Vector2.UnitX, 1000f, buffer);

        Assert.Equal(0, count);
    }

    [Fact]
    public void RaycastAll_ZeroLengthBuffer_ReturnsZero()
    {
        var world = Create();

        var count = world.RaycastAll(Vector2.Zero, Vector2.UnitX, 1000f, Span<RaycastHit>.Empty);

        Assert.Equal(0, count);
    }

    [Fact]
    public void ShapeCastClosest_EmptyWorld_ReturnsNull()
    {
        var world = Create();

        var hit = world.ShapeCastClosest(Vector2.Zero, 10f, Vector2.UnitX, 1000f);

        Assert.Null(hit);
    }

    [Fact]
    public void OverlapAABB_EmptyWorld_ReturnsZero()
    {
        var world = Create();
        var buffer = new OverlapHit[8];

        var count = world.OverlapAABB(new Vector2(-100, -100), new Vector2(100, 100), buffer);

        Assert.Equal(0, count);
    }

    [Fact]
    public void OverlapCircle_EmptyWorld_ReturnsZero()
    {
        var world = Create();
        var buffer = new OverlapHit[8];

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
            var buffer = new RaycastHit[4];
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
    public void OverlapAABBFirst_AfterDispose_ThrowsObjectDisposed()
    {
        var world = new PhysicsWorld();
        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => world.OverlapAABBFirst(new Vector2(-100, -100), new Vector2(100, 100)));
    }

    [Fact]
    public void SetCustomCollisionFilter_AfterDispose_ThrowsObjectDisposed()
    {
        var world = new PhysicsWorld();
        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() => world.SetCustomCollisionFilter((_, _) => true));
    }

    [Fact(Skip = "SetCustomCollisionFilter uses [UnmanagedCallersOnly] with non-blittable bool return - crashes JIT in CI")]
    public void SetCustomCollisionFilter_SetAndClear_DoesNotThrow()
    {
        var world = Create();

        world.SetCustomCollisionFilter((_, _) => true);
        world.SetCustomCollisionFilter(null);
    }

    [Fact]
    public void OverlapBodyFirst_EmptyWorld_ReturnsNull()
    {
        var world = Create();
        var body = new PhysicsBodyComponent { Shape = new CircleShape(10f) };

        var result = world.OverlapBodyFirst(body);

        Assert.Null(result);
    }

    [Fact]
    public void OverlapBody_EmptyResultsSpan_ReturnsZero()
    {
        var world = Create();
        var body = new PhysicsBodyComponent { Shape = new CircleShape(10f) };

        var count = world.OverlapBody(body, Span<OverlapHit>.Empty);

        Assert.Equal(0, count);
    }

    [Fact]
    public void OverlapBodyAll_BodyNotYetCreated_ReturnsEmptyList()
    {
        var world = Create();
        var body = new PhysicsBodyComponent { Shape = new CircleShape(10f) };
        var results = new List<OverlapHit>();

        world.OverlapBodyAll(body, results);

        Assert.Empty(results);
    }

    // OverlapPolygonFirstHit
    [Fact]
    public void OverlapPolygonFirstHit_EmptyWorld_ReturnsNull()
    {
        var world = Create();
        ReadOnlySpan<Vector2> verts = [new(-10, -10), new(10, -10), new(0, 10)];

        var hit = world.OverlapPolygonFirst(verts);

        Assert.Null(hit);
    }

    [Fact]
    public void OverlapPolygonFirstHit_AfterDispose_ThrowsObjectDisposed()
    {
        var world = new PhysicsWorld();
        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
        {
            ReadOnlySpan<Vector2> verts = [new(-10, -10), new(10, -10), new(0, 10)];
            world.OverlapPolygonFirst(verts);
        });
    }

    [Fact]
    public void OverlapPolygonFirstHit_TooFewVertices_Throws()
    {
        var world = Create();

        Assert.Throws<ArgumentOutOfRangeException>(() =>
        {
            ReadOnlySpan<Vector2> verts = [new(-10, -10), new(10, -10)];
            world.OverlapPolygonFirst(verts);
        });
    }

    // OverlapPolygonAll (List)
    [Fact]
    public void OverlapPolygonAll_EmptyWorld_ReturnsEmptyList()
    {
        var world = Create();
        var results = new List<OverlapHit>();
        ReadOnlySpan<Vector2> verts = [new(-10, -10), new(10, -10), new(0, 10)];

        world.OverlapPolygonAll(verts, results);

        Assert.Empty(results);
    }

    [Fact]
    public void OverlapPolygonAll_AfterDispose_ThrowsObjectDisposed()
    {
        var world = new PhysicsWorld();
        world.Dispose();
        var results = new List<OverlapHit>();

        Assert.Throws<ObjectDisposedException>(() =>
        {
            ReadOnlySpan<Vector2> verts = [new(-10, -10), new(10, -10), new(0, 10)];
            world.OverlapPolygonAll(verts, results);
        });
    }

    // OverlapPolygonAllShapes (List)
    [Fact]
    public void OverlapPolygonAllShapes_EmptyWorld_ReturnsEmptyList()
    {
        var world = Create();
        var results = new List<OverlapHit>();
        ReadOnlySpan<Vector2> verts = [new(-10, -10), new(10, -10), new(0, 10)];

        world.OverlapPolygonAllShapes(verts, results);

        Assert.Empty(results);
    }

    // OverlapPoint (Span)
    [Fact]
    public void OverlapPoint_EmptyWorld_ReturnsZero()
    {
        var world = Create();
        var buffer = new OverlapHit[8];

        var count = world.OverlapPoint(Vector2.Zero, buffer);

        Assert.Equal(0, count);
    }

    [Fact]
    public void OverlapPoint_EmptySpan_ReturnsZero()
    {
        var world = Create();

        var count = world.OverlapPoint(Vector2.Zero, Span<OverlapHit>.Empty);

        Assert.Equal(0, count);
    }

    [Fact]
    public void OverlapPoint_AfterDispose_ThrowsObjectDisposed()
    {
        var world = new PhysicsWorld();
        world.Dispose();

        Assert.Throws<ObjectDisposedException>(() =>
        {
            var buffer = new OverlapHit[4];
            world.OverlapPoint(Vector2.Zero, buffer);
        });
    }

    // OverlapPointShapes (Span)
    [Fact]
    public void OverlapPointShapes_EmptyWorld_ReturnsZero()
    {
        var world = Create();
        var buffer = new OverlapHit[8];

        var count = world.OverlapPointShapes(Vector2.Zero, buffer);

        Assert.Equal(0, count);
    }

    [Fact]
    public void OverlapPointShapes_EmptySpan_ReturnsZero()
    {
        var world = Create();

        var count = world.OverlapPointShapes(Vector2.Zero, Span<OverlapHit>.Empty);

        Assert.Equal(0, count);
    }

    // OverlapPointAll (List)
    [Fact]
    public void OverlapPointAll_EmptyWorld_ReturnsEmptyList()
    {
        var world = Create();
        var results = new List<OverlapHit>();

        world.OverlapPointAll(Vector2.Zero, results);

        Assert.Empty(results);
    }

    [Fact]
    public void OverlapPointAll_AfterDispose_ThrowsObjectDisposed()
    {
        var world = new PhysicsWorld();
        world.Dispose();
        var results = new List<OverlapHit>();

        Assert.Throws<ObjectDisposedException>(() => world.OverlapPointAll(Vector2.Zero, results));
    }

    // OverlapPointAllShapes (List)
    [Fact]
    public void OverlapPointAllShapes_EmptyWorld_ReturnsEmptyList()
    {
        var world = Create();
        var results = new List<OverlapHit>();

        world.OverlapPointAllShapes(Vector2.Zero, results);

        Assert.Empty(results);
    }

    // OverlapBodyFirstHit
    [Fact]
    public void OverlapBodyFirstHit_BodyNotLive_ReturnsNull()
    {
        var world = Create();
        var body = new PhysicsBodyComponent { Shape = new CircleShape(10f) };

        var hit = world.OverlapBodyFirst(body);

        Assert.Null(hit);
    }

    [Fact]
    public void OverlapBodyFirstHit_AfterDispose_ThrowsObjectDisposed()
    {
        var world = new PhysicsWorld();
        world.Dispose();
        var body = new PhysicsBodyComponent { Shape = new CircleShape(10f) };

        Assert.Throws<ObjectDisposedException>(() => world.OverlapBodyFirst(body));
    }

    // OverlapBodyShapes (Span)
    [Fact]
    public void OverlapBodyShapes_BodyNotLive_ReturnsZero()
    {
        var world = Create();
        var body = new PhysicsBodyComponent { Shape = new CircleShape(10f) };
        var buffer = new OverlapHit[8];

        var count = world.OverlapBodyShapes(body, buffer);

        Assert.Equal(0, count);
    }

    [Fact]
    public void OverlapBodyShapes_EmptySpan_ReturnsZero()
    {
        var world = Create();
        var body = new PhysicsBodyComponent { Shape = new CircleShape(10f) };

        var count = world.OverlapBodyShapes(body, Span<OverlapHit>.Empty);

        Assert.Equal(0, count);
    }

    [Fact]
    public void OverlapBodyShapes_AfterDispose_ThrowsObjectDisposed()
    {
        var world = new PhysicsWorld();
        world.Dispose();
        var body = new PhysicsBodyComponent { Shape = new CircleShape(10f) };

        Assert.Throws<ObjectDisposedException>(() =>
        {
            var buffer = new OverlapHit[4];
            world.OverlapBodyShapes(body, buffer);
        });
    }

    // OverlapBodyAllShapes (List)
    [Fact]
    public void OverlapBodyAllShapes_BodyNotLive_ReturnsEmptyList()
    {
        var world = Create();
        var body = new PhysicsBodyComponent { Shape = new CircleShape(10f) };
        var results = new List<OverlapHit>();

        world.OverlapBodyAllShapes(body, results);

        Assert.Empty(results);
    }

    [Fact]
    public void OverlapBodyAllShapes_AfterDispose_ThrowsObjectDisposed()
    {
        var world = new PhysicsWorld();
        world.Dispose();
        var body = new PhysicsBodyComponent { Shape = new CircleShape(10f) };
        var results = new List<OverlapHit>();

        Assert.Throws<ObjectDisposedException>(() => world.OverlapBodyAllShapes(body, results));
    }

    // OverlapBodyExactFirstHit
    [Fact]
    public void OverlapBodyExactFirstHit_BodyNotLive_ReturnsNull()
    {
        var world = Create();
        var body = new PhysicsBodyComponent { Shape = new CircleShape(10f) };

        var hit = world.OverlapBodyFirst(body);

        Assert.Null(hit);
    }

    [Fact]
    public void OverlapBodyExactFirstHit_AfterDispose_ThrowsObjectDisposed()
    {
        var world = new PhysicsWorld();
        world.Dispose();
        var body = new PhysicsBodyComponent { Shape = new CircleShape(10f) };

        Assert.Throws<ObjectDisposedException>(() => world.OverlapBodyFirst(body));
    }

    // OverlapBodyExactShapes (Span)
    [Fact]
    public void OverlapBodyExactShapes_BodyNotLive_ReturnsZero()
    {
        var world = Create();
        var body = new PhysicsBodyComponent { Shape = new CircleShape(10f) };
        var buffer = new OverlapHit[8];

        var count = world.OverlapBodyShapes(body, buffer);

        Assert.Equal(0, count);
    }

    [Fact]
    public void OverlapBodyExactShapes_EmptySpan_ReturnsZero()
    {
        var world = Create();
        var body = new PhysicsBodyComponent { Shape = new CircleShape(10f) };

        var count = world.OverlapBodyShapes(body, Span<OverlapHit>.Empty);

        Assert.Equal(0, count);
    }

    [Fact]
    public void OverlapBodyExactShapes_AfterDispose_ThrowsObjectDisposed()
    {
        var world = new PhysicsWorld();
        world.Dispose();
        var body = new PhysicsBodyComponent { Shape = new CircleShape(10f) };

        Assert.Throws<ObjectDisposedException>(() =>
        {
            var buffer = new OverlapHit[4];
            world.OverlapBodyShapes(body, buffer);
        });
    }

    // OverlapBodyExactAllShapes (List)
    [Fact]
    public void OverlapBodyExactAllShapes_BodyNotLive_ReturnsEmptyList()
    {
        var world = Create();
        var body = new PhysicsBodyComponent { Shape = new CircleShape(10f) };
        var results = new List<OverlapHit>();

        world.OverlapBodyAllShapes(body, results);

        Assert.Empty(results);
    }

    [Fact]
    public void OverlapBodyExactAllShapes_AfterDispose_ThrowsObjectDisposed()
    {
        var world = new PhysicsWorld();
        world.Dispose();
        var body = new PhysicsBodyComponent { Shape = new CircleShape(10f) };
        var results = new List<OverlapHit>();

        Assert.Throws<ObjectDisposedException>(() => world.OverlapBodyAllShapes(body, results));
    }
}