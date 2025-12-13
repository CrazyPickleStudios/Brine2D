using System.Runtime.InteropServices;
using Brine2D.Audio;
using Microsoft.Extensions.Logging;
using SDL3;

namespace Brine2D.SDL3;

internal sealed class SdlAudio : IAudio, IDisposable
{
    private const int DefaultEffectTracks = 16;

    private readonly List<IntPtr> _allTracks = new();
    private readonly ILogger<SdlAudio> _logger;
    private readonly Dictionary<ISound, List<IntPtr>> _soundTracks = new();
    private readonly Stack<IntPtr> _trackPool = new();

    private float _master = 1.0f;

    public SdlAudio(ILogger<SdlAudio> logger)
    {
        _logger = logger;

        if (!Mixer.Init())
        {
            throw new InvalidOperationException($"MIX_Init failed: {SDL.GetError()}");
        }

        var spec = new SDL.AudioSpec
        {
            Freq = 48000,
            Format = SDL.AudioFormat.AudioF32LE,
            Channels = 2
        };

        var handle = GCHandle.Alloc(spec, GCHandleType.Pinned);

        try
        {
            MixerHandle = Mixer.CreateMixerDevice(SDL.AudioDeviceDefaultPlayback, handle.AddrOfPinnedObject());
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }

        if (MixerHandle == IntPtr.Zero)
        {
            throw new InvalidOperationException($"MIX_CreateMixerDevice failed: {SDL.GetError()}");
        }

        PreCreateTracks(DefaultEffectTracks);
    }

    public IMusic? CurrentMusic { get; private set; }

    public float MasterVolume
    {
        get => _master;
        set
        {
            _master = Math.Clamp(value, 0f, 1f);
            Mixer.SetMasterGain(MixerHandle, _master);
        }
    }

    internal IntPtr MixerHandle { get; }

    public void Dispose()
    {
        Mixer.StopAllTracks(MixerHandle, 0);

        foreach (var track in _allTracks)
        {
            if (track != IntPtr.Zero)
            {
                Mixer.DestroyTrack(track);
            }
        }

        _trackPool.Clear();
        _allTracks.Clear();
        _soundTracks.Clear();

        if (MixerHandle != IntPtr.Zero)
        {
            Mixer.DestroyMixer(MixerHandle);
        }

        Mixer.Quit();
    }

    public void PauseMusic()
    {
        Mixer.PauseAllTracks(MixerHandle);
    }

    public void Play(ISound sound, float volume = 1.0f, bool loop = false)
    {
        if (sound is not SdlSound s)
        {
            return;
        }

        var track = RentTrack();

        if (track == IntPtr.Zero)
        {
            _logger.LogWarning("No track available for sound playback: {Error}", SDL.GetError());
            return;
        }

        if (!Mixer.SetTrackAudio(track, s.Audio))
        {
            _logger.LogWarning("MIX_SetTrackAudio failed: {Error}", SDL.GetError());
            ReturnTrack(track);

            return;
        }

        var gain = Math.Clamp(volume * _master, 0f, 1f);
        Mixer.SetTrackGain(track, gain);

        var props = SDL.CreateProperties();
        SDL.SetNumberProperty(props, Mixer.Props.PlayLoopsNumber, loop ? -1 : 0);

        if (!Mixer.PlayTrack(track, props))
        {
            _logger.LogWarning("MIX_PlayTrack failed: {Error}", SDL.GetError());
            SDL.DestroyProperties(props);
            ReturnTrack(track);
            return;
        }

        SDL.DestroyProperties(props);

        if (!_soundTracks.TryGetValue(sound, out var tracks))
        {
            tracks = new List<IntPtr>(1);
            _soundTracks[sound] = tracks;
        }

        tracks.Add(track);
    }

    public void PlayMusic(IMusic music, float volume = 1.0f, bool loop = true)
    {
        if (music is not SdlMusic m)
        {
            return;
        }

        var track = RentTrack();

        if (track == IntPtr.Zero)
        {
            _logger.LogWarning("No track available for music playback: {Error}", SDL.GetError());
            return;
        }

        if (!Mixer.SetTrackAudio(track, m.Audio))
        {
            _logger.LogWarning("MIX_SetTrackAudio (music) failed: {Error}", SDL.GetError());
            ReturnTrack(track);
            return;
        }

        var gain = Math.Clamp(volume * _master, 0f, 1f);
        Mixer.SetTrackGain(track, gain);

        var props = SDL.CreateProperties();
        SDL.SetNumberProperty(props, Mixer.Props.PlayLoopsNumber, loop ? -1 : 1);

        if (!Mixer.PlayTrack(track, props))
        {
            _logger.LogWarning("MIX_PlayTrack (music) failed: {Error}", SDL.GetError());
            SDL.DestroyProperties(props);
            ReturnTrack(track);
            return;
        }

        SDL.DestroyProperties(props);

        CurrentMusic = music;
    }

    public void ResumeMusic()
    {
        Mixer.ResumeAllTracks(MixerHandle);
    }

    public void Stop(ISound sound)
    {
        if (!_soundTracks.TryGetValue(sound, out var tracks) || tracks.Count == 0)
        {
            return;
        }

        foreach (var track in tracks)
        {
            if (track != IntPtr.Zero)
            {
                Mixer.StopTrack(track, 0);
                ReturnTrack(track);
            }
        }

        _soundTracks.Remove(sound);
    }

    public void StopAll()
    {
        Mixer.StopAllTracks(MixerHandle, 0);
        ReturnAllTracksToPool();
        _soundTracks.Clear();

        CurrentMusic = null;
    }

    public void StopMusic()
    {
        Mixer.StopAllTracks(MixerHandle, 0);
        ReturnAllTracksToPool();

        CurrentMusic = null;
    }

    private void PreCreateTracks(int count)
    {
        for (var i = 0; i < count; i++)
        {
            var track = Mixer.CreateTrack(MixerHandle);

            if (track == IntPtr.Zero)
            {
                _logger.LogWarning("MIX_CreateTrack (pre-create) failed at index {Index}: {Error}", i, SDL.GetError());
                break;
            }

            _trackPool.Push(track);
            _allTracks.Add(track);
        }
    }

    private IntPtr RentTrack()
    {
        if (_trackPool.Count > 0)
        {
            return _trackPool.Pop();
        }

        var track = Mixer.CreateTrack(MixerHandle);

        if (track == IntPtr.Zero)
        {
            _logger.LogWarning("MIX_CreateTrack (expand) failed: {Error}", SDL.GetError());
            return IntPtr.Zero;
        }

        _allTracks.Add(track);
        return track;
    }

    private void ReturnAllTracksToPool()
    {
        _trackPool.Clear();

        foreach (var t in _allTracks)
        {
            if (t != IntPtr.Zero)
            {
                _trackPool.Push(t);
            }
        }
    }

    private void ReturnTrack(IntPtr track)
    {
        if (track == IntPtr.Zero)
        {
            return;
        }

        _trackPool.Push(track);
    }
}