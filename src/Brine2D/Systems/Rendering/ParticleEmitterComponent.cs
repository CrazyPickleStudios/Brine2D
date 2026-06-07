using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Rendering;
using Brine2D.Rendering.TextureAtlas;
using System.Numerics;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// Component for particle emission and rendering.
/// </summary>
public class ParticleEmitterComponent : Component
{
    public bool IsEmitting { get; set; } = true;
    public float EmissionRate { get; set; } = 10f;
    public int MaxParticles { get; set; } = 100;
    public float ParticleLifetime { get; set; } = 2f;
    public float LifetimeVariation { get; set; } = 0.3f;
    public Color StartColor { get; set; } = Color.White;
    public Color EndColor { get; set; } = new Color(255, 255, 255, 0);

    /// <summary>
    /// Per-particle additive randomisation applied to each channel of <see cref="StartColor"/>
    /// independently. Each channel is nudged by a random value in [-V, +V] where V is the
    /// corresponding channel of this color, then clamped to [0, 255].
    /// </summary>
    public Color StartColorVariation { get; set; } = new Color(0, 0, 0, 0);

    /// <summary>
    /// Per-particle additive randomisation applied to each channel of <see cref="EndColor"/>
    /// independently. Each channel is nudged by a random value in [-V, +V] where V is the
    /// corresponding channel of this color, then clamped to [0, 255].
    /// </summary>
    public Color EndColorVariation { get; set; } = new Color(0, 0, 0, 0);

    /// <summary>
    /// Optional multi-stop color gradient sampled over a particle's lifetime (t = 0..1).
    /// When set, takes priority over <see cref="StartColor"/> and <see cref="EndColor"/>.
    /// Stops are evenly distributed; supply at least two entries.
    /// Note: per-particle <see cref="StartColorVariation"/> / <see cref="EndColorVariation"/>
    /// are not applied when a gradient is active.
    /// </summary>
    public Color[]? ColorGradient { get; set; }

    public float StartSize { get; set; } = 4f;
    public float EndSize { get; set; } = 0f;

    /// <summary>
    /// Per-particle randomisation of <see cref="StartSize"/>. The spawned size is nudged by
    /// a random value in [-SizeVariation, +SizeVariation], clamped to &gt;= 0.
    /// </summary>
    public float SizeVariation { get; set; } = 0f;

    /// <summary>
    /// Per-particle randomisation of <see cref="EndSize"/>. The end size is nudged by
    /// a random value in [-EndSizeVariation, +EndSizeVariation], clamped to &gt;= 0.
    /// </summary>
    public float EndSizeVariation { get; set; } = 0f;

    public Vector2 InitialVelocity { get; set; } = new Vector2(0, -50);

    /// <summary>
    /// Random angular spread in <b>degrees</b> applied to <see cref="InitialVelocity"/> at spawn.
    /// Each particle's direction is nudged by a random value in [-VelocitySpread/2, +VelocitySpread/2] degrees.
    /// <para>
    /// Note: when <see cref="Shape"/> is <see cref="EmitterShape.Cone"/>, <see cref="ConeAngle"/> is
    /// used instead and this property has no effect.
    /// </para>
    /// </summary>
    public float VelocitySpread { get; set; } = 45f;

    public float SpeedVariation { get; set; } = 0.5f;

    /// <summary>
    /// Fraction of the emitter entity's instantaneous velocity (units/second) added to each
    /// newly-spawned particle's velocity at birth. 0 (default) means no inheritance. 1 means
    /// full inheritance. Only applied when the emitter has a <see cref="TransformComponent"/>.
    /// <para>
    /// When <see cref="SimulateInLocalSpace"/> is true the inherited velocity is still computed
    /// from the entity's world-space motion so particles correctly carry the entity's momentum.
    /// </para>
    /// </summary>
    public float VelocityInheritance { get; set; } = 0f;

    /// <summary>
    /// Exponential drag coefficient applied each second via <c>velocity *= exp(-Damping * dt)</c>.
    /// Frame-rate independent; a value of ~0.693 halves speed each second.
    /// 0 (default) means no drag. Gravity is applied after damping.
    /// <para>
    /// Note: when <see cref="StartSpeedMultiplier"/> / <see cref="EndSpeedMultiplier"/> are
    /// active they control the magnitude of the base velocity component independently; combining
    /// both features is supported but the multiplier curve takes priority over damping for the
    /// non-gravity component.
    /// </para>
    /// </summary>
    public float Damping { get; set; } = 0f;

    /// <summary>
    /// Multiplier applied to the spawn-speed of each particle at <c>t = 0</c> (birth).
    /// Linearly interpolated toward <see cref="EndSpeedMultiplier"/> over the particle's
    /// lifetime. Default 1 (no change).
    /// <para>
    /// When either multiplier differs from 1, speed-over-lifetime is active and takes
    /// priority over <see cref="Damping"/> for the non-gravity component of velocity.
    /// </para>
    /// </summary>
    public float StartSpeedMultiplier { get; set; } = 1f;

    /// <summary>
    /// Multiplier applied to the spawn-speed of each particle at <c>t = 1</c> (death).
    /// Default 1 (no change). Set to 0 to decelerate particles to a halt over their lifetime.
    /// </summary>
    public float EndSpeedMultiplier { get; set; } = 1f;

    public Vector2 Gravity { get; set; } = new Vector2(0, 100);
    public Vector2 SpawnOffset { get; set; } = Vector2.Zero;
    public float SpawnRadius { get; set; } = 0f;
    public ITexture? ParticleTexture { get; set; }
    public AtlasRegion? ParticleAtlasRegion { get; set; }

    /// <summary>
    /// An ordered array of atlas regions used as animation frames over the particle's lifetime.
    /// When set, takes priority over <see cref="ParticleAtlasRegion"/>.
    /// Frames are distributed evenly across the lifetime (frame 0 at birth, last frame at death).
    /// </summary>
    public AtlasRegion[]? ParticleFrames { get; set; }

    public float InitialRotation { get; set; } = 0f;

    /// <summary>
    /// Absolute random range in <b>radians</b> added to <see cref="InitialRotation"/> at spawn.
    /// Each particle gets a value in [-InitialRotationVariation, +InitialRotationVariation].
    /// Use <c>MathF.PI</c> for a fully randomised initial rotation across the whole circle.
    /// </summary>
    public float InitialRotationVariation { get; set; } = 0f;

    /// <summary>Rotation speed in <b>radians per second</b>.</summary>
    public float RotationSpeed { get; set; } = 0f;

    /// <summary>
    /// Absolute random range (<b>radians/sec</b>) added to <see cref="RotationSpeed"/> at spawn.
    /// Each particle gets a value in [-RotationSpeedVariation, +RotationSpeedVariation].
    /// This is an additive range, so it works even when RotationSpeed is zero.
    /// </summary>
    public float RotationSpeedVariation { get; set; } = 0f;

    public bool EnableTrails { get; set; } = false;
    public int TrailLength { get; set; } = 5;

    /// <summary>
    /// Alpha of the trail segment nearest to the particle head (t = 1, newest position).
    /// Defaults to 0.8 so the head end of the trail is nearly opaque.
    /// </summary>
    public float TrailHeadAlpha { get; set; } = 0.8f;

    /// <summary>
    /// Alpha of the trail segment farthest from the particle head (t = 0, oldest position).
    /// Defaults to 0.0 so the tail fades to transparent.
    /// </summary>
    public float TrailTailAlpha { get; set; } = 0.0f;

    /// <summary>
    /// Size multiplier for the trail segment farthest from the particle head (t = 0, oldest position).
    /// Defaults to 0.5 so the tail is half the particle's current size.
    /// </summary>
    public float TrailTailSizeRatio { get; set; } = 0.5f;

    /// <summary>
    /// Size multiplier for the trail segment nearest to the particle head (t = 1, newest position).
    /// Defaults to 1.0 so the head-side segment matches the particle's current size.
    /// </summary>
    public float TrailHeadSizeRatio { get; set; } = 1.0f;

    /// <summary>
    /// Controls how the trail history is rendered. Defaults to <see cref="TrailMode.Sprites"/>.
    /// Use <see cref="TrailMode.Lines"/> for a continuous ribbon on untextured particles.
    /// </summary>
    public TrailMode TrailMode { get; set; } = TrailMode.Sprites;

    public BlendMode BlendMode { get; set; } = BlendMode.Alpha;
    public EmitterShape Shape { get; set; } = EmitterShape.Circle;
    public Vector2 ShapeSize { get; set; } = new Vector2(10f, 10f);

    /// <summary>
    /// Rotation angle in <b>radians</b> for the <see cref="EmitterShape.Line"/> shape.
    /// 0 = horizontal, MathF.PI / 2 = vertical.
    /// </summary>
    public float LineAngle { get; set; } = 0f;

    /// <summary>
    /// Length of the <see cref="EmitterShape.Line"/> spawn segment in world units.
    /// When set to a positive value, takes priority over <see cref="ShapeSize"/>.X for line
    /// emitters, giving a more readable API. Leave at 0 (default) to use <see cref="ShapeSize"/>.X.
    /// </summary>
    public float LineLength { get; set; } = 0f;

    /// <summary>
    /// Rotation angle in <b>radians</b> applied to the <see cref="EmitterShape.Box"/> spawn area.
    /// Rotates the box around its centre so spawn offsets follow the box orientation.
    /// 0 (default) = axis-aligned.
    /// </summary>
    public float BoxAngle { get; set; } = 0f;

    /// <summary>
    /// Full cone spread angle in <b>degrees</b> for <see cref="EmitterShape.Cone"/> emitters.
    /// Each particle's direction is randomised within ±ConeAngle/2 of <see cref="InitialVelocity"/>.
    /// </summary>
    public float ConeAngle { get; set; } = 30f;

    /// <summary>
    /// When true, particles for <see cref="EmitterShape.Circle"/> and <see cref="EmitterShape.Cone"/>
    /// spawn exactly on the perimeter of the circle (radius = <see cref="SpawnRadius"/>) rather than
    /// uniformly distributed within it. Useful for ring bursts and shockwave effects.
    /// </summary>
    public bool SpawnOnPerimeter { get; set; } = false;

    /// <summary>
    /// When true, particles are simulated in local space relative to the emitter entity.
    /// Moving the entity moves all live particles with it. Useful for exhaust trails,
    /// auras, and effects that should remain attached to a moving object.
    /// When false (default), particles are simulated in world space and drift freely
    /// after spawn.
    /// <para>
    /// <b>Velocity direction:</b> when local-space simulation is active,
    /// <see cref="InitialVelocity"/> is treated as a local-space direction — it is
    /// <em>not</em> rotated by <see cref="TransformComponent.Rotation"/> at spawn time.
    /// This means particles always travel in a fixed direction relative to the entity's
    /// local axes. If you need velocity to follow the entity's world rotation instead,
    /// set <see cref="SimulateInLocalSpace"/> to false and use
    /// <see cref="VelocityInheritance"/> to carry the entity's momentum.
    /// </para>
    /// <para>
    /// <b>Note:</b> if the entity's <see cref="TransformComponent.Scale"/> changes after
    /// particles have already spawned, all live particle positions will appear to stretch or
    /// compress relative to the entity origin, because positions are multiplied by
    /// <c>localScale</c> each render frame. Avoid animating scale on entities that use local-space
    /// particle simulation with live particles.
    /// </para>
    /// </summary>
    public bool SimulateInLocalSpace { get; set; } = false;

    /// <summary>
    /// When true, emits <see cref="BurstCount"/> particles in a single frame then stops.
    /// The emitter disables itself automatically once all burst particles have expired,
    /// unless <see cref="Loop"/> is also true, in which case it re-arms and fires again.
    /// </summary>
    public bool IsBurst { get; set; } = false;

    /// <summary>
    /// Number of particles to emit in a single burst. Only used when <see cref="IsBurst"/> is true.
    /// </summary>
    public int BurstCount { get; set; } = 30;

    /// <summary>
    /// Optional duration in seconds for continuous emitters. When set, the emitter automatically
    /// stops emitting after this many seconds have elapsed. Does not affect burst emitters.
    /// Set to null (default) for an infinitely running emitter.
    /// </summary>
    public float? Duration { get; set; } = null;

    /// <summary>
    /// When true and <see cref="Duration"/> is set, the emitter automatically restarts its
    /// emission cycle once all particles from the previous cycle have expired. The delay is
    /// re-applied on each loop iteration. <see cref="OnEmitterFinished"/> does not fire while
    /// looping. Also works with burst emitters (<see cref="IsBurst"/> = true): the burst
    /// re-arms and fires again after all particles from the previous burst have expired.
    /// </summary>
    public bool Loop { get; set; } = false;

    /// <summary>
    /// Delay in seconds before emission begins after the emitter is first activated or
    /// restarted after <see cref="Stop"/>. 0 (default) means emit immediately.
    /// The delay is re-applied on each loop iteration when <see cref="Loop"/> is true.
    /// </summary>
    public float Delay { get; set; } = 0f;

    /// <summary>
    /// Pre-simulation duration in seconds applied on the first <see cref="ParticleSystem"/>
    /// update after the emitter becomes active. Use this to make looping ambient effects
    /// appear already running when they first appear on screen. 0 (default) disables warmup.
    /// </summary>
    public float WarmupDuration { get; set; } = 0f;

    /// <summary>
    /// Strength of per-particle velocity noise applied every frame using coherent value
    /// noise sampled from particle position and elapsed time, scaled so the effect is
    /// frame-rate independent. 0 (default) disables turbulence.
    /// <para>
    /// Turbulence is additive — it does not replace the base velocity or interact with
    /// <see cref="Damping"/> or <see cref="StartSpeedMultiplier"/>/<see cref="EndSpeedMultiplier"/>.
    /// </para>
    /// </summary>
    public float TurbulenceStrength { get; set; } = 0f;

    /// <summary>
    /// Spatial frequency of the turbulence noise field. Higher values produce tighter,
    /// higher-frequency swirls. Default is 0.02 (world-unit scale).
    /// </summary>
    public float TurbulenceFrequency { get; set; } = 0.02f;

    /// <summary>
    /// Optional list of extra forces (attractors, wind zones, etc.) applied to every live
    /// particle during the update pass. Forces are evaluated in order and their results are
    /// summed into <c>BaseVelocity</c>, making them subject to subsequent damping and
    /// speed-over-lifetime scaling.
    /// <para>
    /// This list is not captured by <see cref="CaptureDefaultState"/> because force objects
    /// are typically configured once and shared across resets.
    /// </para>
    /// </summary>
    public List<IParticleForce>? Forces { get; set; }

    /// <summary>
    /// Optional list of sub-emitter configs triggered when a particle from this emitter
    /// <b>spawns</b>. For each config a burst of sub-particles is spawned at the new
    /// particle's world position. Useful for spawn-impact effects such as a smoke puff or
    /// splash that appears at the point of materialisation.
    /// <para>
    /// Sub-particles are managed internally by <see cref="ParticleSystem"/> and do not
    /// require additional ECS entities or components.
    /// Sub-particles do not chain — sub-emitters on sub-emitters are not supported.
    /// </para>
    /// <para>
    /// This list is not captured by <see cref="CaptureDefaultState"/> because configs are
    /// typically set once at design time and are not expected to change between resets.
    /// </para>
    /// </summary>
    public List<SubEmitterConfig>? BirthSubEmitters { get; set; }

    /// <summary>
    /// Optional list of sub-emitter configs that are triggered when a particle from this
    /// emitter expires naturally. For each config a burst of sub-particles is spawned at the
    /// dying particle's world position. The sub-particles are managed internally by
    /// <see cref="ParticleSystem"/> and do not require additional ECS entities or components.
    /// <para>
    /// Sub-particles do not chain — sub-emitters on sub-emitters are not supported.
    /// </para>
    /// <para>
    /// This list is not captured by <see cref="CaptureDefaultState"/> because configs are
    /// typically set once at design time and are not expected to change between resets.
    /// </para>
    /// </summary>
    public List<SubEmitterConfig>? DeathSubEmitters { get; set; }

    /// <summary>
    /// Optional list of lifetime-fraction triggers. Each entry fires a sub-emitter burst
    /// once per particle when the particle's age crosses the specified normalised lifetime
    /// fraction (0 = birth, 1 = death). Useful for mid-flight effects such as a rocket
    /// igniting a secondary thruster at half its life.
    /// <para>
    /// Fractions are evaluated every update frame; if a large delta-time steps past a
    /// threshold in a single frame the trigger still fires exactly once for that particle.
    /// </para>
    /// <para>
    /// Sub-particles do not chain — sub-emitters on sub-emitters are not supported.
    /// This list is not captured by <see cref="CaptureDefaultState"/>.
    /// </para>
    /// </summary>
    public List<LifetimeFractionSubEmitter>? LifetimeFractionSubEmitters { get; set; }

    /// <summary>
    /// When true, all particle aging, movement, and new emission are frozen.
    /// Call <see cref="Resume"/> to continue.
    /// </summary>
    public bool IsPaused { get; private set; } = false;

    /// <summary>
    /// Draw order for this emitter relative to other emitters processed by the same
    /// <see cref="ParticleSystem"/>. Lower values are rendered first (further back).
    /// Emitters with the same layer are rendered in entity-creation order.
    /// </summary>
    public int RenderLayer { get; set; } = 0;

    /// <summary>
    /// Optional callback invoked by <see cref="ParticleSystem"/> when a particle expires naturally.
    /// The particle is passed with its final state and is returned to the pool immediately
    /// after the callback returns. Do not hold a reference to the particle.
    /// Not invoked when <see cref="Stop"/> force-clears particles.
    /// This property is not captured by <see cref="CaptureDefaultState"/>.
    /// </summary>
    public Action<Particle>? OnParticleDied { get; set; }

    /// <summary>
    /// Optional callback invoked by <see cref="ParticleSystem"/> immediately after a particle is
    /// spawned and added to the active list. Do not hold a reference to the particle beyond the callback.
    /// This property is not captured by <see cref="CaptureDefaultState"/>.
    /// </summary>
    public Action<Particle>? OnParticleSpawned { get; set; }

    /// <summary>
    /// Optional callback invoked once by <see cref="ParticleSystem"/> when the emitter finishes:
    /// either when a burst emitter's last particle expires (and <see cref="Loop"/> is false),
    /// or when a <see cref="Duration"/>-limited continuous emitter's last particle expires after
    /// emission has stopped (and <see cref="Loop"/> is false). Not invoked by <see cref="Stop"/>
    /// or <see cref="ResetToDefaultState"/>.
    /// </summary>
    public Action? OnEmitterFinished { get; set; }

    internal bool BurstFired { get; set; } = false;

    /// <summary>
    /// When true, the next <see cref="ParticleSystem"/> update will return all live particles
    /// to the pool and clear the list. Set by <see cref="Stop"/> and <see cref="ResetToDefaultState"/>.
    /// </summary>
    internal bool StopRequested { get; private set; } = false;

    internal bool WarmupApplied { get; set; } = false;

    internal bool DurationElapsed { get; set; }

    /// <summary>
    /// Elapsed time in seconds counting toward <see cref="Delay"/>. When this reaches
    /// <see cref="Delay"/> the emitter begins emitting normally. Reset by <see cref="Stop"/>
    /// and <see cref="ResetToDefaultState"/>.
    /// </summary>
    internal float DelayTimer { get; set; } = 0f;

    internal Vector2? PreviousPosition { get; set; }

    internal List<Particle> Particles { get; } = new();

    public IReadOnlyList<Particle> ActiveParticles => Particles;

    public int ParticleCount => Particles.Count;

    internal float EmissionTimer { get; set; }

    internal float DurationTimer { get; set; }

    internal ParticleEmitterState? DefaultState { get; private set; }

    /// <summary>
    /// Registered by <see cref="ParticleSystem"/> the first time this emitter is encountered.
    /// Invoked by <see cref="OnRemoved"/> to return all live particles to the pool before the
    /// component is detached, preventing a pool leak when the entity is destroyed.
    /// </summary>
    internal Action? CleanupForPool { get; set; }

    protected internal override void OnRemoved()
    {
        CleanupForPool?.Invoke();
        CleanupForPool = null;
    }

    /// <summary>
    /// Captures the current configuration as the default state that
    /// <see cref="ResetToDefaultState"/> will restore. Call this once you
    /// have finished configuring the emitter. <see cref="IsPaused"/> is intentionally
    /// excluded — reset always resumes the emitter.
    /// <para>
    /// <see cref="ColorGradient"/> and <see cref="ParticleFrames"/> arrays are deep-copied
    /// so subsequent in-place mutations of those arrays do not silently corrupt the captured
    /// state. Reassigning the property to a new array is always safe regardless.
    /// </para>
    /// </summary>
    public void CaptureDefaultState()
    {
        DefaultState = new ParticleEmitterState
        {
            IsEmitting = IsEmitting,
            IsEnabled = IsEnabled,
            EmissionRate = EmissionRate,
            MaxParticles = MaxParticles,
            ParticleLifetime = ParticleLifetime,
            LifetimeVariation = LifetimeVariation,
            StartColor = StartColor,
            EndColor = EndColor,
            StartColorVariation = StartColorVariation,
            EndColorVariation = EndColorVariation,
            ColorGradient = ColorGradient?.ToArray(),
            StartSize = StartSize,
            EndSize = EndSize,
            SizeVariation = SizeVariation,
            EndSizeVariation = EndSizeVariation,
            InitialVelocity = InitialVelocity,
            VelocitySpread = VelocitySpread,
            SpeedVariation = SpeedVariation,
            VelocityInheritance = VelocityInheritance,
            Damping = Damping,
            Gravity = Gravity,
            SpawnOffset = SpawnOffset,
            SpawnRadius = SpawnRadius,
            ParticleTexture = ParticleTexture,
            ParticleAtlasRegion = ParticleAtlasRegion,
            ParticleFrames = ParticleFrames?.ToArray(),
            InitialRotation = InitialRotation,
            InitialRotationVariation = InitialRotationVariation,
            RotationSpeed = RotationSpeed,
            RotationSpeedVariation = RotationSpeedVariation,
            EnableTrails = EnableTrails,
            TrailLength = TrailLength,
            TrailHeadAlpha = TrailHeadAlpha,
            TrailTailAlpha = TrailTailAlpha,
            TrailTailSizeRatio = TrailTailSizeRatio,
            TrailHeadSizeRatio = TrailHeadSizeRatio,
            TrailMode = TrailMode,
            BlendMode = BlendMode,
            Shape = Shape,
            ShapeSize = ShapeSize,
            LineAngle = LineAngle,
            LineLength = LineLength,
            BoxAngle = BoxAngle,
            ConeAngle = ConeAngle,
            SpawnOnPerimeter = SpawnOnPerimeter,
            SimulateInLocalSpace = SimulateInLocalSpace,
            IsBurst = IsBurst,
            BurstCount = BurstCount,
            Duration = Duration,
            WarmupDuration = WarmupDuration,
            RenderLayer = RenderLayer,
            Delay = Delay,
            Loop = Loop,
            StartSpeedMultiplier = StartSpeedMultiplier,
            EndSpeedMultiplier = EndSpeedMultiplier,
            TurbulenceStrength = TurbulenceStrength,
            TurbulenceFrequency = TurbulenceFrequency,
        };
    }

    /// <summary>
    /// Re-arms the burst so it fires again on the next update.
    /// Has no effect on continuous emitters.
    /// </summary>
    public void ResetBurst()
    {
        BurstFired = false;
        IsEmitting = true;
        IsEnabled = true;
        WarmupApplied = false;
    }

    /// <summary>
    /// Stops emission and schedules all live particles to be returned to the pool on the
    /// next <see cref="ParticleSystem"/> update. <see cref="ParticleCount"/> reaches zero
    /// after that update. Also resumes the emitter if it was paused.
    /// </summary>
    public void Stop()
    {
        IsEmitting = false;
        IsPaused = false;
        BurstFired = false;
        EmissionTimer = 0f;
        DurationTimer = 0f;
        DurationElapsed = false;
        WarmupApplied = false;
        DelayTimer = 0f;
        PreviousPosition = null;
        StopRequested = true;
    }

    /// <summary>
    /// Starts or restarts the emitter from a clean state. Re-enables the emitter, begins
    /// emission, resumes if paused, re-arms the burst, and resets all timers.
    /// Any pending <see cref="Stop"/> is cancelled so live particles are not cleared.
    /// </summary>
    public void Play()
    {
        IsEnabled = true;
        IsEmitting = true;
        IsPaused = false;
        BurstFired = false;
        EmissionTimer = 0f;
        DurationTimer = 0f;
        DurationElapsed = false;
        WarmupApplied = false;
        DelayTimer = 0f;
        PreviousPosition = null;
        StopRequested = false;
    }

    /// <summary>
    /// Freezes all particle aging, movement, and new emission.
    /// </summary>
    public void Pause() => IsPaused = true;

    /// <summary>
    /// Resumes a previously paused emitter.
    /// </summary>
    public void Resume() => IsPaused = false;

    /// <summary>
    /// Restores the configuration that was captured by the last call to
    /// <see cref="CaptureDefaultState"/>. Throws if <see cref="CaptureDefaultState"/>
    /// has never been called. Live particles are scheduled for pool return on the next
    /// system update. <see cref="IsPaused"/> is always reset to <c>false</c>.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when <see cref="CaptureDefaultState"/> has never been called on this emitter.
    /// </exception>
    public void ResetToDefaultState()
    {
        if (DefaultState is null)
            throw new InvalidOperationException(
                "CaptureDefaultState() must be called before ResetToDefaultState().");

        StopRequested = true;

        IsEmitting = DefaultState.IsEmitting;
        IsEnabled = DefaultState.IsEnabled;
        EmissionRate = DefaultState.EmissionRate;
        MaxParticles = DefaultState.MaxParticles;
        ParticleLifetime = DefaultState.ParticleLifetime;
        LifetimeVariation = DefaultState.LifetimeVariation;
        StartColor = DefaultState.StartColor;
        EndColor = DefaultState.EndColor;
        StartColorVariation = DefaultState.StartColorVariation;
        EndColorVariation = DefaultState.EndColorVariation;
        ColorGradient = DefaultState.ColorGradient;
        StartSize = DefaultState.StartSize;
        EndSize = DefaultState.EndSize;
        SizeVariation = DefaultState.SizeVariation;
        EndSizeVariation = DefaultState.EndSizeVariation;
        InitialVelocity = DefaultState.InitialVelocity;
        VelocitySpread = DefaultState.VelocitySpread;
        SpeedVariation = DefaultState.SpeedVariation;
        VelocityInheritance = DefaultState.VelocityInheritance;
        Damping = DefaultState.Damping;
        Gravity = DefaultState.Gravity;
        SpawnOffset = DefaultState.SpawnOffset;
        SpawnRadius = DefaultState.SpawnRadius;
        ParticleTexture = DefaultState.ParticleTexture;
        ParticleAtlasRegion = DefaultState.ParticleAtlasRegion;
        ParticleFrames = DefaultState.ParticleFrames;
        InitialRotation = DefaultState.InitialRotation;
        InitialRotationVariation = DefaultState.InitialRotationVariation;
        RotationSpeed = DefaultState.RotationSpeed;
        RotationSpeedVariation = DefaultState.RotationSpeedVariation;
        EnableTrails = DefaultState.EnableTrails;
        TrailLength = DefaultState.TrailLength;
        TrailHeadAlpha = DefaultState.TrailHeadAlpha;
        TrailTailAlpha = DefaultState.TrailTailAlpha;
        TrailTailSizeRatio = DefaultState.TrailTailSizeRatio;
        TrailHeadSizeRatio = DefaultState.TrailHeadSizeRatio;
        TrailMode = DefaultState.TrailMode;
        BlendMode = DefaultState.BlendMode;
        Shape = DefaultState.Shape;
        ShapeSize = DefaultState.ShapeSize;
        LineAngle = DefaultState.LineAngle;
        LineLength = DefaultState.LineLength;
        BoxAngle = DefaultState.BoxAngle;
        ConeAngle = DefaultState.ConeAngle;
        SpawnOnPerimeter = DefaultState.SpawnOnPerimeter;
        SimulateInLocalSpace = DefaultState.SimulateInLocalSpace;
        IsBurst = DefaultState.IsBurst;
        BurstCount = DefaultState.BurstCount;
        Duration = DefaultState.Duration;
        WarmupDuration = DefaultState.WarmupDuration;
        RenderLayer = DefaultState.RenderLayer;
        Delay = DefaultState.Delay;
        Loop = DefaultState.Loop;
        StartSpeedMultiplier = DefaultState.StartSpeedMultiplier;
        EndSpeedMultiplier = DefaultState.EndSpeedMultiplier;
        TurbulenceStrength = DefaultState.TurbulenceStrength;
        TurbulenceFrequency = DefaultState.TurbulenceFrequency;
        IsPaused = false;
        BurstFired = false;
        EmissionTimer = 0f;
        DurationTimer = 0f;
        DurationElapsed = false;
        WarmupApplied = false;
        DelayTimer = 0f;
        PreviousPosition = null;
    }

    /// <summary>
    /// Attempts to restore the default state if it has been captured, otherwise does nothing.
    /// Prefer <see cref="ResetToDefaultState"/> when you expect a captured state to always exist.
    /// </summary>
    public bool TryResetToDefaultState()
    {
        if (DefaultState is null)
            return false;

        ResetToDefaultState();
        return true;
    }

    internal void ClearStopRequest() => StopRequested = false;
}