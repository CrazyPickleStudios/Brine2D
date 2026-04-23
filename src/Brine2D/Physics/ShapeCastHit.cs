using System.Numerics;
using Box2D.NET.Bindings;
using Brine2D.ECS.Components;

namespace Brine2D.Physics;

/// <summary>
/// Result of a shape cast (sweep) against the physics world.
/// </summary>
public readonly struct ShapeCastHit
{
    /// <summary>
    /// World-space hit point in pixel coordinates.
    /// </summary>
    public Vector2 Point { get; init; }

    /// <summary>
    /// Surface normal at the hit point, pointing away from the hit shape.
    /// </summary>
    public Vector2 Normal { get; init; }

    /// <summary>
    /// Fraction along the sweep (0 = origin, 1 = origin + full translation).
    /// </summary>
    public float Fraction { get; init; }

    /// <summary>
    /// Distance from the sweep origin to the hit point in pixels
    /// (<c>Fraction * maxDistance</c> passed to the query).
    /// </summary>
    public float Distance { get; init; }

    /// <summary>
    /// The <see cref="PhysicsBodyComponent"/> that was hit, or <c>null</c> if the body
    /// has no associated component (e.g. the physics system has not yet initialized).
    /// </summary>
    public PhysicsBodyComponent? Component { get; init; }

    /// <summary>
    /// The specific sub-shape that was hit, or <c>null</c> when the primary shape was hit
    /// (or the body uses a chain shape whose segments have no individual <see cref="SubShape"/>).
    /// </summary>
    public SubShape? SubShape { get; init; }

    internal B2.ShapeId ShapeId { get; init; }
}