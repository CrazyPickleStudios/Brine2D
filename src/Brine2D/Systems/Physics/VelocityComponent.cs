using Brine2D.ECS;
using System.Numerics;

namespace Brine2D.Systems.Physics;

/// <summary>
/// Component for simple velocity-based movement.
/// Automatically applied to TransformComponent by VelocitySystem.
/// </summary>
public class VelocityComponent : Component
{
    /// <summary>
    /// Current velocity (pixels per second).
    /// </summary>
    public Vector2 Velocity { get; set; }

    /// <summary>
    /// Maximum speed (0 = unlimited).
    /// </summary>
    public float MaxSpeed { get; set; } = 0f;

    /// <summary>
    /// Friction/damping factor (0 = no friction, 1 = instant stop).
    /// Applied each frame: velocity *= (1 - friction * deltaTime)
    /// </summary>
    public float Friction { get; set; } = 0f;

    /// <summary>
    /// Whether to apply velocity (can be disabled temporarily).
    /// </summary>
    public bool ApplyVelocity { get; set; } = true;

    /// <summary>
    /// Adds acceleration to velocity.
    /// </summary>
    public void Accelerate(Vector2 acceleration, float deltaTime)
    {
        Velocity += acceleration * deltaTime;
        ClampToMaxSpeed();
    }

    /// <summary>
    /// Sets velocity in a direction with a specific speed.
    /// </summary>
    public void SetDirection(Vector2 direction, float speed)
    {
        if (direction != Vector2.Zero)
        {
            Velocity = Vector2.Normalize(direction) * speed;
        }
    }

    /// <summary>
    /// Clamps velocity to MaxSpeed.
    /// </summary>
    public void ClampToMaxSpeed()
    {
        if (MaxSpeed > 0 && Velocity.LengthSquared() > MaxSpeed * MaxSpeed)
        {
            Velocity = Vector2.Normalize(Velocity) * MaxSpeed;
        }
    }

    /// <summary>
    /// Gets the current speed (magnitude of velocity).
    /// </summary>
    public float CurrentSpeed => Velocity.Length();
}