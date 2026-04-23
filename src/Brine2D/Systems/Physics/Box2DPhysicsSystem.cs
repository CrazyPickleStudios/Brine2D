using System.Buffers;
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

    // Tracks how many currently-live bodies or sub-shapes have ShouldCollide set.
    private int _shouldCollideCount;

    // Tracks whether the system collision filter is currently installed in Box2D.
    private bool _systemFilterInstalled;

    // Tracks awake state per body so we can fire OnBodyWake on transition.
    private readonly Dictionary<nint, bool> _prevAwakeState = new();

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
    }

    public override int FixedUpdateOrder => SystemFixedUpdateOrder.Physics;

    public override void FixedUpdate(IEntityWorld world, GameTime fixedTime)
    {
        _colliderQuery ??= world.CreateCachedQuery<PhysicsBodyComponent, TransformComponent>()
            .OnlyEnabled()
            .Build();

        var dt = (float)fixedTime.DeltaTime;

        TearDownDisabledBodies();
        FlushSimulationDisabledBodies();
        SyncToBox2D(dt);
        SyncJoints(world);
        SyncSystemFilter();
        _physicsWorld.Step(dt);
        SyncFromBox2D();
        DispatchContactEvents();
        DispatchSensorEvents();
        DispatchStayEvents();
        CheckJointBreaks();
        _newContactPairsThisStep.Clear();
        _newSensorPairsThisStep.Clear();
    }

    private void SyncSystemFilter()
    {
        bool needsFilter = _shouldCollideCount > 0;

        if (needsFilter == _systemFilterInstalled)
            return;

        _systemFilterInstalled = needsFilter;
        _physicsWorld.SetSystemCollisionFilter(needsFilter
            ? (shapeA, shapeB) =>
            {
                var compA = _handleToCollider.GetValueOrDefault(B2.ShapeGetBody(shapeA).index1);
                var compB = _handleToCollider.GetValueOrDefault(B2.ShapeGetBody(shapeB).index1);
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
        => _shouldCollideCount += active ? 1 : -1;

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
                    FlushStalePairs(collider);
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
                    continue;

                collider.IsDirty = false;
                collider.IsFilterDirty = false;
                collider.IsBodyTypeDirty = false;
                collider.IsMassDirty = false;
                collider.IsMaterialDirty = false;

                if (collider.BodyType == PhysicsBodyType.Kinematic && B2.BodyIsValid(collider.BodyId))
                {
                    _prevKinematicPositions[collider.BodyId.index1] = transform.Position + collider.Offset;
                    _prevKinematicRotations[collider.BodyId.index1] = transform.Rotation;
                }
            }
            else if (collider.IsFilterDirty)
            {
                if (B2.BodyIsValid(collider.BodyId))
                    ApplyFilter(collider);
                collider.IsFilterDirty = false;
            }
            else if (collider.IsBodyTypeDirty)
            {
                if (B2.BodyIsValid(collider.BodyId))
                    ApplyBodyType(collider, transform);
                collider.IsBodyTypeDirty = false;
            }
            else if (collider.IsMassDirty)
            {
                if (B2.BodyIsValid(collider.BodyId))
                    ApplyMass(collider, collider.BodyId);
                collider.IsMassDirty = false;
            }
            else if (collider.IsMaterialDirty)
            {
                if (B2.BodyIsValid(collider.BodyId))
                    ApplyMaterial(collider);
                collider.IsMaterialDirty = false;
            }

            // Apply per-body gravity override as a manual force each tick.
            if (collider.GravityOverride.HasValue
                && collider.BodyType == PhysicsBodyType.Dynamic
                && B2.BodyIsValid(collider.BodyId)
                && collider.IsSimulationEnabled)
            {
                var massData = B2.BodyGetMassData(collider.BodyId);
                if (massData.mass > 0f)
                {
                    var g = collider.GravityOverride.Value;
                    B2.BodyApplyForceToCenter(collider.BodyId,
                        new B2.Vec2 { x = g.X * massData.mass, y = g.Y * massData.mass },
                        true);
                }
            }

            if (collider.BodyType == PhysicsBodyType.Kinematic
                && B2.BodyIsValid(collider.BodyId)
                && !collider.IsDirty
                && collider.IsSimulationEnabled)
            {
                var pos = transform.Position + collider.Offset;
                var rot = transform.Rotation;

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

                        if (_prevKinematicRotations.TryGetValue(collider.BodyId.index1, out var prevRot))
                        {
                            var angularVelocity = (rot - prevRot) / dt;
                            B2.BodySetAngularVelocity(collider.BodyId, angularVelocity);
                        }
                    }

                    _prevKinematicPositions[collider.BodyId.index1] = pos;
                    _prevKinematicRotations[collider.BodyId.index1] = rot;
                    B2.BodySetTransform(collider.BodyId, new B2.Vec2 { x = pos.X, y = pos.Y },
                        B2.MakeRot(rot));
                }
            }
            else if (collider.BodyType != PhysicsBodyType.Kinematic && collider.IsTeleporting)
            {
                collider.IsTeleporting = false;
            }
        }
    }

    private static void ApplyFilter(PhysicsBodyComponent collider)
    {
        var bodyFilter = new B2.Filter
        {
            categoryBits = 1UL << collider.Layer,
            maskBits = collider.CollisionMask
        };

        if (B2.ShapeIsValid(collider.ShapeId))
            B2.ShapeSetFilter(collider.ShapeId, bodyFilter);

        foreach (var sub in collider.SubShapes)
        {
            if (!B2.ShapeIsValid(sub.ShapeId)) continue;
            var subFilter = new B2.Filter
            {
                categoryBits = 1UL << (sub.Layer ?? collider.Layer),
                maskBits = sub.CollisionMask ?? collider.CollisionMask
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
                joint.JointId = default;
                joint.IsDirty = true;
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
                _physicsWorld.UnregisterJoint(joint, bodyIdA.index1, joint.ConnectedBody?.BodyId.index1 ?? default);
                _physicsWorld.DestroyJoint(joint.JointId);
                joint.JointId = default;
            }
            joint.IsDirty = false;
            return;
        }

        if (!B2.BodyIsValid(bodyIdA) || !B2.BodyIsValid(joint.ConnectedBody.BodyId)) return;

        if (B2.JointIsValid(joint.JointId))
        {
            _physicsWorld.UnregisterJoint(joint, bodyIdA.index1, joint.ConnectedBody.BodyId.index1);
            _physicsWorld.DestroyJoint(joint.JointId);
        }

        joint.JointId = joint.Build(_physicsWorld, bodyIdA);
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

            var force = joint.GetReactionForce();
            var torque = joint.GetReactionTorque();

            if (force.Length() <= joint.BreakForce && MathF.Abs(torque) <= joint.BreakTorque) continue;

            if (joint.ConnectedBody != null)
                _physicsWorld.UnregisterJoint(joint, body.BodyId.index1, joint.ConnectedBody.BodyId.index1);

            B2.DestroyJoint(joint.JointId);
            joint.JointId = default;
            joint.IsDirty = true;
            joint.RaiseBreak();
        }
    }

    private void RebuildBody(Entity entity, PhysicsBodyComponent collider, TransformComponent transform)
    {
        if (collider.Shape == null)
            return;

        var linearVelocity = collider.InitialLinearVelocity;
        var angularVelocity = collider.InitialAngularVelocity;
        if (B2.BodyIsValid(collider.BodyId))
        {
            var lv = B2.BodyGetLinearVelocity(collider.BodyId);
            linearVelocity = new Vector2(lv.x, lv.y);
            angularVelocity = B2.BodyGetAngularVelocity(collider.BodyId);
            DestroyBody(collider);
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
        // Pin B2 gravity scale to 0 when an override is active; the force is applied manually.
        bodyDef.gravityScale = collider.GravityOverride.HasValue ? 0f : collider.GravityScale;
        bodyDef.linearDamping = collider.LinearDamping;
        bodyDef.angularDamping = collider.AngularDamping;
        bodyDef.linearVelocity = new B2.Vec2 { x = linearVelocity.X, y = linearVelocity.Y };
        bodyDef.angularVelocity = angularVelocity;
        bodyDef.isEnabled = collider.IsSimulationEnabled;

        var bodyId = _physicsWorld.CreateBody(&bodyDef);
        collider.BodyId = bodyId;
        _handleToCollider[bodyId.index1] = collider;
        _handleToTransform[bodyId.index1] = transform;
        _prevAwakeState[bodyId.index1] = true;

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
            _physicsWorld.UnregisterJointsForBody(handle);
            _physicsWorld.UntrackBody(collider);
            FlushStalePairs(collider);
            _handleToCollider.Remove(handle);
            _handleToTransform.Remove(handle);
        };

        if (collider.Shape is ChainShape chain)
        {
            if (collider.IsTrigger)
                throw new InvalidOperationException(
                    $"PhysicsBodyComponent on entity '{entity.Name}' has a ChainShape with IsTrigger=true. " +
                    $"Chain shapes do not support trigger/sensor mode.");

            if (collider.IsBullet)
                throw new InvalidOperationException(
                    $"PhysicsBodyComponent on entity '{entity.Name}' has a ChainShape with IsBullet=true. " +
                    $"Chain shapes do not support bullet (continuous collision detection) mode.");

            if (collider.BodyType != PhysicsBodyType.Static)
                throw new InvalidOperationException(
                    $"PhysicsBodyComponent on entity '{entity.Name}' has a ChainShape but BodyType is " +
                    $"{collider.BodyType}. Chain shapes are designed for static terrain and require BodyType.Static.");

            BuildChainShape(collider, bodyId, chain);
            return;
        }

        var shapeDef = B2.DefaultShapeDef();
        shapeDef.isSensor = collider.IsTrigger;
        shapeDef.material.friction = collider.SurfaceFriction;
        shapeDef.material.restitution = collider.Restitution;
        shapeDef.filter.categoryBits = 1UL << collider.Layer;
        shapeDef.filter.maskBits = collider.CollisionMask;
        shapeDef.enableContactEvents = !collider.IsTrigger;
        shapeDef.enableSensorEvents = collider.IsTrigger;
        shapeDef.enableHitEvents = collider.EnableHitEvents;

        collider.ShapeId = BuildShape(entity.Name, collider.Shape, bodyId, &shapeDef);

        foreach (var sub in collider.SubShapes)
        {
            var subDef = shapeDef;
            subDef.isSensor = sub.IsTrigger;
            subDef.material.friction = sub.Friction ?? collider.SurfaceFriction;
            subDef.material.restitution = sub.Restitution ?? collider.Restitution;
            subDef.filter.categoryBits = 1UL << (sub.Layer ?? collider.Layer);
            subDef.filter.maskBits = sub.CollisionMask ?? collider.CollisionMask;
            subDef.enableContactEvents = !sub.IsTrigger;
            subDef.enableSensorEvents = sub.IsTrigger;
            subDef.enableHitEvents = sub.EnableHitEvents ?? collider.EnableHitEvents;
            sub.ShapeId = BuildShape(entity.Name, sub.Definition, bodyId, &subDef);
        }

        ApplyMass(collider, bodyId);
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

        var materials = stackalloc B2.SurfaceMaterial[segmentCount];
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

        fixed (B2.Vec2* ptsPtr = b2pts)
        {
            var chainDef = B2.DefaultChainDef();
            chainDef.points = ptsPtr;
            chainDef.count = chain.Points.Length;
            chainDef.materials = materials;
            chainDef.materialCount = segmentCount;
            chainDef.isLoop = chain.IsLoop;
            chainDef.filter.categoryBits = 1UL << collider.Layer;
            chainDef.filter.maskBits = collider.CollisionMask;
            collider.ChainId = _physicsWorld.CreateChain(bodyId, &chainDef);
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

    private static B2.ShapeId BuildShape(string entityName, ShapeDefinition shape, B2.BodyId bodyId, B2.ShapeDef* shapeDef)
    {
        switch (shape)
        {
            case CircleShape circle:
                {
                    var b2 = new B2.Circle
                    {
                        center = new B2.Vec2 { x = circle.Offset.X, y = circle.Offset.Y },
                        radius = circle.Radius
                    };
                    return B2.CreateCircleShape(bodyId, shapeDef, &b2);
                }
            case BoxShape box:
                {
                    var offset = new B2.Vec2 { x = box.Offset.X, y = box.Offset.Y };
                    var b2 = B2.MakeOffsetBox(box.Width * 0.5f, box.Height * 0.5f, offset, B2.MakeRot(box.Angle));
                    return B2.CreatePolygonShape(bodyId, shapeDef, &b2);
                }
            case CapsuleShape capsule:
                {
                    var b2 = new B2.Capsule
                    {
                        center1 = new B2.Vec2 { x = capsule.Center1.X, y = capsule.Center1.Y },
                        center2 = new B2.Vec2 { x = capsule.Center2.X, y = capsule.Center2.Y },
                        radius = capsule.Radius
                    };
                    return B2.CreateCapsuleShape(bodyId, shapeDef, &b2);
                }
            case PolygonShape poly:
                {
                    var verts = poly.Vertices;
                    var b2Verts = stackalloc B2.Vec2[verts.Length];
                    for (int i = 0; i < verts.Length; i++)
                        b2Verts[i] = new B2.Vec2 { x = verts[i].X, y = verts[i].Y };

                    var hull = B2.ComputeHull(b2Verts, verts.Length);
                    var polygon = B2.MakePolygon(&hull, poly.Radius);
                    return B2.CreatePolygonShape(bodyId, shapeDef, &polygon);
                }
            default:
                throw new InvalidOperationException(
                    $"Unsupported ShapeDefinition type '{shape.GetType().Name}' on entity '{entityName}'.");
        }
    }

    private static void ApplyMass(PhysicsBodyComponent collider, B2.BodyId bodyId)
    {
        if (collider.BodyType != PhysicsBodyType.Dynamic || collider.Mass <= 0f)
            return;

        var massData = B2.BodyGetMassData(bodyId);
        if (massData.mass <= 0f)
        {
            // Box2D does not compute mass for sensor-only shapes. When a Dynamic body has
            // no non-sensor shapes (e.g. an all-trigger body), set the mass data directly
            // so gravity, impulses, and forces still work as expected.
            B2.BodySetMassData(bodyId, new B2.MassData
            {
                mass = collider.Mass,
                center = default,
                rotationalInertia = collider.Mass
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
    }

    private void DestroyBody(PhysicsBodyComponent collider)
    {
        collider.ShouldCollideChanged -= OnShouldCollideChanged;
        if (collider.ShouldCollide != null)
            _shouldCollideCount--;
        foreach (var sub in collider.SubShapes)
        {
            if (sub.ShouldCollide != null)
                _shouldCollideCount--;
        }

        _prevKinematicPositions.Remove(collider.BodyId.index1);
        _prevKinematicRotations.Remove(collider.BodyId.index1);
        _prevAwakeState.Remove(collider.BodyId.index1);
        _physicsWorld.UnregisterJointsForBody(collider.BodyId.index1);
        _physicsWorld.UntrackBody(collider);
        FlushStalePairs(collider);
        _handleToCollider.Remove(collider.BodyId.index1);
        _handleToTransform.Remove(collider.BodyId.index1);
        B2.DestroyBody(collider.BodyId);
        collider.BodyId = default;
        collider.ShapeId = default;
        collider.ChainId = default;
        foreach (var sub in collider.SubShapes)
            sub.ShapeId = default;
        collider.IsDirty = true;
        collider.IsFilterDirty = false;
        collider.IsBodyTypeDirty = false;
        collider.IsMassDirty = false;
        collider.IsMaterialDirty = false;
        collider.IsSimulationEnabledDirty = false;
    }

    private void FlushStalePairs(PhysicsBodyComponent collider)
    {
        PhysicsBodyComponent[]? contactSnapshot = collider.ActiveContactPairs.Count > 0
            ? [.. collider.ActiveContactPairs]
            : null;
        PhysicsBodyComponent[]? sensorSnapshot = collider.ActiveSensorPairs.Count > 0
            ? [.. collider.ActiveSensorPairs]
            : null;

        collider.ActiveContactPairs.Clear();
        collider.ActiveSensorPairs.Clear();
        collider.CollidingEntitiesInternal.Clear();

        if (contactSnapshot != null)
        {
            foreach (var other in contactSnapshot)
            {
                other.ActiveContactPairs.Remove(collider);
                if (collider.Entity != null)
                    other.CollidingEntitiesInternal.Remove(collider.Entity);

                _physicsWorld.UntrackActivePair(collider, other);
                other.RaiseCollisionExit(collider);
                collider.RaiseCollisionExit(other);
            }
        }

        if (sensorSnapshot != null)
        {
            foreach (var other in sensorSnapshot)
            {
                other.ActiveSensorPairs.Remove(collider);
                if (collider.Entity != null)
                    other.CollidingEntitiesInternal.Remove(collider.Entity);

                _physicsWorld.UntrackActivePair(collider, other);
                other.RaiseTriggerExit(collider);
                collider.RaiseTriggerExit(other);
            }
        }
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

            var p = move.transform.p;
            transform.Position = new Vector2(p.x, p.y) - collider.Offset;
            transform.Rotation = MathF.Atan2(move.transform.q.s, move.transform.q.c);

            if (move.fellAsleep)
            {
                _prevAwakeState[move.bodyId.index1] = false;
                collider.NotifyBodySleep();
            }
            else
            {
                // Fire OnBodyWake on the first move event after the body was sleeping.
                if (_prevAwakeState.TryGetValue(move.bodyId.index1, out bool wasAwake) && !wasAwake)
                {
                    _prevAwakeState[move.bodyId.index1] = true;
                    collider.NotifyBodyWake();
                }
            }
        }
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
            var contact = MakeContact(e.manifold);
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
        _activeBodySnapshot.Clear();
        _activeBodySnapshot.AddRange(_physicsWorld.ActiveBodies);

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

                var contact = GetLiveContact(collider, other);

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

        const int maxContacts = 128;
        var contacts = ArrayPool<B2.ContactData>.Shared.Rent(maxContacts);
        try
        {
            fixed (B2.ContactData* ptr = contacts)
            {
                if (a.Shape is ChainShape || b.Shape is ChainShape)
                {
                    int count = B2.BodyGetContactData(a.BodyId, ptr, maxContacts);
                    return FindContactWithBodyId(ptr, count, b.BodyId.index1) ?? CollisionContact.Empty;
                }

                if (B2.ShapeIsValid(a.ShapeId))
                {
                    int count = B2.ShapeGetContactData(a.ShapeId, ptr, maxContacts);
                    var result = FindContactWithBody(ptr, count, a.ShapeId, b.BodyId.index1);
                    if (result.HasValue) return result.Value;
                }

                foreach (var sub in a.SubShapes)
                {
                    if (!B2.ShapeIsValid(sub.ShapeId)) continue;
                    int count = B2.ShapeGetContactData(sub.ShapeId, ptr, maxContacts);
                    var result = FindContactWithBody(ptr, count, sub.ShapeId, b.BodyId.index1);
                    if (result.HasValue) return result.Value;
                }

                if (B2.ShapeIsValid(b.ShapeId))
                {
                    int count = B2.ShapeGetContactData(b.ShapeId, ptr, maxContacts);
                    var result = FindContactWithBody(ptr, count, b.ShapeId, a.BodyId.index1);
                    if (result.HasValue) return result.Value with { Normal = -result.Value.Normal };
                }

                foreach (var sub in b.SubShapes)
                {
                    if (!B2.ShapeIsValid(sub.ShapeId)) continue;
                    int count = B2.ShapeGetContactData(sub.ShapeId, ptr, maxContacts);
                    var result = FindContactWithBody(ptr, count, sub.ShapeId, a.BodyId.index1);
                    if (result.HasValue) return result.Value with { Normal = -result.Value.Normal };
                }
            }
        }
        finally
        {
            ArrayPool<B2.ContactData>.Shared.Return(contacts);
        }

        return CollisionContact.Empty;
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
        _teardownBuffer.Clear();
        _stayContactBuffer.Clear();
        _staySensorBuffer.Clear();
        _activeBodySnapshot.Clear();
    }
}