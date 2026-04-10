using Brine2D.Rendering;
using Brine2D.Threading;
using Microsoft.Extensions.Logging;
using System.Runtime.InteropServices;

namespace Brine2D.Rendering;

public class SDL3TextureLoader : ITextureLoader
{
    private readonly ILogger<SDL3TextureLoader> _logger;
    private readonly ITextureContext _textureContext;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly List<ITexture> _loadedTextures = new();
    private readonly Lock _texturesLock = new();
    private int _disposed;

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
        ObjectDisposedException.ThrowIf(_disposed == 1, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Texture file not found: {path}");

        var fileData = await Task.Run(
            () => File.ReadAllBytes(path), 
            cancellationToken);

        var surfaceInfo = await Task.Run(
            () => DecodeImageData(fileData, path),
            cancellationToken);

        bool surfaceConsumed = false;
        try
        {
            ITexture texture = null!;
            _mainThreadDispatcher.RunOnMainThread(() =>
            {
                surfaceConsumed = true;
                texture = CreateTextureFromSurface(surfaceInfo, scaleMode, path);
            }, waitForCompletion: true);

            return texture;
        }
        catch
        {
            if (!surfaceConsumed)
                SDL3.SDL.DestroySurface(surfaceInfo.Surface);
            throw;
        }
    }

    public ITexture LoadTexture(string path, TextureScaleMode scaleMode = TextureScaleMode.Linear)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
            throw new FileNotFoundException($"Texture file not found: {path}");

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

            ITexture texture = null!;
            _mainThreadDispatcher.RunOnMainThread(() =>
            {
                texture = _textureContext.CreateTextureFromSurface(surface, width, height, scaleMode);

                lock (_texturesLock)
                {
                    _loadedTextures.Add(texture);
                }
            }, waitForCompletion: true);

            _logger.LogDebug("Texture loaded: {Path} ({Width}x{Height})", path, width, height);

            return texture;
        }
        finally
        {
            SDL3.SDL.DestroySurface(surface);
        }
    }

    public ITexture CreateTexture(int width, int height, TextureScaleMode scaleMode = TextureScaleMode.Linear)
    {
        ObjectDisposedException.ThrowIf(_disposed == 1, this);

        if (width <= 0 || height <= 0)
            throw new ArgumentException("Width and height must be positive");

        ITexture texture = null!;
        
        _mainThreadDispatcher.RunOnMainThread(() =>
        {
            texture = _textureContext.CreateBlankTexture(width, height, scaleMode);
            lock (_texturesLock)
            {
                _loadedTextures.Add(texture);
            }
        }, waitForCompletion: true);
        
        return texture;
    }

    public void UnloadTexture(ITexture texture)
    {
        if (texture == null) return;

        _textureContext.ReleaseTexture(texture);
        lock (_texturesLock)
        {
            _loadedTextures.Remove(texture);
        }
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        List<ITexture> textures;
        lock (_texturesLock)
        {
            textures = [.._loadedTextures];
            _loadedTextures.Clear();
        }

        _logger.LogInformation("Disposing texture loader and {Count} textures", textures.Count);

        foreach (var texture in textures)
        {
            _textureContext.ReleaseTexture(texture);
        }
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
            
            lock (_texturesLock)
            {
                _loadedTextures.Add(texture);
            }

            _logger.LogDebug("Texture created: {Path} ({Width}x{Height})", 
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

    private struct SurfaceInfo
    {
        public IntPtr Surface;
        public int Width;
        public int Height;
    }
}