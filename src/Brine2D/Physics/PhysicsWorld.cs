using Box2D.NET.Bindings;
using Brine2D.Collision;
using Brine2D.ECS.Components;
using Brine2D.ECS.Components.Joints;
using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

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
    
    private Func<B2.ShapeId, B2.ShapeId, bool>? _systemFilter;
    private Func<PhysicsBodyComponent, PhysicsBodyComponent, bool>? _userFilter;
    private GCHandle _filterHandle;
    private Func<PreSolveContact, bool>? _preSolveFilter;
    private Func<PreSolveContact, bool>? _systemPreSolveFilter;
    private GCHandle _preSolveHandle;

    private readonly List<OverlapHit> _explosionHitsBuffer = [];

    private sealed class PreSolveContext
    {
        public Func<PreSolveContact, bool> Filter = null!;
        public Func<nint, PhysicsBodyComponent?>? Resolver;
    }

    private readonly Dictionary<nint, HashSet<JointComponent>> _bodyJoints = new();
    private readonly HashSet<PhysicsBodyComponent> _activeBodies = [];

    internal volatile int SimulationThreadId;

    private readonly HashSet<(nint, nint)> _ignoredPairs = [];

    internal Func<nint, PhysicsBodyComponent?>? ComponentResolver { get; set; }
    internal Func<IEnumerable<PhysicsBodyComponent>>? AllBodiesResolver { get; set; }

    internal bool HasIgnoredPairs => _ignoredPairs.Count > 0;

    /// <summary>
    /// Suppresses all collision and contact events between <paramref name="a"/> and
    /// <paramref name="b"/> until <see cref="RestoreCollision"/> is called.
    /// If either body is not yet live, the suppression is deferred and applied automatically
    /// the next time both bodies are created. Safe to call multiple times for the same pair.
    /// </summary>
    public void IgnoreCollision(PhysicsBodyComponent a, PhysicsBodyComponent b)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!B2.BodyIsValid(a.BodyId) || !B2.BodyIsValid(b.BodyId))
        {
            _pendingIgnoredComponents.Add(NormalizedComponentPair(a, b));
            return;
        }

        nint ka = Math.Min(a.BodyId.index1, b.BodyId.index1);
        nint kb = Math.Max(a.BodyId.index1, b.BodyId.index1);
        _ignoredPairs.Add((ka, kb));
    }

    /// <summary>
    /// Restores normal collision between <paramref name="a"/> and <paramref name="b"/>
    /// after a previous <see cref="IgnoreCollision"/> call. Also cancels any pending
    /// deferred ignore for this pair. Safe to call when the pair was not ignored.
    /// </summary>
    public void RestoreCollision(PhysicsBodyComponent a, PhysicsBodyComponent b)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        _pendingIgnoredComponents.Remove(NormalizedComponentPair(a, b));

        nint ka = Math.Min(a.BodyId.index1, b.BodyId.index1);
        nint kb = Math.Max(a.BodyId.index1, b.BodyId.index1);
        _ignoredPairs.Remove((ka, kb));
    }

    private readonly HashSet<(PhysicsBodyComponent, PhysicsBodyComponent)> _pendingIgnoredComponents = [];

    private static (PhysicsBodyComponent, PhysicsBodyComponent) NormalizedComponentPair(
        PhysicsBodyComponent a, PhysicsBodyComponent b)
    {
        return RuntimeHelpers.GetHashCode(a) <= RuntimeHelpers.GetHashCode(b) ? (a, b) : (b, a);
    }
    
    internal bool IsCollisionIgnored(nint indexA, nint indexB)
    {
        nint ka = Math.Min(indexA, indexB);
        nint kb = Math.Max(indexA, indexB);
        return _ignoredPairs.Contains((ka, kb));
    }

    /// <summary>
    /// Checks all pending deferred <see cref="IgnoreCollision"/> entries and promotes any
    /// pair where both bodies are now live. Called by <c>Box2DPhysicsSystem</c> after
    /// a body is created or rebuilt.
    /// </summary>
    internal void FlushPendingIgnoredPairs(PhysicsBodyComponent newBody)
    {
        if (_pendingIgnoredComponents.Count == 0) return;

        _pendingIgnoredComponents.RemoveWhere(pair =>
        {
            var (a, b) = pair;
            if (a != newBody && b != newBody) return false;
            var other = a == newBody ? b : a;
            if (!B2.BodyIsValid(newBody.BodyId) || !B2.BodyIsValid(other.BodyId)) return false;
            nint ka = Math.Min(newBody.BodyId.index1, other.BodyId.index1);
            nint kb = Math.Max(newBody.BodyId.index1, other.BodyId.index1);
            _ignoredPairs.Add((ka, kb));
            return true;
        });
    }

    /// <summary>
    /// Removes all pending deferred <see cref="IgnoreCollision"/> entries that reference
    /// <paramref name="body"/>. Called when a body component is destroyed so stale pending
    /// entries do not accumulate.
    /// </summary>
    internal void PurgePendingIgnoredPairsForComponent(PhysicsBodyComponent body)
    {
        if (_pendingIgnoredComponents.Count == 0) return;
        _pendingIgnoredComponents.RemoveWhere(pair => pair.Item1 == body || pair.Item2 == body);
    }

    /// <summary>
    /// Removes all entries in <see cref="_ignoredPairs"/> that reference <paramref name="bodyIndex"/>.
    /// Must be called when a body is destroyed so its recycled index1 slot does not cause a
    /// new body at the same slot to inherit stale collision-ignore state.
    /// </summary>
    internal void PurgeIgnoredPairsForBody(nint bodyIndex)
    {
        _ignoredPairs.RemoveWhere(p => p.Item1 == bodyIndex || p.Item2 == bodyIndex);
    }

    /// <summary>
    /// Returns all currently simulated <see cref="PhysicsBodyComponent"/> instances tracked by the
    /// physics system. Returns an empty sequence if the physics system has not been initialized yet.
    /// </summary>
    public IEnumerable<PhysicsBodyComponent> GetAllBodies()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return AllBodiesResolver?.Invoke() ?? [];
    }

    /// <summary>
    /// Returns all currently sleeping <see cref="PhysicsBodyComponent"/> instances (dynamic or
    /// kinematic bodies that have come to rest). Returns an empty sequence if no bodies are sleeping
    /// or the physics system has not been initialized yet.
    /// </summary>
    public IEnumerable<PhysicsBodyComponent> GetSleepingBodies()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var all = AllBodiesResolver?.Invoke();
        if (all == null)
            return [];
        return all.Where(b => B2.BodyIsValid(b.BodyId) && !B2.BodyIsAwake(b.BodyId));
    }

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
                if (Math.Abs(_lockedPixelsPerMeter.Value - pixelsPerMeter) > 0.01f)
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

        try
        {
            _worldId = B2.CreateWorld(&worldDef);
        }
        catch
        {
            lock (Lock)
            {
                _instanceCount--;
                if (_instanceCount <= 0)
                {
                    _instanceCount = 0;
                    _lockedPixelsPerMeter = null;
                }
            }
            throw;
        }
    }

    internal B2.WorldId WorldId
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);
            return _worldId;
        }
    }

    /// <summary>
    /// <c>true</c> when the world has not been disposed and the underlying Box2D world handle
    /// is still valid. Safe to call at any time — never throws.
    /// </summary>
    internal bool IsValid => !_disposed && B2.WorldIsValid(_worldId);

    public float PixelsPerMeter { get; }

    /// <summary>
    /// Number of Box2D sub-steps per fixed-update tick. Higher values improve simulation
    /// accuracy for fast-moving bodies at the cost of CPU time. Default is 4.
    /// </summary>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when set to less than 1.</exception>
    public int SubStepCount
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 1);
            field = value;
        }
    }

    /// <summary>
    /// Current world gravity in pixels per second squared.
    /// </summary>
    public Vector2 Gravity { get; private set; }

    /// <summary>
    /// Whether the world is currently paused. When <c>true</c>, <see cref="Step"/> is a no-op
    /// and no simulation or event dispatch occurs. In-flight contact state is preserved.
    /// </summary>
    public bool IsPaused { get; private set; }

    private void AssertSimulationThread()
    {
        var simId = SimulationThreadId;
        if (simId != 0 && Environment.CurrentManagedThreadId != simId)
        {
#if DEBUG
            throw new InvalidOperationException(
                "[Brine2D] PhysicsWorld mutation must be called from the simulation thread " +
                "(inside FixedUpdate). Calling from another thread causes undefined behavior in Box2D.");
#else
            Trace.TraceWarning(
                "[Brine2D] PhysicsWorld mutation called from outside the simulation thread " +
                "(thread " + Environment.CurrentManagedThreadId + " != expected " + simId + "). " +
                "This causes undefined behavior in Box2D.");
#endif
        }
    }

    /// <summary>
    /// Sets world gravity at runtime in pixels per second squared.
    /// All awake dynamic bodies are affected immediately.
    /// </summary>
    public void SetGravity(Vector2 gravity)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        AssertSimulationThread();
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
        AssertSimulationThread();
        B2.WorldSetHitEventThreshold(_worldId, minSpeed);
    }

    /// <summary>
    /// Sets the minimum impact speed (pixels/s) required for restitution (bounciness) to be
    /// applied. Below this threshold contacts are treated as inelastic. Set to 0 to always
    /// apply restitution. Leave unset to use the Box2D default.
    /// </summary>
    public void SetRestitutionThreshold(float threshold)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfNegative(threshold);
        AssertSimulationThread();
        B2.WorldSetRestitutionThreshold(_worldId, threshold);
    }

    /// <summary>
    /// Enables or disables body sleeping. When disabled, all bodies stay awake permanently.
    /// </summary>
    public void SetSleepingEnabled(bool enabled)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        AssertSimulationThread();
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
        AssertSimulationThread();
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
        AssertSimulationThread();
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

    internal B2.ShapeId CreateCapsuleShape(B2.BodyId bodyId, B2.ShapeDef* shapeDef, B2.Capsule* capsule)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return B2.CreateCapsuleShape(bodyId, shapeDef, capsule);
    }

    internal B2.ShapeId CreateSegmentShape(B2.BodyId bodyId, B2.ShapeDef* shapeDef, B2.Segment* segment)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        return B2.CreateSegmentShape(bodyId, shapeDef, segment);
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

    /// <summary>
    /// Fills <paramref name="results"/> with every <see cref="JointComponent"/> currently
    /// registered in the world, deduplicated (joints connecting two bodies appear only once).
    /// Clears <paramref name="results"/> before writing.
    /// </summary>
    public void GetAllJoints(List<JointComponent> results)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();
        var seen = new HashSet<JointComponent>(ReferenceEqualityComparer.Instance);
        foreach (var list in _bodyJoints.Values)
        {
            foreach (var joint in list)
                seen.Add(joint);
        }
        results.AddRange(seen);
    }

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
        if (!_bodyJoints.TryGetValue(bodyIndex, out var joints))
            return;

        foreach (var joint in joints)
        {
            joint.IsDirty = true;
            var otherIndex = joint.ConnectedBody?.BodyId.index1 ?? default;
            if (otherIndex != bodyIndex && otherIndex != default)
                RemoveFromJointRegistry(otherIndex, joint);
        }

        _bodyJoints.Remove(bodyIndex);
    }

    private void AddToJointRegistry(nint bodyIndex, JointComponent joint)
    {
        if (!_bodyJoints.TryGetValue(bodyIndex, out var set))
        {
            set = new HashSet<JointComponent>(ReferenceEqualityComparer.Instance);
            _bodyJoints[bodyIndex] = set;
        }
        set.Add(joint);
    }

    private void RemoveFromJointRegistry(nint bodyIndex, JointComponent joint)
    {
        if (!_bodyJoints.TryGetValue(bodyIndex, out var set))
            return;
        set.Remove(joint);
        if (set.Count == 0)
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

        if (!_bodyJoints.TryGetValue(body.BodyId.index1, out var set))
            return 0;

        int written = 0;
        foreach (var joint in set)
        {
            if (written >= results.Length) break;
            results[written++] = joint;
        }
        return written;
    }

    /// <summary>
    /// Returns all joints that <paramref name="body"/> participates in (as either body A or body B),
    /// adding them to <paramref name="results"/>. Clears <paramref name="results"/> before writing.
    /// Returns immediately if the body has no live joints or has not yet been created.
    /// </summary>
    /// <param name="body">The body to query joints for.</param>
    /// <param name="results">List to receive joint components.</param>
    public void GetJointsAll(PhysicsBodyComponent body, List<JointComponent> results)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();

        if (!B2.BodyIsValid(body.BodyId))
            return;

        if (!_bodyJoints.TryGetValue(body.BodyId.index1, out var list))
            return;

        results.AddRange(list);
    }

    /// <summary>
    /// Reads the live contact manifolds for <paramref name="body"/> directly from Box2D and
    /// writes them into <paramref name="results"/>.
    /// Returns 0 if the body is not live, has no active contacts, or <paramref name="results"/> is empty.
    /// </summary>
    /// <remarks>
    /// This reads live Box2D contact data and is safe to call any time after the first
    /// <see cref="Step"/>. Contact normals are oriented away from the other body toward
    /// <paramref name="body"/>, consistent with <see cref="PhysicsBodyComponent.OnCollisionEnter"/>.
    /// When the number of live contacts exceeds <paramref name="results"/>.Length,
    /// the result is clipped and <paramref name="wasTruncated"/> is set to <c>true</c>.
    /// Use <see cref="GetContactsAll"/> for guaranteed-complete results.
    /// </remarks>
    /// <param name="body">The body to query contacts for.</param>
    /// <param name="results">Buffer to receive contact pairs.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when there were more contacts than <paramref name="results"/> could hold.</param>
    /// <returns>Number of contacts written.</returns>
    public int GetContacts(PhysicsBodyComponent body, Span<ContactPair> results, out bool wasTruncated)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        wasTruncated = false;

        if (!B2.BodyIsValid(body.BodyId) || results.Length == 0)
            return 0;

        int capacity = Math.Max(results.Length * 2, 16);
        while (true)
        {
            var raw = ArrayPool<B2.ContactData>.Shared.Rent(capacity);
            try
            {
                int count;
                fixed (B2.ContactData* ptr = raw)
                    count = B2.BodyGetContactData(body.BodyId, ptr, capacity);

                if (count >= capacity) { capacity *= 2; continue; }

                if (count > results.Length)
                {
                    wasTruncated = true;
                    count = results.Length;
                }

                for (int i = 0; i < count; i++)
                {
                    ref var data = ref raw[i];
                    bool selfIsA = B2.ShapeGetBody(data.shapeIdA).index1 == body.BodyId.index1;
                    var selfShapeId = selfIsA ? data.shapeIdA : data.shapeIdB;
                    var otherShapeId = selfIsA ? data.shapeIdB : data.shapeIdA;

                    var other = ResolveComponent(otherShapeId);
                    var selfSub = ResolveSubShapeOnBody(body, selfShapeId);
                    var otherSub = other != null ? ResolveSubShapeOnBody(other, otherShapeId) : null;

                    var contact = CollisionContact.FromManifold(data.manifold);

                    if (selfIsA)
                        contact = contact with { Normal = -contact.Normal };

                    results[i] = new ContactPair
                    {
                        Other = other,
                        Contact = contact,
                        SelfSubShape = selfSub,
                        OtherSubShape = otherSub
                    };
                }
                return count;
            }
            finally
            {
                ArrayPool<B2.ContactData>.Shared.Return(raw, clearArray: true);
            }
        }
    }

    /// <inheritdoc cref="GetContacts(PhysicsBodyComponent,Span{ContactPair},out bool)"/>
    public int GetContacts(PhysicsBodyComponent body, Span<ContactPair> results)
        => GetContacts(body, results, out _);

    /// <summary>
    /// Reads the live contact manifolds for <paramref name="body"/> directly from Box2D and
    /// adds them to <paramref name="results"/>. Clears <paramref name="results"/> before writing.
    /// Returns immediately if the body is not live or has no active contacts.
    /// </summary>
    /// <remarks>
    /// Retries internally with a larger buffer if needed — guaranteed to return every contact.
    /// Contact normals are oriented away from the other body toward <paramref name="body"/>.
    /// </remarks>
    /// <param name="body">The body to query contacts for.</param>
    /// <param name="results">List to receive contact pairs.</param>
    public void GetContactsAll(PhysicsBodyComponent body, List<ContactPair> results)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();

        if (!B2.BodyIsValid(body.BodyId))
            return;

        int capacity = 16;
        while (true)
        {
            var raw = ArrayPool<B2.ContactData>.Shared.Rent(capacity);
            try
            {
                int count;
                fixed (B2.ContactData* ptr = raw)
                    count = B2.BodyGetContactData(body.BodyId, ptr, capacity);

                if (count >= capacity) { capacity *= 2; continue; }

                for (int i = 0; i < count; i++)
                {
                    ref var data = ref raw[i];
                    bool selfIsA = B2.ShapeGetBody(data.shapeIdA).index1 == body.BodyId.index1;
                    var selfShapeId = selfIsA ? data.shapeIdA : data.shapeIdB;
                    var otherShapeId = selfIsA ? data.shapeIdB : data.shapeIdA;

                    var other = ResolveComponent(otherShapeId);
                    var selfSub = ResolveSubShapeOnBody(body, selfShapeId);
                    var otherSub = other != null ? ResolveSubShapeOnBody(other, otherShapeId) : null;

                    var contact = CollisionContact.FromManifold(data.manifold);
                    // Box2D normals point shapeA -> shapeB; flip when self is A.
                    if (selfIsA)
                        contact = contact with { Normal = -contact.Normal };

                    results.Add(new ContactPair
                    {
                        Other = other,
                        Contact = contact,
                        SelfSubShape = selfSub,
                        OtherSubShape = otherSub
                    });
                }
                return;
            }
            finally
            {
                ArrayPool<B2.ContactData>.Shared.Return(raw, clearArray: true);
            }
        }
    }

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

        if (filter?.ExcludeSensors == true || filter?.ExcludeBody != null || filter?.ExcludeBodies is { Length: > 0 })
        {
            int capacity = 64;
            while (true)
            {
                var rawArray = ArrayPool<RawRaycastHit>.Shared.Rent(capacity);
                try
                {
                    fixed (RawRaycastHit* ptr = rawArray)
                    {
                        var ctx = new RaycastContext { Buffer = ptr, Capacity = capacity, Count = 0 };
                        B2.WorldCastRay(_worldId, o, translation, f, &RaycastAllCallback, &ctx);

                        if (ctx.Count >= capacity) { capacity *= 2; continue; }

                        float best = float.MaxValue;
                        int bestIdx = -1;
                        for (int i = 0; i < ctx.Count; i++)
                        {
                            if (filter?.ExcludeSensors == true && B2.ShapeIsSensor(ptr[i].ShapeId)) continue;
                            if (ShouldExcludeShape(ptr[i].ShapeId, filter)) continue;
                            if (ptr[i].Fraction < best) { best = ptr[i].Fraction; bestIdx = i; }
                        }

                        if (bestIdx < 0)
                            return null;

                        var h = ptr[bestIdx];
                        var resolved = ResolveHitFromShapeId(h.ShapeId);
                        return new RaycastHit
                        {
                            Point = h.Point,
                            Normal = h.Normal,
                            Fraction = h.Fraction,
                            Distance = h.Fraction * maxDistance,
                            ShapeId = h.ShapeId,
                            Component = resolved.Component,
                            SubShape = resolved.SubShape
                        };
                    }
                }
                finally
                {
                    ArrayPool<RawRaycastHit>.Shared.Return(rawArray, clearArray: true);
                }
            }
        }

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
    /// <param name="wasTruncated">Set to <c>true</c> when the internal collection buffer was too small.</param>
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

                new Span<RawRaycastHit>(ptr, raw).Sort((a, b) => a.Fraction.CompareTo(b.Fraction));

                int deduped = DeduplicateRaycastByBody(ptr, raw);
                int written = 0;
                for (int i = 0; i < deduped && written < results.Length; i++)
                {
                    if (filter?.ExcludeSensors == true && B2.ShapeIsSensor(ptr[i].ShapeId)) continue;
                    if (ShouldExcludeShape(ptr[i].ShapeId, filter)) continue;
                    var h = ptr[i];
                    var resolved = ResolveHitFromShapeId(h.ShapeId);
                    results[written++] = new RaycastHit
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

                wasTruncated = raw >= rawCapacity;
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

        int rawCapacity = Math.Max(results.Length * 8, 16);
        var rawArray = ArrayPool<RawRaycastHit>.Shared.Rent(rawCapacity);
        try
        {
            fixed (RawRaycastHit* ptr = rawArray)
            {
                var ctx = new RaycastContext { Buffer = ptr, Capacity = rawCapacity, Count = 0 };
                B2.WorldCastRay(_worldId, o, translation, f, &RaycastAllCallback, &ctx);
                int raw = ctx.Count;

                new Span<RawRaycastHit>(ptr, raw).Sort((a, b) => a.Fraction.CompareTo(b.Fraction));

                int written = 0;
                for (int i = 0; i < raw && written < results.Length; i++)
                {
                    if (filter?.ExcludeSensors == true && B2.ShapeIsSensor(ptr[i].ShapeId)) continue;
                    if (ShouldExcludeShape(ptr[i].ShapeId, filter)) continue;
                    var h = ptr[i];
                    var resolved = ResolveHitFromShapeId(h.ShapeId);
                    results[written++] = new RaycastHit
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

                wasTruncated = raw >= rawCapacity;
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

    /// <summary>
    /// Casts a ray and returns all hits (deduplicated by body, sorted nearest-first) into
    /// <paramref name="results"/>. Unlike the <see cref="Span{T}"/> overloads, this variant
    /// retries with a larger internal buffer if needed and is guaranteed to return every hit.
    /// Clears <paramref name="results"/> before writing.
    /// </summary>
    public void RaycastAll(Vector2 origin, Vector2 direction, float maxDistance,
        List<RaycastHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();

        var norm = NormalizeDirection(direction);
        var translation = new B2.Vec2 { x = norm.X * maxDistance, y = norm.Y * maxDistance };
        var o = new B2.Vec2 { x = origin.X, y = origin.Y };
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        RaycastAllCore(o, translation, maxDistance, f, results, deduplicate: true, filter);
    }

    /// <summary>
    /// Casts a ray and returns all shape hits (not deduplicated, sorted nearest-first) into
    /// <paramref name="results"/>. Unlike the <see cref="Span{T}"/> overloads, this variant
    /// retries with a larger internal buffer if needed and is guaranteed to return every hit.
    /// Clears <paramref name="results"/> before writing.
    /// </summary>
    public void RaycastAllShapes(Vector2 origin, Vector2 direction, float maxDistance,
        List<RaycastHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();

        var norm = NormalizeDirection(direction);
        var translation = new B2.Vec2 { x = norm.X * maxDistance, y = norm.Y * maxDistance };
        var o = new B2.Vec2 { x = origin.X, y = origin.Y };
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        RaycastAllCore(o, translation, maxDistance, f, results, deduplicate: false, filter);
    }

    private void RaycastAllCore(B2.Vec2 origin, B2.Vec2 translation, float maxDistance,
    B2.QueryFilter f, List<RaycastHit> results, bool deduplicate, PhysicsQueryFilter? filter = null)
    {
        int capacity = 64;
        while (true)
        {
            var rawArray = ArrayPool<RawRaycastHit>.Shared.Rent(capacity);
            try
            {
                fixed (RawRaycastHit* ptr = rawArray)
                {
                    var ctx = new RaycastContext { Buffer = ptr, Capacity = capacity, Count = 0 };
                    B2.WorldCastRay(_worldId, origin, translation, f, &RaycastAllCallback, &ctx);
                    int raw = ctx.Count;

                    if (raw >= capacity)
                    {
                        capacity *= 2;
                        continue;
                    }

                    new Span<RawRaycastHit>(ptr, raw).Sort((a, b) => a.Fraction.CompareTo(b.Fraction));

                    if (deduplicate)
                        raw = DeduplicateRaycastByBody(ptr, raw);

                    for (int i = 0; i < raw; i++)
                    {
                        if (filter?.ExcludeSensors == true && B2.ShapeIsSensor(ptr[i].ShapeId)) continue;
                        if (ShouldExcludeShape(ptr[i].ShapeId, filter)) continue;
                        var h = ptr[i];
                        var resolved = ResolveHitFromShapeId(h.ShapeId);
                        results.Add(new RaycastHit
                        {
                            Component = resolved.Component,
                            SubShape = resolved.SubShape,
                            ShapeId = h.ShapeId,
                            Point = h.Point,
                            Normal = h.Normal,
                            Fraction = h.Fraction,
                            Distance = h.Fraction * maxDistance
                        });
                    }

                    return;
                }
            }
            finally
            {
                ArrayPool<RawRaycastHit>.Shared.Return(rawArray, clearArray: true);
            }
        }
    }

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
    /// <remarks>
    /// When <see cref="PhysicsQueryFilter.ExcludeSensors"/> is <c>true</c>, this method collects all
    /// hits via callback and returns the nearest non-sensor, which is more expensive than the default
    /// single-closest path. Prefer leaving <c>ExcludeSensors</c> unset and filtering results manually
    /// in performance-critical code.
    /// </remarks>
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
    /// <remarks>
    /// When <see cref="PhysicsQueryFilter.ExcludeSensors"/> is <c>true</c>, this method collects all
    /// hits via callback and returns the nearest non-sensor, which is more expensive than the default
    /// single-closest path. Prefer leaving <c>ExcludeSensors</c> unset and filtering results manually
    /// in performance-critical code.
    /// </remarks>
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
    /// <remarks>
    /// When <see cref="PhysicsQueryFilter.ExcludeSensors"/> is <c>true</c>, this method collects all
    /// hits via callback and returns the nearest non-sensor, which is more expensive than the default
    /// single-closest path. Prefer leaving <c>ExcludeSensors</c> unset and filtering results manually
    /// in performance-critical code.
    /// </remarks>
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
        // When ExcludeBodies is populated, the single-slot native callback cannot handle it;
        // fall back to the collect-all path and pick the nearest non-excluded hit.
        if (filter?.ExcludeBodies is { Length: > 0 })
            return ShapeCastClosestViaAll(proxy, direction, maxDistance, filter);

        var norm = NormalizeDirection(direction);
        var translation = new B2.Vec2 { x = norm.X * maxDistance, y = norm.Y * maxDistance };
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        var excludeBody = filter?.ExcludeBody;
        var ctx = new ShapeCastClosestContext
        {
            HasHit = false,
            ExcludeSensors = filter?.ExcludeSensors ?? false,
            ExcludeBodyIndex = (excludeBody != null && B2.BodyIsValid(excludeBody.BodyId))
                ? excludeBody.BodyId.index1
                : 0,
            ClosestFraction = float.MaxValue
        };
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

    private ShapeCastHit? ShapeCastClosestViaAll(B2.ShapeProxy* proxy, Vector2 direction, float maxDistance, PhysicsQueryFilter? filter)
    {
        int capacity = 16;
        while (true)
        {
            var poolBuf = ArrayPool<ShapeCastHit>.Shared.Rent(capacity);
            try
            {
                int count = ShapeCastAllCore(proxy, direction, maxDistance, poolBuf.AsSpan(0, capacity), out bool truncated, filter, deduplicate: false);
                if (truncated || count == capacity)
                {
                    capacity = Math.Max(count * 2, capacity * 2);
                    continue;
                }
                return PickClosestShapeCastHit(poolBuf.AsSpan(0, count));
            }
            finally
            {
                ArrayPool<ShapeCastHit>.Shared.Return(poolBuf, clearArray: true);
            }
        }
    }

    private static ShapeCastHit? PickClosestShapeCastHit(Span<ShapeCastHit> hits)
    {
        if (hits.IsEmpty) return null;
        int best = 0;
        for (int i = 1; i < hits.Length; i++)
        {
            if (hits[i].Fraction < hits[best].Fraction)
                best = i;
        }
        return hits[best];
    }

    /// <summary>
    /// Sweeps a circle along a direction and returns all hits sorted by distance,
    /// deduplicated by body (one hit per body, nearest shape wins).
    /// </summary>
    /// <param name="origin">Center of the circle at the start of the sweep in pixel coordinates.</param>
    /// <param name="radius">Circle radius in pixels.</param>
    /// <param name="direction">Sweep direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum sweep distance in pixels.</param>
    /// <param name="results">Buffer to receive hits, sorted nearest-first.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the internal buffer was too small.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of hits written to <paramref name="results"/>.</returns>
    /// <remarks>
    /// The internal collection buffer is sized to <c>results.Length * 8</c>. If more shapes than
    /// that are hit, collection stops early and <paramref name="wasTruncated"/> is set to
    /// <c>true</c>. Provide a larger buffer to increase capacity.
    /// </remarks>
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
        return ShapeCastAllCore(&proxy, direction, maxDistance, results, out wasTruncated, filter, deduplicate: true);
    }

    /// <summary>
    /// Sweeps a circle along a direction and returns all shape hits (not deduplicated, sorted
    /// nearest-first) into <paramref name="results"/>. Compound bodies may appear more than once.
    /// </summary>
    /// <param name="origin">Center of the circle at the start of the sweep in pixel coordinates.</param>
    /// <param name="radius">Circle radius in pixels.</param>
    /// <param name="direction">Sweep direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum sweep distance in pixels.</param>
    /// <param name="results">Buffer to receive hits, sorted nearest-first.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the results buffer was too small to hold all hits.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of hits written to <paramref name="results"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> is zero.</exception>
    public int ShapeCastAllShapes(Vector2 origin, float radius, Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        wasTruncated = false;

        if (results.Length == 0)
            return 0;

        var center = new B2.Vec2 { x = origin.X, y = origin.Y };
        var proxy = B2.MakeProxy(&center, 1, radius);
        return ShapeCastAllCore(&proxy, direction, maxDistance, results, out wasTruncated, filter, deduplicate: false);
    }

    /// <inheritdoc cref="ShapeCastAllShapes(Vector2,float,Vector2,float,Span{ShapeCastHit},out bool,PhysicsQueryFilter?)"/>
    public int ShapeCastAllShapes(Vector2 origin, float radius, Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
        => ShapeCastAllShapes(origin, radius, direction, maxDistance, results, out _, filter);

    /// <summary>
    /// Sweeps a capsule along a direction and returns all shape hits (not deduplicated, sorted
    /// nearest-first) into <paramref name="results"/>. Compound bodies may appear more than once.
    /// </summary>
    /// <param name="center1">First capsule center point at the start of the sweep in pixel coordinates.</param>
    /// <param name="center2">Second capsule center point at the start of the sweep in pixel coordinates.</param>
    /// <param name="radius">Capsule radius in pixels.</param>
    /// <param name="direction">Sweep direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum sweep distance in pixels.</param>
    /// <param name="results">Buffer to receive hits, sorted nearest-first.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the results buffer was too small to hold all hits.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of hits written to <paramref name="results"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> is zero.</exception>
    public int ShapeCastAllShapes(Vector2 center1, Vector2 center2, float radius, Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        wasTruncated = false;

        if (results.Length == 0)
            return 0;

        var pts = stackalloc B2.Vec2[2];
        pts[0] = new B2.Vec2 { x = center1.X, y = center1.Y };
        pts[1] = new B2.Vec2 { x = center2.X, y = center2.Y };
        var proxy = B2.MakeProxy(pts, 2, radius);
        return ShapeCastAllCore(&proxy, direction, maxDistance, results, out wasTruncated, filter, deduplicate: false);
    }

    /// <inheritdoc cref="ShapeCastAllShapes(Vector2,Vector2,float,Vector2,float,Span{ShapeCastHit},out bool,PhysicsQueryFilter?)"/>
    public int ShapeCastAllShapes(Vector2 center1, Vector2 center2, float radius, Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
        => ShapeCastAllShapes(center1, center2, radius, direction, maxDistance, results, out _, filter);

    /// <summary>
    /// Sweeps an oriented box along a direction and returns all shape hits (not deduplicated, sorted
    /// nearest-first) into <paramref name="results"/>. Compound bodies may appear more than once.
    /// </summary>
    /// <param name="origin">Center of the box at the start of the sweep in pixel coordinates.</param>
    /// <param name="halfWidth">Half-width of the box in pixels.</param>
    /// <param name="halfHeight">Half-height of the box in pixels.</param>
    /// <param name="angle">Rotation of the box in radians.</param>
    /// <param name="direction">Sweep direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum sweep distance in pixels.</param>
    /// <param name="results">Buffer to receive hits, sorted nearest-first.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the results buffer was too small to hold all hits.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of hits written to <paramref name="results"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> is zero.</exception>
    public int ShapeCastAllShapes(Vector2 origin, float halfWidth, float halfHeight, float angle,
        Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        wasTruncated = false;

        if (results.Length == 0)
            return 0;

        var b2Center = new B2.Vec2 { x = origin.X, y = origin.Y };
        var box = B2.MakeOffsetBox(halfWidth, halfHeight, b2Center, B2.MakeRot(angle));
        var corners = stackalloc B2.Vec2[4];
        corners[0] = box.vertices[0];
        corners[1] = box.vertices[1];
        corners[2] = box.vertices[2];
        corners[3] = box.vertices[3];
        var proxy = B2.MakeProxy(corners, 4, 0f);
        return ShapeCastAllCore(&proxy, direction, maxDistance, results, out wasTruncated, filter, deduplicate: false);
    }

    /// <inheritdoc cref="ShapeCastAllShapes(Vector2,float,float,float,Vector2,float,Span{ShapeCastHit},out bool,PhysicsQueryFilter?)"/>
    public int ShapeCastAllShapes(Vector2 origin, float halfWidth, float halfHeight, float angle,
        Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
        => ShapeCastAllShapes(origin, halfWidth, halfHeight, angle, direction, maxDistance, results, out _, filter);

    /// <summary>
    /// Sweeps a convex polygon along a direction and returns all shape hits (not deduplicated, sorted
    /// nearest-first) into <paramref name="results"/>. Compound bodies may appear more than once.
    /// Vertices are in world space at the start of the sweep (3–8 vertices).
    /// </summary>
    /// <param name="vertices">Convex polygon vertices in world space (3–8 vertices).</param>
    /// <param name="direction">Sweep direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum sweep distance in pixels.</param>
    /// <param name="results">Buffer to receive hits, sorted nearest-first.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the results buffer was too small to hold all hits.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of hits written to <paramref name="results"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> is zero.</exception>
    public int ShapeCastAllShapes(ReadOnlySpan<Vector2> vertices, Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        wasTruncated = false;

        if (results.Length == 0)
            return 0;

        if (vertices.Length < 3 || vertices.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(vertices), "Polygon shape cast requires 3-8 vertices.");

        var b2Verts = stackalloc B2.Vec2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            b2Verts[i] = new B2.Vec2 { x = vertices[i].X, y = vertices[i].Y };

        var proxy = B2.MakeProxy(b2Verts, vertices.Length, 0f);
        return ShapeCastAllCore(&proxy, direction, maxDistance, results, out wasTruncated, filter, deduplicate: false);
    }

    /// <inheritdoc cref="ShapeCastAllShapes(ReadOnlySpan{Vector2},Vector2,float,Span{ShapeCastHit},out bool,PhysicsQueryFilter?)"/>
    public int ShapeCastAllShapes(ReadOnlySpan<Vector2> vertices, Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
        => ShapeCastAllShapes(vertices, direction, maxDistance, results, out _, filter);

    /// <summary>
    /// Sweeps a circle along a direction and returns all hits (deduplicated by body, sorted
    /// nearest-first) into <paramref name="results"/>. Retries with a larger internal buffer
    /// if needed — guaranteed to return every hit. Clears <paramref name="results"/> before writing.
    /// </summary>
    public void ShapeCastAll(Vector2 origin, float radius, Vector2 direction, float maxDistance,
        List<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();
        var center = new B2.Vec2 { x = origin.X, y = origin.Y };
        var proxy = B2.MakeProxy(&center, 1, radius);
        ShapeCastAllCore(&proxy, direction, maxDistance, results, filter, deduplicate: true);
    }

    /// <summary>
    /// Sweeps a circle along a direction and returns all shape hits (not deduplicated, sorted
    /// nearest-first) into <paramref name="results"/>. Retries with a larger internal buffer
    /// if needed — guaranteed to return every hit. Clears <paramref name="results"/> before writing.
    /// </summary>
    public void ShapeCastAllShapes(Vector2 origin, float radius, Vector2 direction, float maxDistance,
        List<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();
        var center = new B2.Vec2 { x = origin.X, y = origin.Y };
        var proxy = B2.MakeProxy(&center, 1, radius);
        ShapeCastAllCore(&proxy, direction, maxDistance, results, filter, deduplicate: false);
    }

    /// <summary>
    /// Sweeps a capsule along a direction and returns all hits (deduplicated by body, sorted
    /// nearest-first) into <paramref name="results"/>. Retries with a larger internal buffer
    /// if needed — guaranteed to return every hit. Clears <paramref name="results"/> before writing.
    /// </summary>
    public void ShapeCastAll(Vector2 center1, Vector2 center2, float radius, Vector2 direction, float maxDistance,
        List<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();
        var pts = stackalloc B2.Vec2[2];
        pts[0] = new B2.Vec2 { x = center1.X, y = center1.Y };
        pts[1] = new B2.Vec2 { x = center2.X, y = center2.Y };
        var proxy = B2.MakeProxy(pts, 2, radius);
        ShapeCastAllCore(&proxy, direction, maxDistance, results, filter, deduplicate: true);
    }

    /// <summary>
    /// Sweeps a capsule along a direction and returns all shape hits (not deduplicated, sorted
    /// nearest-first) into <paramref name="results"/>. Retries with a larger internal buffer
    /// if needed — guaranteed to return every hit. Clears <paramref name="results"/> before writing.
    /// </summary>
    public void ShapeCastAllShapes(Vector2 center1, Vector2 center2, float radius, Vector2 direction, float maxDistance,
        List<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();
        var pts = stackalloc B2.Vec2[2];
        pts[0] = new B2.Vec2 { x = center1.X, y = center1.Y };
        pts[1] = new B2.Vec2 { x = center2.X, y = center2.Y };
        var proxy = B2.MakeProxy(pts, 2, radius);
        ShapeCastAllCore(&proxy, direction, maxDistance, results, filter, deduplicate: false);
    }

    /// <summary>
    /// Sweeps an oriented box along a direction and returns all hits (deduplicated by body, sorted
    /// nearest-first) into <paramref name="results"/>. Retries with a larger internal buffer
    /// if needed — guaranteed to return every hit. Clears <paramref name="results"/> before writing.
    /// </summary>
    public void ShapeCastAll(Vector2 origin, float halfWidth, float halfHeight, float angle,
        Vector2 direction, float maxDistance, List<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();
        var b2Center = new B2.Vec2 { x = origin.X, y = origin.Y };
        var box = B2.MakeOffsetBox(halfWidth, halfHeight, b2Center, B2.MakeRot(angle));
        var corners = stackalloc B2.Vec2[4];
        corners[0] = box.vertices[0]; corners[1] = box.vertices[1];
        corners[2] = box.vertices[2]; corners[3] = box.vertices[3];
        var proxy = B2.MakeProxy(corners, 4, 0f);
        ShapeCastAllCore(&proxy, direction, maxDistance, results, filter, deduplicate: true);
    }

    /// <summary>
    /// Sweeps an oriented box along a direction and returns all shape hits (not deduplicated, sorted
    /// nearest-first) into <paramref name="results"/>. Retries with a larger internal buffer
    /// if needed — guaranteed to return every hit. Clears <paramref name="results"/> before writing.
    /// </summary>
    public void ShapeCastAllShapes(Vector2 origin, float halfWidth, float halfHeight, float angle,
        Vector2 direction, float maxDistance, List<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();
        var b2Center = new B2.Vec2 { x = origin.X, y = origin.Y };
        var box = B2.MakeOffsetBox(halfWidth, halfHeight, b2Center, B2.MakeRot(angle));
        var corners = stackalloc B2.Vec2[4];
        corners[0] = box.vertices[0]; corners[1] = box.vertices[1];
        corners[2] = box.vertices[2]; corners[3] = box.vertices[3];
        var proxy = B2.MakeProxy(corners, 4, 0f);
        ShapeCastAllCore(&proxy, direction, maxDistance, results, filter, deduplicate: false);
    }

    /// <summary>
    /// Sweeps a convex polygon along a direction and returns all hits (deduplicated by body, sorted
    /// nearest-first) into <paramref name="results"/>. Retries with a larger internal buffer
    /// if needed — guaranteed to return every hit. Clears <paramref name="results"/> before writing.
    /// </summary>
    public void ShapeCastAll(ReadOnlySpan<Vector2> vertices, Vector2 direction, float maxDistance,
        List<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();

        if (vertices.Length < 3 || vertices.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(vertices), "Polygon shape cast requires 3-8 vertices.");

        var b2Verts = stackalloc B2.Vec2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            b2Verts[i] = new B2.Vec2 { x = vertices[i].X, y = vertices[i].Y };

        var proxy = B2.MakeProxy(b2Verts, vertices.Length, 0f);
        ShapeCastAllCore(&proxy, direction, maxDistance, results, filter, deduplicate: true);
    }

    /// <summary>
    /// Sweeps a convex polygon along a direction and returns all shape hits (not deduplicated, sorted
    /// nearest-first) into <paramref name="results"/>. Retries with a larger internal buffer
    /// if needed — guaranteed to return every hit. Clears <paramref name="results"/> before writing.
    /// </summary>
    public void ShapeCastAllShapes(ReadOnlySpan<Vector2> vertices, Vector2 direction, float maxDistance,
        List<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();

        if (vertices.Length < 3 || vertices.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(vertices), "Polygon shape cast requires 3-8 vertices.");

        var b2Verts = stackalloc B2.Vec2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            b2Verts[i] = new B2.Vec2 { x = vertices[i].X, y = vertices[i].Y };

        var proxy = B2.MakeProxy(b2Verts, vertices.Length, 0f);
        ShapeCastAllCore(&proxy, direction, maxDistance, results, filter, deduplicate: false);
    }

    private void ShapeCastAllCore(B2.ShapeProxy* proxy, Vector2 direction, float maxDistance,
    List<ShapeCastHit> results, PhysicsQueryFilter? filter, bool deduplicate)
    {
        var norm = NormalizeDirection(direction);
        var translation = new B2.Vec2 { x = norm.X * maxDistance, y = norm.Y * maxDistance };
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        int capacity = 64;
        while (true)
        {
            var rawArray = ArrayPool<ShapeCastHit>.Shared.Rent(capacity);
            try
            {
                fixed (ShapeCastHit* ptr = rawArray)
                {
                    var ctx = new ShapeCastAllContext { Buffer = ptr, Capacity = capacity, Count = 0 };
                    B2.WorldCastShape(_worldId, proxy, translation, f, &ShapeCastAllCallback, &ctx);
                    int raw = ctx.Count;

                    if (raw >= capacity) { capacity *= 2; continue; }

                    new Span<ShapeCastHit>(ptr, raw).Sort((a, b) => a.Fraction.CompareTo(b.Fraction));

                    if (deduplicate)
                        raw = DeduplicateShapeCastByBody(ptr, raw);

                    for (int i = 0; i < raw; i++)
                    {
                        if (filter?.ExcludeSensors == true && B2.ShapeIsSensor(ptr[i].ShapeId)) continue;
                        if (ShouldExcludeShape(ptr[i].ShapeId, filter)) continue;
                        var h = ptr[i];
                        var resolved = ResolveHitFromShapeId(h.ShapeId);
                        results.Add(new ShapeCastHit
                        {
                            Component = resolved.Component,
                            SubShape = resolved.SubShape,
                            ShapeId = h.ShapeId,
                            Point = h.Point,
                            Normal = h.Normal,
                            Fraction = h.Fraction,
                            Distance = h.Fraction * maxDistance
                        });
                    }
                    return;
                }
            }
            finally
            {
                ArrayPool<ShapeCastHit>.Shared.Return(rawArray, clearArray: true);
            }
        }
    }

    /// <summary>
    /// Returns the first overlapping hit whose AABB intersects the given axis-aligned bounding box,
    /// or <c>null</c> if nothing is hit.
    /// </summary>
    /// <remarks>This is a <b>broad-phase AABB test only</b> — shapes are not tested for exact overlap.</remarks>
    /// <param name="min">Lower-left corner of the query AABB in pixel coordinates.</param>
    /// <param name="max">Upper-right corner of the query AABB in pixel coordinates.</param>
    /// <param name="filter">Optional query filter.</param>
    public OverlapHit? OverlapAABBFirst(Vector2 min, Vector2 max, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var aabb = new B2.AABB
        {
            lowerBound = new B2.Vec2 { x = min.X, y = min.Y },
            upperBound = new B2.Vec2 { x = max.X, y = max.Y }
        };
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        return OverlapAABBFirstHitCore(aabb, f, filter?.ExcludeSensors ?? false, filter);
    }

    /// <summary>
    /// Returns all bodies (deduplicated by body) whose AABB intersects the given
    /// axis-aligned bounding box, written into <paramref name="results"/>.
    /// Use <see cref="OverlapAABBShapes"/> for per-shape granularity.
    /// </summary>
    /// <param name="min">Lower-left corner of the query AABB in pixel coordinates.</param>
    /// <param name="max">Upper-right corner of the query AABB in pixel coordinates.</param>
    /// <param name="results">Buffer to receive results.</param>f
    /// <param name="wasTruncated">Set to <c>true</c> when the internal collection buffer was too small.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of results written.</returns>
    public int OverlapAABB(Vector2 min, Vector2 max, Span<OverlapHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
        {
            wasTruncated = false;
            return 0;
        }

        var aabb = new B2.AABB
        {
            lowerBound = new B2.Vec2 { x = min.X, y = min.Y },
            upperBound = new B2.Vec2 { x = max.X, y = max.Y }
        };
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        int count = OverlapAABBAllShapesSpanCore(aabb, f, results, out wasTruncated, filter?.ExcludeSensors ?? false, deduplicate: false, filter);
        return DeduplicateOverlapHitSpanByBody(results, count);
    }

    /// <inheritdoc cref="OverlapAABB(Vector2,Vector2,Span{OverlapHit},out bool,PhysicsQueryFilter?)"/>
    public int OverlapAABB(Vector2 min, Vector2 max, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
        => OverlapAABB(min, max, results, out _, filter);

    /// <summary>
    /// Returns all shape hits (one per shape) within the given axis-aligned bounding box,
    /// written into <paramref name="results"/>.
    /// </summary>
    /// <param name="min">Lower-left corner of the query AABB in pixel coordinates.</param>
    /// <param name="max">Upper-right corner of the query AABB in pixel coordinates.</param>
    /// <param name="results">Buffer to receive results.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the internal collection buffer was too small.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of results written.</returns>
    public int OverlapAABBShapes(Vector2 min, Vector2 max, Span<OverlapHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
        {
            wasTruncated = false;
            return 0;
        }

        var aabb = new B2.AABB
        {
            lowerBound = new B2.Vec2 { x = min.X, y = min.Y },
            upperBound = new B2.Vec2 { x = max.X, y = max.Y }
        };
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        return OverlapAABBAllShapesSpanCore(aabb, f, results, out wasTruncated, filter?.ExcludeSensors ?? false, deduplicate: false, filter);
    }

    /// <inheritdoc cref="OverlapAABBShapes(Vector2,Vector2,Span{OverlapHit},out bool,PhysicsQueryFilter?)"/>
    public int OverlapAABBShapes(Vector2 min, Vector2 max, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
        => OverlapAABBShapes(min, max, results, out _, filter);

    /// <summary>
    /// Returns all bodies (deduplicated by body) whose AABB intersects the given
    /// axis-aligned bounding box, adding them to <paramref name="results"/>.
    /// Clears <paramref name="results"/> before writing. Retries with a larger internal
    /// buffer if needed — guaranteed to return every hit.
    /// Use <see cref="OverlapAABBAllShapes"/> for per-shape granularity.
    /// </summary>
    /// <param name="min">Lower-left corner of the query AABB in pixel coordinates.</param>
    /// <param name="max">Upper-right corner of the query AABB in pixel coordinates.</param>
    /// <param name="results">List to receive results.</param>
    /// <param name="filter">Optional query filter.</param>
    public void OverlapAABBAll(Vector2 min, Vector2 max, List<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();
        var aabb = new B2.AABB
        {
            lowerBound = new B2.Vec2 { x = min.X, y = min.Y },
            upperBound = new B2.Vec2 { x = max.X, y = max.Y }
        };
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        OverlapAABBAllShapesCore(aabb, f, results, filter?.ExcludeSensors ?? false, filter);
        DeduplicateOverlapHitsByBody(results);
    }

    /// <summary>
    /// Returns all shape hits (one per shape) within the given axis-aligned bounding box,
    /// adding them to <paramref name="results"/>. Clears <paramref name="results"/> before
    /// writing. Retries with a larger internal buffer if needed — guaranteed to return every hit.
    /// </summary>
    /// <param name="min">Lower-left corner of the query AABB in pixel coordinates.</param>
    /// <param name="max">Upper-right corner of the query AABB in pixel coordinates.</param>
    /// <param name="results">List to receive results.</param>
    /// <param name="filter">Optional query filter.</param>
    public void OverlapAABBAllShapes(Vector2 min, Vector2 max, List<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();
        var aabb = new B2.AABB
        {
            lowerBound = new B2.Vec2 { x = min.X, y = min.Y },
            upperBound = new B2.Vec2 { x = max.X, y = max.Y }
        };
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        OverlapAABBAllShapesCore(aabb, f, results, filter?.ExcludeSensors ?? false, filter);
    }

    /// <summary>
    /// Returns the first overlapping hit at the given point, or <c>null</c> if nothing is hit.
    /// </summary>
    /// <param name="point">Query point in pixel coordinates.</param>
    /// <param name="filter">Optional query filter.</param>
    public OverlapHit? OverlapPointFirst(Vector2 point, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var b2Point = new B2.Vec2 { x = point.X, y = point.Y };
        var proxy = B2.MakeProxy(&b2Point, 1, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        return OverlapShapeFirstHitCore(&proxy, f, filter?.ExcludeSensors ?? false, filter);
    }

    /// <summary>
    /// Returns all bodies (deduplicated by body) overlapping the given point,
    /// written into <paramref name="results"/>. Use <see cref="OverlapPointShapes"/> for per-shape granularity.
    /// </summary>
    /// <param name="point">Query point in pixel coordinates.</param>
    /// <param name="results">Buffer to receive results.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the internal collection buffer was too small.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of results written.</returns>
    public int OverlapPoint(Vector2 point, Span<OverlapHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
        {
            wasTruncated = false;
            return 0;
        }

        var b2Point = new B2.Vec2 { x = point.X, y = point.Y };
        var proxy = B2.MakeProxy(&b2Point, 1, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        int count = OverlapShapeAllShapesSpanCore(&proxy, f, results, out wasTruncated, filter?.ExcludeSensors ?? false, deduplicate: false, filter);
        return DeduplicateOverlapHitSpanByBody(results, count);
    }

    /// <inheritdoc cref="OverlapPoint(Vector2,Span{OverlapHit},out bool,PhysicsQueryFilter?)"/>
    public int OverlapPoint(Vector2 point, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
        => OverlapPoint(point, results, out _, filter);

    /// <summary>
    /// Returns all shape hits (one per shape) at the given point,
    /// written into <paramref name="results"/>.
    /// </summary>
    /// <param name="point">Query point in pixel coordinates.</param>
    /// <param name="results">Buffer to receive results.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the internal collection buffer was too small.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of results written.</returns>
    public int OverlapPointShapes(Vector2 point, Span<OverlapHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
        {
            wasTruncated = false;
            return 0;
        }

        var b2Point = new B2.Vec2 { x = point.X, y = point.Y };
        var proxy = B2.MakeProxy(&b2Point, 1, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        return OverlapShapeAllShapesSpanCore(&proxy, f, results, out wasTruncated, filter?.ExcludeSensors ?? false, deduplicate: false, filter);
    }

    /// <inheritdoc cref="OverlapPointShapes(Vector2,Span{OverlapHit},out bool,PhysicsQueryFilter?)"/>
    public int OverlapPointShapes(Vector2 point, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
        => OverlapPointShapes(point, results, out _, filter);

    /// <summary>
    /// Returns all bodies (deduplicated by body) overlapping the given point,
    /// adding them to <paramref name="results"/>. Clears <paramref name="results"/> before writing.
    /// Retries with a larger internal buffer if needed — guaranteed to return every hit.
    /// Use <see cref="OverlapPointAllShapes"/> for per-shape granularity.
    /// </summary>
    /// <param name="point">Query point in pixel coordinates.</param>
    /// <param name="results">List to receive results.</param>
    /// <param name="filter">Optional query filter.</param>
    public void OverlapPointAll(Vector2 point, List<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();
        var b2Point = new B2.Vec2 { x = point.X, y = point.Y };
        var proxy = B2.MakeProxy(&b2Point, 1, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        OverlapAllShapesCore(&proxy, f, results, filter?.ExcludeSensors ?? false, filter);
        DeduplicateOverlapHitsByBody(results);
    }

    /// <summary>
    /// Returns all shape hits (one per shape) at the given point,
    /// adding them to <paramref name="results"/>. Clears <paramref name="results"/> before writing.
    /// Retries with a larger internal buffer if needed — guaranteed to return every hit.
    /// </summary>
    /// <param name="point">Query point in pixel coordinates.</param>
    /// <param name="results">List to receive results.</param>
    /// <param name="filter">Optional query filter.</param>
    public void OverlapPointAllShapes(Vector2 point, List<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();
        var b2Point = new B2.Vec2 { x = point.X, y = point.Y };
        var proxy = B2.MakeProxy(&b2Point, 1, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        OverlapAllShapesCore(&proxy, f, results, filter?.ExcludeSensors ?? false, filter);
    }

    /// <summary>
    /// Returns all bodies (deduplicated by body) overlapping the given circle,
    /// written into <paramref name="results"/>. Use <see cref="OverlapCircleShapes"/> for per-shape granularity.
    /// </summary>
    /// <param name="center">Circle center in pixel coordinates.</param>
    /// <param name="radius">Circle radius in pixels.</param>
    /// <param name="results">Buffer to receive results.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the internal collection buffer was too small.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of results written.</returns>
    public int OverlapCircle(Vector2 center, float radius, Span<OverlapHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
        {
            wasTruncated = false;
            return 0;
        }

        var b2Center = new B2.Vec2 { x = center.X, y = center.Y };
        var proxy = B2.MakeProxy(&b2Center, 1, radius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        int count = OverlapShapeAllShapesSpanCore(&proxy, f, results, out wasTruncated, filter?.ExcludeSensors ?? false, deduplicate: false, filter);
        return DeduplicateOverlapHitSpanByBody(results, count);
    }

    /// <inheritdoc cref="OverlapCircle(Vector2,float,Span{OverlapHit},out bool,PhysicsQueryFilter?)"/>
    public int OverlapCircle(Vector2 center, float radius, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
        => OverlapCircle(center, radius, results, out _, filter);

    /// <summary>
    /// Returns all shape hits (one per shape) overlapping the given circle,
    /// written into <paramref name="results"/>.
    /// </summary>
    /// <param name="center">Circle center in pixel coordinates.</param>
    /// <param name="radius">Circle radius in pixels.</param>
    /// <param name="results">Buffer to receive results.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the internal collection buffer was too small.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of results written.</returns>
    public int OverlapCircleShapes(Vector2 center, float radius, Span<OverlapHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
        {
            wasTruncated = false;
            return 0;
        }

        var b2Center = new B2.Vec2 { x = center.X, y = center.Y };
        var proxy = B2.MakeProxy(&b2Center, 1, radius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        return OverlapShapeAllShapesSpanCore(&proxy, f, results, out wasTruncated, filter?.ExcludeSensors ?? false, deduplicate: false, filter);
    }

    /// <inheritdoc cref="OverlapCircleShapes(Vector2,float,Span{OverlapHit},out bool,PhysicsQueryFilter?)"/>
    public int OverlapCircleShapes(Vector2 center, float radius, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
        => OverlapCircleShapes(center, radius, results, out _, filter);

    /// <summary>
    /// Returns all bodies (deduplicated by body) overlapping the given capsule,
    /// written into <paramref name="results"/>. Use <see cref="OverlapCapsuleShapes"/> for per-shape granularity.
    /// </summary>
    /// <param name="center1">First capsule center point in pixel coordinates.</param>
    /// <param name="center2">Second capsule center point in pixel coordinates.</param>
    /// <param name="radius">Capsule radius in pixels.</param>
    /// <param name="results">Buffer to receive results.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the internal collection buffer was too small.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of results written.</returns>
    public int OverlapCapsule(Vector2 center1, Vector2 center2, float radius, Span<OverlapHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
        {
            wasTruncated = false;
            return 0;
        }

        var pts = stackalloc B2.Vec2[2];
        pts[0] = new B2.Vec2 { x = center1.X, y = center1.Y };
        pts[1] = new B2.Vec2 { x = center2.X, y = center2.Y };
        var proxy = B2.MakeProxy(pts, 2, radius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        int count = OverlapShapeAllShapesSpanCore(&proxy, f, results, out wasTruncated, filter?.ExcludeSensors ?? false, deduplicate: false, filter);
        return DeduplicateOverlapHitSpanByBody(results, count);
    }

    /// <inheritdoc cref="OverlapCapsule(Vector2,Vector2,float,Span{OverlapHit},out bool,PhysicsQueryFilter?)"/>
    public int OverlapCapsule(Vector2 center1, Vector2 center2, float radius, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
        => OverlapCapsule(center1, center2, radius, results, out _, filter);

    /// <summary>
    /// Returns all shape hits (one per shape) overlapping the given capsule,
    /// written into <paramref name="results"/>.
    /// </summary>
    /// <param name="center1">First capsule center point in pixel coordinates.</param>
    /// <param name="center2">Second capsule center point in pixel coordinates.</param>
    /// <param name="radius">Capsule radius in pixels.</param>
    /// <param name="results">Buffer to receive results.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the internal collection buffer was too small.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of results written.</returns>
    public int OverlapCapsuleShapes(Vector2 center1, Vector2 center2, float radius, Span<OverlapHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
        {
            wasTruncated = false;
            return 0;
        }

        var pts = stackalloc B2.Vec2[2];
        pts[0] = new B2.Vec2 { x = center1.X, y = center1.Y };
        pts[1] = new B2.Vec2 { x = center2.X, y = center2.Y };
        var proxy = B2.MakeProxy(pts, 2, radius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        return OverlapShapeAllShapesSpanCore(&proxy, f, results, out wasTruncated, filter?.ExcludeSensors ?? false, deduplicate: false, filter);
    }

    /// <inheritdoc cref="OverlapCapsuleShapes(Vector2,Vector2,float,Span{OverlapHit},out bool,PhysicsQueryFilter?)"/>
    public int OverlapCapsuleShapes(Vector2 center1, Vector2 center2, float radius, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
        => OverlapCapsuleShapes(center1, center2, radius, results, out _, filter);

    /// <summary>
    /// Returns all bodies (deduplicated by body) overlapping the given oriented box,
    /// written into <paramref name="results"/>. Use <see cref="OverlapBoxShapes"/> for per-shape granularity.
    /// </summary>
    /// <param name="center">Box center in pixel coordinates.</param>
    /// <param name="halfWidth">Half-width of the box in pixels.</param>
    /// <param name="halfHeight">Half-height of the box in pixels.</param>
    /// <param name="angle">Rotation of the box in radians.</param>
    /// <param name="results">Buffer to receive results.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the internal collection buffer was too small.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of results written.</returns>
    public int OverlapBox(Vector2 center, float halfWidth, float halfHeight, float angle, Span<OverlapHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
        {
            wasTruncated = false;
            return 0;
        }

        var b2Center = new B2.Vec2 { x = center.X, y = center.Y };
        var box = B2.MakeOffsetBox(halfWidth, halfHeight, b2Center, B2.MakeRot(angle));
        var corners = stackalloc B2.Vec2[4];
        corners[0] = box.vertices[0]; corners[1] = box.vertices[1];
        corners[2] = box.vertices[2]; corners[3] = box.vertices[3];
        var proxy = B2.MakeProxy(corners, 4, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        int count = OverlapShapeAllShapesSpanCore(&proxy, f, results, out wasTruncated, filter?.ExcludeSensors ?? false, deduplicate: false, filter);
        return DeduplicateOverlapHitSpanByBody(results, count);
    }

    /// <inheritdoc cref="OverlapBox(Vector2,float,float,float,Span{OverlapHit},out bool,PhysicsQueryFilter?)"/>
    public int OverlapBox(Vector2 center, float halfWidth, float halfHeight, float angle, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
        => OverlapBox(center, halfWidth, halfHeight, angle, results, out _, filter);

    /// <summary>
    /// Returns all shape hits (one per shape) overlapping the given oriented box,
    /// written into <paramref name="results"/>.
    /// </summary>
    /// <param name="center">Box center in pixel coordinates.</param>
    /// <param name="halfWidth">Half-width of the box in pixels.</param>
    /// <param name="halfHeight">Half-height of the box in pixels.</param>
    /// <param name="angle">Rotation of the box in radians.</param>
    /// <param name="results">Buffer to receive results.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the internal collection buffer was too small.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of results written.</returns>
    public int OverlapBoxShapes(Vector2 center, float halfWidth, float halfHeight, float angle, Span<OverlapHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0)
        {
            wasTruncated = false;
            return 0;
        }

        var b2Center = new B2.Vec2 { x = center.X, y = center.Y };
        var box = B2.MakeOffsetBox(halfWidth, halfHeight, b2Center, B2.MakeRot(angle));
        var corners = stackalloc B2.Vec2[4];
        corners[0] = box.vertices[0]; corners[1] = box.vertices[1];
        corners[2] = box.vertices[2]; corners[3] = box.vertices[3];
        var proxy = B2.MakeProxy(corners, 4, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        return OverlapShapeAllShapesSpanCore(&proxy, f, results, out wasTruncated, filter?.ExcludeSensors ?? false, deduplicate: false, filter);
    }

    /// <inheritdoc cref="OverlapBoxShapes(Vector2,float,float,float,Span{OverlapHit},out bool,PhysicsQueryFilter?)"/>
    public int OverlapBoxShapes(Vector2 center, float halfWidth, float halfHeight, float angle, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
        => OverlapBoxShapes(center, halfWidth, halfHeight, angle, results, out _, filter);

    /// <summary>
    /// Returns the first overlapping hit within the given convex polygon, or <c>null</c> if none.
    /// Vertices are in world space (3–8 vertices).
    /// </summary>
    /// <param name="vertices">Convex polygon vertices in world space.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when vertex count is outside 3–8.</exception>
    public OverlapHit? OverlapPolygonFirst(ReadOnlySpan<Vector2> vertices, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (vertices.Length < 3 || vertices.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(vertices), "Polygon overlap requires 3-8 vertices.");

        var b2Verts = stackalloc B2.Vec2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            b2Verts[i] = new B2.Vec2 { x = vertices[i].X, y = vertices[i].Y };

        var proxy = B2.MakeProxy(b2Verts, vertices.Length, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        return OverlapShapeFirstHitCore(&proxy, f, filter?.ExcludeSensors ?? false, filter);
    }

    /// <summary>
    /// Returns all bodies (deduplicated by body) overlapping the given convex polygon,
    /// written into <paramref name="results"/>. Vertices are in world space (3–8 vertices).
    /// Use <see cref="OverlapPolygonShapes"/> for per-shape granularity.
    /// </summary>
    /// <param name="vertices">Convex polygon vertices in world space.</param>
    /// <param name="results">Buffer to receive results.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the internal collection buffer was too small.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of results written.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when vertex count is outside 3–8.</exception>
    public int OverlapPolygon(ReadOnlySpan<Vector2> vertices, Span<OverlapHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (vertices.Length < 3 || vertices.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(vertices), "Polygon overlap requires 3-8 vertices.");

        if (results.Length == 0)
        {
            wasTruncated = false;
            return 0;
        }

        var b2Verts = stackalloc B2.Vec2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            b2Verts[i] = new B2.Vec2 { x = vertices[i].X, y = vertices[i].Y };

        var proxy = B2.MakeProxy(b2Verts, vertices.Length, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        int count = OverlapShapeAllShapesSpanCore(&proxy, f, results, out wasTruncated, filter?.ExcludeSensors ?? false, deduplicate: true, filter);
        return DeduplicateOverlapHitSpanByBody(results, count);
    }

    /// <inheritdoc cref="OverlapPolygon(ReadOnlySpan{Vector2},Span{OverlapHit},out bool,PhysicsQueryFilter?)"/>
    public int OverlapPolygon(ReadOnlySpan<Vector2> vertices, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
        => OverlapPolygon(vertices, results, out _, filter);

    /// <summary>
    /// Returns all shape hits (one per shape) overlapping the given convex polygon,
    /// written into <paramref name="results"/>. Vertices are in world space (3–8 vertices).
    /// </summary>
    /// <param name="vertices">Convex polygon vertices in world space.</param>
    /// <param name="results">Buffer to receive results.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the internal collection buffer was too small.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of results written.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when vertex count is outside 3–8.</exception>
    public int OverlapPolygonShapes(ReadOnlySpan<Vector2> vertices, Span<OverlapHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (vertices.Length < 3 || vertices.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(vertices), "Polygon overlap requires 3-8 vertices.");

        if (results.Length == 0)
        {
            wasTruncated = false;
            return 0;
        }

        var b2Verts = stackalloc B2.Vec2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            b2Verts[i] = new B2.Vec2 { x = vertices[i].X, y = vertices[i].Y };

        var proxy = B2.MakeProxy(b2Verts, vertices.Length, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        return OverlapShapeAllShapesSpanCore(&proxy, f, results, out wasTruncated, filter?.ExcludeSensors ?? false, deduplicate: false, filter);
    }

    /// <inheritdoc cref="OverlapPolygonShapes(ReadOnlySpan{Vector2},Span{OverlapHit},out bool,PhysicsQueryFilter?)"/>
    public int OverlapPolygonShapes(ReadOnlySpan<Vector2> vertices, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
        => OverlapPolygonShapes(vertices, results, out _, filter);

    /// <summary>
    /// Returns all bodies (deduplicated by body) overlapping the given convex polygon
    /// into <paramref name="results"/>. Clears <paramref name="results"/> before writing.
    /// Retries with a larger internal buffer if needed — guaranteed to return every hit.
    /// Vertices are in world space (3–8 vertices).
    /// Use <see cref="OverlapPolygonAllShapes"/> for per-shape granularity.
    /// </summary>
    public void OverlapPolygonAll(ReadOnlySpan<Vector2> vertices, List<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();

        if (vertices.Length < 3 || vertices.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(vertices), "Polygon overlap requires 3-8 vertices.");

        var b2Verts = stackalloc B2.Vec2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            b2Verts[i] = new B2.Vec2 { x = vertices[i].X, y = vertices[i].Y };

        var proxy = B2.MakeProxy(b2Verts, vertices.Length, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        OverlapAllShapesCore(&proxy, f, results, filter?.ExcludeSensors ?? false, filter);
        DeduplicateOverlapHitsByBody(results);
    }

    /// <summary>
    /// Returns all shape hits (one per shape) overlapping the given convex polygon
    /// into <paramref name="results"/>. Clears <paramref name="results"/> before writing.
    /// Vertices are in world space (3–8 vertices).
    /// </summary>
    public void OverlapPolygonAllShapes(ReadOnlySpan<Vector2> vertices, List<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();

        if (vertices.Length < 3 || vertices.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(vertices), "Polygon overlap requires 3-8 vertices.");

        var b2Verts = stackalloc B2.Vec2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            b2Verts[i] = new B2.Vec2 { x = vertices[i].X, y = vertices[i].Y };

        var proxy = B2.MakeProxy(b2Verts, vertices.Length, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        OverlapAllShapesCore(&proxy, f, results, filter?.ExcludeSensors ?? false, filter);
    }

    /// <summary>
    /// Returns all bodies (deduplicated by body) whose AABB overlaps
    /// <paramref name="body"/>'s AABB, excluding <paramref name="body"/> itself,
    /// written into <paramref name="results"/>.
    /// </summary>
    /// <remarks>
    /// This is a <b>broad-phase AABB test only</b> — shapes are not tested for exact overlap.
    /// Use <see cref="OverlapBody(PhysicsBodyComponent, Span{OverlapHit}, PhysicsQueryFilter?)"/>
    /// for shape-exact results. Use <see cref="OverlapBodyAABBShapes"/> for per-shape granularity.
    /// </remarks>
    /// <param name="wasTruncated">Set to <c>true</c> when the internal collection buffer was too small.</param>
    /// <returns>Number of results written.</returns>
    public int OverlapBodyAABB(PhysicsBodyComponent body, Span<OverlapHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0 || !B2.BodyIsValid(body.BodyId))
        {
            wasTruncated = false;
            return 0;
        }

        var aabb = B2.BodyComputeAABB(body.BodyId);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        int count = OverlapAABBAllShapesSpanCore(aabb, f, results, out wasTruncated, filter?.ExcludeSensors ?? false, deduplicate: true, filter);
        count = DeduplicateOverlapHitSpanByBody(results, count);

        int write = 0;
        for (int i = 0; i < count; i++)
        {
            if (results[i].Component != body)
                results[write++] = results[i];
        }
        return write;
    }

    /// <inheritdoc cref="OverlapBodyAABB(PhysicsBodyComponent,Span{OverlapHit},out bool,PhysicsQueryFilter?)"/>
    public int OverlapBodyAABB(PhysicsBodyComponent body, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
        => OverlapBodyAABB(body, results, out _, filter);

    /// <summary>
    /// Returns all shape hits (one per shape) whose AABB intersects
    /// <paramref name="body"/>'s AABB, excluding <paramref name="body"/> itself,
    /// written into <paramref name="results"/>.
    /// </summary>
    /// <remarks>This is a <b>broad-phase AABB test only</b>.</remarks>
    /// <param name="wasTruncated">Set to <c>true</c> when the internal collection buffer was too small.</param>
    /// <returns>Number of results written.</returns>
    public int OverlapBodyAABBShapes(PhysicsBodyComponent body, Span<OverlapHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0 || !B2.BodyIsValid(body.BodyId))
        {
            wasTruncated = false;
            return 0;
        }

        var aabb = B2.BodyComputeAABB(body.BodyId);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        int count = OverlapAABBAllShapesSpanCore(aabb, f, results, out wasTruncated, filter?.ExcludeSensors ?? false, deduplicate: false, filter);

        int write = 0;
        for (int i = 0; i < count; i++)
        {
            if (results[i].Component != body)
                results[write++] = results[i];
        }
        return write;
    }

    /// <inheritdoc cref="OverlapBodyAABBShapes(PhysicsBodyComponent,Span{OverlapHit},out bool,PhysicsQueryFilter?)"/>
    public int OverlapBodyAABBShapes(PhysicsBodyComponent body, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
        => OverlapBodyAABBShapes(body, results, out _, filter);

    /// <inheritdoc cref="ShapeCastAll(Vector2,float,Vector2,float,Span{ShapeCastHit},out bool,PhysicsQueryFilter?)"/>
    public int ShapeCastAll(Vector2 origin, float radius, Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
        => ShapeCastAll(origin, radius, direction, maxDistance, results, out _, filter);

    /// <summary>
    /// Sweeps a capsule along a direction and returns all hits sorted by distance,
    /// deduplicated by body (one hit per body, nearest shape wins).
    /// </summary>
    /// <param name="center1">First capsule center point at the start of the sweep in pixel coordinates.</param>
    /// <param name="center2">Second capsule center point at the start of the sweep in pixel coordinates.</param>
    /// <param name="radius">Capsule radius in pixels.</param>
    /// <param name="direction">Sweep direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum sweep distance in pixels.</param>
    /// <param name="results">Buffer to receive hits, sorted nearest-first.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the internal buffer was too small.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of hits written to <paramref name="results"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> is zero.</exception>
    public int ShapeCastAll(Vector2 center1, Vector2 center2, float radius, Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        wasTruncated = false;

        if (results.Length == 0)
            return 0;

        var pts = stackalloc B2.Vec2[2];
        pts[0] = new B2.Vec2 { x = center1.X, y = center1.Y };
        pts[1] = new B2.Vec2 { x = center2.X, y = center2.Y };
        var proxy = B2.MakeProxy(pts, 2, radius);
        return ShapeCastAllCore(&proxy, direction, maxDistance, results, out wasTruncated, filter, deduplicate: true);
    }

    /// <inheritdoc cref="ShapeCastAll(Vector2,Vector2,float,Vector2,float,Span{ShapeCastHit},out bool,PhysicsQueryFilter?)"/>
    public int ShapeCastAll(Vector2 center1, Vector2 center2, float radius, Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
        => ShapeCastAll(center1, center2, radius, direction, maxDistance, results, out _, filter);

    /// <summary>
    /// Sweeps an oriented box along a direction and returns all hits sorted by distance,
    /// deduplicated by body (one hit per body, nearest shape wins).
    /// </summary>
    /// <param name="origin">Center of the box at the start of the sweep in pixel coordinates.</param>
    /// <param name="halfWidth">Half-width of the box in pixels.</param>
    /// <param name="halfHeight">Half-height of the box in pixels.</param>
    /// <param name="angle">Rotation of the box in radians.</param>
    /// <param name="direction">Sweep direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum sweep distance in pixels.</param>
    /// <param name="results">Buffer to receive hits, sorted nearest-first.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the internal buffer was too small.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of hits written to <paramref name="results"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> is zero.</exception>
    public int ShapeCastAll(Vector2 origin, float halfWidth, float halfHeight, float angle,
        Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        wasTruncated = false;

        if (results.Length == 0)
            return 0;

        var b2Center = new B2.Vec2 { x = origin.X, y = origin.Y };
        var box = B2.MakeOffsetBox(halfWidth, halfHeight, b2Center, B2.MakeRot(angle));
        var corners = stackalloc B2.Vec2[4];
        corners[0] = box.vertices[0];
        corners[1] = box.vertices[1];
        corners[2] = box.vertices[2];
        corners[3] = box.vertices[3];
        var proxy = B2.MakeProxy(corners, 4, 0f);
        return ShapeCastAllCore(&proxy, direction, maxDistance, results, out wasTruncated, filter, deduplicate: true);
    }

    /// <inheritdoc cref="ShapeCastAll(Vector2,float,float,float,Vector2,float,Span{ShapeCastHit},out bool,PhysicsQueryFilter?)"/>
    public int ShapeCastAll(Vector2 origin, float halfWidth, float halfHeight, float angle,
        Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
        => ShapeCastAll(origin, halfWidth, halfHeight, angle, direction, maxDistance, results, out _, filter);

    /// <summary>
    /// Sweeps a convex polygon along a direction and returns all hits sorted by distance,
    /// deduplicated by body (one hit per body, nearest shape wins).
    /// Vertices are in world space at the start of the sweep.
    /// </summary>
    /// <param name="vertices">Convex polygon vertices in world space (3–8 vertices).</param>
    /// <param name="direction">Sweep direction (does not need to be normalized). Must be non-zero.</param>
    /// <param name="maxDistance">Maximum sweep distance in pixels.</param>
    /// <param name="results">Buffer to receive hits, sorted nearest-first.</param>
    /// <param name="wasTruncated">Set to <c>true</c> when the internal buffer was too small.</param>
    /// <param name="filter">Optional query filter.</param>
    /// <returns>Number of hits written to <paramref name="results"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="direction"/> is zero.</exception>
    public int ShapeCastAll(ReadOnlySpan<Vector2> vertices, Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        wasTruncated = false;

        if (results.Length == 0)
            return 0;

        if (vertices.Length < 3 || vertices.Length > 8)
            throw new ArgumentOutOfRangeException(nameof(vertices), "Polygon shape cast requires 3-8 vertices.");

        var b2Verts = stackalloc B2.Vec2[vertices.Length];
        for (int i = 0; i < vertices.Length; i++)
            b2Verts[i] = new B2.Vec2 { x = vertices[i].X, y = vertices[i].Y };

        var proxy = B2.MakeProxy(b2Verts, vertices.Length, 0f);
        return ShapeCastAllCore(&proxy, direction, maxDistance, results, out wasTruncated, filter, deduplicate: true);
    }

    /// <inheritdoc cref="ShapeCastAll(ReadOnlySpan{Vector2},Vector2,float,Span{ShapeCastHit},out bool,PhysicsQueryFilter?)"/>
    public int ShapeCastAll(ReadOnlySpan<Vector2> vertices, Vector2 direction, float maxDistance,
        Span<ShapeCastHit> results, PhysicsQueryFilter? filter = null)
        => ShapeCastAll(vertices, direction, maxDistance, results, out _, filter);

    private int ShapeCastAllCore(B2.ShapeProxy* proxy, Vector2 direction, float maxDistance,
    Span<ShapeCastHit> results, out bool wasTruncated, PhysicsQueryFilter? filter, bool deduplicate)
    {
        wasTruncated = false;

        if (results.Length == 0)
            return 0;

        var norm = NormalizeDirection(direction);
        var translation = new B2.Vec2 { x = norm.X * maxDistance, y = norm.Y * maxDistance };
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();

        int rawCapacity = deduplicate ? Math.Max(results.Length * 8, 16) : Math.Max(results.Length + 1, 16);
        var rawArray = ArrayPool<ShapeCastHit>.Shared.Rent(rawCapacity);
        try
        {
            fixed (ShapeCastHit* ptr = rawArray)
            {
                var ctx = new ShapeCastAllContext { Buffer = ptr, Capacity = rawCapacity, Count = 0 };
                B2.WorldCastShape(_worldId, proxy, translation, f, &ShapeCastAllCallback, &ctx);
                int raw = ctx.Count;

                new Span<ShapeCastHit>(ptr, raw).Sort((a, b) => a.Fraction.CompareTo(b.Fraction));

                int count = deduplicate ? DeduplicateShapeCastByBody(ptr, raw) : raw;
                int written = 0;
                for (int i = 0; i < count && written < results.Length; i++)
                {
                    if (filter?.ExcludeSensors == true && B2.ShapeIsSensor(ptr[i].ShapeId)) continue;
                    if (ShouldExcludeShape(ptr[i].ShapeId, filter)) continue;
                    var h = ptr[i];
                    var resolved = ResolveHitFromShapeId(h.ShapeId);
                    results[written++] = new ShapeCastHit
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

                wasTruncated = raw >= rawCapacity;
                return written;
            }
        }
        finally
        {
            ArrayPool<ShapeCastHit>.Shared.Return(rawArray, clearArray: true);
        }
    }

    /// <summary>
    /// Tests a circle against the world and returns the first overlapping hit, or <c>null</c>.
    /// </summary>
    /// <param name="center">Circle center in pixel coordinates.</param>
    /// <param name="radius">Circle radius in pixels.</param>
    /// <param name="filter">Optional query filter.</param>
    public OverlapHit? OverlapCircleFirst(Vector2 center, float radius, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var b2Center = new B2.Vec2 { x = center.X, y = center.Y };
        var proxy = B2.MakeProxy(&b2Center, 1, radius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        return OverlapShapeFirstHitCore(&proxy, f, filter?.ExcludeSensors ?? false, filter);
    }

    /// <summary>
    /// Tests a circle against the world and returns all overlapping bodies (deduplicated by body)
    /// into <paramref name="results"/>. Clears <paramref name="results"/> before writing.
    /// Retries with a larger internal buffer if needed — guaranteed to return every hit.
    /// Use <see cref="OverlapCircleAllShapes"/> for per-shape granularity.
    /// </summary>
    /// <param name="center">Circle center in pixel coordinates.</param>
    /// <param name="radius">Circle radius in pixels.</param>
    /// <param name="results">List to receive results.</param>
    /// <param name="filter">Optional query filter.</param>
    public void OverlapCircleAll(Vector2 center, float radius, List<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();
        var b2Center = new B2.Vec2 { x = center.X, y = center.Y };
        var proxy = B2.MakeProxy(&b2Center, 1, radius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        OverlapAllShapesCore(&proxy, f, results, filter?.ExcludeSensors ?? false, filter);
        DeduplicateOverlapHitsByBody(results);
    }

    /// <summary>
    /// Computes the minimum separation distance (or penetration depth) between two live bodies.
    /// Iterates over every shape pair across the two bodies and returns the result for the
    /// closest (or most-overlapping) pair.
    /// </summary>
    /// <param name="a">First body.</param>
    /// <param name="b">Second body.</param>
    /// <param name="pointOnA">Closest point on body A in pixel coordinates, or <see cref="Vector2.Zero"/> when either body is not live.</param>
    /// <param name="pointOnB">Closest point on body B in pixel coordinates, or <see cref="Vector2.Zero"/> when either body is not live.</param>
    /// <returns>
    /// Positive values indicate separation (pixels); negative values indicate overlap/penetration.
    /// Returns <see cref="float.MaxValue"/> when either body has no live shapes.
    /// </returns>
    public float GetShapeDistance(PhysicsBodyComponent a, PhysicsBodyComponent b,
    out Vector2 pointOnA, out Vector2 pointOnB)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        pointOnA = Vector2.Zero;
        pointOnB = Vector2.Zero;

        if (!B2.BodyIsValid(a.BodyId) || !B2.BodyIsValid(b.BodyId))
            return float.MaxValue;

        int countA = B2.BodyGetShapeCount(a.BodyId);
        int countB = B2.BodyGetShapeCount(b.BodyId);
        if (countA == 0 || countB == 0) return float.MaxValue;

        var shapesA = ArrayPool<B2.ShapeId>.Shared.Rent(countA);
        var shapesB = ArrayPool<B2.ShapeId>.Shared.Rent(countB);
        try
        {
            fixed (B2.ShapeId* pA = shapesA)
                B2.BodyGetShapes(a.BodyId, pA, countA);
            fixed (B2.ShapeId* pB = shapesB)
                B2.BodyGetShapes(b.BodyId, pB, countB);

            float best = float.MaxValue;
            var cache = new B2.SimplexCache();
            var input = new B2.DistanceInput { useRadii = true };

            for (int i = 0; i < countA; i++)
            {
                if (!B2.ShapeIsValid(shapesA[i])) continue;
                BuildWorldSpaceProxy(shapesA[i], out var proxyA, out bool validA);
                if (!validA) continue;
                input.proxyA = proxyA;
                input.transformA = B2.BodyGetTransform(a.BodyId);

                for (int j = 0; j < countB; j++)
                {
                    if (!B2.ShapeIsValid(shapesB[j])) continue;
                    BuildWorldSpaceProxy(shapesB[j], out var proxyB, out bool validB);
                    if (!validB) continue;
                    input.proxyB = proxyB;
                    input.transformB = B2.BodyGetTransform(b.BodyId);

                    cache = new B2.SimplexCache();
                    var output = B2.ShapeDistance(&input, &cache, null, 0);
                    if (output.distance < best)
                    {
                        best = output.distance;
                        pointOnA = new Vector2(output.pointA.x, output.pointA.y);
                        pointOnB = new Vector2(output.pointB.x, output.pointB.y);
                    }
                }
            }

            return best;
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(shapesA);
            ArrayPool<B2.ShapeId>.Shared.Return(shapesB);
        }
    }

    /// <inheritdoc cref="GetShapeDistance(PhysicsBodyComponent,PhysicsBodyComponent,out Vector2,out Vector2)"/>
    public float GetShapeDistance(PhysicsBodyComponent a, PhysicsBodyComponent b)
        => GetShapeDistance(a, b, out _, out _);

    /// <summary>
    /// Computes the minimum separation distance between two live bodies and returns a
    /// separation normal pointing from <paramref name="b"/> toward <paramref name="a"/> —
    /// i.e. the direction to push <paramref name="a"/> to resolve the overlap (MTV direction).
    /// </summary>
    /// <param name="a">First body.</param>
    /// <param name="b">Second body.</param>
    /// <param name="pointOnA">Closest point on body A in pixel coordinates.</param>
    /// <param name="pointOnB">Closest point on body B in pixel coordinates.</param>
    /// <param name="separationNormal">
    /// Unit vector from <paramref name="b"/> toward <paramref name="a"/> at the closest feature
    /// pair. Falls back to <see cref="Vector2.UnitY"/> when the shapes are exactly coincident
    /// or either body is not live.
    /// </param>
    /// <returns>
    /// Positive values indicate separation (pixels); negative values indicate penetration depth.
    /// Returns <see cref="float.MaxValue"/> when either body has no live shapes.
    /// </returns>
    public float GetShapeDistance(PhysicsBodyComponent a, PhysicsBodyComponent b,
        out Vector2 pointOnA, out Vector2 pointOnB, out Vector2 separationNormal)
    {
        float dist = GetShapeDistance(a, b, out pointOnA, out pointOnB);
        if (dist >= float.MaxValue)
        {
            separationNormal = Vector2.UnitY;
            return dist;
        }

        var delta = pointOnA - pointOnB;
        float len = delta.Length();
        separationNormal = len > 1e-6f ? delta / len : Vector2.UnitY;
        return dist;
    }

    /// <summary>
    /// Applies an outward radial impulse to all dynamic bodies within <paramref name="radius"/> pixels
    /// of <paramref name="center"/>. Impulse magnitude falls off linearly with distance from center,
    /// reaching zero at the edge of the radius.
    /// </summary>
    /// <remarks>
    /// Must be called from the simulation thread (inside a FixedUpdate handler).
    /// Static and kinematic bodies are unaffected. Sensor-only bodies receive the impulse
    /// but it has no visible effect (Box2D ignores impulses on non-dynamic bodies).
    /// </remarks>
    /// <param name="center">Explosion origin in pixel coordinates.</param>
    /// <param name="radius">Blast radius in pixels. Must be greater than zero.</param>
    /// <param name="force">Peak impulse magnitude at the center, in pixel mass units (mass × pixels/s).</param>
    /// <param name="falloff">
    ///     Exponent applied to the normalized distance for impulse falloff.
    ///     1 = linear (default), 2 = quadratic (sharp drop-off), 0.5 = gentle roll-off.
    /// </param>
    /// <param name="filter">Optional query filter to restrict which bodies are affected.</param>
    /// <exception cref="ArgumentOutOfRangeException">
    ///     Thrown when <paramref name="radius"/> or <paramref name="force"/> is not positive,
    ///     or <paramref name="falloff"/> is negative.
    /// </exception>
    public void ApplyExplosionImpulse(Vector2 center, float radius, float force,
        float falloff = 1f, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(radius, 0f);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(force, 0f);
        ArgumentOutOfRangeException.ThrowIfNegative(falloff);
        AssertSimulationThread();

        OverlapCircleAll(center, radius, _explosionHitsBuffer, filter);

        foreach (var hit in _explosionHitsBuffer)
        {
            var body = hit.Component;
            if (body == null || !B2.BodyIsValid(body.BodyId)) continue;
            if (B2.BodyGetType(body.BodyId) != B2.BodyType.dynamicBody) continue;

            var bodyPos = B2.BodyGetPosition(body.BodyId);
            var toBody = new Vector2(bodyPos.x, bodyPos.y) - center;
            float distance = toBody.Length();

            if (distance < float.Epsilon) continue;

            float normalized = distance / radius;
            float scale = force * (1f - MathF.Pow(normalized, falloff));
            if (scale <= 0f) continue;

            var direction = toBody / distance;
            B2.BodyApplyLinearImpulseToCenter(body.BodyId,
                new B2.Vec2 { x = direction.X * scale, y = direction.Y * scale }, true);
        }
    }

    /// <summary>
    /// Tests a circle against the world and returns all overlapping shape hits (one per shape)
    /// into <paramref name="results"/>. Clears <paramref name="results"/> before writing.
    /// </summary>
    /// <param name="center">Circle center in pixel coordinates.</param>
    /// <param name="radius">Circle radius in pixels.</param>
    /// <param name="results">List to receive results.</param>
    /// <param name="filter">Optional query filter.</param>
    public void OverlapCircleAllShapes(Vector2 center, float radius, List<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();
        var b2Center = new B2.Vec2 { x = center.X, y = center.Y };
        var proxy = B2.MakeProxy(&b2Center, 1, radius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        OverlapAllShapesCore(&proxy, f, results, filter?.ExcludeSensors ?? false, filter);
    }

    /// <summary>
    /// Tests an oriented box against the world and returns the first overlapping hit, or <c>null</c>.
    /// </summary>
    /// <param name="center">Box center in pixel coordinates.</param>
    /// <param name="halfWidth">Half-width of the box in pixels.</param>
    /// <param name="halfHeight">Half-height of the box in pixels.</param>
    /// <param name="angle">Rotation of the box in radians.</param>
    /// <param name="filter">Optional query filter.</param>
    public OverlapHit? OverlapBoxFirst(Vector2 center, float halfWidth, float halfHeight, float angle, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var b2Center = new B2.Vec2 { x = center.X, y = center.Y };
        var box = B2.MakeOffsetBox(halfWidth, halfHeight, b2Center, B2.MakeRot(angle));
        var corners = stackalloc B2.Vec2[4];
        corners[0] = box.vertices[0]; corners[1] = box.vertices[1];
        corners[2] = box.vertices[2]; corners[3] = box.vertices[3];
        var proxy = B2.MakeProxy(corners, 4, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        return OverlapShapeFirstHitCore(&proxy, f, filter?.ExcludeSensors ?? false, filter);
    }

    /// <summary>
    /// Tests an oriented box against the world and returns all overlapping bodies (deduplicated by body)
    /// into <paramref name="results"/>. Clears <paramref name="results"/> before writing.
    /// Retries with a larger internal buffer if needed — guaranteed to return every hit.
    /// Use <see cref="OverlapBoxAllShapes"/> for per-shape granularity.
    /// </summary>
    /// <param name="center">Box center in pixel coordinates.</param>
    /// <param name="halfWidth">Half-width of the box in pixels.</param>
    /// <param name="halfHeight">Half-height of the box in pixels.</param>
    /// <param name="angle">Rotation of the box in radians.</param>
    /// <param name="results">List to receive results.</param>
    /// <param name="filter">Optional query filter.</param>
    public void OverlapBoxAll(Vector2 center, float halfWidth, float halfHeight, float angle, List<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();
        var b2Center = new B2.Vec2 { x = center.X, y = center.Y };
        var box = B2.MakeOffsetBox(halfWidth, halfHeight, b2Center, B2.MakeRot(angle));
        var corners = stackalloc B2.Vec2[4];
        corners[0] = box.vertices[0]; corners[1] = box.vertices[1];
        corners[2] = box.vertices[2]; corners[3] = box.vertices[3];
        var proxy = B2.MakeProxy(corners, 4, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        OverlapAllShapesCore(&proxy, f, results, filter?.ExcludeSensors ?? false, filter);
        DeduplicateOverlapHitsByBody(results);
    }

    /// <summary>
    /// Tests an oriented box against the world and returns all overlapping shape hits (one per shape)
    /// into <paramref name="results"/>. Clears <paramref name="results"/> before writing.
    /// </summary>
    /// <param name="center">Box center in pixel coordinates.</param>
    /// <param name="halfWidth">Half-width of the box in pixels.</param>
    /// <param name="halfHeight">Half-height of the box in pixels.</param>
    /// <param name="angle">Rotation of the box in radians.</param>
    /// <param name="results">List to receive results.</param>
    /// <param name="filter">Optional query filter.</param>
    public void OverlapBoxAllShapes(Vector2 center, float halfWidth, float halfHeight, float angle, List<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();
        var b2Center = new B2.Vec2 { x = center.X, y = center.Y };
        var box = B2.MakeOffsetBox(halfWidth, halfHeight, b2Center, B2.MakeRot(angle));
        var corners = stackalloc B2.Vec2[4];
        corners[0] = box.vertices[0]; corners[1] = box.vertices[1];
        corners[2] = box.vertices[2]; corners[3] = box.vertices[3];
        var proxy = B2.MakeProxy(corners, 4, 0f);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        OverlapAllShapesCore(&proxy, f, results, filter?.ExcludeSensors ?? false, filter);
    }

    /// <summary>
    /// Tests a capsule against the world and returns the first overlapping hit, or <c>null</c>.
    /// </summary>
    /// <param name="center1">First capsule center point in pixel coordinates.</param>
    /// <param name="center2">Second capsule center point in pixel coordinates.</param>
    /// <param name="radius">Capsule radius in pixels.</param>
    /// <param name="filter">Optional query filter.</param>
    public OverlapHit? OverlapCapsuleFirst(Vector2 center1, Vector2 center2, float radius, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        var pts = stackalloc B2.Vec2[2];
        pts[0] = new B2.Vec2 { x = center1.X, y = center1.Y };
        pts[1] = new B2.Vec2 { x = center2.X, y = center2.Y };
        var proxy = B2.MakeProxy(pts, 2, radius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        return OverlapShapeFirstHitCore(&proxy, f, filter?.ExcludeSensors ?? false, filter);
    }

    /// <summary>
    /// Returns all bodies (deduplicated by body) overlapping the given capsule,
    /// adding them to <paramref name="results"/>. Clears <paramref name="results"/> before
    /// writing. Retries with a larger internal buffer if needed — guaranteed to return every hit.
    /// Use <see cref="OverlapCapsuleAllShapes(Vector2,Vector2,float,List{OverlapHit},PhysicsQueryFilter?)"/>
    /// for per-shape granularity.
    /// </summary>
    /// <param name="center1">First capsule center point in pixel coordinates.</param>
    /// <param name="center2">Second capsule center point in pixel coordinates.</param>
    /// <param name="radius">Capsule radius in pixels.</param>
    /// <param name="results">List to receive results.</param>
    /// <param name="filter">Optional query filter.</param>
    public void OverlapCapsuleAll(Vector2 center1, Vector2 center2, float radius, List<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();
        var pts = stackalloc B2.Vec2[2];
        pts[0] = new B2.Vec2 { x = center1.X, y = center1.Y };
        pts[1] = new B2.Vec2 { x = center2.X, y = center2.Y };
        var proxy = B2.MakeProxy(pts, 2, radius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        OverlapAllShapesCore(&proxy, f, results, filter?.ExcludeSensors ?? false, filter);
        DeduplicateOverlapHitsByBody(results);
    }

    /// <summary>
    /// Tests a capsule against the world and returns all overlapping shape hits (one per shape)
    /// into <paramref name="results"/>. Clears <paramref name="results"/> before writing.
    /// </summary>
    /// <param name="center1">First capsule center point in pixel coordinates.</param>
    /// <param name="center2">Second capsule center point in pixel coordinates.</param>
    /// <param name="radius">Capsule radius in pixels.</param>
    /// <param name="results">List to receive results.</param>
    /// <param name="filter">Optional query filter.</param>
    public void OverlapCapsuleAllShapes(Vector2 center1, Vector2 center2, float radius, List<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();
        var pts = stackalloc B2.Vec2[2];
        pts[0] = new B2.Vec2 { x = center1.X, y = center1.Y };
        pts[1] = new B2.Vec2 { x = center2.X, y = center2.Y };
        var proxy = B2.MakeProxy(pts, 2, radius);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        OverlapAllShapesCore(&proxy, f, results, filter?.ExcludeSensors ?? false, filter);
    }

    /// <summary>
    /// Returns the first overlapping hit whose shapes exactly overlap any shape on
    /// <paramref name="body"/>, excluding <paramref name="body"/> itself.
    /// Returns <c>null</c> if none or body is not live.
    /// </summary>
    public OverlapHit? OverlapBodyFirst(PhysicsBodyComponent body, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!B2.BodyIsValid(body.BodyId))
            return null;

        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        bool excludeSensors = filter?.ExcludeSensors ?? false;
        int shapeCount = B2.BodyGetShapeCount(body.BodyId);
        if (shapeCount == 0) return null;

        var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(shapeCount);
        try
        {
            fixed (B2.ShapeId* ptr = shapeIds)
                B2.BodyGetShapes(body.BodyId, ptr, shapeCount);

            for (int s = 0; s < shapeCount; s++)
            {
                BuildWorldSpaceProxy(shapeIds[s], out var proxy, out bool valid);
                if (!valid) continue;

                int capacity = 32;
                while (true)
                {
                    var hitIds = ArrayPool<B2.ShapeId>.Shared.Rent(capacity);
                    try
                    {
                        int hitCount;
                        fixed (B2.ShapeId* hptr = hitIds)
                        {
                            var ctx = new OverlapContext { Buffer = hptr, Capacity = capacity, Count = 0 };
                            delegate* unmanaged<B2.ShapeId, void*, byte> overlapCb = &OverlapCallback;
                            B2.WorldOverlapShape(_worldId, &proxy, f, (delegate* unmanaged<B2.ShapeId, void*, bool>)overlapCb, &ctx);
                            hitCount = ctx.Count;
                        }

                        bool fullScan = hitCount >= capacity;

                        for (int i = 0; i < hitCount; i++)
                        {
                            if (excludeSensors && B2.ShapeIsSensor(hitIds[i])) continue;
                            if (ShouldExcludeShape(hitIds[i], filter)) continue;
                            var comp = ResolveComponent(hitIds[i]);
                            if (comp == null || comp == body) continue;
                            var sub = ResolveSubShapeOnBody(comp, hitIds[i]);
                            return new OverlapHit { Component = comp, SubShape = sub, ShapeId = hitIds[i] };
                        }

                        if (!fullScan) break;
                        capacity *= 2;
                    }
                    finally
                    {
                        ArrayPool<B2.ShapeId>.Shared.Return(hitIds);
                    }
                }
            }
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
        }
        return null;
    }

    /// <summary>
    /// Returns all bodies (deduplicated by body) whose shapes exactly overlap any shape on
    /// <paramref name="body"/>, excluding <paramref name="body"/> itself,
    /// written into <paramref name="results"/>. Returns the number of results written.
    /// Use <see cref="OverlapBodyShapes"/> for per-shape granularity.
    /// </summary>
    public int OverlapBody(PhysicsBodyComponent body, Span<OverlapHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0 || !B2.BodyIsValid(body.BodyId))
        {
            wasTruncated = false;
            return 0;
        }

        return OverlapBodyExactSpanCore(body, results, filter, deduplicate: true, out wasTruncated);
    }

    /// <inheritdoc cref="OverlapBody(PhysicsBodyComponent,Span{OverlapHit},out bool,PhysicsQueryFilter?)"/>
    public int OverlapBody(PhysicsBodyComponent body, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
        => OverlapBody(body, results, out _, filter);

    /// <summary>
    /// Returns all shape hits whose shapes exactly overlap any shape on <paramref name="body"/>,
    /// excluding shapes belonging to <paramref name="body"/> itself,
    /// written into <paramref name="results"/>. Returns the number of results written.
    /// </summary>
    public int OverlapBodyShapes(PhysicsBodyComponent body, Span<OverlapHit> results, out bool wasTruncated, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (results.Length == 0 || !B2.BodyIsValid(body.BodyId))
        {
            wasTruncated = false;
            return 0;
        }

        return OverlapBodyExactSpanCore(body, results, filter, deduplicate: false, out wasTruncated);
    }

    /// <inheritdoc cref="OverlapBodyShapes(PhysicsBodyComponent,Span{OverlapHit},out bool,PhysicsQueryFilter?)"/>
    public int OverlapBodyShapes(PhysicsBodyComponent body, Span<OverlapHit> results, PhysicsQueryFilter? filter = null)
        => OverlapBodyShapes(body, results, out _, filter);
    
    private int OverlapBodyExactSpanCore(PhysicsBodyComponent body, Span<OverlapHit> results,
PhysicsQueryFilter? filter, bool deduplicate, out bool wasTruncated)
    {
        wasTruncated = false;

        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        bool excludeSensors = filter?.ExcludeSensors ?? false;
        int shapeCount = B2.BodyGetShapeCount(body.BodyId);
        if (shapeCount == 0)
            return 0;

        var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(shapeCount);
        int written = 0;
        HashSet<nint>? seenBodies = deduplicate ? [] : null;

        try
        {
            fixed (B2.ShapeId* ptr = shapeIds)
                B2.BodyGetShapes(body.BodyId, ptr, shapeCount);

            for (int s = 0; s < shapeCount && !wasTruncated; s++)
            {
                BuildWorldSpaceProxy(shapeIds[s], out var proxy, out bool valid);
                if (!valid) continue;

                int capacity = 32;
                while (true)
                {
                    var hitIds = ArrayPool<B2.ShapeId>.Shared.Rent(capacity);
                    try
                    {
                        int hitCount;
                        fixed (B2.ShapeId* hptr = hitIds)
                        {
                            var ctx = new OverlapContext { Buffer = hptr, Capacity = capacity, Count = 0 };
                            delegate* unmanaged<B2.ShapeId, void*, byte> overlapCb = &OverlapCallback;
                            B2.WorldOverlapShape(_worldId, &proxy, f,
                                (delegate* unmanaged<B2.ShapeId, void*, bool>)overlapCb, &ctx);
                            hitCount = ctx.Count;
                        }

                        if (hitCount >= capacity) { capacity *= 2; continue; }

                        for (int i = 0; i < hitCount; i++)
                        {
                            if (excludeSensors && B2.ShapeIsSensor(hitIds[i])) continue;
                            if (ShouldExcludeShape(hitIds[i], filter)) continue;
                            var comp = ResolveComponent(hitIds[i]);
                            if (comp == null || comp == body) continue;

                            if (deduplicate && !seenBodies!.Add(comp.BodyId.index1))
                                continue;

                            if (written == results.Length) { wasTruncated = true; break; }
                            var sub = ResolveSubShapeOnBody(comp, hitIds[i]);
                            results[written++] = new OverlapHit { Component = comp, SubShape = sub, ShapeId = hitIds[i] };
                        }
                        break;
                    }
                    finally
                    {
                        ArrayPool<B2.ShapeId>.Shared.Return(hitIds);
                    }
                }
            }
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
        }

        return written;
    }

    /// <summary>
    /// Returns all bodies (deduplicated by body) whose shapes exactly overlap any shape on
    /// <paramref name="body"/>, excluding <paramref name="body"/> itself.
    /// Clears <paramref name="results"/> before writing. Retries with a larger internal buffer
    /// if needed — guaranteed to return every overlapping body.
    /// Use <see cref="OverlapBodyAllShapes"/> for per-shape granularity.
    /// </summary>
    public void OverlapBodyAll(PhysicsBodyComponent body, List<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        OverlapBodyAllShapes(body, results, filter);
        DeduplicateOverlapHitsByBody(results);
    }

    /// <summary>
    /// Returns all shape hits whose shapes exactly overlap any shape on <paramref name="body"/>,
    /// excluding shapes belonging to <paramref name="body"/> itself.
    /// Clears <paramref name="results"/> before writing. Retries with a larger internal buffer
    /// if needed — guaranteed to return every hit.
    /// </summary>
    public void OverlapBodyAllShapes(PhysicsBodyComponent body, List<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();

        if (!B2.BodyIsValid(body.BodyId))
            return;

        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        bool excludeSensors = filter?.ExcludeSensors ?? false;
        int shapeCount = B2.BodyGetShapeCount(body.BodyId);
        if (shapeCount == 0) return;

        var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(shapeCount);
        var seen = new HashSet<nint>();
        try
        {
            fixed (B2.ShapeId* ptr = shapeIds)
                B2.BodyGetShapes(body.BodyId, ptr, shapeCount);

            for (int s = 0; s < shapeCount; s++)
            {
                BuildWorldSpaceProxy(shapeIds[s], out var proxy, out bool valid);
                if (!valid) continue;

                int before = results.Count;
                OverlapAllShapesCore(&proxy, f, results, excludeSensors, filter);

                // Remove any hits added by this probe whose ShapeId was already returned
                // by a prior probe, so compound self-shapes don't produce duplicates.
                int write = before;
                for (int i = before; i < results.Count; i++)
                {
                    if (seen.Add(results[i].ShapeId.index1))
                        results[write++] = results[i];
                }
                results.RemoveRange(write, results.Count - write);
            }
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
        }

        int w = 0;
        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].Component != body)
                results[w++] = results[i];
        }
        results.RemoveRange(w, results.Count - w);
    }

    /// <summary>
    /// Returns the first overlapping hit whose AABB intersects <paramref name="body"/>'s AABB,
    /// excluding <paramref name="body"/> itself, or <c>null</c> if none.
    /// </summary>
    /// <remarks>This is a <b>broad-phase AABB test only</b> — shapes are not tested for exact overlap.
    /// Use <see cref="OverlapBodyFirst"/> for shape-exact results.</remarks>
    public OverlapHit? OverlapBodyAABBFirst(PhysicsBodyComponent body, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (!B2.BodyIsValid(body.BodyId))
            return null;

        var aabb = B2.BodyComputeAABB(body.BodyId);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        bool excludeSensors = filter?.ExcludeSensors ?? false;

        int capacity = 32;
        while (true)
        {
            var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(capacity);
            try
            {
                int count;
                fixed (B2.ShapeId* ptr = shapeIds)
                {
                    var ctx = new OverlapContext { Buffer = ptr, Capacity = capacity, Count = 0 };
                    delegate* unmanaged<B2.ShapeId, void*, byte> overlapCb = &OverlapCallback;
                    B2.WorldOverlapAABB(_worldId, aabb, f, (delegate* unmanaged<B2.ShapeId, void*, bool>)overlapCb, &ctx);
                    count = ctx.Count;
                }

                if (count >= capacity) { capacity *= 2; continue; }

                for (int i = 0; i < count; i++)
                {
                    if (excludeSensors && B2.ShapeIsSensor(shapeIds[i])) continue;
                    if (ShouldExcludeShape(shapeIds[i], filter)) continue;
                    var comp = ResolveComponent(shapeIds[i]);
                    if (comp == null || comp == body) continue;
                    var sub = ResolveSubShapeOnBody(comp, shapeIds[i]);
                    return new OverlapHit { Component = comp, SubShape = sub, ShapeId = shapeIds[i] };
                }

                return null;
            }
            finally
            {
                ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
            }
        }
    }

    /// <summary>
    /// Returns all bodies (deduplicated by body) whose AABB overlaps
    /// <paramref name="body"/>'s AABB, excluding <paramref name="body"/> itself.
    /// Clears <paramref name="results"/> before writing.
    /// </summary>
    /// <remarks>
    /// This is a <b>broad-phase AABB test only</b> — shapes are not tested for exact overlap.
    /// Use <see cref="OverlapBodyAll"/> for shape-exact results.
    /// Use <see cref="OverlapBodyAABBAllShapes"/> for per-shape granularity.
    /// </remarks>
    public void OverlapBodyAABBAll(PhysicsBodyComponent body, List<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();

        if (!B2.BodyIsValid(body.BodyId))
            return;

        var aabb = B2.BodyComputeAABB(body.BodyId);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        OverlapAABBAllShapesCore(aabb, f, results, filter?.ExcludeSensors ?? false, filter);

        int write = 0;
        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].Component != body)
                results[write++] = results[i];
        }
        results.RemoveRange(write, results.Count - write);

        DeduplicateOverlapHitsByBody(results);
    }

    /// <summary>
    /// Returns all shape hits (one per shape) whose AABB overlaps <paramref name="body"/>'s AABB,
    /// excluding shapes belonging to <paramref name="body"/> itself.
    /// Clears <paramref name="results"/> before writing.
    /// </summary>
    /// <remarks>This is a <b>broad-phase AABB test only</b>.</remarks>
    public void OverlapBodyAABBAllShapes(PhysicsBodyComponent body, List<OverlapHit> results, PhysicsQueryFilter? filter = null)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        results.Clear();

        if (!B2.BodyIsValid(body.BodyId))
            return;

        var aabb = B2.BodyComputeAABB(body.BodyId);
        var f = filter?.ToB2() ?? B2.DefaultQueryFilter();
        OverlapAABBAllShapesCore(aabb, f, results, filter?.ExcludeSensors ?? false, filter);

        int write = 0;
        for (int i = 0; i < results.Count; i++)
        {
            if (results[i].Component != body)
                results[write++] = results[i];
        }
        results.RemoveRange(write, results.Count - write);
    }

    private OverlapHit? OverlapShapeFirstHitCore(B2.ShapeProxy* proxy, B2.QueryFilter f,
    bool excludeSensors = false, PhysicsQueryFilter? filter = null)
    {
        int capacity = 32;
        while (true)
        {
            var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(capacity);
            try
            {
                int count;
                fixed (B2.ShapeId* ptr = shapeIds)
                {
                    var ctx = new OverlapContext { Buffer = ptr, Capacity = capacity, Count = 0 };
                    delegate* unmanaged<B2.ShapeId, void*, byte> overlapCb = &OverlapCallback;
                    B2.WorldOverlapShape(_worldId, proxy, f, (delegate* unmanaged<B2.ShapeId, void*, bool>)overlapCb, &ctx);
                    count = ctx.Count;
                }

                if (count < capacity)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (excludeSensors && B2.ShapeIsSensor(shapeIds[i])) continue;
                        if (ShouldExcludeShape(shapeIds[i], filter)) continue;
                        var hit = ResolveShapeIdToHit(shapeIds[i]);
                        if (hit.HasValue) return hit;
                    }
                    return null;
                }

                capacity *= 2;
            }
            finally
            {
                ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
            }
        }
    }

    private OverlapHit? OverlapAABBFirstHitCore(B2.AABB aabb, B2.QueryFilter f,
        bool excludeSensors = false, PhysicsQueryFilter? filter = null)
    {
        int capacity = 32;
        while (true)
        {
            var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(capacity);
            try
            {
                int count;
                fixed (B2.ShapeId* ptr = shapeIds)
                {
                    var ctx = new OverlapContext { Buffer = ptr, Capacity = capacity, Count = 0 };
                    delegate* unmanaged<B2.ShapeId, void*, byte> overlapCb = &OverlapCallback;
                    B2.WorldOverlapAABB(_worldId, aabb, f, (delegate* unmanaged<B2.ShapeId, void*, bool>)overlapCb, &ctx);
                    count = ctx.Count;
                }

                if (count < capacity)
                {
                    for (int i = 0; i < count; i++)
                    {
                        if (excludeSensors && B2.ShapeIsSensor(shapeIds[i])) continue;
                        if (ShouldExcludeShape(shapeIds[i], filter)) continue;
                        var hit = ResolveShapeIdToHit(shapeIds[i]);
                        if (hit.HasValue) return hit;
                    }
                    return null;
                }

                capacity *= 2;
            }
            finally
            {
                ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
            }
        }
    }

    private int OverlapShapeAllShapesSpanCore(B2.ShapeProxy* proxy, B2.QueryFilter f, Span<OverlapHit> results,
    out bool wasTruncated, bool excludeSensors = false, bool deduplicate = false, PhysicsQueryFilter? filter = null)
    {
        int capacity = deduplicate ? Math.Max(results.Length * 8, 16) : Math.Max(results.Length + 1, 16);
        var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(capacity);
        try
        {
            int shapeCount;
            fixed (B2.ShapeId* ptr = shapeIds)
            {
                var ctx = new OverlapContext { Buffer = ptr, Capacity = capacity, Count = 0 };
                delegate* unmanaged<B2.ShapeId, void*, byte> overlapCb = &OverlapCallback;
                B2.WorldOverlapShape(_worldId, proxy, f,
                    (delegate* unmanaged<B2.ShapeId, void*, bool>)overlapCb, &ctx);
                shapeCount = ctx.Count;
            }
            wasTruncated = shapeCount >= capacity;
            return ResolveShapeIdsToHits(shapeIds, shapeCount, results, excludeSensors, filter);
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
        }
    }

    private int OverlapAABBAllShapesSpanCore(B2.AABB aabb, B2.QueryFilter f, Span<OverlapHit> results,
        out bool wasTruncated, bool excludeSensors = false, bool deduplicate = false, PhysicsQueryFilter? filter = null)
    {
        int capacity = deduplicate ? Math.Max(results.Length * 8, 16) : Math.Max(results.Length + 1, 16);
        var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(capacity);
        try
        {
            int shapeCount;
            fixed (B2.ShapeId* ptr = shapeIds)
            {
                var ctx = new OverlapContext { Buffer = ptr, Capacity = capacity, Count = 0 };
                delegate* unmanaged<B2.ShapeId, void*, byte> overlapCb = &OverlapCallback;
                B2.WorldOverlapAABB(_worldId, aabb, f, (delegate* unmanaged<B2.ShapeId, void*, bool>)overlapCb, &ctx);
                shapeCount = ctx.Count;
            }
            wasTruncated = shapeCount >= capacity;
            return ResolveShapeIdsToHits(shapeIds, shapeCount, results, excludeSensors, filter);
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
        }
    }

    private void OverlapAllShapesCore(B2.ShapeProxy* proxy, B2.QueryFilter f, List<OverlapHit> results,
    bool excludeSensors = false, PhysicsQueryFilter? filter = null)
    {
        int capacity = 64;
        while (true)
        {
            var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(capacity);
            int shapeCount;
            try
            {
                fixed (B2.ShapeId* ptr = shapeIds)
                {
                    var ctx = new OverlapContext { Buffer = ptr, Capacity = capacity, Count = 0 };
                    delegate* unmanaged<B2.ShapeId, void*, byte> overlapCb = &OverlapCallback;
                    B2.WorldOverlapShape(_worldId, proxy, f,
                        (delegate* unmanaged<B2.ShapeId, void*, bool>)overlapCb, &ctx);
                    shapeCount = ctx.Count;
                }
                if (shapeCount >= capacity) { capacity *= 2; continue; }
                ResolveShapeIdsToHitList(shapeIds, shapeCount, results, excludeSensors, filter);
                return;
            }
            finally
            {
                ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
            }
        }
    }

    private void OverlapAABBAllShapesCore(B2.AABB aabb, B2.QueryFilter f, List<OverlapHit> results,
        bool excludeSensors = false, PhysicsQueryFilter? filter = null)
    {
        int capacity = 64;
        while (true)
        {
            var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(capacity);
            int shapeCount;
            try
            {
                fixed (B2.ShapeId* ptr = shapeIds)
                {
                    var ctx = new OverlapContext { Buffer = ptr, Capacity = capacity, Count = 0 };
                    delegate* unmanaged<B2.ShapeId, void*, byte> overlapCb = &OverlapCallback;
                    B2.WorldOverlapAABB(_worldId, aabb, f, (delegate* unmanaged<B2.ShapeId, void*, bool>)overlapCb, &ctx);
                    shapeCount = ctx.Count;
                }
                if (shapeCount >= capacity) { capacity *= 2; continue; }
                ResolveShapeIdsToHitList(shapeIds, shapeCount, results, excludeSensors, filter);
                return;
            }
            finally
            {
                ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
            }
        }
    }

    private int ResolveShapeIdsToHits(B2.ShapeId[] shapeIds, int count, Span<OverlapHit> results,
        bool excludeSensors = false, PhysicsQueryFilter? filter = null)
    {
        int written = 0;
        for (int i = 0; i < count && written < results.Length; i++)
        {
            if (excludeSensors && B2.ShapeIsSensor(shapeIds[i])) continue;
            if (ShouldExcludeShape(shapeIds[i], filter)) continue;
            var hit = ResolveShapeIdToHit(shapeIds[i]);
            if (hit == null) continue;
            results[written++] = hit.Value;
        }
        return written;
    }

    private void ResolveShapeIdsToHitList(B2.ShapeId[] shapeIds, int count, List<OverlapHit> results,
        bool excludeSensors = false, PhysicsQueryFilter? filter = null)
    {
        for (int i = 0; i < count; i++)
        {
            if (excludeSensors && B2.ShapeIsSensor(shapeIds[i])) continue;
            if (ShouldExcludeShape(shapeIds[i], filter)) continue;
            var hit = ResolveShapeIdToHit(shapeIds[i]);
            if (hit.HasValue)
                results.Add(hit.Value);
        }
    }

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

    /// <summary>
    /// Registers a pre-solve filter invoked by the Box2D solver for every active contact pair
    /// each step, before collision impulses are applied. Return <c>false</c> to cancel the
    /// contact for that step — no forces are applied and no collision events fire for the pair
    /// that step, but the pair remains active and is re-evaluated next step.
    /// Pass <c>null</c> to remove the filter.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This is the correct mechanism for <b>one-way platforms</b> (jump-through floors).
    /// Cancel the contact when the moving body is approaching from the non-solid side by
    /// checking <see cref="PreSolveContact.Normal"/>.
    /// </para>
    /// <para>
    /// The callback runs on the simulation thread inside the Box2D solver — keep it
    /// allocation-free.
    /// </para>
    /// </remarks>
    public void SetPreSolveFilter(Func<PreSolveContact, bool>? filter)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        _preSolveFilter = filter;
        RebuildPreSolve();
    }

    internal void SetSystemPreSolveFilter(Func<PreSolveContact, bool>? filter)
    {
        _systemPreSolveFilter = filter;
        RebuildPreSolve();
    }

    private void RebuildPreSolve()
    {
        if (_preSolveHandle.IsAllocated)
            _preSolveHandle.Free();

        var sys = _systemPreSolveFilter;
        var usr = _preSolveFilter;

        if (sys == null && usr == null)
        {
            B2.WorldSetPreSolveCallback(_worldId, null, null);
            return;
        }

        Func<PreSolveContact, bool> combined = (sys, usr) switch
        {
            (not null, null) => sys,
            (null, not null) => usr,
            _ => c => sys!(c) && usr!(c)
        };

        var ctx = new PreSolveContext { Filter = combined, Resolver = ComponentResolver };
        _preSolveHandle = GCHandle.Alloc(ctx);
        delegate* unmanaged<B2.ShapeId, B2.ShapeId, B2.Manifold*, void*, byte> preSolveCb = &PreSolveCallback;
        B2.WorldSetPreSolveCallback(_worldId, (delegate* unmanaged<B2.ShapeId, B2.ShapeId, B2.Manifold*, void*, bool>)preSolveCb, (void*)GCHandle.ToIntPtr(_preSolveHandle));
    }

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
        delegate* unmanaged<B2.ShapeId, B2.ShapeId, void*, byte> filterCb = &CustomFilterCallback;
        B2.WorldSetCustomFilterCallback(_worldId, (delegate* unmanaged<B2.ShapeId, B2.ShapeId, void*, bool>)filterCb, (void*)GCHandle.ToIntPtr(_filterHandle));
    }

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

    private PhysicsBodyComponent? ResolveComponent(B2.ShapeId shapeId)
    {
        if (ComponentResolver == null) return null;
        return ComponentResolver(B2.ShapeGetBody(shapeId).index1);
    }

    private static bool ShouldExcludeShape(B2.ShapeId shapeId, PhysicsQueryFilter? filter)
    {
        if (filter == null) return false;

        var bodyIndex = B2.ShapeGetBody(shapeId).index1;

        var exclude = filter.Value.ExcludeBody;
        if (exclude != null)
        {
            if (!B2.BodyIsValid(exclude.BodyId))
            {
                Trace.TraceWarning(
                    "[Brine2D] PhysicsQueryFilter.ExcludeBody references a body that is not live. " +
                    "The exclusion will be ignored. Ensure the body is active before using it in a query filter.");
            }
            else if (bodyIndex == exclude.BodyId.index1)
                return true;
        }

        var excludeBodies = filter.Value.ExcludeBodies;
        if (excludeBodies != null)
        {
            foreach (var eb in excludeBodies)
            {
                if (eb != null && B2.BodyIsValid(eb.BodyId) && bodyIndex == eb.BodyId.index1)
                    return true;
            }
        }

        return false;
    }

    private (PhysicsBodyComponent? Component, SubShape? SubShape) ResolveHitFromShapeId(B2.ShapeId shapeId)
    {
        var comp = ResolveComponent(shapeId);
        if (comp == null)
            return (null, null);

        var sub = ResolveSubShapeOnBody(comp, shapeId);
        return (comp, sub);
    }

    private OverlapHit? ResolveShapeIdToHit(B2.ShapeId shapeId)
    {
        var comp = ResolveComponent(shapeId);
        if (comp == null)
            return null;

        var sub = ResolveSubShapeOnBody(comp, shapeId);
        return new OverlapHit { Component = comp, SubShape = sub, ShapeId = shapeId };
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

    private static void DeduplicateOverlapHitsByBody(List<OverlapHit> hits)
    {
        if (hits.Count <= 1)
            return;

        var seen = ArrayPool<nint>.Shared.Rent(hits.Count);
        int seenCount = 0;
        int write = 0;
        try
        {
            for (int i = 0; i < hits.Count; i++)
            {
                var idx = hits[i].Component?.BodyId.index1 ?? -1;
                bool found = false;
                for (int j = 0; j < seenCount; j++)
                    if (seen[j] == idx) { found = true; break; }
                if (found) continue;
                seen[seenCount++] = idx;
                hits[write++] = hits[i];
            }
            hits.RemoveRange(write, hits.Count - write);
        }
        finally
        {
            ArrayPool<nint>.Shared.Return(seen);
        }
    }

    private static int DeduplicateOverlapHitSpanByBody(Span<OverlapHit> hits, int count)
    {
        if (count <= 1)
            return count;

        var seen = ArrayPool<nint>.Shared.Rent(count);
        int seenCount = 0;
        int write = 0;
        try
        {
            for (int i = 0; i < count; i++)
            {
                var idx = hits[i].Component?.BodyId.index1 ?? -1;
                bool found = false;
                for (int j = 0; j < seenCount; j++)
                    if (seen[j] == idx) { found = true; break; }
                if (found) continue;
                seen[seenCount++] = idx;
                hits[write++] = hits[i];
            }
        }
        finally
        {
            ArrayPool<nint>.Shared.Return(seen);
        }
        return write;
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
                    if (seenBuf[j] == bodyIndex) { found = true; break; }
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
    private static byte PreSolveCallback(B2.ShapeId shapeIdA, B2.ShapeId shapeIdB, B2.Manifold* manifold, void* context)
    {
        var handle = GCHandle.FromIntPtr((nint)context);
        var ctx = (PreSolveContext)handle.Target!;

        var compA = ctx.Resolver?.Invoke(B2.ShapeGetBody(shapeIdA).index1);
        var compB = ctx.Resolver?.Invoke(B2.ShapeGetBody(shapeIdB).index1);

        if (compA == null || compB == null)
            return 1;

        var subA = ResolveSubShapeOnBody(compA, shapeIdA);
        var subB = ResolveSubShapeOnBody(compB, shapeIdB);

        return ctx.Filter(new PreSolveContact
        {
            BodyA = compA,
            BodyB = compB,
            SubShapeA = subA,
            SubShapeB = subB,
            Normal = new Vector2(manifold->normal.x, manifold->normal.y)
        }) ? (byte)1 : (byte)0;
    }

    [UnmanagedCallersOnly]
    private static byte OverlapCallback(B2.ShapeId shapeId, void* context)
    {
        var ctx = (OverlapContext*)context;
        if (ctx->Count >= ctx->Capacity)
            return 0;
        ctx->Buffer[ctx->Count++] = shapeId;
        return 1;
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
    private static byte CustomFilterCallback(B2.ShapeId shapeA, B2.ShapeId shapeB, void* context)
    {
        var handle = GCHandle.FromIntPtr((nint)context);
        var filter = (Func<B2.ShapeId, B2.ShapeId, bool>)handle.Target!;
        return filter(shapeA, shapeB) ? (byte)1 : (byte)0;
    }

    [UnmanagedCallersOnly]
    private static float ShapeCastClosestCallback(B2.ShapeId shapeId, B2.Vec2 point, B2.Vec2 normal, float fraction, void* context)
    {
        var ctx = (ShapeCastClosestContext*)context;
        if (ctx->ExcludeSensors && B2.ShapeIsSensor(shapeId))
            return 1f;
        if (ctx->ExcludeBodyIndex != 0 && B2.ShapeGetBody(shapeId).index1 == ctx->ExcludeBodyIndex)
            return 1f;
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
        public bool ExcludeSensors;
        public nint ExcludeBodyIndex;
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

    private static void BuildWorldSpaceProxy(B2.ShapeId shapeId, out B2.ShapeProxy proxy, out bool valid)
    {
        var xf = B2.BodyGetTransform(B2.ShapeGetBody(shapeId));
        valid = true;

        switch (B2.ShapeGetType(shapeId))
        {
            case B2.ShapeType.circleShape:
                {
                    var circle = B2.ShapeGetCircle(shapeId);
                    var worldCenter = B2.TransformPoint(xf, circle.center);
                    proxy = B2.MakeProxy(&worldCenter, 1, circle.radius);
                    break;
                }
            case B2.ShapeType.capsuleShape:
                {
                    var capsule = B2.ShapeGetCapsule(shapeId);
                    var pts = stackalloc B2.Vec2[2]
                    {
                        B2.TransformPoint(xf, capsule.center1),
                        B2.TransformPoint(xf, capsule.center2)
                    };
                    proxy = B2.MakeProxy(pts, 2, capsule.radius);
                    break;
                }
            case B2.ShapeType.segmentShape:
                {
                    var seg = B2.ShapeGetSegment(shapeId);
                    var pts = stackalloc B2.Vec2[2]
                    {
                        B2.TransformPoint(xf, seg.point1),
                        B2.TransformPoint(xf, seg.point2)
                    };
                    proxy = B2.MakeProxy(pts, 2, 0f);
                    break;
                }
            case B2.ShapeType.polygonShape:
                {
                    var poly = B2.ShapeGetPolygon(shapeId);
                    var pts = stackalloc B2.Vec2[8];
                    for (int i = 0; i < poly.count; i++)
                        pts[i] = B2.TransformPoint(xf, poly.vertices[i]);
                    proxy = B2.MakeProxy(pts, poly.count, poly.radius);
                    break;
                }
            case B2.ShapeType.chainSegmentShape:
                {
                    var chainSeg = B2.ShapeGetChainSegment(shapeId);
                    var pts = stackalloc B2.Vec2[2]
                    {
                        B2.TransformPoint(xf, chainSeg.segment.point1),
                        B2.TransformPoint(xf, chainSeg.segment.point2)
                    };
                    proxy = B2.MakeProxy(pts, 2, 0f);
                    break;
                }
            default:
                System.Diagnostics.Trace.TraceWarning(
                    $"[Brine2D] BuildWorldSpaceProxy: unsupported shape type {B2.ShapeGetType(shapeId)} on shape index {shapeId.index1} — OverlapBody query will skip this shape.");
                proxy = default;
                valid = false;
                break;
        }
    }
    
    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;

        if (_filterHandle.IsAllocated)
            _filterHandle.Free();

        if (_preSolveHandle.IsAllocated)
            _preSolveHandle.Free();

        if (B2.WorldIsValid(_worldId))
            B2.DestroyWorld(_worldId);

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