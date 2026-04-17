using Brine2D.Audio;

namespace Brine2D.Systems.Audio;

/// <summary>
/// Audio source for one-shot or looping sound effects.
/// Supports spatial audio when <see cref="EnableSpatialAudio"/> is set
/// and a TransformComponent is present on the same entity.
/// </summary>
/// <remarks>
/// Multiple tracks can play concurrently on the same entity. Triggering play while
/// sounds are already playing adds a new overlapping track. Use
/// <see cref="AudioSourceComponent.TriggerStop"/> to stop all active tracks,
/// or <see cref="MaxConcurrentInstances"/> to limit how many instances of the
/// same sound can overlap globally.
/// </remarks>
public class SoundEffectSourceComponent : AudioSourceComponent
{
    private float _minDistance = 50f;
    private float _maxDistance = 500f;
    private float _rolloffFactor = 1.0f;
    private float _spatialBlend = 1.0f;

    /// <summary>
    /// Sound effect to play.
    /// </summary>
    public ISoundEffect? SoundEffect { get; set; }

    /// <summary>
    /// Enable 2D spatial audio (distance-based volume + stereo panning).
    /// Requires a TransformComponent on the same entity.
    /// </summary>
    public bool EnableSpatialAudio { get; set; }

    /// <summary>
    /// Minimum distance where sound is at full volume (no attenuation).
    /// Clamped to [0, <see cref="MaxDistance"/>]. Setting a value above
    /// <see cref="MaxDistance"/> raises <see cref="MaxDistance"/> to match.
    /// </summary>
    public float MinDistance
    {
        get => _minDistance;
        set
        {
            _minDistance = Math.Max(value, 0f);
            if (_minDistance > _maxDistance)
                _maxDistance = _minDistance;
        }
    }

    /// <summary>
    /// Maximum distance where sound becomes inaudible (volume = 0).
    /// Clamped to [0, ∞). Setting a value below <see cref="MinDistance"/>
    /// lowers <see cref="MinDistance"/> to match.
    /// </summary>
    public float MaxDistance
    {
        get => _maxDistance;
        set
        {
            _maxDistance = Math.Max(value, 0f);
            if (_maxDistance < _minDistance)
                _minDistance = _maxDistance;
        }
    }

    /// <summary>
    /// Rolloff factor controls how quickly volume decreases with distance.
    /// 0 = no rolloff, 1 = linear, 2 = quadratic (more realistic).
    /// Negative values are clamped to zero.
    /// </summary>
    public float RolloffFactor
    {
        get => _rolloffFactor;
        set => _rolloffFactor = Math.Max(value, 0f);
    }

    /// <summary>
    /// Stereo panning strength (0.0 = center only, 1.0 = full stereo).
    /// Values outside the range are clamped.
    /// </summary>
    public float SpatialBlend
    {
        get => _spatialBlend;
        set => _spatialBlend = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Calculated volume after spatial processing (managed by AudioSystem).
    /// </summary>
    public float SpatialVolume { get; internal set; } = 1.0f;

    /// <summary>
    /// Calculated stereo pan after spatial processing (managed by AudioSystem).
    /// </summary>
    public float SpatialPan { get; internal set; }

    /// <summary>
    /// Doppler effect intensity multiplier. 0 = disabled, 1 = realistic.
    /// Higher values exaggerate the pitch shift. Clamped to [0, 5]. Default 0 (disabled).
    /// Requires <see cref="EnableSpatialAudio"/> to be set.
    /// </summary>
    public float DopplerFactor
    {
        get => field;
        set => field = Math.Clamp(value, 0f, 5f);
    }

    /// <summary>
    /// Calculated pitch multiplier from Doppler processing (managed by AudioSystem).
    /// Combined with <see cref="AudioSourceComponent.Pitch"/> when applying to the track.
    /// </summary>
    public float SpatialPitch { get; internal set; } = 1.0f;

    /// <summary>
    /// Maximum number of concurrent tracks across all entities playing the same
    /// <see cref="SoundEffect"/>. Zero means unlimited. Default 0.
    /// </summary>
    /// <remarks>
    /// The limit used is the value on the entity requesting playback. If two entities
    /// reference the same sound with different limits, the effective cap depends on
    /// which entity triggers play first in a given frame.
    /// </remarks>
    public int MaxConcurrentInstances
    {
        get => field;
        set => field = Math.Max(value, 0);
    }

    /// <summary>
    /// Maximum random pitch offset applied each time the sound plays.
    /// The actual pitch is jittered by ±<see cref="PitchVariation"/> around
    /// <see cref="AudioSourceComponent.Pitch"/>. Clamped to [0, 1]. Default 0.
    /// </summary>
    public float PitchVariation
    {
        get => field;
        set => field = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Maximum random volume reduction applied each time the sound plays.
    /// The actual volume is reduced by up to this fraction of the computed spatial
    /// volume. Clamped to [0, 1]. Default 0.
    /// </summary>
    public float VolumeVariation
    {
        get => field;
        set => field = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Duration in seconds for fading in when the sound starts playing.
    /// Zero means instant full volume. Clamped to [0, ∞). Default 0.
    /// </summary>
    public float FadeInDuration
    {
        get => field;
        set => field = Math.Max(value, 0f);
    }

    /// <summary>
    /// Duration in seconds for fading out when <see cref="AudioSourceComponent.TriggerStop"/> is consumed.
    /// Zero means immediate stop. Clamped to [0, ∞). Default 0.
    /// Setting <see cref="AudioSourceComponent.TriggerStop"/> a second time during an active
    /// fade-out forces an immediate stop.
    /// </summary>
    public float FadeOutDuration
    {
        get => field;
        set => field = Math.Max(value, 0f);
    }

    /// <summary>
    /// Set to <see langword="true"/> to stop the oldest overlapping track on the next frame.
    /// Consumed and reset to <see langword="false"/> by <c>AudioSystem</c> each update.
    /// Does nothing if no tracks are active on this entity.
    /// </summary>
    public bool TriggerStopOldest { get; set; }
}