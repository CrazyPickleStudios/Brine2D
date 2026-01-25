using System.Drawing;
using Brine2D.Events;
using Brine2D.Rendering;
using Brine2D.SDL.Common;
using Brine2D.SDL.Common.Events;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Brine2D.SDL.Rendering;

/// <summary>
/// SDL3 legacy renderer implementation (uses SDL_Renderer API).
/// Simpler but less flexible than GPU renderer.
/// </summary>
public class SDL3Renderer : IRenderer, ISDL3WindowProvider, ITextureContext
{
    private readonly ILogger<SDL3Renderer> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly RenderingOptions _options;  
    private readonly IFontLoader? _fontLoader;
    private readonly EventBus? _eventBus;  

    private ViewportState _viewport;

    private nint _window;
    private nint _renderer;
    private ICamera? _camera;
    private Color _clearColor = Color.FromArgb(255, 52, 78, 65);

    private IFont? _defaultFont;
    private FontAtlas? _defaultFontAtlas;

    private bool _disposed;

    public nint Window => _window;
    public nint Device => _renderer;
    public bool IsInitialized { get; private set; }
    public Color ClearColor  
    { 
        get => _clearColor;
        set => _clearColor = value;
    }
    public ICamera? Camera
    {
        get => _camera;
        set => _camera = value;
    }

    public int Width => _viewport.Width;
    public int Height => _viewport.Height;

    public SDL3Renderer(
        ILogger<SDL3Renderer> logger,
        ILoggerFactory loggerFactory,
        IOptions<RenderingOptions> options,
        IFontLoader? fontLoader = null,
        EventBus? eventBus = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        _fontLoader = fontLoader;
        _eventBus = eventBus;

        _viewport = new ViewportState(_options.WindowWidth, _options.WindowHeight);
    }

    public async Task InitializeAsync(CancellationToken cancellationToken = default)
    {
        if (IsInitialized)
        {
            _logger.LogWarning("Renderer already initialized");
            return;
        }

        _logger.LogInformation("Initializing SDL3 legacy renderer");

        if (!SDL3.SDL.Init(SDL3.SDL.InitFlags.Video))
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to initialize SDL3: {Error}", error);
            throw new InvalidOperationException($"Failed to initialize SDL3: {error}");
        }

        SDL3.SDL.WindowFlags windowFlags = 0;
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

        // Set VSync
        if (!SDL3.SDL.SetRenderVSync(_renderer, _options.VSync ? 1 : 0))
        {
            _logger.LogWarning("Failed to set VSync: {Error}", SDL3.SDL.GetError());
        }

        await LoadDefaultFontAsync(cancellationToken);

        _eventBus?.Subscribe<WindowResizedEvent>(OnWindowResized);

        IsInitialized = true;
        _logger.LogInformation("SDL3 legacy renderer initialized successfully");
    }

    private void OnWindowResized(WindowResizedEvent evt)
    {
        _viewport.Update(evt.Width, evt.Height);
        _logger.LogInformation("Viewport resized to {Width}x{Height}", evt.Width, evt.Height);
    }

    public void BeginFrame()
    {
        ThrowIfNotInitialized();
        
        // Uses the property automatically
        SDL3.SDL.SetRenderDrawColor(_renderer, _clearColor.R, _clearColor.G, _clearColor.B, _clearColor.A);
        SDL3.SDL.RenderClear(_renderer);
    }

    public void EndFrame()
    {
        ThrowIfNotInitialized();
        SDL3.SDL.RenderPresent(_renderer);
    }

    public void DrawRectangleFilled(float x, float y, float width, float height, Color color)
    {
        ThrowIfNotInitialized();

        var position = ApplyCameraTransform(new Vector2(x, y));
        var size = ApplyCameraScale(new Vector2(width, height));

        var rect = new SDL3.SDL.FRect
        {
            X = position.X,
            Y = position.Y,
            W = size.X,
            H = size.Y
        };

        SDL3.SDL.SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
        SDL3.SDL.RenderFillRect(_renderer, ref rect);
    }

    public void DrawRectangleOutline(float x, float y, float width, float height, Color color, float thickness = 1f)
    {
        ThrowIfNotInitialized();

        // Draw 4 lines for outline
        DrawLine(x, y, x + width, y, color, thickness);
        DrawLine(x + width, y, x + width, y + height, color, thickness);
        DrawLine(x + width, y + height, x, y + height, color, thickness);
        DrawLine(x, y + height, x, y, color, thickness);
    }

    public void DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness = 1f)
    {
        ThrowIfNotInitialized();

        var p1 = ApplyCameraTransform(new Vector2(x1, y1));
        var p2 = ApplyCameraTransform(new Vector2(x2, y2));

        SDL3.SDL.SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);
        SDL3.SDL.RenderLine(_renderer, p1.X, p1.Y, p2.X, p2.Y);
    }

    public void DrawCircleFilled(float centerX, float centerY, float radius, Color color)
    {
        ThrowIfNotInitialized();

        var center = ApplyCameraTransform(new Vector2(centerX, centerY));
        var scaledRadius = ApplyCameraScale(new Vector2(radius, 0)).X;

        SDL3.SDL.SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);

        // Simple filled circle using horizontal lines
        for (int y = (int)-scaledRadius; y <= (int)scaledRadius; y++)
        {
            float width = MathF.Sqrt(scaledRadius * scaledRadius - y * y);
            SDL3.SDL.RenderLine(_renderer,
                center.X - width, center.Y + y,
                center.X + width, center.Y + y);
        }
    }

    public void DrawCircleOutline(float centerX, float centerY, float radius, Color color, float thickness = 1f)
    {
        ThrowIfNotInitialized();

        var center = ApplyCameraTransform(new Vector2(centerX, centerY));
        var scaledRadius = ApplyCameraScale(new Vector2(radius, 0)).X;

        SDL3.SDL.SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);

        // Bresenham's circle algorithm
        int x = (int)scaledRadius;
        int y = 0;
        int radiusError = 1 - x;

        while (x >= y)
        {
            SDL3.SDL.RenderPoint(_renderer, center.X + x, center.Y + y);
            SDL3.SDL.RenderPoint(_renderer, center.X + y, center.Y + x);
            SDL3.SDL.RenderPoint(_renderer, center.X - x, center.Y + y);
            SDL3.SDL.RenderPoint(_renderer, center.X - y, center.Y + x);
            SDL3.SDL.RenderPoint(_renderer, center.X - x, center.Y - y);
            SDL3.SDL.RenderPoint(_renderer, center.X - y, center.Y - x);
            SDL3.SDL.RenderPoint(_renderer, center.X + x, center.Y - y);
            SDL3.SDL.RenderPoint(_renderer, center.X + y, center.Y - x);

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
    }

    public void DrawTexture(ITexture texture, float x, float y)
    {
        ThrowIfNotInitialized();

        if (texture is not SDL3Texture sdlTexture)
            throw new ArgumentException("Texture must be an SDL3Texture", nameof(texture));

        if (!sdlTexture.IsLoaded)
            throw new InvalidOperationException("Texture is not loaded");

        var position = ApplyCameraTransform(new Vector2(x, y));
        var size = ApplyCameraScale(new Vector2(texture.Width, texture.Height));

        var destRect = new SDL3.SDL.FRect
        {
            X = position.X,
            Y = position.Y,
            W = size.X,
            H = size.Y
        };

        SDL3.SDL.RenderTexture(_renderer, sdlTexture.Handle, IntPtr.Zero, ref destRect);
    }

    public void DrawTexture(ITexture texture, float x, float y, float width, float height, 
        float rotation = 0f, Color? color = null)
    {
        ThrowIfNotInitialized();

        if (texture is not SDL3Texture sdlTexture)
            throw new ArgumentException("Texture must be an SDL3Texture", nameof(texture));

        if (!sdlTexture.IsLoaded)
            throw new InvalidOperationException("Texture is not loaded");

        var position = ApplyCameraTransform(new Vector2(x, y));
        var size = ApplyCameraScale(new Vector2(width, height));

        var destRect = new SDL3.SDL.FRect
        {
            X = position.X,
            Y = position.Y,
            W = size.X,
            H = size.Y
        };

        // Apply color tint if specified
        if (color.HasValue)
        {
            SDL3.SDL.SetTextureColorMod(sdlTexture.Handle, color.Value.R, color.Value.G, color.Value.B);
            SDL3.SDL.SetTextureAlphaMod(sdlTexture.Handle, color.Value.A);
        }

        // Use SDL's built-in rotation support (much simpler than GPU renderer!)
        if (rotation != 0f)
        {
            // Convert radians to degrees for SDL
            double angleDegrees = rotation * (180.0 / Math.PI);
            
            // Rotate around center
            var center = new SDL3.SDL.FPoint
            {
                X = size.X / 2f,
                Y = size.Y / 2f
            };

            SDL3.SDL.RenderTextureRotated(_renderer, sdlTexture.Handle, IntPtr.Zero, ref destRect,
                angleDegrees, ref center, SDL3.SDL.FlipMode.None);
        }
        else
        {
            SDL3.SDL.RenderTexture(_renderer, sdlTexture.Handle, IntPtr.Zero, ref destRect);
        }

        // Reset color mod if it was changed
        if (color.HasValue)
        {
            SDL3.SDL.SetTextureColorMod(sdlTexture.Handle, 255, 255, 255);
            SDL3.SDL.SetTextureAlphaMod(sdlTexture.Handle, 255);
        }
    }

    public void DrawTexture(ITexture texture,
        float sourceX, float sourceY, float sourceWidth, float sourceHeight,
        float destX, float destY, float destWidth, float destHeight,
        float rotation = 0f, Color? color = null)
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

        var position = ApplyCameraTransform(new Vector2(destX, destY));
        var size = ApplyCameraScale(new Vector2(destWidth, destHeight));

        var destRect = new SDL3.SDL.FRect
        {
            X = position.X,
            Y = position.Y,
            W = size.X,
            H = size.Y
        };

        // Apply color tint if specified
        if (color.HasValue)
        {
            SDL3.SDL.SetTextureColorMod(sdlTexture.Handle, color.Value.R, color.Value.G, color.Value.B);
            SDL3.SDL.SetTextureAlphaMod(sdlTexture.Handle, color.Value.A);
        }

        // Use SDL's built-in rotation support
        if (rotation != 0f)
        {
            // Convert radians to degrees for SDL
            double angleDegrees = rotation * (180.0 / Math.PI);
            
            // Rotate around center
            var center = new SDL3.SDL.FPoint
            {
                X = size.X / 2f,
                Y = size.Y / 2f
            };

            SDL3.SDL.RenderTextureRotated(_renderer, sdlTexture.Handle, ref sourceRect, ref destRect,
                angleDegrees, ref center, SDL3.SDL.FlipMode.None);
        }
        else
        {
            SDL3.SDL.RenderTexture(_renderer, sdlTexture.Handle, ref sourceRect, ref destRect);
        }

        // Reset color mod if it was changed
        if (color.HasValue)
        {
            SDL3.SDL.SetTextureColorMod(sdlTexture.Handle, 255, 255, 255);
            SDL3.SDL.SetTextureAlphaMod(sdlTexture.Handle, 255);
        }
    }

    public void DrawText(string text, float x, float y, Color color)
    {
        ThrowIfNotInitialized();

        if (string.IsNullOrEmpty(text))
            return;

        EnsureFontAtlasGenerated();

        if (_defaultFontAtlas == null || _defaultFontAtlas.Texture == null)
        {
            _logger.LogWarning("No font atlas available");
            return;
        }

        float cursorX = x;
        float cursorY = y;
        var atlasTexture = _defaultFontAtlas.Texture;

        var sdlTexture = ((SDL3Texture)atlasTexture).Handle;
        
        SDL3.SDL.SetTextureBlendMode(sdlTexture, SDL3.SDL.BlendMode.Blend);
        
        SDL3.SDL.SetTextureColorMod(sdlTexture, 255, 255, 255);
        SDL3.SDL.SetTextureAlphaMod(sdlTexture, 255);
        
        SDL3.SDL.SetTextureColorMod(sdlTexture, color.R, color.G, color.B);
        SDL3.SDL.SetTextureAlphaMod(sdlTexture, color.A);

        foreach (char c in text)
        {
            if (c == '\n')
            {
                cursorX = x;
                cursorY += _defaultFontAtlas.LineHeight;
                continue;
            }

            if (!_defaultFontAtlas.TryGetGlyph(c, out var glyph))
                continue;

            var srcRect = new SDL3.SDL.FRect
            {
                X = glyph.AtlasX,
                Y = glyph.AtlasY,
                W = glyph.Width,
                H = glyph.Height
            };

            var dstRect = new SDL3.SDL.FRect
            {
                X = cursorX,
                Y = cursorY,
                W = glyph.Width,
                H = glyph.Height
            };

            SDL3.SDL.RenderTexture(_renderer, sdlTexture, ref srcRect, ref dstRect);

            cursorX += glyph.Advance;
        }

        // Reset color mod
        SDL3.SDL.SetTextureColorMod(sdlTexture, 255, 255, 255);
        SDL3.SDL.SetTextureAlphaMod(sdlTexture, 255);
    }

    public void SetDefaultFont(IFont? font)
    {
        if (font != null && font is not SDL3Font)
        {
            _logger.LogWarning("Font must be an SDL3Font");
            return;
        }

        _defaultFont = font as SDL3Font;
        _defaultFontAtlas?.Dispose();
        _defaultFontAtlas = null;

        if (_defaultFont != null)
        {
            _logger.LogInformation("Default font set to {Font}", _defaultFont.Name);
        }
    }

    private void EnsureFontAtlasGenerated()
    {
        if (_defaultFont == null || _defaultFontAtlas != null)
            return;

        if (_defaultFont is not SDL3Font sdlFont)
        {
            _logger.LogWarning("Default font is not an SDL3Font");
            return;
        }

        _logger.LogInformation("Generating font atlas for {Font}", sdlFont.Name);
        _defaultFontAtlas = new FontAtlas(_loggerFactory.CreateLogger<FontAtlas>());

        if (!_defaultFontAtlas.Generate(sdlFont, this, TextureScaleMode.Nearest))
        {
            _logger.LogError("Failed to generate font atlas");
            _defaultFontAtlas?.Dispose();
            _defaultFontAtlas = null;
        }
    }

    private async Task LoadDefaultFontAsync(CancellationToken cancellationToken)
    {
        if (_fontLoader == null)
        {
            _logger.LogInformation("No font loader available");
            return;
        }

        try
        {
            var assembly = typeof(SDL3Renderer).Assembly;
            var resourceName = "Brine2D.SDL.Fonts.Roboto.ttf";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            if (stream == null)
            {
                _logger.LogWarning("Default font not found");
                return;
            }

            var tempPath = Path.Combine(Path.GetTempPath(), "Brine2D", "Roboto.ttf");
            Directory.CreateDirectory(Path.GetDirectoryName(tempPath)!);

            using (var fileStream = File.Create(tempPath))
            {
                await stream.CopyToAsync(fileStream, cancellationToken);
            }

            var loadedFont = await _fontLoader.LoadFontAsync(tempPath, 16, cancellationToken);

            if (loadedFont is SDL3Font sdlFont)
            {
                _defaultFont = sdlFont;
                _logger.LogInformation("Default font loaded");
            }
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load default font");
        }
    }

    public ITexture CreateTextureFromSurface(nint surface, int width, int height, TextureScaleMode scaleMode)
    {
        ThrowIfNotInitialized();

        var texture = SDL3.SDL.CreateTextureFromSurface(_renderer, surface);
        if (texture == IntPtr.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create texture from surface: {Error}", error);
            throw new InvalidOperationException($"Failed to create texture: {error}");
        }

        SDL3.SDL.SetTextureBlendMode(texture, SDL3.SDL.BlendMode.Blend);
        
        _logger.LogInformation("Created texture with blend mode: Blend");

        var sdlScaleMode = scaleMode == TextureScaleMode.Nearest
            ? SDL3.SDL.ScaleMode.Nearest
            : SDL3.SDL.ScaleMode.Linear;
        SDL3.SDL.SetTextureScaleMode(texture, sdlScaleMode);

        return new SDL3Texture(
            "surface_texture",
            texture,
            width,
            height,
            scaleMode,
            _loggerFactory.CreateLogger<SDL3Texture>());
    }

    public ITexture CreateBlankTexture(int width, int height, TextureScaleMode scaleMode)
    {
        ThrowIfNotInitialized();

        var texture = SDL3.SDL.CreateTexture(
            _renderer,
            SDL3.SDL.PixelFormat.RGBA8888,
            SDL3.SDL.TextureAccess.Target,
            width,
            height);

        if (texture == IntPtr.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create blank texture: {Error}", error);
            throw new InvalidOperationException($"Failed to create texture: {error}");
        }

        var sdlScaleMode = scaleMode == TextureScaleMode.Nearest
            ? SDL3.SDL.ScaleMode.Nearest
            : SDL3.SDL.ScaleMode.Linear;
        SDL3.SDL.SetTextureScaleMode(texture, sdlScaleMode);

        return new SDL3Texture(
            $"blank_{width}x{height}",
            texture,
            width,
            height,
            scaleMode,
            _loggerFactory.CreateLogger<SDL3Texture>());
    }

    public void ReleaseTexture(ITexture texture)
    {
        if (texture is SDL3Texture sdlTexture)
        {
            SDL3.SDL.DestroyTexture(sdlTexture.Handle);
            _logger.LogDebug("Released texture: {Source}", texture.Source);
        }
    }

    public void SetBlendMode(BlendMode blendMode)
    {
        var sdlBlendMode = blendMode switch
        {
            BlendMode.Alpha => SDL3.SDL.BlendMode.Blend,
            BlendMode.Additive => SDL3.SDL.BlendMode.Add,
            BlendMode.Multiply => SDL3.SDL.BlendMode.Mod,
            BlendMode.None => SDL3.SDL.BlendMode.None,
            _ => SDL3.SDL.BlendMode.Blend
        };
        
        SDL3.SDL.SetRenderDrawBlendMode(_renderer, sdlBlendMode);
    }

    // ============================================================
    // VECTOR2 OVERLOADS (delegate to float overloads)
    // ============================================================

    // Rectangles
    public void DrawRectangleFilled(Rectangle rect, Color color)
    {
        DrawRectangleFilled(rect.X, rect.Y, rect.Width, rect.Height, color);
    }

    public void DrawRectangleOutline(Rectangle rect, Color color, float thickness = 1f)
    {
        DrawRectangleOutline(rect.X, rect.Y, rect.Width, rect.Height, color, thickness);
    }

    // Circles
    public void DrawCircleFilled(Vector2 center, float radius, Color color)
    {
        DrawCircleFilled(center.X, center.Y, radius, color);
    }

    public void DrawCircleOutline(Vector2 center, float radius, Color color, float thickness = 1f)
    {
        DrawCircleOutline(center.X, center.Y, radius, color, thickness);
    }

    // Lines
    public void DrawLine(Vector2 start, Vector2 end, Color color, float thickness = 1f)
    {
        DrawLine(start.X, start.Y, end.X, end.Y, color, thickness);
    }

    private Vector2 ApplyCameraTransform(Vector2 position)
    {
        if (_camera == null) return position;
        return _camera.WorldToScreen(position);
    }

    private Vector2 ApplyCameraScale(Vector2 size)
    {
        if (_camera == null) return size;
        return size * _camera.Zoom;
    }

    private void ThrowIfNotInitialized()
    {
        if (!IsInitialized)
            throw new InvalidOperationException("Renderer is not initialized");
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("Disposing SDL3 legacy renderer");

        _eventBus?.Unsubscribe<WindowResizedEvent>(OnWindowResized);

        _defaultFontAtlas?.Dispose();
        _defaultFontAtlas = null;

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

    /// <summary>
    /// Encapsulates runtime viewport/window size separate from configuration.
    /// Follows ASP.NET principle of separating config (immutable) from runtime state (mutable).
    /// </summary>
    private sealed class ViewportState
    {
        public int Width { get; private set; }
        public int Height { get; private set; }

        public ViewportState(int width, int height)
        {
            Width = width;
            Height = height;
        }

        public void Update(int width, int height)
        {
            Width = width;
            Height = height;
        }
    }
}