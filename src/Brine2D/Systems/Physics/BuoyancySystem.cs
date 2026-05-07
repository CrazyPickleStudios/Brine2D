using System.Numerics;
using Box2D.NET.Bindings;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Query;
using Brine2D.ECS.Systems;
using Brine2D.Physics;

namespace Brine2D.Systems.Physics;

/// <summary>
/// Applies buoyancy, drag, and flow forces to dynamic bodies overlapping any
/// <see cref="BuoyancyZoneComponent"/> trigger each fixed-update pre-physics tick.
/// Registered automatically by <c>AddPhysics()</c>.
/// </summary>
public sealed class BuoyancySystem : FixedUpdateSystemBase
{
    private readonly PhysicsWorld _physicsWorld;
    private CachedEntityQuery<BuoyancyZoneComponent, PhysicsBodyComponent>? _query;
    private readonly List<OverlapHit> _overlapBuffer = [];

    public BuoyancySystem(PhysicsWorld physicsWorld)
    {
        _physicsWorld = physicsWorld;
    }

    public override int FixedUpdateOrder => SystemFixedUpdateOrder.PrePhysics;

    public override void FixedUpdate(IEntityWorld world, GameTime fixedTime)
    {
        _query ??= world
            .CreateCachedQuery<BuoyancyZoneComponent, PhysicsBodyComponent>()
            .OnlyEnabled()
            .Build();

        var gravity = _physicsWorld.Gravity;
        var gravityLen = gravity.Length();
        if (gravityLen == 0f) return;

        var upDir = -(gravity / gravityLen);

        foreach (var (_, zone, zoneBody) in _query!)
        {
            if (!B2.BodyIsValid(zoneBody.BodyId)) continue;

            _physicsWorld.OverlapBodyAll(zoneBody, _overlapBuffer);

            var zoneAabb = B2.BodyComputeAABB(zoneBody.BodyId);

            foreach (var hit in _overlapBuffer)
            {
                var other = hit.Component;
                if (other == null || !B2.BodyIsValid(other.BodyId)) continue;
                if (other.BodyType != PhysicsBodyType.Dynamic) continue;

                var bodyAabb = B2.BodyComputeAABB(other.BodyId);

                float ix0 = MathF.Max(zoneAabb.lowerBound.x, bodyAabb.lowerBound.x);
                float iy0 = MathF.Max(zoneAabb.lowerBound.y, bodyAabb.lowerBound.y);
                float ix1 = MathF.Min(zoneAabb.upperBound.x, bodyAabb.upperBound.x);
                float iy1 = MathF.Min(zoneAabb.upperBound.y, bodyAabb.upperBound.y);
                float intersectArea = MathF.Max(0f, ix1 - ix0) * MathF.Max(0f, iy1 - iy0);
                float bodyArea = (bodyAabb.upperBound.x - bodyAabb.lowerBound.x)
                               * (bodyAabb.upperBound.y - bodyAabb.lowerBound.y);

                float fraction = bodyArea > 0f ? intersectArea / bodyArea : 0f;
                if (fraction <= 0f) continue;

                float mass = other.Mass;

                // Buoyancy: counteracts gravity proportionally to submersion and fluid density.
                var buoyancyForce = -gravity * (mass * zone.FluidDensity * fraction);

                // Drag + flow: damp relative velocity between body and fluid.
                var relativeVelocity = other.LinearVelocity - zone.FlowVelocity;
                var dragForce = -relativeVelocity * (zone.LinearDrag * mass * fraction);

                other.ApplyForce(buoyancyForce + dragForce);
            }
        }
    }
}