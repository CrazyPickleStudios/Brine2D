using Brine2D.Engine;
using Brine2D.Graphics;
using Microsoft.Extensions.Logging;
using SDL3;
using System;
using System.Collections.Concurrent;
using System.Drawing;

namespace Brine2D.SDL3;

internal sealed class SdlRenderer : IRenderContext
{
    private readonly IntPtr _renderer;
    internal IntPtr Raw => _renderer;

    private readonly ConcurrentQueue<Action> _workQueue = new();
    private readonly ILogger<SdlRenderer>? _logger;

    public SdlRenderer(SdlWindow window, ILogger<SdlRenderer>? logger = null)
    {
        _logger = logger;
        _renderer = SDL.CreateRenderer(window.RawHandle, null);
        if (_renderer == IntPtr.Zero)
        {
            throw new InvalidOperationException($"SDL renderer creation failed: {SDL.GetError()}");
        }
    }

    public void Enqueue(Action action)
    {
        if (action is null) throw new ArgumentNullException(nameof(action));
        _workQueue.Enqueue(action);
    }

    internal void DrainWorkQueue()
    {
        while (_workQueue.TryDequeue(out var action))
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                _logger?.LogWarning(ex, "Render work item threw an exception and was swallowed to keep the loop running.");
            }
        }
    }

    public void Clear(Color color)
    {
        SDL.SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
        SDL.RenderClear(_renderer);
    }

    public void DrawRect(RectangleF rect, Color color)
    {
        SDL.SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
        var sdlRect = new SDL.FRect { X = rect.X, Y = rect.Y, W = rect.Width, H = rect.Height };
        SDL.RenderFillRect(_renderer, sdlRect);
    }

    public void DrawTexture(ITexture texture, RectangleF dest, RectangleF? src = null, Color? tint = null)
    {
        DrawTexture(texture, dest, src, tint, rotationDegrees: 0f, origin: null, flip: FlipMode.None);
    }

    public void DrawTexture(
        ITexture texture,
        RectangleF dest,
        RectangleF? src,
        Color? tint,
        float rotationDegrees,
        PointF? origin,
        FlipMode flip)
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

        var center = origin.HasValue
            ? new SDL.FPoint { X = origin.Value.X, Y = origin.Value.Y }
            : new SDL.FPoint { X = 0f, Y = 0f };

        var sdlFlip = flip switch
        {
            FlipMode.None => SDL.FlipMode.None,
            FlipMode.Horizontal => SDL.FlipMode.Horizontal,
            FlipMode.Vertical => SDL.FlipMode.Vertical,
            FlipMode.HorizontalVertical => SDL.FlipMode.Horizontal | SDL.FlipMode.Vertical,
            _ => SDL.FlipMode.None
        };

        if (sdlSrc is { } srcRect)
        {
            SDL.RenderTextureRotated(_renderer, sdlTex.Handle, in srcRect, in sdlDst, rotationDegrees, center, sdlFlip);
        }
        else
        {
            SDL.RenderTextureRotated(_renderer, sdlTex.Handle, (IntPtr)null, in sdlDst, rotationDegrees, center, sdlFlip);
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