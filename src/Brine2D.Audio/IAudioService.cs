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
    /// Plays a sound effect with optional volume and panning.
    /// </summary>
    /// <param name="sound">The sound to play.</param>
    /// <param name="volume">Volume override (0.0 to 1.0), or null to use default.</param>
    /// <param name="loops">Number of times to loop (-1 for infinite, 0 for once).</param>
    /// <param name="pan">Stereo panning (-1.0 = left, 0 = center, 1.0 = right).</param>
    void PlaySound(ISoundEffect sound, float? volume = null, int loops = 0, float pan = 0f);

    /// <summary>
    /// Plays a sound effect and returns the track handle for lifecycle tracking.
    /// </summary>
    nint PlaySoundWithTrack(ISoundEffect sound, float? volume = null, int loops = 0, float pan = 0f);

    /// <summary>
    /// Stops a specific track by handle.
    /// </summary>
    void StopTrack(nint track);

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
    /// Plays background music.
    /// </summary>
    void PlayMusic(IMusic music, int loops = -1);

    /// <summary>
    /// Stops the currently playing music.
    /// </summary>
    void StopMusic();

    /// <summary>
    /// Pauses the currently playing music.
    /// </summary>
    void PauseMusic();

    /// <summary>
    /// Resumes paused music.
    /// </summary>
    void ResumeMusic();

    /// <summary>
    /// Unloads music.
    /// </summary>
    void UnloadMusic(IMusic music);

    /// <summary>
    /// Updates a playing track's spatial audio properties in real-time.
    /// </summary>
    void UpdateTrackSpatialAudio(nint track, float volume, float pan);

    /// <summary>
    /// Event fired when a track finishes playing (may be called from audio thread).
    /// </summary>
    event Action<nint>? OnTrackStopped;
}