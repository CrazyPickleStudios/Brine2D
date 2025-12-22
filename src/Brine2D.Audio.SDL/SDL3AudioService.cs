using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Brine2D.Audio.SDL;

/// <summary>
/// SDL3_mixer implementation of audio service.
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

    public void PlaySound(ISoundEffect sound, float? volume = null, int loops = 0)
    {
        if (sound is not SDL3SoundEffect sdlSound)
            throw new ArgumentException("Sound must be an SDL3SoundEffect", nameof(sound));

        if (!sdlSound.IsLoaded)
            throw new InvalidOperationException("Sound is not loaded");

        var track = SDL3.Mixer.CreateTrack(_mixer);
        
        if (track == IntPtr.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogWarning("Failed to create track for sound {Name}: {Error}", sound.Name, error);
            return;
        }

        _tracks.Add(track);

        var gain = volume ?? SoundVolume;
        SDL3.Mixer.SetTrackGain(track, gain);

        if (!SDL3.Mixer.SetTrackAudio(track, sdlSound.Handle))
        {
            var error = SDL3.SDL.GetError();
            _logger.LogWarning("Failed to set track audio for {Name}: {Error}", sound.Name, error);
            SDL3.Mixer.DestroyTrack(track);
            _tracks.Remove(track);
            return;
        }

        uint props = 0;
        if (loops != 0)
        {
            props = SDL3.SDL.CreateProperties();
            SDL3.SDL.SetNumberProperty(props, "SDL_mixer.play.loops", loops);
        }

        if (!SDL3.Mixer.PlayTrack(track, props))
        {
            var error = SDL3.SDL.GetError();
            _logger.LogWarning("Failed to play sound {Name}: {Error}", sound.Name, error);
            SDL3.Mixer.DestroyTrack(track);
            _tracks.Remove(track);
        }

        if (props != 0)
        {
            SDL3.SDL.DestroyProperties(props);
        }
    }

    public void StopAllSounds()
    {
        SDL3.Mixer.StopAllTracks(_mixer, 0);

        foreach (var track in _tracks.ToList())
        {
            if (track != _currentMusicTrack)
            {
                SDL3.Mixer.DestroyTrack(track);
                _tracks.Remove(track);
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

    public void PlayMusic(IMusic music, int loops = -1, int fadeInMs = 0)
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

        if (fadeInMs > 0)
        {
            SDL3.SDL.SetNumberProperty(props, "SDL_mixer.play.fade_ms", fadeInMs);
        }

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

    public void StopMusic(int fadeOutMs = 0)
    {
        if (_currentMusicTrack != IntPtr.Zero)
        {
            SDL3.Mixer.StopTrack(_currentMusicTrack, fadeOutMs);
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