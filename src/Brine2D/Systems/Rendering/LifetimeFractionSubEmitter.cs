namespace Brine2D.Systems.Rendering;

/// <summary>
/// Pairs a normalised lifetime fraction with a <see cref="SubEmitterConfig"/> that fires
/// once per particle when the particle's elapsed fraction crosses the threshold.
/// Assign instances to <see cref="ParticleEmitterComponent.LifetimeFractionSubEmitters"/>.
/// </summary>
public sealed class LifetimeFractionSubEmitter
{
    /// <summary>
    /// Normalised lifetime fraction at which the sub-emitter fires, in the range [0, 1].
    /// 0 = immediately at birth (prefer <see cref="ParticleEmitterComponent.BirthSubEmitters"/>
    /// instead), 0.5 = halfway through, 1 = at death (prefer
    /// <see cref="ParticleEmitterComponent.DeathSubEmitters"/> instead).
    /// </summary>
    public float Fraction { get; set; } = 0.5f;

    /// <summary>The sub-emitter config to burst at the particle's world position.</summary>
    public SubEmitterConfig Config { get; set; } = new SubEmitterConfig();
}