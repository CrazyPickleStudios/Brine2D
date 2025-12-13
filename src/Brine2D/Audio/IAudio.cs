namespace Brine2D.Audio;

/// <summary>
///     Defines the audio system contract for playing sound effects and music,
///     controlling master volume, and managing playback state.
/// </summary>
public interface IAudio
{
    /// <summary>
    ///     Gets the currently playing music track, if any.
    /// </summary>
    IMusic? CurrentMusic { get; }

    /// <summary>
    ///     Gets or sets the master volume applied to all audio playback.
    ///     Expected range is 0.0 (silent) to 1.0 (full volume).
    /// </summary>
    float MasterVolume { get; set; }

    /// <summary>
    ///     Pauses the currently playing music track, if any.
    /// </summary>
    void PauseMusic();

    /// <summary>
    ///     Plays a sound effect with optional volume and loop settings.
    /// </summary>
    /// <param name="sound">The sound effect instance to play.</param>
    /// <param name="volume">The volume multiplier for this sound. Default is 1.0.</param>
    /// <param name="loop">Whether the sound should loop continuously. Default is false.</param>
    void Play(ISound sound, float volume = 1.0f, bool loop = false);

    /// <summary>
    ///     Starts playing a music track with optional volume and looping settings.
    ///     If music is already playing, implementations may stop or fade it out before starting the new track.
    /// </summary>
    /// <param name="music">The music track to play.</param>
    /// <param name="volume">The volume multiplier for the music. Default is 1.0.</param>
    /// <param name="loop">Whether the music should loop continuously. Default is true.</param>
    void PlayMusic(IMusic music, float volume = 1.0f, bool loop = true);

    /// <summary>
    ///     Resumes the currently paused music track, if any.
    /// </summary>
    void ResumeMusic();

    /// <summary>
    ///     Stops playback of a specific sound effect instance.
    /// </summary>
    /// <param name="sound">The sound effect to stop.</param>
    void Stop(ISound sound);

    /// <summary>
    ///     Stops playback of all currently playing sound effects.
    /// </summary>
    void StopAll();

    /// <summary>
    ///     Stops the currently playing music track and clears <see cref="CurrentMusic" />.
    /// </summary>
    void StopMusic();
}