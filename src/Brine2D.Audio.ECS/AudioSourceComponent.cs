using Brine2D.ECS;

namespace Brine2D.Audio.ECS;

/// <summary>
/// Component for playing audio on an entity with optional spatial positioning.
/// </summary>
public class AudioSourceComponent : Component
{
    /// <summary>
    /// Sound effect to play (one-shot sounds).
    /// </summary>
    public ISoundEffect? SoundEffect { get; set; }

    /// <summary>
    /// Music to play (looping background music).
    /// </summary>
    public IMusic? Music { get; set; }

    /// <summary>
    /// Base volume (0.0 to 1.0). Will be modulated by spatial audio if enabled.
    /// </summary>
    public float Volume { get; set; } = 1.0f;

    /// <summary>
    /// Whether to play on component enable.
    /// </summary>
    public bool PlayOnEnable { get; set; } = false;

    /// <summary>
    /// Whether to loop the sound (for sound effects).
    /// </summary>
    public bool Loop { get; set; } = false;

    /// <summary>
    /// Loop count (-1 = infinite, 0 = play once).
    /// </summary>
    public int LoopCount { get; set; } = 0;

    /// <summary>
    /// Whether this is currently playing (managed by AudioSystem).
    /// </summary>
    public bool IsPlaying { get; internal set; }

    /// <summary>
    /// Play the attached sound/music immediately.
    /// </summary>
    public bool TriggerPlay { get; set; }

    /// <summary>
    /// Stop the attached sound/music immediately.
    /// </summary>
    public bool TriggerStop { get; set; }
    
    /// <summary>
    /// Enable 2D spatial audio (distance-based volume + stereo panning).
    /// Requires a TransformComponent on the same entity.
    /// </summary>
    public bool EnableSpatialAudio { get; set; } = false;

    /// <summary>
    /// Minimum distance where sound is at full volume (no attenuation).
    /// </summary>
    public float MinDistance { get; set; } = 50f;

    /// <summary>
    /// Maximum distance where sound becomes inaudible (volume = 0).
    /// </summary>
    public float MaxDistance { get; set; } = 500f;

    /// <summary>
    /// Rolloff factor controls how quickly volume decreases with distance.
    /// 0 = no rolloff, 1 = linear, 2 = quadratic (more realistic).
    /// </summary>
    public float RolloffFactor { get; set; } = 1.0f;

    /// <summary>
    /// Stereo panning strength (0.0 = center only, 1.0 = full stereo).
    /// </summary>
    public float SpatialBlend { get; set; } = 1.0f;

    /// <summary>
    /// Calculated volume after spatial processing (read-only for display).
    /// </summary>
    public float SpatialVolume { get; internal set; } = 1.0f;

    /// <summary>
    /// Calculated stereo pan (read-only for display).
    /// </summary>
    public float SpatialPan { get; internal set; } = 0f;
}