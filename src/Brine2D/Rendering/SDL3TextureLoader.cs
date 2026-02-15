using Brine2D.Rendering;
using Brine2D.Threading;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Brine2D.SDL.Rendering;

public class SDL3TextureLoader : ITextureLoader
{
    private readonly ILogger<SDL3TextureLoader> _logger;
    private readonly ITextureContext _textureContext;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly List<ITexture> _loadedTextures = new();
    private bool _disposed;

    public SDL3TextureLoader(
        ILogger<SDL3TextureLoader> logger,
        ITextureContext textureContext,
        IMainThreadDispatcher mainThreadDispatcher)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _textureContext = textureContext ?? throw new ArgumentNullException(nameof(textureContext));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));
    }

    public async Task<ITexture> LoadTextureAsync(
        string path,
        TextureScaleMode scaleMode = TextureScaleMode.Linear,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Texture file not found: {path}");

        // Phase 1: I/O - Read file from disk (background thread)
        var fileData = await Task.Run(
            () => File.ReadAllBytes(path), 
            cancellationToken);

        // Phase 2: CPU - Decode image (background thread)
        var surfaceInfo = await Task.Run(
            () => DecodeImageData(fileData, path),
            cancellationToken);

        // Phase 3: GPU - Create texture (MUST be on main thread)
        ITexture texture = null!;
        _mainThreadDispatcher.RunOnMainThread(() =>
        {
            texture = CreateTextureFromSurface(surfaceInfo, scaleMode, path);
        }, waitForCompletion: true);

        return texture;
    }

    public ITexture LoadTexture(string path, TextureScaleMode scaleMode = TextureScaleMode.Linear)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        if (!File.Exists(path))
            throw new FileNotFoundException($"Texture file not found: {path}");

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

            // Use the context to create texture (must be on main thread)
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

        ITexture texture = null!;
        
        _mainThreadDispatcher.RunOnMainThread(() =>
        {
            texture = _textureContext.CreateBlankTexture(width, height, scaleMode);
            _loadedTextures.Add(texture);
        }, waitForCompletion: true);
        
        return texture;
    }

    public void UnloadTexture(ITexture texture)
    {
        if (texture == null) return;

        _textureContext.ReleaseTexture(texture);
        _loadedTextures.Remove(texture);
        texture.Dispose();
    }

    private ITexture CreateTextureFromSurface(SurfaceInfo surfaceInfo, TextureScaleMode scaleMode, string path)
    {
        try
        {
            var texture = _textureContext.CreateTextureFromSurface(
                surfaceInfo.Surface,
                surfaceInfo.Width,
                surfaceInfo.Height,
                scaleMode);
            
            _loadedTextures.Add(texture);
            _logger.LogInformation("Texture created: {Path} ({Width}x{Height})", 
                path, texture.Width, texture.Height);
            
            return texture;
        }
        finally
        {
            SDL3.SDL.DestroySurface(surfaceInfo.Surface);
        }
    }

    private SurfaceInfo DecodeImageData(byte[] fileData, string path)
    {
        var handle = GCHandle.Alloc(fileData, GCHandleType.Pinned);
        
        try
        {
            var dataPtr = handle.AddrOfPinnedObject();
            var rwOps = SDL3.SDL.IOFromConstMem(dataPtr, (nuint)fileData.Length);
            
            if (rwOps == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to create IO: {SDL3.SDL.GetError()}");

            var surface = SDL3.Image.LoadIO(rwOps, true);
            
            if (surface == IntPtr.Zero)
                throw new InvalidOperationException($"Failed to decode {path}: {SDL3.SDL.GetError()}");

            var surfaceStruct = Marshal.PtrToStructure<SDL3.SDL.Surface>(surface);
            
            return new SurfaceInfo
            {
                Surface = surface,
                Width = surfaceStruct.Width,
                Height = surfaceStruct.Height
            };
        }
        finally
        {
            handle.Free();
        }
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
        GC.SuppressFinalize(this);
    }

    private struct SurfaceInfo
    {
        public IntPtr Surface;
        public int Width;
        public int Height;
    }
}