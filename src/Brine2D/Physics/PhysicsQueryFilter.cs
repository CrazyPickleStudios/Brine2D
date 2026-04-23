using Box2D.NET.Bindings;

namespace Brine2D.Physics;

/// <summary>
/// Filter for physics queries (raycasts, shape casts, overlap tests).
/// Mirrors the layer/mask system used by <see cref="Brine2D.ECS.Components.PhysicsBodyComponent"/>.
/// </summary>
public readonly struct PhysicsQueryFilter
{
    public PhysicsQueryFilter() { }

    /// <summary>
    /// Bitmask of categories that the querying shape belongs to.
    /// Only shapes whose <see cref="CollisionMask"/> includes this category will be reported.
    /// Default: all categories.
    /// </summary>
    public ulong CategoryMask { get; init; } = ulong.MaxValue;

    /// <summary>
    /// Bitmask of categories this query should hit.
    /// Default: all categories.
    /// </summary>
    public ulong CollisionMask { get; init; } = ulong.MaxValue;

    /// <summary>
    /// Creates a filter that only hits shapes on the given layer (0-63).
    /// </summary>
    public static PhysicsQueryFilter ForLayer(int layer)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(layer, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(layer, 63);
        return new PhysicsQueryFilter { CollisionMask = 1UL << layer };
    }

    internal B2.QueryFilter ToB2() => new() { categoryBits = CategoryMask, maskBits = CollisionMask };
}