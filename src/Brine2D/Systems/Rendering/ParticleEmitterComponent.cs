using System.Drawing;
using Brine2D.Pooling;
using Brine2D.ECS;
using Brine2D.Rendering;
using Brine2D.Rendering.TextureAtlas;
using System.Numerics;

namespace Brine2D.Systems.Rendering;

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
    public Color EndColor { get; set; } = Color.FromArgb(0, 255, 255, 255); // Fade to transparent

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
    /// Optional texture for particles. If null, particles render as colored circles.
    /// </summary>
    public ITexture? ParticleTexture { get; set; }

    /// <summary>
    /// Optional atlas region for particles. Takes precedence over ParticleTexture if set.
    /// Allows particles to use textures from an atlas for better batching.
    /// </summary>
    public AtlasRegion? ParticleAtlasRegion { get; set; }
    
    /// <summary>
    /// Initial rotation of particles in radians (default: 0).
    /// </summary>
    public float InitialRotation { get; set; } = 0f;

    /// <summary>
    /// Random variation in initial rotation (0-1, adds randomness).
    /// Value of 1.0 means full 360° random rotation.
    /// </summary>
    public float InitialRotationVariation { get; set; } = 0f;

    /// <summary>
    /// Rotation speed in radians per second (default: 0 = no rotation).
    /// </summary>
    public float RotationSpeed { get; set; } = 0f;

    /// <summary>
    /// Random variation in rotation speed (0-1, adds randomness).
    /// </summary>
    public float RotationSpeedVariation { get; set; } = 0f;
    
    /// <summary>
    /// Whether to enable particle trails (default: false).
    /// Trails create a "motion blur" effect by drawing fading copies behind the particle.
    /// </summary>
    public bool EnableTrails { get; set; } = false;

    /// <summary>
    /// Number of trail segments to render behind each particle (default: 5).
    /// Higher values create longer trails but use more memory.
    /// </summary>
    public int TrailLength { get; set; } = 5;

    /// <summary>
    /// Color multiplier for trail start (at particle position).
    /// </summary>
    public float TrailStartAlpha { get; set; } = 0.8f;

    /// <summary>
    /// Color multiplier for trail end (furthest from particle).
    /// </summary>
    public float TrailEndAlpha { get; set; } = 0.0f;

    /// <summary>
    /// Blend mode for rendering particles. Use Additive for fire/explosions/lights.
    /// </summary>
    public BlendMode BlendMode { get; set; } = BlendMode.Alpha;

    /// <summary>
    /// Shape of the emitter spawn area.
    /// </summary>
    public EmitterShape Shape { get; set; } = EmitterShape.Circle;

    /// <summary>
    /// For Box shape: width and height. For Line shape: line length (X = length).
    /// </summary>
    public Vector2 ShapeSize { get; set; } = new Vector2(10f, 10f);

    /// <summary>
    /// For Cone shape: cone angle in degrees.
    /// </summary>
    public float ConeAngle { get; set; } = 30f;

    /// <summary>
    /// Active particles (managed by ParticleSystem).
    /// </summary>
    internal List<Particle> Particles { get; } = new();

    /// <summary>
    /// Gets a read-only view of the active particles.
    /// Use this to query particle state for gameplay logic.
    /// </summary>
    public IReadOnlyList<Particle> ActiveParticles => Particles;

    /// <summary>
    /// Gets the current number of active particles.
    /// </summary>
    public int ParticleCount => Particles.Count;

    /// <summary>
    /// Emission timer (managed by ParticleSystem).
    /// </summary>
    internal float EmissionTimer { get; set; }

    internal readonly ParticleEmitterState DefaultState;

    public ParticleEmitterComponent()
    {
        DefaultState = new ParticleEmitterState
        {
            IsEmitting = IsEmitting,
            EmissionRate = EmissionRate,
            MaxParticles = MaxParticles,
            ParticleLifetime = ParticleLifetime,
            LifetimeVariation = LifetimeVariation,
            StartColor = StartColor,
            EndColor = EndColor,
            StartSize = StartSize,
            EndSize = EndSize,
            InitialVelocity = InitialVelocity,
            VelocitySpread = VelocitySpread,
            SpeedVariation = SpeedVariation,
            Gravity = Gravity,
            SpawnOffset = SpawnOffset,
            SpawnRadius = SpawnRadius,
            ParticleTexture = ParticleTexture,
            ParticleAtlasRegion = ParticleAtlasRegion,
            InitialRotation = InitialRotation,
            InitialRotationVariation = InitialRotationVariation,
            RotationSpeed = RotationSpeed,
            RotationSpeedVariation = RotationSpeedVariation,
            EnableTrails = EnableTrails,
            TrailLength = TrailLength,
            TrailStartAlpha = TrailStartAlpha,
            TrailEndAlpha = TrailEndAlpha,
            BlendMode = BlendMode,
            Shape = Shape,
            ShapeSize = ShapeSize,
            ConeAngle = ConeAngle
        };
    }

    public void ResetToDefaultState()
    {
        IsEmitting = DefaultState.IsEmitting;
        EmissionRate = DefaultState.EmissionRate;
        MaxParticles = DefaultState.MaxParticles;
        ParticleLifetime = DefaultState.ParticleLifetime;
        LifetimeVariation = DefaultState.LifetimeVariation;
        StartColor = DefaultState.StartColor;
        EndColor = DefaultState.EndColor;
        StartSize = DefaultState.StartSize;
        EndSize = DefaultState.EndSize;
        InitialVelocity = DefaultState.InitialVelocity;
        VelocitySpread = DefaultState.VelocitySpread;
        SpeedVariation = DefaultState.SpeedVariation;
        Gravity = DefaultState.Gravity;
        SpawnOffset = DefaultState.SpawnOffset;
        SpawnRadius = DefaultState.SpawnRadius;
        ParticleTexture = DefaultState.ParticleTexture;
        ParticleAtlasRegion = DefaultState.ParticleAtlasRegion;
        InitialRotation = DefaultState.InitialRotation;
        InitialRotationVariation = DefaultState.InitialRotationVariation;
        RotationSpeed = DefaultState.RotationSpeed;
        RotationSpeedVariation = DefaultState.RotationSpeedVariation;
        EnableTrails = DefaultState.EnableTrails;
        TrailLength = DefaultState.TrailLength;
        TrailStartAlpha = DefaultState.TrailStartAlpha;
        TrailEndAlpha = DefaultState.TrailEndAlpha;
        BlendMode = DefaultState.BlendMode;
        Shape = DefaultState.Shape;
        ShapeSize = DefaultState.ShapeSize;
        ConeAngle = DefaultState.ConeAngle;
    }
}