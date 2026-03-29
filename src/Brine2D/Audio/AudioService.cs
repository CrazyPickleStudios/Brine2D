using Brine2D.Audio;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Brine2D.Audio;

/// <summary>
/// SDL3_mixer implementation of audio service with spatial audio support and proper callbacks.
/// </summary>
internal class AudioService : IAudioService
{
    private readonly ILogger<AudioService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly List<SDL3SoundEffect> _loadedSounds = new();
    private readonly List<SDL3Music> _loadedMusic = new();
    private readonly List<nint> _tracks = new();
    private readonly Dictionary<nint, TrackCallbackData> _trackCallbacks = new();
    private readonly Lock _tracksLock = new();
    private readonly Lock _assetLock = new();
    private nint _mixer;
    private nint _currentMusicTrack;
    private int _disposed;
    private float _soundVolume = 1.0f;

    /// <inheritdoc/>
    public float MasterVolume
    {
        get => SDL3.Mixer.GetMasterGain(_mixer);
        set => SDL3.Mixer.SetMasterGain(_mixer, Math.Clamp(value, 0f, 10f));
    }

    /// <inheritdoc/>
    public float MusicVolume { get; set; } = 1.0f;

    /// <inheritdoc/>
    public float SoundVolume
    {
        get => _soundVolume;
        set => _soundVolume = Math.Clamp(value, 0f, 10f);
    }

    public bool IsMusicPlaying { get; private set; }

    public bool IsMusicPaused { get; private set; }

    /// <inheritdoc/>
    public event Action<nint>? OnTrackStopped;

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void TrackStoppedCallback(nint userdata);

    private struct TrackCallbackData
    {
        public TrackStoppedCallback Callback;
        public nint Track;
    }

    public AudioService(ILogger<AudioService> logger, ILoggerFactory loggerFactory)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));

        Initialize();
    }

    private void Initialize()
    {
        _logger.LogInformation("Initializing SDL3_mixer audio system");

        if (!SDL3.Mixer.Init())
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to initialize SDL3_mixer: {Error}", error);
            throw new InvalidOperationException($"Failed to initialize SDL3_mixer: {error}");
        }

        _mixer = SDL3.Mixer.CreateMixerDevice(SDL3.SDL.AudioDeviceDefaultPlayback, nint.Zero);
        if (_mixer == nint.Zero)
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

    /// <inheritdoc/>
    public async Task<ISoundEffect> LoadSoundAsync(string path, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => LoadSound(path), cancellationToken);
    }

    private SDL3SoundEffect LoadSound(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Sound file not found: {path}");

        _logger.LogDebug("Loading sound: {Path}", path);

        var audio = SDL3.Mixer.LoadAudio(_mixer, path, true);

        if (audio == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to load sound {Path}: {Error}", path, error);
            throw new InvalidOperationException($"Failed to load sound: {error}");
        }

        var sound = new SDL3SoundEffect(path, audio, _loggerFactory.CreateLogger<SDL3SoundEffect>());
        lock (_assetLock)
        {
            _loadedSounds.Add(sound);
        }

        _logger.LogDebug("Sound loaded: {Path}", path);
        return sound;
    }

    /// <inheritdoc/>
    public void PlaySound(ISoundEffect sound, float? volume = null, int loops = 0, float pan = 0f)
    {
        PlaySoundWithTrack(sound, volume, loops, pan);
    }

    /// <inheritdoc/>
    public nint PlaySoundWithTrack(ISoundEffect sound, float? volume = null, int loops = 0, float pan = 0f)
    {
        if (sound is not SDL3SoundEffect sdlSound)
            throw new ArgumentException("Sound must be an SDL3SoundEffect", nameof(sound));

        if (!sdlSound.IsLoaded)
        {
            _logger.LogWarning("Attempted to play unloaded sound");
            return nint.Zero;
        }

        var finalVolume = (volume ?? 1.0f) * _soundVolume;
        pan = Math.Clamp(pan, -1.0f, 1.0f);

        var track = SDL3.Mixer.CreateTrack(_mixer);
        if (track == nint.Zero)
        {
            _logger.LogWarning("Failed to create track for sound {Name}", sound.Name);
            return nint.Zero;
        }

        SDL3.Mixer.SetTrackGain(track, finalVolume);

        if (!SDL3.Mixer.SetTrackAudio(track, sdlSound.Handle))
        {
            _logger.LogWarning("Failed to set track audio for sound {Name}", sound.Name);
            SDL3.Mixer.DestroyTrack(track);
            return nint.Zero;
        }

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
        var callbackPtr = Marshal.GetFunctionPointerForDelegate(callback);

        var props = SDL3.SDL.CreateProperties();
        SDL3.SDL.SetNumberProperty(props, "SDL_mixer.play.loops", loops);
        SDL3.SDL.SetPointerProperty(props, "SDL_mixer.play.stopped_callback", callbackPtr);
        SDL3.SDL.SetPointerProperty(props, "SDL_mixer.play.stopped_userdata", track);

        // Register tracking data BEFORE PlayTrack so the completion callback
        // always finds valid entries even if the sound finishes immediately.
        lock (_tracksLock)
        {
            _tracks.Add(track);
            _trackCallbacks[track] = new TrackCallbackData { Callback = callback, Track = track };
        }

        if (!SDL3.Mixer.PlayTrack(track, props))
        {
            _logger.LogWarning("Failed to play sound {Name}: {Error}", sound.Name, SDL3.SDL.GetError());
            lock (_tracksLock)
            {
                _tracks.Remove(track);
                _trackCallbacks.Remove(track);
            }
            SDL3.Mixer.DestroyTrack(track);
            SDL3.SDL.DestroyProperties(props);
            return nint.Zero;
        }

        SDL3.SDL.DestroyProperties(props);
        _logger.LogDebug("Playing sound {Name} on track {Track} with callback", sound.Name, track);
        return track;
    }

    private void OnTrackStoppedNative(nint track)
    {
        bool removed;
        lock (_tracksLock)
        {
            removed = _tracks.Remove(track);
            _trackCallbacks.Remove(track);

            if (removed && track == _currentMusicTrack)
            {
                _currentMusicTrack = nint.Zero;
                IsMusicPlaying = false;
                IsMusicPaused = false;
            }
        }

        // Raise outside the lock — a subscriber re-entering any AudioService method
        // while we hold _tracksLock would deadlock.
        if (removed)
        {
            _logger.LogDebug("Track {Track} stopped (audio thread callback)", track);
            OnTrackStopped?.Invoke(track);
        }
    }

    /// <inheritdoc/>
    public void StopTrack(nint track)
    {
        bool removed;
        lock (_tracksLock)
        {
            removed = _tracks.Remove(track);
            _trackCallbacks.Remove(track);

            if (removed && track == _currentMusicTrack)
            {
                _currentMusicTrack = nint.Zero;
                IsMusicPlaying = false;
                IsMusicPaused = false;
            }
        }

        if (removed)
        {
            SDL3.Mixer.StopTrack(track, 0);
            SDL3.Mixer.DestroyTrack(track);
            OnTrackStopped?.Invoke(track);
        }
    }

    /// <inheritdoc/>
    public void UpdateTrackSpatialAudio(nint track, float volume, float pan)
    {
        lock (_tracksLock)
        {
            if (!_tracks.Contains(track))
                return;
        }

        SDL3.Mixer.SetTrackGain(track, volume);

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

    /// <inheritdoc/>
    public void StopAllSounds()
    {
        List<nint> toStop;
        lock (_tracksLock)
        {
            toStop = _tracks.Where(t => t != _currentMusicTrack).ToList();
            foreach (var track in toStop)
            {
                _tracks.Remove(track);
                _trackCallbacks.Remove(track);
            }
        }

        foreach (var track in toStop)
        {
            SDL3.Mixer.StopTrack(track, 0);
            SDL3.Mixer.DestroyTrack(track);
        }

        _logger.LogDebug("Stopped all sound effects");
    }

    /// <inheritdoc/>
    public void UnloadSound(ISoundEffect sound)
    {
        if (sound is SDL3SoundEffect sdlSound)
        {
            lock (_assetLock)
            {
                _loadedSounds.Remove(sdlSound);
            }
            sdlSound.Dispose();
            _logger.LogDebug("Sound unloaded: {Name}", sound.Name);
        }
    }

    /// <inheritdoc/>
    public async Task<IMusic> LoadMusicAsync(string path, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => LoadMusic(path), cancellationToken);
    }

    private SDL3Music LoadMusic(string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Music file not found: {path}");

        _logger.LogDebug("Loading music: {Path}", path);

        var audio = SDL3.Mixer.LoadAudio(_mixer, path, false);
        if (audio == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to load music {Path}: {Error}", path, error);
            throw new InvalidOperationException($"Failed to load music: {error}");
        }

        var musicObj = new SDL3Music(path, audio, _loggerFactory.CreateLogger<SDL3Music>());
        lock (_assetLock)
        {
            _loadedMusic.Add(musicObj);
        }

        _logger.LogDebug("Music loaded: {Path}", path);
        return musicObj;
    }

    /// <inheritdoc/>
    public void PlayMusic(IMusic music, int loops = -1)
    {
        if (music is not SDL3Music sdlMusic)
            throw new ArgumentException("Music must be an SDL3Music", nameof(music));

        if (!sdlMusic.IsLoaded)
            throw new InvalidOperationException("Music is not loaded");

        // Capture and evict the previous track under lock; SDL calls happen after the lock.
        nint oldTrack;
        lock (_tracksLock)
        {
            oldTrack = _currentMusicTrack;
            if (oldTrack != nint.Zero)
            {
                _tracks.Remove(oldTrack);
                _trackCallbacks.Remove(oldTrack);
                _currentMusicTrack = nint.Zero;
                IsMusicPlaying = false;
                IsMusicPaused = false;
            }
        }

        if (oldTrack != nint.Zero)
        {
            SDL3.Mixer.StopTrack(oldTrack, 0);
            SDL3.Mixer.DestroyTrack(oldTrack);
        }

        var newTrack = SDL3.Mixer.CreateTrack(_mixer);
        if (newTrack == nint.Zero)
        {
            _logger.LogWarning("Failed to create track for music {Name}", music.Name);
            return;
        }

        SDL3.Mixer.SetTrackGain(newTrack, MusicVolume);

        if (!SDL3.Mixer.SetTrackAudio(newTrack, sdlMusic.Handle))
        {
            _logger.LogWarning("Failed to set track audio for music {Name}", music.Name);
            SDL3.Mixer.DestroyTrack(newTrack);
            return;
        }

        var callback = new TrackStoppedCallback((userdata) => OnTrackStoppedNative(userdata));
        var callbackPtr = Marshal.GetFunctionPointerForDelegate(callback);

        var props = SDL3.SDL.CreateProperties();
        SDL3.SDL.SetNumberProperty(props, "SDL_mixer.play.loops", loops);
        SDL3.SDL.SetPointerProperty(props, "SDL_mixer.play.stopped_callback", callbackPtr);
        SDL3.SDL.SetPointerProperty(props, "SDL_mixer.play.stopped_userdata", newTrack);

        // Register BEFORE PlayTrack for the same reason as PlaySoundWithTrack.
        lock (_tracksLock)
        {
            _currentMusicTrack = newTrack;
            _tracks.Add(newTrack);
            _trackCallbacks[newTrack] = new TrackCallbackData { Callback = callback, Track = newTrack };
        }

        if (!SDL3.Mixer.PlayTrack(newTrack, props))
        {
            _logger.LogWarning("Failed to play music {Name}", music.Name);
            lock (_tracksLock)
            {
                _tracks.Remove(newTrack);
                _trackCallbacks.Remove(newTrack);
                _currentMusicTrack = nint.Zero;
            }
            SDL3.Mixer.DestroyTrack(newTrack);
            SDL3.SDL.DestroyProperties(props);
            return;
        }

        SDL3.SDL.DestroyProperties(props);

        lock (_tracksLock)
        {
            // Guard: callback may have already fired if the track was extremely short.
            if (_currentMusicTrack == newTrack)
            {
                IsMusicPlaying = true;
                IsMusicPaused = false;
            }
        }

        _logger.LogInformation("Playing music: {Name}", music.Name);
    }

    /// <inheritdoc/>
    public void PauseMusic()
    {
        nint track;
        lock (_tracksLock)
        {
            track = _currentMusicTrack;
            if (track != nint.Zero)
                IsMusicPaused = true;
        }

        if (track != nint.Zero)
        {
            SDL3.Mixer.PauseTrack(track);
            _logger.LogDebug("Music paused");
        }
    }

    /// <inheritdoc/>
    public void ResumeMusic()
    {
        nint track;
        lock (_tracksLock)
        {
            track = _currentMusicTrack;
            if (track != nint.Zero)
                IsMusicPaused = false;
        }

        if (track != nint.Zero)
        {
            SDL3.Mixer.ResumeTrack(track);
            _logger.LogDebug("Music resumed");
        }
    }

    /// <inheritdoc/>
    public void StopMusic()
    {
        nint track;
        lock (_tracksLock)
        {
            track = _currentMusicTrack;
            if (track != nint.Zero)
            {
                _tracks.Remove(track);
                _trackCallbacks.Remove(track);
                _currentMusicTrack = nint.Zero;
                IsMusicPlaying = false;
                IsMusicPaused = false;
            }
        }

        if (track != nint.Zero)
        {
            SDL3.Mixer.StopTrack(track, 0);
            SDL3.Mixer.DestroyTrack(track);
            _logger.LogDebug("Music stopped");
        }
    }

    /// <inheritdoc/>
    public void UnloadMusic(IMusic music)
    {
        if (music is SDL3Music sdlMusic)
        {
            lock (_assetLock)
            {
                _loadedMusic.Remove(sdlMusic);
            }
            sdlMusic.Dispose();
            _logger.LogDebug("Music unloaded: {Name}", music.Name);
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        _logger.LogInformation("Disposing audio service");

        StopMusic();
        StopAllSounds();

        List<nint> remainingTracks;
        lock (_tracksLock)
        {
            remainingTracks = [.._tracks];
            _tracks.Clear();
            _trackCallbacks.Clear();
        }

        foreach (var track in remainingTracks)
            SDL3.Mixer.DestroyTrack(track);

        List<SDL3SoundEffect> sounds;
        List<SDL3Music> music;
        lock (_assetLock)
        {
            sounds = [.._loadedSounds];
            _loadedSounds.Clear();
            music = [.._loadedMusic];
            _loadedMusic.Clear();
        }

        foreach (var sound in sounds)
            sound.Dispose();

        foreach (var m in music)
            m.Dispose();

        try
        {
            if (_mixer != nint.Zero)
            {
                SDL3.Mixer.DestroyMixer(_mixer);
                _mixer = nint.Zero;
            }

            SDL3.Mixer.Quit();
        }
        catch (Exception ex)
        {
            // SDL may already be shut down by the time DI disposes this service.
            // TODO: dispose AudioService during GameEngine.Shutdown() before SDL tears down.
            _logger.LogDebug(ex, "Exception during SDL3_mixer teardown; SDL may have already exited");
        }
    }
}