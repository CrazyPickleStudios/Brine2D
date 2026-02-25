using Microsoft.Extensions.Logging;

namespace Brine2D.Audio;

/// <summary>
/// No-op audio service for headless mode (servers, testing).
/// All audio operations are silently ignored. Volume properties are initialised
/// from <see cref="AudioOptions"/> so configured values are observable in tests.
/// </summary>
internal sealed class HeadlessAudioService : IAudioService
{
    private readonly ILogger<HeadlessAudioService>? _logger;

    public float MasterVolume { get; set; }
    public float MusicVolume { get; set; }
    public float SoundVolume { get; set; }

    public event Action<nint>? OnTrackStopped;

    public HeadlessAudioService(AudioOptions options, ILogger<HeadlessAudioService>? logger = null)
    {
        MasterVolume = options.MasterVolume;
        MusicVolume = options.MusicVolume;
        SoundVolume = options.SoundVolume;
        _logger = logger;
        _logger?.LogInformation("Headless audio service initialized (no audio will play)");
    }

    public Task<ISoundEffect> LoadSoundAsync(string path, CancellationToken cancellationToken = default)
    {
        _logger?.LogTrace("LoadSoundAsync ignored (headless): {Path}", path);
        return Task.FromResult<ISoundEffect>(new HeadlessSoundEffect(path));
    }

    public Task<IMusic> LoadMusicAsync(string path, CancellationToken cancellationToken = default)
    {
        _logger?.LogTrace("LoadMusicAsync ignored (headless): {Path}", path);
        return Task.FromResult<IMusic>(new HeadlessMusic(path));
    }

    public void PlaySound(ISoundEffect sound, float? volume = null, int loops = 0, float pan = 0f)
        => _logger?.LogTrace("PlaySound ignored (headless)");

    public nint PlaySoundWithTrack(ISoundEffect sound, float? volume = null, int loops = 0, float pan = 0f)
    {
        _logger?.LogTrace("PlaySoundWithTrack ignored (headless)");
        return nint.Zero;
    }

    public void StopTrack(nint track)
        => _logger?.LogTrace("StopTrack ignored (headless)");

    public void StopAllSounds()
        => _logger?.LogTrace("StopAllSounds ignored (headless)");

    public void UnloadSound(ISoundEffect sound)
    {
        _logger?.LogTrace("UnloadSound ignored (headless)");
        sound?.Dispose();
    }

    public void PlayMusic(IMusic music, int loops = -1)
        => _logger?.LogTrace("PlayMusic ignored (headless)");

    public void StopMusic()
        => _logger?.LogTrace("StopMusic ignored (headless)");

    public void PauseMusic()
        => _logger?.LogTrace("PauseMusic ignored (headless)");

    public void ResumeMusic()
        => _logger?.LogTrace("ResumeMusic ignored (headless)");

    public void UnloadMusic(IMusic music)
    {
        _logger?.LogTrace("UnloadMusic ignored (headless)");
        music?.Dispose();
    }

    public void UpdateTrackSpatialAudio(nint track, float volume, float pan)
        => _logger?.LogTrace("UpdateTrackSpatialAudio ignored (headless)");

    public void Dispose()
        => _logger?.LogDebug("Headless audio service disposed");
}

/// <summary>
/// No-op sound effect for headless mode.
/// </summary>
internal sealed class HeadlessSoundEffect : ISoundEffect
{
    public nint Handle => nint.Zero;
    public string Name { get; }
    public bool IsLoaded => true;

    public HeadlessSoundEffect(string name) => Name = name;

    public void Dispose() { }
}

/// <summary>
/// No-op music for headless mode.
/// </summary>
internal sealed class HeadlessMusic : IMusic
{
    public nint Handle => nint.Zero;
    public string Name { get; }
    public bool IsLoaded => true;

    public HeadlessMusic(string name) => Name = name;

    public void Dispose() { }
}