using Brine2D.ECS;

namespace Brine2D.Systems.Audio;

/// <summary>
/// Abstract base for audio source components. Use <see cref="SoundEffectSourceComponent"/>
/// for one-shot/looping sound effects or <see cref="MusicSourceComponent"/> for streaming music.
/// </summary>
public abstract class AudioSourceComponent : Component
{
    private float _volume = 1.0f;
    private float _pitch = 1.0f;
    private int _loopCount;

    /// <summary>
    /// Base volume (0.0 to 1.0). Values outside the range are clamped.
    /// </summary>
    public float Volume
    {
        get => _volume;
        set => _volume = Math.Clamp(value, 0f, 1f);
    }

    /// <summary>
    /// Playback speed multiplier (0.25 to 4.0, default 1.0).
    /// Values outside the range are clamped.
    /// </summary>
    public float Pitch
    {
        get => _pitch;
        set => _pitch = Math.Clamp(value, 0.25f, 4f);
    }

    /// <summary>
    /// Track priority for eviction when all tracks are in use.
    /// Higher values are less likely to be evicted.
    /// </summary>
    public int Priority { get; set; }

    /// <summary>
    /// Audio bus name for group operations (per-bus volume, pause, resume, stop).
    /// Defaults to <c>"sfx"</c> for sound effects and <c>"music"</c> for music.
    /// </summary>
    public string Bus { get; set; } = "sfx";

    /// <summary>
    /// Whether to automatically play when the component becomes enabled.
    /// </summary>
    /// <remarks>
    /// This triggers both when the entity first enters the <see cref="AudioSystem"/> query
    /// with <see cref="Component.IsEnabled"/> already <see langword="true"/> and when a
    /// previously disabled component is re-enabled. Set <see cref="Component.IsEnabled"/>
    /// to <see langword="false"/> before attaching the component to defer playback.
    /// </remarks>
    public bool PlayOnEnable { get; set; }

    /// <summary>
    /// Number of times to loop (-1 = infinite, 0 = play once).
    /// Values below -1 are clamped to -1.
    /// </summary>
    public int LoopCount
    {
        get => _loopCount;
        set => _loopCount = Math.Max(value, -1);
    }

    /// <summary>
    /// Whether this is currently playing (managed by AudioSystem).
    /// </summary>
    public bool IsPlaying { get; internal set; }

    /// <summary>
    /// Set to <see langword="true"/> to play the attached sound/music on the next frame.
    /// Consumed and reset to <see langword="false"/> by <c>AudioSystem</c> each update.
    /// If both <see cref="TriggerPlay"/> and <see cref="TriggerStop"/> are set, both are
    /// cleared and no action is taken.
    /// </summary>
    public bool TriggerPlay { get; set; }

    /// <summary>
    /// Set to <see langword="true"/> to stop the attached sound/music on the next frame.
    /// Consumed and reset to <see langword="false"/> by <c>AudioSystem</c> each update.
    /// If both <see cref="TriggerPlay"/> and <see cref="TriggerStop"/> are set, both are
    /// cleared and no action is taken.
    /// </summary>
    public bool TriggerStop { get; set; }

    /// <summary>
    /// Set to <see langword="true"/> by the audio system when playback ends naturally
    /// (track completed or evicted), not by <see cref="TriggerStop"/> or disabling.
    /// Reset to <see langword="false"/> when new playback starts via <see cref="TriggerPlay"/>.
    /// </summary>
    public bool PlaybackEnded { get; internal set; }

    /// <summary>
    /// Set to <see langword="true"/> to pause the sound/music on the next frame.
    /// Consumed and reset to <see langword="false"/> by <c>AudioSystem</c> each update.
    /// If both <see cref="TriggerPause"/> and <see cref="TriggerResume"/> are set, both are
    /// cleared and no action is taken.
    /// </summary>
    public bool TriggerPause { get; set; }

    /// <summary>
    /// Set to <see langword="true"/> to resume a paused sound/music on the next frame.
    /// Consumed and reset to <see langword="false"/> by <c>AudioSystem</c> each update.
    /// If both <see cref="TriggerPause"/> and <see cref="TriggerResume"/> are set, both are
    /// cleared and no action is taken.
    /// </summary>
    public bool TriggerResume { get; set; }

    /// <summary>
    /// Whether this is currently paused (managed by AudioSystem).
    /// </summary>
    public bool IsPaused { get; internal set; }
}