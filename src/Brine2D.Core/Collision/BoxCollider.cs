using System.Numerics;

namespace Brine2D.Core.Collision;

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

    public override RectangleF GetBounds()
    {
        return new RectangleF(
            Position.X + Offset.X,
            Position.Y + Offset.Y,
            Width,
            Height);
    }

    public override bool Intersects(CollisionShape other)
    {
        if (!IsEnabled || !other.IsEnabled)
            return false;

        return other switch
        {
            BoxCollider box => GetBounds().Intersects(box.GetBounds()),
            CircleCollider circle => IntersectsCircle(circle),
            _ => false
        };
    }

    private bool IntersectsCircle(CircleCollider circle)
    {
        var rect = GetBounds();
        var circlePos = circle.Position;

        var closestX = Math.Clamp(circlePos.X, rect.Left, rect.Right);
        var closestY = Math.Clamp(circlePos.Y, rect.Top, rect.Bottom);

        var distanceX = circlePos.X - closestX;
        var distanceY = circlePos.Y - closestY;

        var distanceSquared = (distanceX * distanceX) + (distanceY * distanceY);
        return distanceSquared < (circle.Radius * circle.Radius);
    }
}