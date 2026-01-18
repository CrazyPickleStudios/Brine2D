using Brine2D.ECS;

namespace Brine2D.Audio.ECS;

/// <summary>
/// Represents the audio listener (usually attached to the camera or player).
/// There should only be one active listener at a time.
/// </summary>
public class AudioListenerComponent : Component
{
    /// <summary>
    /// Global volume multiplier for all spatial audio (0.0 to 1.0).
    /// </summary>
    public float GlobalSpatialVolume { get; set; } = 1.0f;
}