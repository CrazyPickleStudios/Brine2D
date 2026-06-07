using Brine2D.Core;
using Brine2D.ECS.Components;
using Brine2D.Rendering;
using Brine2D.Rendering.TextureAtlas;
using System.Numerics;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// Describes a secondary burst of particles that <see cref="ParticleSystem"/> spawns at the
/// position where a parent particle dies or spawns. Assign one or more instances to
/// <see cref="ParticleEmitterComponent.DeathSubEmitters"/> or
/// <see cref="ParticleEmitterComponent.BirthSubEmitters"/>.
/// <para>
/// Each <see cref="SubEmitterConfig"/> is self-contained: it carries its own visual and
/// physics settings and does <em>not</em> require a separate entity or component in the ECS.
/// The <see cref="ParticleSystem"/> creates a transient <see cref="SubEmitterState"/>
/// internally and drives it for the lifetime of those sub-particles.
/// </para>
/// </summary>
public sealed class SubEmitterConfig
{
    /// <summary>Number of particles to burst at the trigger position.</summary>
    public int BurstCount { get; set; } = 10;

    public float ParticleLifetime { get; set; } = 0.5f;
    public float LifetimeVariation { get; set; } = 0.2f;

    public Color StartColor { get; set; } = Color.White;
    public Color EndColor { get; set; } = new Color(255, 255, 255, 0);
    public Color StartColorVariation { get; set; } = new Color(0, 0, 0, 0);
    public Color EndColorVariation { get; set; } = new Color(0, 0, 0, 0);
    public Color[]? ColorGradient { get; set; }

    public float StartSize { get; set; } = 3f;
    public float EndSize { get; set; } = 0f;
    public float SizeVariation { get; set; } = 0f;
    public float EndSizeVariation { get; set; } = 0f;

    /// <summary>
    /// Base velocity given to each sub-particle. <see cref="VelocitySpread"/> randomises the
    /// direction; a zero vector combined with a non-zero spread produces an omnidirectional burst.
    /// <para>
    /// <b>Always world-space.</b> Unlike <see cref="ParticleEmitterComponent.InitialVelocity"/>,
    /// this vector is never rotated by an entity's <see cref="TransformComponent.Rotation"/>
    /// because sub-emitters have no entity context. Sub-particles always spray in the configured
    /// world direction regardless of the orientation of the entity that owns the parent emitter.
    /// </para>
    /// </summary>
    public Vector2 InitialVelocity { get; set; } = Vector2.Zero;

    /// <summary>
    /// Fraction of the triggering particle's velocity added to each newly-spawned
    /// sub-particle's velocity at birth. 0 (default) means no inheritance. 1 means the
    /// sub-particle fully inherits the parent particle's velocity at the moment of the
    /// trigger (death, birth, or lifetime-fraction). Useful for sparks or debris that
    /// should carry the momentum of the particle that produced them.
    /// <para>
    /// <b>Note:</b> <see cref="InitialVelocity"/> is always in world space (no entity
    /// transform is applied to sub-emitters). The inherited velocity is added in world
    /// space on top of <see cref="InitialVelocity"/> after spread randomisation.
    /// </para>
    /// </summary>
    public float VelocityInheritance { get; set; } = 0f;

    /// <summary>Random angular spread in <b>degrees</b> applied to <see cref="InitialVelocity"/>.</summary>
    public float VelocitySpread { get; set; } = 360f;

    public float SpeedVariation { get; set; } = 0.5f;
    public Vector2 Gravity { get; set; } = new Vector2(0, 100);
    public float Damping { get; set; } = 0f;

    /// <summary>
    /// Multiplier applied to the spawn-speed of each sub-particle at <c>t = 0</c> (birth).
    /// Linearly interpolated toward <see cref="EndSpeedMultiplier"/> over the sub-particle's
    /// lifetime. Default 1 (no change).
    /// <para>
    /// When either multiplier differs from 1, speed-over-lifetime is active and takes
    /// priority over <see cref="Damping"/> for the non-gravity component of velocity.
    /// </para>
    /// </summary>
    public float StartSpeedMultiplier { get; set; } = 1f;

    /// <summary>
    /// Multiplier applied to the spawn-speed of each sub-particle at <c>t = 1</c> (death).
    /// Default 1 (no change). Set to 0 to decelerate sub-particles to a halt over their lifetime.
    /// </summary>
    public float EndSpeedMultiplier { get; set; } = 1f;

    public ITexture? ParticleTexture { get; set; }
    public AtlasRegion? ParticleAtlasRegion { get; set; }

    /// <summary>
    /// An ordered array of atlas regions used as animation frames over the sub-particle's
    /// lifetime. When set, takes priority over <see cref="ParticleAtlasRegion"/>.
    /// Frames are distributed evenly across the lifetime (frame 0 at birth, last frame at death).
    /// </summary>
    public AtlasRegion[]? ParticleFrames { get; set; }

    public float InitialRotation { get; set; } = 0f;
    public float InitialRotationVariation { get; set; } = MathF.PI;
    public float RotationSpeed { get; set; } = 0f;
    public float RotationSpeedVariation { get; set; } = 0f;

    public BlendMode BlendMode { get; set; } = BlendMode.Alpha;
    public int RenderLayer { get; set; } = 0;

    /// <summary>
    /// Hard cap on the total number of live sub-particles across <em>all</em> active
    /// <see cref="SubEmitterState"/> entries that share this config instance. When the cap
    /// is reached, new bursts for this config are skipped until existing sub-particles expire.
    /// Prevents runaway particle counts when the parent emitter's death rate is very high.
    /// <para>
    /// <b>Important:</b> the cap is enforced by reference equality on the config instance.
    /// If the same <see cref="SubEmitterConfig"/> object is assigned to multiple parent
    /// emitters (e.g. both their <see cref="ParticleEmitterComponent.BirthSubEmitters"/> and
    /// <see cref="ParticleEmitterComponent.DeathSubEmitters"/> lists, or across different
    /// entities), all of those emitters share this single cap. Bursts from any one of them
    /// count against the limit and can starve the others. Create a separate
    /// <see cref="SubEmitterConfig"/> instance per parent emitter if independent caps are needed.
    /// </para>
    /// </summary>
    public int MaxParticles { get; set; } = 200;

    /// <summary>
    /// Shape of the area from which sub-particles are scattered relative to the trigger
    /// position. Defaults to <see cref="EmitterShape.Point"/> (all particles spawn at the
    /// exact trigger position). Use <see cref="EmitterShape.Circle"/> with
    /// <see cref="SpawnRadius"/> to scatter particles outward from an impact area.
    /// </summary>
    public EmitterShape Shape { get; set; } = EmitterShape.Point;

    /// <summary>
    /// Radius in world units used when <see cref="Shape"/> is <see cref="EmitterShape.Circle"/>
    /// or <see cref="EmitterShape.Cone"/>. Has no effect for other shapes.
    /// </summary>
    public float SpawnRadius { get; set; } = 0f;

    /// <summary>
    /// Width and height of the spawn area when <see cref="Shape"/> is
    /// <see cref="EmitterShape.Box"/>. Has no effect for other shapes.
    /// </summary>
    public Vector2 ShapeSize { get; set; } = new Vector2(10f, 10f);

    /// <summary>
    /// When true and <see cref="Shape"/> is <see cref="EmitterShape.Circle"/> or
    /// <see cref="EmitterShape.Cone"/>, particles spawn exactly on the perimeter of the
    /// circle rather than distributed within it. Useful for ring bursts.
    /// </summary>
    public bool SpawnOnPerimeter { get; set; } = false;

    /// <summary>
    /// Full cone spread angle in <b>degrees</b> when <see cref="Shape"/> is
    /// <see cref="EmitterShape.Cone"/>. Each particle's direction is randomised within
    /// ±ConeAngle/2 of <see cref="InitialVelocity"/>.
    /// </summary>
    public float ConeAngle { get; set; } = 30f;

    /// <summary>
    /// Rotation angle in <b>radians</b> for the spawn line when <see cref="Shape"/> is
    /// <see cref="EmitterShape.Line"/>. 0 = horizontal, MathF.PI / 2 = vertical.
    /// </summary>
    public float LineAngle { get; set; } = 0f;

    /// <summary>
    /// Length of the spawn segment in world units when <see cref="Shape"/> is
    /// <see cref="EmitterShape.Line"/>. When 0 (default), falls back to
    /// <see cref="ShapeSize"/>.X.
    /// </summary>
    public float LineLength { get; set; } = 0f;

    /// <summary>
    /// Rotation angle in <b>radians</b> applied to the spawn box when <see cref="Shape"/>
    /// is <see cref="EmitterShape.Box"/>. 0 (default) = axis-aligned.
    /// </summary>
    public float BoxAngle { get; set; } = 0f;

    /// <summary>
    /// Strength of per-particle velocity noise applied every frame using coherent value
    /// noise, frame-rate independently. 0 (default) disables turbulence.
    /// </summary>
    public float TurbulenceStrength { get; set; } = 0f;

    /// <summary>
    /// Spatial frequency of the turbulence noise field. Higher values produce tighter,
    /// higher-frequency swirls. Default is 0.02 (world-unit scale).
    /// </summary>
    public float TurbulenceFrequency { get; set; } = 0.02f;

    /// <summary>
    /// When true, sub-particles render a positional trail behind them.
    /// Requires <see cref="TrailLength"/> &gt; 0.
    /// </summary>
    public bool EnableTrails { get; set; } = false;

    /// <summary>Number of trail history slots. Only used when <see cref="EnableTrails"/> is true.</summary>
    public int TrailLength { get; set; } = 5;

    /// <summary>
    /// Alpha of the trail segment nearest to the particle head (t = 1, newest position).
    /// </summary>
    public float TrailHeadAlpha { get; set; } = 0.8f;

    /// <summary>
    /// Alpha of the trail segment farthest from the particle head (t = 0, oldest position).
    /// </summary>
    public float TrailTailAlpha { get; set; } = 0.0f;

    /// <summary>
    /// Size multiplier for the trail segment farthest from the particle head (t = 0, oldest position).
    /// </summary>
    public float TrailTailSizeRatio { get; set; } = 0.5f;

    /// <summary>
    /// Size multiplier for the trail segment nearest to the particle head (t = 1, newest position).
    /// </summary>
    public float TrailHeadSizeRatio { get; set; } = 1.0f;

    /// <summary>
    /// Controls how the trail history is rendered. Defaults to <see cref="TrailMode.Sprites"/>.
    /// Use <see cref="TrailMode.Lines"/> for a continuous ribbon on untextured particles.
    /// </summary>
    public TrailMode TrailMode { get; set; } = TrailMode.Sprites;

    /// <summary>
    /// Optional list of extra forces (attractors, wind zones, etc.) applied to every live
    /// sub-particle during the update pass. Forces are evaluated in order and their results
    /// are summed into <c>BaseVelocity</c>, making them subject to subsequent damping and
    /// speed-over-lifetime scaling.
    /// </summary>
    public List<IParticleForce>? Forces { get; set; }

    /// <summary>
    /// Optional callback invoked immediately after a sub-particle is spawned and added to
    /// the active list. Do not hold a reference to the particle beyond the callback.
    /// </summary>
    public Action<Particle>? OnParticleSpawned { get; set; }

    /// <summary>
    /// Optional callback invoked when a sub-particle expires naturally. The particle is
    /// passed with its final state and is returned to the pool immediately after the
    /// callback returns. Do not hold a reference to the particle.
    /// </summary>
    public Action<Particle>? OnParticleDied { get; set; }
}