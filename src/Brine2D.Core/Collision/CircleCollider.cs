using System.Numerics;

namespace Brine2D.Core.Collision;

/// <summary>
/// Circle collider for round objects.
/// </summary>
public class CircleCollider : CollisionShape
{
    public float Radius { get; set; }

    public CircleCollider(float radius)
    {
        Radius = radius;
    }

    public override RectangleF GetBounds()
    {
        return new RectangleF(
            Position.X - Radius,
            Position.Y - Radius,
            Radius * 2,
            Radius * 2);
    }

    public override bool Intersects(CollisionShape other)
    {
        if (!IsEnabled || !other.IsEnabled)
            return false;

        return other switch
        {
            CircleCollider circle => IntersectsCircle(circle),
            BoxCollider box => box.Intersects(this),
            _ => false
        };
    }

    private bool IntersectsCircle(CircleCollider other)
    {
        var distance = Vector2.Distance(Position, other.Position);
        return distance < (Radius + other.Radius);
    }
}