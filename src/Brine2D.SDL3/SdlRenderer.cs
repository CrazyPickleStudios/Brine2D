using System;
using System.Drawing;
using Brine2D.Engine;
using SDL3;

namespace Brine2D.SDL3;

internal sealed class SdlRenderer : IRenderContext
{
    private readonly IntPtr _renderer;
    internal IntPtr Raw => _renderer;

    public SdlRenderer(SdlWindow window)
    {
        _renderer = SDL.CreateRenderer(window.RawHandle, null);

        if (_renderer == IntPtr.Zero)
        {
            throw new InvalidOperationException($"SDL renderer creation failed: {SDL.GetError()}");
        }
    }

    public void Clear(Color color)
    {
        SDL.SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
        SDL.RenderClear(_renderer);
    }

    public void DrawRect(Rectangle rect, Color color)
    {
        SDL.SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
        var sdlRect = new SDL.FRect { X = rect.X, Y = rect.Y, W = rect.Width, H = rect.Height };
        SDL.RenderFillRect(_renderer, sdlRect);
    }

    public void DrawTexture(ITexture texture, Rectangle dest, Rectangle? src = null, Color? tint = null)
    {
        if (texture is not SdlTexture sdlTex)
        {
            throw new ArgumentException("Unsupported texture type for SDL renderer.", nameof(texture));
        }

        if (tint is { } c)
        {
            SDL.SetTextureColorMod(sdlTex.Handle, c.R, c.G, c.B);
            SDL.SetTextureAlphaMod(sdlTex.Handle, c.A);
        }
        else
        {
            SDL.SetTextureColorMod(sdlTex.Handle, 255, 255, 255);
            SDL.SetTextureAlphaMod(sdlTex.Handle, 255);
        }

        SDL.FRect? sdlSrc = null;

        if (src is { } s)
        {
            sdlSrc = new SDL.FRect { X = s.X, Y = s.Y, W = s.Width, H = s.Height };
        }

        var sdlDst = new SDL.FRect { X = dest.X, Y = dest.Y, W = dest.Width, H = dest.Height };

        if (sdlSrc is { } srcRect)
        {
            SDL.RenderTexture(_renderer, sdlTex.Handle, in srcRect, in sdlDst);
        }
        else
        {
            SDL.RenderTexture(_renderer, sdlTex.Handle,  (IntPtr)null, in sdlDst);
        }
    }

    public void Present()
    {
        SDL.RenderPresent(_renderer);
    }

    public void Dispose()
    {
        if (_renderer != IntPtr.Zero)
        {
            SDL.DestroyRenderer(_renderer);
        }
    }
}