using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using Box2D.NET.Bindings;
using Brine2D.Collision;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Components.Joints;
using Brine2D.ECS.Query;
using Brine2D.ECS.Systems;
using Brine2D.Physics;

namespace Brine2D.Systems.Physics;

/// <summary>
/// Fixed-update system that drives Box2D simulation. Each step:
/// 1. Tears down bodies for disabled entities (flushing their contact state).
/// 2. Flushes contact/sensor state for bodies whose simulation was disabled this tick.
/// 3. Syncs dirty <see cref="PhysicsBodyComponent"/> data → Box2D bodies/shapes.
/// 4. Applies lightweight filter updates for layer/mask-only changes on live bodies.
/// 5. Applies lightweight body-type changes via <c>B2.BodySetType</c> (no full rebuild).
/// 6. Applies lightweight material updates (friction, restitution, hit-events) on live bodies.
/// 7. Applies <see cref="PhysicsBodyComponent.GravityOverride"/> forces to dynamic bodies.
/// 8. Pushes ECS transforms for kinematic bodies into Box2D (derives linear + angular velocity from displacement).
/// 9. Syncs joint components → Box2D joints.
/// 10. Installs or removes the system collision filter only when at least one body or sub-shape
///     has a <see cref="PhysicsBodyComponent.ShouldCollide"/> or
///     <see cref="SubShape.ShouldCollide"/> predicate.
/// 11. Steps the Box2D world.
/// 12. Reads back body positions/rotations into ECS transforms.
/// 13. Dispatches collision, sensor, hit, and sleep/wake events.
/// 14. Checks joint break thresholds and fires <see cref="JointComponent.OnBreak"/> as needed.
/// </summary>
public sealed unsafe class Box2DPhysicsSystem : FixedUpdateSystemBase, IDisposable
{
    private readonly PhysicsWorld _physicsWorld;

    // Keyed by BodyId.index1. Safe against B2 slot recycling because every code path that
    // destroys a body removes the entry from the dictionary before B2.DestroyBody is called,
    // so a recycled index1 always enters as a fresh Add.
    private readonly Dictionary<nint, PhysicsBodyComponent> _handleToCollider = new();
    private readonly Dictionary<nint, TransformComponent> _handleToTransform = new();
    private readonly Dictionary<nint, Vector2> _prevKinematicPositions = new();
    private readonly Dictionary<nint, float> _prevKinematicRotations = new();
    private readonly HashSet<(nint, nint)> _newContactPairsThisStep = new();
    private readonly HashSet<(nint, nint)> _newSensorPairsThisStep = new();
    private readonly List<PhysicsBodyComponent> _teardownBuffer = [];
    private readonly List<PhysicsBodyComponent> _stayContactBuffer = [];
    private readonly List<PhysicsBodyComponent> _staySensorBuffer = [];
    private readonly List<PhysicsBodyComponent> _activeBodySnapshot = [];

    private bool _wasPaused;

    private readonly Dictionary<nint, HashSet<(nint, nint)>> _contactKeysByBody = new();

    private readonly Dictionary<(nint, nint), CollisionContact> _lastKnownContacts = new();

    private readonly List<nint> _wakeBuffer = [];

    // Carries the old body index alongside the component so FlushStalePairs uses the correct
    // dictionary key even after BodyId has been reset to default by DestroyBody.
    private readonly List<(PhysicsBodyComponent Collider, nint OldBodyIndex)> _pendingFlushAfterStep = [];

    // Tracks how many currently-live bodies or sub-shapes have ShouldCollide set.
    private int _shouldCollideCount;

    // Tracks whether the system collision filter is currently installed in Box2D.
    private bool _systemFilterInstalled;

    private int _oneWayPlatformCount;
    private bool _oneWayPlatformFilterInstalled;

    // Tracks awake state per body so we can fire OnBodyWake on transition.
    private readonly Dictionary<nint, bool> _prevAwakeState = new();
    private readonly Dictionary<nint, bool> _registeredOneWayPlatform = new();

    private CachedEntityQuery<PhysicsBodyComponent, TransformComponent>? _colliderQuery;
    private CachedEntityQuery<RevoluteJointComponent, PhysicsBodyComponent>? _revoluteQuery;
    private CachedEntityQuery<DistanceJointComponent, PhysicsBodyComponent>? _distanceQuery;
    private CachedEntityQuery<WeldJointComponent, PhysicsBodyComponent>? _weldQuery;
    private CachedEntityQuery<PrismaticJointComponent, PhysicsBodyComponent>? _prismaticQuery;
    private CachedEntityQuery<MotorJointComponent, PhysicsBodyComponent>? _motorQuery;
    private CachedEntityQuery<WheelJointComponent, PhysicsBodyComponent>? _wheelQuery;
    private CachedEntityQuery<MouseJointComponent, PhysicsBodyComponent>? _mouseJointQuery;

    public Box2DPhysicsSystem(PhysicsWorld physicsWorld)
    {
        _physicsWorld = physicsWorld;
        _physicsWorld.ComponentResolver = handle =>
            _handleToCollider.TryGetValue(handle, out var c) ? c : null;
        _physicsWorld.AllBodiesResolver = () => _handleToCollider.Values;
    }

    public override int FixedUpdateOrder => SystemFixedUpdateOrder.Physics;

    public override void FixedUpdate(IEntityWorld world, GameTime fixedTime)
    {
        _colliderQuery ??= world.CreateCachedQuery<PhysicsBodyComponent, TransformComponent>()
            .OnlyEnabled()
            .Build();

        _physicsWorld.SimulationThreadId = Environment.CurrentManagedThreadId;
        try
        {
            var dt = (float)fixedTime.DeltaTime;

            TearDownDisabledBodies();
            FlushSimulationDisabledBodies();
            SyncToBox2D(dt);
            SyncJoints(world);
            SyncSystemFilter();
            SyncOneWayPlatformFilter();
            ClampFrozenAxes();

            if (_physicsWorld.IsPaused)
            {
                _wasPaused = true;
                return;
            }

            if (_wasPaused)
            {
                _wasPaused = false;
                WakeAllDynamicBodies();
            }

            _physicsWorld.Step(dt);
            ClampFrozenAxes();
            SyncFromBox2D();
            DispatchSleepWakeEvents();
            FlushPendingRebuildPairs();
            DispatchContactEvents();
            DispatchSensorEvents();
            DispatchStayEvents();
            CheckJointBreaks();
            _newContactPairsThisStep.Clear();
            _newSensorPairsThisStep.Clear();
        }
        finally
        {
            _physicsWorld.SimulationThreadId = 0;
        }
    }

    private void WakeAllDynamicBodies()
    {
        foreach (var collider in _handleToCollider.Values)
        {
            if (!B2.BodyIsValid(collider.BodyId)) continue;
            if (collider.BodyType == PhysicsBodyType.Dynamic && collider.IsSimulationEnabled)
                B2.BodySetAwake(collider.BodyId, true);
        }
    }

    private void FlushPendingRebuildPairs()
    {
        if (_pendingFlushAfterStep.Count == 0) return;
        foreach (var (collider, oldBodyIndex) in _pendingFlushAfterStep)
            FlushStalePairs(collider, oldBodyIndex);
        _pendingFlushAfterStep.Clear();
    }

    private void SyncSystemFilter()
    {
        bool needsFilter = _shouldCollideCount > 0 || _physicsWorld.HasIgnoredPairs;

        if (needsFilter == _systemFilterInstalled)
            return;

        _systemFilterInstalled = needsFilter;
        _physicsWorld.SetSystemCollisionFilter(needsFilter
            ? (shapeA, shapeB) =>
            {
                var bodyIndexA = B2.ShapeGetBody(shapeA).index1;
                var bodyIndexB = B2.ShapeGetBody(shapeB).index1;

                if (_physicsWorld.IsCollisionIgnored(bodyIndexA, bodyIndexB)) return false;

                var compA = _handleToCollider.GetValueOrDefault(bodyIndexA);
                var compB = _handleToCollider.GetValueOrDefault(bodyIndexB);
                if (compA == null || compB == null) return true;

                if (compA.ShouldCollide != null && !compA.ShouldCollide(compB)) return false;
                if (compB.ShouldCollide != null && !compB.ShouldCollide(compA)) return false;

                var subA = ResolveSubShape(compA, shapeA);
                var subB = ResolveSubShape(compB, shapeB);

                if (subA?.ShouldCollide != null && !subA.ShouldCollide(compB, subB)) return false;
                if (subB?.ShouldCollide != null && !subB.ShouldCollide(compA, subA)) return false;

                return true;
            }
            : null);
    }

    private void SyncOneWayPlatformFilter()
    {
        bool needsFilter = _oneWayPlatformCount > 0;
        if (needsFilter == _oneWayPlatformFilterInstalled) return;
        _oneWayPlatformFilterInstalled = needsFilter;
        _physicsWorld.SetSystemPreSolveFilter(needsFilter ? static c =>
            {
                if (c.BodyA.IsOneWayPlatform)
                {
                    // Normal points A→B. Cancel when it opposes PlatformNormalDirection
                    // (visitor is on the pass-through side).
                    if (Vector2.Dot(c.Normal, c.BodyA.PlatformNormalDirection) < 0f)
                        return false;
                }
                if (c.BodyB.IsOneWayPlatform)
                {
                    // Normal still points A→B (toward platform). Cancel when it aligns
                    // with PlatformNormalDirection (visitor approached from pass-through side).
                    if (Vector2.Dot(c.Normal, c.BodyB.PlatformNormalDirection) > 0f)
                        return false;
                }
                return true;
            }
            : null);
    }

    private void ClampFrozenAxes()
    {
        foreach (var (_, collider, transform) in _colliderQuery!)
        {
            if (!collider.FreezePositionX && !collider.FreezePositionY) continue;
            if (!B2.BodyIsValid(collider.BodyId) || !collider.IsSimulationEnabled) continue;

            if (collider.BodyType == PhysicsBodyType.Dynamic)
            {
                var vel = B2.BodyGetLinearVelocity(collider.BodyId);
                if (collider.FreezePositionX) vel.x = 0f;
                if (collider.FreezePositionY) vel.y = 0f;
                B2.BodySetLinearVelocity(collider.BodyId, vel);
            }
            else if (collider.BodyType == PhysicsBodyType.Kinematic)
            {
                // Kinematic bodies are positioned by writing TransformComponent.
                // Restore the frozen axis to the body's current Box2D position so the
                // kinematic sync doesn't drive it along that axis next tick.
                var bodyPos = B2.BodyGetPosition(collider.BodyId);
                if (collider.FreezePositionX)
                    transform.Position = new Vector2(bodyPos.x - collider.Offset.X, transform.Position.Y);
                if (collider.FreezePositionY)
                    transform.Position = new Vector2(transform.Position.X, bodyPos.y - collider.Offset.Y);
            }
        }
    }

    private static SubShape? ResolveSubShape(PhysicsBodyComponent body, B2.ShapeId shapeId)
    {
        foreach (var sub in body.SubShapes)
        {
            if (B2.ShapeIsValid(sub.ShapeId) && sub.ShapeId.index1 == shapeId.index1)
                return sub;
        }
        return null;
    }

    private void OnShouldCollideChanged(bool active)
    {
        if (!active && _shouldCollideCount <= 0)
        {
            System.Diagnostics.Debug.Fail(
                "ShouldCollide unsubscribe imbalance: _shouldCollideCount would go negative.");
            Trace.TraceWarning(
                "[Brine2D] ShouldCollide unsubscribe imbalance detected: _shouldCollideCount is already 0. " +
                "This indicates a subscribe/unsubscribe mismatch in the physics filter callback tracking.");
            return;
        }

        _shouldCollideCount += active ? 1 : -1;
    }

    private void TearDownDisabledBodies()
    {
        foreach (var collider in _handleToCollider.Values)
        {
            if (collider.Entity is not { IsActive: true } && B2.BodyIsValid(collider.BodyId))
                _teardownBuffer.Add(collider);
        }

        foreach (var collider in _teardownBuffer)
            DestroyBody(collider);

        _teardownBuffer.Clear();
    }

    private void FlushSimulationDisabledBodies()
    {
        foreach (var collider in _handleToCollider.Values)
        {
            if (collider.IsSimulationEnabledDirty)
            {
                collider.IsSimulationEnabledDirty = false;
                if (!collider.IsSimulationEnabled)
                {
                    _prevKinematicPositions.Remove(collider.BodyId.index1);
                    _prevKinematicRotations.Remove(collider.BodyId.index1);
                    FlushStalePairs(collider, collider.BodyId.index1);
                }
            }
        }
    }

    private void SyncToBox2D(float dt)
    {
        foreach (var (entity, collider, transform) in _colliderQuery!)
        {
            if (collider.IsDirty)
            {
                RebuildBody(entity, collider, transform);

                if (collider.Shape == null)
                {
                    collider.IsDirty = false;
                    continue;
                }

                ClearAllDirtyFlags(collider);
                PostRebuildSync(collider, transform);
                continue;
            }

            // IsBulletDirty requires a full rebuild. IsDirty takes precedence (handled above),
            // so by this point IsDirty is guaranteed false.
            if (collider.IsBulletDirty)
            {
                RebuildBody(entity, collider, transform);

                if (collider.Shape != null)
                {
                    ClearAllDirtyFlags(collider);
                    PostRebuildSync(collider, transform);
                }
                else
                {
                    collider.IsBulletDirty = false;
                }

                continue;
            }

            if (collider.IsFilterDirty)
            {
                if (B2.BodyIsValid(collider.BodyId))
                    ApplyFilter(collider);
                collider.IsFilterDirty = false;
            }
            if (collider.IsBodyTypeDirty)
            {
                if (B2.BodyIsValid(collider.BodyId))
                    ApplyBodyType(collider, transform);
                collider.IsBodyTypeDirty = false;
            }
            if (collider.IsMassDirty)
            {
                if (B2.BodyIsValid(collider.BodyId))
                    ApplyMass(collider, collider.BodyId);
                collider.IsMassDirty = false;
            }
            if (collider.IsMaterialDirty)
            {
                if (B2.BodyIsValid(collider.BodyId))
                    ApplyMaterial(collider);
                collider.IsMaterialDirty = false;
            }
            if (collider.IsSubShapeTriggerDirty)
            {
                if (B2.BodyIsValid(collider.BodyId))
                    ApplySubShapeTrigger(collider);
                collider.IsSubShapeTriggerDirty = false;
            }
            if (collider.IsSubShapeGeometryDirty)
            {
                if (B2.BodyIsValid(collider.BodyId))
                    ApplySubShapeGeometry(collider);
                collider.IsSubShapeGeometryDirty = false;
            }
            if (collider.IsOneWayPlatformDirty && B2.BodyIsValid(collider.BodyId))
            {
                bool wasRegistered = _registeredOneWayPlatform.TryGetValue(collider.BodyId.index1, out var prev) && prev;
                bool isNow = collider.IsOneWayPlatform;
                if (wasRegistered != isNow)
                    _oneWayPlatformCount += isNow ? 1 : -1;
                _registeredOneWayPlatform[collider.BodyId.index1] = isNow;
                collider.IsOneWayPlatformDirty = false;
            }

            // Apply per-body gravity override as a manual force each tick.
            if (collider.GravityOverride.HasValue
                && collider.BodyType == PhysicsBodyType.Dynamic
                && B2.BodyIsValid(collider.BodyId)
                && collider.IsSimulationEnabled)
            {
                var g = collider.GravityOverride.Value;
                if (g != Vector2.Zero)
                {
                    var massData = B2.BodyGetMassData(collider.BodyId);
                    if (massData.mass > 0f)
                    {
                        B2.BodyApplyForceToCenter(collider.BodyId,
                            new B2.Vec2 { x = g.X * massData.mass, y = g.Y * massData.mass },
                            true);
                    }
                }
            }

            if (collider.BodyType == PhysicsBodyType.Kinematic
                && B2.BodyIsValid(collider.BodyId)
                && !collider.IsDirty
                && collider.IsSimulationEnabled)
            {
                var pos = transform.Position + collider.Offset;
                var rot = transform.Rotation;

                // Zero any frozen axes on pos before deriving velocity so the
                // position delta on a frozen axis is always exactly zero.
                if (collider.FreezePositionX || collider.FreezePositionY)
                {
                    var bodyPos = B2.BodyGetPosition(collider.BodyId);
                    if (collider.FreezePositionX) pos.X = bodyPos.x;
                    if (collider.FreezePositionY) pos.Y = bodyPos.y;
                    // Also snap the transform so it stays consistent.
                    transform.Position = pos - collider.Offset;
                }

                if (collider.IsTeleporting)
                {
                    _prevKinematicPositions[collider.BodyId.index1] = pos;
                    _prevKinematicRotations[collider.BodyId.index1] = rot;
                    collider.IsTeleporting = false;
                }
                else
                {
                    if (dt > 0f)
                    {
                        if (_prevKinematicPositions.TryGetValue(collider.BodyId.index1, out var prevPos))
                        {
                            var velocity = (pos - prevPos) / dt;
                            B2.BodySetLinearVelocity(collider.BodyId, new B2.Vec2 { x = velocity.X, y = velocity.Y });
                        }
                        else
                        {
                            B2.BodySetLinearVelocity(collider.BodyId, default);
                        }

                        if (_prevKinematicRotations.TryGetValue(collider.BodyId.index1, out var prevRot))
                        {
                            var angularVelocity = (rot - prevRot) / dt;
                            B2.BodySetAngularVelocity(collider.BodyId, angularVelocity);
                        }
                        else
                        {
                            B2.BodySetAngularVelocity(collider.BodyId, 0f);
                        }
                    }

                    _prevKinematicPositions[collider.BodyId.index1] = pos;
                    _prevKinematicRotations[collider.BodyId.index1] = rot;
                }
            }
            else if (collider.BodyType != PhysicsBodyType.Kinematic && collider.IsTeleporting)
            {
                collider.IsTeleporting = false;
            }
            else if (collider.BodyType == PhysicsBodyType.Static
                     && B2.BodyIsValid(collider.BodyId)
                     && !collider.IsDirty)
            {
                var bodyPos = B2.BodyGetPosition(collider.BodyId);
                var expectedPos = transform.Position + collider.Offset;
                float dx = bodyPos.x - expectedPos.X;
                float dy = bodyPos.y - expectedPos.Y;
                if (dx * dx + dy * dy > 0.01f)
                {
                    Trace.TraceWarning(
                        $"[Brine2D] Static body on entity '{entity.Name}' has a TransformComponent position " +
                        $"that differs from its Box2D body position by ({dx:F1}, {dy:F1}) pixels. " +
                        "Setting transform.Position on a static body has no effect once the body is live. " +
                        "Use Teleport() to reposition a static body at runtime.");
                }
            }
        }
    }

    private static void ClearAllDirtyFlags(PhysicsBodyComponent collider)
    {
        collider.IsDirty = false;
        collider.IsFilterDirty = false;
        collider.IsBodyTypeDirty = false;
        collider.IsMassDirty = false;
        collider.IsMaterialDirty = false;
        collider.IsSubShapeTriggerDirty = false;
        collider.IsSubShapeGeometryDirty = false;
        collider.IsBulletDirty = false;
        collider.IsOneWayPlatformDirty = false;
    }

    private void PostRebuildSync(PhysicsBodyComponent collider, TransformComponent transform)
    {
        if (!B2.BodyIsValid(collider.BodyId)) return;

        bool wasRegistered = _registeredOneWayPlatform.TryGetValue(collider.BodyId.index1, out var prev) && prev;
        bool isNow = collider.IsOneWayPlatform;
        if (wasRegistered != isNow)
            _oneWayPlatformCount += isNow ? 1 : -1;
        _registeredOneWayPlatform[collider.BodyId.index1] = isNow;

        if (collider.BodyType == PhysicsBodyType.Kinematic)
        {
            _prevKinematicPositions[collider.BodyId.index1] = transform.Position + collider.Offset;
            _prevKinematicRotations[collider.BodyId.index1] = transform.Rotation;
        }
    }

    private void ApplySubShapeGeometry(PhysicsBodyComponent collider)
    {
        foreach (var sub in collider.SubShapes)
        {
            if (!B2.ShapeIsValid(sub.ShapeId)) continue;

            switch (sub.Definition)
            {
                case CircleShape circle:
                    {
                        var b2Circle = new B2.Circle
                        {
                            center = new B2.Vec2 { x = circle.Offset.X, y = circle.Offset.Y },
                            radius = circle.Radius
                        };
                        B2.ShapeSetCircle(sub.ShapeId, &b2Circle);
                        break;
                    }
                case BoxShape box:
                    {
                        var center = new B2.Vec2 { x = box.Offset.X, y = box.Offset.Y };
                        var polygon = B2.MakeOffsetBox(box.Width / 2f, box.Height / 2f, center, B2.MakeRot(box.Angle));
                        B2.ShapeSetPolygon(sub.ShapeId, &polygon);
                        break;
                    }
                case CapsuleShape capsule:
                    {
                        var b2Capsule = new B2.Capsule
                        {
                            center1 = new B2.Vec2 { x = capsule.Center1.X + capsule.Offset.X, y = capsule.Center1.Y + capsule.Offset.Y },
                            center2 = new B2.Vec2 { x = capsule.Center2.X + capsule.Offset.X, y = capsule.Center2.Y + capsule.Offset.Y },
                            radius = capsule.Radius
                        };
                        B2.ShapeSetCapsule(sub.ShapeId, &b2Capsule);
                        break;
                    }
                case PolygonShape polygon:
                {
                    if (polygon.Vertices.Count < 3 || polygon.Vertices.Count > ShapeDefinition.MaxPolygonVertices)
                        throw new InvalidOperationException(
                            $"PolygonShape has {polygon.Vertices.Count} vertices; Box2D requires 3–{ShapeDefinition.MaxPolygonVertices}.");
                    var b2Verts = stackalloc B2.Vec2[polygon.Vertices.Count];
                    for (int i = 0; i < polygon.Vertices.Count; i++)
                        b2Verts[i] = new B2.Vec2 { x = polygon.Vertices[i].X + polygon.Offset.X, y = polygon.Vertices[i].Y + polygon.Offset.Y };
                    var hull = B2.ComputeHull(b2Verts, polygon.Vertices.Count);
                    var b2Polygon = B2.MakePolygon(&hull, polygon.Radius);
                    B2.ShapeSetPolygon(sub.ShapeId, &b2Polygon);
                    break;
                }
                case SegmentShape segment:
                    {
                        var b2Segment = new B2.Segment
                        {
                            point1 = new B2.Vec2 { x = segment.Point1.X + segment.Offset.X, y = segment.Point1.Y + segment.Offset.Y },
                            point2 = new B2.Vec2 { x = segment.Point2.X + segment.Offset.X, y = segment.Point2.Y + segment.Offset.Y }
                        };
                        B2.ShapeSetSegment(sub.ShapeId, &b2Segment);
                        break;
                    }
            }
        }

        ApplyMass(collider, collider.BodyId);
    }

    private static void ApplyFilter(PhysicsBodyComponent collider)
    {
        var bodyFilter = new B2.Filter
        {
            categoryBits = collider.CategoryBits != 0 ? collider.CategoryBits : 1UL << collider.Layer,
            maskBits = collider.CollisionMask,
            groupIndex = collider.GroupIndex
        };

        if (B2.ShapeIsValid(collider.ShapeId))
            B2.ShapeSetFilter(collider.ShapeId, bodyFilter);

        foreach (var sub in collider.SubShapes)
        {
            if (!B2.ShapeIsValid(sub.ShapeId)) continue;
            var subFilter = new B2.Filter
            {
                categoryBits = sub.CategoryBits is { } subCat && subCat != 0 ? subCat : 1UL << (sub.Layer ?? collider.Layer),
                maskBits = sub.CollisionMask ?? collider.CollisionMask,
                groupIndex = sub.GroupIndex != 0 ? sub.GroupIndex : collider.GroupIndex
            };
            B2.ShapeSetFilter(sub.ShapeId, subFilter);
        }

        if (collider.Shape is ChainShape && B2.ChainIsValid(collider.ChainId))
        {
            int count = B2.BodyGetShapeCount(collider.BodyId);
            if (count > 0)
            {
                var shapes = ArrayPool<B2.ShapeId>.Shared.Rent(count);
                try
                {
                    fixed (B2.ShapeId* ptr = shapes)
                        B2.BodyGetShapes(collider.BodyId, ptr, count);
                    for (int i = 0; i < count; i++)
                        B2.ShapeSetFilter(shapes[i], bodyFilter);
                }
                finally
                {
                    ArrayPool<B2.ShapeId>.Shared.Return(shapes);
                }
            }
        }
    }

    private void ApplySubShapeTrigger(PhysicsBodyComponent collider)
    {
        foreach (var sub in collider.SubShapes)
        {
            if (!B2.ShapeIsValid(sub.ShapeId)) continue;
            B2.ShapeEnableSensorEvents(sub.ShapeId, sub.IsTrigger);
            B2.ShapeEnableContactEvents(sub.ShapeId, !sub.IsTrigger);
            B2.ShapeSetDensity(sub.ShapeId, sub.IsTrigger ? 0f : 1f, false);
        }

        ApplyMass(collider, collider.BodyId);

        // Flush any stale contact/sensor pairs that were tracked under the previous mode.
        FlushStalePairs(collider, collider.BodyId.index1);
    }

    private static void ApplyMaterial(PhysicsBodyComponent collider)
    {
        if (B2.ShapeIsValid(collider.ShapeId))
        {
            B2.ShapeSetFriction(collider.ShapeId, collider.SurfaceFriction);
            B2.ShapeSetRestitution(collider.ShapeId, collider.Restitution);
            B2.ShapeEnableHitEvents(collider.ShapeId, collider.EnableHitEvents);
        }

        foreach (var sub in collider.SubShapes)
        {
            if (!B2.ShapeIsValid(sub.ShapeId)) continue;
            B2.ShapeSetFriction(sub.ShapeId, sub.Friction ?? collider.SurfaceFriction);
            B2.ShapeSetRestitution(sub.ShapeId, sub.Restitution ?? collider.Restitution);
            B2.ShapeEnableHitEvents(sub.ShapeId, sub.EnableHitEvents ?? collider.EnableHitEvents);
        }

        if (collider.Shape is ChainShape chain && B2.ChainIsValid(collider.ChainId))
        {
            int count = B2.BodyGetShapeCount(collider.BodyId);
            if (count <= 0) return;

            var shapes = ArrayPool<B2.ShapeId>.Shared.Rent(count);
            try
            {
                fixed (B2.ShapeId* ptr = shapes)
                    B2.BodyGetShapes(collider.BodyId, ptr, count);

                for (int i = 0; i < count; i++)
                {
                    float friction = collider.SurfaceFriction;
                    float restitution = collider.Restitution;

                    // Preserve per-segment materials when the chain was built with them.
                    // The segment order from B2.BodyGetShapes mirrors insertion order, which
                    // matches the SegmentMaterials array built in BuildChainShape.
                    if (chain.SegmentMaterials != null && i < chain.SegmentMaterials.Length)
                    {
                        friction = chain.SegmentMaterials[i].Friction;
                        restitution = chain.SegmentMaterials[i].Restitution;
                    }

                    B2.ShapeSetFriction(shapes[i], friction);
                    B2.ShapeSetRestitution(shapes[i], restitution);
                    B2.ShapeEnableHitEvents(shapes[i], collider.EnableHitEvents);
                }
            }
            finally
            {
                ArrayPool<B2.ShapeId>.Shared.Return(shapes);
            }
        }
    }

    private void ApplyBodyType(PhysicsBodyComponent collider, TransformComponent transform)
    {
        var b2Type = collider.BodyType switch
        {
            PhysicsBodyType.Static => B2.BodyType.staticBody,
            PhysicsBodyType.Kinematic => B2.BodyType.kinematicBody,
            _ => B2.BodyType.dynamicBody
        };

        B2.BodySetType(collider.BodyId, b2Type);

        if (collider.SleepThreshold > 0f)
            B2.BodySetSleepThreshold(collider.BodyId, collider.SleepThreshold);

        if (collider.BodyType == PhysicsBodyType.Kinematic)
        {
            _prevKinematicPositions[collider.BodyId.index1] = transform.Position + collider.Offset;
            _prevKinematicRotations[collider.BodyId.index1] = transform.Rotation;
        }
        else
        {
            _prevKinematicPositions.Remove(collider.BodyId.index1);
            _prevKinematicRotations.Remove(collider.BodyId.index1);

            if (collider.BodyType == PhysicsBodyType.Dynamic)
                ApplyMass(collider, collider.BodyId);
        }

        // A body type change causes Box2D to temporarily remove and re-insert the body
        // in the broad phase. This can fire EndTouch events without matching BeginTouch
        // events (or vice versa), leaving the C# contact/sensor pair state stale.
        // Flush here so exit events fire cleanly from the known C# state.
        FlushStalePairs(collider, collider.BodyId.index1);
    }

    private void SyncJoints(IEntityWorld world)
    {
        _revoluteQuery ??= world.CreateCachedQuery<RevoluteJointComponent, PhysicsBodyComponent>().OnlyEnabled().Build();
        _distanceQuery ??= world.CreateCachedQuery<DistanceJointComponent, PhysicsBodyComponent>().OnlyEnabled().Build();
        _weldQuery ??= world.CreateCachedQuery<WeldJointComponent, PhysicsBodyComponent>().OnlyEnabled().Build();
        _prismaticQuery ??= world.CreateCachedQuery<PrismaticJointComponent, PhysicsBodyComponent>().OnlyEnabled().Build();
        _motorQuery ??= world.CreateCachedQuery<MotorJointComponent, PhysicsBodyComponent>().OnlyEnabled().Build();
        _wheelQuery ??= world.CreateCachedQuery<WheelJointComponent, PhysicsBodyComponent>().OnlyEnabled().Build();
        _mouseJointQuery ??= world.CreateCachedQuery<MouseJointComponent, PhysicsBodyComponent>().OnlyEnabled().Build();

        foreach (var (_, joint, body) in _revoluteQuery!) ProcessJoint(joint, body.BodyId);
        foreach (var (_, joint, body) in _distanceQuery!) ProcessJoint(joint, body.BodyId);
        foreach (var (_, joint, body) in _weldQuery!) ProcessJoint(joint, body.BodyId);
        foreach (var (_, joint, body) in _prismaticQuery!) ProcessJoint(joint, body.BodyId);
        foreach (var (_, joint, body) in _motorQuery!) ProcessJoint(joint, body.BodyId);
        foreach (var (_, joint, body) in _wheelQuery!) ProcessJoint(joint, body.BodyId);
        foreach (var (_, joint, body) in _mouseJointQuery!) ProcessJoint(joint, body.BodyId);
    }

    private void ProcessJoint(JointComponent joint, B2.BodyId bodyIdA)
    {
        if (!joint.IsDirty)
        {
            if (!B2.JointIsValid(joint.JointId))
            {
                // Joint was destroyed externally (connected body destroyed, etc.).
                if (joint.ConnectedBody != null)
                    _physicsWorld.UnregisterJoint(joint, bodyIdA.index1, joint.ConnectedBody.BodyId.index1);
                joint.ConnectedBody = null;
                joint.JointId = default;
                joint.IsDirty = false;
                joint.RaiseBreak();
            }
            else
            {
                return;
            }
        }

        if (joint.ConnectedBody == null)
        {
            if (B2.JointIsValid(joint.JointId))
            {
                // ConnectedBody is null so we have no bodyB index to recover.
                // Only clean up bodyA's registry entry to avoid stomping body-slot 0.
                _physicsWorld.UnregisterJoint(joint, bodyIdA.index1, bodyIdA.index1);
                _physicsWorld.DestroyJoint(joint.JointId);
                joint.JointId = default;
            }
            joint.IsDirty = false;
            return;
        }

        // Connected body exists but its Box2D body is gone — treat as a broken joint.
        if (!B2.BodyIsValid(joint.ConnectedBody.BodyId))
        {
            if (B2.JointIsValid(joint.JointId))
            {
                _physicsWorld.UnregisterJoint(joint, bodyIdA.index1, joint.ConnectedBody.BodyId.index1);
                _physicsWorld.DestroyJoint(joint.JointId);
            }
            joint.ConnectedBody = null;
            joint.JointId = default;
            joint.IsDirty = false;
            joint.RaiseBreak();
            return;
        }

        if (!B2.BodyIsValid(bodyIdA)) return;

        if (B2.JointIsValid(joint.JointId))
        {
            _physicsWorld.UnregisterJoint(joint, bodyIdA.index1, joint.ConnectedBody.BodyId.index1);
            _physicsWorld.DestroyJoint(joint.JointId);
        }

        joint.JointId = joint.Build(_physicsWorld, bodyIdA);

        var capturedBodyIdA = bodyIdA;
        joint.OnUnregister = () =>
        {
            if (joint.ConnectedBody != null)
                _physicsWorld.UnregisterJoint(joint, capturedBodyIdA.index1, joint.ConnectedBody.BodyId.index1);
            else
                _physicsWorld.UnregisterJoint(joint, capturedBodyIdA.index1, capturedBodyIdA.index1);
        };

        _physicsWorld.RegisterJoint(joint, bodyIdA.index1, joint.ConnectedBody.BodyId.index1);
        joint.IsDirty = false;
    }

    private void CheckJointBreaks()
    {
        CheckJointBreaks(_revoluteQuery);
        CheckJointBreaks(_distanceQuery);
        CheckJointBreaks(_weldQuery);
        CheckJointBreaks(_prismaticQuery);
        CheckJointBreaks(_motorQuery);
        CheckJointBreaks(_wheelQuery);
        CheckJointBreaks(_mouseJointQuery);
    }

    private void CheckJointBreaks<T>(CachedEntityQuery<T, PhysicsBodyComponent>? query)
        where T : JointComponent
    {
        if (query == null) return;

        foreach (var (_, joint, body) in query)
        {
            if (!joint.IsLive) continue;
            if (joint.BreakForce == float.PositiveInfinity && joint.BreakTorque == float.PositiveInfinity) continue;

            if (joint is WeldJointComponent weld && weld.LinearHertz == 0f && joint.BreakForce != float.PositiveInfinity)
            {
                Trace.TraceWarning(
                    $"[Brine2D] WeldJointComponent on entity '{body.Entity?.Name}' has BreakForce set but LinearHertz=0 " +
                    "(rigid weld). Box2D 3.x does not report constraint force for rigid joints — BreakForce has no effect. " +
                    "Set LinearHertz > 0 to enable soft-weld break detection.");
                continue;
            }

            var force = joint.GetReactionForce();
            var torque = joint.GetReactionTorque();

            if (force.Length() <= joint.BreakForce && MathF.Abs(torque) <= joint.BreakTorque) continue;

            if (joint.ConnectedBody != null)
                _physicsWorld.UnregisterJoint(joint, body.BodyId.index1, joint.ConnectedBody.BodyId.index1);

            B2.DestroyJoint(joint.JointId);
            joint.JointId = default;
            if (joint.RebuildAfterBreak)
                joint.IsDirty = true;
            joint.RaiseBreak();
        }
    }

    private void RebuildBody(Entity entity, PhysicsBodyComponent collider, TransformComponent transform)
    {
        if (collider.Shape == null)
            return;

        if (collider.Shape is SegmentShape && collider.BodyType != PhysicsBodyType.Static)
            throw new InvalidOperationException(
                $"Entity '{entity.Name}' has a SegmentShape with BodyType={collider.BodyType}. " +
                "SegmentShape is one-sided and intended for static geometry only. " +
                "Set BodyType = Static or use a CapsuleShape / BoxShape for dynamic/kinematic bodies.");

        var linearVelocity = collider.InitialLinearVelocity;
        var angularVelocity = collider.InitialAngularVelocity;
        bool wasLive = B2.BodyIsValid(collider.BodyId);
        if (wasLive)
        {
            var lv = B2.BodyGetLinearVelocity(collider.BodyId);
            linearVelocity = new Vector2(lv.x, lv.y);
            angularVelocity = B2.BodyGetAngularVelocity(collider.BodyId);
            var oldIndex = collider.BodyId.index1;
            DestroyBody(collider, deferPairFlush: true, deferredBodyIndex: oldIndex);
        }

        var pos = transform.Position + collider.Offset;

        var bodyDef = B2.DefaultBodyDef();
        bodyDef.type = collider.BodyType switch
        {
            PhysicsBodyType.Static => B2.BodyType.staticBody,
            PhysicsBodyType.Kinematic => B2.BodyType.kinematicBody,
            _ => B2.BodyType.dynamicBody
        };
        bodyDef.position = new B2.Vec2 { x = pos.X, y = pos.Y };
        bodyDef.rotation = B2.MakeRot(transform.Rotation);
        bodyDef.isBullet = collider.IsBullet;
        bodyDef.fixedRotation = collider.FixedRotation;
        bodyDef.gravityScale = collider.GravityOverride.HasValue ? 0f : collider.GravityScale;
        bodyDef.linearDamping = collider.LinearDamping;
        bodyDef.angularDamping = collider.AngularDamping;
        bodyDef.linearVelocity = new B2.Vec2 { x = linearVelocity.X, y = linearVelocity.Y };
        bodyDef.angularVelocity = angularVelocity;
        bodyDef.isEnabled = collider.IsSimulationEnabled;
        if (collider.SleepThreshold > 0f)
            bodyDef.sleepThreshold = collider.SleepThreshold;

        var bodyId = _physicsWorld.CreateBody(&bodyDef);
        collider.BodyId = bodyId;
        collider.World = _physicsWorld;

        _physicsWorld.FlushPendingIgnoredPairs(collider);

        if (!wasLive)
        {
            collider.InitialLinearVelocity = Vector2.Zero;
            collider.InitialAngularVelocity = 0f;
        }
        _handleToCollider[bodyId.index1] = collider;
        _handleToTransform[bodyId.index1] = transform;
        _prevAwakeState[bodyId.index1] = true;

        if (collider.IsOneWayPlatform)
            _oneWayPlatformCount++;

        _registeredOneWayPlatform[bodyId.index1] = collider.IsOneWayPlatform;

        if (collider.ShouldCollide != null)
            _shouldCollideCount++;
        foreach (var sub in collider.SubShapes)
        {
            if (sub.ShouldCollide != null)
                _shouldCollideCount++;
        }
        collider.ShouldCollideChanged += OnShouldCollideChanged;

        collider.OnBodyDestroyed = handle =>
        {
            collider.ShouldCollideChanged -= OnShouldCollideChanged;
            if (collider.ShouldCollide != null)
                _shouldCollideCount--;
            foreach (var sub in collider.SubShapes)
            {
                if (sub.ShouldCollide != null)
                    _shouldCollideCount--;
            }
            _prevKinematicPositions.Remove(handle);
            _prevKinematicRotations.Remove(handle);
            _prevAwakeState.Remove(handle);
            _physicsWorld.PurgeIgnoredPairsForBody(handle);
            _physicsWorld.PurgePendingIgnoredPairsForComponent(collider);
            _physicsWorld.UnregisterJointsForBody(handle);

            if (_registeredOneWayPlatform.TryGetValue(handle, out bool wasOWP) && wasOWP)
                _oneWayPlatformCount--;
            _registeredOneWayPlatform.Remove(handle);

            _physicsWorld.UntrackBody(collider);
            FlushStalePairs(collider, handle);
            _handleToCollider.Remove(handle);
            _handleToTransform.Remove(handle);
            collider.World = null;
        };

        if (collider.Shape is ChainShape chain)
        {
            if (collider.IsTrigger)
                throw new InvalidOperationException(
                    $"PhysicsBodyComponent on entity '{entity.Name}' has a ChainShape with IsTrigger=true. " +
                    "Chain shapes do not support trigger/sensor mode.");

            if (collider.IsBullet)
                throw new InvalidOperationException(
                    $"PhysicsBodyComponent on entity '{entity.Name}' has a ChainShape with IsBullet=true. " +
                    "Chain shapes do not support bullet (continuous collision detection) mode.");

            if (collider.BodyType != PhysicsBodyType.Static)
                throw new InvalidOperationException(
                    $"PhysicsBodyComponent on entity '{entity.Name}' has a ChainShape but BodyType is " +
                    $"{collider.BodyType}. Chain shapes are designed for static terrain and require BodyType.Static.");

            BuildChainShape(collider, bodyId, chain);
            return;
        }

        var shapeDef = B2.DefaultShapeDef();
        shapeDef.isSensor = collider.IsTrigger;
        shapeDef.density = collider.IsTrigger ? 0f : 1f;
        shapeDef.material.friction = collider.SurfaceFriction;
        shapeDef.material.restitution = collider.Restitution;
        shapeDef.filter.categoryBits = collider.CategoryBits != 0 ? collider.CategoryBits : 1UL << collider.Layer;
        shapeDef.filter.maskBits = collider.CollisionMask;
        shapeDef.filter.groupIndex = collider.GroupIndex;
        shapeDef.enableContactEvents = !collider.IsTrigger;
        shapeDef.enableSensorEvents = true;
        shapeDef.enablePreSolveEvents = true;
        shapeDef.enableHitEvents = collider.EnableHitEvents;

        collider.ShapeId = BuildShape(entity.Name, collider.Shape, bodyId, &shapeDef);

        foreach (var sub in collider.SubShapes)
        {
            var subDef = shapeDef;
            subDef.isSensor = sub.IsTrigger;
            subDef.density = sub.IsTrigger ? 0f : 1f;
            subDef.material.friction = sub.Friction ?? collider.SurfaceFriction;
            subDef.material.restitution = sub.Restitution ?? collider.Restitution;
            subDef.filter.categoryBits = sub.CategoryBits is { } subCat && subCat != 0 ? subCat : 1UL << (sub.Layer ?? collider.Layer);
            subDef.filter.maskBits = sub.CollisionMask ?? collider.CollisionMask;
            subDef.filter.groupIndex = sub.GroupIndex != 0 ? sub.GroupIndex : collider.GroupIndex;
            subDef.enableContactEvents = !sub.IsTrigger;
            subDef.enableSensorEvents = true;
            subDef.enablePreSolveEvents = true;
            subDef.enableHitEvents = sub.EnableHitEvents ?? collider.EnableHitEvents;
            sub.ShapeId = BuildShape(entity.Name, sub.Definition, bodyId, &subDef);
        }

        ApplyMass(collider, bodyId);
    }

    private unsafe B2.ShapeId BuildShape(string entityName, ShapeDefinition? shape, B2.BodyId bodyId, B2.ShapeDef* shapeDef)
    {
        return shape switch
        {
            CircleShape circle => BuildCircleShape(bodyId, shapeDef, circle),
            BoxShape box => BuildBoxShape(bodyId, shapeDef, box),
            CapsuleShape capsule => BuildCapsuleShape(bodyId, shapeDef, capsule),
            PolygonShape polygon => BuildPolygonShape(bodyId, shapeDef, polygon),
            SegmentShape segment => BuildSegmentShape(bodyId, shapeDef, segment),
            null => throw new InvalidOperationException(
                $"PhysicsBodyComponent on entity '{entityName}' has a null Shape."),
            _ => throw new NotSupportedException(
                $"PhysicsBodyComponent on entity '{entityName}' uses unsupported shape type '{shape.GetType().Name}'.")
        };
    }

    private B2.ShapeId BuildSegmentShape(B2.BodyId bodyId, B2.ShapeDef* shapeDef, SegmentShape shape)
    {
        var segment = new B2.Segment
        {
            point1 = new B2.Vec2 { x = shape.Point1.X + shape.Offset.X, y = shape.Point1.Y + shape.Offset.Y },
            point2 = new B2.Vec2 { x = shape.Point2.X + shape.Offset.X, y = shape.Point2.Y + shape.Offset.Y }
        };
        return _physicsWorld.CreateSegmentShape(bodyId, shapeDef, &segment);
    }

    private B2.ShapeId BuildCircleShape(B2.BodyId bodyId, B2.ShapeDef* shapeDef, CircleShape shape)
    {
        var circle = new B2.Circle
        {
            center = new B2.Vec2 { x = shape.Offset.X, y = shape.Offset.Y },
            radius = shape.Radius
        };
        return _physicsWorld.CreateCircleShape(bodyId, shapeDef, &circle);
    }

    private B2.ShapeId BuildBoxShape(B2.BodyId bodyId, B2.ShapeDef* shapeDef, BoxShape shape)
    {
        var center = new B2.Vec2 { x = shape.Offset.X, y = shape.Offset.Y };
        var polygon = B2.MakeOffsetBox(shape.Width / 2f, shape.Height / 2f, center, B2.MakeRot(shape.Angle));
        return _physicsWorld.CreatePolygonShape(bodyId, shapeDef, &polygon);
    }

    private B2.ShapeId BuildCapsuleShape(B2.BodyId bodyId, B2.ShapeDef* shapeDef, CapsuleShape shape)
    {
        var capsule = new B2.Capsule
        {
            center1 = new B2.Vec2 { x = shape.Center1.X + shape.Offset.X, y = shape.Center1.Y + shape.Offset.Y },
            center2 = new B2.Vec2 { x = shape.Center2.X + shape.Offset.X, y = shape.Center2.Y + shape.Offset.Y },
            radius = shape.Radius
        };
        return _physicsWorld.CreateCapsuleShape(bodyId, shapeDef, &capsule);
    }

    private B2.ShapeId BuildPolygonShape(B2.BodyId bodyId, B2.ShapeDef* shapeDef, PolygonShape shape)
    {
        if (shape.Vertices.Count < 3 || shape.Vertices.Count > ShapeDefinition.MaxPolygonVertices)
            throw new InvalidOperationException(
                $"PolygonShape has {shape.Vertices.Count} vertices; Box2D requires 3–{ShapeDefinition.MaxPolygonVertices}.");

        var span = shape.VerticesSpan;
        var b2Verts = stackalloc B2.Vec2[span.Length];
        for (int i = 0; i < span.Length; i++)
            b2Verts[i] = new B2.Vec2 { x = span[i].X + shape.Offset.X, y = span[i].Y + shape.Offset.Y };

        var hull = B2.ComputeHull(b2Verts, span.Length);
        var polygon = B2.MakePolygon(&hull, shape.Radius);
        return _physicsWorld.CreatePolygonShape(bodyId, shapeDef, &polygon);
    }

    private void BuildChainShape(PhysicsBodyComponent collider, B2.BodyId bodyId, ChainShape chain)
    {
        int segmentCount = chain.IsLoop ? chain.Points.Length : chain.Points.Length - 1;

        if (chain.SegmentMaterials != null && chain.SegmentMaterials.Length != segmentCount)
            throw new InvalidOperationException(
                $"ChainShape.SegmentMaterials length ({chain.SegmentMaterials.Length}) must equal the " +
                $"segment count ({segmentCount}) for a {(chain.IsLoop ? "loop" : "open")} chain on entity '{collider.Entity?.Name}'.");

        var b2pts = new B2.Vec2[chain.Points.Length];
        for (int i = 0; i < chain.Points.Length; i++)
            b2pts[i] = new B2.Vec2 { x = chain.Points[i].X, y = chain.Points[i].Y };

        // Avoid stackalloc for large chains — use a rented array and pin it instead.
        const int StackAllocThreshold = 256;
        B2.SurfaceMaterial[]? rentedMaterials = null;
        B2.SurfaceMaterial* materials;

        if (segmentCount <= StackAllocThreshold)
        {
            var stackMaterials = stackalloc B2.SurfaceMaterial[segmentCount];
            FillChainMaterials(stackMaterials, segmentCount, chain, collider);
            materials = stackMaterials;
            SubmitChain(collider, bodyId, chain, b2pts, materials, segmentCount);
        }
        else
        {
            rentedMaterials = ArrayPool<B2.SurfaceMaterial>.Shared.Rent(segmentCount);
            fixed (B2.SurfaceMaterial* ptr = rentedMaterials)
            {
                FillChainMaterials(ptr, segmentCount, chain, collider);
                SubmitChain(collider, bodyId, chain, b2pts, ptr, segmentCount);
            }
            ArrayPool<B2.SurfaceMaterial>.Shared.Return(rentedMaterials);
        }

        int shapeCount = B2.BodyGetShapeCount(bodyId);
        if (shapeCount > 0)
        {
            var shapeIds = ArrayPool<B2.ShapeId>.Shared.Rent(shapeCount);
            try
            {
                fixed (B2.ShapeId* ptr = shapeIds)
                    B2.BodyGetShapes(bodyId, ptr, shapeCount);
                for (int i = 0; i < shapeCount; i++)
                {
                    B2.ShapeEnableContactEvents(shapeIds[i], true);
                    B2.ShapeEnableHitEvents(shapeIds[i], collider.EnableHitEvents);
                }
            }
            finally
            {
                ArrayPool<B2.ShapeId>.Shared.Return(shapeIds);
            }
        }
    }

    private static void FillChainMaterials(B2.SurfaceMaterial* materials, int segmentCount,
        ChainShape chain, PhysicsBodyComponent collider)
    {
        for (int i = 0; i < segmentCount; i++)
        {
            if (chain.SegmentMaterials != null)
            {
                materials[i].friction = chain.SegmentMaterials[i].Friction;
                materials[i].restitution = chain.SegmentMaterials[i].Restitution;
            }
            else
            {
                materials[i].friction = collider.SurfaceFriction;
                materials[i].restitution = collider.Restitution;
            }
        }
    }

    private void SubmitChain(PhysicsBodyComponent collider, B2.BodyId bodyId, ChainShape chain,
        B2.Vec2[] b2pts, B2.SurfaceMaterial* materials, int segmentCount)
    {
        fixed (B2.Vec2* ptsPtr = b2pts)
        {
            var chainDef = B2.DefaultChainDef();
            chainDef.points = ptsPtr;
            chainDef.count = chain.Points.Length;
            chainDef.materials = materials;
            chainDef.materialCount = segmentCount;
            chainDef.isLoop = chain.IsLoop;
            chainDef.filter.categoryBits = collider.CategoryBits != 0 ? collider.CategoryBits : 1UL << collider.Layer;
            chainDef.filter.maskBits = collider.CollisionMask;
            chainDef.filter.groupIndex = collider.GroupIndex;
            collider.ChainId = _physicsWorld.CreateChain(bodyId, &chainDef);
        }
    }

    private static void ApplyMass(PhysicsBodyComponent collider, B2.BodyId bodyId)
    {
        if (collider.BodyType != PhysicsBodyType.Dynamic || collider.Mass <= 0f)
            return;

        if (!collider.IsTrigger && B2.ShapeIsValid(collider.ShapeId))
            B2.ShapeSetDensity(collider.ShapeId, 1f, false);

        foreach (var sub in collider.SubShapes)
            if (!sub.IsTrigger && B2.ShapeIsValid(sub.ShapeId))
                B2.ShapeSetDensity(sub.ShapeId, 1f, false);

        B2.BodyApplyMassFromShapes(bodyId);

        var massData = B2.BodyGetMassData(bodyId);
        if (massData.mass <= 0f)
        {
            // All shapes are sensors — Box2D computed zero mass. Set mass data explicitly.
            // Rotational inertia defaults to mass * 1000 (≈ a disc of radius ~45px at 100 px/m).
            // Set RotationalInertiaOverride on the component for accurate rotational simulation.
            Debug.WriteLineIf(collider.RotationalInertiaOverride == null,
                $"[Brine2D] Dynamic body '{collider.Entity?.Name}' has all-trigger shapes and no " +
                "RotationalInertiaOverride. Using Mass×1000 as rotational inertia. " +
                "Set RotationalInertiaOverride for correct rotational behavior.");
            B2.BodySetMassData(bodyId, new B2.MassData
            {
                mass = collider.Mass,
                center = collider.CenterOfMassOverride.HasValue
                    ? new B2.Vec2 { x = collider.CenterOfMassOverride.Value.X, y = collider.CenterOfMassOverride.Value.Y }
                    : default,
                rotationalInertia = collider.RotationalInertiaOverride ?? (collider.Mass * 1000f)
            });
            return;
        }

        float density = collider.Mass / massData.mass;

        if (!collider.IsTrigger && B2.ShapeIsValid(collider.ShapeId))
            B2.ShapeSetDensity(collider.ShapeId, density, false);

        foreach (var sub in collider.SubShapes)
            if (!sub.IsTrigger && B2.ShapeIsValid(sub.ShapeId))
                B2.ShapeSetDensity(sub.ShapeId, density, false);

        B2.BodyApplyMassFromShapes(bodyId);

        if (collider.CenterOfMassOverride.HasValue || collider.RotationalInertiaOverride.HasValue)
        {
            var current = B2.BodyGetMassData(bodyId);
            B2.BodySetMassData(bodyId, new B2.MassData
            {
                mass = current.mass,
                center = collider.CenterOfMassOverride.HasValue
                    ? new B2.Vec2 { x = collider.CenterOfMassOverride.Value.X, y = collider.CenterOfMassOverride.Value.Y }
                    : current.center,
                rotationalInertia = collider.RotationalInertiaOverride ?? current.rotationalInertia
            });
        }
    }

    private void DestroyBody(PhysicsBodyComponent collider, bool deferPairFlush = false, nint deferredBodyIndex = 0)
    {
        collider.ShouldCollideChanged -= OnShouldCollideChanged;
        if (collider.ShouldCollide != null)
            _shouldCollideCount--;
        foreach (var sub in collider.SubShapes)
        {
            if (sub.ShouldCollide != null)
                _shouldCollideCount--;
        }

        var bodyIndex = collider.BodyId.index1;

        if (_registeredOneWayPlatform.TryGetValue(bodyIndex, out bool wasOWP) && wasOWP)
            _oneWayPlatformCount--;
        _registeredOneWayPlatform.Remove(bodyIndex);

        _prevKinematicPositions.Remove(bodyIndex);
        _prevKinematicRotations.Remove(bodyIndex);
        _prevAwakeState.Remove(bodyIndex);
        _physicsWorld.UnregisterJointsForBody(bodyIndex);
        _physicsWorld.UntrackBody(collider);

        // Purge ignored pairs immediately — before the Box2D slot is recycled — so a new
        // body created at the same index1 does not inherit stale collision-ignore state.
        _physicsWorld.PurgeIgnoredPairsForBody(bodyIndex);

        if (deferPairFlush)
            _pendingFlushAfterStep.Add((collider, deferredBodyIndex != 0 ? deferredBodyIndex : bodyIndex));
        else
            FlushStalePairs(collider, bodyIndex);

        _handleToCollider.Remove(bodyIndex);
        _handleToTransform.Remove(bodyIndex);
        B2.DestroyBody(collider.BodyId);
        collider.BodyId = default;
        collider.ShapeId = default;
        collider.ChainId = default;
        foreach (var sub in collider.SubShapes)
            sub.ShapeId = default;
        ClearAllDirtyFlags(collider);
        collider.IsDirty = true;
        // Prevent OnRemoved re-entry from double-decrementing the counters.
        collider.OnBodyDestroyed = null;
    }

    // bodyIndex is passed explicitly so callers that defer the flush (and thus have already
    // reset collider.BodyId to default) still remove the correct _lastKnownContacts entries.
    private void FlushStalePairs(PhysicsBodyComponent collider, nint bodyIndex)
    {
        PhysicsBodyComponent[]? contactSnapshot = collider.ActiveContactPairs.Count > 0
            ? [.. collider.ActiveContactPairs]
            : null;
        PhysicsBodyComponent[]? sensorSnapshot = collider.ActiveSensorPairs.Count > 0
            ? [.. collider.ActiveSensorPairs]
            : null;

        collider.ActiveContactPairs.Clear();
        collider.ActiveSensorPairs.Clear();
        collider.ActiveContactSubShapes.Clear();
        collider.ActiveSensorSubShapes.Clear();

        if (contactSnapshot != null)
        {
            foreach (var other in contactSnapshot)
            {
                nint otherOldIndex = ResolveOtherOldIndex(bodyIndex, other);
                other.ActiveContactPairs.Remove(collider);
                other.ActiveContactSubShapes.Remove(bodyIndex);
                if (collider.Entity != null)
                    other.CollidingEntitiesInternal.Remove(collider.Entity);

                _physicsWorld.UntrackActivePair(collider, other);
                other.RaiseCollisionExit(collider, bodyIndex);
                collider.RaiseCollisionExit(other, otherOldIndex);
            }
        }

        if (sensorSnapshot != null)
        {
            foreach (var other in sensorSnapshot)
            {
                nint otherOldIndex = ResolveOtherOldIndex(bodyIndex, other);
                other.ActiveSensorPairs.Remove(collider);
                other.ActiveSensorSubShapes.Remove(bodyIndex);
                if (collider.Entity != null)
                    other.CollidingEntitiesInternal.Remove(collider.Entity);

                _physicsWorld.UntrackActivePair(collider, other);
                other.RaiseTriggerExit(collider, bodyIndex);
                collider.RaiseTriggerExit(other, otherOldIndex);
            }
        }

        collider.CollidingEntitiesInternal.Clear();

        PurgeContactEntriesForBody(bodyIndex);
        // PurgeIgnoredPairsForBody was already called in DestroyBody before slot recycling.
    }

    /// <summary>
    /// Resolves the old body index for `other` as seen from `bodyIndex`'s pair-key set.
    /// When both bodies are rebuilt in the same tick other.BodyId is already default(0),
    /// so we cannot use other.BodyId.index1. The pair key in _contactKeysByBody always
    /// encodes both old indices, so we extract the correct one from there.
    /// </summary>
    private nint ResolveOtherOldIndex(nint bodyIndex, PhysicsBodyComponent other)
    {
        if (_contactKeysByBody.TryGetValue(bodyIndex, out var keys))
        {
            foreach (var key in keys)
            {
                nint candidate = key.Item1 == bodyIndex ? key.Item2 : key.Item1;
                if (_handleToCollider.TryGetValue(candidate, out var mapped) && mapped == other)
                    return candidate;
            }
        }

        // Fallback: other's BodyId hasn't been reset yet (normal single-destroy path).
        return other.BodyId.index1;
    }

    private void PurgeContactEntriesForBody(nint bodyIndex)
    {
        if (!_contactKeysByBody.TryGetValue(bodyIndex, out var keys))
            return;

        foreach (var key in keys)
        {
            _lastKnownContacts.Remove(key);
            var other = key.Item1 == bodyIndex ? key.Item2 : key.Item1;
            if (_contactKeysByBody.TryGetValue(other, out var otherKeys))
                otherKeys.Remove(key);
        }

        _contactKeysByBody.Remove(bodyIndex);
    }

    private void SyncFromBox2D()
    {
        var bodyEvents = _physicsWorld.GetBodyEvents();
        var moves = new ReadOnlySpan<B2.BodyMoveEvent>(bodyEvents.moveEvents, bodyEvents.moveCount);

        foreach (ref readonly var move in moves)
        {
            if (!_handleToTransform.TryGetValue(move.bodyId.index1, out var transform))
                continue;

            if (!_handleToCollider.TryGetValue(move.bodyId.index1, out var collider))
                continue;

            if (collider.Entity is not { IsActive: true })
                continue;

            if (collider.BodyType != PhysicsBodyType.Kinematic)
            {
                var p = move.transform.p;
                transform.Position = new Vector2(p.x, p.y) - collider.Offset;
                transform.Rotation = MathF.Atan2(move.transform.q.s, move.transform.q.c);
            }

            if (move.fellAsleep)
            {
                _prevAwakeState[move.bodyId.index1] = false;
                collider.NotifyBodySleep();
            }
        }
    }

    /// <summary>
    /// Polls awake state for all bodies that were sleeping after the previous step.
    /// Box2D only fires body move events for bodies that actually moved, so OnBodyWake
    /// would be delayed by one tick if we relied solely on move events to detect waking.
    /// Polling here ensures the event fires the same tick the body transitions to awake.
    /// </summary>
    private void DispatchSleepWakeEvents()
    {
        foreach (var (index, wasAwake) in _prevAwakeState)
        {
            if (wasAwake) continue;
            if (!_handleToCollider.TryGetValue(index, out var collider)) continue;
            if (!B2.BodyIsValid(collider.BodyId)) continue;
            if (!B2.BodyIsAwake(collider.BodyId)) continue;
            _wakeBuffer.Add(index);
        }

        foreach (var index in _wakeBuffer)
        {
            _prevAwakeState[index] = true;
            if (_handleToCollider.TryGetValue(index, out var collider))
                collider.NotifyBodyWake();
        }

        _wakeBuffer.Clear();
    }

    private HashSet<(nint, nint)> GetOrCreateContactKeySet(nint bodyIndex)
    {
        if (!_contactKeysByBody.TryGetValue(bodyIndex, out var set))
            _contactKeysByBody[bodyIndex] = set = new HashSet<(nint, nint)>();
        return set;
    }

    private void DispatchContactEvents()
    {
        var events = _physicsWorld.GetContactEvents();

        var begins = new ReadOnlySpan<B2.ContactBeginTouchEvent>(events.beginEvents, events.beginCount);
        foreach (ref readonly var e in begins)
        {
            if (!TryGetColliderPair(e.shapeIdA, e.shapeIdB, out var a, out var b))
                continue;

            var key = MakePairKey(a.BodyId.index1, b.BodyId.index1);
            if (!_newContactPairsThisStep.Add(key))
                continue;

            var subA = ResolveSubShape(a, e.shapeIdA);
            var subB = ResolveSubShape(b, e.shapeIdB);

            _physicsWorld.TrackActivePair(a, b);
            var contact = CollisionContact.FromManifoldEnter(e.manifold);
            if (!contact.IsEmpty)
            {
                _lastKnownContacts[key] = contact;
                GetOrCreateContactKeySet(a.BodyId.index1).Add(key);
                GetOrCreateContactKeySet(b.BodyId.index1).Add(key);
            }
            a.NotifyCollisionEnter(b, contact, subA, subB);
            b.NotifyCollisionEnter(a, contact with { Normal = -contact.Normal }, subB, subA);
        }

        var ends = new ReadOnlySpan<B2.ContactEndTouchEvent>(events.endEvents, events.endCount);
        foreach (ref readonly var e in ends)
        {
            if (!TryGetColliderPair(e.shapeIdA, e.shapeIdB, out var a, out var b))
                continue;

            if (!a.ActiveContactPairs.Contains(b))
                continue;

            var key = MakePairKey(a.BodyId.index1, b.BodyId.index1);
            _lastKnownContacts.Remove(key);
            if (_contactKeysByBody.TryGetValue(a.BodyId.index1, out var setA))
            {
                setA.Remove(key);
                if (setA.Count == 0) _contactKeysByBody.Remove(a.BodyId.index1);
            }
            if (_contactKeysByBody.TryGetValue(b.BodyId.index1, out var setB))
            {
                setB.Remove(key);
                if (setB.Count == 0) _contactKeysByBody.Remove(b.BodyId.index1);
            }
            a.NotifyCollisionExit(b);
            b.NotifyCollisionExit(a);
            _physicsWorld.UntrackActivePair(a, b);
        }

        var hits = new ReadOnlySpan<B2.ContactHitEvent>(events.hitEvents, events.hitCount);
        foreach (ref readonly var e in hits)
        {
            if (!TryGetColliderPair(e.shapeIdA, e.shapeIdB, out var a, out var b))
                continue;

            var contact = new CollisionContact
            {
                Normal = new Vector2(e.normal.x, e.normal.y),
                ContactPoint = new Vector2(e.point.x, e.point.y),
                ImpactSpeed = e.approachSpeed
            };
            a.NotifyCollisionHit(b, contact);
            b.NotifyCollisionHit(a, contact with { Normal = -contact.Normal });
        }
    }

    private void DispatchSensorEvents()
    {
        var events = _physicsWorld.GetSensorEvents();

        var begins = new ReadOnlySpan<B2.SensorBeginTouchEvent>(events.beginEvents, events.beginCount);
        foreach (ref readonly var e in begins)
        {
            if (!TryGetColliderPair(e.sensorShapeId, e.visitorShapeId, out var sensor, out var visitor))
                continue;

            var key = MakePairKey(sensor.BodyId.index1, visitor.BodyId.index1);
            if (!_newSensorPairsThisStep.Add(key))
                continue;

            var sensorSubShape = ResolveSubShape(sensor, e.sensorShapeId);
            var visitorSubShape = ResolveSubShape(visitor, e.visitorShapeId);

            _physicsWorld.TrackActivePair(sensor, visitor);
            sensor.NotifyTriggerEnter(visitor, sensorSubShape, visitorSubShape);
            visitor.NotifyTriggerEnter(sensor, visitorSubShape, sensorSubShape);
        }

        var ends = new ReadOnlySpan<B2.SensorEndTouchEvent>(events.endEvents, events.endCount);
        foreach (ref readonly var e in ends)
        {
            if (!TryGetColliderPair(e.sensorShapeId, e.visitorShapeId, out var sensor, out var visitor))
                continue;

            if (!sensor.ActiveSensorPairs.Contains(visitor))
                continue;

            sensor.NotifyTriggerExit(visitor);
            visitor.NotifyTriggerExit(sensor);
            _physicsWorld.UntrackActivePair(sensor, visitor);
        }
    }

    private void DispatchStayEvents()
    {
        var activeBodies = _physicsWorld.ActiveBodies;

        // Ensure the snapshot list has enough capacity to avoid reallocation mid-frame.
        if (_activeBodySnapshot.Capacity < activeBodies.Count)
            _activeBodySnapshot.Capacity = activeBodies.Count + 16;

        _activeBodySnapshot.Clear();
        _activeBodySnapshot.AddRange(activeBodies);

        foreach (var collider in _activeBodySnapshot)
        {
            if (collider.Entity is not { IsActive: true }) continue;

            _stayContactBuffer.Clear();
            foreach (var p in collider.ActiveContactPairs)
                _stayContactBuffer.Add(p);

            _staySensorBuffer.Clear();
            foreach (var p in collider.ActiveSensorPairs)
                _staySensorBuffer.Add(p);

            foreach (var other in _stayContactBuffer)
            {
                if (other.Entity is not { IsActive: true }) continue;
                if (collider.BodyId.index1 > other.BodyId.index1) continue;
                if (_newContactPairsThisStep.Contains(MakePairKey(collider.BodyId.index1, other.BodyId.index1))) continue;

                var key = MakePairKey(collider.BodyId.index1, other.BodyId.index1);

                CollisionContact contact;
                bool needsLiveContact = collider.HasCollisionStaySubscribers || other.HasCollisionStaySubscribers;
                if (needsLiveContact)
                {
                    contact = GetLiveContact(collider, other);
                    if (contact.IsEmpty)
                        _lastKnownContacts.TryGetValue(key, out contact);
                    else
                        _lastKnownContacts[key] = contact;
                }
                else
                {
                    _lastKnownContacts.TryGetValue(key, out contact);
                }

                collider.ActiveContactSubShapes.TryGetValue(other.BodyId.index1, out var pairAB);
                collider.NotifyCollisionStay(other, contact, pairAB.Self, pairAB.Other);

                other.ActiveContactSubShapes.TryGetValue(collider.BodyId.index1, out var pairBA);
                other.NotifyCollisionStay(collider, contact with { Normal = -contact.Normal }, pairBA.Self, pairBA.Other);
            }

            foreach (var other in _staySensorBuffer)
            {
                if (other.Entity is not { IsActive: true }) continue;
                if (collider.BodyId.index1 > other.BodyId.index1) continue;
                if (_newSensorPairsThisStep.Contains(MakePairKey(collider.BodyId.index1, other.BodyId.index1))) continue;

                collider.NotifyTriggerStay(other);
                other.NotifyTriggerStay(collider);
            }
        }
    }

    private static CollisionContact GetLiveContact(PhysicsBodyComponent a, PhysicsBodyComponent b)
    {
        if (!B2.BodyIsValid(a.BodyId) || !B2.BodyIsValid(b.BodyId))
            return CollisionContact.Empty;

        const int maxCapacity = 4096;
        int initialCapacity = Math.Max(B2.BodyGetShapeCount(a.BodyId) + B2.BodyGetShapeCount(b.BodyId), 16);

        for (int capacity = initialCapacity; capacity <= maxCapacity; capacity *= 2)
        {
            var contacts = ArrayPool<B2.ContactData>.Shared.Rent(capacity);
            CollisionContact? found = null;
            bool needsRetry = false;

            try
            {
                fixed (B2.ContactData* ptr = contacts)
                {
                    if (a.Shape is ChainShape || b.Shape is ChainShape)
                    {
                        bool aIsChain = a.Shape is ChainShape;
                        var chainBodyId = aIsChain ? a.BodyId : b.BodyId;
                        var otherBodyIndex = aIsChain ? b.BodyId.index1 : a.BodyId.index1;
                        int count = B2.BodyGetContactData(chainBodyId, ptr, capacity);
                        if (count >= capacity) { needsRetry = true; }
                        else
                        {
                            var r = FindContactWithBodyId(ptr, count, chainBodyId.index1, otherBodyIndex);
                            if (r.HasValue)
                            {
                                // r.Value.Contact has normal in B2's shapeA→shapeB direction.
                                // r.Value.ChainIsShapeA tells us whether the chain body is shapeA.
                                // Normalise to chain→other:
                                var chainToOther = r.Value.ChainIsShapeA
                                    ? r.Value.Contact
                                    : r.Value.Contact with { Normal = -r.Value.Contact.Normal };
                                // GetLiveContact convention: result normal points from a→b.
                                found = aIsChain ? chainToOther : chainToOther with { Normal = -chainToOther.Normal };
                            }
                            else
                            {
                                found = CollisionContact.Empty;
                            }
                        }
                    }
                    else
                    {
                        if (!needsRetry && B2.ShapeIsValid(a.ShapeId))
                        {
                            int count = B2.ShapeGetContactData(a.ShapeId, ptr, capacity);
                            if (count >= capacity) needsRetry = true;
                            else
                            {
                                var r = FindContactWithBody(ptr, count, a.ShapeId, b.BodyId.index1);
                                if (r.HasValue) found = r.Value;
                            }
                        }

                        if (!needsRetry && !found.HasValue)
                        {
                            foreach (var sub in a.SubShapes)
                            {
                                if (!B2.ShapeIsValid(sub.ShapeId)) continue;
                                int count = B2.ShapeGetContactData(sub.ShapeId, ptr, capacity);
                                if (count >= capacity) { needsRetry = true; break; }
                                var r = FindContactWithBody(ptr, count, sub.ShapeId, b.BodyId.index1);
                                if (r.HasValue) { found = r.Value; break; }
                            }
                        }

                        if (!needsRetry && !found.HasValue && B2.ShapeIsValid(b.ShapeId))
                        {
                            int count = B2.ShapeGetContactData(b.ShapeId, ptr, capacity);
                            if (count >= capacity) needsRetry = true;
                            else
                            {
                                var r = FindContactWithBody(ptr, count, b.ShapeId, a.BodyId.index1);
                                if (r.HasValue) found = r.Value with { Normal = -r.Value.Normal };
                            }
                        }

                        if (!needsRetry && !found.HasValue)
                        {
                            foreach (var sub in b.SubShapes)
                            {
                                if (!B2.ShapeIsValid(sub.ShapeId)) continue;
                                int count = B2.ShapeGetContactData(sub.ShapeId, ptr, capacity);
                                if (count >= capacity) { needsRetry = true; break; }
                                var r = FindContactWithBody(ptr, count, sub.ShapeId, a.BodyId.index1);
                                if (r.HasValue) { found = r.Value with { Normal = -r.Value.Normal }; break; }
                            }
                        }
                    }
                }
            }
            finally
            {
                ArrayPool<B2.ContactData>.Shared.Return(contacts);
            }

            if (!needsRetry)
                return found ?? CollisionContact.Empty;
        }

        System.Diagnostics.Debug.Fail(
            $"GetLiveContact: contact buffer capacity exceeded {maxCapacity}. Returning Empty.");
        return CollisionContact.Empty;
    }

    private readonly record struct ChainContactResult(CollisionContact Contact, bool ChainIsShapeA);

    private static ChainContactResult? FindContactWithBodyId(B2.ContactData* contacts, int count,
        nint chainBodyIndex, nint targetBodyIndex)
    {
        for (int i = 0; i < count; i++)
        {
            ref var c = ref contacts[i];
            bool chainIsA = B2.ShapeGetBody(c.shapeIdA).index1 == chainBodyIndex;
            nint otherIdx = chainIsA
                ? B2.ShapeGetBody(c.shapeIdB).index1
                : B2.ShapeGetBody(c.shapeIdA).index1;
            if (otherIdx == targetBodyIndex)
                return new ChainContactResult(MakeContact(c.manifold), chainIsA);
        }
        return null;
    }

    private static CollisionContact? FindContactWithBody(B2.ContactData* contacts, int count,
        B2.ShapeId selfShapeId, nint targetBodyIndex)
    {
        for (int i = 0; i < count; i++)
        {
            ref var c = ref contacts[i];
            var otherShape = c.shapeIdA.index1 == selfShapeId.index1 ? c.shapeIdB : c.shapeIdA;
            if (B2.ShapeGetBody(otherShape).index1 == targetBodyIndex)
                return MakeContact(c.manifold);
        }
        return null;
    }

    private static CollisionContact? FindContactWithBodyId(B2.ContactData* contacts, int count, nint targetBodyIndex)
    {
        for (int i = 0; i < count; i++)
        {
            ref var c = ref contacts[i];
            if (B2.ShapeGetBody(c.shapeIdA).index1 == targetBodyIndex ||
                B2.ShapeGetBody(c.shapeIdB).index1 == targetBodyIndex)
                return MakeContact(c.manifold);
        }
        return null;
    }

    private bool TryGetColliderPair(B2.ShapeId shapeA, B2.ShapeId shapeB,
        out PhysicsBodyComponent a, out PhysicsBodyComponent b)
    {
        a = null!;
        b = null!;
        return _handleToCollider.TryGetValue(B2.ShapeGetBody(shapeA).index1, out a!)
            && _handleToCollider.TryGetValue(B2.ShapeGetBody(shapeB).index1, out b!);
    }

    private static CollisionContact MakeContact(B2.Manifold manifold) =>
        CollisionContact.FromManifold(manifold);

    private static (nint, nint) MakePairKey(nint a, nint b) => a <= b ? (a, b) : (b, a);

    public void Dispose()
    {
        _physicsWorld.SetSystemCollisionFilter(null);
        _physicsWorld.SetSystemPreSolveFilter(null);
        _systemFilterInstalled = false;
        _oneWayPlatformFilterInstalled = false;

        _physicsWorld.ComponentResolver = null;
        _physicsWorld.AllBodiesResolver = null;

        _colliderQuery?.Dispose();
        _revoluteQuery?.Dispose();
        _distanceQuery?.Dispose();
        _weldQuery?.Dispose();
        _prismaticQuery?.Dispose();
        _motorQuery?.Dispose();
        _wheelQuery?.Dispose();
        _mouseJointQuery?.Dispose();
        _handleToCollider.Clear();
        _handleToTransform.Clear();
        _prevKinematicPositions.Clear();
        _prevKinematicRotations.Clear();
        _prevAwakeState.Clear();
        _newContactPairsThisStep.Clear();
        _newSensorPairsThisStep.Clear();
        _lastKnownContacts.Clear();
        _contactKeysByBody.Clear();
        _teardownBuffer.Clear();
        _stayContactBuffer.Clear();
        _staySensorBuffer.Clear();
        _activeBodySnapshot.Clear();
        _wakeBuffer.Clear();
    }
}