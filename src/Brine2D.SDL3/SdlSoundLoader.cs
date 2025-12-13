using System.Runtime.InteropServices;
using Brine2D.Audio;
using Brine2D.Content;
using SDL3;

namespace Brine2D.SDL3;

internal sealed class SdlSoundLoader : IAssetLoader<ISound>
{
    private readonly SdlAudio _audio;

    public SdlSoundLoader(SdlAudio audio)
    {
        _audio = audio;
    }

    public Task<ISound> LoadAsync(string path, CancellationToken ct = default)
    {
        var audio = Mixer.LoadAudio(_audio.MixerHandle, path, true);

        if (audio == IntPtr.Zero)
        {
            throw new InvalidOperationException($"MIX_LoadAudio failed: {SDL.GetError()}");
        }

        var seconds = Mixer.GetAudioDuration(audio);

        return Task.FromResult<ISound>(new SdlSound(audio, seconds));
    }

    public Task<ISound> LoadAsync(Stream stream, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();

        stream.CopyTo(ms);

        var data = ms.ToArray();
        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);

        try
        {
            var io = SDL.IOFromConstMem(handle.AddrOfPinnedObject(), (nuint)data.Length);
            var audio = Mixer.LoadAudioIO(_audio.MixerHandle, io, true, true);

            if (audio == IntPtr.Zero)
            {
                throw new InvalidOperationException($"MIX_LoadAudioIO failed: {SDL.GetError()}");
            }

            var seconds = Mixer.GetAudioDuration(audio);

            return Task.FromResult<ISound>(new SdlSound(audio, seconds));
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