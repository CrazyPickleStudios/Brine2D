using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Brine2D.Rendering.SDL;

/// <summary>
/// SDL3 implementation of texture loader using SDL3_image.
/// </summary>
public class SDL3TextureLoader : ITextureLoader
{
    private readonly ILogger<SDL3TextureLoader> _logger;
    private readonly ILoggerFactory _loggerFactory;
    private readonly nint _renderer;
    private readonly List<SDL3Texture> _loadedTextures = new();
    private bool _disposed;

    public SDL3TextureLoader(
        ILogger<SDL3TextureLoader> logger,
        ILoggerFactory loggerFactory,
        nint renderer)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _renderer = renderer;
        
        _logger.LogInformation("SDL3_image texture loader ready (PNG, JPG, BMP support)");
    }

    public async Task<ITexture> LoadTextureAsync(string path, CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => LoadTexture(path), cancellationToken);
    }

    public ITexture LoadTexture(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Texture file not found: {path}");

        _logger.LogInformation("Loading texture: {Path}", path);

        // Load surface from file using SDL_image
        var surface = SDL3.Image.Load(path);
        if (surface == IntPtr.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to load image {Path}: {Error}", path, error);
            throw new InvalidOperationException($"Failed to load image: {error}");
        }

        try
        {
            // Marshal the surface pointer to the Surface struct to read dimensions
            var surfaceStruct = Marshal.PtrToStructure<SDL3.SDL.Surface>(surface);
            var width = surfaceStruct.Width;
            var height = surfaceStruct.Height;

            if (width <= 0 || height <= 0)
            {
                _logger.LogError("Invalid surface dimensions for {Path}: {Width}x{Height}", path, width, height);
                throw new InvalidOperationException($"Invalid surface dimensions: {width}x{height}");
            }

            // Create texture from surface
            var texture = SDL3.SDL.CreateTextureFromSurface(_renderer, surface);
            if (texture == IntPtr.Zero)
            {
                var error = SDL3.SDL.GetError();
                _logger.LogError("Failed to create texture from surface {Path}: {Error}", path, error);
                throw new InvalidOperationException($"Failed to create texture: {error}");
            }

            var sdlTexture = new SDL3Texture(
                path,
                texture,
                width,
                height,
                _loggerFactory.CreateLogger<SDL3Texture>());

            _loadedTextures.Add(sdlTexture);
            _logger.LogInformation("Texture loaded: {Path} ({Width}x{Height})", path, width, height);

            return sdlTexture;
        }
        finally
        {
            // Always free the surface
            SDL3.SDL.DestroySurface(surface);
        }
    }

    public ITexture CreateTexture(int width, int height)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Width and height must be positive");

        _logger.LogDebug("Creating blank texture: {Width}x{Height}", width, height);

        var texture = SDL3.SDL.CreateTexture(
            _renderer,
            SDL3.SDL.PixelFormat.RGBA8888,
            SDL3.SDL.TextureAccess.Target,
            width,
            height);

        if (texture == IntPtr.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to create texture: {Error}", error);
            throw new InvalidOperationException($"Failed to create texture: {error}");
        }

        var sdlTexture = new SDL3Texture(
            $"blank_{width}x{height}",
            texture,
            width,
            height,
            _loggerFactory.CreateLogger<SDL3Texture>());

        _loadedTextures.Add(sdlTexture);
        return sdlTexture;
    }

    public void UnloadTexture(ITexture texture)
    {
        if (texture is SDL3Texture sdlTexture)
        {
            _loadedTextures.Remove(sdlTexture);
            sdlTexture.Dispose();
            _logger.LogDebug("Texture unloaded: {Source}", texture.Source);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("Disposing texture loader and {Count} textures", _loadedTextures.Count);

        foreach (var texture in _loadedTextures.ToList())
        {
            texture.Dispose();
        }
        _loadedTextures.Clear();

        // SDL3_image no longer has quit - cleanup is automatic
        _disposed = true;
    }
}