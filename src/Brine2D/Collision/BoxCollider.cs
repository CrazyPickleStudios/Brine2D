using System.Numerics;
using Brine2D.Core;

namespace Brine2D.Collision;

/// <summary>
/// Axis-aligned bounding box collider.
/// </summary>
public class BoxCollider : CollisionShape
{
    public float Width { get; set; }
    public float Height { get; set; }

    /// <summary>
    /// Offset from position (useful for centering collision box).
    /// </summary>
    public Vector2 Offset { get; set; }

    public BoxCollider(float width, float height, Vector2? offset = null)
    {
        Width = width;
        Height = height;
        Offset = offset ?? Vector2.Zero;
    }

    public override Rectangle GetBounds()
    {
        return new Rectangle(
            Position.X + Offset.X,
            Position.Y + Offset.Y,
            Width,
            Height);
    }

    public override bool Intersects(CollisionShape other)
    {
        if (!IsEnabled || !other.IsEnabled)
            return false;

        // Zero-size colliders don't collide
        if (Width <= 0 || Height <= 0)
            return false;

        return other switch
        {
            BoxCollider box => box.Width <= 0 || box.Height <= 0 ? false : IntersectsBox(box),
            CircleCollider circle => IntersectsCircle(circle),
            _ => false
        };
    }

    private bool IntersectsBox(BoxCollider other)
    {
        var b1 = GetBounds();
        var b2 = other.GetBounds();
        
        // Use <= instead of < to include touching edges
        return b1.Left <= b2.Right &&
               b1.Right >= b2.Left &&
               b1.Top <= b2.Bottom &&
               b1.Bottom >= b2.Top;
    }

    private bool IntersectsCircle(CircleCollider circle)
    {
        if (circle.Radius <= 0)
            return false;
        
        var rect = GetBounds();
        var circlePos = circle.Position + circle.Offset;

        var closestX = Math.Clamp(circlePos.X, rect.Left, rect.Right);
        var closestY = Math.Clamp(circlePos.Y, rect.Top, rect.Bottom);

        var distanceX = circlePos.X - closestX;
        var distanceY = circlePos.Y - closestY;

        var distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
        return distanceSquared <= (circle.Radius * circle.Radius);
    }
}