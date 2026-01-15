using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Brine2D.Rendering.SDL;

/// <summary>
/// SDL3 texture loader that works with any renderer through ITextureContext abstraction.
/// Modern, renderer-agnostic design following ASP.NET patterns.
/// </summary>
public class SDL3TextureLoader : ITextureLoader
{
    private readonly ILogger<SDL3TextureLoader> _logger;
    private readonly ITextureContext _textureContext;
    private readonly List<ITexture> _loadedTextures = new();
    private bool _disposed;

    public SDL3TextureLoader(
        ILogger<SDL3TextureLoader> logger,
        ITextureContext textureContext)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _textureContext = textureContext ?? throw new ArgumentNullException(nameof(textureContext));
        
        _logger.LogInformation("SDL3 texture loader initialized");
    }

    public async Task<ITexture> LoadTextureAsync(
        string path, 
        TextureScaleMode scaleMode = TextureScaleMode.Linear, 
        CancellationToken cancellationToken = default)
    {
        return await Task.Run(() => LoadTexture(path, scaleMode), cancellationToken);
    }

    public ITexture LoadTexture(string path, TextureScaleMode scaleMode = TextureScaleMode.Linear)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Texture file not found: {path}");

        _logger.LogInformation("Loading texture: {Path} (ScaleMode: {ScaleMode})", path, scaleMode);

        // Load image surface using SDL_image
        var surface = SDL3.Image.Load(path);
        if (surface == IntPtr.Zero)
        {
            var error = SDL3.SDL.GetError();
            _logger.LogError("Failed to load image {Path}: {Error}", path, error);
            throw new InvalidOperationException($"Failed to load image: {error}");
        }

        try
        {
            var surfaceStruct = Marshal.PtrToStructure<SDL3.SDL.Surface>(surface);
            var width = surfaceStruct.Width;
            var height = surfaceStruct.Height;

            if (width <= 0 || height <= 0)
            {
                _logger.LogError("Invalid surface dimensions for {Path}: {Width}x{Height}", path, width, height);
                throw new InvalidOperationException($"Invalid surface dimensions: {width}x{height}");
            }

            // Use the context to create texture - renderer handles implementation details!
            var texture = _textureContext.CreateTextureFromSurface(surface, width, height, scaleMode);
            
            _loadedTextures.Add(texture);
            _logger.LogInformation("Texture loaded: {Path} ({Width}x{Height})", path, width, height);

            return texture;
        }
        finally
        {
            SDL3.SDL.DestroySurface(surface);
        }
    }

    public ITexture CreateTexture(int width, int height, TextureScaleMode scaleMode = TextureScaleMode.Linear)
    {
        if (width <= 0 || height <= 0)
            throw new ArgumentException("Width and height must be positive");

        _logger.LogDebug("Creating blank texture: {Width}x{Height}", width, height);

        var texture = _textureContext.CreateBlankTexture(width, height, scaleMode);
        _loadedTextures.Add(texture);
        
        return texture;
    }

    public void UnloadTexture(ITexture texture)
    {
        if (texture == null)
            return;

        _textureContext.ReleaseTexture(texture);
        _loadedTextures.Remove(texture);
        texture.Dispose();
        
        _logger.LogDebug("Texture unloaded: {Source}", texture.Source);
    }

    public void Dispose()
    {
        if (_disposed) return;

        _logger.LogInformation("Disposing texture loader and {Count} textures", _loadedTextures.Count);

        foreach (var texture in _loadedTextures.ToList())
        {
            _textureContext.ReleaseTexture(texture);
            texture.Dispose();
        }

        _loadedTextures.Clear();
        _disposed = true;
    }
}