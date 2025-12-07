using System.Drawing;
using Brine2D.Abstractions;
using Microsoft.Extensions.Logging;
using SDL3;

namespace Brine2D.SDL3;

public sealed class SdlRenderer : IRenderContext, IDisposable
{
    private readonly ILogger<SdlRenderer> _logger;
    private readonly SdlWindow _window;
    private IntPtr _renderer;

    public SdlRenderer(ILogger<SdlRenderer> logger, SdlWindow window)
    {
        _logger = logger;
        _window = window;
        _renderer = SDL.CreateRenderer(window.RawHandle, null);
        if (_renderer == IntPtr.Zero)
        {
            throw new InvalidOperationException($"SDL_CreateRenderer failed: {SDL.GetError()}");
        }
    }

    public void Clear(Color color)
    {
        SDL.SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
        SDL.RenderClear(_renderer);
    }

    public void Dispose()
    {
        if (_renderer != IntPtr.Zero)
        {
            SDL.DestroyRenderer(_renderer);
            _renderer = IntPtr.Zero;
        }
    }

    public void DrawRect(Rectangle rect, Color color)
    {
        SDL.SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
        var r = new SDL.FRect { X = rect.X, Y = rect.Y, W = rect.Width, H = rect.Height };
        SDL.RenderFillRect(_renderer, in r);
    }

    public void Present()
    {
        SDL.RenderPresent(_renderer);
    }
}