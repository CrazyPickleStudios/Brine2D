using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Brine2D.Rendering.SDL;

/// <summary>
/// SDL3 implementation of the renderer.
/// </summary>
public class SDL3Renderer : IRenderer
{
    private readonly ILogger<SDL3Renderer> _logger;
    private readonly RenderingOptions _options;
    private nint _window;
    private nint _renderer;
    private bool _disposed;

    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets the internal SDL renderer handle for texture creation.
    /// </summary>
    internal nint RendererHandle => _renderer;

    public SDL3Renderer(ILogger<SDL3Renderer> logger, IOptions<RenderingOptions> options)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
    }

    public Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            _logger.LogWarning("Renderer already initialized");
            return Task.CompletedTask;
        }

        _logger.LogInformation("Initializing SDL3 renderer");
        
        // Initialize SDL with Video, Gamepad, and Audio
        var initFlags = SDL3.SDL.InitFlags.Video | SDL3.SDL.InitFlags.Gamepad | SDL3.SDL.InitFlags.Audio;
        
        if (!SDL3.SDL.Init(initFlags))
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to initialize SDL3: {Error}", error);
            throw new InvalidOperationException($"Failed to initialize SDL3: {error}");
        }

        var windowFlags = SDL3.SDL.WindowFlags.OpenGL;
        if (_options.Resizable)
            windowFlags |= SDL3.SDL.WindowFlags.Resizable;
        if (_options.Fullscreen)
            windowFlags |= SDL3.SDL.WindowFlags.Fullscreen;

        _window = SDL3.SDL.CreateWindow(
            _options.WindowTitle,
            _options.WindowWidth,
            _options.WindowHeight,
            windowFlags
        );

        if (_window == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create window: {Error}", error);
            throw new InvalidOperationException($"Failed to create window: {error}");
        }

        _renderer = SDL3.SDL.CreateRenderer(_window, null);

        if (_renderer == nint.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create renderer: {Error}", error);
            throw new InvalidOperationException($"Failed to create renderer: {error}");
        }

        if (_options.VSync)
        {
            SDL3.SDL.SetRenderVSync(_renderer, 1);
        }

        IsInitialized = true;
        _logger.LogInformation("SDL3 renderer initialized successfully (Video + Gamepad)");

        return Task.CompletedTask;
    }

    public void Clear(Color color)
    {
        ThrowIfNotInitialized();
        SDL3.SDL.SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
        SDL3.SDL.RenderClear(_renderer);
    }

    public void BeginFrame()
    {
        ThrowIfNotInitialized();
    }

    public void EndFrame()
    {
        ThrowIfNotInitialized();
        SDL3.SDL.RenderPresent(_renderer);
    }

    public void DrawRectangle(float x, float y, float width, float height, Color color)
    {
        ThrowIfNotInitialized();
        SDL3.SDL.SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);

        var rect = new SDL3.SDL.FRect
        {
            X = x,
            Y = y,
            W = width,
            H = height
        };

        SDL3.SDL.RenderFillRect(_renderer, ref rect);
    }

    public void DrawTexture(ITexture texture, float x, float y)
    {
        ThrowIfNotInitialized();
        
        if (texture is not SDL3Texture sdlTexture)
            throw new ArgumentException("Texture must be an SDL3Texture", nameof(texture));

        if (!sdlTexture.IsLoaded)
            throw new InvalidOperationException("Texture is not loaded");

        var destRect = new SDL3.SDL.FRect
        {
            X = x,
            Y = y,
            W = texture.Width,
            H = texture.Height
        };

        SDL3.SDL.RenderTexture(_renderer, sdlTexture.Handle, IntPtr.Zero, ref destRect);
    }

    public void DrawTexture(ITexture texture, float x, float y, float width, float height)
    {
        ThrowIfNotInitialized();
        
        if (texture is not SDL3Texture sdlTexture)
            throw new ArgumentException("Texture must be an SDL3Texture", nameof(texture));

        if (!sdlTexture.IsLoaded)
            throw new InvalidOperationException("Texture is not loaded");

        var destRect = new SDL3.SDL.FRect
        {
            X = x,
            Y = y,
            W = width,
            H = height
        };

        SDL3.SDL.RenderTexture(_renderer, sdlTexture.Handle, IntPtr.Zero, ref destRect);
    }

    public void DrawTexture(ITexture texture,
        float sourceX, float sourceY, float sourceWidth, float sourceHeight,
        float destX, float destY, float destWidth, float destHeight)
    {
        ThrowIfNotInitialized();
        
        if (texture is not SDL3Texture sdlTexture)
            throw new ArgumentException("Texture must be an SDL3Texture", nameof(texture));

        if (!sdlTexture.IsLoaded)
            throw new InvalidOperationException("Texture is not loaded");

        var sourceRect = new SDL3.SDL.FRect
        {
            X = sourceX,
            Y = sourceY,
            W = sourceWidth,
            H = sourceHeight
        };

        var destRect = new SDL3.SDL.FRect
        {
            X = destX,
            Y = destY,
            W = destWidth,
            H = destHeight
        };

        SDL3.SDL.RenderTexture(_renderer, sdlTexture.Handle, ref sourceRect, ref destRect);
    }

    public void DrawText(string text, float x, float y, Color color)
    {
        // TODO: Implement text rendering
        _logger.LogWarning("Text rendering not yet implemented");
    }

    private void ThrowIfNotInitialized()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Renderer is not initialized");
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("Disposing SDL3 renderer");

        if (_renderer != nint.Zero)
        {
            SDL3.SDL.DestroyRenderer(_renderer);
            _renderer = nint.Zero;
        }

        if (_window != nint.Zero)
        {
            SDL3.SDL.DestroyWindow(_window);
            _window = nint.Zero;
        }

        SDL3.SDL.Quit();

        IsInitialized = false;
        _disposed = true;
    }
}
