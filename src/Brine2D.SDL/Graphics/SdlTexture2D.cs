using Brine2D.Core.Graphics;
using SDL;
using static SDL.SDL3;

namespace Brine2D.SDL.Graphics;

public sealed unsafe class SdlTexture2D : ITexture2D
{
    internal SdlTexture2D(SDL_GPUDevice* device, SDL_GPUTexture* texture, int width, int height, TextureFormat format)
    {
        Device = device;
        Texture = texture;
        Width = width;
        Height = height;
        Format = format;
    }

    public TextureFormat Format { get; }
    public int Height { get; }

    public int Width { get; }
    internal SDL_GPUDevice* Device { get; }
    internal SDL_GPUTexture* Texture { get; private set; }

    public void Dispose()
    {
        if (Texture != null && Device != null)
        {
            SDL_ReleaseGPUTexture(Device, Texture);
            Texture = null;
        }
    }
}