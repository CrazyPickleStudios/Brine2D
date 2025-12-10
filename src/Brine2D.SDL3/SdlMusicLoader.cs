using System.Runtime.InteropServices;
using Brine2D.Engine;
using SDL3;

namespace Brine2D.SDL3;

internal sealed class SdlMusicLoader : IAssetLoader<IMusic>
{
    private readonly SdlAudio _audio;

    public SdlMusicLoader(SdlAudio audio)
    {
        _audio = audio;
    }

    public Task<IMusic> LoadAsync(string path, CancellationToken ct = default)
    {
        var audio = Mixer.LoadAudio(_audio.MixerHandle, path, false);

        if (audio == IntPtr.Zero)
        {
            throw new InvalidOperationException($"MIX_LoadAudio failed: {SDL.GetError()}");
        }

        var seconds = Mixer.GetAudioDuration(audio);

        return Task.FromResult<IMusic>(new SdlMusic(audio, seconds));
    }

    public Task<IMusic> LoadAsync(Stream stream, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();

        stream.CopyTo(ms);

        var data = ms.ToArray();

        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);

        try
        {
            var io = SDL.IOFromConstMem(handle.AddrOfPinnedObject(), (nuint)data.Length);
            var audio = Mixer.LoadAudioIO(_audio.MixerHandle, io, false, true);

            if (audio == IntPtr.Zero)
            {
                throw new InvalidOperationException($"MIX_LoadAudioIO failed: {SDL.GetError()}");
            }

            var seconds = Mixer.GetAudioDuration(audio);

            return Task.FromResult<IMusic>(new SdlMusic(audio, seconds));
        }
        finally
        {
            if (handle.IsAllocated)
            {
                handle.Free();
            }
        }
    }
}