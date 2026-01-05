using Brine2D.ECS;

namespace Brine2D.Audio.ECS;

/// <summary>
/// Component for playing audio on an entity.
/// Lives in Brine2D.Audio.ECS because it's audio-specific.
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
    /// Volume (0.0 to 1.0).
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
}