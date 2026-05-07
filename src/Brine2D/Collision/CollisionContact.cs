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
    ///     Approximate world-space contact point (deepest penetration point when two points
    ///     are present). See <see cref="ContactPoint2" /> for the second point, if any.
    /// </summary>
    public Vector2 ContactPoint { get; init; }

    /// <summary>
    ///     Second contact point when the manifold contains two points, otherwise
    ///     <see cref="Vector2.Zero" />. Use <see cref="ContactPointCount" /> to distinguish a
    ///     genuine second point from the default value.
    /// </summary>
    public Vector2 ContactPoint2 { get; init; }

    /// <summary>
    ///     Number of contact points available (0, 1, or 2).
    ///     <list type="bullet">
    ///         <item>0 — the manifold had no points (e.g. an initial speculative contact).</item>
    ///         <item>1 — a single contact point; <see cref="ContactPoint2" /> is zero.</item>
    ///         <item>2 — two contact points, useful for edge-on-face contacts such as a box
    ///             resting flat on a surface.</item>
    ///     </list>
    /// </summary>
    public int ContactPointCount { get; init; }

    /// <summary>
    ///     Closing speed along the contact normal at the moment of detection.
    ///     <para>
    ///         <b>Important:</b> this value is only meaningful when received via
    ///         <see cref="ECS.Components.PhysicsBodyComponent.OnCollisionHit" />, which uses Box2D's
    ///         dedicated hit-event approach speed. When received via
    ///         <see cref="ECS.Components.PhysicsBodyComponent.OnCollisionEnter" />, the value is
    ///         always <c>0</c> — Box2D's <c>normalVelocity</c> is unreliable on the first contact
    ///         frame. When received via
    ///         <see cref="ECS.Components.PhysicsBodyComponent.OnCollisionStay" />, the value is
    ///         derived from the manifold's <c>normalVelocity</c> field, which can be noisy.
    ///         Use <see cref="ECS.Components.PhysicsBodyComponent.OnCollisionHit" /> with
    ///         <see cref="ECS.Components.PhysicsBodyComponent.EnableHitEvents" /> for impact-speed-based
    ///         logic (e.g. collision sound volume).
    ///     </para>
    /// </summary>
    public float ImpactSpeed { get; init; }

    internal static readonly CollisionContact Empty = default;
    internal bool IsEmpty => ContactPointCount == 0 && Normal == Vector2.Zero;

    internal static CollisionContact FromManifold(B2.Manifold manifold) =>
        FromManifoldCore(manifold, zeroeImpactSpeed: false);

    internal static CollisionContact FromManifoldEnter(B2.Manifold manifold) =>
        FromManifoldCore(manifold, zeroeImpactSpeed: true);

    private static CollisionContact FromManifoldCore(B2.Manifold manifold, bool zeroeImpactSpeed)
    {
        var normal = new Vector2(manifold.normal.x, manifold.normal.y);

        if (manifold.pointCount == 0)
            return new CollisionContact { Normal = normal };

        int primaryIdx = 0;
        if (manifold.pointCount > 1 && manifold.points[1].separation < manifold.points[0].separation)
            primaryIdx = 1;

        int secondaryIdx = 1 - primaryIdx;
        var primary = manifold.points[primaryIdx];
        float impactSpeed = zeroeImpactSpeed ? 0f : MathF.Max(0f, primary.normalVelocity);

        if (manifold.pointCount == 1)
        {
            return new CollisionContact
            {
                Normal = normal,
                ContactPoint = new Vector2(primary.point.x, primary.point.y),
                Depth = MathF.Abs(primary.separation),
                ImpactSpeed = impactSpeed,
                ContactPointCount = 1
            };
        }

        var secondary = manifold.points[secondaryIdx];

        return new CollisionContact
        {
            Normal = normal,
            ContactPoint = new Vector2(primary.point.x, primary.point.y),
            Depth = MathF.Abs(primary.separation),
            ContactPoint2 = new Vector2(secondary.point.x, secondary.point.y),
            ImpactSpeed = impactSpeed,
            ContactPointCount = 2
        };
    }
}