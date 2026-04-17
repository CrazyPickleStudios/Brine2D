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

    /// <summary>Gets whether music is currently playing.</summary>
    bool IsMusicPlaying { get; }

    /// <summary>Gets whether music is currently paused.</summary>
    bool IsMusicPaused { get; }

    /// <summary>
    /// Gets whether music is currently fading out to silence via <see cref="StopMusic"/>.
    /// Returns <see langword="false"/> during a crossfade to another track.
    /// </summary>
    bool IsMusicFadingOut { get; }

    /// <summary>Gets the number of currently active sound-effect tracks (excludes music).</summary>
    int ActiveSoundTrackCount { get; }

    /// <summary>Gets the maximum number of concurrent sound-effect tracks.</summary>
    int MaxSoundTracks { get; }

    /// <summary>
    /// Gets the current music playback position in milliseconds, or -1 if no music
    /// is playing. Paused tracks report the position at which they were paused.
    /// </summary>
    double MusicPositionMs { get; }

    /// <summary>
    /// Gets the total duration of the currently loaded music in milliseconds,
    /// or -1 if no music is playing or the duration is unknown (e.g. streaming sources).
    /// </summary>
    double MusicDurationMs { get; }

    /// <summary>Plays a sound effect and returns the track handle for lifecycle tracking.</summary>
    /// <param name="sound">The sound effect to play.</param>
    /// <param name="volume">
    /// Per-sound volume multiplier (0.0 to 1.0, default 1.0).
    /// The final gain applied to the track is <paramref name="volume"/> × <see cref="SoundVolume"/>.
    /// </param>
    /// <param name="loops">Number of times to loop (0 = play once, -1 = infinite).</param>
    /// <param name="pan">Stereo pan (-1.0 left to 1.0 right).</param>
    /// <param name="pitch">Playback speed multiplier (0.25 to 4.0, default 1.0).</param>
    /// <param name="priority">
    /// Track priority for eviction. When all tracks are in use the lowest-priority
    /// track is evicted if the new sound's priority is equal or higher.
    /// </param>
    /// <param name="bus">
    /// Optional bus name to tag the track with atomically at creation time.
    /// When non-<see langword="null"/>, the track is tagged before playback begins,
    /// eliminating the window between <see cref="PlaySound"/> and <see cref="TagTrack"/>
    /// where a <see cref="PauseBus"/> or <see cref="StopBus"/> call could miss the track.
    /// </param>
    /// <returns>
    /// A track handle for use with <see cref="StopTrack"/>, <see cref="PauseTrack"/>,
    /// <see cref="ResumeTrack"/>, <see cref="IsTrackAlive"/>, <see cref="TagTrack"/>,
    /// <see cref="SetTrackVolume"/>, <see cref="SetTrackVolumeAndPan"/>,
    /// <see cref="SetTrackPan"/> and <see cref="SetTrackPitch"/>,
    /// or <see cref="nint.Zero"/> if the sound could not be played.
    /// </returns>
    nint PlaySound(ISoundEffect sound, float volume = 1.0f, int loops = 0, float pan = 0f, float pitch = 1f, int priority = 0, string? bus = null);

    /// <summary>
    /// Pauses a specific sound-effect track. The track remains alive and can be
    /// resumed with <see cref="ResumeTrack"/>. Does nothing for unknown or completed tracks.
    /// </summary>
    void PauseTrack(nint track);

    /// <summary>
    /// Resumes a paused sound-effect track. Does nothing for unknown, completed,
    /// or non-paused tracks.
    /// </summary>
    void ResumeTrack(nint track);

    /// <summary>
    /// Returns whether the given sound-effect track handle is still actively playing.
    /// Returns <see langword="false"/> for <see cref="nint.Zero"/>, unknown, or completed tracks.
    /// </summary>
    bool IsTrackAlive(nint track);

    /// <summary>
    /// Tags a track with a bus name for group operations (<see cref="SetBusVolume"/>,
    /// <see cref="PauseBus"/>, <see cref="ResumeBus"/>, <see cref="StopBus"/>).
    /// A track can have multiple tags. Does nothing for unknown or completed tracks.
    /// </summary>
    /// <param name="track">The track handle returned by <see cref="PlaySound"/>.</param>
    /// <param name="bus">Bus name (e.g. <c>"sfx"</c>, <c>"ui"</c>, <c>"ambient"</c>).</param>
    void TagTrack(nint track, string bus);

    /// <summary>
    /// Sets the volume multiplier for all tracks tagged with <paramref name="bus"/>.
    /// This is an additional multiplier on top of per-track gain and
    /// <see cref="SoundVolume"/>/<see cref="MusicVolume"/>.
    /// </summary>
    /// <param name="bus">Bus name to adjust.</param>
    /// <param name="volume">Volume multiplier (0.0 to 1.0). Values are clamped.</param>
    void SetBusVolume(string bus, float volume);

    /// <summary>
    /// Gets the current volume multiplier for the given bus, or 1.0 if the bus
    /// volume has not been explicitly set via <see cref="SetBusVolume"/>.
    /// </summary>
    /// <param name="bus">Bus name to query.</param>
    float GetBusVolume(string bus);

    /// <summary>
    /// Pauses all tracks tagged with <paramref name="bus"/>. Paused tracks remain
    /// alive and can be resumed with <see cref="ResumeBus"/>.
    /// </summary>
    void PauseBus(string bus);

    /// <summary>
    /// Resumes all tracks tagged with <paramref name="bus"/>.
    /// </summary>
    void ResumeBus(string bus);

    /// <summary>
    /// Stops all tracks tagged with <paramref name="bus"/> with optional fade-out.
    /// </summary>
    /// <param name="bus">Bus name to stop.</param>
    /// <param name="fadeOutSeconds">
    /// Fade-out duration in seconds. Zero means immediate stop.
    /// </param>
    /// <remarks>
    /// When <paramref name="fadeOutSeconds"/> is greater than zero and the bus contains the
    /// active music track, the music fade is handled by the managed crossfade state machine.
    /// Non-music sound-effect tracks on the same bus are stopped immediately in this case
    /// because the native tag-based fade would conflict with the managed music fade.
    /// </remarks>
    void StopBus(string bus, float fadeOutSeconds = 0f);

    /// <summary>
    /// Returns whether the given bus is currently paused via <see cref="PauseBus"/>.
    /// </summary>
    bool IsBusPaused(string bus);

    /// <summary>Plays background music.</summary>
    /// <param name="loopStartMs">
    /// When looping, the track loops back to this position in milliseconds instead
    /// of the beginning. This allows an intro section before the loop point. Default 0.
    /// </param>
    /// <param name="bus">
    /// Bus name to tag the music track with. Defaults to <c>"music"</c> when <see langword="null"/>.
    /// </param>
    void PlayMusic(IMusic music, int loops = -1, long loopStartMs = 0, string? bus = null);

    /// <summary>
    /// Crossfades from the currently playing music to <paramref name="music"/> over
    /// <paramref name="duration"/> seconds. If no music is playing or
    /// <paramref name="duration"/> is &lt;= 0, behaves identically to <see cref="PlayMusic"/>.
    /// Call <see cref="Update"/> each frame to advance the fade.
    /// </summary>
    /// <param name="loopStartMs">
    /// When looping, the track loops back to this position in milliseconds instead
    /// of the beginning. This allows an intro section before the loop point. Default 0.
    /// </param>
    /// <param name="bus">
    /// Bus name to tag the music track with. Defaults to <c>"music"</c> when <see langword="null"/>.
    /// </param>
    void CrossfadeMusic(IMusic music, float duration, int loops = -1, long loopStartMs = 0, string? bus = null);

    /// <summary>
    /// Per-frame update that processes deferred track cleanup and advances any
    /// in-progress music crossfade. Call once per frame with the frame delta time.
    /// </summary>
    void Update(float deltaTime);

    /// <summary>
    /// Stops the currently playing music. When <paramref name="fadeDuration"/> is
    /// greater than zero the music fades out over that many seconds; call
    /// <see cref="Update"/> each frame to advance the fade.
    /// </summary>
    /// <remarks>
    /// The fade shares internal state with <see cref="CrossfadeMusic"/>. Starting a
    /// crossfade or calling <see cref="PlayMusic"/> while a fade-out is in progress
    /// will cancel the fade immediately.
    /// </remarks>
    /// <param name="fadeDuration">
    /// Fade-out duration in seconds. Zero or negative means immediate stop.
    /// </param>
    void StopMusic(float fadeDuration = 0f);

    /// <summary>Stops a specific track by handle.</summary>
    void StopTrack(nint track);

    /// <summary>
    /// Stops all currently playing sound-effect tracks. Music is not affected.
    /// </summary>
    void StopAllSounds();

    /// <summary>
    /// Pauses all currently playing sound-effect tracks. Music is not affected.
    /// Paused tracks remain alive and can be resumed with <see cref="ResumeAllSounds"/>.
    /// </summary>
    void PauseAllSounds();

    /// <summary>
    /// Resumes all paused sound-effect tracks. Music is not affected.
    /// </summary>
    void ResumeAllSounds();

    /// <summary>Pauses the currently playing music.</summary>
    void PauseMusic();

    /// <summary>Resumes paused music.</summary>
    void ResumeMusic();

    /// <summary>
    /// Seeks the currently playing music to <paramref name="positionMs"/> milliseconds.
    /// Does nothing if no music is playing.
    /// </summary>
    /// <param name="positionMs">Target position in milliseconds (clamped to ≥ 0).</param>
    void SeekMusic(double positionMs);

    /// <summary>Updates a playing track's volume without changing its pan.</summary>
    /// <param name="track">The track handle returned by <see cref="PlaySound"/>.</param>
    /// <param name="volume">
    /// Per-sound volume multiplier (0.0 to 1.0). The final gain applied is
    /// <paramref name="volume"/> × <see cref="SoundVolume"/>.
    /// </param>
    void SetTrackVolume(nint track, float volume);

    /// <summary>Updates a playing track's volume and stereo pan in real-time.</summary>
    /// <param name="track">The track handle returned by <see cref="PlaySound"/>.</param>
    /// <param name="volume">
    /// Per-sound volume multiplier. The final gain applied is
    /// <paramref name="volume"/> × <see cref="SoundVolume"/>.
    /// </param>
    /// <param name="pan">Stereo pan (-1.0 left to 1.0 right).</param>
    void SetTrackVolumeAndPan(nint track, float volume, float pan);

    /// <summary>Updates a playing track's stereo pan without changing its volume.</summary>
    /// <param name="track">The track handle returned by <see cref="PlaySound"/>.</param>
    /// <param name="pan">Stereo pan (-1.0 left to 1.0 right).</param>
    void SetTrackPan(nint track, float pan);

    /// <summary>Sets the playback speed (pitch) of an active track.</summary>
    /// <param name="track">The track handle returned by <see cref="PlaySound"/>.</param>
    /// <param name="pitch">Playback speed multiplier (0.25 to 4.0). Values are clamped.</param>
    void SetTrackPitch(nint track, float pitch);

    /// <summary>
    /// Sets the per-track volume multiplier for the current music track.
    /// The final music gain is <see cref="MusicVolume"/> × <paramref name="volume"/>.
    /// Does nothing during a crossfade (the crossfade drives gains directly).
    /// </summary>
    /// <param name="volume">Volume multiplier (0.0 to 1.0, default 1.0). Values are clamped.</param>
    void SetMusicTrackVolume(float volume);

    /// <summary>
    /// Sets the playback speed (pitch) of the current music track.
    /// Does nothing if no music is playing.
    /// </summary>
    /// <param name="pitch">Playback speed multiplier (0.25 to 4.0). Values are clamped.</param>
    void SetMusicPitch(float pitch);
}