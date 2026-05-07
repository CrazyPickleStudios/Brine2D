using Box2D.NET.Bindings;
using Brine2D.ECS.Components;

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
    /// When <c>true</c>, sensor (trigger) shapes are excluded from query results.
    /// Filtering is applied post-callback via <c>B2.ShapeIsSensor</c>.
    /// Useful for ground-detection casts and line-of-sight tests that must not hit trigger zones.
    /// Default is <c>false</c> (sensors are included).
    /// </summary>
    /// <remarks>
    /// For <c>RaycastClosest</c> and <c>ShapeCastClosest</c> with <c>ExcludeSensors = true</c>,
    /// the query internally collects all hits and returns the nearest non-sensor result, so a solid
    /// body behind a trigger zone is correctly found.
    /// </remarks>
    public bool ExcludeSensors { get; init; }

    /// <summary>
    /// When non-<c>null</c>, all shapes belonging to this body are excluded from query results.
    /// Useful for self-exclusion when casting from a body's own position.
    /// For excluding multiple bodies, use <see cref="ExcludeBodies"/>.
    /// </summary>
    public PhysicsBodyComponent? ExcludeBody { get; init; }

    /// <summary>
    /// When non-<c>null</c>, all shapes belonging to any of these bodies are excluded from query
    /// results. Extends <see cref="ExcludeBody"/> for multi-body self-exclusion (e.g. compound
    /// characters with multiple colliders, or ignore-list queries).
    /// </summary>
    public PhysicsBodyComponent[]? ExcludeBodies { get; init; }

    /// <summary>
    /// A pre-built filter that excludes sensor shapes and hits all solid layers.
    /// Suitable for ground-detection casts and line-of-sight tests.
    /// </summary>
    public static PhysicsQueryFilter SolidOnly { get; } = new() { ExcludeSensors = true };

    /// <summary>
    /// Creates a filter that only hits shapes on the given layer (0–63).
    /// </summary>
    public static PhysicsQueryFilter ForLayer(int layer)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(layer, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(layer, 63);
        return new PhysicsQueryFilter { CollisionMask = 1UL << layer };
    }

    /// <summary>
    /// Creates a filter that only hits solid (non-sensor) shapes on the given layer (0–63).
    /// </summary>
    public static PhysicsQueryFilter SolidLayer(int layer)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(layer, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(layer, 63);
        return new PhysicsQueryFilter { CollisionMask = 1UL << layer, ExcludeSensors = true };
    }

    /// <summary>
    /// Creates a filter that hits shapes on any of the given layers (0–63).
    /// </summary>
    /// <example>
    /// <code>PhysicsQueryFilter.ForLayers(0, 2, 5)</code>
    /// </example>
    public static PhysicsQueryFilter ForLayers(params ReadOnlySpan<int> layers)
    {
        ulong mask = 0;
        foreach (int layer in layers)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(layer, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(layer, 63);
            mask |= 1UL << layer;
        }
        return new PhysicsQueryFilter { CollisionMask = mask };
    }

    /// <summary>
    /// Creates a filter that only hits solid (non-sensor) shapes on any of the given layers (0–63).
    /// </summary>
    /// <example>
    /// <code>PhysicsQueryFilter.SolidLayers(0, 2, 5)</code>
    /// </example>
    public static PhysicsQueryFilter SolidLayers(params ReadOnlySpan<int> layers)
    {
        ulong mask = 0;
        foreach (int layer in layers)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(layer, 0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(layer, 63);
            mask |= 1UL << layer;
        }
        return new PhysicsQueryFilter { CollisionMask = mask, ExcludeSensors = true };
    }

    internal B2.QueryFilter ToB2() => new()
    {
        categoryBits = CategoryMask,
        maskBits = CollisionMask
    };
}