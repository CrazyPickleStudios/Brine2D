using Brine2D.Audio;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Brine2D.SDL.Audio;

/// <summary>
/// SDL3_mixer implementation of audio service with spatial audio support and proper callbacks.
/// </summary>
public class SDL3AudioService : IAudioService
{
    private readonly ILogger<SDL3AudioService> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly List<SDL3SoundEffect> _loadedSounds = new();
    private readonly List<SDL3Music> _loadedMusic = new();
    private readonly List<nint> _tracks = new();
    private nint _mixer;
    private bool _disposed;

    private float _soundVolume = 1.0f;

    public float MasterVolume
    {
        get => SDL3.Mixer.GetMasterGain(_mixer);
        set => SDL3.Mixer.SetMasterGain(_mixer, Math.Clamp(value, 0f, 10f));
    }

    public float MusicVolume { get; set; } = 1.0f;
    public float SoundVolume
    {
        get => _soundVolume;
        set => _soundVolume = Math.Clamp(value, 0f, 10f);
    }

    public bool IsMusicPlaying { get; private set; }
    public bool IsMusicPaused { get; private set; }

    private nint _currentMusicTrack;

    public event Action<nint>? OnTrackStopped;

    private readonly Dictionary<nint, TrackCallbackData> _trackCallbacks = new();

    [UnmanagedFunctionPointer(CallingConvention.Cdecl)]
    private delegate void TrackStoppedCallback(nint userdata);

    private struct TrackCallbackData
    {
        public TrackStoppedCallback Callback;
        public nint Track;
    }

    public SDL3AudioService(ILogger<SDL3AudioService> logger, ILoggerFactory loggerFactory)
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

    public async Task<ISoundEffect> LoadSoundAsync(string path, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => LoadSound(path), cancellationToken);
    }

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

    public void PlaySound(ISoundEffect sound, float? volume = null, int loops = 0, float pan = 0f)
    {
        PlaySoundWithTrack(sound, volume, loops, pan);
    }

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

    private void OnTrackStoppedNative(nint track)
    {
        _logger.LogDebug("Track {Track} stopped (audio thread callback)", track);
        _trackCallbacks.Remove(track);
        OnTrackStopped?.Invoke(track);
    }

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

    public void UnloadSound(ISoundEffect sound)
    {
        if (sound is SDL3SoundEffect sdlSound)
        {
            _loadedSounds.Remove(sdlSound);
            sdlSound.Dispose();
            _logger.LogDebug("Sound unloaded: {Name}", sound.Name);
        }
    }

    public async Task<IMusic> LoadMusicAsync(string path, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => LoadMusic(path), cancellationToken);
    }

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

    public void PauseMusic()
    {
        if (_currentMusicTrack != IntPtr.Zero)
        {
            SDL3.Mixer.PauseTrack(_currentMusicTrack);
            IsMusicPaused = true;
            _logger.LogDebug("Music paused");
        }
    }

    public void ResumeMusic()
    {
        if (_currentMusicTrack != IntPtr.Zero)
        {
            SDL3.Mixer.ResumeTrack(_currentMusicTrack);
            IsMusicPaused = false;
            _logger.LogDebug("Music resumed");
        }
    }

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

    public void UnloadMusic(IMusic music)
    {
        if (music is SDL3Music sdlMusic)
        {
            _loadedMusic.Remove(sdlMusic);
            sdlMusic.Dispose();
            _logger.LogDebug("Music unloaded: {Name}", music.Name);
        }
    }

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