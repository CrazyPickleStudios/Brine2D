using System.Numerics;

namespace Brine2D.Physics;

/// <summary>
/// Describes the geometry of a Box2D shape. Passed to
/// <see cref="Brine2D.ECS.Components.PhysicsBodyComponent.Shape"/> or
/// <see cref="Brine2D.ECS.Components.PhysicsBodyComponent.AddSubShape"/>.
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
}

/// <summary>
/// Convex polygon. Vertices are in local body space.
/// <para>
/// Box2D computes the convex hull of the supplied vertices — concave or self-intersecting
/// vertex lists are silently reduced to their convex hull. Ensure your vertices are already
/// convex if the exact shape matters. In DEBUG builds, a convexity assertion is raised when
/// non-convex input is detected so issues surface during development.
/// </para>
/// Maximum of <see cref="ShapeDefinition.MaxPolygonVertices"/> vertices (Box2D limit).
/// </summary>
public sealed record PolygonShape : ShapeDefinition
{
    public PolygonShape(ReadOnlySpan<Vector2> vertices, float radius = 0f)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(vertices.Length, 3);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(vertices.Length, MaxPolygonVertices);
        ArgumentOutOfRangeException.ThrowIfNegative(radius);
#if DEBUG
        AssertConvex(vertices);
#endif
        Vertices = vertices.ToArray();
        Radius = radius;
    }

    public Vector2[] Vertices { get; }

    /// <summary>
    /// Skin radius (polygon inflation) in pixels. Box2D uses this for numerical stability
    /// and continuous collision detection. Default is 0 (sharp edges).
    /// </summary>
    public float Radius { get; init; }

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