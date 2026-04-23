using System.Numerics;
using Box2D.NET.Bindings;

namespace Brine2D.Collision;

/// <summary>
///     Lightweight collision data passed to <see cref="ECS.Components.PhysicsBodyComponent" /> event handlers.
///     Contains the contact normal, penetration depth, and approximate contact point.
///     For trigger pairs, <see cref="Normal" />, <see cref="Depth" />, and <see cref="ContactPoint" />
///     may be zero when no penetration resolution was performed.
/// </summary>
public readonly struct CollisionContact
{
    /// <summary>
    ///     Surface normal pointing away from the other collider toward this collider.
    /// </summary>
    public Vector2 Normal { get; init; }

    /// <summary>
    ///     Penetration depth along <see cref="Normal" />.
    /// </summary>
    public float Depth { get; init; }

    /// <summary>
    ///     Approximate world-space contact point.
    /// </summary>
    public Vector2 ContactPoint { get; init; }

    /// <summary>
    ///     Closing speed along the contact normal at the moment of detection.
    ///     Positive means the bodies were approaching. Zero for triggers or missing velocity data.
    ///     Useful for scaling impact sound volume or particle effects.
    /// </summary>
    public float ImpactSpeed { get; init; }

    internal static readonly CollisionContact Empty = default;

    internal static CollisionContact FromManifold(B2.Manifold manifold)
    {
        var normal = new Vector2(manifold.normal.x, manifold.normal.y);

        if (manifold.pointCount == 0)
        {
            return new CollisionContact { Normal = normal };
        }

        var p = manifold.points[0];
        if (manifold.pointCount > 1 && manifold.points[1].separation < manifold.points[0].separation)
        {
            p = manifold.points[1];
        }

        return new CollisionContact
        {
            Normal = normal,
            ContactPoint = new Vector2(p.point.x, p.point.y),
            Depth = MathF.Abs(p.separation),
            ImpactSpeed = MathF.Max(0f, p.normalVelocity)
        };
    }
}