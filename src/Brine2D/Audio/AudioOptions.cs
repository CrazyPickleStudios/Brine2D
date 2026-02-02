namespace Brine2D.Audio;

/// <summary>
/// Configuration options for the audio system.
/// </summary>
public class AudioOptions
{
    /// <summary>
    /// Configuration section name for binding from JSON.
    /// </summary>
    public const string SectionName = "Audio";
    
    /// <summary>
    /// Gets or sets the maximum number of concurrent audio tracks.
    /// </summary>
    public int MaxTracks { get; set; } = 32;
    
    /// <summary>
    /// Gets or sets the master volume (0.0 to 10.0, where 1.0 is normal volume).
    /// </summary>
    public float MasterVolume { get; set; } = 1.0f;
    
    /// <summary>
    /// Gets or sets the default music volume (0.0 to 10.0, where 1.0 is normal volume).
    /// </summary>
    public float MusicVolume { get; set; } = 1.0f;
    
    /// <summary>
    /// Gets or sets the default sound effects volume (0.0 to 10.0, where 1.0 is normal volume).
    /// </summary>
    public float SoundVolume { get; set; } = 1.0f;
    
    /// <summary>
    /// Gets or sets whether audio is enabled. Set to false to disable all audio.
    /// </summary>
    public bool Enabled { get; set; } = true;
}