using Brine2D.ECS;

namespace Brine2D.Systems.Audio;

/// <summary>
/// Represents the audio listener (usually attached to the camera or player).
/// There should only be one active listener at a time.
/// </summary>
/// <remarks>
/// Listener orientation is derived from the entity's <c>TransformComponent.Rotation</c>.
/// Spatial panning is calculated relative to the listener's facing direction.
/// </remarks>
public class AudioListenerComponent : Component
{
    private float _globalSpatialVolume = 1.0f;

    /// <summary>
    /// Global volume multiplier for all spatial audio (0.0 to 1.0).
    /// Values outside the range are clamped.
    /// </summary>
    public float GlobalSpatialVolume
    {
        get => _globalSpatialVolume;
        set => _globalSpatialVolume = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Speed of sound in world units per second, used for Doppler calculations.
    /// Adjust to match your game's world scale. Clamped to [1, ∞). Default 343.
    /// </summary>
    public float SpeedOfSound
    {
        get => field;
        set => field = Math.Max(value, 1f);
    } = 343f;
}