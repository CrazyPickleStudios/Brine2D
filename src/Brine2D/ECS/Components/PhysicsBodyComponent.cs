using System.Buffers;
using System.Diagnostics;
using System.Numerics;
using Box2D.NET.Bindings;
using Brine2D.Collision;
using Brine2D.Physics;

namespace Brine2D.ECS.Components;

/// <summary>
/// Component that defines a physics body and collision shape for an entity.
/// Set <see cref="Shape"/> to any <see cref="ShapeDefinition"/> subtype to configure the
/// primary shape. Additional shapes can be attached via <see cref="AddSubShape"/>.
/// Pure data — logic handled by the physics system.
/// </summary>
public class PhysicsBodyComponent : Component
{
    public const int MaxPolygonVertices = 8;

    private ShapeDefinition? _shape;
    private readonly HashSet<Entity> _collidingEntities = [];
    private readonly List<SubShape> _subShapes = [];

    internal B2.BodyId BodyId { get; set; }

    internal B2.ShapeId ShapeId { get; set; }

    internal B2.ChainId ChainId { get; set; }

    internal bool IsDirty { get; set; } = true;

    internal PhysicsWorld? World { get; set; }

    internal bool HasCollisionStaySubscribers =>
        OnCollisionStay != null || OnCollisionStayWithShape != null;

    /// <summary>
    /// Set by the physics system when only the collision filter (layer/mask) has changed on a
    /// live body. The system applies a lightweight <c>B2.ShapeSetFilter</c> call instead of a
    /// full body rebuild, preserving velocity and sleeping state.
    /// </summary>
    internal bool IsFilterDirty { get; set; }

    /// <summary>
    /// Set by the physics system when only <see cref="BodyType"/> has changed on a live body.
    /// The system calls <c>B2.BodySetType</c> instead of a full rebuild, preserving position,
    /// velocity, and all attached shapes.
    /// </summary>
    internal bool IsBodyTypeDirty { get; set; }

    /// <summary>
    /// Set by the <see cref="Mass"/> setter when the body is already live. The system calls
    /// <c>B2.BodyApplyMassFromShapes</c> instead of a full rebuild, preserving position,
    /// velocity, contacts, and sleeping state.
    /// </summary>
    internal bool IsMassDirty { get; set; }

    /// <summary>
    /// Set when <see cref="SurfaceFriction"/>, <see cref="Restitution"/>, or
    /// <see cref="EnableHitEvents"/> changes on a live body. The system calls
    /// <c>B2.ShapeSetFriction</c>, <c>B2.ShapeSetRestitution</c>, and
    /// <c>B2.ShapeEnableHitEvents</c> instead of a full rebuild.
    /// </summary>
    internal bool IsMaterialDirty { get; set; }

    /// <summary>
    /// Set when <see cref="SubShape.IsTrigger"/> changes on a live body sub-shape. The system
    /// calls <c>B2.ShapeEnableSensorEvents</c> and <c>B2.ShapeEnableContactEvents</c> on the
    /// affected sub-shape, updates density, and flushes stale contact/sensor pairs — no full
    /// rebuild is required.
    /// </summary>
    internal bool IsSubShapeTriggerDirty { get; set; }

    /// <summary>
    /// Set by <see cref="Teleport"/> to tell the physics system to reset the kinematic
    /// previous-position and previous-rotation records, suppressing the phantom velocity that
    /// would otherwise be derived from the discontinuous displacement.
    /// </summary>
    internal bool IsTeleporting { get; set; }

    /// <summary>
    /// Set when <see cref="IsSimulationEnabled"/> changes on a live body so the physics system
    /// can flush contact/sensor pair state and fire exit events on the next tick.
    /// </summary>
    internal bool IsSimulationEnabledDirty { get; set; }

    /// <summary>
    /// The primary collision shape for this body. Assign any <see cref="ShapeDefinition"/>
    /// subtype: <see cref="CircleShape"/>, <see cref="BoxShape"/>, <see cref="CapsuleShape"/>,
    /// <see cref="PolygonShape"/>, or <see cref="ChainShape"/>.
    /// Setting this marks the body dirty and triggers a rebuild on the next physics step.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when assigning a <see cref="ChainShape"/> while <see cref="IsTrigger"/>, <see cref="IsBullet"/>,
    /// or a non-<see cref="PhysicsBodyType.Static"/> <see cref="BodyType"/> is already set.
    /// </exception>
    public ShapeDefinition? Shape
    {
        get => _shape;
        set
        {
            if (value is ChainShape)
            {
                if (IsTrigger)
                    throw new InvalidOperationException(
                        "ChainShape does not support IsTrigger. Set IsTrigger = false before assigning a ChainShape.");
                if (IsBullet)
                    throw new InvalidOperationException(
                        "ChainShape does not support IsBullet. Set IsBullet = false before assigning a ChainShape.");
                if (BodyType != PhysicsBodyType.Static)
                    throw new InvalidOperationException(
                        $"ChainShape requires BodyType.Static (current: {BodyType}). Set BodyType = Static before assigning a ChainShape.");
            }

            if (value is SegmentShape && BodyType != PhysicsBodyType.Static)
                throw new InvalidOperationException(
                    $"SegmentShape requires BodyType.Static (current: {BodyType}). Set BodyType = Static before assigning a SegmentShape.");

            _shape = value;
            IsDirty = true;
        }
    }

    public int Layer
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(value, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 63);
            field = value;
            if (B2.BodyIsValid(BodyId))
                IsFilterDirty = true;
            else
                IsDirty = true;
        }
    }

    /// <summary>
    /// Raw category bitmask for this body's shapes. When non-zero, overrides the single-bit
    /// mask derived from <see cref="Layer"/> (i.e. <c>1UL &lt;&lt; Layer</c>), allowing a
    /// body to belong to multiple collision categories simultaneously.
    /// <para>
    /// Example: <c>CategoryBits = (1UL &lt;&lt; 0) | (1UL &lt;&lt; 3)</c> makes this body
    /// a member of both layer 0 and layer 3 for the purpose of collision filtering.
    /// </para>
    /// Set to <c>0</c> (default) to use the single-layer behaviour driven by <see cref="Layer"/>.
    /// </summary>
    public ulong CategoryBits
    {
        get;
        set
        {
            if (value == 0 && field != 0)
                Trace.TraceWarning(
                    $"[Brine2D] CategoryBits reset to 0 on entity '{Entity?.Name}' — this body will fall back to the single-layer category derived from Layer. " +
                    "If you intended to make this body invisible to all queries, set CollisionMask = 0 instead.");
            field = value;
            if (B2.BodyIsValid(BodyId))
                IsFilterDirty = true;
            else
                IsDirty = true;
        }
    }

    public ulong CollisionMask
    {
        get;
        set
        {
            if (value == 0)
                Trace.TraceWarning(
                    $"[Brine2D] CollisionMask = 0 on entity '{Entity?.Name}' — this body will not collide with anything.");
            field = value;
            if (B2.BodyIsValid(BodyId))
                IsFilterDirty = true;
            else
                IsDirty = true;
        }
    } = ulong.MaxValue;

    /// <summary>
    /// When <c>true</c>, this body acts as a sensor: it reports overlaps via
    /// <c>OnTriggerEnter</c> / <c>OnTriggerStay</c> / <c>OnTriggerExit</c> but generates
    /// no collision response forces.
    /// </summary>
    /// <remarks>
    /// Box2D only generates sensor events when a sensor shape overlaps a <em>non-sensor</em>
    /// shape. Two bodies that both have <c>IsTrigger = true</c> will not fire trigger events
    /// with each other.
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown when setting to <c>true</c> while <see cref="Shape"/> is a <see cref="ChainShape"/>.
    /// </exception>
    public bool IsTrigger
    {
        get ;
        set
        {
            if (value && _shape is ChainShape)
                throw new InvalidOperationException(
                    "ChainShape does not support IsTrigger. Assign a different Shape first.");
            field = value;
            IsDirty = true;
        }
    }

    /// <summary>
    /// How this body participates in physics simulation.
    /// Changes to a live body use a lightweight <c>B2.BodySetType</c> call — no full rebuild.
    /// Note that switching to <see cref="PhysicsBodyType.Dynamic"/> re-applies mass from shapes.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when changing away from <see cref="PhysicsBodyType.Static"/> while
    /// <see cref="Shape"/> is a <see cref="ChainShape"/>.
    /// </exception>
    public PhysicsBodyType BodyType
    {
        get;
        set
        {
            if (field == value) return;
            if (value != PhysicsBodyType.Static && _shape is ChainShape)
                throw new InvalidOperationException(
                    $"ChainShape requires BodyType.Static. Cannot change BodyType to {value}. Assign a different Shape first.");
            if (value != PhysicsBodyType.Static && _shape is SegmentShape)
                throw new InvalidOperationException(
                    $"SegmentShape requires BodyType.Static. Cannot change BodyType to {value}. Assign a different Shape first.");
            field = value;
            if (B2.BodyIsValid(BodyId))
                IsBodyTypeDirty = true;
            else
                IsDirty = true;
        }
    } = PhysicsBodyType.Dynamic;

    /// <summary>
    /// Bounciness of this body's primary shape (0–1).
    /// Changes on a live body apply immediately without a full rebuild.
    /// </summary>
    public float Restitution
    {
        get;
        set
        {
            field = Math.Clamp(value, 0f, 1f);
            if (B2.BodyIsValid(BodyId))
                IsMaterialDirty = true;
            else
                IsDirty = true;
        }
    }

    /// <summary>
    /// Mass of the body in simulation units. Must be greater than zero. Default is 1.
    /// </summary>
    /// <remarks>
    /// Changes to a live body use a lightweight density re-scale instead of a full rebuild.
    /// Has no effect on <see cref="PhysicsBodyType.Static"/> or <see cref="PhysicsBodyType.Kinematic"/> bodies.
    /// </remarks>
    public float Mass
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value, 0f);
            field = value;
            if (B2.BodyIsValid(BodyId))
                IsMassDirty = true;
            else
                IsDirty = true;
        }
    } = 1f;

    /// <summary>
    /// Surface friction of this body's primary shape (0–1).
    /// Changes on a live body apply immediately without a full rebuild.
    /// </summary>
    public float SurfaceFriction
    {
        get;
        set
        {
            field = Math.Clamp(value, 0f, 1f);
            if (B2.BodyIsValid(BodyId))
                IsMaterialDirty = true;
            else
                IsDirty = true;
        }
    }

    /// <exception cref="InvalidOperationException">
    /// Thrown when setting to <c>true</c> while <see cref="Shape"/> is a <see cref="ChainShape"/>.
    /// </exception>
    public bool IsBullet
    {
        get;
        set
        {
            if (value && _shape is ChainShape)
                throw new InvalidOperationException(
                    "ChainShape does not support IsBullet. Assign a different Shape first.");
            if (field == value) return;
            field = value;
            if (B2.BodyIsValid(BodyId))
                IsBulletDirty = true;
            else
                IsDirty = true;
        }
    }

    /// <summary>
    /// Set when <see cref="IsBullet"/> changes on a live body. The physics system performs a
    /// full body rebuild (bullet mode cannot be changed without recreating the body in Box2D),
    /// preserving the current velocity.
    /// </summary>
    internal bool IsBulletDirty { get; set; }

    /// <summary>
    /// Whether hit events (<see cref="OnCollisionHit"/>) are enabled for this body's primary shape.
    /// Defaults to <c>true</c>. Changes on a live body apply immediately without a full rebuild.
    /// </summary>
    public bool EnableHitEvents
    {
        get ;
        set
        {
            field = value;
            if (B2.BodyIsValid(BodyId))
                IsMaterialDirty = true;
            else
                IsDirty = true;
        }
    } = true;



    public bool FixedRotation
    {
        get;
        set
        {
            field = value;
            if (B2.BodyIsValid(BodyId))
            {
                AssertSimulationThread();
                B2.BodySetFixedRotation(BodyId, value);
            }
            else
                IsDirty = true;
        }
    }

    /// <summary>
    /// When <c>true</c>, the physics system zeroes the X component of this body's linear
    /// velocity every fixed-update step. Only affects <see cref="PhysicsBodyType.Dynamic"/> bodies.
    /// </summary>
    public bool FreezePositionX { get; set; }

    /// <summary>
    /// When <c>true</c>, the physics system zeroes the Y component of this body's linear
    /// velocity every fixed-update step. Only affects <see cref="PhysicsBodyType.Dynamic"/> bodies.
    /// </summary>
    public bool FreezePositionY { get; set; }

    /// <summary>
    /// When <c>true</c>, this body acts as a one-way platform: bodies approaching from the
    /// non-solid side (opposite to <see cref="PlatformNormalDirection"/>) pass through,
    /// while bodies approaching from the solid side collide normally.
    /// </summary>
    /// <remarks>
    /// The physics system installs a Box2D pre-solve callback when any body in the scene has
    /// this flag set, and removes it automatically when none do.
    /// </remarks>
    public bool IsOneWayPlatform
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            IsOneWayPlatformDirty = true;
        }
    }

    /// <summary>
    /// The outward normal of the solid surface in world space (does not need to be normalized).
    /// Default is <c>(0, -1)</c> — solid from above in Y-down screen space.
    /// Only used when <see cref="IsOneWayPlatform"/> is <c>true</c>.
    /// </summary>
    public Vector2 PlatformNormalDirection { get; set; } = new Vector2(0f, -1f);

    /// <summary>
    /// Applies a <see cref="Physics.PhysicsMaterial"/> preset, setting
    /// <see cref="SurfaceFriction"/> and <see cref="Restitution"/> in one assignment.
    /// </summary>
    public PhysicsMaterial? Material
    {
        set
        {
            if (value == null) return;
            SurfaceFriction = value.Friction;
            Restitution = value.Restitution;
        }
    }

    internal bool IsOneWayPlatformDirty { get; set; }

    /// <summary>
    /// Scales the world gravity applied to this body. Default is 1. Set to 0 to disable
    /// gravity on this body without changing the world gravity or providing a direction.
    /// Ignored when <see cref="GravityOverride"/> is set.
    /// </summary>
    public float GravityScale
    {
        get;
        set
        {
            field = value;
            if (B2.BodyIsValid(BodyId))
            {
                AssertSimulationThread();
                if (GravityOverride == null)
                    B2.BodySetGravityScale(BodyId, value);
            }
            else
                IsDirty = true;
        }
    } = 1f;

    /// <summary>
    /// Overrides the world gravity direction and magnitude for this body in pixels per second
    /// squared. When set, world gravity and <see cref="GravityScale"/> are both ignored for
    /// this body; the physics system applies the override as a manual force each fixed-update
    /// frame. Set to <c>null</c> to restore normal gravity.
    /// </summary>
    /// <remarks>
    /// Only affects <see cref="PhysicsBodyType.Dynamic"/> bodies. Kinematic and static bodies
    /// are unaffected by gravity regardless of this property.
    /// Common uses: ceiling-walkers, per-planet gravity, zero-g zones, directional gravity flips.
    /// </remarks>
    public Vector2? GravityOverride
    {
        get;
        set
        {
            Debug.WriteLineIf(
                value.HasValue && BodyType != PhysicsBodyType.Dynamic,
                $"[Brine2D] GravityOverride set on non-Dynamic body '{Entity?.Name}' — has no effect.");
            Debug.WriteLineIf(
                value.HasValue && value.Value == Vector2.Zero,
                $"[Brine2D] GravityOverride = Vector2.Zero on '{Entity?.Name}' means zero-gravity (gravity disabled), " +
                "not world gravity. Set GravityOverride = null to restore world gravity.");
            field = value;
            if (B2.BodyIsValid(BodyId))
                B2.BodySetGravityScale(BodyId, value.HasValue ? 0f : GravityScale);
            else
                IsDirty = true;
        }
    }

    /// <summary>
    /// Overrides the body-local center of mass offset in pixels.
    /// When set, the physics system applies this after computing mass from shapes.
    /// Set to <c>null</c> to use the geometry-derived center.
    /// Changes on a live body apply immediately.
    /// </summary>
    public Vector2? CenterOfMassOverride
    {
        get;
        set
        {
            field = value;
            if (B2.BodyIsValid(BodyId))
                IsMassDirty = true;
            else
                IsDirty = true;
        }
    }

    /// <summary>
    /// Overrides the rotational inertia of the body in simulation units.
    /// Must be greater than zero when set. Set to <c>null</c> to use the geometry-derived value.
    /// Changes on a live body apply immediately.
    /// Has no effect on <see cref="PhysicsBodyType.Static"/> or <see cref="PhysicsBodyType.Kinematic"/> bodies.
    /// </summary>
    public float? RotationalInertiaOverride
    {
        get;
        set
        {
            if (value.HasValue)
                ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(value.Value, 0f);
            field = value;
            if (B2.BodyIsValid(BodyId))
                IsMassDirty = true;
            else
                IsDirty = true;
        }
    }

    public float LinearDamping
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
            if (B2.BodyIsValid(BodyId))
            {
                AssertSimulationThread();
                B2.BodySetLinearDamping(BodyId, value);
            }
            else
                IsDirty = true;
        }
    }

    public float AngularDamping
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
            if (B2.BodyIsValid(BodyId))
            {
                AssertSimulationThread();
                B2.BodySetAngularDamping(BodyId, value);
            }
            else
                IsDirty = true;
        }
    }

    /// <summary>
    /// The sleep speed threshold for this body in simulation units per second. When the body's
    /// linear and angular speed drops below this value it becomes eligible for sleeping.
    /// Set to 0 to use the Box2D world default. Changes on a live body apply immediately.
    /// </summary>
    public float SleepThreshold
    {
        get;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
            if (B2.BodyIsValid(BodyId))
            {
                AssertSimulationThread();
                B2.BodySetSleepThreshold(BodyId, value > 0f ? value : B2.DefaultBodyDef().sleepThreshold);
            }
            else
                IsDirty = true;
        }
    }

    /// <summary>
    /// Translates the physics body's origin relative to the entity's
    /// <see cref="TransformComponent.Position"/> in pixels.
    /// </summary>
    /// <remarks>
    /// <b>Warning:</b> changing <c>Offset</c> on a live body triggers a full body rebuild
    /// (velocity is preserved, but all shape IDs change and sub-step kinematic state resets).
    /// Set <c>Offset</c> once at construction time. If you need to reposition the body at
    /// runtime, use <see cref="Teleport"/> instead.
    /// </remarks>
    public Vector2 Offset
    {
        get ;
        set { field = value; IsDirty = true; }
    } = Vector2.Zero;

    /// <summary>
    /// When <c>false</c>, the Box2D body is removed from the broad-phase: it stops moving,
    /// stops colliding, and does not appear in queries. All shapes, joints, and body data are
    /// preserved so simulation resumes correctly when set back to <c>true</c>.
    /// Defaults to <c>true</c>.
    /// </summary>
    /// <remarks>
    /// This is independent of <see cref="Entity.IsActive"/>. A component can be simulation-
    /// disabled while its entity remains active (e.g. a ghost power-up, a body waiting to
    /// spawn). Setting this to <c>false</c> flushes all active contact and sensor pairs,
    /// firing the appropriate exit events on the next physics tick.
    /// </remarks>
    public bool IsSimulationEnabled
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            if (B2.BodyIsValid(BodyId))
            {
                AssertSimulationThread();
                if (value)
                    B2.BodyEnable(BodyId);
                else
                    B2.BodyDisable(BodyId);
                IsSimulationEnabledDirty = true;
            }
            else
                IsDirty = true;
        }
    } = true;

    /// <summary>
    /// Linear velocity applied to the body when it is first created by the physics system.
    /// Has no effect once the body is live — use <see cref="LinearVelocity"/> instead.
    /// Has no meaningful effect on <see cref="PhysicsBodyType.Kinematic"/> bodies because
    /// the physics system derives velocity from position delta on every fixed-update frame.
    /// </summary>
    public Vector2 InitialLinearVelocity { get; set; }

    /// <summary>
    /// Angular velocity (radians/s) applied to the body when it is first created.
    /// Has no effect once the body is live — use <see cref="AngularVelocity"/> instead.
    /// Has no meaningful effect on <see cref="PhysicsBodyType.Kinematic"/> bodies because
    /// the physics system derives angular velocity from rotation delta on every fixed-update frame.
    /// </summary>
    public float InitialAngularVelocity { get; set; }

    internal IReadOnlyList<SubShape> SubShapes => _subShapes;

    public IReadOnlySet<Entity> CollidingEntities => _collidingEntities;

    /// <summary>
    /// Returns all bodies currently in active contact or sensor overlap with this body.
    /// This is the union of <see cref="ActiveContactPairs"/> and <see cref="ActiveSensorPairs"/>.
    /// </summary>
    public IEnumerable<PhysicsBodyComponent> CollidingBodies =>
        ActiveContactPairs.Concat<PhysicsBodyComponent>(ActiveSensorPairs);

    internal HashSet<PhysicsBodyComponent> ActiveContactPairs { get; } = [];

    internal HashSet<PhysicsBodyComponent> ActiveSensorPairs { get; } = [];

    /// <summary>Sub-shape pairs for active sensor overlaps, keyed by other body's index1.</summary>
    internal Dictionary<nint, (SubShape? Self, SubShape? Other)> ActiveSensorSubShapes { get; } = new();

    /// <summary>Sub-shape pairs for active contact pairs, keyed by other body's index1.</summary>
    internal Dictionary<nint, (SubShape? Self, SubShape? Other)> ActiveContactSubShapes { get; } = new();

    /// <summary>
    /// Fired once when another body begins touching this body.
    /// </summary>
    /// <remarks>
    /// In rare cases — typically when a fast-moving body makes and breaks contact within
    /// a single physics sub-step — <see cref="OnCollisionEnter"/> and
    /// <see cref="OnCollisionExit"/> may both fire on the same fixed-update tick with no
    /// intervening <see cref="OnCollisionStay"/>. This is expected Box2D behavior.
    /// </remarks>
    public event Action<PhysicsBodyComponent, CollisionContact>? OnCollisionEnter;

    public event Action<PhysicsBodyComponent, CollisionContact>? OnCollisionStay;

    /// <summary>
    /// Fired once when another body stops touching this body.
    /// </summary>
    /// <remarks>
    /// May fire on the same tick as <see cref="OnCollisionEnter"/> for contacts that are
    /// created and destroyed within a single physics sub-step. See <see cref="OnCollisionEnter"/>
    /// for details.
    /// </remarks>
    public event Action<PhysicsBodyComponent>? OnCollisionExit;

    /// <summary>
    /// Fired every fixed-update tick while this body remains in contact with another body,
    /// with sub-shape detail.
    /// </summary>
    /// <remarks>
    /// The sub-shape arguments reflect the sub-shapes that were present at the time the
    /// contact was <em>first established</em> (i.e. the enter tick), not necessarily the
    /// current sub-shapes. If sub-shapes are rebuilt at runtime (e.g. via a
    /// <see cref="Shape"/> reassignment or <see cref="AddSubShape"/> call) while a contact
    /// is already active, the reported sub-shape references may be stale until the contact
    /// ends and re-enters. Use <see cref="OnCollisionStay"/> for body-level events that are
    /// unaffected by sub-shape rebuilds.
    /// </remarks>
    public event Action<PhysicsBodyComponent, CollisionContact, SubShape?, SubShape?>? OnCollisionStayWithShape;

    /// <summary>
    /// Fired once when another body begins touching this body, with sub-shape detail.
    /// The first <see cref="SubShape"/> is the sub-shape on <em>this</em> body that was hit
    /// (<c>null</c> when the primary shape was hit). The second is the sub-shape on the
    /// <em>other</em> body (<c>null</c> when its primary shape was hit).
    /// </summary>
    public event Action<PhysicsBodyComponent, CollisionContact, SubShape?, SubShape?>? OnCollisionEnterWithShape;
    
    /// <summary>
    /// Fired once when another body stops touching this body, with sub-shape detail.
    /// Arguments mirror <see cref="OnCollisionEnterWithShape"/>.
    /// </summary>
    public event Action<PhysicsBodyComponent, SubShape?, SubShape?>? OnCollisionExitWithShape;

    /// <summary>
    /// Fired once when another body begins overlapping this trigger.
    /// </summary>
    /// <remarks>
    /// In rare cases — typically when a fast-moving body enters and exits the trigger within
    /// a single physics sub-step — <see cref="OnTriggerEnter"/> and <see cref="OnTriggerExit"/>
    /// may both fire on the same fixed-update tick with no intervening <see cref="OnTriggerStay"/>.
    /// This is expected Box2D behavior.
    /// </remarks>
    public event Action<PhysicsBodyComponent>? OnTriggerEnter;

    /// <summary>
    /// Fired once when another body begins overlapping this trigger, with sub-shape detail.
    /// The first <see cref="SubShape"/> is the sub-shape on <em>this</em> body that acted as
    /// the sensor (<c>null</c> when the primary shape was the sensor). The second is the
    /// sub-shape on the <em>visitor</em> body (<c>null</c> when its primary shape overlapped).
    /// </summary>
    public event Action<PhysicsBodyComponent, SubShape?, SubShape?>? OnTriggerEnterWithShape;

    /// <summary>
    /// Fired each fixed-update frame while a trigger overlap persists.
    /// </summary>
    public event Action<PhysicsBodyComponent>? OnTriggerStay;

    /// <summary>
    /// Fired each fixed-update frame while a trigger overlap persists, with sub-shape detail.
    /// Arguments mirror <see cref="OnTriggerEnterWithShape"/>.
    /// </summary>
    public event Action<PhysicsBodyComponent, SubShape?, SubShape?>? OnTriggerStayWithShape;

    /// <summary>
    /// Fired once when another body stops overlapping this trigger.
    /// </summary>
    /// <remarks>
    /// May fire on the same tick as <see cref="OnTriggerEnter"/> for overlaps that are
    /// created and destroyed within a single physics sub-step. See <see cref="OnTriggerEnter"/>
    /// for details.
    /// </remarks>
    public event Action<PhysicsBodyComponent>? OnTriggerExit;

    /// <summary>
    /// Fired once when another body stops overlapping this trigger, with sub-shape detail.
    /// Arguments mirror <see cref="OnTriggerEnterWithShape"/>: the first sub-shape is on
    /// <em>this</em> body, the second is on the <em>visitor</em> body.
    /// </summary>
    public event Action<PhysicsBodyComponent, SubShape?, SubShape?>? OnTriggerExitWithShape;

    /// <summary>
    /// Fired once when a high-speed contact impact is detected.
    /// <see cref="CollisionContact.ImpactSpeed"/> contains the closing speed.
    /// </summary>
    public event Action<PhysicsBodyComponent, CollisionContact>? OnCollisionHit;

    /// <summary>
    /// Per-body collision filter. When set, called for every candidate contact pair involving
    /// this body — return <c>false</c> to prevent the pair from colliding or triggering.
    /// Both bodies in the pair are checked; either can veto the contact.
    /// The callback is invoked from the Box2D broad-phase on the simulation thread — keep it
    /// allocation-free.
    /// </summary>
    public Func<PhysicsBodyComponent, bool>? ShouldCollide
    {
        get;
        set
        {
            bool wasSet = field != null;
            field = value;
            bool isSet = field != null;
            if (wasSet != isSet)
                ShouldCollideChanged?.Invoke(isSet);
        }
    }

    internal void NotifyBodySleep() => OnBodySleep?.Invoke(this);

    internal void NotifyBodyWake() => OnBodyWake?.Invoke(this);

    /// <summary>Fired once when this body falls asleep.</summary>
    public event Action<PhysicsBodyComponent>? OnBodySleep;

    /// <summary>Fired once when this body wakes from sleep.</summary>
    public event Action<PhysicsBodyComponent>? OnBodyWake;

    internal void AssertSimulationThread()
    {
        var simId = World?.SimulationThreadId ?? 0;
        if (simId != 0 && Environment.CurrentManagedThreadId != simId)
        {
#if DEBUG
            throw new InvalidOperationException(
                "[Brine2D] PhysicsBodyComponent live-body mutation must be called from the simulation thread " +
                "(inside FixedUpdate). Calling from another thread causes undefined behavior in Box2D.");
#else
            Trace.TraceWarning(
                "[Brine2D] PhysicsBodyComponent live-body mutation called from outside the simulation thread " +
                "(thread " + Environment.CurrentManagedThreadId + " != expected " + simId + "). " +
                "This causes undefined behavior in Box2D.");
#endif
        }
    }

    /// <summary>
    /// Raised by the physics system when the <see cref="ShouldCollide"/> delegate (or any
    /// sub-shape <see cref="SubShape.ShouldCollide"/> delegate) transitions between null and
    /// non-null. The system uses this to install or remove the Box2D custom filter callback.
    /// </summary>
    internal event Action<bool>? ShouldCollideChanged;

    /// <summary>
    /// Adds an additional collision shape to this body. Chain shapes are not supported as sub-shapes.
    /// </summary>
    /// <param name="definition">Any <see cref="ShapeDefinition"/> except <see cref="ChainShape"/>.</param>
    /// <param name="isTrigger">When <c>true</c>, the sub-shape acts as a sensor.</param>
    /// <param name="friction">Surface friction override (0–1). Pass <c>null</c> to inherit from the body.</param>
    /// <param name="restitution">Restitution override (0–1). Pass <c>null</c> to inherit from the body.</param>
    public SubShape AddSubShape(ShapeDefinition definition, bool isTrigger = false,
        float? friction = null, float? restitution = null)
    {
        if (definition is ChainShape)
            throw new ArgumentException("Chain shapes are not supported as sub-shapes.", nameof(definition));

        if (definition is SegmentShape && BodyType != PhysicsBodyType.Static)
            throw new ArgumentException(
                "SegmentShape is one-sided static geometry and cannot be used as a sub-shape on a non-static body. " +
                "Use a CapsuleShape or BoxShape for dynamic/kinematic compound bodies.",
                nameof(definition));

        var sub = new SubShape(definition, isTrigger,
            friction.HasValue ? Math.Clamp(friction.Value, 0f, 1f) : null,
            restitution.HasValue ? Math.Clamp(restitution.Value, 0f, 1f) : null);

        sub.MarkOwnerDirty = () => { IsDirty = true; };
        sub.MarkOwnerFilterDirty = () =>
        {
            if (B2.BodyIsValid(BodyId))
                IsFilterDirty = true;
            else
                IsDirty = true;
        };
        sub.MarkOwnerMaterialDirty = () =>
        {
            if (B2.BodyIsValid(BodyId))
                IsMaterialDirty = true;
            else
                IsDirty = true;
        };
        sub.MarkOwnerShouldCollideChanged = isSet => ShouldCollideChanged?.Invoke(isSet);

        sub.MarkOwnerTriggerDirty = () =>
        {
            if (B2.BodyIsValid(BodyId))
                IsSubShapeTriggerDirty = true;
            else
                IsDirty = true;
        };

        sub.MarkOwnerGeometryDirty = () =>
        {
            if (B2.BodyIsValid(BodyId))
                IsSubShapeGeometryDirty = true;
            else
                IsDirty = true;
        };

        _subShapes.Add(sub);
        IsDirty = true;
        return sub;
    }

    public bool RemoveSubShape(SubShape sub)
    {
        if (!_subShapes.Remove(sub)) return false;
        if (sub.ShouldCollide != null)
            ShouldCollideChanged?.Invoke(false);
        sub.MarkOwnerDirty = null;
        sub.MarkOwnerFilterDirty = null;
        sub.MarkOwnerMaterialDirty = null;
        sub.MarkOwnerTriggerDirty = null;
        sub.MarkOwnerShouldCollideChanged = null;
        sub.MarkOwnerGeometryDirty = null;
        IsDirty = true;
        return true;
    }

    public void ClearSubShapes()
    {
        foreach (var sub in _subShapes)
        {
            if (sub.ShouldCollide != null)
                ShouldCollideChanged?.Invoke(false);
            sub.MarkOwnerDirty = null;
            sub.MarkOwnerFilterDirty = null;
            sub.MarkOwnerMaterialDirty = null;
            sub.MarkOwnerTriggerDirty = null;
            sub.MarkOwnerShouldCollideChanged = null;
            sub.MarkOwnerGeometryDirty = null;
        }
        _subShapes.Clear();
        IsDirty = true;
    }

    protected internal override void OnRemoved()
    {
        if (B2.BodyIsValid(BodyId))
        {
            OnBodyDestroyed?.Invoke(BodyId.index1);

            if (B2.WorldIsValid(B2.BodyGetWorld(BodyId)))
                B2.DestroyBody(BodyId);
        }

        BodyId = default;
        ShapeId = default;
        ChainId = default;

        foreach (var sub in _subShapes)
        {
            sub.ShapeId = default;
            sub.MarkOwnerDirty = null;
            sub.MarkOwnerFilterDirty = null;
            sub.MarkOwnerMaterialDirty = null;
            sub.MarkOwnerTriggerDirty = null;
            sub.MarkOwnerShouldCollideChanged = null;
            sub.MarkOwnerGeometryDirty = null;
        }

        _shape = null;
        _subShapes.Clear();

        BodyType = PhysicsBodyType.Dynamic;
        Layer = 0;
        CategoryBits = 0;
        CollisionMask = ulong.MaxValue;
        GroupIndex = 0;
        IsTrigger = false;
        IsBullet = false;
        EnableHitEvents = true;
        FixedRotation = false;
        FreezePositionX = false;
        FreezePositionY = false;
        IsOneWayPlatform = false;
        PlatformNormalDirection = new Vector2(0f, -1f);
        Mass = 1f;
        SurfaceFriction = 0f;
        Restitution = 0f;
        LinearDamping = 0f;
        AngularDamping = 0f;
        GravityScale = 1f;
        Offset = Vector2.Zero;
        IsSimulationEnabled = true;

        IsDirty = true;
        IsFilterDirty = false;
        IsBodyTypeDirty = false;
        IsMassDirty = false;
        IsMaterialDirty = false;
        IsSubShapeTriggerDirty = false;
        IsSubShapeGeometryDirty = false;
        IsBulletDirty = false;
        IsTeleporting = false;
        IsSimulationEnabledDirty = false;
        IsOneWayPlatformDirty = false;

        InitialLinearVelocity = Vector2.Zero;
        InitialAngularVelocity = 0f;
        SleepThreshold = 0;
        GravityOverride = null;
        CenterOfMassOverride = null;
        RotationalInertiaOverride = null;
        _collidingEntities.Clear();
        ActiveContactPairs.Clear();
        ActiveSensorPairs.Clear();
        ActiveSensorSubShapes.Clear();
        ActiveContactSubShapes.Clear();
        OnBodyDestroyed = null;
        ShouldCollide = null;
        OnBodySleep = null;
        OnBodyWake = null;
        OnCollisionEnter = null;
        OnCollisionStay = null;
        OnCollisionExit = null;
        OnCollisionHit = null;
        OnTriggerEnter = null;
        OnTriggerStay = null;
        OnTriggerExit = null;
        OnCollisionEnterWithShape = null;
        OnCollisionStayWithShape = null;
        OnCollisionExitWithShape = null;
        OnTriggerEnterWithShape = null;
        OnTriggerStayWithShape = null;
        OnTriggerExitWithShape = null;
        ShouldCollideChanged = null;
    }

    public void ApplyForce(Vector2 force)
    {
        if (!B2.BodyIsValid(BodyId)) return;
        Debug.WriteLineIf(!IsSimulationEnabled,
            $"[Brine2D] ApplyForce called on simulation-disabled body '{Entity?.Name}' — has no effect.");
        Debug.WriteLineIf(BodyType != PhysicsBodyType.Dynamic,
            $"[Brine2D] ApplyForce called on {BodyType} body '{Entity?.Name}' — forces only affect Dynamic bodies.");
        AssertSimulationThread();
        B2.BodyApplyForceToCenter(BodyId, new B2.Vec2 { x = force.X, y = force.Y }, true);
    }

    public void ApplyForce(Vector2 force, Vector2 worldPoint)
    {
        if (!B2.BodyIsValid(BodyId)) return;
        Debug.WriteLineIf(!IsSimulationEnabled,
            $"[Brine2D] ApplyForce called on simulation-disabled body '{Entity?.Name}' — has no effect.");
        Debug.WriteLineIf(BodyType != PhysicsBodyType.Dynamic,
            $"[Brine2D] ApplyForce called on {BodyType} body '{Entity?.Name}' — forces only affect Dynamic bodies.");
        AssertSimulationThread();
        B2.BodyApplyForce(BodyId,
            new B2.Vec2 { x = force.X, y = force.Y },
            new B2.Vec2 { x = worldPoint.X, y = worldPoint.Y },
            true);
    }

    public void ApplyLinearImpulse(Vector2 impulse)
    {
        if (!B2.BodyIsValid(BodyId)) return;
        Debug.WriteLineIf(!IsSimulationEnabled,
            $"[Brine2D] ApplyLinearImpulse called on simulation-disabled body '{Entity?.Name}' — has no effect.");
        Debug.WriteLineIf(BodyType != PhysicsBodyType.Dynamic,
            $"[Brine2D] ApplyLinearImpulse called on {BodyType} body '{Entity?.Name}' — impulses only affect Dynamic bodies.");
        AssertSimulationThread();
        B2.BodyApplyLinearImpulseToCenter(BodyId, new B2.Vec2 { x = impulse.X, y = impulse.Y }, true);
    }

    public void ApplyLinearImpulse(Vector2 impulse, Vector2 worldPoint)
    {
        if (!B2.BodyIsValid(BodyId)) return;
        Debug.WriteLineIf(!IsSimulationEnabled,
            $"[Brine2D] ApplyLinearImpulse called on simulation-disabled body '{Entity?.Name}' — has no effect.");
        Debug.WriteLineIf(BodyType != PhysicsBodyType.Dynamic,
            $"[Brine2D] ApplyLinearImpulse called on {BodyType} body '{Entity?.Name}' — impulses only affect Dynamic bodies.");
        AssertSimulationThread();
        B2.BodyApplyLinearImpulse(BodyId,
            new B2.Vec2 { x = impulse.X, y = impulse.Y },
            new B2.Vec2 { x = worldPoint.X, y = worldPoint.Y },
            true);
    }

    public void ApplyAngularImpulse(float impulse)
    {
        if (!B2.BodyIsValid(BodyId)) return;
        Debug.WriteLineIf(!IsSimulationEnabled,
            $"[Brine2D] ApplyAngularImpulse called on simulation-disabled body '{Entity?.Name}' — has no effect.");
        Debug.WriteLineIf(BodyType != PhysicsBodyType.Dynamic,
            $"[Brine2D] ApplyAngularImpulse called on {BodyType} body '{Entity?.Name}' — impulses only affect Dynamic bodies.");
        AssertSimulationThread();
        B2.BodyApplyAngularImpulse(BodyId, impulse, true);
    }

    public void ApplyTorque(float torque)
    {
        if (!B2.BodyIsValid(BodyId)) return;
        Debug.WriteLineIf(!IsSimulationEnabled,
            $"[Brine2D] ApplyTorque called on simulation-disabled body '{Entity?.Name}' — has no effect.");
        Debug.WriteLineIf(BodyType != PhysicsBodyType.Dynamic,
            $"[Brine2D] ApplyTorque called on {BodyType} body '{Entity?.Name}' — torque only affects Dynamic bodies.");
        AssertSimulationThread();
        B2.BodyApplyTorque(BodyId, torque, true);
    }

    /// <summary>
    /// Gets or sets the linear velocity of this body in pixels per second.
    /// </summary>
    /// <remarks>
    /// For <see cref="PhysicsBodyType.Kinematic"/> bodies, the setter has no persistent effect.
    /// The physics system derives and overwrites kinematic velocity every fixed-update frame
    /// from the body's position displacement.
    /// </remarks>
    public Vector2 LinearVelocity
    {
        get
        {
            if (!B2.BodyIsValid(BodyId)) return Vector2.Zero;
            var v = B2.BodyGetLinearVelocity(BodyId);
            return new Vector2(v.x, v.y);
        }
        set
        {
            if (!B2.BodyIsValid(BodyId)) return;
            AssertSimulationThread();
            if (BodyType == PhysicsBodyType.Kinematic)
                Trace.TraceWarning(
                    $"[Brine2D] LinearVelocity set directly on Kinematic body '{Entity?.Name}'. " +
                    "The physics system overwrites kinematic velocity every fixed-update frame from position " +
                    "displacement; this value will not persist.");
            B2.BodySetLinearVelocity(BodyId, new B2.Vec2 { x = value.X, y = value.Y });
        }
    }

    public float AngularVelocity
    {
        get => B2.BodyIsValid(BodyId) ? B2.BodyGetAngularVelocity(BodyId) : 0f;
        set
        {
            if (!B2.BodyIsValid(BodyId)) return;
            AssertSimulationThread();
            B2.BodySetAngularVelocity(BodyId, value);
        }
    }

    /// <summary>
    /// Returns the velocity of this body at a given world-space point, accounting for both
    /// linear and angular velocity. Useful for computing impact speed at a contact point.
    /// Returns <see cref="Vector2.Zero"/> if the body is not live.
    /// </summary>
    /// <param name="worldPoint">The point in world (pixel) coordinates.</param>
    public Vector2 GetVelocityAtPoint(Vector2 worldPoint)
    {
        if (!B2.BodyIsValid(BodyId)) return Vector2.Zero;
        var lv = B2.BodyGetLinearVelocity(BodyId);
        var av = B2.BodyGetAngularVelocity(BodyId);
        var xf = B2.BodyGetTransform(BodyId);
        float rx = worldPoint.X - xf.p.x;
        float ry = worldPoint.Y - xf.p.y;
        // v = linearVelocity + angularVelocity × r  (2D cross: ω × r = (-ω*ry, ω*rx))
        return new Vector2(lv.x - av * ry, lv.y + av * rx);
    }

    public bool IsAwake => B2.BodyIsValid(BodyId) && B2.BodyIsAwake(BodyId);

    /// <summary>
    /// Reads the live contact manifolds for this body directly from Box2D, written into
    /// <paramref name="results"/>. Returns the number of contacts written.
    /// Returns 0 if the body is not live, has no active contacts, or the component has not
    /// been registered with a <see cref="PhysicsWorld"/>.
    /// </summary>
    /// <remarks>
    /// Contact normals are oriented away from the other body toward this body.
    /// Delegates to <see cref="PhysicsWorld.GetContacts(PhysicsBodyComponent,Span{ContactPair},out bool)"/>.
    /// </remarks>
    public int GetContacts(Span<ContactPair> results, out bool wasTruncated)
        => World?.GetContacts(this, results, out wasTruncated) ?? (wasTruncated = false, 0).Item2;

    /// <inheritdoc cref="GetContacts(Span{ContactPair},out bool)"/>
    public int GetContacts(Span<ContactPair> results)
        => World?.GetContacts(this, results) ?? 0;

    /// <summary>
    /// Reads all live contact manifolds for this body into <paramref name="results"/>,
    /// retrying internally with a larger buffer until every contact is captured.
    /// Clears <paramref name="results"/> before writing.
    /// Delegates to <see cref="PhysicsWorld.GetContactsAll"/>.
    /// </summary>
    public void GetContactsAll(List<ContactPair> results)
    {
        if (World != null)
            World.GetContactsAll(this, results);
        else
            results.Clear();
    }

    /// <summary>
    /// Returns the physics-side world-space position of this body (body pivot, not entity origin).
    /// Includes the <see cref="Offset"/> — equivalent to <c>transform.Position + Offset</c> when
    /// the body is live. Returns <see cref="Vector2.Zero"/> if the body has not yet been created.
    /// </summary>
    public Vector2 GetWorldPosition()
    {
        if (!B2.BodyIsValid(BodyId)) return Vector2.Zero;
        var p = B2.BodyGetPosition(BodyId);
        return new Vector2(p.x, p.y);
    }

    /// <summary>
    /// Returns the physics-side world-space rotation of this body in radians.
    /// Returns 0 if the body has not yet been created.
    /// </summary>
    public float GetWorldRotation()
    {
        if (!B2.BodyIsValid(BodyId)) return 0f;
        var q = B2.BodyGetRotation(BodyId);
        return MathF.Atan2(q.s, q.c);
    }

    public void SetAwake(bool awake)
    {
        if (!B2.BodyIsValid(BodyId)) return;
        AssertSimulationThread();
        B2.BodySetAwake(BodyId, awake);
    }

    /// <summary>
    /// Instantly moves this body to <paramref name="position"/> (and optionally sets its
    /// rotation) without triggering a full body rebuild. Linear and angular velocity are
    /// preserved. For kinematic bodies the previous-position and previous-rotation records are
    /// also reset, preventing a phantom velocity spike on the next fixed update.
    /// </summary>
    /// <remarks>
    /// The <see cref="TransformComponent"/> is always updated, even before the physics system
    /// has created the body, so the body spawns at the correct position on first tick.
    /// </remarks>
    public void Teleport(Vector2 position, float? rotation = null)
    {
        var transform = Entity?.GetComponent<TransformComponent>();
        if (transform != null)
        {
            transform.Position = position - Offset;
            if (rotation.HasValue)
                transform.Rotation = rotation.Value;
        }

        if (!B2.BodyIsValid(BodyId)) return;

        var rot = rotation.HasValue ? B2.MakeRot(rotation.Value) : B2.BodyGetRotation(BodyId);
        B2.BodySetTransform(BodyId, new B2.Vec2 { x = position.X, y = position.Y }, rot);
        B2.BodySetAwake(BodyId, true);
        IsTeleporting = true;
    }

    public PhysicsMassData GetMassData()
    {
        if (!B2.BodyIsValid(BodyId))
            return default;

        var data = B2.BodyGetMassData(BodyId);
        var xf = B2.BodyGetTransform(BodyId);

        float cos = xf.q.c;
        float sin = xf.q.s;
        float lx = data.center.x;
        float ly = data.center.y;

        return new PhysicsMassData
        {
            Mass = data.mass,
            Inertia = data.rotationalInertia,
            WorldCenterOfMass = new Vector2(
                xf.p.x + lx * cos - ly * sin,
                xf.p.y + lx * sin + ly * cos)
        };
    }

    /// <summary>
    /// Computes the world-space axis-aligned bounding box that encloses all shapes on this body.
    /// Returns <c>false</c> if the body has not yet been created by the physics system.
    /// </summary>
    public unsafe bool TryGetAABB(out Vector2 min, out Vector2 max)
    {
        if (!B2.BodyIsValid(BodyId))
        {
            min = max = Vector2.Zero;
            return false;
        }

        int shapeCount = B2.BodyGetShapeCount(BodyId);
        if (shapeCount == 0)
        {
            min = max = Vector2.Zero;
            return false;
        }

        var rented = ArrayPool<B2.ShapeId>.Shared.Rent(shapeCount);
        try
        {
            fixed (B2.ShapeId* ptr = rented)
                B2.BodyGetShapes(BodyId, ptr, shapeCount);

            var first = B2.ShapeGetAABB(rented[0]);
            float minX = first.lowerBound.x, minY = first.lowerBound.y;
            float maxX = first.upperBound.x, maxY = first.upperBound.y;

            for (int i = 1; i < shapeCount; i++)
            {
                var aabb = B2.ShapeGetAABB(rented[i]);
                if (aabb.lowerBound.x < minX) minX = aabb.lowerBound.x;
                if (aabb.lowerBound.y < minY) minY = aabb.lowerBound.y;
                if (aabb.upperBound.x > maxX) maxX = aabb.upperBound.x;
                if (aabb.upperBound.y > maxY) maxY = aabb.upperBound.y;
            }

            min = new Vector2(minX, minY);
            max = new Vector2(maxX, maxY);
            return true;
        }
        finally
        {
            ArrayPool<B2.ShapeId>.Shared.Return(rented);
        }
    }

    internal Action<nint>? OnBodyDestroyed { get; set; }

    internal void NotifyCollisionEnter(PhysicsBodyComponent other, CollisionContact contact,
        SubShape? selfSubShape, SubShape? otherSubShape)
    {
        if (other.Entity == null) return;
        _collidingEntities.Add(other.Entity);
        ActiveContactPairs.Add(other);
        ActiveContactSubShapes[other.BodyId.index1] = (selfSubShape, otherSubShape);
        OnCollisionEnter?.Invoke(other, contact);
        OnCollisionEnterWithShape?.Invoke(other, contact, selfSubShape, otherSubShape);
    }

    internal void NotifyTriggerEnter(PhysicsBodyComponent other, SubShape? selfSubShape, SubShape? otherSubShape)
    {
        if (other.Entity == null) return;
        _collidingEntities.Add(other.Entity);
        ActiveSensorPairs.Add(other);
        ActiveSensorSubShapes[other.BodyId.index1] = (selfSubShape, otherSubShape);
        OnTriggerEnter?.Invoke(other);
        OnTriggerEnterWithShape?.Invoke(other, selfSubShape, otherSubShape);
    }

    internal void NotifyCollisionStay(PhysicsBodyComponent other, CollisionContact contact,
        SubShape? selfSubShape, SubShape? otherSubShape)
    {
        if (other.Entity == null) return;
        OnCollisionStay?.Invoke(other, contact);
        if (OnCollisionStayWithShape != null)
        {
            if (!ActiveContactSubShapes.TryGetValue(other.BodyId.index1, out var pair))
                pair = (selfSubShape, otherSubShape);
            OnCollisionStayWithShape.Invoke(other, contact, pair.Self, pair.Other);
        }
    }

    internal void NotifyTriggerStay(PhysicsBodyComponent other)
    {
        if (other.Entity == null) return;
        OnTriggerStay?.Invoke(other);

        if (OnTriggerStayWithShape != null)
        {
            ActiveSensorSubShapes.TryGetValue(other.BodyId.index1, out var pair);
            OnTriggerStayWithShape.Invoke(other, pair.Self, pair.Other);
        }
    }

    internal void NotifyCollisionHit(PhysicsBodyComponent other, CollisionContact contact)
    {
        if (other.Entity == null) return;
        OnCollisionHit?.Invoke(other, contact);
    }

    internal void NotifyCollisionExit(PhysicsBodyComponent other)
    {
        if (other.Entity == null) return;
        ActiveContactPairs.Remove(other);
        ActiveContactSubShapes.TryGetValue(other.BodyId.index1, out var pair);
        ActiveContactSubShapes.Remove(other.BodyId.index1);
        if (!IsEntityStillTracked(other.Entity))
            _collidingEntities.Remove(other.Entity);
        OnCollisionExit?.Invoke(other);
        OnCollisionExitWithShape?.Invoke(other, pair.Self, pair.Other);
    }

    internal void NotifyTriggerExit(PhysicsBodyComponent other)
    {
        if (other.Entity == null) return;
        ActiveSensorPairs.Remove(other);
        ActiveSensorSubShapes.TryGetValue(other.BodyId.index1, out var pair);
        ActiveSensorSubShapes.Remove(other.BodyId.index1);
        if (!IsEntityStillTracked(other.Entity))
            _collidingEntities.Remove(other.Entity);
        OnTriggerExit?.Invoke(other);
        OnTriggerExitWithShape?.Invoke(other, pair.Self, pair.Other);
    }

    private bool IsEntityStillTracked(Entity entity)
    {
        foreach (var body in ActiveContactPairs)
            if (body.Entity == entity) return true;
        foreach (var body in ActiveSensorPairs)
            if (body.Entity == entity) return true;
        return false;
    }

    internal void RaiseCollisionExit(PhysicsBodyComponent other, nint otherBodyIndex)
    {
        if (other.Entity == null) return;
        ActiveContactSubShapes.TryGetValue(otherBodyIndex, out var pair);
        ActiveContactSubShapes.Remove(otherBodyIndex);
        OnCollisionExit?.Invoke(other);
        OnCollisionExitWithShape?.Invoke(other, pair.Self, pair.Other);
    }

    internal void RaiseTriggerExit(PhysicsBodyComponent other, nint otherBodyIndex)
    {
        if (other.Entity == null) return;
        ActiveSensorSubShapes.TryGetValue(otherBodyIndex, out var pair);
        ActiveSensorSubShapes.Remove(otherBodyIndex);
        OnTriggerExit?.Invoke(other);
        OnTriggerExitWithShape?.Invoke(other, pair.Self, pair.Other);
    }

    /// <summary>
    /// Set when <see cref="SubShape.UpdateDefinition"/> is called with a same-type shape on a live
    /// body. The system calls <c>B2.ShapeSet*</c> instead of a full rebuild, preserving contacts,
    /// velocity, and sleeping state. A type-changing update marks <see cref="IsDirty"/> instead.
    /// </summary>
    internal bool IsSubShapeGeometryDirty { get; set; }

    internal HashSet<Entity> CollidingEntitiesInternal => _collidingEntities;

    /// <summary>
    /// Applies a force at a local-space point, generating both linear and angular acceleration.
    /// <paramref name="localPoint"/> is in the body's local coordinate frame.
    /// </summary>
    public void ApplyForceAtLocalPoint(Vector2 force, Vector2 localPoint)
    {
        if (!B2.BodyIsValid(BodyId)) return;
        AssertSimulationThread();
        var xf = B2.BodyGetTransform(BodyId);
        var worldPoint = new Vector2(
            xf.p.x + localPoint.X * xf.q.c - localPoint.Y * xf.q.s,
            xf.p.y + localPoint.X * xf.q.s + localPoint.Y * xf.q.c);
        B2.BodyApplyForce(BodyId,
            new B2.Vec2 { x = force.X, y = force.Y },
            new B2.Vec2 { x = worldPoint.X, y = worldPoint.Y },
            true);
    }

    /// <summary>
    /// Applies a linear impulse at a local-space point, generating both linear and angular
    /// velocity changes. <paramref name="localPoint"/> is in the body's local coordinate frame.
    /// </summary>
    public void ApplyLinearImpulseAtLocalPoint(Vector2 impulse, Vector2 localPoint)
    {
        if (!B2.BodyIsValid(BodyId)) return;
        AssertSimulationThread();
        var xf = B2.BodyGetTransform(BodyId);
        var worldPoint = new Vector2(
            xf.p.x + localPoint.X * xf.q.c - localPoint.Y * xf.q.s,
            xf.p.y + localPoint.X * xf.q.s + localPoint.Y * xf.q.c);
        B2.BodyApplyLinearImpulse(BodyId,
            new B2.Vec2 { x = impulse.X, y = impulse.Y },
            new B2.Vec2 { x = worldPoint.X, y = worldPoint.Y },
            true);
    }

    /// <summary>
    /// Box2D collision group index. Positive values cause members of the same group to
    /// <em>always</em> collide with each other regardless of category/mask bits.
    /// Negative values cause members of the same group to <em>never</em> collide with
    /// each other. Zero (default) disables group-index logic and falls back to
    /// category/mask filtering.
    /// </summary>
    public int GroupIndex
    {
        get => field;
        set
        {
            if (field == value) return;
            field = value;
            if (B2.BodyIsValid(BodyId))
                IsFilterDirty = true;
            else
                IsDirty = true;
        }
    }

    /// <summary>
    /// Converts a point from local body space to world space using the body's current
    /// live transform. Returns the point unchanged if the body has not yet been created.
    /// </summary>
    public Vector2 TransformPoint(Vector2 localPoint)
    {
        if (!B2.BodyIsValid(BodyId))
            return localPoint;

        var xf = B2.BodyGetTransform(BodyId);
        float cos = xf.q.c;
        float sin = xf.q.s;
        return new Vector2(
            cos * localPoint.X - sin * localPoint.Y + xf.p.x,
            sin * localPoint.X + cos * localPoint.Y + xf.p.y);
    }

    /// <summary>
    /// Converts a point from world space to local body space using the body's current
    /// live transform. Returns the point unchanged if the body has not yet been created.
    /// </summary>
    public Vector2 InverseTransformPoint(Vector2 worldPoint)
    {
        if (!B2.BodyIsValid(BodyId))
            return worldPoint;

        var xf = B2.BodyGetTransform(BodyId);
        float cos = xf.q.c;
        float sin = xf.q.s;
        float dx = worldPoint.X - xf.p.x;
        float dy = worldPoint.Y - xf.p.y;
        return new Vector2(
             cos * dx + sin * dy,
            -sin * dx + cos * dy);
    }

    /// <summary>
    /// Converts a direction vector from local body space to world space (rotation only,
    /// no translation). Returns the vector unchanged if the body has not yet been created.
    /// </summary>
    public Vector2 TransformDirection(Vector2 localDirection)
    {
        if (!B2.BodyIsValid(BodyId))
            return localDirection;

        var xf = B2.BodyGetTransform(BodyId);
        float cos = xf.q.c;
        float sin = xf.q.s;
        return new Vector2(
            cos * localDirection.X - sin * localDirection.Y,
            sin * localDirection.X + cos * localDirection.Y);
    }

    /// <summary>
    /// Converts a direction vector from world space to local body space (rotation only,
    /// no translation). Returns the vector unchanged if the body has not yet been created.
    /// </summary>
    public Vector2 InverseTransformDirection(Vector2 worldDirection)
    {
        if (!B2.BodyIsValid(BodyId))
            return worldDirection;

        var xf = B2.BodyGetTransform(BodyId);
        float cos = xf.q.c;
        float sin = xf.q.s;
        return new Vector2(
             cos * worldDirection.X + sin * worldDirection.Y,
            -sin * worldDirection.X + cos * worldDirection.Y);
    }
}