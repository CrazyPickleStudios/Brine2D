using System.Diagnostics;
using Brine2D.Core.Graphics;
using SDL;
using static SDL.SDL3;
using Brine2D.SDL.Hosting;

namespace Brine2D.SDL.Graphics;

public sealed unsafe class SdlTexture2D : ITexture2D, ITrackedResource
{
    private bool _disposed;

    internal SdlTexture2D(SDL_GPUDevice* device, SDL_GPUTexture* texture, int width, int height, TextureFormat format, string? debugName = null)
    {
        Device = device;
        Texture = texture;
        Width = width;
        Height = height;
        Format = format;
        DebugName = debugName;
    }

    public TextureFormat Format { get; }
    public int Height { get; }
    public int Width { get; }
    internal SDL_GPUDevice* Device { get; }
    internal SDL_GPUTexture* Texture { get; private set; }

    public string? DebugName { get; }
    public bool IsDisposed => _disposed;

    public void Dispose()
    {
        if (_disposed) return;
        if (Texture != null && Device != null)
        {
            SDL_ReleaseGPUTexture(Device, Texture);
            Texture = null;
        }

        _disposed = true;
    }

#if DEBUG
    ~SdlTexture2D()
    {
        if (!_disposed)
        {
            Debug.WriteLine($"[Leak] Finalizer ran for texture '{DebugName ?? "(unnamed)"}' without Dispose().");
        }
    }
#endif
}