using System.Numerics;
using Brine2D.SDL.Common;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Brine2D.Rendering.SDL;

/// <summary>
/// SDL3 implementation of the renderer.
/// </summary>
public class SDL3Renderer : IRenderer, ISDL3WindowProvider
{
    private readonly ILogger<SDL3Renderer> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly RenderingOptions _options;
    private readonly IFontLoader? _fontLoader;
    private nint _window;
    private nint _renderer;
    private bool _disposed;

    public bool IsInitialized { get; private set; }

    /// <summary>
    /// Gets the internal SDL renderer handle for texture creation.
    /// </summary>
    internal nint RendererHandle => _renderer;

    /// <summary>
    /// Gets the current SDL window handle.
    /// </summary>
    public nint Window => _window;

    private ICamera? _camera;

    public ICamera? Camera
    {
        get => _camera;
        set => _camera = value;
    }

    private IFont? _defaultFont;

    public SDL3Renderer(
        ILogger<SDL3Renderer> logger, 
        ILoggerFactory loggerFactory, 
        IOptions<RenderingOptions> options,
        IFontLoader? fontLoader = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory;
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _fontLoader = fontLoader;
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            _logger.LogWarning("Renderer already initialized");
            return;
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

        // Load embedded default font using the font loader (if available)
        if (_fontLoader != null)
        {
            await LoadDefaultFontAsync(cancellationToken);
        }
        else
        {
            _logger.LogInformation("No font loader available, text will use fallback rendering");
        }
        
        IsInitialized = true;
        _logger.LogInformation("SDL3 renderer initialized successfully (Video + Gamepad)");
    }

    private async Task LoadDefaultFontAsync(CancellationToken cancellationToken)
    {
        try
        {
            // Extract embedded font to temp location
            var assembly = typeof(SDL3Renderer).Assembly;
            var resourceName = "Brine2D.Rendering.SDL.Fonts.Roboto.ttf";
            
            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.LogWarning("Default font not found in embedded resources");
                return;
            }
            
            // Write to temp file (SDL_ttf requires file path)
            var tempPath = Path.Combine(Path.GetTempPath(), "Brine2D", "Roboto.ttf");
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);
            
            using (var fileStream = File.Create(tempPath))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }
            
            _logger.LogDebug("Font extracted to: {TempPath}", tempPath);
            
            // Use the font loader to load the font (it handles SDL_ttf init)
            _defaultFont = await _fontLoader!.LoadFontAsync(tempPath, 16, cancellationToken);
            _logger.LogInformation("Default font loaded from embedded resource");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load default font, text will use fallback rendering");
        }
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
        
        // Apply camera transform if camera is set
        var position = new Vector2(x, y);
        if (_camera != null)
        {
            position = _camera.WorldToScreen(position);
            width *= _camera.Zoom;
            height *= _camera.Zoom;
        }
        
        // Enable alpha blending for translucent rectangles
        if (color.A < 255)
        {
            SDL3.SDL.SetRenderDrawBlendMode(_renderer, SDL3.SDL.BlendMode.Blend);
        }
        
        SDL3.SDL.SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);

        var rect = new SDL3.SDL.FRect
        {
            X = position.X,
            Y = position.Y,
            W = width,
            H = height
        };

        SDL3.SDL.RenderFillRect(_renderer, ref rect);
        
        // Reset blend mode to default
        if (color.A < 255)
        {
            SDL3.SDL.SetRenderDrawBlendMode(_renderer, SDL3.SDL.BlendMode.None);
        }
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

        // Apply camera transform if camera is set
        var position = new Vector2(x, y);
        if (_camera != null)
        {
            position = _camera.WorldToScreen(position);
            width *= _camera.Zoom;
            height *= _camera.Zoom;
        }

        var destRect = new SDL3.SDL.FRect
        {
            X = position.X,
            Y = position.Y,
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

        // Apply camera transform
        var position = new Vector2(destX, destY);
        if (_camera != null)
        {
            position = _camera.WorldToScreen(position);
            destWidth *= _camera.Zoom;
            destHeight *= _camera.Zoom;
        }

        var destRect = new SDL3.SDL.FRect
        {
            X = position.X,
            Y = position.Y,
            W = destWidth,
            H = destHeight
        };

        SDL3.SDL.RenderTexture(_renderer, sdlTexture.Handle, ref sourceRect, ref destRect);
    }

    public void DrawText(string text, float x, float y, Color color)
    {
        ThrowIfNotInitialized();

        if (string.IsNullOrEmpty(text)) return;

        // If no default font, skip rendering (or use placeholder)
        if (_defaultFont == null || _defaultFont is not SDL3Font sdlFont || !sdlFont.IsLoaded)
        {
            // Fallback to simple rectangles (temporary)
            DrawTextFallback(text, x, y, color);
            return;
        }

        // Render text using SDL_ttf
        var sdlColor = new SDL3.SDL.Color { R = color.R, G = color.G, B = color.B, A = color.A };

        // Render to surface - ADD LENGTH PARAMETER
        var textLength = (UIntPtr)text.Length;
        var surface = SDL3.TTF.RenderTextBlended(sdlFont.Handle, text, textLength, sdlColor);
        
        if (surface == nint.Zero)
        {
            _logger.LogWarning("Failed to render text: {Text}", text);
            return;
        }

        try
        {
            // Create texture from surface
            var texture = SDL3.SDL.CreateTextureFromSurface(_renderer, surface);
            if (texture == nint.Zero)
            {
                _logger.LogWarning("Failed to create texture from text surface");
                return;
            }

            try
            {
                // Get texture size
                SDL3.SDL.GetTextureSize(texture, out var w, out var h);

                // Apply camera transform if needed
                var position = new Vector2(x, y);
                if (_camera != null)
                {
                    position = _camera.WorldToScreen(position);
                    w *= _camera.Zoom;
                    h *= _camera.Zoom;
                }

                var destRect = new SDL3.SDL.FRect
                {
                    X = position.X,
                    Y = position.Y,
                    W = w,
                    H = h
                };

                SDL3.SDL.RenderTexture(_renderer, texture, IntPtr.Zero, ref destRect);
            }
            finally
            {
                SDL3.SDL.DestroyTexture(texture);
            }
        }
        finally
        {
            SDL3.SDL.DestroySurface(surface);
        }
    }

    private void DrawTextFallback(string text, float x, float y, Color color)
    {
        const int charWidth = 8;
        const int charHeight = 12;
        const int spacing = 2;

        for (int i = 0; i < text.Length; i++)
        {
            var charX = x + (i * (charWidth + spacing));
            // This now respects camera transform via DrawRectangle
            DrawRectangle(charX, y, charWidth, charHeight, color);
        }
    }

    public void SetDefaultFont(IFont? font)
    {
        _defaultFont = font;
    }

    public void DrawCircle(float centerX, float centerY, float radius, Color color)
    {
        ThrowIfNotInitialized();
        
        // Apply camera transform if camera is set
        var center = new Vector2(centerX, centerY);
        if (_camera != null)
        {
            center = _camera.WorldToScreen(center);
            radius *= _camera.Zoom;
        }
        
        // Enable alpha blending for translucent circles
        if (color.A < 255)
        {
            SDL3.SDL.SetRenderDrawBlendMode(_renderer, SDL3.SDL.BlendMode.Blend);
        }
        
        SDL3.SDL.SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);

        // Draw filled circle using midpoint circle algorithm
        int x = (int)radius;
        int y = 0;
        int radiusError = 1 - x;

        while (x >= y)
        {
            // Draw horizontal lines to fill the circle
            DrawHorizontalLine((int)center.X - x, (int)center.X + x, (int)center.Y + y);
            DrawHorizontalLine((int)center.X - x, (int)center.X + x, (int)center.Y - y);
            DrawHorizontalLine((int)center.X - y, (int)center.X + y, (int)center.Y + x);
            DrawHorizontalLine((int)center.X - y, (int)center.X + y, (int)center.Y - x);

            y++;
            if (radiusError < 0)
            {
                radiusError += 2 * y + 1;
            }
            else
            {
                x--;
                radiusError += 2 * (y - x + 1);
            }
        }
        
        // Reset blend mode to default
        if (color.A < 255)
        {
            SDL3.SDL.SetRenderDrawBlendMode(_renderer, SDL3.SDL.BlendMode.None);
        }
    }

    private void DrawHorizontalLine(int x1, int x2, int y)
    {
        SDL3.SDL.RenderLine(_renderer, x1, y, x2, y);
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

        // Unload default font using the font loader (it handles cleanup)
        if (_defaultFont != null && _fontLoader != null)
        {
            _fontLoader.UnloadFont(_defaultFont);
            _defaultFont = null;
        }

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
