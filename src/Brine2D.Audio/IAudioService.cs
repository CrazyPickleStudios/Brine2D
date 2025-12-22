namespace Brine2D.Audio;

/// <summary>
/// Audio playback and management service.
/// </summary>
public interface IAudioService : IDisposable
{
    /// <summary>
    /// Gets or sets the master volume (0.0 to 1.0).
    /// </summary>
    float MasterVolume { get; set; }

    /// <summary>
    /// Gets or sets the music volume (0.0 to 1.0).
    /// </summary>
    float MusicVolume { get; set; }

    /// <summary>
    /// Gets or sets the sound effects volume (0.0 to 1.0).
    /// </summary>
    float SoundVolume { get; set; }

    /// <summary>
    /// Loads a sound effect from a file.
    /// </summary>
    Task<ISoundEffect> LoadSoundAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Plays a sound effect.
    /// </summary>
    /// <param name="sound">The sound to play.</param>
    /// <param name="volume">Volume override (0.0 to 1.0), or null to use default.</param>
    /// <param name="loops">Number of times to loop (-1 for infinite, 0 for once).</param>
    void PlaySound(ISoundEffect sound, float? volume = null, int loops = 0);

    /// <summary>
    /// Stops all playing sound effects.
    /// </summary>
    void StopAllSounds();

    /// <summary>
    /// Unloads a sound effect.
    /// </summary>
    void UnloadSound(ISoundEffect sound);

    /// <summary>
    /// Loads music from a file.
    /// </summary>
    Task<IMusic> LoadMusicAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Plays music.
    /// </summary>
    /// <param name="music">The music to play.</param>
    /// <param name="loops">Number of times to loop (-1 for infinite).</param>
    /// <param name="fadeInMs">Fade in duration in milliseconds (0 for no fade).</param>
    void PlayMusic(IMusic music, int loops = -1, int fadeInMs = 0);

    /// <summary>
    /// Pauses the currently playing music.
    /// </summary>
    void PauseMusic();

    /// <summary>
    /// Resumes paused music.
    /// </summary>
    void ResumeMusic();

    /// <summary>
    /// Stops the currently playing music.
    /// </summary>
    /// <param name="fadeOutMs">Fade out duration in milliseconds (0 for immediate stop).</param>
    void StopMusic(int fadeOutMs = 0);

    /// <summary>
    /// Gets whether music is currently playing.
    /// </summary>
    bool IsMusicPlaying { get; }

    /// <summary>
    /// Gets whether music is currently paused.
    /// </summary>
    bool IsMusicPaused { get; }

    /// <summary>
    /// Unloads music.
    /// </summary>
    void UnloadMusic(IMusic music);
}