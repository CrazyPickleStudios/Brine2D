using System.Numerics;
using Brine2D.ECS;
using Brine2D.Rendering;

namespace Brine2D.Rendering.ECS;

/// <summary>
/// Component for particle emission and rendering.
/// Lives in Brine2D.Rendering.ECS because it's rendering-specific.
/// </summary>
public class ParticleEmitterComponent : Component
{
    /// <summary>
    /// Whether the emitter is currently active.
    /// </summary>
    public bool IsEmitting { get; set; } = true;

    /// <summary>
    /// Particle emission rate (particles per second).
    /// </summary>
    public float EmissionRate { get; set; } = 10f;

    /// <summary>
    /// Maximum number of particles.
    /// </summary>
    public int MaxParticles { get; set; } = 100;

    /// <summary>
    /// Particle lifetime in seconds.
    /// </summary>
    public float ParticleLifetime { get; set; } = 2f;

    /// <summary>
    /// Lifetime variation (0-1, adds randomness).
    /// </summary>
    public float LifetimeVariation { get; set; } = 0.3f;

    /// <summary>
    /// Start color of particles.
    /// </summary>
    public Color StartColor { get; set; } = Color.White;

    /// <summary>
    /// End color of particles (lerps over lifetime).
    /// </summary>
    public Color EndColor { get; set; } = new Color(255, 255, 255, 0); // Fade to transparent

    /// <summary>
    /// Start size of particles.
    /// </summary>
    public float StartSize { get; set; } = 4f;

    /// <summary>
    /// End size of particles.
    /// </summary>
    public float EndSize { get; set; } = 0f;

    /// <summary>
    /// Initial velocity direction and magnitude.
    /// </summary>
    public Vector2 InitialVelocity { get; set; } = new Vector2(0, -50);

    /// <summary>
    /// Velocity randomness (angle in degrees).
    /// </summary>
    public float VelocitySpread { get; set; } = 45f;

    /// <summary>
    /// Speed randomness multiplier (0-1).
    /// </summary>
    public float SpeedVariation { get; set; } = 0.5f;

    /// <summary>
    /// Gravity applied to particles (pixels/sec²).
    /// </summary>
    public Vector2 Gravity { get; set; } = new Vector2(0, 100);

    /// <summary>
    /// Spawn offset from emitter position.
    /// </summary>
    public Vector2 SpawnOffset { get; set; } = Vector2.Zero;

    /// <summary>
    /// Spawn area radius (0 = point emitter).
    /// </summary>
    public float SpawnRadius { get; set; } = 0f;

    /// <summary>
    /// Active particles (managed by ParticleSystem).
    /// </summary>
    internal List<Particle> Particles { get; } = new();

    /// <summary>
    /// Emission timer (managed by ParticleSystem).
    /// </summary>
    internal float EmissionTimer { get; set; }
}

/// <summary>
/// Individual particle data.
/// </summary>
internal class Particle
{
    public Vector2 Position;
    public Vector2 Velocity;
    public float Life;
    public float MaxLife;
    public float Size;
}