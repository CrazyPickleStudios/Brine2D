using Brine2D.Audio;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Brine2D.SDL.Audio;

/// <summary>
/// SDL3_mixer implementation of audio service with spatial audio support and proper callbacks.
/// </summary>
/// <remarks>
/// This service provides comprehensive audio playback capabilities including:
/// <list type="bullet">
/// <item>Sound effect playback with spatial audio (panning)</item>
/// <item>Music streaming with loop support</item>
/// <item>Individual volume controls for master, music, and sound effects</item>
/// <item>Track-based audio management with callbacks</item>
/// <item>Proper resource disposal and cleanup</item>
/// </list>
/// </remarks>
public class SDL3AudioService : IAudioService
{
    /// <summary>
    /// Logger instance for audio service operations.
    /// </summary>
    private readonly ILogger<SDL3AudioService> _logger;

    /// <summary>
    /// Logger factory for creating loggers for child audio objects.
    /// </summary>
    private readonly ILoggerFactory _loggerFactory;

    /// <summary>
    /// Collection of all currently loaded sound effects.
    /// </summary>
    private readonly List<SDL3SoundEffect> _loadedSounds = new();

    /// <summary>
    /// Collection of all currently loaded music tracks.
    /// </summary>
    private readonly List<SDL3Music> _loadedMusic = new();

    /// <summary>
    /// Collection of all active audio tracks (both sounds and music).
    /// </summary>
    private readonly List<nint> _tracks = new();

    /// <summary>
    /// Handle to the SDL3 mixer device.
    /// </summary>
    private nint _mixer;

    /// <summary>
    /// Indicates whether this instance has been disposed.
    /// </summary>
    private bool _disposed;

    /// <summary>
    /// Internal storage for sound effect volume level.
    /// </summary>
    private float _soundVolume = 1.0f;

    /// <summary>
    /// Gets or sets the master volume for all audio output.
    /// </summary>
    /// <value>
    /// The master gain value. Valid range is 0.0 to 10.0, where 1.0 is normal volume.
    /// Values are automatically clamped to this range when set.
    /// </value>
    /// <remarks>
    /// This property controls the overall volume for all audio played through the mixer,
    /// affecting both music and sound effects. Changes take effect immediately.
    /// </remarks>
    public float MasterVolume
    {
        get => SDL3.Mixer.GetMasterGain(_mixer);
        set => SDL3.Mixer.SetMasterGain(_mixer, Math.Clamp(value, 0f, 10f));
    }

    /// <summary>
    /// Gets or sets the volume level for music playback.
    /// </summary>
    /// <value>
    /// The music volume multiplier. Default is 1.0. Valid range is 0.0 to 10.0.
    /// </value>
    /// <remarks>
    /// This volume is applied when a new music track starts playing.
    /// Changes to this property do not affect currently playing music.
    /// </remarks>
    public float MusicVolume { get; set; } = 1.0f;

    /// <summary>
    /// Gets or sets the volume level for sound effect playback.
    /// </summary>
    /// <value>
    /// The sound volume multiplier. Valid range is 0.0 to 10.0, where 1.0 is normal volume.
    /// Values are automatically clamped to this range when set.
    /// </value>
    /// <remarks>
    /// This volume is multiplied with individual sound volumes when playing sound effects.
    /// </remarks>
    public float SoundVolume
    {
        get => _soundVolume;
        set => _soundVolume = Math.Clamp(value, 0f, 10f);
    }

    /// <summary>
    /// Gets a value indicating whether music is currently playing.
    /// </summary>
    /// <value>
    /// <c>true</c> if music is playing; otherwise, <c>false</c>.
    /// </value>
    public bool IsMusicPlaying { get; private set; }

    /// <summary>
    /// Gets a value indicating whether music playback is currently paused.
    /// </summary>
    /// <value>
    /// <c>true</c> if music is paused; otherwise, <c>false</c>.
    /// </value>
    public bool IsMusicPaused { get; private set; }

    /// <summary>
    /// Handle to the currently playing music track.
    /// </summary>
    private nint _currentMusicTrack;

    /// <summary>
    /// Event raised when an audio track stops playing.
    /// </summary>
    /// <remarks>
    /// This event is triggered from the SDL audio thread callback when a track completes.
    /// Subscribers should be thread-safe as this may be called from a different thread.
    /// </remarks>
    public event Action<nint>? OnTrackStopped;

    /// <summary>
    /// Dictionary mapping track handles to their associated callback data.
    /// </summary>
    private readonly Dictionary<nint, TrackCallbackData> _trackCallbacks = new();

    /// <summary>
    /// Unmanaged callback delegate for track stopped events.
    /// </summary>
    /// <param name="userdata">User data pointer containing the track handle.</param>
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void TrackStoppedCallback(nint userdata);

    /// <summary>
    /// Stores callback information for an audio track.
    /// </summary>
    private struct TrackCallbackData
    {
        /// <summary>
        /// The callback delegate to invoke when the track stops.
        /// </summary>
        public TrackStoppedCallback Callback;

        /// <summary>
        /// Handle to the associated audio track.
        /// </summary>
        public nint Track;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="SDL3AudioService"/> class.
    /// </summary>
    /// <param name="logger">Logger for audio service operations.</param>
    /// <param name="loggerFactory">Factory for creating loggers for child objects.</param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="logger"/> or <paramref name="loggerFactory"/> is null.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when SDL3_mixer initialization or mixer device creation fails.
    /// </exception>
    public SDL3AudioService(ILogger<SDL3AudioService> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

        Initialize();
    }

    /// <summary>
    /// Initializes the SDL3_mixer audio system and creates the mixer device.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when SDL3_mixer initialization fails or mixer device creation fails.
    /// </exception>
    private void Initialize()
    {
        _logger.LogInformation("Initializing SDL3_mixer audio system");

        if (!SDL3.Mixer.Init())
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to initialize SDL3_mixer: {Error}", error);
            throw new InvalidOperationException($"Failed to initialize SDL3_mixer: {error}");
        }

        _mixer = SDL3.Mixer.CreateMixerDevice(SDL3.SDL.AudioDeviceDefaultPlayback, IntPtr.Zero);
        if (_mixer == IntPtr.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create mixer device: {Error}", error);
            throw new InvalidOperationException($"Failed to create mixer device: {error}");
        }

        var specPtr = Marshal.AllocHGlobal(Marshal.SizeOf<SDL3.SDL.AudioSpec>());
        try
        {
            if (SDL3.Mixer.GetMixerFormat(_mixer, specPtr))
            {
                var spec = Marshal.PtrToStructure<SDL3.SDL.AudioSpec>(specPtr);
                _logger.LogInformation("SDL3_mixer initialized: {Freq} Hz, {Channels} channels",
                    spec.Freq, spec.Channels);
            }
            else
            {
                _logger.LogInformation("SDL3_mixer initialized successfully");
            }
        }
        finally
        {
            Marshal.FreeHGlobal(specPtr);
        }
    }

    /// <summary>
    /// Asynchronously loads a sound effect from the specified file path.
    /// </summary>
    /// <param name="path">The file path to the sound effect.</param>
    /// <param name="cancellationToken">Token to cancel the loading operation.</param>
    /// <returns>A task that represents the asynchronous load operation. The task result contains the loaded sound effect.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file at <paramref name="path"/> does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when SDL fails to load the audio file.</exception>
    public async Task<ISoundEffect> LoadSoundAsync(string path, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => LoadSound(path), cancellationToken);
    }

    /// <summary>
    /// Synchronously loads a sound effect from the specified file path.
    /// </summary>
    /// <param name="path">The file path to the sound effect.</param>
    /// <returns>The loaded sound effect.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file at <paramref name="path"/> does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when SDL fails to load the audio file.</exception>
    private ISoundEffect LoadSound(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Sound file not found: {path}");

        _logger.LogInformation("Loading sound: {Path}", path);

        var audio = SDL3.Mixer.LoadAudio(_mixer, path, true);

        if (audio == IntPtr.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to load sound {Path}: {Error}", path, error);
            throw new InvalidOperationException($"Failed to load sound: {error}");
        }

        var sound = new SDL3SoundEffect(path, audio, _loggerFactory.CreateLogger<SDL3SoundEffect>());
        _loadedSounds.Add(sound);

        _logger.LogDebug("Sound loaded: {Path}", path);
        return sound;
    }

    /// <summary>
    /// Plays a sound effect with optional volume, looping, and stereo panning.
    /// </summary>
    /// <param name="sound">The sound effect to play.</param>
    /// <param name="volume">Optional volume override (0.0 to 10.0). If null, uses default volume.</param>
    /// <param name="loops">Number of times to loop. 0 for no loop, -1 for infinite loop.</param>
    /// <param name="pan">Stereo panning (-1.0 for full left, 0.0 for center, 1.0 for full right).</param>
    /// <remarks>
    /// This method calls <see cref="PlaySoundWithTrack"/> internally but does not return the track handle.
    /// </remarks>
    public void PlaySound(ISoundEffect sound, float? volume = null, int loops = 0, float pan = 0f)
    {
        PlaySoundWithTrack(sound, volume, loops, pan);
    }

    /// <summary>
    /// Plays a sound effect with optional volume, looping, and stereo panning, returning the track handle.
    /// </summary>
    /// <param name="sound">The sound effect to play.</param>
    /// <param name="volume">Optional volume override (0.0 to 10.0). If null, uses default volume.</param>
    /// <param name="loops">Number of times to loop. 0 for no loop, -1 for infinite loop.</param>
    /// <param name="pan">Stereo panning (-1.0 for full left, 0.0 for center, 1.0 for full right).</param>
    /// <returns>
    /// Handle to the created audio track, or <see cref="nint.Zero"/> if playback failed.
    /// This handle can be used to stop or update the track's spatial audio.
    /// </returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="sound"/> is not an SDL3SoundEffect.</exception>
    /// <remarks>
    /// The final volume is calculated as: (volume ?? 1.0) * SoundVolume.
    /// The pan value is automatically clamped to the range [-1.0, 1.0].
    /// A callback is registered to handle track completion events.
    /// </remarks>
    public nint PlaySoundWithTrack(ISoundEffect sound, float? volume = null, int loops = 0, float pan = 0f)
    {
        if (sound is not SDL3SoundEffect sdlSound)
            throw new ArgumentException("Sound must be SDL3SoundEffect", nameof(sound));

        if (!sdlSound.IsLoaded)
        {
            _logger.LogWarning("Attempted to play unloaded sound");
            return nint.Zero;
        }

        var finalVolume = (volume ?? 1.0f) * _soundVolume;
        pan = Math.Clamp(pan, -1.0f, 1.0f);

        // Create a new track for this sound
        var track = SDL3.Mixer.CreateTrack(_mixer);
        if (track == IntPtr.Zero)
        {
            _logger.LogWarning("Failed to create track for sound {Name}", sound.Name);
            return nint.Zero;
        }

        _tracks.Add(track);

        // Set track gain (volume)
        SDL3.Mixer.SetTrackGain(track, finalVolume);

        // Set track audio source
        if (!SDL3.Mixer.SetTrackAudio(track, sdlSound.Handle))
        {
            _logger.LogWarning("Failed to set track audio for sound {Name}", sound.Name);
            SDL3.Mixer.DestroyTrack(track);
            _tracks.Remove(track);
            return nint.Zero;
        }

        // Apply spatial audio (stereo panning) if needed
        if (Math.Abs(pan) > 0.001f)
        {
            float leftGain = (1.0f - Math.Max(0, pan));
            float rightGain = (1.0f + Math.Min(0, pan));

            var gains = new float[] { leftGain, rightGain };
            var gainsHandle = GCHandle.Alloc(gains, GCHandleType.Pinned);
            try
            {
                SDL3.Mixer.SetTrackStereo(track, gainsHandle.AddrOfPinnedObject());
            }
            finally
            {
                gainsHandle.Free();
            }
        }

        var callback = new TrackStoppedCallback((userdata) => OnTrackStoppedNative(userdata));
        _trackCallbacks[track] = new TrackCallbackData { Callback = callback, Track = track };

        var props = SDL3.SDL.CreateProperties();
        SDL3.SDL.SetNumberProperty(props, "SDL_mixer.play.loops", loops);

        var callbackPtr = Marshal.GetFunctionPointerForDelegate(callback);
        SDL3.SDL.SetPointerProperty(props, "SDL_mixer.play.stopped_callback", callbackPtr);
        SDL3.SDL.SetPointerProperty(props, "SDL_mixer.play.stopped_userdata", track);

        if (!SDL3.Mixer.PlayTrack(track, props))
        {
            _logger.LogWarning("Failed to play sound {Name}: {Error}", sound.Name, SDL3.SDL.GetError());
            SDL3.Mixer.DestroyTrack(track);
            _tracks.Remove(track);
            _trackCallbacks.Remove(track);
            SDL3.SDL.DestroyProperties(props);
            return nint.Zero;
        }

        SDL3.SDL.DestroyProperties(props);

        _logger.LogDebug("Playing sound {Name} on track {Track} with callback", sound.Name, track);
        return track;
    }

    /// <summary>
    /// Native callback invoked when an audio track stops playing.
    /// </summary>
    /// <param name="track">Handle to the track that stopped.</param>
    /// <remarks>
    /// This method is called from the SDL audio thread. It cleans up callback data
    /// and raises the <see cref="OnTrackStopped"/> event.
    /// </remarks>
    private void OnTrackStoppedNative(nint track)
    {
        _logger.LogDebug("Track {Track} stopped (audio thread callback)", track);
        _trackCallbacks.Remove(track);
        OnTrackStopped?.Invoke(track);
    }

    /// <summary>
    /// Stops and destroys the specified audio track.
    /// </summary>
    /// <param name="track">Handle to the track to stop.</param>
    /// <remarks>
    /// This method immediately stops playback, destroys the track, removes it from tracking,
    /// cleans up callbacks, and raises the <see cref="OnTrackStopped"/> event.
    /// If the track is not found in the active tracks list, no action is taken.
    /// </remarks>
    public void StopTrack(nint track)
    {
        if (_tracks.Contains(track))
        {
            SDL3.Mixer.StopTrack(track, 0);
            SDL3.Mixer.DestroyTrack(track);
            _tracks.Remove(track);
            _trackCallbacks.Remove(track);

            OnTrackStopped?.Invoke(track);
        }
    }

    /// <summary>
    /// Updates the volume and stereo panning for an active audio track.
    /// </summary>
    /// <param name="track">Handle to the track to update.</param>
    /// <param name="volume">New volume level (0.0 to 10.0).</param>
    /// <param name="pan">New stereo panning (-1.0 for full left, 0.0 for center, 1.0 for full right).</param>
    /// <remarks>
    /// If the track is not found in the active tracks list, no action is taken.
    /// Panning is only applied if the absolute value is greater than 0.001.
    /// </remarks>
    public void UpdateTrackSpatialAudio(nint track, float volume, float pan)
    {
        if (!_tracks.Contains(track))
            return;

        // Update volume
        SDL3.Mixer.SetTrackGain(track, volume);

        // Update panning
        if (Math.Abs(pan) > 0.001f)
        {
            float leftGain = (1.0f - Math.Max(0, pan));
            float rightGain = (1.0f + Math.Min(0, pan));

            var gains = new float[] { leftGain, rightGain };
            var gainsHandle = GCHandle.Alloc(gains, GCHandleType.Pinned);
            try
            {
                SDL3.Mixer.SetTrackStereo(track, gainsHandle.AddrOfPinnedObject());
            }
            finally
            {
                gainsHandle.Free();
            }
        }
    }

    /// <summary>
    /// Stops all currently playing sound effects, but not music.
    /// </summary>
    /// <remarks>
    /// This method iterates through all active tracks and stops those that are not
    /// the current music track. Each stopped track is destroyed and removed from tracking.
    /// </remarks>
    public void StopAllSounds()
    {
        foreach (var track in _tracks.ToList())
        {
            if (track != _currentMusicTrack)
            {
                SDL3.Mixer.StopTrack(track, 0);
                SDL3.Mixer.DestroyTrack(track);
                _tracks.Remove(track);
                _trackCallbacks.Remove(track);
            }
        }

        _logger.LogDebug("Stopped all sound effects");
    }

    /// <summary>
    /// Unloads a sound effect, freeing its resources.
    /// </summary>
    /// <param name="sound">The sound effect to unload.</param>
    /// <remarks>
    /// If the sound is an SDL3SoundEffect, it is removed from the loaded sounds list
    /// and disposed. No action is taken if the sound is not an SDL3SoundEffect.
    /// </remarks>
    public void UnloadSound(ISoundEffect sound)
    {
        if (sound is SDL3SoundEffect sdlSound)
        {
            _loadedSounds.Remove(sdlSound);
            sdlSound.Dispose();
            _logger.LogDebug("Sound unloaded: {Name}", sound.Name);
        }
    }

    /// <summary>
    /// Asynchronously loads music from the specified file path.
    /// </summary>
    /// <param name="path">The file path to the music file.</param>
    /// <param name="cancellationToken">Token to cancel the loading operation.</param>
    /// <returns>A task that represents the asynchronous load operation. The task result contains the loaded music.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file at <paramref name="path"/> does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when SDL fails to load the audio file.</exception>
    public async Task<IMusic> LoadMusicAsync(string path, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => LoadMusic(path), cancellationToken);
    }

    /// <summary>
    /// Synchronously loads music from the specified file path.
    /// </summary>
    /// <param name="path">The file path to the music file.</param>
    /// <returns>The loaded music.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="path"/> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown when the file at <paramref name="path"/> does not exist.</exception>
    /// <exception cref="InvalidOperationException">Thrown when SDL fails to load the audio file.</exception>
    /// <remarks>
    /// Music is loaded without buffering (streaming mode) to conserve memory for large files.
    /// </remarks>
    private IMusic LoadMusic(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Music file not found: {path}");

        _logger.LogInformation("Loading music: {Path}", path);

        var audio = SDL3.Mixer.LoadAudio(_mixer, path, false);
        if (audio == IntPtr.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to load music {Path}: {Error}", path, error);
            throw new InvalidOperationException($"Failed to load music: {error}");
        }

        var musicObj = new SDL3Music(path, audio, _loggerFactory.CreateLogger<SDL3Music>());
        _loadedMusic.Add(musicObj);

        _logger.LogDebug("Music loaded: {Path}", path);
        return musicObj;
    }

    /// <summary>
    /// Plays music with optional looping.
    /// </summary>
    /// <param name="music">The music to play.</param>
    /// <param name="loops">Number of times to loop. 0 for no loop, -1 for infinite loop. Default is -1.</param>
    /// <exception cref="ArgumentException">Thrown when <paramref name="music"/> is not an SDL3Music instance.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the music is not loaded.</exception>
    /// <remarks>
    /// If music is already playing, it will be stopped before starting the new track.
    /// The music is played at the volume specified by <see cref="MusicVolume"/>.
    /// Sets <see cref="IsMusicPlaying"/> to true and <see cref="IsMusicPaused"/> to false on success.
    /// </remarks>
    public void PlayMusic(IMusic music, int loops = -1)
    {
        if (music is not SDL3Music sdlMusic)
            throw new ArgumentException("Music must be SDL3Music", nameof(music));

        if (!sdlMusic.IsLoaded)
            throw new InvalidOperationException("Music is not loaded");

        if (_currentMusicTrack != IntPtr.Zero)
        {
            SDL3.Mixer.StopTrack(_currentMusicTrack, 0);
            SDL3.Mixer.DestroyTrack(_currentMusicTrack);
            _tracks.Remove(_currentMusicTrack);
        }

        _currentMusicTrack = SDL3.Mixer.CreateTrack(_mixer);
        if (_currentMusicTrack == IntPtr.Zero)
        {
            _logger.LogWarning("Failed to create track for music {Name}", music.Name);
            return;
        }

        _tracks.Add(_currentMusicTrack);
        SDL3.Mixer.SetTrackGain(_currentMusicTrack, MusicVolume);

        if (!SDL3.Mixer.SetTrackAudio(_currentMusicTrack, sdlMusic.Handle))
        {
            _logger.LogWarning("Failed to set track audio for music {Name}", music.Name);
            SDL3.Mixer.DestroyTrack(_currentMusicTrack);
            _tracks.Remove(_currentMusicTrack);
            _currentMusicTrack = IntPtr.Zero;
            return;
        }

        var props = SDL3.SDL.CreateProperties();
        SDL3.SDL.SetNumberProperty(props, "SDL_mixer.play.loops", loops);

        if (!SDL3.Mixer.PlayTrack(_currentMusicTrack, props))
        {
            _logger.LogWarning("Failed to play music {Name}", music.Name);
            SDL3.Mixer.DestroyTrack(_currentMusicTrack);
            _tracks.Remove(_currentMusicTrack);
            _currentMusicTrack = IntPtr.Zero;
        }
        else
        {
            IsMusicPlaying = true;
            IsMusicPaused = false;
            _logger.LogInformation("Playing music: {Name}", music.Name);
        }

        SDL3.SDL.DestroyProperties(props);
    }

    /// <summary>
    /// Pauses the currently playing music.
    /// </summary>
    /// <remarks>
    /// If no music is currently playing, this method has no effect.
    /// Sets <see cref="IsMusicPaused"/> to true.
    /// Use <see cref="ResumeMusic"/> to continue playback.
    /// </remarks>
    public void PauseMusic()
    {
        if (_currentMusicTrack != IntPtr.Zero)
        {
            SDL3.Mixer.PauseTrack(_currentMusicTrack);
            IsMusicPaused = true;
            _logger.LogDebug("Music paused");
        }
    }

    /// <summary>
    /// Resumes paused music playback.
    /// </summary>
    /// <remarks>
    /// If no music is currently paused, this method has no effect.
    /// Sets <see cref="IsMusicPaused"/> to false.
    /// </remarks>
    public void ResumeMusic()
    {
        if (_currentMusicTrack != IntPtr.Zero)
        {
            SDL3.Mixer.ResumeTrack(_currentMusicTrack);
            IsMusicPaused = false;
            _logger.LogDebug("Music resumed");
        }
    }

    /// <summary>
    /// Stops the currently playing music.
    /// </summary>
    /// <remarks>
    /// If no music is currently playing, this method has no effect.
    /// The music track is stopped, destroyed, and removed from tracking.
    /// Sets <see cref="IsMusicPlaying"/> to false and <see cref="IsMusicPaused"/> to false.
    /// </remarks>
    public void StopMusic()
    {
        if (_currentMusicTrack != IntPtr.Zero)
        {
            SDL3.Mixer.StopTrack(_currentMusicTrack, 0);
            SDL3.Mixer.DestroyTrack(_currentMusicTrack);
            _tracks.Remove(_currentMusicTrack);
            _currentMusicTrack = IntPtr.Zero;
            IsMusicPlaying = false;
            IsMusicPaused = false;
            _logger.LogDebug("Music stopped");
        }
    }

    /// <summary>
    /// Unloads music, freeing its resources.
    /// </summary>
    /// <param name="music">The music to unload.</param>
    /// <remarks>
    /// If the music is an SDL3Music instance, it is removed from the loaded music list
    /// and disposed. No action is taken if the music is not an SDL3Music instance.
    /// </remarks>
    public void UnloadMusic(IMusic music)
    {
        if (music is SDL3Music sdlMusic)
        {
            _loadedMusic.Remove(sdlMusic);
            sdlMusic.Dispose();
            _logger.LogDebug("Music unloaded: {Name}", music.Name);
        }
    }

    /// <summary>
    /// Releases all resources used by the <see cref="SDL3AudioService"/>.
    /// </summary>
    /// <remarks>
    /// This method performs the following cleanup:
    /// <list type="number">
    /// <item>Stops all music and sound effects</item>
    /// <item>Destroys all active audio tracks</item>
    /// <item>Clears all callback registrations</item>
    /// <item>Disposes all loaded sounds and music</item>
    /// <item>Destroys the mixer device</item>
    /// <item>Quits SDL3_mixer</item>
    /// </list>
    /// This method is idempotent and can be called multiple times safely.
    /// </remarks>
    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("Disposing audio service");

        StopMusic();
        StopAllSounds();

        foreach (var track in _tracks.ToList())
        {
            SDL3.Mixer.DestroyTrack(track);
        }
        _tracks.Clear();
        _trackCallbacks.Clear();

        foreach (var sound in _loadedSounds.ToList())
        {
            sound.Dispose();
        }
        _loadedSounds.Clear();

        foreach (var music in _loadedMusic.ToList())
        {
            music.Dispose();
        }
        _loadedMusic.Clear();

        if (_mixer != IntPtr.Zero)
        {
            SDL3.Mixer.DestroyMixer(_mixer);
            _mixer = IntPtr.Zero;
        }

        SDL3.Mixer.Quit();

        _disposed = true;
    }
}