using System.Numerics;
using Brine2D.Core;

namespace Brine2D.Collision;

/// <summary>
/// Circle collider for round objects.
/// </summary>
public class CircleCollider : CollisionShape
{
    public float Radius { get; set; }
    
    /// <summary>
    /// Offset from position (for centering).
    /// </summary>
    public Vector2 Offset { get; set; }

    public CircleCollider(float radius, Vector2? offset = null)
    {
        Radius = radius;
        Offset = offset ?? Vector2.Zero;
    }

    public override Rectangle GetBounds()
    {
        var actualPos = Position + Offset;
        return new Rectangle(
            actualPos.X - Radius,
            actualPos.Y - Radius,
            Radius * 2,
            Radius * 2);
    }

    public override bool Intersects(CollisionShape other)
    {
        if (!IsEnabled || !other.IsEnabled)
            return false;

        // Zero-radius colliders don't collide
        if (Radius <= 0)
            return false;

        return other switch
        {
            CircleCollider circle => circle.Radius <= 0 ? false : IntersectsCircle(circle),
            BoxCollider box => box.Intersects(this),
            _ => false
        };
    }

    private bool IntersectsCircle(CircleCollider other)
    {
        // Zero-radius circles don't collide
        if (Radius <= 0 || other.Radius <= 0)
            return false;
    
        var pos1 = Position + Offset;
        var pos2 = other.Position + other.Offset;
        var distance = Vector2.Distance(pos1, pos2);
        return distance <= (Radius + other.Radius);
    }
}