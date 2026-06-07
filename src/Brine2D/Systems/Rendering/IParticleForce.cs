using System.Numerics;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// Applies a per-frame velocity delta to a particle during the update pass.
/// Implement this interface and add instances to
/// <see cref="ParticleEmitterComponent.Forces"/> to extend particle physics beyond
/// gravity and turbulence.
/// </summary>
public interface IParticleForce
{
    /// <summary>
    /// Returns the velocity change to add to the particle for this frame.
    /// <paramref name="deltaTime"/> is in seconds. The returned vector is added directly
    /// to the particle's <c>BaseVelocity</c> and is therefore subject to subsequent
    /// damping and speed-over-lifetime scaling.
    /// </summary>
    Vector2 Evaluate(Vector2 particleWorldPosition, float deltaTime);
}