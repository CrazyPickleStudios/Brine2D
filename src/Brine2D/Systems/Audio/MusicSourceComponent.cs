using Brine2D.Audio;

namespace Brine2D.Systems.Audio;

/// <summary>
/// Audio source for streaming background music.
/// Only one music track plays at a time — starting music from a second entity
/// replaces the previous one.
/// </summary>
/// <remarks>
/// Spatial audio is not applicable to music. The <see cref="AudioSystem"/>
/// processes spatial properties only for <see cref="SoundEffectSourceComponent"/>.
/// </remarks>
public class MusicSourceComponent : AudioSourceComponent
{
    private float _crossfadeDuration;
    private float _fadeOutDuration;
    private long _loopStartMs;

    public MusicSourceComponent()
    {
        Bus = "music";
    }

    /// <summary>
    /// Music to play (looping background music).
    /// </summary>
    public IMusic? Music { get; set; }

    /// <summary>
    /// When looping, the track loops back to this position in milliseconds instead
    /// of the beginning. This allows an intro section before the loop point.
    /// Negative values are clamped to zero. Default 0.
    /// </summary>
    public long LoopStartMs
    {
        get => _loopStartMs;
        set => _loopStartMs = Math.Max(value, 0);
    }

    /// <summary>
    /// Duration in seconds for crossfading when this music entity replaces another.
    /// Zero means instant switch. Negative values are clamped to zero.
    /// </summary>
    public float CrossfadeDuration
    {
        get => _crossfadeDuration;
        set => _crossfadeDuration = Math.Max(value, 0f);
    }

    /// <summary>
    /// Duration in seconds for fading out when <see cref="AudioSourceComponent.TriggerStop"/> is consumed.
    /// Zero means immediate stop. Negative values are clamped to zero.
    /// </summary>
    public float FadeOutDuration
    {
        get => _fadeOutDuration;
        set => _fadeOutDuration = Math.Max(value, 0f);
    }

    /// <summary>
    /// Set to <see langword="true"/> to seek to <see cref="SeekPositionMs"/> on the next frame.
    /// Consumed and reset to <see langword="false"/> by <c>AudioSystem</c> each update.
    /// </summary>
    public bool TriggerSeek { get; set; }

    /// <summary>
    /// Target position in milliseconds for <see cref="TriggerSeek"/>.
    /// Negative values are clamped to zero by the audio service.
    /// </summary>
    public double SeekPositionMs { get; set; }
}