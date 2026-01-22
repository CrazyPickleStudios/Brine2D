using System.Numerics;

namespace Brine2D.Collision;

/// <summary>
/// Helper methods for collision response calculations.
/// </summary>
public static class CollisionResponse
{
    /// <summary>
    /// Calculates a bounce response when a moving object hits a static surface.
    /// </summary>
    /// <param name="velocity">Current velocity of the moving object.</param>
    /// <param name="penetration">Penetration vector from collision.</param>
    /// <param name="restitution">Bounciness factor (0 = no bounce, 1 = perfect bounce).</param>
    /// <returns>New velocity after bounce.</returns>
    public static Vector2 Bounce(Vector2 velocity, Vector2 penetration, float restitution = 0.8f)
    {
        // Determine which axis has more penetration
        if (Math.Abs(penetration.X) > Math.Abs(penetration.Y))
        {
            // Horizontal collision - reverse X velocity
            return new Vector2(-velocity.X * restitution, velocity.Y);
        }
        else
        {
            // Vertical collision - reverse Y velocity
            return new Vector2(velocity.X, -velocity.Y * restitution);
        }
    }

    /// <summary>
    /// Pushes an object out of collision by the penetration amount.
    /// </summary>
    public static Vector2 Push(Vector2 position, Vector2 penetration)
    {
        return position - penetration;
    }

    /// <summary>
    /// Reflects a vector off a surface normal.
    /// </summary>
    public static Vector2 Reflect(Vector2 direction, Vector2 normal)
    {
        return direction - 2 * Vector2.Dot(direction, normal) * normal;
    }
}