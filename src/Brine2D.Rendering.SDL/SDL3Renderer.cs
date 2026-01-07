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

    public Color ClearColor { get; set; } = Color.Black;

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
    
    /// <summary>
    /// Draws a filled rectangle.
    /// </summary>
    public void DrawRectangleFilled(float x, float y, float width, float height, Color color)
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

    /// <summary>
    /// Draws a rectangle outline (no fill).
    /// </summary>
    public void DrawRectangleOutline(float x, float y, float width, float height, Color color, float thickness = 1f)
    {
        ThrowIfNotInitialized();
        
        // Apply camera transform if camera is set
        var position = new Vector2(x, y);
        if (_camera != null)
        {
            position = _camera.WorldToScreen(position);
            width *= _camera.Zoom;
            height *= _camera.Zoom;
            thickness *= _camera.Zoom;
        }
        
        // Enable alpha blending
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

        // For thickness > 1, draw multiple rectangles
        if (thickness > 1f)
        {
            for (float t = 0; t < thickness; t++)
            {
                var thickRect = new SDL3.SDL.FRect
                {
                    X = rect.X + t,
                    Y = rect.Y + t,
                    W = rect.W - t * 2,
                    H = rect.H - t * 2
                };
                SDL3.SDL.RenderRect(_renderer, ref thickRect);
            }
        }
        else
        {
            SDL3.SDL.RenderRect(_renderer, ref rect);
        }
        
        // Reset blend mode to default
        if (color.A < 255)
        {
            SDL3.SDL.SetRenderDrawBlendMode(_renderer, SDL3.SDL.BlendMode.None);
        }
    }

    public void DrawLine(float x1, float y1, float x2, float y2, Color color, float thickness = 1f)
    {
        if (!IsInitialized)
        {
            _logger?.LogWarning("Attempted to draw line before renderer initialization");
            return;
        }

        // Transform by camera if present
        if (Camera != null)
        {
            var start = Camera.WorldToScreen(new Vector2(x1, y1));
            var end = Camera.WorldToScreen(new Vector2(x2, y2));
            x1 = start.X;
            y1 = start.Y;
            x2 = end.X;
            y2 = end.Y;
            thickness *= Camera.Zoom;
        }

        // Enable alpha blending if needed
        if (color.A < 255)
        {
            SDL3.SDL.SetRenderDrawBlendMode(_renderer, SDL3.SDL.BlendMode.Blend);
        }

        SDL3.SDL.SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);

        if (thickness <= 1f)
        {
            // Simple thin line
            SDL3.SDL.RenderLine(_renderer, x1, y1, x2, y2);
        }
        else
        {
            // Thick line - draw as a rotated rectangle using multiple thin lines
            var dx = x2 - x1;
            var dy = y2 - y1;
            var length = MathF.Sqrt(dx * dx + dy * dy);

            if (length == 0) return;

            var angle = MathF.Atan2(dy, dx);
            var perpX = -MathF.Sin(angle);
            var perpY = MathF.Cos(angle);

            var halfThickness = thickness / 2f;

            // Draw multiple parallel lines to create thickness
            for (float offset = -halfThickness; offset <= halfThickness; offset += 0.5f)
            {
                var offsetX = perpX * offset;
                var offsetY = perpY * offset;

                SDL3.SDL.RenderLine(
                    _renderer,
                    x1 + offsetX,
                    y1 + offsetY,
                    x2 + offsetX,
                    y2 + offsetY
                );
            }
        }

        // Reset blend mode
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
            DrawRectangleFilled(charX, y, charWidth, charHeight, color);
        }
    }

    public void SetDefaultFont(IFont? font)
    {
        _defaultFont = font;
    }

    /// <summary>
    /// Draws a filled circle.
    /// </summary>
    public void DrawCircleFilled(float centerX, float centerY, float radius, Color color)
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
        float x = radius;
        float y = 0f;
        float radiusError = 1f - x;

        float cx = center.X;
        float cy = center.Y;

        while (x >= y)
        {
            // Draw horizontal lines to fill the circle
            SDL3.SDL.RenderLine(_renderer, cx - x, cy + y, cx + x, cy + y);
            SDL3.SDL.RenderLine(_renderer, cx - x, cy - y, cx + x, cy - y);
            SDL3.SDL.RenderLine(_renderer, cx - y, cy + x, cx + y, cy + x);
            SDL3.SDL.RenderLine(_renderer, cx - y, cy - x, cx + y, cy - x);

            y += 1f;
            if (radiusError < 0f)
            {
                radiusError += 2f * y + 1f;
            }
            else
            {
                x -= 1f;
                radiusError += 2f * (y - x + 1f);
            }
        }

        // Reset blend mode to default
        if (color.A < 255)
        {
            SDL3.SDL.SetRenderDrawBlendMode(_renderer, SDL3.SDL.BlendMode.None);
        }
    }

    /// <summary>
    /// Draws a circle outline (no fill).
    /// </summary>
    public void DrawCircleOutline(float centerX, float centerY, float radius, Color color, float thickness = 1f)
    {
        ThrowIfNotInitialized();

        // Apply camera transform
        var center = new Vector2(centerX, centerY);
        if (_camera != null)
        {
            center = _camera.WorldToScreen(center);
            radius *= _camera.Zoom;
            thickness *= _camera.Zoom;
        }

        // Enable alpha blending
        if (color.A < 255)
        {
            SDL3.SDL.SetRenderDrawBlendMode(_renderer, SDL3.SDL.BlendMode.Blend);
        }

        SDL3.SDL.SetRenderDrawColor(_renderer, color.R, color.G, color.B, color.A);

        // Fixed number of segments for smoothness (calculated based on radius)
        int segments = Math.Max(16, (int)(radius * 2)); // More segments for larger circles

        if (thickness <= 1f)
        {
            // Thin outline - single circle
            var points = new SDL3.SDL.FPoint[segments + 1];
            float angleStep = MathF.PI * 2f / segments;
            
            for (int i = 0; i < segments; i++)
            {
                float angle = i * angleStep;
                points[i] = new SDL3.SDL.FPoint
                {
                    X = center.X + MathF.Cos(angle) * radius,
                    Y = center.Y + MathF.Sin(angle) * radius
                };
            }
            points[segments] = points[0]; // Close the circle

            SDL3.SDL.RenderLines(_renderer, points, points.Length);
        }
        else
        {
            // Thick outline - draw multiple concentric circles
            float halfThickness = thickness / 2f;
            int numCircles = Math.Max(2, (int)thickness);
            
            for (int t = 0; t < numCircles; t++)
            {
                float offset = -halfThickness + (thickness * t / (numCircles - 1));
                float currentRadius = radius + offset;
                
                if (currentRadius <= 0) continue;
                
                var points = new SDL3.SDL.FPoint[segments + 1];
                float angleStep = MathF.PI * 2f / segments;
                
                for (int i = 0; i < segments; i++)
                {
                    float angle = i * angleStep;
                    points[i] = new SDL3.SDL.FPoint
                    {
                        X = center.X + MathF.Cos(angle) * currentRadius,
                        Y = center.Y + MathF.Sin(angle) * currentRadius
                    };
                }
                points[segments] = points[0];

                SDL3.SDL.RenderLines(_renderer, points, points.Length);
            }
        }

        if (color.A < 255)
        {
            SDL3.SDL.SetRenderDrawBlendMode(_renderer, SDL3.SDL.BlendMode.None);
        }
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
