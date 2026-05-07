using System.Diagnostics;
using System.Numerics;
using Box2D.NET.Bindings;
using Brine2D.Collision;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Query;
using Brine2D.ECS.Systems;
using Brine2D.Physics;
using Microsoft.Extensions.Logging;

namespace Brine2D.Systems.Physics;

/// <summary>
/// Drives <see cref="KinematicCharacterBody"/> components each fixed-update tick.
/// Register two instances in DI — one at <see cref="SystemFixedUpdateOrder.PrePhysics"/>
/// (pre-step: slides velocity, carries platform motion, integrates position) and one at
/// <see cref="SystemFixedUpdateOrder.PostPhysics"/> (post-step: classifies contacts, updates
/// grounded state, snaps to floor, tracks moving platform).
/// Both instances are registered automatically by <c>AddPhysics()</c>.
/// Add both to a scene with <c>world.AddSystem&lt;KinematicCharacterSystem&gt;()</c>.
/// </summary>
/// <remarks>
/// Box2D 3.x does not generate contact manifolds between kinematic and static bodies.
/// All surface detection (ground, wall, ceiling) and pre-step slide deflection therefore
/// use shape-casts rather than <c>BodyGetContactData</c>.  Dynamic bodies (moving platforms,
/// physics objects) are still detected via <c>GetContactsAll</c> because kinematic↔dynamic
/// contacts are generated normally by Box2D.
/// </remarks>
public class KinematicCharacterSystem : FixedUpdateSystemBase
{
    // Small outward skin used for proximity probes so the shape doesn't start inside the surface.
    private const float ProbeSkin = 2f;

    private readonly PhysicsWorld _physicsWorld;
    private readonly bool _isPostStep;
    private readonly ILogger<KinematicCharacterSystem> _logger;
    private CachedEntityQuery<KinematicCharacterBody, PhysicsBodyComponent, TransformComponent>? _query;
    private readonly List<ContactPair> _dynamicContactBuffer = [];
    private readonly HashSet<PhysicsBodyComponent> _probedBodies = new(ReferenceEqualityComparer.Instance);

    public KinematicCharacterSystem(PhysicsWorld physicsWorld, bool isPostStep, ILogger<KinematicCharacterSystem> logger)
    {
        _physicsWorld = physicsWorld;
        _isPostStep = isPostStep;
        _logger = logger;
    }

    public override int FixedUpdateOrder => _isPostStep
        ? SystemFixedUpdateOrder.PostPhysics
        : SystemFixedUpdateOrder.PrePhysics;

    public override void FixedUpdate(IEntityWorld world, GameTime fixedTime)
    {
        _query ??= world
            .CreateCachedQuery<KinematicCharacterBody, PhysicsBodyComponent, TransformComponent>()
            .OnlyEnabled()
            .Build();

        if (_isPostStep)
            PostStep();
        else
            PreStep((float)fixedTime.DeltaTime);
    }

    private void PreStep(float dt)
    {
        foreach (var (_, character, body, transform) in _query!)
        {
            if (!B2.BodyIsValid(body.BodyId)) continue;

            ApplyPlatformCarry(character, body, transform, dt);
            character.FloorBody = null;

            // MoveAndCollide is consumed unconditionally so it never carries over when dt == 0.
            if (character.PendingMoveAndCollide is { } motion)
            {
                ProcessMoveAndCollide(character, body, transform, motion);
                continue;
            }

            if (dt <= 0f) continue;

            ApplyMoveAndSlide(character, body, transform, dt);
        }
    }

    private void ApplyPlatformCarry(KinematicCharacterBody character, PhysicsBodyComponent body,
        TransformComponent transform, float dt)
    {
        if (dt <= 0f) return;
        if (character.FloorBody is not { } floorBody) return;
        if (!B2.BodyIsValid(floorBody.BodyId)) return;
        if (floorBody.BodyType != PhysicsBodyType.Kinematic && floorBody.BodyType != PhysicsBodyType.Dynamic) return;

        var b2Pos = B2.BodyGetPosition(floorBody.BodyId);
        var platformEcsPos = floorBody.Entity?.GetComponent<TransformComponent>()?.Position ?? default;
        var platformDelta = (platformEcsPos + floorBody.Offset) - new Vector2(b2Pos.x, b2Pos.y);

        if (character.EnableDebugLogging)
            _logger.LogDebug("Carry: ecsPos={EcsPos} b2Pos=<{B2PosX:F2},{B2PosY:F2}> delta={PlatformDelta} pos={TransformPosition}",
                platformEcsPos, b2Pos.x, b2Pos.y, platformDelta, transform.Position);

        if (platformDelta != Vector2.Zero)
        {
            var castFilter = BuildCastFilter(body);
            float deltaLen = platformDelta.Length();
            var deltaDir = platformDelta / deltaLen;

            var platformHit = ShapeCastBodyShape(body, transform, deltaDir,
                deltaLen + ProbeSkin * 2f,
                castFilter,
                originOffset: -deltaDir * ProbeSkin);

            if (platformHit != null
                && !IsOneWayPassThrough(platformHit.Value)
                && Vector2.Dot(platformHit.Value.Normal, deltaDir) < 0f)
            {
                // Keep ProbeSkin clearance; negative adjustment pushes away from wall.
                float adjustment = platformHit.Value.Distance - 2f * ProbeSkin;
                transform.Position += deltaDir * adjustment;

                if (character.EnableDebugLogging)
                    _logger.LogDebug("Carry blocked by {EntityName} dist={Distance:F3} adj={Adjustment:F3} normal={Normal} newPos={NewPosition}",
                        platformHit.Value.Component?.Entity?.Name ?? "static",
                        platformHit.Value.Distance,
                        adjustment,
                        platformHit.Value.Normal,
                        transform.Position);
            }
            else
            {
                transform.Position += platformDelta;
            }
        }

        float angularDelta = character.PlatformAngularVelocity * dt;
        if (angularDelta != 0f)
        {
            var offset = transform.Position - character.PlatformCenter;
            float cos = MathF.Cos(angularDelta);
            float sin = MathF.Sin(angularDelta);
            transform.Position = character.PlatformCenter + new Vector2(
                offset.X * cos - offset.Y * sin,
                offset.X * sin + offset.Y * cos);
            transform.Rotation += angularDelta;
        }
    }

    private void ProcessMoveAndCollide(KinematicCharacterBody character, PhysicsBodyComponent body,
        TransformComponent transform, Vector2 motion)
    {
        character.PendingMoveAndCollide = null;
        character.LastMoveAndCollideHit = null;
        character.EffectiveVelocity = Vector2.Zero;

        float motionLen = motion.Length();
        if (motionLen <= 0f)
        {
            character.MotionRemainder = Vector2.Zero;
            return;
        }

        var castFilter = BuildCastFilter(body);
        var dir = motion / motionLen;
        var castHit = ShapeCastBodyShape(body, transform, dir, motionLen, castFilter);

        if (castHit is { } h)
        {
            transform.Position += dir * h.Distance;
            character.LastMoveAndCollideHit = h;
            character.MotionRemainder = motion - dir * h.Distance;
        }
        else
        {
            transform.Position += motion;
            character.MotionRemainder = Vector2.Zero;
        }
    }

    private void ApplyMoveAndSlide(KinematicCharacterBody character, PhysicsBodyComponent body,
        TransformComponent transform, float dt)
    {
        var velocity = character.Velocity;

        if (character.MaxSpeed > 0f)
        {
            float speed = velocity.Length();
            if (speed > character.MaxSpeed)
                velocity = velocity / speed * character.MaxSpeed;
        }

        float remainingTime = dt;
        bool hitFloorThisStep = false;
        Vector2 floorNormalThisStep = Vector2.Zero;

        if (velocity != Vector2.Zero)
        {
            var castFilter = BuildCastFilter(body);
            int maxSlides = Math.Max(1, character.MaxSlides);

            var gravity = _physicsWorld.Gravity;
            var gravityLen = gravity.Length();
            var upDir = character.UpDirection is { } up
                ? Vector2.Normalize(up)
                : (gravityLen > 0f ? -gravity / gravityLen : new Vector2(0f, -1f));
            float cosFloorLimit = MathF.Cos(character.FloorAngleLimit);

            foreach (var contact in character.SlideCollisions)
            {
                var n = contact.Contact.Normal;
                float vDotN = Vector2.Dot(velocity, n);
                if (vDotN < 0f)
                {
                    if (character.EnableDebugLogging)
                        _logger.LogDebug("PreDeflect: normal={Normal} vDotN={VDotN:F3} contact={Contact}", velocity -= vDotN * n);
                }
            }

            if (character.EnableDebugLogging && velocity != Vector2.Zero)
                _logger.LogDebug("SlideStart: vel={Velocity} pos={Position} remainingTime={RemainingTime:F4}", velocity, transform.Position, remainingTime);

            for (int iter = 0; iter < maxSlides && remainingTime > 1e-6f; iter++)
            {
                float speed = velocity.Length();
                if (speed == 0f) break;

                var velDir = velocity / speed;
                float moveDist = speed * remainingTime;

                var hit = ShapeCastBodyShape(body, transform, velDir, moveDist + ProbeSkin, castFilter);
                if (hit == null)
                {
                    var probeHit = ShapeCastBodyShape(body, transform, velDir, ProbeSkin * 2f + 1f,
                        castFilter, originOffset: -velDir * ProbeSkin);

                    if (character.EnableDebugLogging)
                        _logger.LogDebug(
                            "iter={Iter} NULL cast, velDir={VelDir} probeHit={ProbeHit} probeBody={ProbeBody}",
                            iter, velDir, probeHit?.Normal.ToString() ?? "null",
                            probeHit?.Component?.Entity?.Name ?? "none");

                    if (probeHit == null) break;
                    if (IsOneWayPassThrough(probeHit.Value)) break;

                    var pn = probeHit.Value.Normal;

                    // Retracted probe hit something behind the cast direction (e.g. floor when jumping) — ignore.
                    if (Vector2.Dot(pn, velDir) > 0f) break;

                    float pvDotN = Vector2.Dot(velocity, pn);
                    if (pvDotN >= 0f)
                    {
                        if (pvDotN > 0f)
                            velocity -= pvDotN * pn;
                        break;
                    }
                    velocity -= pvDotN * pn;
                    continue;
                }

                if (IsOneWayPassThrough(hit.Value))
                {
                    if (character.EnableDebugLogging)
                        _logger.LogDebug("iter={Iter} one-way passthrough hit, breaking. normal={Normal} body={Body}",
                            iter, hit.Value.Normal, hit.Value.Component?.Entity?.Name ?? "none");
                    break;
                }

                var n = hit.Value.Normal;
                float vDotN = Vector2.Dot(velocity, n);
                if (vDotN >= 0f) break;

                float travelDist = MathF.Max(0f, hit.Value.Distance - ProbeSkin);
                if (character.EnableDebugLogging)
                    _logger.LogDebug("iter={Iter} hit: body={Body} normal={Normal} dist={Distance:F2} travel={Travel:F2} vDotN={VDotN:F3}",
                        iter, hit.Value.Component?.Entity?.Name ?? "static", n, hit.Value.Distance, travelDist, vDotN);

                transform.Position += velDir * travelDist;
                remainingTime = MathF.Max(0f, remainingTime - travelDist / speed);

                if (Vector2.Dot(n, upDir) >= cosFloorLimit)
                {
                    hitFloorThisStep = true;
                    floorNormalThisStep = n;
                }

                velocity -= vDotN * n;
            }
        }

        if (character.StopOnSlope
            && (hitFloorThisStep || (character.IsGrounded && character.FloorNormal != Vector2.Zero)))
        {
            var activeNormal = hitFloorThisStep ? floorNormalThisStep : character.FloorNormal;
            float vDotFloor = Vector2.Dot(velocity, activeNormal);
            if (vDotFloor < 0f)
                velocity -= vDotFloor * activeNormal;
        }

        if (character.StepHeight > 0f && velocity != Vector2.Zero)
            velocity = TryStepUp(character, body, transform, velocity, ref remainingTime);

        if (character.PushForce > 0f)
        {
            _physicsWorld.GetContactsAll(body, _dynamicContactBuffer);

            Dictionary<PhysicsBodyComponent, Vector2>? pushAccum = null;

            foreach (var contact in _dynamicContactBuffer)
            {
                if (contact.Other == null) continue;
                if (contact.Other.BodyType != PhysicsBodyType.Dynamic) continue;
                if (!B2.BodyIsValid(contact.Other.BodyId)) continue;

                float vDotN = Vector2.Dot(character.Velocity, -contact.Contact.Normal);
                if (vDotN <= 0f) continue;

                var massData = B2.BodyGetMassData(contact.Other.BodyId);
                if (massData.mass <= 0f) continue;

                pushAccum ??= new Dictionary<PhysicsBodyComponent, Vector2>(ReferenceEqualityComparer.Instance);
                if (pushAccum.TryGetValue(contact.Other, out var existing))
                    pushAccum[contact.Other] = existing + contact.Contact.Normal;
                else
                    pushAccum[contact.Other] = contact.Contact.Normal;
            }

            if (pushAccum != null)
            {
                foreach (var (target, normal) in pushAccum)
                {
                    if (normal != Vector2.Zero)
                        target.ApplyLinearImpulse(-Vector2.Normalize(normal) * character.PushForce * remainingTime);
                }
            }

            _dynamicContactBuffer.Clear();
        }

        character.EffectiveVelocity = velocity;
        if (character.EnableDebugLogging && (velocity != Vector2.Zero || remainingTime <= 1e-6f))
            _logger.LogDebug("FinalIntegrate: vel={Velocity} remaining={Remaining:F4} delta={Delta} newPos={NewPos}",
                velocity, remainingTime, velocity * remainingTime, transform.Position + velocity * remainingTime);
        transform.Position += velocity * remainingTime;
    }

    /// <summary>
    /// Returns true when a shape cast hit lands on the passthrough (non-solid) face of a
    /// one-way platform. The hit normal points toward the caster; if it opposes the platform's
    /// solid-face normal the caster is approaching from below and should not be blocked.
    /// </summary>
    private static bool IsOneWayPassThrough(ShapeCastHit hit)
    {
        if (hit.Component is not { IsOneWayPlatform: true } platform)
            return false;

        var solidNormal = Vector2.Normalize(platform.PlatformNormalDirection);
        return Vector2.Dot(hit.Normal, solidNormal) < 0f;
    }

    private Vector2 TryStepUp(KinematicCharacterBody character, PhysicsBodyComponent body,
        TransformComponent transform, Vector2 velocity, ref float remainingTime)
    {
        float speed = velocity.Length();
        var moveDir = velocity / speed;
        float moveDist = speed * remainingTime;
        var castFilter = BuildCastFilter(body);

        var horizontalHit = ShapeCastBodyShape(body, transform.Position, transform.Rotation, moveDir, moveDist, castFilter);
        if (horizontalHit == null) return velocity;

        var upDir = character.UpDirection is { } up ? Vector2.Normalize(up) : new Vector2(0f, -1f);
        var downDir = -upDir;

        var ceilingHit = ShapeCastBodyShape(body, transform.Position, transform.Rotation, upDir, character.StepHeight, castFilter);
        float rawClearance = ceilingHit?.Distance ?? character.StepHeight;
        float clearance = MathF.Max(0f, rawClearance - ProbeSkin);
        if (clearance < 1f) return velocity;

        var raisedPosition = transform.Position + upDir * clearance;

        var raisedHit = ShapeCastBodyShape(body, raisedPosition, transform.Rotation, moveDir, moveDist, castFilter);
        if (raisedHit != null) return velocity;

        var forwardPosition = raisedPosition + moveDir * moveDist;

        var landHit = ShapeCastBodyShape(body, forwardPosition, transform.Rotation, downDir, character.StepHeight + 1f, castFilter);
        if (landHit == null) return velocity;

        float landDist = MathF.Max(0f, landHit.Value.Distance - ProbeSkin);
        transform.Position = forwardPosition + downDir * landDist;

        if (B2.BodyIsValid(body.BodyId))
        {
            var newBodyPos = transform.Position + body.Offset;
            B2.BodySetTransform(body.BodyId,
                new B2.Vec2 { x = newBodyPos.X, y = newBodyPos.Y },
                B2.BodyGetRotation(body.BodyId));
        }

        body.IsTeleporting = true;
        remainingTime = 0f;

        return moveDir * speed;
    }

    private void PostStep()
    {
        var gravity = _physicsWorld.Gravity;
        var gravityLen = gravity.Length();
        var worldDownDir = gravityLen > 0f ? gravity / gravityLen : new Vector2(0f, 1f);

        foreach (var (_, character, body, transform) in _query!)
        {
            character.WasGrounded = character.IsGrounded;
            var previousFloorBody = character.FloorBody;
            character.FloorBody = null;

            if (!B2.BodyIsValid(body.BodyId))
            {
                character.IsGrounded = false;
                character.IsOnWall = false;
                character.IsOnCeiling = false;
                character.FloorNormal = Vector2.Zero;
                character.WallNormal = Vector2.Zero;
                character.CeilingNormal = Vector2.Zero;
                character.PlatformVelocity = Vector2.Zero;
                character.PlatformAngularVelocity = 0f;
                character.PlatformCenter = Vector2.Zero;
                character.SlideCollisions.Clear();
                continue;
            }

            var upDir = character.UpDirection is { } overrideUp
                ? Vector2.Normalize(overrideUp)
                : -worldDownDir;
            var downDir = -upDir;

            float cosFloorLimit = MathF.Cos(character.FloorAngleLimit);
            float cosCeilingLimit = MathF.Cos(character.CeilingAngleLimit);
            float cosWallLimit = float.IsPositiveInfinity(character.WallAngleLimit)
                ? -1f
                : MathF.Cos(character.WallAngleLimit);

            var castFilter = BuildCastFilter(body);

            character.SlideCollisions.Clear();
            _probedBodies.Clear();

            var floorAccum = Vector2.Zero;
            int floorCount = 0;
            var wallAccum = Vector2.Zero;
            int wallCount = 0;
            var bestCeilingNormal = Vector2.Zero;
            float bestCeilingDot = float.MinValue;
            PhysicsBodyComponent? platformFloorBody = null;

            ProbeDirection(body, transform, downDir, castFilter, cosFloorLimit, cosCeilingLimit, cosWallLimit,
                upDir, downDir, character,
                ref floorAccum, ref floorCount, ref wallAccum, ref wallCount,
                ref bestCeilingNormal, ref bestCeilingDot, ref platformFloorBody);

            ProbeDirection(body, transform, upDir, castFilter, cosFloorLimit, cosCeilingLimit, cosWallLimit,
                upDir, downDir, character,
                ref floorAccum, ref floorCount, ref wallAccum, ref wallCount,
                ref bestCeilingNormal, ref bestCeilingDot, ref platformFloorBody);

            var effVel = character.EffectiveVelocity;
            Vector2 horizDir;
            if (effVel != Vector2.Zero)
            {
                var velFlat = effVel - Vector2.Dot(effVel, upDir) * upDir;
                horizDir = velFlat.Length() > 0.001f ? Vector2.Normalize(velFlat) : Vector2.Zero;
            }
            else
            {
                horizDir = new Vector2(upDir.Y, -upDir.X);
            }

            if (horizDir != Vector2.Zero)
            {
                ProbeDirection(body, transform, horizDir, castFilter, cosFloorLimit, cosCeilingLimit, cosWallLimit,
                    upDir, downDir, character,
                    ref floorAccum, ref floorCount, ref wallAccum, ref wallCount,
                    ref bestCeilingNormal, ref bestCeilingDot, ref platformFloorBody);

                ProbeDirection(body, transform, -horizDir, castFilter, cosFloorLimit, cosCeilingLimit, cosWallLimit,
                    upDir, downDir, character,
                    ref floorAccum, ref floorCount, ref wallAccum, ref wallCount,
                    ref bestCeilingNormal, ref bestCeilingDot, ref platformFloorBody);
            }

            _physicsWorld.GetContactsAll(body, _dynamicContactBuffer);
            foreach (var contact in _dynamicContactBuffer)
            {
                bool alreadyProbed = contact.Other != null && !_probedBodies.Add(contact.Other);

                if (!alreadyProbed)
                    character.SlideCollisions.Add(contact);

                var n = contact.Contact.Normal;
                float upDot = Vector2.Dot(n, upDir);
                if (upDot >= cosFloorLimit)
                {
                    floorAccum += n;
                    floorCount++;
                    if (platformFloorBody == null && contact.Other != null
                        && (contact.Other.BodyType == PhysicsBodyType.Kinematic
                            || contact.Other.BodyType == PhysicsBodyType.Dynamic))
                    {
                        platformFloorBody = contact.Other;
                    }
                }
                else
                {
                    float downDot = Vector2.Dot(n, downDir);
                    if (downDot >= cosCeilingLimit)
                    {
                        if (downDot > bestCeilingDot)
                        {
                            bestCeilingDot = downDot;
                            bestCeilingNormal = n;
                        }
                    }
                    else
                    {
                        float horizDot = MathF.Sqrt(MathF.Max(0f, 1f - upDot * upDot));
                        if (horizDot >= cosWallLimit)
                        {
                            wallAccum += n;
                            wallCount++;
                        }
                    }
                }
            }
            _dynamicContactBuffer.Clear();

            character.IsGrounded = floorCount > 0;
            character.FloorNormal = floorCount > 0
                ? Vector2.Normalize(floorAccum)
                : Vector2.Zero;
            character.IsOnCeiling = bestCeilingDot >= cosCeilingLimit;
            character.CeilingNormal = character.IsOnCeiling ? bestCeilingNormal : Vector2.Zero;
            character.IsOnWall = wallCount > 0;
            character.WallNormal = wallCount > 0
                ? Vector2.Normalize(wallAccum)
                : Vector2.Zero;

            if (platformFloorBody != null)
            {
                var platformPos = B2.BodyGetPosition(platformFloorBody.BodyId);
                character.FloorBody = platformFloorBody;
                character.PlatformVelocity = platformFloorBody.LinearVelocity;
                character.PlatformAngularVelocity = B2.BodyGetAngularVelocity(platformFloorBody.BodyId);
                character.PlatformCenter = new Vector2(platformPos.x, platformPos.y);
            }
            else
            {
                character.PlatformVelocity = Vector2.Zero;
                character.PlatformAngularVelocity = 0f;
                character.PlatformCenter = Vector2.Zero;
            }

            // Snap to floor.
            if (!character.IsGrounded && character.WasGrounded && character.SnapDistance > 0f)
            {
                float upwardSpeed = Vector2.Dot(character.EffectiveVelocity, upDir);
                if (upwardSpeed <= 0f)
                {
                    var bodyPos = B2.BodyGetPosition(body.BodyId);
                    var aabb = B2.BodyComputeAABB(body.BodyId);

                    var bodyOrigin = new Vector2(bodyPos.x, bodyPos.y);
                    var aabbMin = new Vector2(aabb.lowerBound.x, aabb.lowerBound.y);
                    var aabbMax = new Vector2(aabb.upperBound.x, aabb.upperBound.y);

                    float feetDist = MathF.Max(
                        MathF.Max(
                            Vector2.Dot(new Vector2(aabbMin.X, aabbMin.Y) - bodyOrigin, downDir),
                            Vector2.Dot(new Vector2(aabbMax.X, aabbMin.Y) - bodyOrigin, downDir)),
                        MathF.Max(
                            Vector2.Dot(new Vector2(aabbMin.X, aabbMax.Y) - bodyOrigin, downDir),
                            Vector2.Dot(new Vector2(aabbMax.X, aabbMax.Y) - bodyOrigin, downDir)));

                    feetDist = MathF.Max(feetDist, 0f);

                    var snapFilter = BuildCastFilter(body);
                    var hit = ShapeCastBodyShape(body, transform, downDir, feetDist + character.SnapDistance, snapFilter);

                    if (hit is { } h && h.Distance > 0f && h.Distance <= character.SnapDistance)
                    {
                        transform.Position += downDir * h.Distance;

                        if (B2.BodyIsValid(body.BodyId))
                        {
                            var newBodyPos = transform.Position + body.Offset;
                            B2.BodySetTransform(body.BodyId,
                                new B2.Vec2 { x = newBodyPos.X, y = newBodyPos.Y },
                                B2.BodyGetRotation(body.BodyId));
                        }

                        body.IsTeleporting = true;
                        character.IsGrounded = true;
                        character.FloorNormal = h.Normal;

                        if (h.Component != null
                            && (h.Component.BodyType == PhysicsBodyType.Kinematic
                                || h.Component.BodyType == PhysicsBodyType.Dynamic))
                        {
                            var platformPos = B2.BodyGetPosition(h.Component.BodyId);
                            character.FloorBody = h.Component;
                            character.PlatformVelocity = h.Component.LinearVelocity;
                            character.PlatformAngularVelocity = B2.BodyGetAngularVelocity(h.Component.BodyId);
                            character.PlatformCenter = new Vector2(platformPos.x, platformPos.y);
                        }
                    }
                }
            }

            if (character.IsGrounded && !character.WasGrounded)
                character.RaiseLanded();
            else if (!character.IsGrounded && character.WasGrounded)
                character.RaiseAirborne();
        }
    }

    private void ProbeDirection(
        PhysicsBodyComponent body, TransformComponent transform,
        Vector2 probeDir, PhysicsQueryFilter castFilter,
        float cosFloorLimit, float cosCeilingLimit, float cosWallLimit,
        Vector2 upDir, Vector2 downDir,
        KinematicCharacterBody character,
        ref Vector2 floorAccum, ref int floorCount,
        ref Vector2 wallAccum, ref int wallCount,
        ref Vector2 bestCeilingNormal, ref float bestCeilingDot,
        ref PhysicsBodyComponent? platformFloorBody)
    {
        var originOffset = -probeDir * ProbeSkin;
        var hit = ShapeCastBodyShape(body, transform, probeDir, ProbeSkin * 2f + 1f, castFilter, originOffset);
        if (hit == null) return;

        if (IsOneWayPassThrough(hit.Value)) return;

        // Skip classification if the character is moving with the solid normal (e.g. jumping through).
        if (hit.Value.Component is { IsOneWayPlatform: true } owp)
        {
            var solidNormal = Vector2.Normalize(owp.PlatformNormalDirection);
            if (Vector2.Dot(character.EffectiveVelocity, solidNormal) > 0f) return;
        }

        var n = hit.Value.Normal;

        // Track first encounter per body to avoid duplicate SlideCollisions entries.
        bool firstEncounter = hit.Value.Component == null || _probedBodies.Add(hit.Value.Component);
        if (firstEncounter)
        {
            character.SlideCollisions.Add(new ContactPair
            {
                Other = hit.Value.Component,
                Contact = new CollisionContact { Normal = n }
            });
        }

        float upDot = Vector2.Dot(n, upDir);
        if (upDot >= cosFloorLimit)
        {
            floorAccum += n;
            floorCount++;
            if (platformFloorBody == null && hit.Value.Component != null
                && (hit.Value.Component.BodyType == PhysicsBodyType.Kinematic
                    || hit.Value.Component.BodyType == PhysicsBodyType.Dynamic))
            {
                platformFloorBody = hit.Value.Component;
            }
        }
        else
        {
            float downDot = Vector2.Dot(n, downDir);
            if (downDot >= cosCeilingLimit)
            {
                if (downDot > bestCeilingDot)
                {
                    bestCeilingDot = downDot;
                    bestCeilingNormal = n;
                }
            }
            else
            {
                float horizDot = MathF.Sqrt(MathF.Max(0f, 1f - upDot * upDot));
                if (horizDot >= cosWallLimit)
                {
                    wallAccum += n;
                    wallCount++;
                }
            }
        }
    }

    private ShapeCastHit? ShapeCastBodyShape(PhysicsBodyComponent body, TransformComponent transform,
        Vector2 direction, float maxDistance, PhysicsQueryFilter filter, Vector2 originOffset = default)
        => ShapeCastBodyShape(body, transform.Position, transform.Rotation, direction, maxDistance, filter, originOffset);

    private ShapeCastHit? ShapeCastBodyShape(PhysicsBodyComponent body, Vector2 position, float rotation,
        Vector2 direction, float maxDistance, PhysicsQueryFilter filter, Vector2 originOffset = default)
    {
        var bodyOrigin = position + body.Offset + originOffset;
        var best = ShapeCastSingleShape(body.Shape, bodyOrigin, rotation, direction, maxDistance, filter);

        foreach (var sub in body.SubShapes)
        {
            if (sub.IsTrigger) continue;
            var candidate = ShapeCastSingleShape(sub.Definition, bodyOrigin, rotation, direction, maxDistance, filter);
            if (candidate is { } c && (best == null || c.Fraction < best.Value.Fraction))
                best = c;
        }

        return best;
    }

    private ShapeCastHit? ShapeCastSingleShape(ShapeDefinition? shape, Vector2 bodyOrigin, float rotation,
        Vector2 direction, float maxDistance, PhysicsQueryFilter filter)
    {
        return shape switch
        {
            CircleShape circle => _physicsWorld.ShapeCastClosest(
                bodyOrigin + circle.Offset, circle.Radius, direction, maxDistance, filter),

            CapsuleShape capsule => _physicsWorld.ShapeCastClosest(
                bodyOrigin + capsule.Offset + capsule.Center1,
                bodyOrigin + capsule.Offset + capsule.Center2,
                capsule.Radius, direction, maxDistance, filter),

            BoxShape box => _physicsWorld.ShapeCastClosest(
                bodyOrigin + box.Offset, box.Width / 2f, box.Height / 2f, box.Angle + rotation,
                direction, maxDistance, filter),

            PolygonShape polygon => ShapeCastPolygon(polygon, rotation, bodyOrigin, direction, maxDistance, filter),

            null => null,

            _ => FallbackRaycast(shape, direction, bodyOrigin, maxDistance, filter)
        };
    }

    private ShapeCastHit? FallbackRaycast(ShapeDefinition shape, Vector2 direction, Vector2 origin,
        float maxDistance, PhysicsQueryFilter filter)
    {
        Trace.TraceWarning(
            $"[Brine2D] KinematicCharacterSystem: unsupported shape type '{shape.GetType().Name}' on kinematic body — " +
            "falling back to point raycast. Ground/wall/ceiling detection may be inaccurate.");

        var rayHit = _physicsWorld.RaycastClosest(origin, direction, maxDistance, filter);
        if (rayHit == null) return null;

        return new ShapeCastHit
        {
            Point = rayHit.Value.Point,
            Normal = rayHit.Value.Normal,
            Fraction = rayHit.Value.Fraction,
            Distance = rayHit.Value.Distance,
            Component = rayHit.Value.Component,
            SubShape = rayHit.Value.SubShape
        };
    }

    private ShapeCastHit? ShapeCastPolygon(PolygonShape polygon, float rotation, Vector2 bodyOrigin,
        Vector2 direction, float maxDistance, PhysicsQueryFilter filter)
    {
        var vertices = polygon.VerticesSpan;
        var worldVerts = new Vector2[vertices.Length];
        float cos = MathF.Cos(rotation);
        float sin = MathF.Sin(rotation);
        var origin = bodyOrigin + polygon.Offset;

        for (int i = 0; i < vertices.Length; i++)
        {
            var lv = vertices[i];
            worldVerts[i] = origin + new Vector2(lv.X * cos - lv.Y * sin, lv.X * sin + lv.Y * cos);
        }

        return _physicsWorld.ShapeCastClosest(worldVerts, direction, maxDistance, filter);
    }

    private ShapeCastHit? FallbackRaycast(Vector2 direction, Vector2 origin, float maxDistance,
        PhysicsQueryFilter filter)
    {
        var rayHit = _physicsWorld.RaycastClosest(origin, direction, maxDistance, filter);
        if (rayHit == null) return null;

        return new ShapeCastHit
        {
            Point = rayHit.Value.Point,
            Normal = rayHit.Value.Normal,
            Fraction = rayHit.Value.Fraction,
            Distance = rayHit.Value.Distance,
            Component = rayHit.Value.Component,
            SubShape = rayHit.Value.SubShape
        };
    }

    private static PhysicsQueryFilter BuildCastFilter(PhysicsBodyComponent body) => new()
    {
        ExcludeSensors = true,
        ExcludeBody = body,
        CategoryMask = body.CategoryBits != 0 ? body.CategoryBits : 1UL << body.Layer,
        CollisionMask = body.CollisionMask
    };
}

/// <summary>
/// Pre-physics instance of <see cref="KinematicCharacterSystem"/>.
/// Registered separately in DI so both the pre- and post-step instances can be resolved
/// via <c>AddSystem</c> without DI type-key collisions.
/// </summary>
public sealed class PrePhysicsKinematicCharacterSystem : KinematicCharacterSystem
{
    public PrePhysicsKinematicCharacterSystem(PhysicsWorld physicsWorld, ILogger<PrePhysicsKinematicCharacterSystem> logger)
        : base(physicsWorld, isPostStep: false, logger) { }
}

/// <summary>
/// Post-physics instance of <see cref="KinematicCharacterSystem"/>.
/// Registered separately in DI so both the pre- and post-step instances can be resolved
/// via <c>AddSystem</c> without DI type-key collisions.
/// </summary>
public sealed class PostPhysicsKinematicCharacterSystem : KinematicCharacterSystem
{
    public PostPhysicsKinematicCharacterSystem(PhysicsWorld physicsWorld, ILogger<PostPhysicsKinematicCharacterSystem> logger)
        : base(physicsWorld, isPostStep: true, logger) { }
}