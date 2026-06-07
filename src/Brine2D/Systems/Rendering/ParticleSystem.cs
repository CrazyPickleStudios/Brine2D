using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Query;
using Brine2D.ECS.Systems;
using Brine2D.Pooling;
using Brine2D.Rendering;
using Brine2D.Rendering.TextureAtlas;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// System that updates and renders particle emitters.
/// </summary>
public class ParticleSystem : IUpdateSystem, IRenderSystem, IDisposable
{
    public string Name => "ParticleSystem";
    public int UpdateOrder => 250;
    public int RenderOrder => 100;

    public bool IsEnabled { get; set; } = true;

    private readonly Random _random;
    private readonly ILogger<ParticleSystem>? _logger;
    private CachedEntityQuery<ParticleEmitterComponent>? _emitterQuery;
    private readonly ObjectPool<Particle> _particlePool;
    private readonly List<RenderItem> _renderBuffer = new();

    private readonly
        List<(Vector2 Position, SubEmitterConfig Config, Vector2 ParentVelocity, ParticleEmitterComponent? Owner)>
        _pendingSubEmits = new();

    private readonly List<SubEmitterState> _activeSubEmitters = new();

    private readonly HashSet<ParticleEmitterComponent> _gradientVariationWarned =
        new(ReferenceEqualityComparer.Instance);

    private readonly HashSet<SubEmitterConfig> _trailModeWarned = new(ReferenceEqualityComparer.Instance);

    private readonly HashSet<ParticleEmitterComponent>
        _trailModeEmitterWarned = new(ReferenceEqualityComparer.Instance);

    private float _turbulenceTime;

    // Per-emitter turbulence time offsets keyed by reference. Seeded once on first sight so
    // two emitters with identical settings do not animate in lockstep.
    private readonly Dictionary<ParticleEmitterComponent, float> _emitterTurbulenceOffset =
        new(ReferenceEqualityComparer.Instance);

    // Reusable staging buffer for building instance data before submitting to SDL3ParticleRenderer.
    // Grows on demand; never shrinks. Zero per-frame allocations after warm-up.
    private SDL3ParticleRenderer.ParticleInstance[] _instanceStagingBuffer =
        new SDL3ParticleRenderer.ParticleInstance[256];

    private const float WarmupStep = 1f / 30f;

    /// <summary>
    /// Maximum allowed <see cref="ParticleEmitterComponent.WarmupDuration"/> that will be
    /// pre-simulated. Values above this are silently clamped so a misconfigured emitter
    /// cannot stall the first frame.
    /// </summary>
    public const float MaxWarmupDuration = 10f;

    /// <summary>
    /// Minimum particle lifetime enforced after variation is applied, in seconds.
    /// Prevents negative or zero lifetimes from causing particles that expire instantly.
    /// </summary>
    public const float MinParticleLifetime = 0.001f;

    /// <summary>
    /// Total number of live particles across all active emitters and sub-emitters managed by
    /// this system. Useful for performance budgets and debug overlays.
    /// </summary>
    public int TotalParticleCount
    {
        get
        {
            var total = 0;
            if (_emitterQuery != null)
                foreach (var (_, e) in _emitterQuery)
                    total += e.ParticleCount;
            foreach (var s in _activeSubEmitters)
                total += s.Particles.Count;
            return total;
        }
    }

    public ParticleSystem(ObjectPoolProvider poolProvider, int? seed = null, ILogger<ParticleSystem>? logger = null)
    {
        _random = seed.HasValue ? new Random(seed.Value) : new Random();
        _particlePool = poolProvider.Create(new PoolableObjectPolicy<Particle>());
        _logger = logger;
    }

    /// <inheritdoc/>
    public void Dispose()
    {
        if (_emitterQuery != null)
        {
            foreach (var (_, emitter) in _emitterQuery)
            {
                foreach (var p in emitter.Particles)
                    _particlePool.Return(p);
                emitter.Particles.Clear();
            }
        }

        foreach (var state in _activeSubEmitters)
        {
            foreach (var p in state.Particles)
                _particlePool.Return(p);
            state.Particles.Clear();
        }

        _activeSubEmitters.Clear();
        _pendingSubEmits.Clear();
        _trailModeWarned.Clear();
    }

    /// <summary>
    /// Fire-and-forget burst: spawns sub-particles at <paramref name="worldPosition"/> using
    /// <paramref name="config"/> without requiring an ECS entity or component.
    /// The burst is processed on the next <see cref="Update"/> call.
    /// </summary>
    public void Burst(Vector2 worldPosition, SubEmitterConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);
        SpawnSubEmitterBurst(worldPosition, config);
    }

    /// <summary>
    /// Immediately expires all live sub-particles spawned from <paramref name="config"/> and
    /// removes their states. Useful for cancelling death or birth sub-effects mid-flight.
    /// The <paramref name="config"/> instance is matched by reference.
    /// </summary>
    public void ClearSubEmitters(SubEmitterConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        for (int i = _activeSubEmitters.Count - 1; i >= 0; i--)
        {
            var state = _activeSubEmitters[i];
            if (!ReferenceEquals(state.Config, config))
                continue;

            foreach (var p in state.Particles)
                _particlePool.Return(p);
            state.Particles.Clear();
            _activeSubEmitters.RemoveAt(i);
        }

        _trailModeWarned.Remove(config);
    }

    /// <summary>
    /// Immediately expires all live sub-particles across every active sub-emitter and
    /// clears the internal sub-emitter list. Does not affect main emitter particles.
    /// </summary>
    public void ClearAllSubEmitters()
    {
        foreach (var state in _activeSubEmitters)
        {
            foreach (var p in state.Particles)
                _particlePool.Return(p);
            state.Particles.Clear();
        }

        _activeSubEmitters.Clear();
        _trailModeWarned.Clear();
    }

    public void Update(IEntityWorld world, GameTime gameTime)
    {
        _emitterQuery ??= world.CreateCachedQuery<ParticleEmitterComponent>().Build();
        var deltaTime = (float)gameTime.DeltaTime;

        _turbulenceTime += deltaTime;

        // Snapshot the count so sub-emitters spawned this frame by DrainSubEmits are not
        // updated until the next frame, preventing a double-advance on their birth frame.
        int existingSubEmitterCount = _activeSubEmitters.Count;

        foreach (var (entity, emitter) in _emitterQuery)
        {
            if (emitter.StopRequested)
            {
                foreach (var p in emitter.Particles)
                    _particlePool.Return(p);
                emitter.Particles.Clear();
                emitter.ClearStopRequest();
                _gradientVariationWarned.Remove(emitter);
                _emitterTurbulenceOffset.Remove(emitter);
                _trailModeEmitterWarned.Remove(emitter);

                // Return sub-particles that were spawned by this emitter (birth, death, or
                // fraction triggers) to the pool alongside the main particles so that Stop()
                // produces a visually clean cutoff with no orphaned sub-effects.
                for (int si = _activeSubEmitters.Count - 1; si >= 0; si--)
                {
                    if (!ReferenceEquals(_activeSubEmitters[si].Owner, emitter))
                        continue;
                    foreach (var p in _activeSubEmitters[si].Particles)
                        _particlePool.Return(p);
                    _activeSubEmitters[si].Particles.Clear();
                    _activeSubEmitters.RemoveAt(si);
                }
            }

            if (!emitter.IsEnabled || emitter.IsPaused)
                continue;

            var transform = entity.GetComponent<TransformComponent>();

            if (transform == null)
            {
                _logger?.LogWarning(
                    "[ParticleSystem] Entity '{Name}' (Id={Id}) has a ParticleEmitterComponent but no TransformComponent — emission is suppressed.",
                    entity.Name, entity.Id);
                continue;
            }

            WarnIfGradientVariationConflict(emitter);
            WarnIfTrailModeLineWithTexture(emitter);
            EnsureTurbulenceOffset(emitter);
            EnsureCleanupRegistered(emitter);

            if (!emitter.WarmupApplied && emitter.WarmupDuration > 0f)
            {
                emitter.WarmupApplied = true;
                ApplyWarmup(emitter, transform);


                if (emitter.IsBurst && emitter.BurstFired && emitter.Particles.Count == 0)
                {
                    emitter.IsEnabled = false;
                    _gradientVariationWarned.Remove(emitter);
                    _emitterTurbulenceOffset.Remove(emitter);
                    _trailModeEmitterWarned.Remove(emitter);
                    emitter.OnEmitterFinished?.Invoke();
                    continue;
                }
            }

            var hadParticles = emitter.Particles.Count > 0;
            UpdateParticles(emitter, transform, deltaTime);
            DrainSubEmits(deltaTime);

            float emitDelta = deltaTime;
            if (emitter.Delay > 0f && emitter.DelayTimer < emitter.Delay)
            {
                emitter.DelayTimer += deltaTime;
                emitDelta = emitter.DelayTimer < emitter.Delay
                    ? 0f
                    : emitter.DelayTimer - emitter.Delay;
            }

            if (emitter.IsEmitting && emitDelta > 0f)
            {
                EmitParticles(emitter, transform, emitDelta);

                if (!emitter.IsBurst && emitter.Duration.HasValue)
                {
                    emitter.DurationTimer += emitDelta;
                    if (emitter.DurationTimer >= emitter.Duration.Value)
                    {
                        emitter.IsEmitting = false;
                        emitter.DurationTimer = 0f;
                        emitter.DurationElapsed = true;
                    }
                }
            }

            // Flush birth sub-emits queued during EmitParticles for this emitter immediately
            // so they are not held over to the next frame.
            DrainSubEmits(deltaTime);

            emitter.PreviousPosition = transform.Position;

            if (emitter.IsBurst && emitter.BurstFired && !emitter.IsEmitting && emitter.Particles.Count == 0)
            {
                if (emitter.Loop && hadParticles)
                {
                    emitter.BurstFired = false;
                    emitter.IsEmitting = true;
                    emitter.DelayTimer = 0f;
                }
                else
                {
                    emitter.IsEnabled = false;
                    _gradientVariationWarned.Remove(emitter);
                    _emitterTurbulenceOffset.Remove(emitter);
                    _trailModeEmitterWarned.Remove(emitter);
                    if (hadParticles)
                        emitter.OnEmitterFinished?.Invoke();
                }
            }
            else if (!emitter.IsBurst && emitter.DurationElapsed && !emitter.IsEmitting && emitter.Particles.Count == 0)
            {
                emitter.DurationElapsed = false;

                if (emitter.Loop)
                {
                    emitter.IsEmitting = true;
                    emitter.DurationTimer = 0f;
                    emitter.DelayTimer = 0f;
                    emitter.EmissionTimer = 0f;
                }
                else
                {
                    _gradientVariationWarned.Remove(emitter);
                    _emitterTurbulenceOffset.Remove(emitter);
                    _trailModeEmitterWarned.Remove(emitter);
                    emitter.OnEmitterFinished?.Invoke();
                }
            }
        }

        // Only update sub-emitters that existed before this frame's emitter loop ran.
        // Newly spawned sub-emitters start moving on the next frame.
        for (int i = Math.Min(existingSubEmitterCount, _activeSubEmitters.Count) - 1; i >= 0; i--)
        {
            var s = _activeSubEmitters[i];

            // Respect the owner emitter's pause state so that pausing a single emitter
            // also freezes its birth/death/fraction sub-particles.
            // Sub-emitters with no owner (spawned via ParticleSystem.Burst) always run.
            if (s.Owner?.IsPaused == true)
                continue;

            UpdateSubEmitterParticles(s, deltaTime);
            if (s.Particles.Count == 0)
                _activeSubEmitters.RemoveAt(i);
        }
    }

    private void WarnIfGradientVariationConflict(ParticleEmitterComponent emitter)
    {
        if (_logger == null)
            return;
        if (emitter.ColorGradient is not { Length: >= 2 })
            return;

        var hasVariation =
            emitter.StartColorVariation.R != 0 || emitter.StartColorVariation.G != 0 ||
            emitter.StartColorVariation.B != 0 || emitter.StartColorVariation.A != 0 ||
            emitter.EndColorVariation.R != 0 || emitter.EndColorVariation.G != 0 ||
            emitter.EndColorVariation.B != 0 || emitter.EndColorVariation.A != 0;

        if (!hasVariation)
            return;

        if (_gradientVariationWarned.Add(emitter))
        {
            _logger.LogWarning(
                "[ParticleSystem] Emitter has both ColorGradient and StartColorVariation/EndColorVariation set. " +
                "Color variation is ignored when a gradient is active. Clear the variation or remove the gradient.");
        }
    }

    private void WarnIfTrailModeLineWithTexture(ParticleEmitterComponent emitter)
    {
        if (_logger == null)
            return;
        if (emitter.TrailMode != TrailMode.Lines || !emitter.EnableTrails)
            return;

        var hasTexture = emitter.ParticleTexture != null
                         || emitter.ParticleAtlasRegion != null
                         || emitter.ParticleFrames != null;

        if (!hasTexture)
            return;

        if (_trailModeEmitterWarned.Add(emitter))
        {
            _logger.LogWarning(
                "[ParticleSystem] Emitter has TrailMode.Lines set but also has a texture assigned. " +
                "TrailMode.Lines is not supported for textured particles and will fall back to TrailMode.Sprites. " +
                "Remove the texture or set TrailMode = TrailMode.Sprites to silence this warning.");
        }
    }

    private void WarnIfSubEmitterTrailModeLineWithTexture(SubEmitterConfig cfg)
    {
        if (_logger == null)
            return;
        if (cfg.TrailMode != TrailMode.Lines || !cfg.EnableTrails)
            return;

        var hasTexture = cfg.ParticleTexture != null
                         || cfg.ParticleAtlasRegion != null
                         || cfg.ParticleFrames != null;

        if (!hasTexture)
            return;

        if (_trailModeWarned.Add(cfg))
        {
            _logger.LogWarning(
                "[ParticleSystem] SubEmitterConfig has TrailMode.Lines set but also has a texture assigned. " +
                "TrailMode.Lines is not supported for textured particles and will fall back to TrailMode.Sprites. " +
                "Remove the texture or set TrailMode = TrailMode.Sprites to silence this warning.");
        }
    }

    private void EnsureTurbulenceOffset(ParticleEmitterComponent emitter)
    {
        if (!_emitterTurbulenceOffset.ContainsKey(emitter))
            _emitterTurbulenceOffset[emitter] = (float)(_random.NextDouble() * 1000.0);
    }

    private void EnsureCleanupRegistered(ParticleEmitterComponent emitter)
    {
        if (emitter.CleanupForPool != null)
            return;

        emitter.CleanupForPool = () =>
        {
            foreach (var p in emitter.Particles)
                _particlePool.Return(p);
            emitter.Particles.Clear();
            _gradientVariationWarned.Remove(emitter);
            _emitterTurbulenceOffset.Remove(emitter);
            _trailModeEmitterWarned.Remove(emitter);
        };
    }

    private float GetTurbulenceTime(ParticleEmitterComponent emitter) =>
        _turbulenceTime + (_emitterTurbulenceOffset.TryGetValue(emitter, out var offset) ? offset : 0f);

    private void DrainSubEmits(float deltaTime)
    {
        if (_pendingSubEmits.Count == 0)
            return;

        foreach (var (pos, cfg, parentVelocity, owner) in _pendingSubEmits)
        {
            WarnIfSubEmitterTrailModeLineWithTexture(cfg);
            SpawnSubEmitterBurst(pos, cfg, parentVelocity, owner);
        }

        _pendingSubEmits.Clear();
    }

    private void ApplyWarmup(ParticleEmitterComponent emitter, TransformComponent transform)
    {
        var remaining = MathF.Min(emitter.WarmupDuration, MaxWarmupDuration);

        var delayRemaining = emitter.Delay;
        while (delayRemaining > 0f && remaining > 0f)
        {
            var step = MathF.Min(delayRemaining, MathF.Min(remaining, WarmupStep));
            delayRemaining -= step;
            remaining -= step;
        }

        emitter.DelayTimer = MathF.Max(emitter.Delay, emitter.DelayTimer);

        while (remaining > 0f)
        {
            var step = MathF.Min(remaining, WarmupStep);
            EmitParticles(emitter, transform, step);
            DrainSubEmits(step);
            UpdateParticles(emitter, transform, step);

            for (int i = _activeSubEmitters.Count - 1; i >= 0; i--)
            {
                var s = _activeSubEmitters[i];
                UpdateSubEmitterParticles(s, step);
                if (s.Particles.Count == 0)
                    _activeSubEmitters.RemoveAt(i);
            }

            remaining -= step;
        }
    }

    /// <summary>
    /// Converts a local-space particle position to world space, applying the entity's full
    /// transform (translation, rotation, scale). Used when computing world positions for
    /// sub-emitter spawn points on local-space emitters so that all three triggers — death,
    /// birth, and lifetime-fraction — correctly account for a rotated or scaled entity.
    /// </summary>
    private static Vector2 LocalToWorldPos(Vector2 localPos, TransformComponent transform)
    {
        var rotation = transform.Rotation;
        var scale = transform.Scale;
        return rotation != 0f
            ? transform.Position + Vector2.Transform(localPos * scale, Matrix3x2.CreateRotation(rotation))
            : localPos * scale + transform.Position;
    }

    private void UpdateParticles(ParticleEmitterComponent emitter, TransformComponent transform, float deltaTime)
    {
        var speedOverLifetimeActive = emitter.StartSpeedMultiplier != 1f || emitter.EndSpeedMultiplier != 1f;
        var hasTurbulence = emitter.TurbulenceStrength > 0f;
        var hasDeathSubEmitters = emitter.DeathSubEmitters is { Count: > 0 };
        var hasForces = emitter.Forces is { Count: > 0 };
        var hasFractionTriggers = emitter.LifetimeFractionSubEmitters is { Count: > 0 };
        var turbFreq = emitter.TurbulenceFrequency;
        var turbTime = hasTurbulence ? GetTurbulenceTime(emitter) : 0f;

        for (int i = emitter.Particles.Count - 1; i >= 0; i--)
        {
            var particle = emitter.Particles[i];
            particle.Life -= deltaTime;

            if (particle.Life <= 0)
            {
                emitter.OnParticleDied?.Invoke(particle);

                if (hasDeathSubEmitters)
                {
                    var worldPos = emitter.SimulateInLocalSpace
                        ? LocalToWorldPos(particle.Position, transform)
                        : particle.Position;

                    foreach (var cfg in emitter.DeathSubEmitters!)
                        _pendingSubEmits.Add((worldPos, cfg, particle.Velocity, emitter));
                }

                _particlePool.Return(particle);
                emitter.Particles.RemoveAt(i);
                continue;
            }

            if (hasFractionTriggers)
                CheckFractionTriggers(emitter, transform, particle);

            if (emitter.Damping > 0f)
            {
                var dampFactor = MathF.Exp(-emitter.Damping * deltaTime);
                particle.BaseVelocity *= dampFactor;
                particle.GravityVelocity *= dampFactor;
            }

            particle.GravityVelocity += emitter.Gravity * deltaTime;

            if (speedOverLifetimeActive)
            {
                var t = 1f - (particle.Life / particle.MaxLife);
                var targetSpeed = particle.BaseSpeed *
                                  MathHelper.Lerp(emitter.StartSpeedMultiplier, emitter.EndSpeedMultiplier, t);
                var baseLen = particle.BaseVelocity.Length();
                if (baseLen > 0.0001f)
                    particle.BaseVelocity = particle.BaseVelocity * (targetSpeed / baseLen);
            }

            if (hasTurbulence)
            {
                var nx = ValueNoise2D(particle.Position.X * turbFreq + turbTime, particle.Position.Y * turbFreq);
                var ny = ValueNoise2D(particle.Position.X * turbFreq, particle.Position.Y * turbFreq + turbTime + 3.7f);
                particle.BaseVelocity += new Vector2(
                    (nx * 2f - 1f) * emitter.TurbulenceStrength * deltaTime,
                    (ny * 2f - 1f) * emitter.TurbulenceStrength * deltaTime);
            }

            if (hasForces)
            {
                var worldPos = emitter.SimulateInLocalSpace
                    ? LocalToWorldPos(particle.Position, transform)
                    : particle.Position;

                foreach (var force in emitter.Forces!)
                    particle.BaseVelocity += force.Evaluate(worldPos, deltaTime);
            }

            particle.Velocity = particle.BaseVelocity + particle.GravityVelocity;

            var oldPosition = particle.Position;
            particle.Position += particle.Velocity * deltaTime;

            particle.Rotation += particle.RotationSpeed * deltaTime;

            if (emitter.EnableTrails && particle.TrailPositions != null)
            {
                var tTrail = 1f - (particle.Life / particle.MaxLife);
                var trailColor = emitter.ColorGradient is { Length: >= 2 } gradient
                    ? SampleColorGradient(gradient, tTrail)
                    : LerpColor(particle.StartColor, particle.EndColor, tTrail);

                int trailFrameIndex = -1;
                if (emitter.ParticleFrames is { Length: > 0 } frames)
                    trailFrameIndex = Math.Clamp((int)(tTrail * frames.Length), 0, frames.Length - 1);

                particle.TrailPositions[particle.TrailIndex] = oldPosition;

                if (particle.TrailRotations != null)
                    particle.TrailRotations[particle.TrailIndex] = particle.Rotation;

                if (particle.TrailColors != null)
                    particle.TrailColors[particle.TrailIndex] = trailColor;

                if (particle.TrailFrameIndices != null)
                    particle.TrailFrameIndices[particle.TrailIndex] = trailFrameIndex;

                particle.TrailIndex = (particle.TrailIndex + 1) % particle.TrailPositions.Length;
                if (particle.TrailFilled < particle.TrailPositions.Length)
                    particle.TrailFilled++;
            }
        }
    }

    private void CheckFractionTriggers(ParticleEmitterComponent emitter, TransformComponent transform,
        Particle particle)
    {
        var triggers = emitter.LifetimeFractionSubEmitters!;
        var elapsed = 1f - (particle.Life / particle.MaxLife);

        for (int ti = 0; ti < triggers.Count; ti++)
        {
            var trigger = triggers[ti];
            if (elapsed < trigger.Fraction)
                continue;

            particle.FiredFractionTriggers ??= new HashSet<int>();
            if (!particle.FiredFractionTriggers.Add(ti))
                continue;

            var worldPos = emitter.SimulateInLocalSpace
                ? LocalToWorldPos(particle.Position, transform)
                : particle.Position;

            _pendingSubEmits.Add((worldPos, trigger.Config, particle.Velocity, emitter));
        }
    }

    private void UpdateSubEmitterParticles(SubEmitterState state, float deltaTime)
    {
        var cfg = state.Config;
        var hasTurbulence = cfg.TurbulenceStrength > 0f;
        var speedOverLifetimeActive = cfg.StartSpeedMultiplier != 1f || cfg.EndSpeedMultiplier != 1f;
        var hasForces = cfg.Forces is { Count: > 0 };
        var turbFreq = cfg.TurbulenceFrequency;
        var turbTime = hasTurbulence ? _turbulenceTime + state.TurbulenceOffset : 0f;

        for (int i = state.Particles.Count - 1; i >= 0; i--)
        {
            var particle = state.Particles[i];
            particle.Life -= deltaTime;

            if (particle.Life <= 0)
            {
                cfg.OnParticleDied?.Invoke(particle);
                _particlePool.Return(particle);
                state.Particles.RemoveAt(i);
                continue;
            }

            if (cfg.Damping > 0f)
            {
                var dampFactor = MathF.Exp(-cfg.Damping * deltaTime);
                particle.BaseVelocity *= dampFactor;
                particle.GravityVelocity *= dampFactor;
            }

            particle.GravityVelocity += cfg.Gravity * deltaTime;

            if (speedOverLifetimeActive)
            {
                var t = 1f - (particle.Life / particle.MaxLife);
                var targetSpeed = particle.BaseSpeed *
                                  MathHelper.Lerp(cfg.StartSpeedMultiplier, cfg.EndSpeedMultiplier, t);
                var baseLen = particle.BaseVelocity.Length();
                if (baseLen > 0.0001f)
                    particle.BaseVelocity = particle.BaseVelocity * (targetSpeed / baseLen);
            }

            if (hasTurbulence)
            {
                var nx = ValueNoise2D(particle.Position.X * turbFreq + turbTime, particle.Position.Y * turbFreq);
                var ny = ValueNoise2D(particle.Position.X * turbFreq, particle.Position.Y * turbFreq + turbTime + 3.7f);
                particle.BaseVelocity += new Vector2(
                    (nx * 2f - 1f) * cfg.TurbulenceStrength * deltaTime,
                    (ny * 2f - 1f) * cfg.TurbulenceStrength * deltaTime);
            }

            if (hasForces)
            {
                foreach (var force in cfg.Forces!)
                    particle.BaseVelocity += force.Evaluate(particle.Position, deltaTime);
            }

            particle.Velocity = particle.BaseVelocity + particle.GravityVelocity;
            var oldPosition = particle.Position;
            particle.Position += particle.Velocity * deltaTime;
            particle.Rotation += particle.RotationSpeed * deltaTime;

            if (cfg.EnableTrails && particle.TrailPositions != null)
            {
                var tTrail = 1f - (particle.Life / particle.MaxLife);
                var trailColor = cfg.ColorGradient is { Length: >= 2 } gradient
                    ? SampleColorGradient(gradient, tTrail)
                    : LerpColor(particle.StartColor, particle.EndColor, tTrail);

                int trailFrameIndex = -1;
                if (cfg.ParticleFrames is { Length: > 0 } frames)
                    trailFrameIndex = Math.Clamp((int)(tTrail * frames.Length), 0, frames.Length - 1);

                particle.TrailPositions[particle.TrailIndex] = oldPosition;

                if (particle.TrailRotations != null)
                    particle.TrailRotations[particle.TrailIndex] = particle.Rotation;

                if (particle.TrailColors != null)
                    particle.TrailColors[particle.TrailIndex] = trailColor;

                if (particle.TrailFrameIndices != null)
                    particle.TrailFrameIndices[particle.TrailIndex] = trailFrameIndex;

                particle.TrailIndex = (particle.TrailIndex + 1) % particle.TrailPositions.Length;
                if (particle.TrailFilled < particle.TrailPositions.Length)
                    particle.TrailFilled++;
            }
        }
    }

    /// <summary>
    /// Returns the total number of live particles across all active <see cref="SubEmitterState"/>
    /// entries that share the given <paramref name="config"/> instance (by reference).
    /// </summary>
    private int CountLiveSubParticles(SubEmitterConfig config)
    {
        var total = 0;
        foreach (var state in _activeSubEmitters)
        {
            if (ReferenceEquals(state.Config, config))
                total += state.Particles.Count;
        }

        return total;
    }

    private void SpawnSubEmitterBurst(Vector2 worldPos, SubEmitterConfig cfg, Vector2 parentVelocity = default,
        ParticleEmitterComponent? owner = null)
    {
        var alreadyLive = CountLiveSubParticles(cfg);
        var available = cfg.MaxParticles - alreadyLive;
        if (available <= 0)
            return;

        var count = Math.Min(cfg.BurstCount, available);
        var state = new SubEmitterState(cfg, (float)(_random.NextDouble() * 1000.0), owner);

        for (int i = 0; i < count; i++)
        {
            var spawnPos = worldPos + GetSpawnOffsetForSubEmitter(cfg);

            float randomAngle;
            if (cfg.Shape == EmitterShape.Cone)
            {
                var baseAngle = MathF.Atan2(cfg.InitialVelocity.Y, cfg.InitialVelocity.X);
                var coneRadians = cfg.ConeAngle * (MathF.PI / 180f);
                randomAngle = baseAngle + ((float)_random.NextDouble() - 0.5f) * coneRadians;
            }
            else
            {
                var baseAngle = MathF.Atan2(cfg.InitialVelocity.Y, cfg.InitialVelocity.X);
                var spreadRadians = cfg.VelocitySpread * (MathF.PI / 180f);
                randomAngle = baseAngle + ((float)_random.NextDouble() - 0.5f) * spreadRadians;
            }

            var speed = cfg.InitialVelocity.Length();
            var speedMult = 1f + ((float)_random.NextDouble() - 0.5f) * cfg.SpeedVariation;
            speed *= Math.Max(0f, speedMult);

            var velocity = new Vector2(MathF.Cos(randomAngle) * speed, MathF.Sin(randomAngle) * speed);

            if (cfg.VelocityInheritance != 0f)
                velocity += parentVelocity * cfg.VelocityInheritance;

            var lifetime = cfg.ParticleLifetime;
            lifetime += ((float)_random.NextDouble() - 0.5f) * cfg.LifetimeVariation * cfg.ParticleLifetime;
            lifetime = MathF.Max(lifetime, MinParticleLifetime);

            var rotation = cfg.InitialRotation;
            if (cfg.InitialRotationVariation > 0f)
                rotation += ((float)_random.NextDouble() - 0.5f) * 2f * cfg.InitialRotationVariation;

            var rotationSpeed = cfg.RotationSpeed
                                + ((float)_random.NextDouble() - 0.5f) * 2f * cfg.RotationSpeedVariation;

            var size = cfg.StartSize;
            if (cfg.SizeVariation > 0f)
                size = Math.Max(0f, size + ((float)_random.NextDouble() - 0.5f) * 2f * cfg.SizeVariation);

            var endSize = cfg.EndSize;
            if (cfg.EndSizeVariation > 0f)
                endSize = Math.Max(0f, endSize + ((float)_random.NextDouble() - 0.5f) * 2f * cfg.EndSizeVariation);

            var startColor = VaryColor(cfg.StartColor, cfg.StartColorVariation);
            var endColor = VaryColor(cfg.EndColor, cfg.EndColorVariation);

            var particle = _particlePool.Get();
            particle.Position = spawnPos;
            particle.BaseVelocity = velocity;
            particle.GravityVelocity = Vector2.Zero;
            particle.BaseSpeed = velocity.Length();
            particle.Velocity = velocity;
            particle.Life = lifetime;
            particle.MaxLife = lifetime;
            particle.Size = size;
            particle.StartSize = size;
            particle.EndSize = endSize;
            particle.StartColor = startColor;
            particle.EndColor = endColor;
            particle.Rotation = rotation;
            particle.RotationSpeed = rotationSpeed;

            var effectiveTrailLength = cfg.EnableTrails && cfg.TrailLength > 0 ? cfg.TrailLength : 0;
            if (effectiveTrailLength > 0)
            {
                if (particle.TrailPositions == null || particle.TrailPositions.Length != effectiveTrailLength)
                    particle.TrailPositions = new Vector2[effectiveTrailLength];

                if (particle.TrailRotations == null || particle.TrailRotations.Length != effectiveTrailLength)
                    particle.TrailRotations = new float[effectiveTrailLength];

                if (particle.TrailColors == null || particle.TrailColors.Length != effectiveTrailLength)
                    particle.TrailColors = new Color[effectiveTrailLength];

                if (particle.TrailFrameIndices == null || particle.TrailFrameIndices.Length != effectiveTrailLength)
                    particle.TrailFrameIndices = new int[effectiveTrailLength];

                for (int j = 0; j < particle.TrailPositions.Length; j++)
                {
                    particle.TrailPositions[j] = spawnPos;
                    particle.TrailRotations[j] = rotation;
                    particle.TrailColors[j] = startColor;
                    particle.TrailFrameIndices[j] = -1;
                }

                particle.TrailIndex = 0;
                particle.TrailFilled = 0;
            }
            else
            {
                particle.TrailPositions = null;
                particle.TrailRotations = null;
                particle.TrailColors = null;
                particle.TrailFrameIndices = null;
                particle.TrailFilled = 0;
            }

            state.Particles.Add(particle);
            cfg.OnParticleSpawned?.Invoke(particle);
        }

        if (state.Particles.Count > 0)
            _activeSubEmitters.Add(state);
    }

    private Vector2 GetSpawnOffsetForSubEmitter(SubEmitterConfig cfg)
    {
        return cfg.Shape switch
        {
            EmitterShape.Point => Vector2.Zero,
            EmitterShape.Circle => GetCircleSpawn(cfg.SpawnRadius, cfg.SpawnOnPerimeter),
            EmitterShape.Cone => GetCircleSpawn(cfg.SpawnRadius, cfg.SpawnOnPerimeter),
            EmitterShape.Box => GetBoxSpawn(cfg.ShapeSize, cfg.BoxAngle),
            EmitterShape.Line => GetLineSpawn(
                cfg.LineLength > 0f ? cfg.LineLength : cfg.ShapeSize.X,
                cfg.LineAngle),
            _ => Vector2.Zero
        };
    }

    private void EmitParticles(ParticleEmitterComponent emitter, TransformComponent transform, float deltaTime)
    {
        if (emitter.EmissionRate <= 0f && !emitter.IsBurst)
            return;

        int particlesToEmit;
        bool isContinuous = false;

        if (emitter.IsBurst && !emitter.BurstFired)
        {
            particlesToEmit = Math.Min(emitter.BurstCount, emitter.MaxParticles);
            emitter.BurstFired = true;
            emitter.IsEmitting = false;
        }
        else if (!emitter.IsBurst)
        {
            isContinuous = true;
            emitter.EmissionTimer += deltaTime;
            particlesToEmit = (int)(emitter.EmissionRate * emitter.EmissionTimer);
        }
        else
        {
            return;
        }

        if (particlesToEmit <= 0)
            return;

        var currentPos = emitter.SimulateInLocalSpace
            ? emitter.SpawnOffset
            : transform.Position + emitter.SpawnOffset;

        Vector2 emitterVelocity = Vector2.Zero;
        if (emitter.PreviousPosition.HasValue && deltaTime > 0f)
            emitterVelocity = (transform.Position - emitter.PreviousPosition.Value) / deltaTime;

        var prevPos = emitter.PreviousPosition.HasValue
            ? (emitter.SimulateInLocalSpace
                ? emitter.SpawnOffset
                : emitter.PreviousPosition.Value + emitter.SpawnOffset)
            : currentPos;

        // Clamp to available capacity before computing interpolation fractions so that
        // the particles which do spawn are evenly distributed across the arc, rather than
        // clustering near the start when the cap cuts the loop short mid-way.
        var available = emitter.MaxParticles - emitter.Particles.Count;
        var interpolationCount = Math.Min(particlesToEmit, available);

        int actuallySpawned = 0;
        for (int i = 0; i < interpolationCount; i++)
        {
            // Burst emitters always spawn at the current position — interpolating across
            // the movement arc would smear all burst particles in a line when the entity
            // is moving. Only continuous emitters fill the inter-frame gap.
            var interpolatedBase = (!emitter.IsBurst && interpolationCount > 1)
                ? Vector2.Lerp(prevPos, currentPos, (float)i / (interpolationCount - 1))
                : currentPos;

            SpawnParticle(emitter, transform, interpolatedBase, emitterVelocity);
            actuallySpawned++;
        }

        if (isContinuous)
        {
            emitter.EmissionTimer -= actuallySpawned / emitter.EmissionRate;

            var maxTimer = emitter.EmissionRate > 0f ? 1f / emitter.EmissionRate : 0f;
            if (emitter.EmissionTimer < 0f)
                emitter.EmissionTimer = 0f;
            else if (emitter.EmissionTimer > maxTimer)
                emitter.EmissionTimer = maxTimer;
        }
    }

    private void SpawnParticle(
        ParticleEmitterComponent emitter,
        TransformComponent transform,
        Vector2 basePosition,
        Vector2 emitterVelocity)
    {
        var spawnPos = basePosition + GetSpawnOffsetForShape(emitter);

        var velocityRotation = emitter.SimulateInLocalSpace ? 0f : transform.Rotation;
        var rotatedVelocity = Vector2.Transform(
            emitter.InitialVelocity,
            Matrix3x2.CreateRotation(velocityRotation));

        var baseAngle = MathF.Atan2(rotatedVelocity.Y, rotatedVelocity.X);

        float randomAngle;
        if (emitter.Shape == EmitterShape.Cone)
        {
            var coneRadians = emitter.ConeAngle * (MathF.PI / 180f);
            randomAngle = baseAngle + ((float)_random.NextDouble() - 0.5f) * coneRadians;
        }
        else
        {
            var spreadRadians = emitter.VelocitySpread * (MathF.PI / 180f);
            randomAngle = baseAngle + ((float)_random.NextDouble() - 0.5f) * spreadRadians;
        }

        var speed = emitter.InitialVelocity.Length();
        var speedMult = 1f + ((float)_random.NextDouble() - 0.5f) * emitter.SpeedVariation;
        speed *= Math.Max(0f, speedMult);

        var velocity = new Vector2(
            MathF.Cos(randomAngle) * speed,
            MathF.Sin(randomAngle) * speed);

        if (emitter.VelocityInheritance != 0f)
            velocity += emitterVelocity * emitter.VelocityInheritance;

        var lifetime = emitter.ParticleLifetime;
        lifetime += ((float)_random.NextDouble() - 0.5f) * emitter.LifetimeVariation * emitter.ParticleLifetime;
        lifetime = MathF.Max(lifetime, MinParticleLifetime);

        var rotation = emitter.InitialRotation;
        if (emitter.InitialRotationVariation > 0f)
            rotation += ((float)_random.NextDouble() - 0.5f) * 2f * emitter.InitialRotationVariation;

        var rotationSpeed = emitter.RotationSpeed
                            + ((float)_random.NextDouble() - 0.5f) * 2f * emitter.RotationSpeedVariation;

        var size = emitter.StartSize;
        if (emitter.SizeVariation > 0f)
            size = Math.Max(0f, size + ((float)_random.NextDouble() - 0.5f) * 2f * emitter.SizeVariation);

        var endSize = emitter.EndSize;
        if (emitter.EndSizeVariation > 0f)
            endSize = Math.Max(0f, endSize + ((float)_random.NextDouble() - 0.5f) * 2f * emitter.EndSizeVariation);

        var startColor = VaryColor(emitter.StartColor, emitter.StartColorVariation);
        var endColor = VaryColor(emitter.EndColor, emitter.EndColorVariation);

        var particle = _particlePool.Get();
        particle.Position = spawnPos;
        particle.BaseVelocity = velocity;
        particle.GravityVelocity = Vector2.Zero;
        particle.BaseSpeed = velocity.Length();
        particle.Velocity = velocity;
        particle.Life = lifetime;
        particle.MaxLife = lifetime;
        particle.Size = size;
        particle.StartSize = size;
        particle.EndSize = endSize;
        particle.StartColor = startColor;
        particle.EndColor = endColor;
        particle.Rotation = rotation;
        particle.RotationSpeed = rotationSpeed;

        var effectiveTrailLength = emitter.TrailLength > 0 ? emitter.TrailLength : 0;

        if (emitter.EnableTrails && effectiveTrailLength > 0)
        {
            if (particle.TrailPositions == null || particle.TrailPositions.Length != effectiveTrailLength)
                particle.TrailPositions = new Vector2[effectiveTrailLength];

            if (particle.TrailRotations == null || particle.TrailRotations.Length != effectiveTrailLength)
                particle.TrailRotations = new float[effectiveTrailLength];

            if (particle.TrailColors == null || particle.TrailColors.Length != effectiveTrailLength)
                particle.TrailColors = new Color[effectiveTrailLength];

            if (particle.TrailFrameIndices == null || particle.TrailFrameIndices.Length != effectiveTrailLength)
                particle.TrailFrameIndices = new int[effectiveTrailLength];

            for (int j = 0; j < particle.TrailPositions.Length; j++)
            {
                particle.TrailPositions[j] = spawnPos;
                particle.TrailRotations[j] = rotation;
                particle.TrailColors[j] = startColor;
                particle.TrailFrameIndices[j] = -1;
            }

            particle.TrailIndex = 0;
            particle.TrailFilled = 0;
        }
        else
        {
            particle.TrailPositions = null;
            particle.TrailRotations = null;
            particle.TrailColors = null;
            particle.TrailFrameIndices = null;
            particle.TrailFilled = 0;
        }

        emitter.Particles.Add(particle);
        emitter.OnParticleSpawned?.Invoke(particle);

        if (emitter.BirthSubEmitters is { Count: > 0 })
        {
            var worldSpawnPos = emitter.SimulateInLocalSpace
                ? LocalToWorldPos(spawnPos, transform)
                : spawnPos;

            foreach (var cfg in emitter.BirthSubEmitters)
                _pendingSubEmits.Add((worldSpawnPos, cfg, particle.Velocity, emitter));
        }
    }

    private Color VaryColor(Color base_, Color variation)
    {
        if (variation.R == 0 && variation.G == 0 && variation.B == 0 && variation.A == 0)
            return base_;

        return new Color(
            (byte)Math.Clamp(base_.R + (int)(((float)_random.NextDouble() - 0.5f) * 2f * variation.R), 0, 255),
            (byte)Math.Clamp(base_.G + (int)(((float)_random.NextDouble() - 0.5f) * 2f * variation.G), 0, 255),
            (byte)Math.Clamp(base_.B + (int)(((float)_random.NextDouble() - 0.5f) * 2f * variation.B), 0, 255),
            (byte)Math.Clamp(base_.A + (int)(((float)_random.NextDouble() - 0.5f) * 2f * variation.A), 0, 255));
    }

    private static Color SampleColorGradient(Color[] gradient, float t)
    {
        if (gradient.Length == 1)
            return gradient[0];

        var scaled = t * (gradient.Length - 1);
        var lower = (int)scaled;
        var upper = Math.Min(lower + 1, gradient.Length - 1);
        var localT = scaled - lower;
        return LerpColor(gradient[lower], gradient[upper], localT);
    }

    private Vector2 GetSpawnOffsetForShape(ParticleEmitterComponent emitter)
    {
        return emitter.Shape switch
        {
            EmitterShape.Point => Vector2.Zero,
            EmitterShape.Circle => GetCircleSpawn(emitter.SpawnRadius, emitter.SpawnOnPerimeter),
            EmitterShape.Box => GetBoxSpawn(emitter.ShapeSize, emitter.BoxAngle),
            EmitterShape.Line => GetLineSpawn(
                emitter.LineLength > 0f ? emitter.LineLength : emitter.ShapeSize.X,
                emitter.LineAngle),
            EmitterShape.Cone => GetCircleSpawn(emitter.SpawnRadius, emitter.SpawnOnPerimeter),
            _ => Vector2.Zero
        };
    }

    private Vector2 GetCircleSpawn(float radius, bool onPerimeter)
    {
        if (radius <= 0) return Vector2.Zero;

        var angle = (float)(_random.NextDouble() * Math.PI * 2);
        var distance = onPerimeter ? radius : MathF.Sqrt((float)_random.NextDouble()) * radius;

        return new Vector2(
            MathF.Cos(angle) * distance,
            MathF.Sin(angle) * distance);
    }

    private Vector2 GetBoxSpawn(Vector2 size, float angleRadians)
    {
        var localOffset = new Vector2(
            ((float)_random.NextDouble() - 0.5f) * size.X,
            ((float)_random.NextDouble() - 0.5f) * size.Y);

        if (angleRadians == 0f)
            return localOffset;

        return Vector2.Transform(localOffset, Matrix3x2.CreateRotation(angleRadians));
    }

    private Vector2 GetLineSpawn(float length, float angleRadians)
    {
        var t = (float)_random.NextDouble();
        var localX = t * length - length / 2f;
        return new Vector2(
            localX * MathF.Cos(angleRadians),
            localX * MathF.Sin(angleRadians));
    }

    public void Render(IEntityWorld world, IRenderer renderer)
    {
        _emitterQuery ??= world.CreateCachedQuery<ParticleEmitterComponent>().Build();

        _renderBuffer.Clear();

        foreach (var (entity, emitter) in _emitterQuery)
        {
            if (emitter.IsEnabled)
                _renderBuffer.Add(new RenderItem(emitter.RenderLayer, entity, emitter, null));
        }

        foreach (var subState in _activeSubEmitters)
            _renderBuffer.Add(new RenderItem(subState.Config.RenderLayer, null, null, subState));

        SortHelper.StableSort(_renderBuffer, static (a, b) => a.Layer.CompareTo(b.Layer));

        var rawBounds = renderer.Camera?.GetVisibleBounds();
        Rectangle? visibleBounds = rawBounds is { Width: > 0, Height: > 0 } ? rawBounds : null;

        // Attempt to obtain the hardware-instanced particle renderer. Null for HeadlessRenderer
        // and any other IRenderer that is not SDL3Renderer — batch path is used transparently.
        var particleRenderer = (renderer as SDL3Renderer)?.ParticleRenderer;

        foreach (var item in _renderBuffer)
        {
            if (item.Emitter != null)
                RenderEmitter(renderer, particleRenderer, item.Entity!, item.Emitter, visibleBounds);
            else if (item.SubState != null)
                RenderSubEmitter(renderer, particleRenderer, item.SubState, visibleBounds);
        }

        renderer.SetBlendMode(BlendMode.Alpha);
    }

    private void RenderEmitter(
        IRenderer renderer,
        SDL3ParticleRenderer? particleRenderer,
        Entity entity,
        ParticleEmitterComponent emitter,
        Rectangle? visibleBounds)
    {
        renderer.SetBlendMode(emitter.BlendMode);

        TransformComponent? transform = null;
        Vector2 localOffset = Vector2.Zero;
        float localRotation = 0f;
        Vector2 localScale = Vector2.One;

        if (emitter.SimulateInLocalSpace)
        {
            transform = entity.GetComponent<TransformComponent>();
            if (transform != null)
            {
                localOffset = transform.Position;
                localRotation = transform.Rotation;
                localScale = transform.Scale;
            }
        }

        var maxParticleSize = Math.Max(emitter.StartSize, emitter.EndSize);

        // Expand cull bounds by both particle size and potential trail reach so trail
        // segments that extend into the visible area are not incorrectly discarded.
        var trailReach = emitter.EnableTrails && emitter.TrailLength > 0
            ? ComputeMaxTrailReach(emitter.TrailLength, emitter.InitialVelocity.Length(), emitter.ParticleLifetime)
            : 0f;

        var cullBounds = visibleBounds.HasValue
            ? ExpandRect(visibleBounds.Value, maxParticleSize + trailReach)
            : (Rectangle?)null;

        // Trails always use the batch path — instancing doesn't help there.
        bool useInstancing = particleRenderer != null && !emitter.EnableTrails;

        if (useInstancing)
            RenderEmitterInstanced(renderer, particleRenderer!, emitter, localOffset, localRotation, localScale, cullBounds);
        else
            RenderEmitterBatched(renderer, emitter, localOffset, localRotation, localScale, cullBounds);
    }

    private void RenderEmitterInstanced(
        IRenderer renderer,
        SDL3ParticleRenderer particleRenderer,
        ParticleEmitterComponent emitter,
        Vector2 localOffset,
        float localRotation,
        Vector2 localScale,
        Rectangle? cullBounds)
    {
        ResolveEmitterTexture(emitter, out var texture, out var textureHandle,
            out var scaleMode, out var baseUVRect, out var hasFrames);

        EnsureInstanceStagingCapacity(emitter.Particles.Count);
        int count = 0;

        foreach (var particle in emitter.Particles)
        {
            var t = 1f - (particle.Life / particle.MaxLife);

            var color = emitter.ColorGradient is { Length: >= 2 } gradient
                ? SampleColorGradient(gradient, t)
                : LerpColor(particle.StartColor, particle.EndColor, t);

            var size = MathHelper.Lerp(particle.StartSize, particle.EndSize, t);
            var scaledSize = size * Math.Max(localScale.X, localScale.Y);

            var worldPos = localRotation != 0f
                ? localOffset + Vector2.Transform(particle.Position * localScale,
                    Matrix3x2.CreateRotation(localRotation))
                : particle.Position * localScale + localOffset;

            if (cullBounds.HasValue && !cullBounds.Value.Contains(worldPos))
                continue;

            var uvRect = hasFrames ? ResolveFrameUVRect(emitter.ParticleFrames!, t) : baseUVRect;

            _instanceStagingBuffer[count++] = new SDL3ParticleRenderer.ParticleInstance(
                worldPos,
                scaledSize,
                particle.Rotation,
                new System.Numerics.Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f),
                uvRect
            );
        }

        if (count == 0)
            return;

        int firstInstance = particleRenderer.AppendInstances(_instanceStagingBuffer.AsSpan(0, count));
        if (firstInstance < 0)
            return;

        (renderer as SDL3Renderer)!.EnqueueParticleDrawCall(
            firstInstance, count, emitter.BlendMode, textureHandle, scaleMode, texture);
    }

    private void RenderEmitterBatched(
        IRenderer renderer,
        ParticleEmitterComponent emitter,
        Vector2 localOffset,
        float localRotation,
        Vector2 localScale,
        Rectangle? cullBounds)
    {
        foreach (var particle in emitter.Particles)
        {
            var t = 1f - (particle.Life / particle.MaxLife);

            var color = emitter.ColorGradient is { Length: >= 2 } gradient
                ? SampleColorGradient(gradient, t)
                : LerpColor(particle.StartColor, particle.EndColor, t);

            var size = MathHelper.Lerp(particle.StartSize, particle.EndSize, t);
            var scaledSize = size * Math.Max(localScale.X, localScale.Y);

            var worldPos = localRotation != 0f
                ? localOffset + Vector2.Transform(particle.Position * localScale,
                    Matrix3x2.CreateRotation(localRotation))
                : particle.Position * localScale + localOffset;

            if (cullBounds.HasValue && !cullBounds.Value.Contains(worldPos))
                continue;

            if (emitter.EnableTrails && particle.TrailPositions != null)
                RenderTrail(renderer, emitter, particle, color, scaledSize, localOffset, localRotation, localScale);

            if (emitter.ParticleFrames is { Length: > 0 } frames)
            {
                var frameIndex = Math.Clamp((int)(t * frames.Length), 0, frames.Length - 1);
                var frame = frames[frameIndex];
                RenderTexturedParticle(renderer, frame.AtlasTexture, frame.SourceRect, worldPos, particle.Rotation,
                    color, scaledSize);
            }
            else if (emitter.ParticleAtlasRegion != null)
            {
                RenderTexturedParticle(
                    renderer,
                    emitter.ParticleAtlasRegion.AtlasTexture,
                    emitter.ParticleAtlasRegion.SourceRect,
                    worldPos,
                    particle.Rotation,
                    color,
                    scaledSize);
            }
            else if (emitter.ParticleTexture != null)
            {
                RenderTexturedParticle(renderer, emitter.ParticleTexture, null, worldPos, particle.Rotation, color,
                    scaledSize);
            }
            else
            {
                renderer.DrawCircleFilled(worldPos.X, worldPos.Y, scaledSize, color);
            }
        }
    }

    private void RenderSubEmitter(
        IRenderer renderer,
        SDL3ParticleRenderer? particleRenderer,
        SubEmitterState state,
        Rectangle? visibleBounds)
    {
        var cfg = state.Config;
        renderer.SetBlendMode(cfg.BlendMode);

        var maxParticleSize = Math.Max(cfg.StartSize, cfg.EndSize);
        var trailReach = cfg.EnableTrails && cfg.TrailLength > 0
            ? ComputeMaxTrailReach(cfg.TrailLength, cfg.InitialVelocity.Length(), cfg.ParticleLifetime)
            : 0f;

        var cullBounds = visibleBounds.HasValue
            ? ExpandRect(visibleBounds.Value, maxParticleSize + trailReach)
            : (Rectangle?)null;

        bool useInstancing = particleRenderer != null && !cfg.EnableTrails;

        if (useInstancing)
            RenderSubEmitterInstanced(renderer, particleRenderer!, cfg, state.Particles, cullBounds);
        else
            RenderSubEmitterBatched(renderer, cfg, state.Particles, cullBounds);
    }

    private void RenderSubEmitterInstanced(
        IRenderer renderer,
        SDL3ParticleRenderer particleRenderer,
        SubEmitterConfig cfg,
        List<Particle> particles,
        Rectangle? cullBounds)
    {
        ResolveSubEmitterTexture(cfg, out var texture, out var textureHandle,
            out var scaleMode, out var baseUVRect, out var hasFrames);

        EnsureInstanceStagingCapacity(particles.Count);
        int count = 0;

        foreach (var particle in particles)
        {
            var t = 1f - (particle.Life / particle.MaxLife);

            var color = cfg.ColorGradient is { Length: >= 2 } gradient
                ? SampleColorGradient(gradient, t)
                : LerpColor(particle.StartColor, particle.EndColor, t);

            var size = MathHelper.Lerp(particle.StartSize, particle.EndSize, t);

            if (cullBounds.HasValue && !cullBounds.Value.Contains(particle.Position))
                continue;

            var uvRect = hasFrames ? ResolveFrameUVRect(cfg.ParticleFrames!, t) : baseUVRect;

            _instanceStagingBuffer[count++] = new SDL3ParticleRenderer.ParticleInstance(
                particle.Position,
                size,
                particle.Rotation,
                new System.Numerics.Vector4(color.R / 255f, color.G / 255f, color.B / 255f, color.A / 255f),
                uvRect
            );
        }

        if (count == 0)
            return;

        int firstInstance = particleRenderer.AppendInstances(_instanceStagingBuffer.AsSpan(0, count));
        if (firstInstance < 0)
            return;

        (renderer as SDL3Renderer)!.EnqueueParticleDrawCall(
            firstInstance, count, cfg.BlendMode, textureHandle, scaleMode, texture);
    }

    private void RenderSubEmitterBatched(
        IRenderer renderer,
        SubEmitterConfig cfg,
        List<Particle> particles,
        Rectangle? cullBounds)
    {
        foreach (var particle in particles)
        {
            var t = 1f - (particle.Life / particle.MaxLife);
            var color = cfg.ColorGradient is { Length: >= 2 } gradient
                ? SampleColorGradient(gradient, t)
                : LerpColor(particle.StartColor, particle.EndColor, t);
            var size = MathHelper.Lerp(particle.StartSize, particle.EndSize, t);

            if (cullBounds.HasValue && !cullBounds.Value.Contains(particle.Position))
                continue;

            if (cfg.EnableTrails && particle.TrailPositions != null)
                RenderSubTrail(renderer, cfg, particle, color, size);

            if (cfg.ParticleFrames is { Length: > 0 } frames)
            {
                var frameIndex = Math.Clamp((int)(t * frames.Length), 0, frames.Length - 1);
                var frame = frames[frameIndex];
                RenderTexturedParticle(renderer, frame.AtlasTexture, frame.SourceRect,
                    particle.Position, particle.Rotation, color, size);
            }
            else if (cfg.ParticleAtlasRegion != null)
            {
                RenderTexturedParticle(renderer, cfg.ParticleAtlasRegion.AtlasTexture,
                    cfg.ParticleAtlasRegion.SourceRect, particle.Position, particle.Rotation, color, size);
            }
            else if (cfg.ParticleTexture != null)
            {
                RenderTexturedParticle(renderer, cfg.ParticleTexture, null, particle.Position, particle.Rotation, color,
                    size);
            }
            else
            {
                renderer.DrawCircleFilled(particle.Position.X, particle.Position.Y, size, color);
            }
        }
    }

    private void EnsureInstanceStagingCapacity(int needed)
    {
        if (_instanceStagingBuffer.Length < needed)
        {
            var newSize = Math.Max(needed, _instanceStagingBuffer.Length * 2);
            _instanceStagingBuffer = new SDL3ParticleRenderer.ParticleInstance[newSize];
        }
    }

    private static void ResolveEmitterTexture(
        ParticleEmitterComponent emitter,
        out ITexture? texture,
        out nint textureHandle,
        out TextureScaleMode scaleMode,
        out System.Numerics.Vector4 baseUVRect,
        out bool hasFrames)
    {
        hasFrames = emitter.ParticleFrames is { Length: > 0 };

        if (hasFrames)
        {
            var firstFrame = emitter.ParticleFrames![0];
            texture = firstFrame.AtlasTexture;
            textureHandle = (texture as SDL3Texture)?.Handle ?? nint.Zero;
            scaleMode = texture?.ScaleMode ?? TextureScaleMode.Linear;
            baseUVRect = System.Numerics.Vector4.Zero;
            return;
        }

        if (emitter.ParticleAtlasRegion != null)
        {
            texture = emitter.ParticleAtlasRegion.AtlasTexture;
            textureHandle = (texture as SDL3Texture)?.Handle ?? nint.Zero;
            scaleMode = texture?.ScaleMode ?? TextureScaleMode.Linear;
            var uv = emitter.ParticleAtlasRegion.UVCoordinates;
            baseUVRect = new System.Numerics.Vector4(uv.U1, uv.V1, uv.U2, uv.V2);
            return;
        }

        if (emitter.ParticleTexture != null)
        {
            texture = emitter.ParticleTexture;
            textureHandle = (texture as SDL3Texture)?.Handle ?? nint.Zero;
            scaleMode = texture?.ScaleMode ?? TextureScaleMode.Linear;
            baseUVRect = SDL3ParticleRenderer.FullTextureUVRect;
            return;
        }

        texture = null;
        textureHandle = nint.Zero;
        scaleMode = TextureScaleMode.Linear;
        baseUVRect = SDL3ParticleRenderer.SdfCircleUVRect;
    }

    private static void ResolveSubEmitterTexture(
        SubEmitterConfig cfg,
        out ITexture? texture,
        out nint textureHandle,
        out TextureScaleMode scaleMode,
        out System.Numerics.Vector4 baseUVRect,
        out bool hasFrames)
    {
        hasFrames = cfg.ParticleFrames is { Length: > 0 };

        if (hasFrames)
        {
            var firstFrame = cfg.ParticleFrames![0];
            texture = firstFrame.AtlasTexture;
            textureHandle = (texture as SDL3Texture)?.Handle ?? nint.Zero;
            scaleMode = texture?.ScaleMode ?? TextureScaleMode.Linear;
            baseUVRect = System.Numerics.Vector4.Zero;
            return;
        }

        if (cfg.ParticleAtlasRegion != null)
        {
            texture = cfg.ParticleAtlasRegion.AtlasTexture;
            textureHandle = (texture as SDL3Texture)?.Handle ?? nint.Zero;
            scaleMode = texture?.ScaleMode ?? TextureScaleMode.Linear;
            var uv = cfg.ParticleAtlasRegion.UVCoordinates;
            baseUVRect = new System.Numerics.Vector4(uv.U1, uv.V1, uv.U2, uv.V2);
            return;
        }

        if (cfg.ParticleTexture != null)
        {
            texture = cfg.ParticleTexture;
            textureHandle = (texture as SDL3Texture)?.Handle ?? nint.Zero;
            scaleMode = texture?.ScaleMode ?? TextureScaleMode.Linear;
            baseUVRect = SDL3ParticleRenderer.FullTextureUVRect;
            return;
        }

        texture = null;
        textureHandle = nint.Zero;
        scaleMode = TextureScaleMode.Linear;
        baseUVRect = SDL3ParticleRenderer.SdfCircleUVRect;
    }

    private static System.Numerics.Vector4 ResolveFrameUVRect(
        AtlasRegion[] frames, float t)
    {
        var frameIndex = Math.Clamp((int)(t * frames.Length), 0, frames.Length - 1);
        var uv = frames[frameIndex].UVCoordinates;
        return new System.Numerics.Vector4(uv.U1, uv.V1, uv.U2, uv.V2);
    }

    private static Rectangle ExpandRect(Rectangle rect, float margin)
    {
        return new Rectangle
        {
            X = rect.X - margin,
            Y = rect.Y - margin,
            Width = rect.Width + margin * 2f,
            Height = rect.Height + margin * 2f,
        };
    }

    private static float ComputeMaxTrailReach(int trailLength, float speed, float lifetime) =>
        MathF.Min(speed * lifetime * Math.Min(1f, trailLength / 10f) + trailLength * 4f, 2000f);

    private void RenderTexturedParticle(
        IRenderer renderer,
        ITexture texture,
        Rectangle? sourceRect,
        Vector2 worldPosition,
        float rotation,
        Color color,
        float size)
    {
        var width = sourceRect?.Width ?? texture.Width;
        var height = sourceRect?.Height ?? texture.Height;

        var scaleX = (size * 2f) / width;
        var scaleY = (size * 2f) / height;

        renderer.DrawTexture(
            texture,
            position: worldPosition,
            sourceRect: sourceRect,
            origin: new Vector2(0.5f, 0.5f),
            rotation: rotation,
            scale: new Vector2(scaleX, scaleY),
            color: color,
            flip: SpriteFlip.None);
    }

    private void RenderTrail(
        IRenderer renderer,
        ParticleEmitterComponent emitter,
        Particle particle,
        Color baseColor,
        float baseSize,
        Vector2 localOffset,
        float localRotation,
        Vector2 localScale)
    {
        if (particle.TrailPositions == null || particle.TrailFilled == 0)
            return;

        var segmentsToRender = particle.TrailFilled;
        var useLines = emitter.TrailMode == TrailMode.Lines
                       && emitter.ParticleTexture == null
                       && emitter.ParticleAtlasRegion == null
                       && emitter.ParticleFrames == null;

        Vector2 ToWorld(Vector2 local) => localRotation != 0f
            ? localOffset + Vector2.Transform(local * localScale, Matrix3x2.CreateRotation(localRotation))
            : local * localScale + localOffset;

        if (useLines)
        {
            Vector2? prev = null;
            for (int i = 0; i < segmentsToRender; i++)
            {
                var index = (particle.TrailIndex - segmentsToRender + i + particle.TrailPositions.Length)
                            % particle.TrailPositions.Length;
                var worldPos = ToWorld(particle.TrailPositions[index]);
                // (segmentsToRender - 1) denominator ensures the newest slot maps to segT = 1.0.
                var segT = segmentsToRender > 1 ? (float)i / (segmentsToRender - 1) : 1f;
                var alpha = MathHelper.Lerp(emitter.TrailTailAlpha, emitter.TrailHeadAlpha, segT);
                var thickness =
                    baseSize * MathHelper.Lerp(emitter.TrailTailSizeRatio, emitter.TrailHeadSizeRatio, segT);

                Color slotColor = particle.TrailColors != null && index < particle.TrailColors.Length
                    ? particle.TrailColors[index]
                    : baseColor;
                var lineColor = new Color(slotColor.R, slotColor.G, slotColor.B, (byte)(slotColor.A * alpha));

                if (prev.HasValue)
                    renderer.DrawLine(prev.Value, worldPos, lineColor, Math.Max(1f, thickness));

                prev = worldPos;
            }

            // Connect last trail slot to the live particle head.
            var headPos = ToWorld(particle.Position);
            var headAlpha = emitter.TrailHeadAlpha;
            var headThickness = baseSize * emitter.TrailHeadSizeRatio;
            var headLineColor = new Color(baseColor.R, baseColor.G, baseColor.B, (byte)(baseColor.A * headAlpha));
            if (prev.HasValue)
                renderer.DrawLine(prev.Value, headPos, headLineColor, Math.Max(1f, headThickness));

            return;
        }

        for (int i = 0; i < segmentsToRender; i++)
        {
            var index = (particle.TrailIndex - segmentsToRender + i + particle.TrailPositions.Length)
                        % particle.TrailPositions.Length;
            var trailPos = particle.TrailPositions[index];

            var worldTrailPos = ToWorld(trailPos);

            // (segmentsToRender - 1) denominator ensures the newest slot maps to segT = 1.0.
            var segT = segmentsToRender > 1 ? (float)i / (segmentsToRender - 1) : 1f;
            var alpha = MathHelper.Lerp(emitter.TrailTailAlpha, emitter.TrailHeadAlpha, segT);
            var trailSize = baseSize * MathHelper.Lerp(emitter.TrailTailSizeRatio, emitter.TrailHeadSizeRatio, segT);

            var trailRotation = particle.TrailRotations != null && index < particle.TrailRotations.Length
                ? particle.TrailRotations[index]
                : particle.Rotation;

            Color slotColor = particle.TrailColors != null && index < particle.TrailColors.Length
                ? particle.TrailColors[index]
                : baseColor;

            var trailColor = new Color(slotColor.R, slotColor.G, slotColor.B, (byte)(slotColor.A * alpha));

            if (emitter.ParticleFrames is { Length: > 0 } frames)
            {
                var storedIndex = particle.TrailFrameIndices != null ? particle.TrailFrameIndices[index] : -1;
                var frameIndex = storedIndex >= 0 && storedIndex < frames.Length
                    ? storedIndex
                    : Math.Clamp((int)((1f - (particle.Life / particle.MaxLife)) * frames.Length), 0,
                        frames.Length - 1);
                var frame = frames[frameIndex];
                RenderTexturedParticle(renderer, frame.AtlasTexture, frame.SourceRect, worldTrailPos, trailRotation,
                    trailColor, trailSize);
            }
            else if (emitter.ParticleAtlasRegion != null)
            {
                RenderTexturedParticle(renderer, emitter.ParticleAtlasRegion.AtlasTexture,
                    emitter.ParticleAtlasRegion.SourceRect, worldTrailPos, trailRotation, trailColor, trailSize);
            }
            else if (emitter.ParticleTexture != null)
            {
                RenderTexturedParticle(renderer, emitter.ParticleTexture, null, worldTrailPos, trailRotation,
                    trailColor, trailSize);
            }
            else
            {
                renderer.DrawCircleFilled(worldTrailPos.X, worldTrailPos.Y, trailSize, trailColor);
            }
        }
    }

    private void RenderSubTrail(
        IRenderer renderer,
        SubEmitterConfig cfg,
        Particle particle,
        Color baseColor,
        float baseSize)
    {
        if (particle.TrailPositions == null || particle.TrailFilled == 0)
            return;

        var segmentsToRender = particle.TrailFilled;
        var useLines = cfg.TrailMode == TrailMode.Lines
                       && cfg.ParticleTexture == null
                       && cfg.ParticleAtlasRegion == null
                       && cfg.ParticleFrames == null;

        if (useLines)
        {
            Vector2? prev = null;
            for (int i = 0; i < segmentsToRender; i++)
            {
                var index = (particle.TrailIndex - segmentsToRender + i + particle.TrailPositions.Length)
                            % particle.TrailPositions.Length;
                var worldPos = particle.TrailPositions[index];
                // (segmentsToRender - 1) denominator ensures the newest slot maps to segT = 1.0.
                var segT = segmentsToRender > 1 ? (float)i / (segmentsToRender - 1) : 1f;
                var alpha = MathHelper.Lerp(cfg.TrailTailAlpha, cfg.TrailHeadAlpha, segT);
                var thickness = baseSize * MathHelper.Lerp(cfg.TrailTailSizeRatio, cfg.TrailHeadSizeRatio, segT);

                Color slotColor = particle.TrailColors != null && index < particle.TrailColors.Length
                    ? particle.TrailColors[index]
                    : baseColor;
                var lineColor = new Color(slotColor.R, slotColor.G, slotColor.B, (byte)(slotColor.A * alpha));

                if (prev.HasValue)
                    renderer.DrawLine(prev.Value, worldPos, lineColor, Math.Max(1f, thickness));

                prev = worldPos;
            }

            var headAlpha = cfg.TrailHeadAlpha;
            var headThickness = baseSize * cfg.TrailHeadSizeRatio;
            var headLineColor = new Color(baseColor.R, baseColor.G, baseColor.B, (byte)(baseColor.A * headAlpha));
            if (prev.HasValue)
                renderer.DrawLine(prev.Value, particle.Position, headLineColor, Math.Max(1f, headThickness));

            return;
        }

        for (int i = 0; i < segmentsToRender; i++)
        {
            var index = (particle.TrailIndex - segmentsToRender + i + particle.TrailPositions.Length)
                        % particle.TrailPositions.Length;
            var trailPos = particle.TrailPositions[index];

            // (segmentsToRender - 1) denominator ensures the newest slot maps to segT = 1.0.
            var segT = segmentsToRender > 1 ? (float)i / (segmentsToRender - 1) : 1f;
            var alpha = MathHelper.Lerp(cfg.TrailTailAlpha, cfg.TrailHeadAlpha, segT);
            var trailSize = baseSize * MathHelper.Lerp(cfg.TrailTailSizeRatio, cfg.TrailHeadSizeRatio, segT);

            var trailRotation = particle.TrailRotations != null && index < particle.TrailRotations.Length
                ? particle.TrailRotations[index]
                : particle.Rotation;

            Color slotColor = particle.TrailColors != null && index < particle.TrailColors.Length
                ? particle.TrailColors[index]
                : baseColor;

            var trailColor = new Color(slotColor.R, slotColor.G, slotColor.B, (byte)(slotColor.A * alpha));

            if (cfg.ParticleFrames is { Length: > 0 } frames)
            {
                var storedIndex = particle.TrailFrameIndices != null ? particle.TrailFrameIndices[index] : -1;
                var frameIndex = storedIndex >= 0 && storedIndex < frames.Length
                    ? storedIndex
                    : Math.Clamp((int)((1f - (particle.Life / particle.MaxLife)) * frames.Length), 0,
                        frames.Length - 1);
                var frame = frames[frameIndex];
                RenderTexturedParticle(renderer, frame.AtlasTexture, frame.SourceRect, trailPos, trailRotation,
                    trailColor, trailSize);
            }
            else if (cfg.ParticleAtlasRegion != null)
            {
                RenderTexturedParticle(renderer, cfg.ParticleAtlasRegion.AtlasTexture,
                    cfg.ParticleAtlasRegion.SourceRect, trailPos, trailRotation, trailColor, trailSize);
            }
            else if (cfg.ParticleTexture != null)
            {
                RenderTexturedParticle(renderer, cfg.ParticleTexture, null, trailPos, trailRotation,
                    trailColor, trailSize);
            }
            else
            {
                renderer.DrawCircleFilled(trailPos.X, trailPos.Y, trailSize, trailColor);
            }
        }
    }

    private static Color LerpColor(Color start, Color end, float t)
    {
        return new Color(
            (byte)MathHelper.Lerp(start.R, end.R, t),
            (byte)MathHelper.Lerp(start.G, end.G, t),
            (byte)MathHelper.Lerp(start.B, end.B, t),
            (byte)MathHelper.Lerp(start.A, end.A, t));
    }

    /// <summary>
    /// Returns a deterministic pseudo-random float in [0, 1] for the given integer lattice point.
    /// </summary>
    private static float LatticeNoise(int x, int y)
    {
        var n = x + y * 57;
        n = (n << 13) ^ n;
        return (1f - ((n * (n * n * 15731 + 789221) + 1376312589) & 0x7fffffff) / 1073741824f) * 0.5f + 0.5f;
    }

    /// <summary>
    /// Bilinear value noise in [0, 1]. Smooth-stepped between integer lattice values for
    /// coherent, swirling turbulence rather than per-frame white noise.
    /// </summary>
    private static float ValueNoise2D(float x, float y)
    {
        var ix = (int)MathF.Floor(x);
        var iy = (int)MathF.Floor(y);
        var fx = x - MathF.Floor(x);
        var fy = y - MathF.Floor(y);

        var ux = fx * fx * (3f - 2f * fx);
        var uy = fy * fy * (3f - 2f * fy);

        var a = LatticeNoise(ix, iy);
        var b = LatticeNoise(ix + 1, iy);
        var c = LatticeNoise(ix, iy + 1);
        var d = LatticeNoise(ix + 1, iy + 1);

        return MathHelper.Lerp(MathHelper.Lerp(a, b, ux), MathHelper.Lerp(c, d, ux), uy);
    }

    private readonly struct RenderItem(
        int layer,
        Entity? entity,
        ParticleEmitterComponent? emitter,
        SubEmitterState? subState)
    {
        public int Layer { get; } = layer;
        public Entity? Entity { get; } = entity;
        public ParticleEmitterComponent? Emitter { get; } = emitter;
        public SubEmitterState? SubState { get; } = subState;
    }

    private sealed class SubEmitterState(
        SubEmitterConfig config,
        float turbulenceOffset,
        ParticleEmitterComponent? owner = null)
    {
        public SubEmitterConfig Config { get; } = config;
        public float TurbulenceOffset { get; } = turbulenceOffset;

        /// <summary>
        /// The <see cref="ParticleEmitterComponent"/> that caused this state to be created
        /// (via a birth, death, or lifetime-fraction trigger). Null for states spawned
        /// through the public <see cref="ParticleSystem.Burst"/> API.
        /// Used to propagate <see cref="ParticleEmitterComponent.IsPaused"/> and
        /// <see cref="ParticleEmitterComponent.StopRequested"/> to sub-particles.
        /// </summary>
        public ParticleEmitterComponent? Owner { get; } = owner;

        public List<Particle> Particles { get; } = new();
    }
}