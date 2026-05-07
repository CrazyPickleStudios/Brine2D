using System.Numerics;

namespace Brine2D.Physics;

/// <summary>
/// Describes the geometry of a Box2D shape. Passed to
/// <see cref="Brine2D.ECS.Components.PhysicsBodyComponent.Shape"/> or
/// <see cref="Brine2D.ECS.Components.PhysicsBodyComponent.AddSubShape"/>.
/// <para>
/// <b>Shape definitions are immutable records.</b> Changing a property (e.g. <c>Radius</c>,
/// <c>Width</c>, <c>Height</c>) creates a new record value — it does not automatically
/// update a live Box2D body. To resize a shape at runtime you must reassign
/// <see cref="Brine2D.ECS.Components.PhysicsBodyComponent.Shape"/> (or the sub-shape's
/// <see cref="Brine2D.ECS.Components.SubShape.Definition"/>) with the new record.
/// The physics system detects the assignment and triggers a full body rebuild on the next tick,
/// which resets all shape IDs and clears sub-step kinematic state.
/// Set shape dimensions once at construction time where possible.
/// </para>
/// </summary>
public abstract record ShapeDefinition
{
    /// <summary>Maximum polygon vertices supported by Box2D.</summary>
    public const int MaxPolygonVertices = 8;
}

/// <param name="Radius">Circle radius in pixels. Must be greater than zero.</param>
public sealed record CircleShape(float Radius) : ShapeDefinition
{
    public float Radius { get; init; } = Radius > 0f
        ? Radius
        : throw new ArgumentOutOfRangeException(nameof(Radius), "Radius must be greater than zero.");

    /// <summary>
    /// Local-space offset of the circle center from the body origin in pixels.
    /// Use this to position the circle away from the body pivot on compound bodies.
    /// Default is <see cref="Vector2.Zero"/>.
    /// </summary>
    public Vector2 Offset { get; init; } = Vector2.Zero;
}

/// <param name="Width">Full width in pixels. Must be greater than zero.</param>
/// <param name="Height">Full height in pixels. Must be greater than zero.</param>
public sealed record BoxShape(float Width, float Height) : ShapeDefinition
{
    public float Width { get; init; } = Width > 0f
        ? Width
        : throw new ArgumentOutOfRangeException(nameof(Width), "Width must be greater than zero.");

    public float Height { get; init; } = Height > 0f
        ? Height
        : throw new ArgumentOutOfRangeException(nameof(Height), "Height must be greater than zero.");

    /// <summary>
    /// Local-space offset of the box center from the body origin in pixels.
    /// Use this to position the box away from the body pivot on compound bodies.
    /// Default is <see cref="Vector2.Zero"/>.
    /// </summary>
    public Vector2 Offset { get; init; } = Vector2.Zero;

    /// <summary>
    /// Rotation of the box in radians relative to the body's local space.
    /// Default is 0 (axis-aligned).
    /// </summary>
    public float Angle { get; init; } = 0f;
}

/// <param name="Center1">First center point in local body space (pixels).</param>
/// <param name="Center2">Second center point in local body space (pixels).</param>
/// <param name="Radius">Capsule radius in pixels. Must be greater than zero.</param>
public sealed record CapsuleShape(Vector2 Center1, Vector2 Center2, float Radius) : ShapeDefinition
{
    public float Radius { get; init; } = Radius > 0f
        ? Radius
        : throw new ArgumentOutOfRangeException(nameof(Radius), "Radius must be greater than zero.");

    /// <summary>
    /// Local-space offset applied to both center points, shifting the entire capsule
    /// relative to the body origin in pixels.
    /// Default is <see cref="Vector2.Zero"/>.
    /// </summary>
    public Vector2 Offset { get; init; } = Vector2.Zero;
}

/// <summary>
/// Convex polygon. Vertices are in local body space.
/// <para>
/// Box2D computes the convex hull of the supplied vertices — concave or self-intersecting
/// vertex lists are silently reduced to their convex hull in all build configurations.
/// Ensure your vertices are already convex and wound consistently (clockwise or
/// counter-clockwise) if the exact shape matters; Box2D may discard vertices or reorder
/// them during hull computation. In DEBUG builds a convexity assertion is raised when
/// non-convex input is detected so issues surface during development, but in RELEASE builds
/// the hull is computed silently with no warning. There is no runtime convex-decomposition;
/// for concave shapes use multiple <see cref="PhysicsBodyComponent.AddSubShape"/> calls,
/// each with a convex <see cref="PolygonShape"/>.
/// </para>
/// Maximum of <see cref="ShapeDefinition.MaxPolygonVertices"/> vertices (Box2D limit).
/// </summary>
public sealed record PolygonShape : ShapeDefinition
{
    private readonly Vector2[] _vertices;

    public PolygonShape(ReadOnlySpan<Vector2> vertices, float radius = 0f)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(vertices.Length, 3);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(vertices.Length, MaxPolygonVertices);
        ArgumentOutOfRangeException.ThrowIfNegative(radius);
#if DEBUG
        AssertConvex(vertices);
#endif
        _vertices = vertices.ToArray();
        Radius = radius;
    }

    public IReadOnlyList<Vector2> Vertices => _vertices;

    internal ReadOnlySpan<Vector2> VerticesSpan => _vertices;

    /// <summary>
    /// Skin radius (polygon inflation) in pixels. Box2D uses this for numerical stability
    /// and continuous collision detection. Default is 0 (sharp edges).
    /// </summary>
    public float Radius { get; init; }

    /// <summary>
    /// Local-space offset applied to every vertex, shifting the entire polygon
    /// relative to the body origin in pixels.
    /// Use this to position the polygon away from the body pivot on compound bodies
    /// without recomputing all vertex positions manually.
    /// Default is <see cref="Vector2.Zero"/>.
    /// </summary>
    public Vector2 Offset { get; init; } = Vector2.Zero;

#if DEBUG
    private static void AssertConvex(ReadOnlySpan<Vector2> vertices)
    {
        int n = vertices.Length;
        float? expectedSign = null;

        for (int i = 0; i < n; i++)
        {
            var a = vertices[i];
            var b = vertices[(i + 1) % n];
            var c = vertices[(i + 2) % n];

            float cross = (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

            if (MathF.Abs(cross) < 1e-6f)
                continue;

            if (expectedSign == null)
            {
                expectedSign = MathF.Sign(cross);
                continue;
            }

            if (MathF.Sign(cross) != (int)expectedSign.Value)
                throw new ArgumentException(
                    "PolygonShape vertices are not convex. Box2D will silently compute a convex hull " +
                    "of the supplied points, which may not match the intended shape. " +
                    "Ensure vertices are ordered consistently (CW or CCW) and form a convex outline.",
                    nameof(vertices));
        }
    }
#endif
}

/// <summary>
/// Chain shape for smooth static terrain. Points are in local body space.
/// Chain shapes do not support triggers, bullet mode, mass, or non-Static body types.
/// <para>
/// <b>Sensor limitation:</b> Box2D only generates sensor/trigger events when a sensor shape
/// overlaps a <em>non-sensor</em> shape. Two <see cref="Brine2D.ECS.Components.PhysicsBodyComponent.IsTrigger"/>
/// bodies will never fire trigger events with each other.
/// </para>
/// <para>
/// <b>Contact event volume:</b> each segment of a chain shape is an independent Box2D shape.
/// On long terrain chains, a fast-moving body can simultaneously touch many segments,
/// generating a proportional number of contact events per fixed-update tick.
/// Keep chain point density reasonable for sections where dynamic bodies travel.
/// </para>
/// </summary>
public sealed record ChainShape : ShapeDefinition
{
    public ChainShape(ReadOnlySpan<Vector2> points, bool isLoop = false)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(points.Length, 2);
        Points = points.ToArray();
        IsLoop = isLoop;
    }

    public Vector2[] Points { get; }
    public bool IsLoop { get; }

    /// <summary>
    /// Per-segment surface materials. When non-<c>null</c>, length must equal the segment count:
    /// <c>Points.Length - 1</c> for open chains, <c>Points.Length</c> for loops.
    /// When <c>null</c>, every segment inherits <see cref="ECS.Components.PhysicsBodyComponent.SurfaceFriction"/>
    /// and <see cref="ECS.Components.PhysicsBodyComponent.Restitution"/> from the owning body.
    /// </summary>
    public (float Friction, float Restitution)[]? SegmentMaterials { get; init; }
}

/// <summary>
/// Single line segment in local body space.
/// <para>
/// <b>One-sided collision:</b> segment shapes only collide with shapes approaching from
/// the <em>outward normal</em> side. The outward normal is the left-hand perpendicular
/// of the direction <c>Point1 → Point2</c> — i.e. rotating <c>(Point2 - Point1)</c>
/// 90° counter-clockwise. Shapes that contact the segment from the opposite (back) side
/// pass straight through without generating any collision response. This makes segment
/// shapes useful for one-way platforms and directional blockers, but unsuitable for
/// closed geometry where back-face hits must be resolved.
/// </para>
/// <para>
/// <b>Use cases:</b> one-way platforms (horizontal segment, normal pointing up),
/// directional walls, simple ramp surfaces.
/// For smooth multi-segment terrain that avoids ghost collisions at joints, prefer
/// <see cref="ChainShape"/>, which Box2D handles as a single continuous surface.
/// For a two-sided collidable edge, use a very thin <see cref="CapsuleShape"/> or a
/// narrow <see cref="BoxShape"/>.
/// </para>
/// <para>
/// <b>Static geometry only:</b> dynamic bodies with segment shapes behave unpredictably
/// under rotation and are not supported by Box2D.
/// </para>
/// </summary>
/// <param name="Point1">Start of the segment in local body space (pixels).</param>
/// <param name="Point2">End of the segment in local body space (pixels).</param>
public sealed record SegmentShape(Vector2 Point1, Vector2 Point2) : ShapeDefinition
{
    /// <summary>
    /// Local-space offset applied to both <see cref="Point1"/> and <see cref="Point2"/>
    /// when the shape is built. Use this to position the segment away from the body origin
    /// on compound bodies without recomputing both points manually.
    /// Default is <see cref="Vector2.Zero"/>.
    /// </summary>
    public Vector2 Offset { get; init; } = Vector2.Zero;
}