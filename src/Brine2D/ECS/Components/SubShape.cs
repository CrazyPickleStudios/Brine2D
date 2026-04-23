using Box2D.NET.Bindings;
using Brine2D.Physics;

namespace Brine2D.ECS.Components;

/// <summary>
///     An additional collision shape attached to the same Box2D body as a
///     <see cref="PhysicsBodyComponent" />. Chain shapes are not supported as sub-shapes.
/// </summary>
public sealed class SubShape
{
    internal SubShape(ShapeDefinition definition, bool isTrigger, float? friction, float? restitution)
    {
        Definition = definition;
        IsTrigger = isTrigger;
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

    public ShapeDefinition Definition { get; }

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

    public bool IsTrigger { get; }

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
    ///     Invoked when <see cref="ShouldCollide" /> transitions between null and non-null.
    ///     Wired by the owning <see cref="PhysicsBodyComponent" /> to propagate changes into the
    ///     system's filter-active count.
    /// </summary>
    internal Action<bool>? MarkOwnerShouldCollideChanged { get; set; }

    internal B2.ShapeId ShapeId { get; set; }
}