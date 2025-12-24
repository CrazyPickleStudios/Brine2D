using System.Numerics;

namespace Brine2D.Core.Collision;

/// <summary>
/// Represents a rectangle with floating-point coordinates.
/// </summary>
public struct RectangleF
{
    public float X { get; set; }
    public float Y { get; set; }
    public float Width { get; set; }
    public float Height { get; set; }

    public float Left => X;
    public float Right => X + Width;
    public float Top => Y;
    public float Bottom => Y + Height;

    public Vector2 Center => new Vector2(X + Width / 2, Y + Height / 2);
    public Vector2 TopLeft => new Vector2(X, Y);
    public Vector2 TopRight => new Vector2(X + Width, Y);
    public Vector2 BottomLeft => new Vector2(X, Y + Height);
    public Vector2 BottomRight => new Vector2(X + Width, Y + Height);

    public RectangleF(float x, float y, float width, float height)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Creates a rectangle from center position and size.
    /// </summary>
    public static RectangleF FromCenter(Vector2 center, float width, float height)
    {
        return new RectangleF(
            center.X - width / 2,
            center.Y - height / 2,
            width,
            height);
    }

    /// <summary>
    /// Checks if this rectangle intersects with another.
    /// </summary>
    public bool Intersects(RectangleF other)
    {
        return Right > other.Left &&
               Left < other.Right &&
               Bottom > other.Top &&
               Top < other.Bottom;
    }

    /// <summary>
    /// Checks if this rectangle contains a point.
    /// </summary>
    public bool Contains(Vector2 point)
    {
        return point.X >= Left &&
               point.X <= Right &&
               point.Y >= Top &&
               point.Y <= Bottom;
    }

    /// <summary>
    /// Gets the penetration depth when intersecting with another rectangle.
    /// Returns zero vector if not intersecting.
    /// </summary>
    public Vector2 GetPenetration(RectangleF other)
    {
        if (!Intersects(other))
            return Vector2.Zero;

        float overlapX = Math.Min(Right, other.Right) - Math.Max(Left, other.Left);
        float overlapY = Math.Min(Bottom, other.Bottom) - Math.Max(Top, other.Top);

        if (overlapX < overlapY)
        {
            return new Vector2(Center.X < other.Center.X ? -overlapX : overlapX, 0);
        }
        else
        {
            return new Vector2(0, Center.Y < other.Center.Y ? -overlapY : overlapY);
        }
    }
}