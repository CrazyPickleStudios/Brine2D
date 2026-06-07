using System.Numerics;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// Pulls or pushes particles toward/away from a world-space point.
/// </summary>
public sealed class PointAttractor : IParticleForce
{
    /// <summary>World-space position of the attractor.</summary>
    public Vector2 Position { get; set; }

    /// <summary>
    /// Acceleration magnitude in units per second squared.
    /// Positive values attract; negative values repel.
    /// </summary>
    public float Strength { get; set; } = 200f;

    /// <summary>
    /// Minimum distance at which the force is clamped to avoid singularity blow-up
    /// when a particle is very close to the attractor. Default 1 unit.
    /// </summary>
    public float MinDistance { get; set; } = 1f;

    /// <summary>
    /// When true, force falls off with the square of distance (inverse-square law).
    /// When false (default), force magnitude is constant regardless of distance.
    /// </summary>
    public bool UseInverseSquare { get; set; } = false;

    public Vector2 Evaluate(Vector2 particleWorldPosition, float deltaTime)
    {
        var delta = Position - particleWorldPosition;
        var dist = MathF.Max(delta.Length(), MinDistance);
        var direction = delta / dist;

        var magnitude = UseInverseSquare
            ? Strength / (dist * dist)
            : Strength;

        return direction * magnitude * deltaTime;
    }
}

/// <summary>
/// Adds a constant directional acceleration to all particles — a world-space wind or
/// conveyor-belt force. Behaves like a second gravity axis.
/// </summary>
public sealed class DirectionalWind : IParticleForce
{
    /// <summary>
    /// Direction and magnitude of the wind acceleration in units per second squared.
    /// For example <c>new Vector2(150, 0)</c> pushes particles rightward at 150 u/s².
    /// </summary>
    public Vector2 Acceleration { get; set; } = new Vector2(100f, 0f);

    public Vector2 Evaluate(Vector2 particleWorldPosition, float deltaTime) =>
        Acceleration * deltaTime;
}