using System.Runtime.InteropServices;
using Brine2D.Engine;
using SDL3;

namespace Brine2D.SDL3;

internal sealed class SdlTextureLoader : IAssetLoader<ITexture>
{
    private readonly SdlRenderer _renderer;

    public SdlTextureLoader(SdlRenderer renderer)
    {
        _renderer = renderer;
    }

    public Task<ITexture> LoadAsync(string path, CancellationToken ct = default)
    {
        var surface = Image.Load(path);

        if (surface == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to load image '{path}': {SDL.GetError()}");
        }

        var tex = SDL.CreateTextureFromSurface(_renderer.Raw, surface);
        SDL.DestroySurface(surface);

        if (tex == IntPtr.Zero)
        {
            throw new InvalidOperationException($"Failed to create texture: {SDL.GetError()}");
        }

        SDL.GetTextureSize(tex, out var w, out var h);

        return Task.FromResult<ITexture>(new SdlTexture(tex, w, h));
    }

    public Task<ITexture> LoadAsync(Stream stream, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var data = ms.ToArray();

        var handle = GCHandle.Alloc(data, GCHandleType.Pinned);

        try
        {
            var ptr = handle.AddrOfPinnedObject();
            var io = SDL.IOFromConstMem(ptr, (nuint)data.Length);

            if (io == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to create IO stream: {SDL.GetError()}");
            }

            var surface = Image.LoadIO(io, true);

            if (surface == IntPtr.Zero)
            {
                try
                {
                    SDL.CloseIO(io);
                }
                catch
                {
                    // No-op, ignore. -RP
                }

                throw new InvalidOperationException($"Failed to load image from stream: {SDL.GetError()}");
            }

            var tex = SDL.CreateTextureFromSurface(_renderer.Raw, surface);
            SDL.DestroySurface(surface);

            if (tex == IntPtr.Zero)
            {
                throw new InvalidOperationException($"Failed to create texture: {SDL.GetError()}");
            }

            SDL.GetTextureSize(tex, out var w, out var h);

            return Task.FromResult<ITexture>(new SdlTexture(tex, w, h));
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