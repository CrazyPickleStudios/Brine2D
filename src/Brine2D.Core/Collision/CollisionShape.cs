using System.Numerics;

namespace Brine2D.Core.Collision;

/// <summary>
/// Base class for collision shapes.
/// </summary>
public abstract class CollisionShape
{
    /// <summary>
    /// Gets or sets the position of the shape in world space.
    /// </summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Gets or sets whether this shape is active for collision detection.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Checks if this shape intersects with another shape.
    /// </summary>
    public abstract bool Intersects(CollisionShape other);

    /// <summary>
    /// Gets the axis-aligned bounding box for broad-phase detection.
    /// </summary>
    public abstract RectangleF GetBounds();
}