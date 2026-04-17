using Microsoft.Extensions.Logging;

namespace Brine2D.Audio;

/// <summary>
/// No-op audio service for headless mode (servers, testing).
/// All audio operations are silently ignored. Volume properties are initialised
/// from <see cref="AudioOptions"/> so configured values are observable in tests.
/// </summary>
/// <remarks>
/// <para>
/// Because no real playback occurs, non-looping sound-effect tracks are expired on
/// the next <see cref="Update"/> call. Looping tracks (loops &lt; 0) remain alive
/// until explicitly stopped via <see cref="StopTrack"/> or <see cref="StopBus"/>.
/// Non-looping music is likewise completed on the next <see cref="Update"/> call.
/// </para>
/// <para>
/// When all tracks are in use a new sound whose priority is equal to or higher than
/// the lowest active track will evict that track; otherwise the request is rejected.
/// </para>
/// </remarks>
internal sealed class HeadlessAudioService : IAudioService
{
    private readonly ILogger<HeadlessAudioService> _logger;
    private readonly HashSet<nint> _activeTracks = new();
    private readonly HashSet<nint> _finitePlaybackTracks = new();
    private readonly HashSet<nint> _pausedTracks = new();
    private readonly HashSet<nint> _individuallyPausedTracks = new();
    private readonly Dictionary<nint, int> _trackPriorities = new();
    private readonly Dictionary<nint, long> _trackAges = new();
    private readonly Dictionary<nint, HashSet<string>> _trackBuses = new();
    private readonly Dictionary<string, float> _busVolumes = new(StringComparer.Ordinal);
    private readonly HashSet<string> _pausedBuses = new(StringComparer.Ordinal);
    private readonly List<nint> _expiredTracks = new();
    private nint _nextTrackHandle;
    private long _nextTrackAge;
    private MusicFade _fade;
    private int _disposed;
    private bool _finiteMusic;
    private string? _musicBus;
    private bool _isMusicIndividuallyPaused;

    public float MasterVolume
    {
        get => field;
        set => field = Math.Clamp(value, 0f, 1f);
    }

    public float MusicVolume
    {
        get => field;
        set => field = Math.Clamp(value, 0f, 1f);
    }

    public float SoundVolume
    {
        get => field;
        set => field = Math.Clamp(value, 0f, 1f);
    }

    public bool IsMusicPlaying { get; private set; }
    public bool IsMusicPaused { get; private set; }
    public bool IsMusicFadingOut => _fade.IsFadingOut;
    public int ActiveSoundTrackCount => _activeTracks.Count;
    public int MaxSoundTracks { get; }

    public double MusicPositionMs => -1;
    public double MusicDurationMs => -1;

    public HeadlessAudioService(AudioOptions options, ILogger<HeadlessAudioService> logger)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentOutOfRangeException.ThrowIfLessThan(options.MaxTracks, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(options.MaxTracks, 32);
        MasterVolume = options.MasterVolume;
        MusicVolume = options.MusicVolume;
        SoundVolume = options.SoundVolume;
        MaxSoundTracks = options.MaxTracks;
        _logger = logger;
        _logger.LogInformation("Headless audio service initialized (no audio will play)");
    }

    public Task<ISoundEffect> LoadSoundAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogTrace("LoadSoundAsync ignored (headless): {Path}", path);
        return Task.FromResult<ISoundEffect>(new HeadlessSoundEffect(path));
    }

    public Task<IMusic> LoadMusicAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        cancellationToken.ThrowIfCancellationRequested();
        _logger.LogTrace("LoadMusicAsync ignored (headless): {Path}", path);
        return Task.FromResult<IMusic>(new HeadlessMusic(path));
    }

    public nint PlaySound(ISoundEffect sound, float volume = 1.0f, int loops = 0, float pan = 0f, float pitch = 1f, int priority = 0, string? bus = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(sound);

        if (_activeTracks.Count >= MaxSoundTracks)
        {
            nint evictTrack = nint.Zero;
            int lowestPriority = int.MaxValue;
            long oldestAge = long.MaxValue;
            foreach (var (t, p) in _trackPriorities)
            {
                var age = _trackAges.GetValueOrDefault(t, long.MaxValue);
                if (p < lowestPriority || (p == lowestPriority && age < oldestAge))
                {
                    lowestPriority = p;
                    evictTrack = t;
                    oldestAge = age;
                }
            }

            if (evictTrack == nint.Zero || priority < lowestPriority)
            {
                _logger.LogTrace("PlaySound ignored (headless): max tracks reached");
                return nint.Zero;
            }

            _activeTracks.Remove(evictTrack);
            _finitePlaybackTracks.Remove(evictTrack);
            _pausedTracks.Remove(evictTrack);
            _individuallyPausedTracks.Remove(evictTrack);
            _trackPriorities.Remove(evictTrack);
            _trackAges.Remove(evictTrack);
            _trackBuses.Remove(evictTrack);
        }

        var handle = ++_nextTrackHandle;
        _activeTracks.Add(handle);
        _trackPriorities[handle] = priority;
        _trackAges[handle] = _nextTrackAge++;

        if (loops >= 0)
            _finitePlaybackTracks.Add(handle);

        if (bus != null)
            _trackBuses[handle] = new HashSet<string>(StringComparer.Ordinal) { bus };

        _logger.LogTrace("PlaySound (headless): track {Track}", handle);
        return handle;
    }

    public void StopTrack(nint track)
    {
        ThrowIfDisposed();
        _activeTracks.Remove(track);
        _finitePlaybackTracks.Remove(track);
        _pausedTracks.Remove(track);
        _individuallyPausedTracks.Remove(track);
        _trackPriorities.Remove(track);
        _trackAges.Remove(track);
        _trackBuses.Remove(track);
        _logger.LogTrace("StopTrack (headless): {Track}", track);
    }

    public void StopAllSounds()
    {
        ThrowIfDisposed();
        _expiredTracks.Clear();
        foreach (var track in _activeTracks)
            _expiredTracks.Add(track);

        foreach (var track in _expiredTracks)
        {
            _activeTracks.Remove(track);
            _finitePlaybackTracks.Remove(track);
            _pausedTracks.Remove(track);
            _individuallyPausedTracks.Remove(track);
            _trackPriorities.Remove(track);
            _trackAges.Remove(track);
            _trackBuses.Remove(track);
        }

        _logger.LogTrace("StopAllSounds (headless)");
    }

    public void PauseAllSounds()
    {
        ThrowIfDisposed();
        foreach (var track in _activeTracks)
            _pausedTracks.Add(track);
        _logger.LogTrace("PauseAllSounds (headless)");
    }

    public void ResumeAllSounds()
    {
        ThrowIfDisposed();
        _pausedTracks.Clear();
        _individuallyPausedTracks.Clear();
        _logger.LogTrace("ResumeAllSounds (headless)");
    }

    public bool IsTrackAlive(nint track)
    {
        if (track == nint.Zero) return false;
        ThrowIfDisposed();
        return _activeTracks.Contains(track);
    }

    public void PauseTrack(nint track)
    {
        ThrowIfDisposed();
        if (_activeTracks.Contains(track))
        {
            _pausedTracks.Add(track);
            _individuallyPausedTracks.Add(track);
        }
    }

    public void ResumeTrack(nint track)
    {
        ThrowIfDisposed();
        _pausedTracks.Remove(track);
        _individuallyPausedTracks.Remove(track);
    }

    public void TagTrack(nint track, string bus)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(bus);
        if (!_activeTracks.Contains(track))
            return;

        if (!_trackBuses.TryGetValue(track, out var buses))
        {
            buses = new HashSet<string>(StringComparer.Ordinal);
            _trackBuses[track] = buses;
        }

        buses.Add(bus);
    }

    public void SetBusVolume(string bus, float volume)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(bus);
        _busVolumes[bus] = Math.Clamp(volume, 0f, 1f);
        _logger.LogTrace("SetBusVolume ignored (headless): {Bus} = {Volume}", bus, volume);
    }

    public float GetBusVolume(string bus)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(bus);
        return _busVolumes.TryGetValue(bus, out var volume) ? volume : 1f;
    }

    public void PauseBus(string bus)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(bus);
        _pausedBuses.Add(bus);
        foreach (var (track, buses) in _trackBuses)
        {
            if (buses.Contains(bus))
                _pausedTracks.Add(track);
        }

        if (IsMusicPlaying && string.Equals(_musicBus, bus, StringComparison.Ordinal))
            IsMusicPaused = true;

        _logger.LogTrace("PauseBus (headless): {Bus}", bus);
    }

    public void ResumeBus(string bus)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(bus);
        _pausedBuses.Remove(bus);
        foreach (var (track, buses) in _trackBuses)
        {
            if (!buses.Contains(bus) || _individuallyPausedTracks.Contains(track))
                continue;

            bool stillPausedByOtherBus = false;
            foreach (var b in buses)
            {
                if (!string.Equals(b, bus, StringComparison.Ordinal) && _pausedBuses.Contains(b))
                {
                    stillPausedByOtherBus = true;
                    break;
                }
            }

            if (!stillPausedByOtherBus)
                _pausedTracks.Remove(track);
        }

        if (IsMusicPlaying && string.Equals(_musicBus, bus, StringComparison.Ordinal))
            IsMusicPaused = _isMusicIndividuallyPaused;

        _logger.LogTrace("ResumeBus (headless): {Bus}", bus);
    }

    public void StopBus(string bus, float fadeOutSeconds = 0f)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(bus);
        _pausedBuses.Remove(bus);

        bool isMusicBus = string.Equals(_musicBus, bus, StringComparison.Ordinal) && IsMusicPlaying;

        if (isMusicBus)
            StopMusic(fadeOutSeconds);

        _expiredTracks.Clear();
        foreach (var (track, buses) in _trackBuses)
        {
            if (buses.Contains(bus))
                _expiredTracks.Add(track);
        }

        foreach (var track in _expiredTracks)
        {
            _activeTracks.Remove(track);
            _finitePlaybackTracks.Remove(track);
            _pausedTracks.Remove(track);
            _individuallyPausedTracks.Remove(track);
            _trackPriorities.Remove(track);
            _trackAges.Remove(track);
            _trackBuses.Remove(track);
        }

        _logger.LogTrace("StopBus (headless): {Bus}", bus);
    }

    public bool IsBusPaused(string bus)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(bus);
        return _pausedBuses.Contains(bus);
    }

    public void UnloadSound(ISoundEffect sound)
    {
        ThrowIfDisposed();
        _logger.LogTrace("UnloadSound ignored (headless)");
        sound?.Dispose();
    }

    public void PlayMusic(IMusic music, int loops = -1, long loopStartMs = 0, string? bus = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(music);
        _logger.LogTrace("PlayMusic ignored (headless)");
        _fade.Reset();
        IsMusicPlaying = true;
        IsMusicPaused = false;
        _isMusicIndividuallyPaused = false;
        _finiteMusic = loops != -1;
        _musicBus = bus ?? "music";
    }

    public void CrossfadeMusic(IMusic music, float duration, int loops = -1, long loopStartMs = 0, string? bus = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(music);
        _logger.LogTrace("CrossfadeMusic ignored (headless)");
        _fade.Reset();
        IsMusicPlaying = true;
        IsMusicPaused = false;
        _isMusicIndividuallyPaused = false;
        _finiteMusic = loops != -1;
        _musicBus = bus ?? "music";
    }

    public void Update(float deltaTime)
    {
        ThrowIfDisposed();

        if (_finitePlaybackTracks.Count > 0)
        {
            _expiredTracks.Clear();
            foreach (var track in _finitePlaybackTracks)
            {
                if (!_pausedTracks.Contains(track))
                    _expiredTracks.Add(track);
            }

            foreach (var track in _expiredTracks)
            {
                _activeTracks.Remove(track);
                _finitePlaybackTracks.Remove(track);
                _pausedTracks.Remove(track);
                _individuallyPausedTracks.Remove(track);
                _trackPriorities.Remove(track);
                _trackAges.Remove(track);
                _trackBuses.Remove(track);
            }
        }

        if (_fade.IsFadingOut && !IsMusicPaused)
        {
            if (_fade.Advance(deltaTime))
            {
                _fade.Reset();
                IsMusicPlaying = false;
                IsMusicPaused = false;
                _isMusicIndividuallyPaused = false;
                _finiteMusic = false;
                _musicBus = null;
            }
        }

        if (_finiteMusic && IsMusicPlaying && !IsMusicPaused && !_fade.IsFadingOut)
        {
            IsMusicPlaying = false;
            _finiteMusic = false;
            _musicBus = null;
        }
    }

    public void StopMusic(float fadeDuration = 0f)
    {
        ThrowIfDisposed();

        if (fadeDuration <= 0f || !IsMusicPlaying)
        {
            _logger.LogTrace("StopMusic (headless)");
            _fade.Reset();
            IsMusicPlaying = false;
            IsMusicPaused = false;
            _isMusicIndividuallyPaused = false;
            _finiteMusic = false;
            _musicBus = null;
            return;
        }

        _fade.StartFadeOut(fadeDuration, 1.0f);
        _logger.LogTrace("StopMusic(fade) started (headless)");
    }

    public void PauseMusic()
    {
        ThrowIfDisposed();
        _logger.LogTrace("PauseMusic ignored (headless)");
        if (IsMusicPlaying)
        {
            IsMusicPaused = true;
            _isMusicIndividuallyPaused = true;
        }
    }

    public void ResumeMusic()
    {
        ThrowIfDisposed();
        _logger.LogTrace("ResumeMusic ignored (headless)");
        _isMusicIndividuallyPaused = false;
        if (IsMusicPlaying && (_musicBus == null || !_pausedBuses.Contains(_musicBus)))
            IsMusicPaused = false;
    }

    public void UnloadMusic(IMusic music)
    {
        ThrowIfDisposed();
        _logger.LogTrace("UnloadMusic ignored (headless)");
        music?.Dispose();
    }

    public void SetTrackVolume(nint track, float volume)
    {
        ThrowIfDisposed();
    }

    public void SetTrackPan(nint track, float pan)
    {
        ThrowIfDisposed();
    }

    public void SetTrackVolumeAndPan(nint track, float volume, float pan)
    {
        ThrowIfDisposed();
    }

    public void SetTrackPitch(nint track, float pitch)
    {
        ThrowIfDisposed();
    }

    public void SeekMusic(double positionMs)
    {
        ThrowIfDisposed();
    }

    public void SetMusicTrackVolume(float volume)
    {
        ThrowIfDisposed();
    }

    public void SetMusicPitch(float pitch)
    {
        ThrowIfDisposed();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        _activeTracks.Clear();
        _finitePlaybackTracks.Clear();
        _pausedTracks.Clear();
        _individuallyPausedTracks.Clear();
        _trackPriorities.Clear();
        _trackAges.Clear();
        _trackBuses.Clear();
        _busVolumes.Clear();
        _pausedBuses.Clear();
        _isMusicIndividuallyPaused = false;
        _musicBus = null;
        _logger.LogDebug("Headless audio service disposed");
    }

    private void ThrowIfDisposed() =>
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);

    private sealed class HeadlessSoundEffect(string name) : ISoundEffect
    {
        private int _disposed;

        public string Name { get; } = name;
        public bool IsLoaded => Volatile.Read(ref _disposed) == 0;
        public void Dispose() => Interlocked.Exchange(ref _disposed, 1);
    }

    private sealed class HeadlessMusic(string name) : IMusic
    {
        private int _disposed;

        public string Name { get; } = name;
        public bool IsLoaded => Volatile.Read(ref _disposed) == 0;
        public void Dispose() => Interlocked.Exchange(ref _disposed, 1);
    }
}