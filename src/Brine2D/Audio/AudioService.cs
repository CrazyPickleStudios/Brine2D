using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;
using System.Collections.Concurrent;

namespace Brine2D.Audio;

/// <summary>
/// SDL3_mixer implementation of audio service with spatial audio support and proper callbacks.
/// </summary>
internal sealed class AudioService : IAudioService
{
    private const string MixerPlayLoops = "SDL_mixer.play.loops";
    private const string MixerPlayLoopStartMs = "SDL_mixer.play.loop_start_millisecond";
    private const string MixerPlayStoppedCallback = "SDL_mixer.play.stopped_callback";
    private const string MixerPlayStoppedUserdata = "SDL_mixer.play.stopped_userdata";

    private readonly ILogger<AudioService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly HashSet<SDL3SoundEffect> _loadedSounds = new();
    private readonly HashSet<SDL3Music> _loadedMusic = new();
    private readonly Dictionary<nint, nint> _tracks = new();
    private readonly Dictionary<nint, float> _trackVolumes = new();
    private readonly Dictionary<nint, int> _trackPriorities = new();
    private readonly Dictionary<nint, HashSet<string>> _trackBuses = new();
    private readonly ConcurrentQueue<nint> _deferredDestroys = new();
    private readonly List<nint> _soundTrackBuffer = new();
    private readonly Dictionary<string, float> _busVolumes = new(StringComparer.Ordinal); // game-thread only; no lock required
    private readonly HashSet<string> _pausedBuses = new(StringComparer.Ordinal); // game-thread only; no lock required
    private readonly Lock _tracksLock = new();
    private readonly Lock _assetLock = new();
    private readonly int _maxTracks;
    private readonly TrackStoppedCallback _trackStoppedCallback;
    private readonly nint _trackStoppedCallbackPtr;
    private nint _mixer;
    private nint _currentMusicTrack;
    private nint _crossfadeOutTrack;
    private MusicFade _fade;
    private int _disposed;
    private float _soundVolume = 1.0f;
    private float _musicVolume = 1.0f;
    private float _musicEntityVolume = 1.0f;
    private string? _musicBus;
    private bool _isMusicPlaying;
    private bool _isMusicPaused;
    private bool _isMusicIndividuallyPaused;
    private readonly Dictionary<nint, long> _trackAges = new();
    private long _nextTrackAge;

    /// <inheritdoc/>
    public float MasterVolume
    {
        get
        {
            ThrowIfDisposed();
            return SDL3.Mixer.GetMixerGain(_mixer);
        }
        set
        {
            ThrowIfDisposed();
            SDL3.Mixer.SetMixerGain(_mixer, Math.Clamp(value, 0f, 1f));
        }
    }

    /// <inheritdoc/>
    public float MusicVolume
    {
        get => Volatile.Read(ref _musicVolume);
        set
        {
            ThrowIfDisposed();
            var clamped = Math.Clamp(value, 0f, 1f);
            Volatile.Write(ref _musicVolume, clamped);

            // When a crossfade is active the per-track gains are driven by
            // Update, so skip the immediate update here.
            nint track;
            bool crossfading;
            lock (_tracksLock)
            {
                track = _currentMusicTrack;
                crossfading = _fade.IsActive;
            }

            if (!crossfading && track != nint.Zero)
                SDL3.Mixer.SetTrackGain(track, clamped * Volatile.Read(ref _musicEntityVolume));
        }
    }

    /// <inheritdoc/>
    public float SoundVolume
    {
        get => Volatile.Read(ref _soundVolume);
        set
        {
            ThrowIfDisposed();
            var clamped = Math.Clamp(value, 0f, 1f);
            Volatile.Write(ref _soundVolume, clamped);
            ReapplySoundTrackGains(clamped);
        }
    }

    public bool IsMusicPlaying
    {
        get => Volatile.Read(ref _isMusicPlaying);
        private set => Volatile.Write(ref _isMusicPlaying, value);
    }

    public bool IsMusicPaused
    {
        get => Volatile.Read(ref _isMusicPaused);
        private set => Volatile.Write(ref _isMusicPaused, value);
    }

    public bool IsMusicFadingOut
    {
        get
        {
            lock (_tracksLock)
            {
                return _fade.IsFadingOut;
            }
        }
    }

    /// <inheritdoc/>
    public int ActiveSoundTrackCount
    {
        get
        {
            lock (_tracksLock)
            {
                int musicTracks = (_currentMusicTrack != nint.Zero ? 1 : 0)
                                + (_crossfadeOutTrack != nint.Zero ? 1 : 0);
                return _tracks.Count - musicTracks;
            }
        }
    }

    /// <inheritdoc/>
    public int MaxSoundTracks => _maxTracks;
    
    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void TrackStoppedCallback(nint userdata);

    public AudioService(AudioOptions options, ILogger<AudioService> logger, ILoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(loggerFactory);
        _logger = logger;
        _loggerFactory = loggerFactory;
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxTracks, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.MaxTracks, 32);
        _maxTracks = options.MaxTracks;
        _trackStoppedCallback = OnTrackStoppedNative;
        _trackStoppedCallbackPtr = Marshal.GetFunctionPointerForDelegate(_trackStoppedCallback);

        Initialize();

        MasterVolume = options.MasterVolume;
        MusicVolume = options.MusicVolume;
        SoundVolume = options.SoundVolume;
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
            SDL3.Mixer.Quit();
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create mixer device: {Error}", error);
            throw new InvalidOperationException($"Failed to create mixer device: {error}");
        }

        unsafe
        {
            SDL3.SDL.AudioSpec spec;
            if (SDL3.Mixer.GetMixerFormat(_mixer, (nint)(&spec)))
            {
                _logger.LogInformation("SDL3_mixer initialized: {Freq} Hz, {Channels} channels",
                    spec.Freq, spec.Channels);
            }
            else
            {
                _logger.LogInformation("SDL3_mixer initialized successfully");
            }
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// MIX_LoadAudio is thread-safe per SDL documentation, so offloading to the
    /// thread pool via <see cref="Task.Run"/> is safe for concurrent loads.
    /// See: https://wiki.libsdl.org/SDL3_mixer/MIX_LoadAudio
    /// </remarks>
    public async Task<ISoundEffect> LoadSoundAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return await Task.Run(() => LoadSound(path, cancellationToken), cancellationToken);
    }

    private SDL3SoundEffect LoadSound(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Sound file not found: {path}");

        cancellationToken.ThrowIfCancellationRequested();

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
    public nint PlaySound(ISoundEffect sound, float volume = 1.0f, int loops = 0, float pan = 0f, float pitch = 1f, int priority = 0, string? bus = null)
    {
        ThrowIfDisposed();

        if (sound is not SDL3SoundEffect sdlSound)
            throw new ArgumentException("Sound must be an SDL3SoundEffect", nameof(sound));

        if (!sdlSound.TryGetHandle(out var audioHandle))
        {
            _logger.LogWarning("Attempted to play disposed or unloaded sound");
            return nint.Zero;
        }

        var clampedVolume = Math.Clamp(volume, 0f, 1f);
        var finalVolume = clampedVolume * Volatile.Read(ref _soundVolume);
        pan = Math.Clamp(pan, -1.0f, 1.0f);
        pitch = Math.Clamp(pitch, 0.25f, 4.0f);

        var track = SDL3.Mixer.CreateTrack(_mixer);
        if (track == nint.Zero)
        {
            _logger.LogWarning("Failed to create track for sound {Name}", sound.Name);
            return nint.Zero;
        }

        SDL3.Mixer.SetTrackGain(track, finalVolume);

        if (!SDL3.Mixer.SetTrackAudio(track, audioHandle))
        {
            _logger.LogWarning("Failed to set track audio for sound {Name}", sound.Name);
            SDL3.Mixer.DestroyTrack(track);
            return nint.Zero;
        }

        if (Math.Abs(pan) > 0.001f)
            ApplyStereoPan(track, pan);

        if (Math.Abs(pitch - 1f) > 0.001f)
            SDL3.Mixer.SetTrackFrequencyRatio(track, pitch);

        if (bus != null)
            SDL3.Mixer.TagTrack(track, bus);

        var props = SDL3.SDL.CreateProperties();
        if (props == nint.Zero)
        {
            _logger.LogWarning("Failed to create properties for sound {Name}", sound.Name);
            SDL3.Mixer.DestroyTrack(track);
            return nint.Zero;
        }

        nint evictedTrack = nint.Zero;
        try
        {
            SDL3.SDL.SetNumberProperty(props, MixerPlayLoops, loops);
            SDL3.SDL.SetPointerProperty(props, MixerPlayStoppedCallback, _trackStoppedCallbackPtr);
            SDL3.SDL.SetPointerProperty(props, MixerPlayStoppedUserdata, track);

            lock (_tracksLock)
            {
                int reservedMusicTracks = (_currentMusicTrack != nint.Zero ? 1 : 0)
                                        + (_crossfadeOutTrack != nint.Zero ? 1 : 0);

                if (_tracks.Count - reservedMusicTracks >= _maxTracks)
                {
                    nint lowestTrack = nint.Zero;
                    int lowestPriority = int.MaxValue;
                    long oldestAge = long.MaxValue;
                    foreach (var (t, p) in _trackPriorities)
                    {
                        var age = _trackAges.GetValueOrDefault(t, long.MaxValue);
                        if (p < lowestPriority || (p == lowestPriority && age < oldestAge))
                        {
                            lowestPriority = p;
                            lowestTrack = t;
                            oldestAge = age;
                        }
                    }

                    if (lowestTrack == nint.Zero || priority < lowestPriority)
                    {
                        _logger.LogWarning(
                            "Maximum sound tracks ({MaxTracks}) reached, cannot play sound {Name} (priority {Priority})",
                            _maxTracks, sound.Name, priority);
                        SDL3.Mixer.DestroyTrack(track);
                        return nint.Zero;
                    }

                    _tracks.Remove(lowestTrack);
                    _trackVolumes.Remove(lowestTrack);
                    _trackPriorities.Remove(lowestTrack);
                    _trackAges.Remove(lowestTrack);
                    _trackBuses.Remove(lowestTrack);
                    evictedTrack = lowestTrack;
                }

                _tracks[track] = audioHandle;
                _trackVolumes[track] = clampedVolume;
                _trackPriorities[track] = priority;
                _trackAges[track] = _nextTrackAge++;
                if (bus != null)
                    _trackBuses[track] = new HashSet<string>(StringComparer.Ordinal) { bus };
            }

            if (evictedTrack != nint.Zero)
                FinalizeTrack(evictedTrack, stop: true, deferDestroy: true);

            if (!SDL3.Mixer.PlayTrack(track, props))
            {
                _logger.LogWarning("Failed to play sound {Name}: {Error}", sound.Name, SDL3.SDL.GetError());
                lock (_tracksLock)
                {
                    _tracks.Remove(track);
                    _trackVolumes.Remove(track);
                    _trackPriorities.Remove(track);
                    _trackAges.Remove(track);
                    _trackBuses.Remove(track);
                }
                SDL3.Mixer.DestroyTrack(track);
                return nint.Zero;
            }
        }
        finally
        {
            SDL3.SDL.DestroyProperties(props);
        }

        _logger.LogDebug("Playing sound {Name} on track {Track}", sound.Name, track);
        return track;
    }

    /// <inheritdoc/>
    public bool IsTrackAlive(nint track)
    {
        if (track == nint.Zero) return false;
        ThrowIfDisposed();
        lock (_tracksLock)
        {
            return _tracks.ContainsKey(track)
                && track != _currentMusicTrack
                && track != _crossfadeOutTrack;
        }
    }

    /// <summary>
    /// Unregisters a track from internal state and optionally stops it.
    /// Handles music track and crossfade bookkeeping and destroys the native track.
    /// </summary>
    /// <param name="track">Native track handle.</param>
    /// <param name="stop">Whether to call <c>StopTrack</c> on the native handle.</param>
    /// <param name="deferDestroy">
    /// When <see langword="true"/>, queues the track for destruction on the game thread
    /// instead of destroying immediately. Use from the SDL audio callback to avoid
    /// destroying a track inside its own completion callback.
    /// </param>
    /// <returns><see langword="true"/> if the track was known and removed.</returns>
    private bool RemoveTrack(nint track, bool stop, bool deferDestroy = false)
    {
        bool removed;
        nint promoteTrack = nint.Zero;
        lock (_tracksLock)
        {
            removed = _tracks.Remove(track);
            _trackVolumes.Remove(track);
            _trackPriorities.Remove(track);
            _trackAges.Remove(track);
            _trackBuses.Remove(track);

            if (removed && track == _currentMusicTrack)
            {
                _currentMusicTrack = nint.Zero;
                IsMusicPlaying = false;
                IsMusicPaused = false;
            }

            if (track == _crossfadeOutTrack)
            {
                _crossfadeOutTrack = nint.Zero;
                _fade.Reset();
                promoteTrack = _currentMusicTrack;

                if (promoteTrack == nint.Zero)
                {
                    IsMusicPlaying = false;
                    IsMusicPaused = false;
                }
            }

            if (_currentMusicTrack == nint.Zero && _crossfadeOutTrack == nint.Zero)
            {
                _musicBus = null;
                _isMusicIndividuallyPaused = false;
            }
        }

        if (promoteTrack != nint.Zero)
            SDL3.Mixer.SetTrackGain(promoteTrack, Volatile.Read(ref _musicVolume) * Volatile.Read(ref _musicEntityVolume));

        if (removed)
            FinalizeTrack(track, stop, deferDestroy);

        return removed;
    }

    /// <summary>
    /// Performs the stop → destroy sequence for a track that has already
    /// been removed from <see cref="_tracks"/>. All cleanup paths converge here
    /// so ordering and deferred-destroy semantics are consistent.
    /// </summary>
    private void FinalizeTrack(nint track, bool stop, bool deferDestroy)
    {
        if (stop)
            SDL3.Mixer.StopTrack(track, 0);

        if (deferDestroy)
            _deferredDestroys.Enqueue(track);
        else
            SDL3.Mixer.DestroyTrack(track);
    }

    private void OnTrackStoppedNative(nint track)
    {
        if (RemoveTrack(track, stop: false, deferDestroy: true))
            _logger.LogDebug("Track {Track} stopped (audio thread callback)", track);
    }

    /// <inheritdoc/>
    public void StopTrack(nint track)
    {
        ThrowIfDisposed();
        RemoveTrack(track, stop: true);
    }

    /// <inheritdoc/>
    public void PauseTrack(nint track)
    {
        ThrowIfDisposed();
        lock (_tracksLock)
        {
            if (!_tracks.ContainsKey(track) || track == _currentMusicTrack || track == _crossfadeOutTrack)
                return;
        }

        SDL3.Mixer.PauseTrack(track);
    }

    /// <inheritdoc/>
    public void ResumeTrack(nint track)
    {
        ThrowIfDisposed();
        lock (_tracksLock)
        {
            if (!_tracks.ContainsKey(track) || track == _currentMusicTrack || track == _crossfadeOutTrack)
                return;
        }

        SDL3.Mixer.ResumeTrack(track);
    }
    
    private unsafe void ApplyStereoPan(nint track, float pan)
    {
        pan = Math.Clamp(pan, -1.0f, 1.0f);
        float angle = (pan + 1f) * MathF.PI * 0.25f;
        float* gains = stackalloc float[2];
        gains[0] = MathF.Cos(angle);
        gains[1] = MathF.Sin(angle);
        SDL3.Mixer.SetTrackStereo(track, (nint)gains);
    }

    public void StopAllSounds()
    {
        ThrowIfDisposed();

        lock (_tracksLock)
        {
            _soundTrackBuffer.Clear();
            foreach (var t in _tracks.Keys)
            {
                if (t != _currentMusicTrack && t != _crossfadeOutTrack)
                    _soundTrackBuffer.Add(t);
            }

            foreach (var track in _soundTrackBuffer)
            {
                _tracks.Remove(track);
                _trackVolumes.Remove(track);
                _trackPriorities.Remove(track);
                _trackAges.Remove(track);
                _trackBuses.Remove(track);
            }
        }

        foreach (var track in _soundTrackBuffer)
            FinalizeTrack(track, stop: true, deferDestroy: false);

        _logger.LogDebug("Stopped all sound effects");
    }

    /// <inheritdoc/>
    public void PauseAllSounds()
    {
        ThrowIfDisposed();

        lock (_tracksLock)
        {
            _soundTrackBuffer.Clear();
            foreach (var t in _tracks.Keys)
            {
                if (t != _currentMusicTrack && t != _crossfadeOutTrack)
                    _soundTrackBuffer.Add(t);
            }
        }

        foreach (var track in _soundTrackBuffer)
            SDL3.Mixer.PauseTrack(track);

        _logger.LogDebug("Paused all sound effects");
    }

    /// <inheritdoc/>
    public void ResumeAllSounds()
    {
        ThrowIfDisposed();

        lock (_tracksLock)
        {
            _soundTrackBuffer.Clear();
            foreach (var t in _tracks.Keys)
            {
                if (t != _currentMusicTrack && t != _crossfadeOutTrack)
                    _soundTrackBuffer.Add(t);
            }
        }

        foreach (var track in _soundTrackBuffer)
            SDL3.Mixer.ResumeTrack(track);

        _logger.LogDebug("Resumed all sound effects");
    }

    /// <summary>
    /// Stops and destroys all tracks that reference the given native audio handle.
    /// Used by <see cref="UnloadSound"/> and <see cref="UnloadMusic"/> to ensure
    /// no tracks reference an asset before it is freed.
    /// </summary>
    private void StopTracksForAudio(nint audioHandle)
    {
        List<nint>? tracksToStop = null;
        lock (_tracksLock)
        {
            foreach (var (track, audio) in _tracks)
            {
                if (audio != audioHandle) continue;
                tracksToStop ??= [];
                tracksToStop.Add(track);
            }
        }

        if (tracksToStop == null) return;

        foreach (var track in tracksToStop)
            RemoveTrack(track, stop: true);
    }

    /// <inheritdoc/>
    public void UnloadSound(ISoundEffect sound)
    {
        ThrowIfDisposed();

        if (sound is SDL3SoundEffect sdlSound)
        {
            if (sdlSound.IsLoaded)
                StopTracksForAudio(sdlSound.Handle);

            lock (_assetLock)
            {
                _loadedSounds.Remove(sdlSound);
            }
            sdlSound.Dispose();
            _logger.LogDebug("Sound unloaded: {Name}", sound.Name);
        }
    }

    /// <inheritdoc/>
    public void UnloadMusic(IMusic music)
    {
        ThrowIfDisposed();

        if (music is SDL3Music sdlMusic)
        {
            if (sdlMusic.IsLoaded)
                StopTracksForAudio(sdlMusic.Handle);

            lock (_assetLock)
            {
                _loadedMusic.Remove(sdlMusic);
            }
            sdlMusic.Dispose();
            _logger.LogDebug("Music unloaded: {Name}", music.Name);
        }
    }

    /// <inheritdoc/>
    /// <remarks>
    /// MIX_LoadAudio is thread-safe per SDL documentation, so offloading to the
    /// thread pool via <see cref="Task.Run"/> is safe for concurrent loads.
    /// See: https://wiki.libsdl.org/SDL3_mixer/MIX_LoadAudio
    /// </remarks>
    public async Task<IMusic> LoadMusicAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        return await Task.Run(() => LoadMusic(path, cancellationToken), cancellationToken);
    }

    private SDL3Music LoadMusic(string path, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Music file not found: {path}");

        cancellationToken.ThrowIfCancellationRequested();

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
    public void PlayMusic(IMusic music, int loops = -1, long loopStartMs = 0, string? bus = null)
    {
        ThrowIfDisposed();

        if (music is not SDL3Music sdlMusic)
            throw new ArgumentException("Music must be an SDL3Music", nameof(music));

        if (!sdlMusic.IsLoaded)
            throw new InvalidOperationException("Music is not loaded");

        FinishCrossfade();

        // Reset entity volume so a new track does not inherit the previous track's value.
        Volatile.Write(ref _musicEntityVolume, 1.0f);
        _isMusicIndividuallyPaused = false;
        _musicBus = bus ?? "music";

        nint oldTrack;
        lock (_tracksLock)
        {
            oldTrack = _currentMusicTrack;
            if (oldTrack != nint.Zero)
            {
                _tracks.Remove(oldTrack);
                _trackBuses.Remove(oldTrack);
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

        var newTrack = CreateAndPlayMusicTrack(sdlMusic, loops, Volatile.Read(ref _musicVolume) * Volatile.Read(ref _musicEntityVolume), loopStartMs, _musicBus);
        if (newTrack == nint.Zero)
        {
            _logger.LogWarning("Failed to start music {Name}", music.Name);
            _musicBus = null;
            return;
        }

        lock (_tracksLock)
        {
            if (_tracks.ContainsKey(newTrack))
            {
                _currentMusicTrack = newTrack;
                IsMusicPlaying = true;
                IsMusicPaused = false;
            }
        }

        _logger.LogInformation("Playing music: {Name}", music.Name);
    }

    /// <inheritdoc/>
    public void CrossfadeMusic(IMusic music, float duration, int loops = -1, long loopStartMs = 0, string? bus = null)
    {
        ThrowIfDisposed();

        if (music is not SDL3Music sdlMusic)
            throw new ArgumentException("Music must be an SDL3Music", nameof(music));

        if (!sdlMusic.IsLoaded)
            throw new InvalidOperationException("Music is not loaded");

        FinishCrossfade();

        bool needsDirectPlay;
        lock (_tracksLock)
        {
            needsDirectPlay = duration <= 0f || _currentMusicTrack == nint.Zero;
            if (!needsDirectPlay)
            {
                _crossfadeOutTrack = _currentMusicTrack;
                _currentMusicTrack = nint.Zero;
                _fade.StartCrossfade(duration, Volatile.Read(ref _musicEntityVolume));
            }
        }

        // Reset entity volume so the incoming track starts clean.
        Volatile.Write(ref _musicEntityVolume, 1.0f);
        _isMusicIndividuallyPaused = false;
        _musicBus = bus ?? "music";

        if (needsDirectPlay)
        {
            PlayMusic(music, loops, loopStartMs, bus);
            return;
        }

        var newTrack = CreateAndPlayMusicTrack(sdlMusic, loops, 0f, loopStartMs, _musicBus);
        if (newTrack == nint.Zero)
        {
            _logger.LogWarning("Failed to create track for music crossfade {Name}, restoring previous", music.Name);
            lock (_tracksLock)
            {
                _currentMusicTrack = _crossfadeOutTrack;
                _crossfadeOutTrack = nint.Zero;
                Volatile.Write(ref _musicEntityVolume, _fade.OutgoingEntityVolume);
                _fade.Reset();
            }
            return;
        }

        lock (_tracksLock)
        {
            if (_tracks.ContainsKey(newTrack))
            {
                _currentMusicTrack = newTrack;
                IsMusicPlaying = true;
                IsMusicPaused = false;
            }
        }

        _logger.LogInformation("Crossfading to music: {Name} ({Duration}s)", music.Name, duration);
    }

    /// <inheritdoc/>
    public void Update(float deltaTime)
    {
        ThrowIfDisposed();

        ProcessDeferredDestroys();

        nint outTrack, inTrack;
        float fadeOutGain, fadeInGain, outEntityVol;
        bool complete;

        lock (_tracksLock)
        {
            if (!_fade.IsActive || IsMusicPaused)
                return;

            complete = _fade.Advance(deltaTime);
            outTrack = _crossfadeOutTrack;
            inTrack = _currentMusicTrack;
            fadeOutGain = _fade.FadeOutGain;
            fadeInGain = _fade.FadeInGain;
            outEntityVol = _fade.OutgoingEntityVolume;
        }

        float musicVol = Volatile.Read(ref _musicVolume);
        float entityVol = Volatile.Read(ref _musicEntityVolume);

        bool outAlive, inAlive;
        lock (_tracksLock)
        {
            outAlive = _tracks.ContainsKey(outTrack);
            inAlive = inTrack != nint.Zero && _tracks.ContainsKey(inTrack);
        }

        if (outAlive)
            SDL3.Mixer.SetTrackGain(outTrack, musicVol * outEntityVol * fadeOutGain);

        if (inAlive)
            SDL3.Mixer.SetTrackGain(inTrack, musicVol * entityVol * fadeInGain);

        if (complete)
            FinishCrossfade();
    }

    private void ProcessDeferredDestroys()
    {
        while (_deferredDestroys.TryDequeue(out var track))
            SDL3.Mixer.DestroyTrack(track);
    }

    private void FinishCrossfade()
    {
        nint outTrack;
        nint inTrack;
        lock (_tracksLock)
        {
            if (!_fade.IsActive)
                return;

            outTrack = _crossfadeOutTrack;
            _crossfadeOutTrack = nint.Zero;
            _fade.Reset();
            _tracks.Remove(outTrack);
            _trackBuses.Remove(outTrack);
            inTrack = _currentMusicTrack;

            if (inTrack == nint.Zero)
            {
                IsMusicPlaying = false;
                IsMusicPaused = false;
                _isMusicIndividuallyPaused = false;
                _musicBus = null;
            }
        }

        if (outTrack != nint.Zero)
        {
            SDL3.Mixer.StopTrack(outTrack, 0);
            SDL3.Mixer.DestroyTrack(outTrack);
        }

        if (inTrack != nint.Zero)
            SDL3.Mixer.SetTrackGain(inTrack, Volatile.Read(ref _musicVolume) * Volatile.Read(ref _musicEntityVolume));
    }

    private nint CreateAndPlayMusicTrack(SDL3Music sdlMusic, int loops, float initialGain, long loopStartMs = 0, string? bus = null)
    {
        if (!sdlMusic.TryGetHandle(out var audioHandle))
            return nint.Zero;

        var newTrack = SDL3.Mixer.CreateTrack(_mixer);
        if (newTrack == nint.Zero)
            return nint.Zero;

        SDL3.Mixer.SetTrackGain(newTrack, initialGain);

        if (!SDL3.Mixer.SetTrackAudio(newTrack, audioHandle))
        {
            SDL3.Mixer.DestroyTrack(newTrack);
            return nint.Zero;
        }

        var resolvedBus = bus ?? "music";
        SDL3.Mixer.TagTrack(newTrack, resolvedBus);

        var props = SDL3.SDL.CreateProperties();
        if (props == nint.Zero)
        {
            SDL3.Mixer.DestroyTrack(newTrack);
            return nint.Zero;
        }

        try
        {
            SDL3.SDL.SetNumberProperty(props, MixerPlayLoops, loops);
            SDL3.SDL.SetPointerProperty(props, MixerPlayStoppedCallback, _trackStoppedCallbackPtr);
            SDL3.SDL.SetPointerProperty(props, MixerPlayStoppedUserdata, newTrack);

            if (loopStartMs > 0)
                SDL3.SDL.SetNumberProperty(props, MixerPlayLoopStartMs, loopStartMs);

            // Music tracks are intentionally excluded from _trackVolumes.
            // _trackVolumes stores per-sound volume multipliers scaled by SoundVolume;
            // music gain is driven by MusicVolume and crossfade logic instead.
            lock (_tracksLock)
            {
                _tracks[newTrack] = audioHandle;
                _trackBuses[newTrack] = new HashSet<string>(StringComparer.Ordinal) { resolvedBus };
            }

            if (!SDL3.Mixer.PlayTrack(newTrack, props))
            {
                lock (_tracksLock)
                {
                    _tracks.Remove(newTrack);
                    _trackBuses.Remove(newTrack);
                }
                SDL3.Mixer.DestroyTrack(newTrack);
                return nint.Zero;
            }
        }
        finally
        {
            SDL3.SDL.DestroyProperties(props);
        }

        return newTrack;
    }

    /// <inheritdoc/>
    public void PauseMusic()
    {
        ThrowIfDisposed();

        nint track, fadeOut;
        lock (_tracksLock)
        {
            track = _currentMusicTrack;
            fadeOut = _crossfadeOutTrack;
            if (track != nint.Zero || fadeOut != nint.Zero)
            {
                IsMusicPaused = true;
                _isMusicIndividuallyPaused = true;
            }
        }

        if (track != nint.Zero)
            SDL3.Mixer.PauseTrack(track);
        if (fadeOut != nint.Zero)
            SDL3.Mixer.PauseTrack(fadeOut);

        if (track != nint.Zero || fadeOut != nint.Zero)
            _logger.LogDebug("Music paused");
    }

    /// <inheritdoc/>
    public void ResumeMusic()
    {
        ThrowIfDisposed();

        _isMusicIndividuallyPaused = false;

        nint track, fadeOut;
        string? bus;
        lock (_tracksLock)
        {
            track = _currentMusicTrack;
            fadeOut = _crossfadeOutTrack;
            bus = _musicBus;
        }

        if (track == nint.Zero && fadeOut == nint.Zero)
            return;

        if (bus != null && _pausedBuses.Contains(bus))
            return;

        IsMusicPaused = false;

        if (track != nint.Zero)
            SDL3.Mixer.ResumeTrack(track);
        if (fadeOut != nint.Zero)
            SDL3.Mixer.ResumeTrack(fadeOut);

        _logger.LogDebug("Music resumed");
    }

    /// <inheritdoc/>
    public void StopMusic(float fadeDuration = 0f)
    {
        ThrowIfDisposed();

        if (fadeDuration <= 0f)
        {
            StopMusicCore();
            return;
        }

        FinishCrossfade();

        lock (_tracksLock)
        {
            if (_currentMusicTrack == nint.Zero)
                return;

            _crossfadeOutTrack = _currentMusicTrack;
            _currentMusicTrack = nint.Zero;
            _fade.StartFadeOut(fadeDuration, Volatile.Read(ref _musicEntityVolume));
        }

        _logger.LogDebug("Fading out music over {Duration}s", fadeDuration);
    }

    private void StopMusicCore()
    {
        FinishCrossfade();

        nint track;
        lock (_tracksLock)
        {
            track = _currentMusicTrack;
            if (track != nint.Zero)
            {
                _tracks.Remove(track);
                _trackBuses.Remove(track);
                _currentMusicTrack = nint.Zero;
                IsMusicPlaying = false;
                IsMusicPaused = false;
                _isMusicIndividuallyPaused = false;
                _musicBus = null;
            }
        }

        if (track != nint.Zero)
        {
            SDL3.Mixer.StopTrack(track, 0);
            SDL3.Mixer.DestroyTrack(track);
            _logger.LogDebug("Music stopped");
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        _logger.LogInformation("Disposing audio service");

        FinishCrossfade();
        StopMusicCore();
        StopAllSounds();
        ProcessDeferredDestroys();

        List<nint> remainingTracks;
        lock (_tracksLock)
        {
            remainingTracks = [.. _tracks.Keys];
            _tracks.Clear();
            _trackVolumes.Clear();
            _trackPriorities.Clear();
            _trackAges.Clear();
            _trackBuses.Clear();
        }

        _pausedBuses.Clear();
        _busVolumes.Clear();

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
            _logger.LogDebug(ex, "Exception during SDL3_mixer teardown; SDL may have already exited");
        }

        GC.KeepAlive(_trackStoppedCallback);
    }

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

    private void ReapplySoundTrackGains(float soundVolume)
    {
        lock (_tracksLock)
        {
            foreach (var (track, vol) in _trackVolumes)
                SDL3.Mixer.SetTrackGain(track, vol * soundVolume);
        }
    }

    /// <inheritdoc/>
    public void SetTrackPitch(nint track, float pitch)
    {
        ThrowIfDisposed();
        pitch = Math.Clamp(pitch, 0.25f, 4.0f);

        lock (_tracksLock)
        {
            if (!_tracks.ContainsKey(track))
                return;
        }

        SDL3.Mixer.SetTrackFrequencyRatio(track, pitch);
    }

    /// <inheritdoc/>
    public double MusicPositionMs
    {
        get
        {
            nint track;
            lock (_tracksLock)
            {
                track = _currentMusicTrack;
            }

            if (track == nint.Zero)
                return -1;

            long frames = SDL3.Mixer.GetTrackPlaybackPosition(track);
            if (frames < 0)
                return -1;

            return SDL3.Mixer.TrackFramesToMS(track, frames);
        }
    }

    /// <inheritdoc/>
    public void SeekMusic(double positionMs)
    {
        ThrowIfDisposed();

        nint track;
        lock (_tracksLock)
        {
            track = _currentMusicTrack;
        }

        if (track == nint.Zero)
            return;

        positionMs = Math.Max(positionMs, 0);
        long frames = SDL3.Mixer.TrackMSToFrames(track, (long)positionMs);
        if (frames >= 0)
            SDL3.Mixer.SetTrackPlaybackPosition(track, frames);
    }

    /// <inheritdoc/>
    public double MusicDurationMs
    {
        get
        {
            nint audioHandle;
            lock (_tracksLock)
            {
                if (_currentMusicTrack == nint.Zero
                    || !_tracks.TryGetValue(_currentMusicTrack, out audioHandle))
                    return -1;
            }

            long frames = SDL3.Mixer.GetAudioDuration(audioHandle);
            if (frames < 0) // MIX_DURATION_UNKNOWN (-1) or MIX_DURATION_INFINITE (-2)
                return -1;

            return SDL3.Mixer.AudioFramesToMS(audioHandle, frames);
        }
    }

    /// <inheritdoc/>
    public void SetMusicTrackVolume(float volume)
    {
        ThrowIfDisposed();
        volume = Math.Clamp(volume, 0f, 1f);
        Volatile.Write(ref _musicEntityVolume, volume);

        nint track;
        bool crossfading;
        lock (_tracksLock)
        {
            track = _currentMusicTrack;
            crossfading = _fade.IsActive;
        }

        if (!crossfading && track != nint.Zero)
            SDL3.Mixer.SetTrackGain(track, Volatile.Read(ref _musicVolume) * volume);
    }

    /// <inheritdoc/>
    public void SetMusicPitch(float pitch)
    {
        ThrowIfDisposed();
        pitch = Math.Clamp(pitch, 0.25f, 4.0f);

        nint track;
        lock (_tracksLock)
        {
            track = _currentMusicTrack;
        }

        if (track != nint.Zero)
            SDL3.Mixer.SetTrackFrequencyRatio(track, pitch);
    }

    /// <inheritdoc/>
    public void TagTrack(nint track, string bus)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(bus);

        lock (_tracksLock)
        {
            if (!_tracks.ContainsKey(track))
                return;

            if (!_trackBuses.TryGetValue(track, out var buses))
            {
                buses = new HashSet<string>(StringComparer.Ordinal);
                _trackBuses[track] = buses;
            }

            buses.Add(bus);
        }

        SDL3.Mixer.TagTrack(track, bus);
    }

    /// <inheritdoc/>
    public void SetBusVolume(string bus, float volume)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(bus);
        var clamped = Math.Clamp(volume, 0f, 1f);
        _busVolumes[bus] = clamped;
        SDL3.Mixer.SetTagGain(_mixer, bus, clamped);
    }

    /// <inheritdoc/>
    public float GetBusVolume(string bus)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(bus);
        return _busVolumes.TryGetValue(bus, out var volume) ? volume : 1f;
    }

    /// <inheritdoc/>
    public void PauseBus(string bus)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(bus);
        _pausedBuses.Add(bus);
        SDL3.Mixer.PauseTag(_mixer, bus);

        lock (_tracksLock)
        {
            if (string.Equals(_musicBus, bus, StringComparison.Ordinal)
                && (_currentMusicTrack != nint.Zero || _crossfadeOutTrack != nint.Zero))
            {
                IsMusicPaused = true;
            }
        }

        _logger.LogDebug("Paused bus {Bus}", bus);
    }

    /// <inheritdoc/>
    public void ResumeBus(string bus)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(bus);
        _pausedBuses.Remove(bus);
        SDL3.Mixer.ResumeTag(_mixer, bus);

        nint track = nint.Zero, fadeOut = nint.Zero;
        bool rePause = false;
        lock (_tracksLock)
        {
            if (string.Equals(_musicBus, bus, StringComparison.Ordinal)
                && (_currentMusicTrack != nint.Zero || _crossfadeOutTrack != nint.Zero))
            {
                if (_isMusicIndividuallyPaused)
                {
                    track = _currentMusicTrack;
                    fadeOut = _crossfadeOutTrack;
                    rePause = true;
                }
                else
                {
                    IsMusicPaused = false;
                }
            }
        }

        if (rePause)
        {
            if (track != nint.Zero)
                SDL3.Mixer.PauseTrack(track);
            if (fadeOut != nint.Zero)
                SDL3.Mixer.PauseTrack(fadeOut);
        }

        _logger.LogDebug("Resumed bus {Bus}", bus);
    }

    /// <inheritdoc/>
    public void StopBus(string bus, float fadeOutSeconds = 0f)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(bus);
        _pausedBuses.Remove(bus);

        bool isMusicBus;
        lock (_tracksLock)
        {
            isMusicBus = string.Equals(_musicBus, bus, StringComparison.Ordinal)
                         && (_currentMusicTrack != nint.Zero || _crossfadeOutTrack != nint.Zero);
        }

        if (isMusicBus)
            StopMusic(fadeOutSeconds);

        if (isMusicBus && fadeOutSeconds > 0f)
        {
            // The music track is fading via the managed state machine. StopTag
            // would also stop that track and cancel the fade, so stop non-music
            // tracks on this bus individually instead.
            StopNonMusicTracksOnBus(bus);
            _logger.LogDebug("Stopped bus {Bus} (managed fade {Fade}s)", bus, fadeOutSeconds);
            return;
        }

        SDL3.Mixer.StopTag(_mixer, bus, (long)(Math.Max(fadeOutSeconds, 0f) * 1000f));
        _logger.LogDebug("Stopped bus {Bus} (fade {Fade}s)", bus, fadeOutSeconds);
    }

    /// <summary>
    /// Stops non-music tracks on a bus individually. Used by <see cref="StopBus"/>
    /// when a managed music fade is active to avoid <c>StopTag</c> cancelling the fade.
    /// </summary>
    private void StopNonMusicTracksOnBus(string bus)
    {
        List<nint>? tracksToStop = null;
        lock (_tracksLock)
        {
            foreach (var (track, buses) in _trackBuses)
            {
                if (track == _currentMusicTrack || track == _crossfadeOutTrack)
                    continue;
                if (buses.Contains(bus))
                {
                    tracksToStop ??= [];
                    tracksToStop.Add(track);
                }
            }
        }

        if (tracksToStop == null) return;

        foreach (var track in tracksToStop)
            RemoveTrack(track, stop: true);
    }

    /// <inheritdoc/>
    public bool IsBusPaused(string bus)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(bus);
        return _pausedBuses.Contains(bus);
    }

    /// <inheritdoc/>
    public void SetTrackPan(nint track, float pan)
    {
        ThrowIfDisposed();

        lock (_tracksLock)
        {
            if (!_tracks.ContainsKey(track))
                return;
        }

        ApplyStereoPan(track, pan);
    }

    /// <inheritdoc/>
    public void SetTrackVolumeAndPan(nint track, float volume, float pan)
    {
        ThrowIfDisposed();

        volume = Math.Clamp(volume, 0f, 1f);

        lock (_tracksLock)
        {
            if (!_tracks.ContainsKey(track))
                return;

            _trackVolumes[track] = volume;
        }

        SDL3.Mixer.SetTrackGain(track, volume * Volatile.Read(ref _soundVolume));
        ApplyStereoPan(track, pan);
    }

    /// <inheritdoc/>
    public void SetTrackVolume(nint track, float volume)
    {
        ThrowIfDisposed();

        volume = Math.Clamp(volume, 0f, 1f);

        lock (_tracksLock)
        {
            if (!_tracks.ContainsKey(track))
                return;

            _trackVolumes[track] = volume;
        }

        SDL3.Mixer.SetTrackGain(track, volume * Volatile.Read(ref _soundVolume));
    }
}