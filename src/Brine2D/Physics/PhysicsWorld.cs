using System.Buffers;
using System.Numerics;
using System.Runtime.InteropServices;
using Box2D.NET.Bindings;
using Brine2D.Collision;
using Brine2D.ECS.Components;
using Brine2D.ECS.Components.Joints;

namespace Brine2D.Physics;

/// <summary>
/// Scoped wrapper around a Box2D world. One instance per scene, registered as scoped in DI.
/// Users work entirely in pixel coordinates; the <see cref="PixelsPerMeter"/> ratio is applied
/// once via <see cref="B2.SetLengthUnitsPerMeter"/> so Box2D operates in pixel space internally.
/// Because <see cref="B2.SetLengthUnitsPerMeter"/> is a process-wide global, the value is locked
/// to the first instance and released automatically when the last world is disposed.
/// </summary>
public sealed unsafe class PhysicsWorld : IDisposable
{
    private static readonly Lock Lock = new();
    private static float? _lockedPixelsPerMeter;
    private static int _instanceCount;

    private B2.WorldId _worldId;
    private bool _disposed;

    // The system filter receives raw shape IDs so Box2DPhysicsSystem can resolve both
    // body-level and sub-shape-level ShouldCollide predicates in one callback without
    // requiring PhysicsWorld to know about sub-shapes. The user filter retains the
    // friendlier component-pair signature.
    private Func<B2.ShapeId, B2.ShapeId, bool>? _systemFilter;
    private Func<PhysicsBodyComponent, PhysicsBodyComponent, bool>? _userFilter;
    private GCHandle _filterHandle;

    private readonly Dictionary<nint, List<JointComponent>> _bodyJoints = new();

    // Bodies that currently have at least one active contact or sensor pair.
    // Maintained by the physics system via TrackActivePair / UntrackActivePair so that
    // DispatchStayEvents can iterate only active bodies instead of the entire body table.
    private readonly HashSet<PhysicsBodyComponent> _activeBodies = [];

    internal Func<nint, PhysicsBodyComponent?>? ComponentResolver { get; set; }

    /// <summary>
    /// Initializes a new <see cref="PhysicsWorld"/> with default settings.
    /// </summary>
    /// <remarks>
    /// Default gravity is (0, 980) pixels/s² — downward in Y-down screen space, equivalent to
    /// approximately 9.8 m/s² at the default 100 pixels-per-meter scale.
    /// </remarks>
    public PhysicsWorld()
        : this(new Vector2(0f, 980f))
    {
    }

    public PhysicsWorld(Vector2 gravity, float pixelsPerMeter = 100f, int subStepCount = 4)
    {
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(pixelsPerMeter, 0f);
        ArgumentOutOfRangeException.ThrowIfLessThan(subStepCount, 1);

        lock (Lock)
        {
            if (_lockedPixelsPerMeter.HasValue)
            {
                if (Math.Abs(_lockedPixelsPerMeter.Value - pixelsPerMeter) > float.Epsilon)
                {
                    throw new InvalidOperationException(
                        $"B2.SetLengthUnitsPerMeter is a process-wide global and was already set to " +
                        $"{_lockedPixelsPerMeter.Value}. Cannot change to {pixelsPerMeter}.");
                }
            }
            else
            {
                B2.SetLengthUnitsPerMeter(pixelsPerMeter);
                _lockedPixelsPerMeter = pixelsPerMeter;
            }

            _instanceCount++;
        }

        PixelsPerMeter = pixelsPerMeter;
        SubStepCount = subStepCount;
        Gravity = gravity;

        var worldDef = B2.DefaultWorldDef();
        worldDef.gravity = new B2.Vec2 { x = gravity.X, y = gravity.Y };

        _worldId = B2.CreateWorld(&worldDef);
    }

    internal B2.WorldId WorldId
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _worldId;
        }
    }

    public float PixelsPerMeter { get; }

    /// <summary>
    /// Number of Box2D sub-steps per fixed-update tick. Higher values improve simulation
    /// accuracy for fast-moving bodies at the cost of CPU time. Default is 4.
    /// </summary>
    public int SubStepCount { get; }

    /// <summary>
    /// Current world gravity in pixels per second squared.
    /// </summary>
    public Vector2 Gravity { get; private set; }

    /// <summary>
    /// Whether the world is currently paused. When <c>true</c>, <see cref="Step"/> is a no-op
    /// and no simulation or event dispatch occurs. In-flight contact state is preserved.
    /// </summary>
    public bool IsPaused { get; private set; }

    /// <summary>
    /// Sets world gravity at runtime in pixels per second squared.
    /// All awake dynamic bodies are affected immediately.
    /// </summary>
    public void SetGravity(Vector2 gravity)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        Gravity = gravity;
        B2.WorldSetGravity(_worldId, new B2.Vec2 { x = gravity.X, y = gravity.Y });
    }

    /// <summary>
    /// Pauses the world. <see cref="Step"/> becomes a no-op until <see cref="Resume"/> is called.
    /// In-flight contacts and sensor pairs are preserved; no exit events are fired on pause.
    /// </summary>
    public void Pause()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        IsPaused = true;
    }

    /// <summary>
    /// Resumes a paused world.
    /// </summary>
    public void Resume()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        IsPaused = false;
    }

    /// <summary>
    /// Sets the minimum closing speed (pixels/s) at which a contact fires
    /// <see cref="ECS.Components.PhysicsBodyComponent.OnCollisionHit"/>.
    /// Lower values make hit events more sensitive; set to 0 to fire on every contact.
    /// </summary>
    public void SetContactHitEventThreshold(float minSpeed)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(minSpeed);
        B2.WorldSetHitEventThreshold(_worldId, minSpeed);
    }

    /// <summary>
    /// Enables or disables body sleeping. When disabled, all bodies stay awake permanently.
    /// </summary>
    public void SetSleepingEnabled(bool enabled)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        B2.WorldEnableSleeping(_worldId, enabled);
    }

    /// <summary>
    /// Enables or disables continuous collision detection (CCD) for the world.
    /// When disabled, fast-moving bodies may tunnel through thin shapes. Default is enabled.
    /// Per-body bullet mode (<see cref="PhysicsBodyComponent.IsBullet"/>) is unaffected by this
    /// setting and will still use sub-step CCD.
    /// </summary>
    public void SetContinuousEnabled(bool enabled)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        B2.WorldEnableContinuous(_worldId, enabled);
    }

    /// <summary>
    /// Sets the maximum linear speed (pixels/s) a body can reach. Box2D clamps body velocity
    /// to this value each step. Useful as a safety cap in particle-heavy or high-impulse scenes.
    /// Must be greater than zero.
    /// </summary>
    public void SetMaxLinearSpeed(float maxSpeed)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(maxSpeed, 0f);
        B2.WorldSetMaximumLinearSpeed(_worldId, maxSpeed);
    }

    internal void Step(float timeStep)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (IsPaused)
            return;
        B2.WorldStep(_worldId, timeStep, SubStepCount);
    }

    internal B2.BodyId CreateBody(B2.BodyDef* bodyDef)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return B2.CreateBody(_worldId, bodyDef);
    }

    internal B2.ShapeId CreateCircleShape(B2.BodyId bodyId, B2.ShapeDef* shapeDef, B2.Circle* circle)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return B2.CreateCircleShape(bodyId, shapeDef, circle);
    }

    internal B2.ShapeId CreatePolygonShape(B2.BodyId bodyId, B2.ShapeDef* shapeDef, B2.Polygon* polygon)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return B2.CreatePolygonShape(bodyId, shapeDef, polygon);
    }

    internal B2.ChainId CreateChain(B2.BodyId bodyId, B2.ChainDef* chainDef)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return B2.CreateChain(bodyId, chainDef);
    }

    internal B2.ContactEvents GetContactEvents()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return B2.WorldGetContactEvents(_worldId);
    }

    internal B2.SensorEvents GetSensorEvents()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return B2.WorldGetSensorEvents(_worldId);
    }

    internal B2.BodyEvents GetBodyEvents()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return B2.WorldGetBodyEvents(_worldId);
    }

    // ----- Active-body tracking (maintained by Box2DPhysicsSystem) -----

    internal void TrackActivePair(PhysicsBodyComponent a, PhysicsBodyComponent b)
    {
        _activeBodies.Add(a);
        _activeBodies.Add(b);
    }

    internal void UntrackActivePair(PhysicsBodyComponent a, PhysicsBodyComponent b)
    {
        if (a.ActiveContactPairs.Count == 0 && a.ActiveSensorPairs.Count == 0)
            _activeBodies.Remove(a);
        if (b.ActiveContactPairs.Count == 0 && b.ActiveSensorPairs.Count == 0)
            _activeBodies.Remove(b);
    }

    internal void UntrackBody(PhysicsBodyComponent body) => _activeBodies.Remove(body);

    internal HashSet<PhysicsBodyComponent> ActiveBodies => _activeBodies;

    // ----- Joint registry -----

    internal void RegisterJoint(JointComponent joint, nint bodyAIndex, nint bodyBIndex)
    {
        AddToJointRegistry(bodyAIndex, joint);
        AddToJointRegistry(bodyBIndex, joint);
    }

    internal void UnregisterJoint(JointComponent joint, nint bodyAIndex, nint bodyBIndex)
    {
        RemoveFromJointRegistry(bodyAIndex, joint);
        RemoveFromJointRegistry(bodyBIndex, joint);
    }

    internal void UnregisterJointsForBody(nint bodyIndex)
    {
        _bodyJoints.Remove(bodyIndex);
    }

    private void AddToJointRegistry(nint bodyIndex, JointComponent joint)
    {
        if (!_bodyJoints.TryGetValue(bodyIndex, out var list))
        {
            list = [];
            _bodyJoints[bodyIndex] = list;
        }
        if (!list.Contains(joint))
            list.Add(joint);
    }

    private void RemoveFromJointRegistry(nint bodyIndex, JointComponent joint)
    {
        if (!_bodyJoints.TryGetValue(bodyIndex, out var list))
            return;
        list.Remove(joint);
        if (list.Count == 0)
            _bodyJoints.Remove(bodyIndex);
    }

    /// <summary>
    /// Returns all joints that <paramref name="body"/> participates in (as either body A or body B),
    /// written into <paramref name="results"/>.
    /// Returns 0 if the body has no live joints or has not yet been created.
    /// </summary>
    /// <param name="body">The body to query joints for.</param>
    /// <param name="results">Buffer to receive joint components.</param>
    /// <returns>Number of joints written to <paramref name="results"/>.</returns>
    public int GetJoints(PhysicsBodyComponent body, Span<JointComponent> results)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!B2.BodyIsValid(body.BodyId) || results.Length == 0)
            return 0;

        if (!_bodyJoints.TryGetValue(body.BodyId.index1, out var list))
            return 0;

        int written = Math.Min(list.Count, results.Length);
        for (int i = 0; i < written; i++)
            results[i] = list[i];
        return written;
    }

    // ----- Raycasting -----

    /// <summary>
    /// Casts a ray through the world and returns the closest hit.
    /// Returns <c>null</c> if nothing was hit.
    /// </summary>
    /// <param name="origin">Ray start in pixel coordinates.</param>
    /// <param name="direction">Ray direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum ray length in pixels.</param>
    /// <param name="filter">Optional query filter. Pass <c>null</c> to hit all layers.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> is zero.</exception>
    public RaycastHit? RaycastClosest(Vector2 origin, Vector2 direction, float maxDistance, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var norm = NormalizeDirection(direction);
        var translation = new B2.Vec2 { x = norm.X * maxDistance, y = norm.Y * maxDistance };
        var o = new B2.Vec2 { x = origin.X, y = origin.Y };
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        var result = B2.WorldCastRayClosest(_worldId, o, translation, f);

        if (!result.hit)
            return null;

        var hit = ResolveHitFromShapeId(result.shapeId);
        return new RaycastHit
        {
            Point = new Vector2(result.point.x, result.point.y),
            Normal = new Vector2(result.normal.x, result.normal.y),
            Fraction = result.fraction,
            Distance = result.fraction * maxDistance,
            ShapeId = result.shapeId,
            Component = hit.Component,
            SubShape = hit.SubShape
        };
    }

    /// <summary>
    /// Casts a ray and returns all hits sorted by distance, deduplicated by body (one entry per
    /// body), written into the caller-provided buffer. Use <see cref="RaycastAllShapes"/> when
    /// you need per-shape granularity on compound bodies.
    /// </summary>
    /// <param name="origin">Ray start in pixel coordinates.</param>
    /// <param name="direction">Ray direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum ray length in pixels.</param>
    /// <param name="results">Buffer to receive hits. One entry per body, sorted nearest-first.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of hits written to <paramref name="results"/>.</returns>
    /// <remarks>
    /// The internal collection buffer is sized to <c>results.Length * 8</c>. If more shapes than
    /// that overlap the ray, collection stops early and <paramref name="wasTruncated"/> is set to
    /// <c>true</c>. Provide a larger buffer to increase capacity.
    /// </remarks>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> is zero.</exception>
    public int RaycastAll(Vector2 origin, Vector2 direction, float maxDistance,
        Span<RaycastHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        wasTruncated = false;

        if (results.Length == 0)
            return 0;

        var norm = NormalizeDirection(direction);
        var translation = new B2.Vec2 { x = norm.X * maxDistance, y = norm.Y * maxDistance };
        var o = new B2.Vec2 { x = origin.X, y = origin.Y };
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        int rawCapacity = Math.Max(results.Length * 8, 16);
        var rawArray = ArrayPool<RawRaycastHit>.Shared.Rent(rawCapacity);
        try
        {
            fixed (RawRaycastHit* ptr = rawArray)
            {
                var ctx = new RaycastContext { Buffer = ptr, Capacity = rawCapacity, Count = 0 };
                B2.WorldCastRay(_worldId, o, translation, f, &RaycastAllCallback, &ctx);
                int raw = ctx.Count;

                wasTruncated = raw >= rawCapacity;

                new Span<RawRaycastHit>(ptr, raw).Sort((a, b) => a.Fraction.CompareTo(b.Fraction));

                int deduped = DeduplicateRaycastByBody(ptr, raw);
                int written = Math.Min(deduped, results.Length);

                for (int i = 0; i < written; i++)
                {
                    var h = ptr[i];
                    var resolved = ResolveHitFromShapeId(h.ShapeId);
                    results[i] = new RaycastHit
                    {
                        Component = resolved.Component,
                        SubShape = resolved.SubShape,
                        ShapeId = h.ShapeId,
                        Point = h.Point,
                        Normal = h.Normal,
                        Fraction = h.Fraction,
                        Distance = h.Fraction * maxDistance
                    };
                }

                return written;
            }
        }
        finally
        {
            ArrayPool<RawRaycastHit>.Shared.Return(rawArray, clearArray: true);
        }
    }

    /// <inheritdoc cref="RaycastAll(Vector2,Vector2,float,Span{RaycastHit},out bool,PhysicsQueryFilter?)"/>
    public int RaycastAll(Vector2 origin, Vector2 direction, float maxDistance,
        Span<RaycastHit> results, PhysicsQueryFilter? filter = null)
        => RaycastAll(origin, direction, maxDistance, results, out _, filter);

    /// <summary>
    /// Casts a ray and returns all shape hits sorted by distance, without deduplicating by body.
    /// Compound bodies with multiple shapes can appear more than once in the results.
    /// Use this when you need to know exactly which shape on a body was hit.
    /// </summary>
    /// <param name="origin">Ray start in pixel coordinates.</param>
    /// <param name="direction">Ray direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum ray length in pixels.</param>
    /// <param name="results">Buffer to receive hits, sorted nearest-first.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the results buffer was too small to hold all hits.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of hits written to <paramref name="results"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> is zero.</exception>
    public int RaycastAllShapes(Vector2 origin, Vector2 direction, float maxDistance,
        Span<RaycastHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        wasTruncated = false;

        if (results.Length == 0)
            return 0;

        var norm = NormalizeDirection(direction);
        var translation = new B2.Vec2 { x = norm.X * maxDistance, y = norm.Y * maxDistance };
        var o = new B2.Vec2 { x = origin.X, y = origin.Y };
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        int rawCapacity = Math.Max(results.Length, 16);
        var rawArray = ArrayPool<RawRaycastHit>.Shared.Rent(rawCapacity);
        try
        {
            fixed (RawRaycastHit* ptr = rawArray)
            {
                var ctx = new RaycastContext { Buffer = ptr, Capacity = rawCapacity, Count = 0 };
                B2.WorldCastRay(_worldId, o, translation, f, &RaycastAllCallback, &ctx);
                int raw = ctx.Count;

                wasTruncated = raw >= rawCapacity;

                new Span<RawRaycastHit>(ptr, raw).Sort((a, b) => a.Fraction.CompareTo(b.Fraction));

                int written = Math.Min(raw, results.Length);
                for (int i = 0; i < written; i++)
                {
                    var h = ptr[i];
                    var resolved = ResolveHitFromShapeId(h.ShapeId);
                    results[i] = new RaycastHit
                    {
                        Component = resolved.Component,
                        SubShape = resolved.SubShape,
                        ShapeId = h.ShapeId,
                        Point = h.Point,
                        Normal = h.Normal,
                        Fraction = h.Fraction,
                        Distance = h.Fraction * maxDistance
                    };
                }

                return written;
            }
        }
        finally
        {
            ArrayPool<RawRaycastHit>.Shared.Return(rawArray, clearArray: true);
        }
    }

    /// <inheritdoc cref="RaycastAllShapes(Vector2,Vector2,float,Span{RaycastHit},out bool,PhysicsQueryFilter?)"/>
    public int RaycastAllShapes(Vector2 origin, Vector2 direction, float maxDistance,
        Span<RaycastHit> results, PhysicsQueryFilter? filter = null)
        => RaycastAllShapes(origin, direction, maxDistance, results, out _, filter);

    // ----- Shape casting (closest) -----

    /// <summary>
    /// Sweeps a circle along a direction and returns the closest hit.
    /// </summary>
    /// <param name="origin">Center of the circle at the start of the sweep in pixel coordinates.</param>
    /// <param name="radius">Circle radius in pixels.</param>
    /// <param name="direction">Sweep direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum sweep distance in pixels.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> is zero.</exception>
    public ShapeCastHit? ShapeCastClosest(Vector2 origin, float radius, Vector2 direction, float maxDistance, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var center = new B2.Vec2 { x = origin.X, y = origin.Y };
        var proxy = B2.MakeProxy(&center, 1, radius);
        return ShapeCastClosestCore(&proxy, direction, maxDistance, filter);
    }

    /// <summary>
    /// Sweeps a capsule along a direction and returns the closest hit.
    /// </summary>
    /// <param name="center1">First capsule center point at the start of the sweep in pixel coordinates.</param>
    /// <param name="center2">Second capsule center point at the start of the sweep in pixel coordinates.</param>
    /// <param name="radius">Capsule radius in pixels. Must be greater than zero.</param>
    /// <param name="direction">Sweep direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum sweep distance in pixels.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> is zero.</exception>
    public ShapeCastHit? ShapeCastClosest(Vector2 center1, Vector2 center2, float radius,
        Vector2 direction, float maxDistance, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var pts = stackalloc B2.Vec2[2];
        pts[0] = new B2.Vec2 { x = center1.X, y = center1.Y };
        pts[1] = new B2.Vec2 { x = center2.X, y = center2.Y };
        var proxy = B2.MakeProxy(pts, 2, radius);
        return ShapeCastClosestCore(&proxy, direction, maxDistance, filter);
    }

    /// <summary>
    /// Sweeps an oriented box along a direction and returns the closest hit.
    /// </summary>
    /// <param name="origin">Center of the box at the start of the sweep in pixel coordinates.</param>
    /// <param name="halfWidth">Half-width of the box in pixels.</param>
    /// <param name="halfHeight">Half-height of the box in pixels.</param>
    /// <param name="angle">Rotation of the box in radians.</param>
    /// <param name="direction">Sweep direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum sweep distance in pixels.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> is zero.</exception>
    public ShapeCastHit? ShapeCastClosest(Vector2 origin, float halfWidth, float halfHeight, float angle,
        Vector2 direction, float maxDistance, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var center = new B2.Vec2 { x = origin.X, y = origin.Y };
        var box = B2.MakeOffsetBox(halfWidth, halfHeight, center, B2.MakeRot(angle));
        var corners = stackalloc B2.Vec2[4];
        corners[0] = box.vertices[0];
        corners[1] = box.vertices[1];
        corners[2] = box.vertices[2];
        corners[3] = box.vertices[3];
        var proxy = B2.MakeProxy(corners, 4, 0f);
        return ShapeCastClosestCore(&proxy, direction, maxDistance, filter);
    }

    /// <summary>
    /// Sweeps a convex polygon along a direction and returns the closest hit.
    /// Vertices are in world space at the start of the sweep.
    /// </summary>
    /// <param name="vertices">Convex polygon vertices in world space (3–8 vertices).</param>
    /// <param name="direction">Sweep direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum sweep distance in pixels.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> is zero.</exception>
    public ShapeCastHit? ShapeCastClosest(ReadOnlySpan<Vector2> vertices, Vector2 direction, float maxDistance, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (vertices.Length < 3 || vertices.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(vertices), "Polygon shape cast requires 3-8 vertices.");

        var b2Verts = stackalloc B2.Vec2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            b2Verts[i] = new B2.Vec2 { x = vertices[i].X, y = vertices[i].Y };

        var proxy = B2.MakeProxy(b2Verts, vertices.Length, 0f);
        return ShapeCastClosestCore(&proxy, direction, maxDistance, filter);
    }

    private ShapeCastHit? ShapeCastClosestCore(B2.ShapeProxy* proxy, Vector2 direction, float maxDistance, PhysicsQueryFilter? filter)
    {
        var norm = NormalizeDirection(direction);
        var translation = new B2.Vec2 { x = norm.X * maxDistance, y = norm.Y * maxDistance };
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        var ctx = new ShapeCastClosestContext { HasHit = false, ClosestFraction = float.MaxValue };
        B2.WorldCastShape(_worldId, proxy, translation, f, &ShapeCastClosestCallback, &ctx);

        if (!ctx.HasHit)
            return null;

        var hit = ResolveHitFromShapeId(ctx.ShapeId);
        return new ShapeCastHit
        {
            Point = new Vector2(ctx.Point.x, ctx.Point.y),
            Normal = new Vector2(ctx.Normal.x, ctx.Normal.y),
            Fraction = ctx.ClosestFraction,
            Distance = ctx.ClosestFraction * maxDistance,
            ShapeId = ctx.ShapeId,
            Component = hit.Component,
            SubShape = hit.SubShape
        };
    }

    // ----- Shape casting (all, deduplicated by body) -----

    /// <summary>
    /// Sweeps a circle along a direction and returns all hits sorted by distance, deduplicated by
    /// body (one entry per body, keeping the closest hit per body). Use <see cref="ShapeCastAllShapes"/>
    /// when per-shape granularity is needed on compound bodies.
    /// </summary>
    /// <param name="origin">Center of the circle at the start of the sweep in pixel coordinates.</param>
    /// <param name="radius">Circle radius in pixels.</param>
    /// <param name="direction">Sweep direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum sweep distance in pixels.</param>
    /// <param name="results">Buffer to receive hits, sorted nearest-first.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when more shapes were hit than the internal buffer could hold.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of hits written to <paramref name="results"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> is zero.</exception>
    public int ShapeCastAll(Vector2 origin, float radius, Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        wasTruncated = false;

        if (results.Length == 0)
            return 0;

        var center = new B2.Vec2 { x = origin.X, y = origin.Y };
        var proxy = B2.MakeProxy(&center, 1, radius);
        return ShapeCastAllCore(&proxy, direction, maxDistance, results, out wasTruncated, filter);
    }

    /// <inheritdoc cref="ShapeCastAll(Vector2,float,Vector2,float,Span{ShapeCastHit},out bool,PhysicsQueryFilter?)"/>
    public int ShapeCastAll(Vector2 origin, float radius, Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
        => ShapeCastAll(origin, radius, direction, maxDistance, results, out _, filter);

    /// <summary>
    /// Sweeps a capsule along a direction and returns all hits sorted by distance, deduplicated by body.
    /// </summary>
    /// <param name="center1">First capsule center point at the start of the sweep in pixel coordinates.</param>
    /// <param name="center2">Second capsule center point at the start of the sweep in pixel coordinates.</param>
    /// <param name="radius">Capsule radius in pixels.</param>
    /// <param name="direction">Sweep direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum sweep distance in pixels.</param>
    /// <param name="results">Buffer to receive hits, sorted nearest-first.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when more shapes were hit than the internal buffer could hold.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of hits written to <paramref name="results"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> is zero.</exception>
    public int ShapeCastAll(Vector2 center1, Vector2 center2, float radius,
        Vector2 direction, float maxDistance, Span<ShapeCastHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        wasTruncated = false;

        if (results.Length == 0)
            return 0;

        var pts = stackalloc B2.Vec2[2];
        pts[0] = new B2.Vec2 { x = center1.X, y = center1.Y };
        pts[1] = new B2.Vec2 { x = center2.X, y = center2.Y };
        var proxy = B2.MakeProxy(pts, 2, radius);
        return ShapeCastAllCore(&proxy, direction, maxDistance, results, out wasTruncated, filter);
    }

    /// <inheritdoc cref="ShapeCastAll(Vector2,Vector2,float,Vector2,float,Span{ShapeCastHit},out bool,PhysicsQueryFilter?)"/>
    public int ShapeCastAll(Vector2 center1, Vector2 center2, float radius,
        Vector2 direction, float maxDistance, Span<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
        => ShapeCastAll(center1, center2, radius, direction, maxDistance, results, out _, filter);

    /// <summary>
    /// Sweeps an oriented box along a direction and returns all hits sorted by distance, deduplicated by body.
    /// </summary>
    /// <param name="origin">Center of the box at the start of the sweep in pixel coordinates.</param>
    /// <param name="halfWidth">Half-width of the box in pixels.</param>
    /// <param name="halfHeight">Half-height of the box in pixels.</param>
    /// <param name="angle">Rotation of the box in radians.</param>
    /// <param name="direction">Sweep direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum sweep distance in pixels.</param>
    /// <param name="results">Buffer to receive hits, sorted nearest-first.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when more shapes were hit than the internal buffer could hold.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of hits written to <paramref name="results"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> is zero.</exception>
    public int ShapeCastAll(Vector2 origin, float halfWidth, float halfHeight, float angle,
        Vector2 direction, float maxDistance, Span<ShapeCastHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        wasTruncated = false;

        if (results.Length == 0)
            return 0;

        var center = new B2.Vec2 { x = origin.X, y = origin.Y };
        var box = B2.MakeOffsetBox(halfWidth, halfHeight, center, B2.MakeRot(angle));
        var corners = stackalloc B2.Vec2[4];
        corners[0] = box.vertices[0];
        corners[1] = box.vertices[1];
        corners[2] = box.vertices[2];
        corners[3] = box.vertices[3];
        var proxy = B2.MakeProxy(corners, 4, 0f);
        return ShapeCastAllCore(&proxy, direction, maxDistance, results, out wasTruncated, filter);
    }

    /// <inheritdoc cref="ShapeCastAll(Vector2,float,float,float,Vector2,float,Span{ShapeCastHit},out bool,PhysicsQueryFilter?)"/>
    public int ShapeCastAll(Vector2 origin, float halfWidth, float halfHeight, float angle,
        Vector2 direction, float maxDistance, Span<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
        => ShapeCastAll(origin, halfWidth, halfHeight, angle, direction, maxDistance, results, out _, filter);

    private int ShapeCastAllCore(B2.ShapeProxy* proxy, Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, out bool wasTruncated, PhysicsQueryFilter? filter)
    {
        var norm = NormalizeDirection(direction);
        var translation = new B2.Vec2 { x = norm.X * maxDistance, y = norm.Y * maxDistance };
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        int rawCapacity = Math.Max(results.Length * 8, 16);
        var rawArray = ArrayPool<ShapeCastHit>.Shared.Rent(rawCapacity);
        try
        {
            fixed (ShapeCastHit* ptr = rawArray)
            {
                var ctx = new ShapeCastAllContext { Buffer = ptr, Capacity = rawCapacity, Count = 0 };
                B2.WorldCastShape(_worldId, proxy, translation, f, &ShapeCastAllCallback, &ctx);
                int raw = ctx.Count;

                wasTruncated = raw >= rawCapacity;

                new Span<ShapeCastHit>(ptr, raw).Sort((a, b) => a.Fraction.CompareTo(b.Fraction));

                int deduped = DeduplicateShapeCastByBody(ptr, raw);
                int written = Math.Min(deduped, results.Length);
                new ReadOnlySpan<ShapeCastHit>(ptr, written).CopyTo(results);

                for (int i = 0; i < written; i++)
                    results[i] = results[i] with { Distance = results[i].Fraction * maxDistance };

                return written;
            }
        }
        finally
        {
            ArrayPool<ShapeCastHit>.Shared.Return(rawArray, clearArray: true);
        }
    }

    /// <summary>
    /// Sweeps a convex polygon along a direction and returns all hits sorted by distance,
    /// without deduplicating by body. Vertices are in world space at the start position.
    /// </summary>
    /// <param name="vertices">Convex polygon vertices in world space (3–8 vertices).</param>
    /// <param name="direction">Sweep direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum sweep distance in pixels.</param>
    /// <param name="results">Buffer to receive hits, sorted nearest-first.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when more shapes were hit than the internal buffer could hold.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of hits written to <paramref name="results"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> is zero.</exception>
    public int ShapeCastAllShapes(ReadOnlySpan<Vector2> vertices, Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
        {
            wasTruncated = false;
            return 0;
        }

        if (vertices.Length < 3 || vertices.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(vertices), "Polygon shape cast requires 3-8 vertices.");

        var b2Verts = stackalloc B2.Vec2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            b2Verts[i] = new B2.Vec2 { x = vertices[i].X, y = vertices[i].Y };

        var proxy = B2.MakeProxy(b2Verts, vertices.Length, 0f);
        return ShapeCastAllShapesCore(&proxy, direction, maxDistance, results, out wasTruncated, filter);
    }

    /// <inheritdoc cref="ShapeCastAllShapes(ReadOnlySpan{Vector2},Vector2,float,Span{ShapeCastHit},out bool,PhysicsQueryFilter?)"/>
    public int ShapeCastAllShapes(ReadOnlySpan<Vector2> vertices, Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
        => ShapeCastAllShapes(vertices, direction, maxDistance, results, out _, filter);

    private int ShapeCastAllShapesCore(B2.ShapeProxy* proxy, Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, out bool wasTruncated, PhysicsQueryFilter? filter)
    {
        var norm = NormalizeDirection(direction);
        var translation = new B2.Vec2 { x = norm.X * maxDistance, y = norm.Y * maxDistance };
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        int rawCapacity = Math.Max(results.Length, 16);
        var rawArray = ArrayPool<ShapeCastHit>.Shared.Rent(rawCapacity);
        try
        {
            fixed (ShapeCastHit* ptr = rawArray)
            {
                var ctx = new ShapeCastAllContext { Buffer = ptr, Capacity = rawCapacity, Count = 0 };
                B2.WorldCastShape(_worldId, proxy, translation, f, &ShapeCastAllCallback, &ctx);
                int raw = ctx.Count;

                wasTruncated = raw >= rawCapacity;

                new Span<ShapeCastHit>(ptr, raw).Sort((a, b) => a.Fraction.CompareTo(b.Fraction));

                int written = Math.Min(raw, results.Length);
                for (int i = 0; i < written; i++)
                {
                    var hit = ResolveHitFromShapeId(ptr[i].ShapeId);
                    results[i] = ptr[i] with
                    {
                        Component = hit.Component,
                        SubShape = hit.SubShape,
                        Distance = ptr[i].Fraction * maxDistance
                    };
                }

                return written;
            }
        }
        finally
        {
            ArrayPool<ShapeCastHit>.Shared.Return(rawArray, clearArray: true);
        }
    }

    // ----- Overlap queries -----

    /// <summary>
    /// Queries all shapes overlapping the given axis-aligned bounding box.
    /// </summary>
    public int OverlapAABB(Vector2 min, Vector2 max, Span<PhysicsBodyComponent> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
            return 0;

        var aabb = new B2.AABB
        {
            lowerBound = new B2.Vec2 { x = min.X, y = min.Y },
            upperBound = new B2.Vec2 { x = max.X, y = max.Y }
        };
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        int rawCapacity = Math.Max(results.Length * 8, 16);
        var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(rawCapacity);
        try
        {
            int shapeCount;
            fixed (B2.ShapeId* ptr = shapeIds)
            {
                var ctx = new OverlapContext { Buffer = ptr, Capacity = rawCapacity, Count = 0 };
                B2.WorldOverlapAABB(_worldId, aabb, f, &OverlapCallback, &ctx);
                shapeCount = ctx.Count;
            }
            return ResolveShapeIds(shapeIds, shapeCount, results);
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
        }
    }

    /// <summary>
    /// Queries all shapes overlapping the given axis-aligned bounding box, returning one
    /// <see cref="OverlapHit"/> per shape rather than per body.
    /// </summary>
    public int OverlapAABBShapes(Vector2 min, Vector2 max, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
            return 0;

        var aabb = new B2.AABB
        {
            lowerBound = new B2.Vec2 { x = min.X, y = min.Y },
            upperBound = new B2.Vec2 { x = max.X, y = max.Y }
        };
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(results.Length);
        try
        {
            int shapeCount;
            fixed (B2.ShapeId* ptr = shapeIds)
            {
                var ctx = new OverlapContext { Buffer = ptr, Capacity = results.Length, Count = 0 };
                B2.WorldOverlapAABB(_worldId, aabb, f, &OverlapCallback, &ctx);
                shapeCount = ctx.Count;
            }
            return ResolveShapeIdsToHits(shapeIds, shapeCount, results);
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
        }
    }

    /// <summary>
    /// Queries all shapes overlapping a circle.
    /// </summary>
    public int OverlapCircle(Vector2 center, float radius, Span<PhysicsBodyComponent> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
            return 0;

        var point = new B2.Vec2 { x = center.X, y = center.Y };
        var proxy = B2.MakeProxy(&point, 1, radius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        int rawCapacity = Math.Max(results.Length * 8, 16);
        var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(rawCapacity);
        try
        {
            int shapeCount;
            fixed (B2.ShapeId* ptr = shapeIds)
            {
                var ctx = new OverlapContext { Buffer = ptr, Capacity = rawCapacity, Count = 0 };
                B2.WorldOverlapShape(_worldId, &proxy, f, &OverlapCallback, &ctx);
                shapeCount = ctx.Count;
            }
            return ResolveShapeIds(shapeIds, shapeCount, results);
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
        }
    }

    /// <summary>
    /// Queries all shapes overlapping a circle, returning one <see cref="OverlapHit"/> per shape.
    /// </summary>
    public int OverlapCircleShapes(Vector2 center, float radius, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
            return 0;

        var point = new B2.Vec2 { x = center.X, y = center.Y };
        var proxy = B2.MakeProxy(&point, 1, radius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(results.Length);
        try
        {
            int shapeCount;
            fixed (B2.ShapeId* ptr = shapeIds)
            {
                var ctx = new OverlapContext { Buffer = ptr, Capacity = results.Length, Count = 0 };
                B2.WorldOverlapShape(_worldId, &proxy, f, &OverlapCallback, &ctx);
                shapeCount = ctx.Count;
            }
            return ResolveShapeIdsToHits(shapeIds, shapeCount, results);
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
        }
    }

    /// <summary>
    /// Queries all shapes overlapping a capsule.
    /// </summary>
    public int OverlapCapsule(Vector2 center1, Vector2 center2, float radius,
        Span<PhysicsBodyComponent> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
            return 0;

        var pts = stackalloc B2.Vec2[2];
        pts[0] = new B2.Vec2 { x = center1.X, y = center1.Y };
        pts[1] = new B2.Vec2 { x = center2.X, y = center2.Y };
        var proxy = B2.MakeProxy(pts, 2, radius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        int rawCapacity = Math.Max(results.Length * 8, 16);
        var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(rawCapacity);
        try
        {
            int shapeCount;
            fixed (B2.ShapeId* ptr = shapeIds)
            {
                var ctx = new OverlapContext { Buffer = ptr, Capacity = rawCapacity, Count = 0 };
                B2.WorldOverlapShape(_worldId, &proxy, f, &OverlapCallback, &ctx);
                shapeCount = ctx.Count;
            }
            return ResolveShapeIds(shapeIds, shapeCount, results);
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
        }
    }

    /// <summary>
    /// Queries all shapes overlapping a capsule, returning one <see cref="OverlapHit"/> per shape.
    /// </summary>
    public int OverlapCapsuleShapes(Vector2 center1, Vector2 center2, float radius,
        Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
            return 0;

        var pts = stackalloc B2.Vec2[2];
        pts[0] = new B2.Vec2 { x = center1.X, y = center1.Y };
        pts[1] = new B2.Vec2 { x = center2.X, y = center2.Y };
        var proxy = B2.MakeProxy(pts, 2, radius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(results.Length);
        try
        {
            int shapeCount;
            fixed (B2.ShapeId* ptr = shapeIds)
            {
                var ctx = new OverlapContext { Buffer = ptr, Capacity = results.Length, Count = 0 };
                B2.WorldOverlapShape(_worldId, &proxy, f, &OverlapCallback, &ctx);
                shapeCount = ctx.Count;
            }
            return ResolveShapeIdsToHits(shapeIds, shapeCount, results);
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
        }
    }

    /// <summary>
    /// Queries all shapes overlapping an oriented box.
    /// </summary>
    public int OverlapBox(Vector2 center, float halfWidth, float halfHeight, float angle,
        Span<PhysicsBodyComponent> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
            return 0;

        var b2Center = new B2.Vec2 { x = center.X, y = center.Y };
        var box = B2.MakeOffsetBox(halfWidth, halfHeight, b2Center, B2.MakeRot(angle));
        var corners = stackalloc B2.Vec2[4];
        corners[0] = box.vertices[0];
        corners[1] = box.vertices[1];
        corners[2] = box.vertices[2];
        corners[3] = box.vertices[3];
        var proxy = B2.MakeProxy(corners, 4, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        int rawCapacity = Math.Max(results.Length * 8, 16);
        var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(rawCapacity);
        try
        {
            int shapeCount;
            fixed (B2.ShapeId* ptr = shapeIds)
            {
                var ctx = new OverlapContext { Buffer = ptr, Capacity = rawCapacity, Count = 0 };
                B2.WorldOverlapShape(_worldId, &proxy, f, &OverlapCallback, &ctx);
                shapeCount = ctx.Count;
            }
            return ResolveShapeIds(shapeIds, shapeCount, results);
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
        }
    }

    /// <summary>
    /// Queries all shapes overlapping an oriented box, returning one <see cref="OverlapHit"/> per shape.
    /// </summary>
    public int OverlapBoxShapes(Vector2 center, float halfWidth, float halfHeight, float angle,
        Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
            return 0;

        var b2Center = new B2.Vec2 { x = center.X, y = center.Y };
        var box = B2.MakeOffsetBox(halfWidth, halfHeight, b2Center, B2.MakeRot(angle));
        var corners = stackalloc B2.Vec2[4];
        corners[0] = box.vertices[0];
        corners[1] = box.vertices[1];
        corners[2] = box.vertices[2];
        corners[3] = box.vertices[3];
        var proxy = B2.MakeProxy(corners, 4, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(results.Length);
        try
        {
            int shapeCount;
            fixed (B2.ShapeId* ptr = shapeIds)
            {
                var ctx = new OverlapContext { Buffer = ptr, Capacity = results.Length, Count = 0 };
                B2.WorldOverlapShape(_worldId, &proxy, f, &OverlapCallback, &ctx);
                shapeCount = ctx.Count;
            }
            return ResolveShapeIdsToHits(shapeIds, shapeCount, results);
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
        }
    }

    /// <summary>
    /// Queries all shapes overlapping a convex polygon. Vertices are in world space.
    /// </summary>
    public int OverlapPolygon(ReadOnlySpan<Vector2> vertices, Span<PhysicsBodyComponent> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
            return 0;

        if (vertices.Length < 3 || vertices.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(vertices), "Polygon overlap requires 3-8 vertices.");

        var b2Verts = stackalloc B2.Vec2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            b2Verts[i] = new B2.Vec2 { x = vertices[i].X, y = vertices[i].Y };

        var proxy = B2.MakeProxy(b2Verts, vertices.Length, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        int rawCapacity = Math.Max(results.Length * 8, 16);
        var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(rawCapacity);
        try
        {
            int shapeCount;
            fixed (B2.ShapeId* ptr = shapeIds)
            {
                var ctx = new OverlapContext { Buffer = ptr, Capacity = rawCapacity, Count = 0 };
                B2.WorldOverlapShape(_worldId, &proxy, f, &OverlapCallback, &ctx);
                shapeCount = ctx.Count;
            }
            return ResolveShapeIds(shapeIds, shapeCount, results);
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
        }
    }

    /// <summary>
    /// Queries all shapes overlapping a convex polygon, returning one <see cref="OverlapHit"/> per shape.
    /// </summary>
    public int OverlapPolygonShapes(ReadOnlySpan<Vector2> vertices, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
            return 0;

        if (vertices.Length < 3 || vertices.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(vertices), "Polygon overlap requires 3-8 vertices.");

        var b2Verts = stackalloc B2.Vec2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            b2Verts[i] = new B2.Vec2 { x = vertices[i].X, y = vertices[i].Y };

        var proxy = B2.MakeProxy(b2Verts, vertices.Length, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(results.Length);
        try
        {
            int shapeCount;
            fixed (B2.ShapeId* ptr = shapeIds)
            {
                var ctx = new OverlapContext { Buffer = ptr, Capacity = results.Length, Count = 0 };
                B2.WorldOverlapShape(_worldId, &proxy, f, &OverlapCallback, &ctx);
                shapeCount = ctx.Count;
            }
            return ResolveShapeIdsToHits(shapeIds, shapeCount, results);
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
        }
    }

    /// <summary>
    /// Queries all shapes whose area contains the given point.
    /// </summary>
    public int OverlapPoint(Vector2 point, Span<PhysicsBodyComponent> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
            return 0;

        var b2Point = new B2.Vec2 { x = point.X, y = point.Y };
        var proxy = B2.MakeProxy(&b2Point, 1, PointQueryRadius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        int rawCapacity = Math.Max(results.Length * 8, 16);
        var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(rawCapacity);
        try
        {
            int shapeCount;
            fixed (B2.ShapeId* ptr = shapeIds)
            {
                var ctx = new OverlapContext { Buffer = ptr, Capacity = rawCapacity, Count = 0 };
                B2.WorldOverlapShape(_worldId, &proxy, f, &OverlapCallback, &ctx);
                shapeCount = ctx.Count;
            }
            return ResolveShapeIds(shapeIds, shapeCount, results);
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
        }
    }

    /// <summary>
    /// Queries all shapes whose area contains the given point, returning one
    /// <see cref="OverlapHit"/> per shape rather than per body.
    /// </summary>
    public int OverlapPointShapes(Vector2 point, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
            return 0;

        var b2Point = new B2.Vec2 { x = point.X, y = point.Y };
        var proxy = B2.MakeProxy(&b2Point, 1, PointQueryRadius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(results.Length);
        try
        {
            int shapeCount;
            fixed (B2.ShapeId* ptr = shapeIds)
            {
                var ctx = new OverlapContext { Buffer = ptr, Capacity = results.Length, Count = 0 };
                B2.WorldOverlapShape(_worldId, &proxy, f, &OverlapCallback, &ctx);
                shapeCount = ctx.Count;
            }
            return ResolveShapeIdsToHits(shapeIds, shapeCount, results);
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
        }
    }

    /// <summary>
    /// Returns the first <see cref="PhysicsBodyComponent"/> whose shape contains
    /// <paramref name="point"/>, or <c>null</c> if nothing overlaps.
    /// Stops the Box2D query after the first hit — no heap allocation.
    /// </summary>
    public PhysicsBodyComponent? OverlapPointFirst(Vector2 point, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var b2Point = new B2.Vec2 { x = point.X, y = point.Y };
        var proxy = B2.MakeProxy(&b2Point, 1, PointQueryRadius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        var ctx = new OverlapFirstContext { ShapeId = default, Found = false };
        B2.WorldOverlapShape(_worldId, &proxy, f, &OverlapFirstCallback, &ctx);

        return ctx.Found ? ResolveComponent(ctx.ShapeId) : null;
    }

    /// <summary>
    /// Returns the first <see cref="OverlapHit"/> whose shape contains <paramref name="point"/>,
    /// or <c>null</c> if nothing overlaps. Stops the Box2D query after the first hit — no heap
    /// allocation. Returns the full <see cref="OverlapHit"/> so callers can identify the specific
    /// sub-shape on a compound body (e.g. mouse picking).
    /// </summary>
    public OverlapHit? OverlapPointFirstHit(Vector2 point, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var b2Point = new B2.Vec2 { x = point.X, y = point.Y };
        var proxy = B2.MakeProxy(&b2Point, 1, PointQueryRadius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        var ctx = new OverlapFirstContext { ShapeId = default, Found = false };
        B2.WorldOverlapShape(_worldId, &proxy, f, &OverlapFirstCallback, &ctx);

        if (!ctx.Found)
            return null;

        return ResolveShapeIdToHit(ctx.ShapeId);
    }

    /// <summary>
    /// Returns the first <see cref="PhysicsBodyComponent"/> overlapping the given circle,
    /// or <c>null</c> if nothing overlaps. Stops the Box2D query after the first hit.
    /// </summary>
    public PhysicsBodyComponent? OverlapCircleFirst(Vector2 center, float radius, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var point = new B2.Vec2 { x = center.X, y = center.Y };
        var proxy = B2.MakeProxy(&point, 1, radius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        var ctx = new OverlapFirstContext { ShapeId = default, Found = false };
        B2.WorldOverlapShape(_worldId, &proxy, f, &OverlapFirstCallback, &ctx);

        return ctx.Found ? ResolveComponent(ctx.ShapeId) : null;
    }

    /// <summary>
    /// Returns the first <see cref="OverlapHit"/> whose shape overlaps the given circle,
    /// or <c>null</c> if nothing overlaps. Stops the Box2D query after the first hit.
    /// </summary>
    public OverlapHit? OverlapCircleFirstHit(Vector2 center, float radius, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var point = new B2.Vec2 { x = center.X, y = center.Y };
        var proxy = B2.MakeProxy(&point, 1, radius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        var ctx = new OverlapFirstContext { ShapeId = default, Found = false };
        B2.WorldOverlapShape(_worldId, &proxy, f, &OverlapFirstCallback, &ctx);

        return ctx.Found ? ResolveShapeIdToHit(ctx.ShapeId) : null;
    }

    /// <summary>
    /// Returns the first <see cref="PhysicsBodyComponent"/> overlapping the given capsule,
    /// or <c>null</c> if nothing overlaps. Stops the Box2D query after the first hit.
    /// </summary>
    public PhysicsBodyComponent? OverlapCapsuleFirst(Vector2 center1, Vector2 center2, float radius, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var pts = stackalloc B2.Vec2[2];
        pts[0] = new B2.Vec2 { x = center1.X, y = center1.Y };
        pts[1] = new B2.Vec2 { x = center2.X, y = center2.Y };
        var proxy = B2.MakeProxy(pts, 2, radius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        var ctx = new OverlapFirstContext { ShapeId = default, Found = false };
        B2.WorldOverlapShape(_worldId, &proxy, f, &OverlapFirstCallback, &ctx);

        return ctx.Found ? ResolveComponent(ctx.ShapeId) : null;
    }

    /// <summary>
    /// Returns the first <see cref="OverlapHit"/> whose shape overlaps the given capsule,
    /// or <c>null</c> if nothing overlaps. Stops the Box2D query after the first hit.
    /// </summary>
    public OverlapHit? OverlapCapsuleFirstHit(Vector2 center1, Vector2 center2, float radius, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var pts = stackalloc B2.Vec2[2];
        pts[0] = new B2.Vec2 { x = center1.X, y = center1.Y };
        pts[1] = new B2.Vec2 { x = center2.X, y = center2.Y };
        var proxy = B2.MakeProxy(pts, 2, radius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        var ctx = new OverlapFirstContext { ShapeId = default, Found = false };
        B2.WorldOverlapShape(_worldId, &proxy, f, &OverlapFirstCallback, &ctx);

        return ctx.Found ? ResolveShapeIdToHit(ctx.ShapeId) : null;
    }

    /// <summary>
    /// Returns the first <see cref="PhysicsBodyComponent"/> overlapping the given oriented box,
    /// or <c>null</c> if nothing overlaps. Stops the Box2D query after the first hit.
    /// </summary>
    public PhysicsBodyComponent? OverlapBoxFirst(Vector2 center, float halfWidth, float halfHeight, float angle, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var b2Center = new B2.Vec2 { x = center.X, y = center.Y };
        var box = B2.MakeOffsetBox(halfWidth, halfHeight, b2Center, B2.MakeRot(angle));
        var corners = stackalloc B2.Vec2[4];
        corners[0] = box.vertices[0];
        corners[1] = box.vertices[1];
        corners[2] = box.vertices[2];
        corners[3] = box.vertices[3];
        var proxy = B2.MakeProxy(corners, 4, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        var ctx = new OverlapFirstContext { ShapeId = default, Found = false };
        B2.WorldOverlapShape(_worldId, &proxy, f, &OverlapFirstCallback, &ctx);

        return ctx.Found ? ResolveComponent(ctx.ShapeId) : null;
    }

    /// <summary>
    /// Returns the first <see cref="OverlapHit"/> whose shape overlaps the given oriented box,
    /// or <c>null</c> if nothing overlaps. Stops the Box2D query after the first hit.
    /// </summary>
    public OverlapHit? OverlapBoxFirstHit(Vector2 center, float halfWidth, float halfHeight, float angle, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        var b2Center = new B2.Vec2 { x = center.X, y = center.Y };
        var box = B2.MakeOffsetBox(halfWidth, halfHeight, b2Center, B2.MakeRot(angle));
        var corners = stackalloc B2.Vec2[4];
        corners[0] = box.vertices[0];
        corners[1] = box.vertices[1];
        corners[2] = box.vertices[2];
        corners[3] = box.vertices[3];
        var proxy = B2.MakeProxy(corners, 4, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        var ctx = new OverlapFirstContext { ShapeId = default, Found = false };
        B2.WorldOverlapShape(_worldId, &proxy, f, &OverlapFirstCallback, &ctx);

        return ctx.Found ? ResolveShapeIdToHit(ctx.ShapeId) : null;
    }

    // ----- Contact queries -----

    /// <summary>
    /// Returns all currently active contacts involving <paramref name="body"/>, written into the
    /// caller-provided buffer. Each entry contains the other body and the contact manifold data,
    /// with the normal oriented away from <paramref name="body"/>.
    /// Returns 0 if the body has not yet been created by the physics system.
    /// </summary>
    /// <param name="body">The body to query contacts for.</param>
    /// <param name="results">Buffer to receive contact pairs.</param    /// <param name="maxContacts">
    /// Maximum number of raw contacts to sample from Box2D. Defaults to 128.
    /// Increase this for bodies that may simultaneously touch many shapes.
    /// </param>
    /// <returns>Number of contacts written to <paramref name="results"/>.</returns>
    /// <remarks>
    /// Use the <see cref="GetContacts(PhysicsBodyComponent, Span{ContactPair}, out bool, int)"/>
    /// overload to detect truncation.
    /// </remarks>
    public int GetContacts(PhysicsBodyComponent body, Span<ContactPair> results, int maxContacts = 128)
        => GetContacts(body, results, out _, maxContacts);

    /// <summary>
    /// Returns all currently active contacts involving <paramref name="body"/>, written into the
    /// caller-provided buffer. Sets <paramref name="wasTruncated"/> to <c>true</c> when Box2D
    /// returned more contacts than the cap, meaning some active contacts were not returned.
    /// </summary>
    public int GetContacts(PhysicsBodyComponent body, Span<ContactPair> results, out bool wasTruncated, int maxContacts = 128)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxContacts, 1);

        wasTruncated = false;

        if (!B2.BodyIsValid(body.BodyId) || results.Length == 0)
            return 0;

        var raw = ArrayPool<B2.ContactData>.Shared.Rent(maxContacts);
        try
        {
            int count;
            fixed (B2.ContactData* ptr = raw)
                count = B2.BodyGetContactData(body.BodyId, ptr, maxContacts);

            wasTruncated = count == maxContacts || count > results.Length;
            int written = Math.Min(count, results.Length);

            nint selfIndex = body.BodyId.index1;
            for (int i = 0; i < written; i++)
            {
                ref var c = ref raw[i];

                var bodyIndexA = B2.ShapeGetBody(c.shapeIdA).index1;
                var bodyIndexB = B2.ShapeGetBody(c.shapeIdB).index1;

                bool selfIsA = bodyIndexA == selfIndex;
                nint otherBodyIndex = selfIsA ? bodyIndexB : bodyIndexA;

                var other = ComponentResolver?.Invoke(otherBodyIndex);
                var contact = CollisionContact.FromManifold(c.manifold);

                if (!selfIsA)
                    contact = contact with { Normal = -contact.Normal };

                SubShape? selfSubShape = ResolveSubShapeOnBody(body, selfIsA ? c.shapeIdA : c.shapeIdB);
                SubShape? otherSubShape = other != null
                    ? ResolveSubShapeOnBody(other, selfIsA ? c.shapeIdB : c.shapeIdA)
                    : null;

                results[i] = new ContactPair
                {
                    Other = other,
                    Contact = contact,
                    SelfSubShape = selfSubShape,
                    OtherSubShape = otherSubShape
                };
            }

            return written;
        }
        finally
        {
            ArrayPool<B2.ContactData>.Shared.Return(raw);
        }
    }

    /// <summary>
    /// Returns the first active contact between <paramref name="bodyA"/> and
    /// <paramref name="bodyB"/>, with the normal oriented away from <paramref name="bodyA"/>.
    /// Returns <c>null</c> if the bodies are not currently in contact or either body is invalid.
    /// </summary>
    /// <param name="bodyA">The reference body; the contact normal points away from it.</param>
    /// <param name="bodyB">The body to test contact against.</param>
    /// <param name="maxContacts">
    /// Maximum number of raw contacts to sample from Box2D. Defaults to 128.
    /// Increase for bodies touching many shapes simultaneously.
    /// </param>
    public ContactPair? GetContact(PhysicsBodyComponent bodyA, PhysicsBodyComponent bodyB, int maxContacts = 128)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxContacts, 1);

        if (!B2.BodyIsValid(bodyA.BodyId) || !B2.BodyIsValid(bodyB.BodyId))
            return null;

        var raw = ArrayPool<B2.ContactData>.Shared.Rent(maxContacts);
        try
        {
            int count;
            fixed (B2.ContactData* ptr = raw)
                count = B2.BodyGetContactData(bodyA.BodyId, ptr, maxContacts);

            nint selfIndex = bodyA.BodyId.index1;
            nint targetIndex = bodyB.BodyId.index1;

            for (int i = 0; i < count; i++)
            {
                ref var c = ref raw[i];

                var idxA = B2.ShapeGetBody(c.shapeIdA).index1;
                var idxB = B2.ShapeGetBody(c.shapeIdB).index1;

                bool selfIsA = idxA == selfIndex;
                nint otherIndex = selfIsA ? idxB : idxA;

                if (otherIndex != targetIndex)
                    continue;

                var contact = CollisionContact.FromManifold(c.manifold);
                if (!selfIsA)
                    contact = contact with { Normal = -contact.Normal };

                return new ContactPair
                {
                    Other = bodyB,
                    Contact = contact,
                    SelfSubShape = ResolveSubShapeOnBody(bodyA, selfIsA ? c.shapeIdA : c.shapeIdB),
                    OtherSubShape = ResolveSubShapeOnBody(bodyB, selfIsA ? c.shapeIdB : c.shapeIdA)
                };
            }

            return null;
        }
        finally
        {
            ArrayPool<B2.ContactData>.Shared.Return(raw);
        }
    }

    // ----- Collision filtering -----

    /// <summary>
    /// Registers a user-level collision filter invoked for every potential contact pair.
    /// Return <c>false</c> to prevent the pair from colliding. Pass <c>null</c> to remove it.
    /// This filter is evaluated after per-body <see cref="PhysicsBodyComponent.ShouldCollide"/>
    /// and per-sub-shape <see cref="SubShape.ShouldCollide"/> checks — all must pass for a
    /// contact to proceed.
    /// The callback is invoked from the Box2D broad-phase during fixed-update — keep it
    /// allocation-free.
    /// </summary>
    public void SetCustomCollisionFilter(Func<PhysicsBodyComponent, PhysicsBodyComponent, bool>? filter)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _userFilter = filter;
        RebuildFilter();
    }

    // Raw shape IDs are passed so the system can resolve sub-shape predicates without
    // PhysicsWorld needing to know about the SubShape type.
    internal void SetSystemCollisionFilter(Func<B2.ShapeId, B2.ShapeId, bool>? filter)
    {
        _systemFilter = filter;
        RebuildFilter();
    }

    private void RebuildFilter()
    {
        if (_filterHandle.IsAllocated)
            _filterHandle.Free();

        if (_systemFilter == null && _userFilter == null)
        {
            B2.WorldSetCustomFilterCallback(_worldId, null, null);
            return;
        }

        var sys = _systemFilter;
        var usr = _userFilter;

        Func<B2.ShapeId, B2.ShapeId, bool> combined = (shapeA, shapeB) =>
        {
            if (sys != null && !sys(shapeA, shapeB)) return false;
            if (usr != null)
            {
                var compA = ResolveComponent(shapeA);
                var compB = ResolveComponent(shapeB);
                if (compA != null && compB != null && !usr(compA, compB)) return false;
            }
            return true;
        };

        _filterHandle = GCHandle.Alloc(combined);
        B2.WorldSetCustomFilterCallback(_worldId, &CustomFilterCallback, (void*)GCHandle.ToIntPtr(_filterHandle));
    }

    // ----- Joint creation (internal, called by Box2DPhysicsSystem) -----

    internal B2.JointId CreateDistanceJoint(B2.DistanceJointDef* jointDef)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return B2.CreateDistanceJoint(_worldId, jointDef);
    }

    internal B2.JointId CreateRevoluteJoint(B2.RevoluteJointDef* jointDef)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return B2.CreateRevoluteJoint(_worldId, jointDef);
    }

    internal B2.JointId CreatePrismaticJoint(B2.PrismaticJointDef* jointDef)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return B2.CreatePrismaticJoint(_worldId, jointDef);
    }

    internal B2.JointId CreateWeldJoint(B2.WeldJointDef* jointDef)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return B2.CreateWeldJoint(_worldId, jointDef);
    }

    internal B2.JointId CreateMouseJoint(B2.MouseJointDef* jointDef)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return B2.CreateMouseJoint(_worldId, jointDef);
    }

    internal B2.JointId CreateWheelJoint(B2.WheelJointDef* jointDef)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return B2.CreateWheelJoint(_worldId, jointDef);
    }

    internal B2.JointId CreateMotorJoint(B2.MotorJointDef* jointDef)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return B2.CreateMotorJoint(_worldId, jointDef);
    }

    internal void DestroyJoint(B2.JointId jointId)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        B2.DestroyJoint(jointId);
    }

    // ----- Profiling -----

    /// <summary>
    /// Returns a timing breakdown (in milliseconds) for the last Box2D world step.
    /// All values are zero before the first call to <see cref="Step"/>.
    /// </summary>
    public PhysicsWorldProfile GetProfile()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var p = B2.WorldGetProfile(_worldId);
        return new PhysicsWorldProfile
        {
            Step = p.step,
            Pairs = p.pairs,
            Collide = p.collide,
            Solve = p.solve,
            MergeIslands = p.mergeIslands,
            PrepareStages = p.prepareStages,
            SolveConstraints = p.solveConstraints,
            PrepareConstraints = p.prepareConstraints,
            IntegrateVelocities = p.integrateVelocities,
            WarmStart = p.warmStart,
            SolveImpulses = p.solveImpulses,
            IntegratePositions = p.integratePositions,
            RelaxImpulses = p.relaxImpulses,
            ApplyRestitution = p.applyRestitution,
            StoreImpulses = p.storeImpulses,
            SplitIslands = p.splitIslands,
            Transforms = p.transforms,
            HitEvents = p.hitEvents,
            Refit = p.refit,
            Bullets = p.bullets,
            SleepIslands = p.sleepIslands,
            Sensors = p.sensors
        };
    }

    /// <summary>
    /// Returns live simulation counters: body, shape, contact, joint, and island counts.
    /// Useful for debug overlays.
    /// </summary>
    public PhysicsWorldCounters GetCounters()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var c = B2.WorldGetCounters(_worldId);
        return new PhysicsWorldCounters
        {
            BodyCount = c.bodyCount,
            ShapeCount = c.shapeCount,
            ContactCount = c.contactCount,
            JointCount = c.jointCount,
            IslandCount = c.islandCount,
            StackUsed = c.stackUsed,
            StaticTreeHeight = c.staticTreeHeight,
            TreeHeight = c.treeHeight,
            ByteCount = c.byteCount,
            TaskCount = c.taskCount,
            AwakeBodyCount = B2.WorldGetAwakeBodyCount(_worldId)
        };
    }

    // ----- Private helpers -----

    private PhysicsBodyComponent? ResolveComponent(B2.ShapeId shapeId)
    {
        if (ComponentResolver == null) return null;
        return ComponentResolver(B2.ShapeGetBody(shapeId).index1);
    }

    private (PhysicsBodyComponent? Component, SubShape? SubShape) ResolveHitFromShapeId(B2.ShapeId shapeId)
    {
        var comp = ResolveComponent(shapeId);
        if (comp == null)
            return (null, null);

        var sub = ResolveSubShapeOnBody(comp, shapeId);
        return (comp, sub);
    }

    private static SubShape? ResolveSubShapeOnBody(PhysicsBodyComponent body, B2.ShapeId shapeId)
    {
        if (B2.ShapeIsValid(body.ShapeId) && body.ShapeId.index1 == shapeId.index1)
            return null;

        foreach (var sub in body.SubShapes)
        {
            if (B2.ShapeIsValid(sub.ShapeId) && sub.ShapeId.index1 == shapeId.index1)
                return sub;
        }

        return null;
    }

    private int ResolveShapeIds(B2.ShapeId[] shapeIds, int count, Span<PhysicsBodyComponent> results)
    {
        int resolved = 0;
        var seenBuf = ArrayPool<nint>.Shared.Rent(count > 0 ? count : 1);
        int seenCount = 0;
        try
        {
            for (int i = 0; i < count && resolved < results.Length; i++)
            {
                var comp = ResolveComponent(shapeIds[i]);
                if (comp == null) continue;

                var bodyIndex = comp.BodyId.index1;
                bool found = false;
                for (int j = 0; j < seenCount; j++)
                {
                    if (seenBuf[j] == bodyIndex) { found = true; break; }
                }
                if (found) continue;

                seenBuf[seenCount++] = bodyIndex;
                results[resolved++] = comp;
            }
        }
        finally
        {
            ArrayPool<nint>.Shared.Return(seenBuf);
        }
        return resolved;
    }

    private int ResolveShapeIdsToHits(B2.ShapeId[] shapeIds, int count, Span<OverlapHit> results)
    {
        int written = 0;
        for (int i = 0; i < count && written < results.Length; i++)
        {
            var hit = ResolveShapeIdToHit(shapeIds[i]);
            if (hit == null) continue;
            results[written++] = hit.Value;
        }
        return written;
    }

    private OverlapHit? ResolveShapeIdToHit(B2.ShapeId shapeId)
    {
        var comp = ResolveComponent(shapeId);
        if (comp == null) return null;

        if (B2.ShapeIsValid(comp.ShapeId) && comp.ShapeId.index1 == shapeId.index1)
            return new OverlapHit { Component = comp, SubShape = null };

        foreach (var sub in comp.SubShapes)
        {
            if (B2.ShapeIsValid(sub.ShapeId) && sub.ShapeId.index1 == shapeId.index1)
                return new OverlapHit { Component = comp, SubShape = sub };
        }

        return new OverlapHit { Component = comp, SubShape = null };
    }

    private int DeduplicateRaycastByBody(RawRaycastHit* ptr, int raw)
    {
        int count = 0;
        var seenBuf = ArrayPool<nint>.Shared.Rent(raw > 0 ? raw : 1);
        int seenCount = 0;
        try
        {
            for (int i = 0; i < raw; i++)
            {
                var bodyIndex = B2.ShapeGetBody(ptr[i].ShapeId).index1;
                bool found = false;
                for (int j = 0; j < seenCount; j++)
                {
                    if (seenBuf[j] == bodyIndex)
                    {
                        found = true;
                        break;
                    }
                }

                if (!found)
                {
                    seenBuf[seenCount++] = bodyIndex;
                    ptr[count++] = ptr[i];
                }
            }
        }
        finally
        {
            ArrayPool<nint>.Shared.Return(seenBuf);
        }
        return count;
    }

    private int DeduplicateShapeCastByBody(ShapeCastHit* ptr, int raw)
    {
        int count = 0;
        var seenBuf = ArrayPool<nint>.Shared.Rent(raw > 0 ? raw : 1);
        int seenCount = 0;
        try
        {
            for (int i = 0; i < raw; i++)
            {
                var bodyIndex = B2.ShapeGetBody(ptr[i].ShapeId).index1;
                bool found = false;
                for (int j = 0; j < seenCount; j++)
                {
                    if (seenBuf[j] == bodyIndex) { found = true; break; }
                }
                if (found) continue;

                seenBuf[seenCount++] = bodyIndex;
                var hit = ResolveHitFromShapeId(ptr[i].ShapeId);
                ptr[count++] = ptr[i] with { Component = hit.Component, SubShape = hit.SubShape };
            }
        }
        finally
        {
            ArrayPool<nint>.Shared.Return(seenBuf);
        }
        return count;
    }

    private static Vector2 NormalizeDirection(Vector2 direction)
    {
        if (direction == Vector2.Zero)
            throw new ArgumentException("Direction must be a non-zero vector.", nameof(direction));
        return Vector2.Normalize(direction);
    }

    [UnmanagedCallersOnly]
    private static bool OverlapCallback(B2.ShapeId shapeId, void* context)
    {
        var ctx = (OverlapContext*)context;
        if (ctx->Count >= ctx->Capacity)
            return false;
        ctx->Buffer[ctx->Count++] = shapeId;
        return true;
    }

    [UnmanagedCallersOnly]
    private static bool OverlapFirstCallback(B2.ShapeId shapeId, void* context)
    {
        var ctx = (OverlapFirstContext*)context;
        ctx->ShapeId = shapeId;
        ctx->Found = true;
        return false;
    }

    [UnmanagedCallersOnly]
    private static float RaycastAllCallback(B2.ShapeId shapeId, B2.Vec2 point, B2.Vec2 normal, float fraction, void* context)
    {
        var ctx = (RaycastContext*)context;
        if (ctx->Count >= ctx->Capacity)
            return 0f;

        ctx->Buffer[ctx->Count++] = new RawRaycastHit
        {
            ShapeId = shapeId,
            Point = new Vector2(point.x, point.y),
            Normal = new Vector2(normal.x, normal.y),
            Fraction = fraction
        };

        return 1f;
    }

    [UnmanagedCallersOnly]
    private static bool CustomFilterCallback(B2.ShapeId shapeA, B2.ShapeId shapeB, void* context)
    {
        var handle = GCHandle.FromIntPtr((nint)context);
        var filter = (Func<B2.ShapeId, B2.ShapeId, bool>)handle.Target!;
        return filter(shapeA, shapeB);
    }

    [UnmanagedCallersOnly]
    private static float ShapeCastClosestCallback(B2.ShapeId shapeId, B2.Vec2 point, B2.Vec2 normal, float fraction, void* context)
    {
        var ctx = (ShapeCastClosestContext*)context;
        if (fraction < ctx->ClosestFraction)
        {
            ctx->HasHit = true;
            ctx->ClosestFraction = fraction;
            ctx->Point = point;
            ctx->Normal = normal;
            ctx->ShapeId = shapeId;
        }
        return fraction;
    }

    [UnmanagedCallersOnly]
    private static float ShapeCastAllCallback(B2.ShapeId shapeId, B2.Vec2 point, B2.Vec2 normal, float fraction, void* context)
    {
        var ctx = (ShapeCastAllContext*)context;
        if (ctx->Count >= ctx->Capacity)
            return 0f;

        ctx->Buffer[ctx->Count++] = new ShapeCastHit
        {
            Point = new Vector2(point.x, point.y),
            Normal = new Vector2(normal.x, normal.y),
            Fraction = fraction,
            ShapeId = shapeId
        };

        return 1f;
    }

    private struct OverlapContext
    {
        public B2.ShapeId* Buffer;
        public int Capacity;
        public int Count;
    }

    private struct OverlapFirstContext
    {
        public B2.ShapeId ShapeId;
        public bool Found;
    }

    private struct RawRaycastHit
    {
        public B2.ShapeId ShapeId;
        public Vector2 Point;
        public Vector2 Normal;
        public float Fraction;
    }

    private struct RaycastContext
    {
        public RawRaycastHit* Buffer;
        public int Capacity;
        public int Count;
    }

    private struct ShapeCastClosestContext
    {
        public bool HasHit;
        public float ClosestFraction;
        public B2.Vec2 Point;
        public B2.Vec2 Normal;
        public B2.ShapeId ShapeId;
    }

    private struct ShapeCastAllContext
    {
        public ShapeCastHit* Buffer;
        public int Capacity;
        public int Count;
    }

    // A zero-radius proxy is geometrically degenerate and may miss containment in some
    // shape types via the GJK overlap test. 0.01px is imperceptible but reliable.
    private const float PointQueryRadius = 0.01f;

    public void Dispose()
    {
        if (_disposed)
            return;

        _disposed = true;

        if (_filterHandle.IsAllocated)
            _filterHandle.Free();

        B2.DestroyWorld(_worldId);
        _worldId = default;

        _bodyJoints.Clear();
        _activeBodies.Clear();

        lock (Lock)
        {
            _instanceCount--;
            if (_instanceCount <= 0)
            {
                _instanceCount = 0;
                _lockedPixelsPerMeter = null;
            }
        }
    }

    /// <summary>
    /// Resets the process-wide <see cref="B2.SetLengthUnitsPerMeter"/> lock.
    /// Intended for use in unit tests only — do not call in production code.
    /// </summary>
    internal static void ResetForTesting()
    {
        lock (Lock)
        {
            _lockedPixelsPerMeter = null;
            _instanceCount = 0;
        }
    }
}