using System.Text.Json.Serialization;
using Box2D.NET.Bindings;
using Brine2D.Physics;

namespace Brine2D.ECS.Components;

/// <summary>
///     An additional collision shape attached to the same Box2D body as a
///     <see cref="PhysicsBodyComponent" />. Chain shapes are not supported as sub-shapes.
/// </summary>
public sealed class SubShape
{
    private bool _isTrigger;

    internal SubShape(ShapeDefinition definition, bool isTrigger, float? friction, float? restitution)
    {
        _definition = definition;
        _isTrigger = isTrigger;
        Friction = friction;
        Restitution = restitution;
    }

    /// <summary>
    ///     Collision mask override for this sub-shape. When <c>null</c>, inherits
    ///     <see cref="PhysicsBodyComponent.CollisionMask" /> from the owning body.
    ///     Takes effect immediately on a live body via a lightweight filter update.
    /// </summary>
    public ulong? CollisionMask
    {
        get;
        set
        {
            field = value;
            MarkOwnerFilterDirty?.Invoke();
        }
    }

    private ShapeDefinition _definition;

    public ShapeDefinition Definition => _definition;

    /// <summary>
    /// Updates the geometry of this sub-shape. When the body is live and the new definition
    /// is the same shape type as the current one, the change is applied via a lightweight
    /// <c>B2.ShapeSet*</c> call. When the shape type changes, a full body rebuild is triggered.
    /// Chain shapes are not supported.
    /// </summary>
    /// <exception cref="ArgumentException">Thrown when <paramref name="newDefinition"/> is a <see cref="ChainShape"/>.</exception>
    public void UpdateDefinition(ShapeDefinition newDefinition)
    {
        if (newDefinition is ChainShape)
            throw new ArgumentException("Chain shapes are not supported as sub-shapes.", nameof(newDefinition));

        bool sameType = _definition.GetType() == newDefinition.GetType();
        _definition = newDefinition;

        if (sameType)
            MarkOwnerGeometryDirty?.Invoke();
        else
            MarkOwnerDirty?.Invoke();
    }

    internal Action? MarkOwnerGeometryDirty { get; set; }

    /// <summary>
    ///     Whether hit events (<see cref="PhysicsBodyComponent.OnCollisionHit" />) are enabled for
    ///     this sub-shape. When <c>null</c>, inherits <see cref="PhysicsBodyComponent.EnableHitEvents" />
    ///     from the owning body. Changes on a live body apply immediately.
    /// </summary>
    public bool? EnableHitEvents
    {
        get;
        set
        {
            field = value;
            MarkOwnerMaterialDirty?.Invoke();
        }
    }

    /// <summary>
    ///     Surface friction for this sub-shape (0–1). When <c>null</c>, inherits
    ///     <see cref="PhysicsBodyComponent.SurfaceFriction" /> from the owning body.
    ///     Changes on a live body apply immediately via a lightweight material update.
    /// </summary>
    public float? Friction
    {
        get;
        set
        {
            field = value;
            MarkOwnerMaterialDirty?.Invoke();
        }
    }

    /// <summary>
    ///     When <c>true</c>, this sub-shape acts as a sensor: it fires trigger events but
    ///     generates no collision response. Changing this on a live body applies immediately
    ///     via a lightweight sensor-events toggle — no full rebuild is required.
    /// </summary>
    public bool IsTrigger
    {
        get => _isTrigger;
        set
        {
            if (_isTrigger == value) return;
            _isTrigger = value;
            MarkOwnerTriggerDirty?.Invoke();
        }
    }

    /// <summary>
    ///     Collision layer override for this sub-shape (0–63). When <c>null</c>, inherits
    ///     <see cref="PhysicsBodyComponent.Layer" /> from the owning body.
    ///     Takes effect immediately on a live body via a lightweight filter update.
    /// </summary>
    public int? Layer
    {
        get;
        set
        {
            if (value.HasValue)
            {
                ArgumentOutOfRangeException.ThrowIfLessThan(value.Value, 0);
                ArgumentOutOfRangeException.ThrowIfGreaterThan(value.Value, 63);
            }

            field = value;
            MarkOwnerFilterDirty?.Invoke();
        }
    }

    /// <summary>
    ///     Raw category bitmask override for this sub-shape. When non-zero, overrides the
    ///     single-bit mask derived from <see cref="Layer"/> (i.e. <c>1UL &lt;&lt; Layer</c>).
    ///     When <c>null</c>, falls back to <see cref="Layer"/>-derived bits or the owning
    ///     body's <see cref="PhysicsBodyComponent.CategoryBits"/>.
    ///     Takes effect immediately on a live body via a lightweight filter update.
    /// </summary>
    public ulong? CategoryBits
    {
        get;
        set
        {
            field = value;
            MarkOwnerFilterDirty?.Invoke();
        }
    }

    /// <summary>
    ///     Restitution (bounciness) for this sub-shape (0–1). When <c>null</c>, inherits
    ///     <see cref="PhysicsBodyComponent.Restitution" /> from the owning body.
    ///     Changes on a live body apply immediately via a lightweight material update.
    /// </summary>
    public float? Restitution
    {
        get;
        set
        {
            field = value;
            MarkOwnerMaterialDirty?.Invoke();
        }
    }

    /// <summary>
    ///     Per-sub-shape collision filter. When set, called for every candidate contact pair
    ///     involving this sub-shape — return <c>false</c> to prevent the pair from colliding.
    ///     <para>
    ///         The first parameter is the other <see cref="PhysicsBodyComponent" />. The second is the
    ///         specific <see cref="SubShape" /> on that body involved in the contact, or <c>null</c>
    ///         when the other body's primary shape or a chain segment is the candidate.
    ///     </para>
    ///     The callback is invoked from the Box2D broad-phase on the simulation thread — keep it
    ///     allocation-free.
    /// </summary>
    [JsonIgnore]
    public Func<PhysicsBodyComponent, SubShape?, bool>? ShouldCollide
    {
        get;
        set
        {
            var wasSet = field != null;
            field = value;
            var isSet = field != null;

            if (wasSet != isSet)
            {
                MarkOwnerShouldCollideChanged?.Invoke(isSet);
            }
        }
    }

    internal Action? MarkOwnerDirty { get; set; }

    internal Action? MarkOwnerFilterDirty { get; set; }

    internal Action? MarkOwnerMaterialDirty { get; set; }

    /// <summary>
    ///     Invoked when <see cref="IsTrigger" /> changes on a live body sub-shape.
    ///     Wired by the owning <see cref="PhysicsBodyComponent" /> to propagate the change
    ///     into the system's sub-shape trigger handler.
    /// </summary>
    internal Action? MarkOwnerTriggerDirty { get; set; }

    /// <summary>
    ///     Invoked when <see cref="ShouldCollide" /> transitions between null and non-null.
    ///     Wired by the owning <see cref="PhysicsBodyComponent" /> to propagate changes into the
    ///     system's filter-active count.
    /// </summary>
    internal Action<bool>? MarkOwnerShouldCollideChanged { get; set; }

    internal B2.ShapeId ShapeId { get; set; }

    /// <summary>
    /// Box2D collision group index for this sub-shape. Overrides the owning body's
    /// <see cref="PhysicsBodyComponent.GroupIndex"/> for this shape only.
    /// Positive = always collide with same group, negative = never collide, 0 = use
    /// category/mask bits (default).
    /// </summary>
    public int GroupIndex
    {
        get => field;
        set
        {
            if (field == value) return;
            field = value;
            MarkOwnerFilterDirty?.Invoke();
        }
    }
}