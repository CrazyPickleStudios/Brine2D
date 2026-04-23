using System.Buffers;
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

    public ulong CollisionMask
    {
        get ;
        set
        {
            field = value;
            if (B2.BodyIsValid(BodyId))
                IsFilterDirty = true;
            else
                IsDirty = true;
        }
    } = ulong.MaxValue;

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
        get ;
        set
        {
            if (field == value) return;
            if (value != PhysicsBodyType.Static && _shape is ChainShape)
                throw new InvalidOperationException(
                    $"ChainShape requires BodyType.Static. Cannot change BodyType to {value}. Assign a different Shape first.");
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
        get ;
        set
        {
            if (value && _shape is ChainShape)
                throw new InvalidOperationException(
                    "ChainShape does not support IsBullet. Assign a different Shape first.");
            field = value;
            IsDirty = true;
        }
    }

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
        get ;
        set
        {
            field = value;
            if (B2.BodyIsValid(BodyId))
                B2.BodySetFixedRotation(BodyId, value);
            else
                IsDirty = true;
        }
    }

    /// <summary>
    /// Scales the world gravity applied to this body. Default is 1. Set to 0 to disable
    /// gravity on this body without changing the world gravity or providing a direction.
    /// Ignored when <see cref="GravityOverride"/> is set.
    /// </summary>
    public float GravityScale
    {
        get ;
        set
        {
            field = value;
            if (B2.BodyIsValid(BodyId))
            {
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
        get ;
        set
        {
            field = value;
            if (B2.BodyIsValid(BodyId))
                B2.BodySetGravityScale(BodyId, value.HasValue ? 0f : GravityScale);
            else
                IsDirty = true;
        }
    }

    public float LinearDamping
    {
        get ;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
            if (B2.BodyIsValid(BodyId))
                B2.BodySetLinearDamping(BodyId, value);
            else
                IsDirty = true;
        }
    }

    public float AngularDamping
    {
        get ;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            field = value;
            if (B2.BodyIsValid(BodyId))
                B2.BodySetAngularDamping(BodyId, value);
            else
                IsDirty = true;
        }
    }

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

    internal HashSet<PhysicsBodyComponent> ActiveContactPairs { get; } = [];

    internal HashSet<PhysicsBodyComponent> ActiveSensorPairs { get; } = [];

    /// <summary>
    /// Tracks the sub-shapes involved in each active sensor pair so that
    /// <see cref="OnTriggerStayWithShape"/> and <see cref="OnTriggerExitWithShape"/> receive
    /// the same sub-shape detail as <see cref="OnTriggerEnterWithShape"/>.
    /// Key: other body's <c>BodyId.index1</c>.
    /// Value: (selfSubShape, otherSubShape) — either may be null for the primary shape.
    /// </summary>
    internal Dictionary<nint, (SubShape? Self, SubShape? Other)> ActiveSensorSubShapes { get; } = new();

    /// <summary>
    /// Tracks the sub-shapes involved in each active contact pair so that
    /// <see cref="OnCollisionStayWithShape"/> and <see cref="OnCollisionExitWithShape"/> receive
    /// the same sub-shape detail as <see cref="OnCollisionEnterWithShape"/>.
    /// Key: other body's <c>BodyId.index1</c>.
    /// Value: (selfSubShape, otherSubShape) — either may be null for the primary shape.
    /// </summary>
    internal Dictionary<nint, (SubShape? Self, SubShape? Other)> ActiveContactSubShapes { get; } = new();

    public event Action<PhysicsBodyComponent, CollisionContact>? OnCollisionEnter;

    public event Action<PhysicsBodyComponent, CollisionContact>? OnCollisionStay;

    public event Action<PhysicsBodyComponent>? OnCollisionExit;

    /// <summary>
    /// Fired once when another body begins touching this body, with sub-shape detail.
    /// The first <see cref="SubShape"/> is the sub-shape on <em>this</em> body that was hit
    /// (<c>null</c> when the primary shape was hit). The second is the sub-shape on the
    /// <em>other</em> body (<c>null</c> when its primary shape was hit).
    /// </summary>
    public event Action<PhysicsBodyComponent, CollisionContact, SubShape?, SubShape?>? OnCollisionEnterWithShape;

    /// <summary>
    /// Fired each fixed-update frame while a collision persists, with sub-shape detail.
    /// Arguments mirror <see cref="OnCollisionEnterWithShape"/>.
    /// </summary>
    public event Action<PhysicsBodyComponent, CollisionContact, SubShape?, SubShape?>? OnCollisionStayWithShape;

    /// <summary>
    /// Fired once when another body stops touching this body, with sub-shape detail.
    /// Arguments mirror <see cref="OnCollisionEnterWithShape"/>.
    /// </summary>
    public event Action<PhysicsBodyComponent, SubShape?, SubShape?>? OnCollisionExitWithShape;

    /// <summary>
    /// Fired once when another body begins overlapping this trigger.
    /// </summary>
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
        sub.MarkOwnerShouldCollideChanged = null;
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
            sub.MarkOwnerShouldCollideChanged = null;
        }
        _subShapes.Clear();
        IsDirty = true;
    }

    protected internal override void OnRemoved()
    {
        if (B2.BodyIsValid(BodyId))
        {
            OnBodyDestroyed?.Invoke(BodyId.index1);
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
            sub.MarkOwnerShouldCollideChanged = null;
        }

        IsDirty = true;
        IsFilterDirty = false;
        IsBodyTypeDirty = false;
        IsMassDirty = false;
        IsMaterialDirty = false;
        IsTeleporting = false;
        IsSimulationEnabledDirty = false;
        GravityOverride = null;
        _collidingEntities.Clear();
        ActiveContactPairs.Clear();
        ActiveSensorPairs.Clear();
        ActiveSensorSubShapes.Clear();
        ActiveContactSubShapes.Clear();
        OnBodyDestroyed = null;
        ShouldCollide = null;
        OnBodySleep = null;
        OnBodyWake = null;
        OnCollisionEnterWithShape = null;
        OnCollisionStayWithShape = null;
        OnCollisionExitWithShape = null;
        OnTriggerEnterWithShape = null;
        OnTriggerStayWithShape = null;
        OnTriggerExitWithShape = null;
        ShouldCollideChanged = null;

        ActiveContactSubShapes.Clear();
        OnCollisionEnterWithShape = null;
        OnCollisionStayWithShape = null;
        OnCollisionExitWithShape = null;
    }

    public void ApplyForce(Vector2 force)
    {
        if (!B2.BodyIsValid(BodyId)) return;
        B2.BodyApplyForceToCenter(BodyId, new B2.Vec2 { x = force.X, y = force.Y }, true);
    }

    public void ApplyForce(Vector2 force, Vector2 worldPoint)
    {
        if (!B2.BodyIsValid(BodyId)) return;
        B2.BodyApplyForce(BodyId,
            new B2.Vec2 { x = force.X, y = force.Y },
            new B2.Vec2 { x = worldPoint.X, y = worldPoint.Y },
            true);
    }

    public void ApplyLinearImpulse(Vector2 impulse)
    {
        if (!B2.BodyIsValid(BodyId)) return;
        B2.BodyApplyLinearImpulseToCenter(BodyId, new B2.Vec2 { x = impulse.X, y = impulse.Y }, true);
    }

    public void ApplyLinearImpulse(Vector2 impulse, Vector2 worldPoint)
    {
        if (!B2.BodyIsValid(BodyId)) return;
        B2.BodyApplyLinearImpulse(BodyId,
            new B2.Vec2 { x = impulse.X, y = impulse.Y },
            new B2.Vec2 { x = worldPoint.X, y = worldPoint.Y },
            true);
    }

    public void ApplyAngularImpulse(float impulse)
    {
        if (!B2.BodyIsValid(BodyId)) return;
        B2.BodyApplyAngularImpulse(BodyId, impulse, true);
    }

    public void ApplyTorque(float torque)
    {
        if (!B2.BodyIsValid(BodyId)) return;
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
            B2.BodySetLinearVelocity(BodyId, new B2.Vec2 { x = value.X, y = value.Y });
        }
    }

    public float AngularVelocity
    {
        get => B2.BodyIsValid(BodyId) ? B2.BodyGetAngularVelocity(BodyId) : 0f;
        set
        {
            if (!B2.BodyIsValid(BodyId)) return;
            B2.BodySetAngularVelocity(BodyId, value);
        }
    }

    public bool IsAwake => B2.BodyIsValid(BodyId) && B2.BodyIsAwake(BodyId);

    public void SetAwake(bool awake)
    {
        if (!B2.BodyIsValid(BodyId)) return;
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
        _collidingEntities.Remove(other.Entity);
        ActiveContactPairs.Remove(other);
        ActiveContactSubShapes.TryGetValue(other.BodyId.index1, out var pair);
        ActiveContactSubShapes.Remove(other.BodyId.index1);
        OnCollisionExit?.Invoke(other);
        OnCollisionExitWithShape?.Invoke(other, pair.Self, pair.Other);
    }

    internal void NotifyTriggerExit(PhysicsBodyComponent other)
    {
        if (other.Entity == null) return;
        _collidingEntities.Remove(other.Entity);
        ActiveSensorPairs.Remove(other);
        ActiveSensorSubShapes.TryGetValue(other.BodyId.index1, out var pair);
        ActiveSensorSubShapes.Remove(other.BodyId.index1);
        OnTriggerExit?.Invoke(other);
        OnTriggerExitWithShape?.Invoke(other, pair.Self, pair.Other);
    }

    internal void RaiseCollisionExit(PhysicsBodyComponent other)
    {
        ActiveContactSubShapes.TryGetValue(other.BodyId.index1, out var pair);
        ActiveContactSubShapes.Remove(other.BodyId.index1);
        OnCollisionExit?.Invoke(other);
        OnCollisionExitWithShape?.Invoke(other, pair.Self, pair.Other);
    }

    internal void RaiseTriggerExit(PhysicsBodyComponent other)
    {
        ActiveSensorSubShapes.TryGetValue(other.BodyId.index1, out var pair);
        ActiveSensorSubShapes.Remove(other.BodyId.index1);
        OnTriggerExit?.Invoke(other);
        OnTriggerExitWithShape?.Invoke(other, pair.Self, pair.Other);
    }

    internal HashSet<Entity> CollidingEntitiesInternal => _collidingEntities;
}