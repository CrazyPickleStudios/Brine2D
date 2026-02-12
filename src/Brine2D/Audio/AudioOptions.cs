using System.ComponentModel.DataAnnotations;

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
    /// Gets or sets the maximum number of audio tracks (channels) available.
    /// Must be between 1 and 32. Defaults to 8.
    /// </summary>
    [Range(1, 32, ErrorMessage = "MaxTracks must be between 1 and 32.")]
    public int MaxTracks { get; set; } = 8;

    /// <summary>
    /// Gets or sets the master volume (0.0 to 1.0). Defaults to 1.0.
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "MasterVolume must be between 0.0 and 1.0.")]
    public float MasterVolume { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the default music volume (0.0 to 1.0). Defaults to 0.7.
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "MusicVolume must be between 0.0 and 1.0.")]
    public float MusicVolume { get; set; } = 0.7f;

    /// <summary>
    /// Gets or sets the default sound effect volume (0.0 to 1.0). Defaults to 1.0.
    /// </summary>
    [Range(0.0, 1.0, ErrorMessage = "SoundVolume must be between 0.0 and 1.0.")]
    public float SoundVolume { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets whether audio is enabled. Set to false to disable all audio.
    /// </summary>
    public bool Enabled { get; set; } = true;
}