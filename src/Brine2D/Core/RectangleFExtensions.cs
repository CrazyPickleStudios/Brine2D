using System.Drawing;
using System.Numerics;

namespace Brine2D.Core;

/// <summary>
/// Extension methods for System.Drawing.RectangleF with game-specific functionality.
/// </summary>
public static class RectangleFExtensions
{
    /// <summary>
    /// Gets the penetration depth when intersecting with another rectangle.
    /// Returns zero vector if not intersecting.
    /// Useful for collision response (pushing objects apart).
    /// </summary>
    public static Vector2 GetPenetration(this RectangleF rect, RectangleF other)
    {
        if (!rect.IntersectsWith(other))
            return Vector2.Zero;

        float overlapX = Math.Min(rect.Right, other.Right) - Math.Max(rect.Left, other.Left);
        float overlapY = Math.Min(rect.Bottom, other.Bottom) - Math.Max(rect.Top, other.Top);

        // Get centers to determine push direction
        var rectCenter = new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
        var otherCenter = new Vector2(other.X + other.Width / 2, other.Y + other.Height / 2);

        // Push along the axis with smallest penetration
        if (overlapX < overlapY)
        {
            return new Vector2(rectCenter.X < otherCenter.X ? -overlapX : overlapX, 0);
        }
        else
        {
            return new Vector2(0, rectCenter.Y < otherCenter.Y ? -overlapY : overlapY);
        }
    }

    /// <summary>
    /// Gets the center of the rectangle as a Vector2.
    /// </summary>
    public static Vector2 GetCenter(this RectangleF rect)
    {
        return new Vector2(rect.X + rect.Width / 2, rect.Y + rect.Height / 2);
    }

    /// <summary>
    /// Checks if this rectangle contains a Vector2 point.
    /// </summary>
    public static bool Contains(this RectangleF rect, Vector2 point)
    {
        return rect.Contains(point.X, point.Y);
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

    // Optional: Corner helpers if you need them
    public static Vector2 GetTopLeft(this RectangleF rect)
        => new Vector2(rect.X, rect.Y);

    public static Vector2 GetTopRight(this RectangleF rect)
        => new Vector2(rect.Right, rect.Y);

    public static Vector2 GetBottomLeft(this RectangleF rect)
        => new Vector2(rect.X, rect.Bottom);

    public static Vector2 GetBottomRight(this RectangleF rect)
        => new Vector2(rect.Right, rect.Bottom);
}