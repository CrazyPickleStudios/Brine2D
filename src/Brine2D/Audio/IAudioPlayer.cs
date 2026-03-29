namespace Brine2D.Audio;

/// <summary>
/// Audio playback and volume control.
/// </summary>
public interface IAudioPlayer : IDisposable
{
    /// <summary>Gets or sets the master volume (0.0 to 1.0).</summary>
    float MasterVolume { get; set; }

    /// <summary>Gets or sets the music volume (0.0 to 1.0).</summary>
    float MusicVolume { get; set; }

    /// <summary>Gets or sets the sound effects volume (0.0 to 1.0).</summary>
    float SoundVolume { get; set; }

    /// <summary>Plays a sound effect with optional volume and panning.</summary>
    void PlaySound(ISoundEffect sound, float? volume = null, int loops = 0, float pan = 0f);

    /// <summary>Plays a sound effect and returns the track handle for lifecycle tracking.</summary>
    nint PlaySoundWithTrack(ISoundEffect sound, float? volume = null, int loops = 0, float pan = 0f);

    /// <summary>Stops a specific track by handle.</summary>
    void StopTrack(nint track);

    /// <summary>Stops all playing sound effects.</summary>
    void StopAllSounds();

    /// <summary>Plays background music.</summary>
    void PlayMusic(IMusic music, int loops = -1);

    /// <summary>Stops the currently playing music.</summary>
    void StopMusic();

    /// <summary>Pauses the currently playing music.</summary>
    void PauseMusic();

    /// <summary>Resumes paused music.</summary>
    void ResumeMusic();

    /// <summary>Updates a playing track's spatial audio properties in real-time.</summary>
    void UpdateTrackSpatialAudio(nint track, float volume, float pan);

    /// <summary>Event fired when a track finishes playing (may be called from audio thread).</summary>
    event Action<nint>? OnTrackStopped;
}