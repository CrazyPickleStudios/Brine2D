using Brine2D.Rendering;
using Brine2D.Threading;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Brine2D.Assets;

/// <summary>
/// Manages asynchronous loading of game assets with caching and progress tracking.
/// </summary>
public interface IAssetLoader
{
    /// <summary>
    /// Loads a texture asynchronously with optional progress reporting.
    /// IAssetLoader handles threading internally - just await this method.
    /// </summary>
    Task<ITexture> LoadTextureAsync(
        string path, 
        TextureScaleMode scaleMode = TextureScaleMode.Linear,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Preloads multiple assets in parallel with overall progress reporting
    /// </summary>
    Task PreloadAssetsAsync(
        IEnumerable<AssetDescriptor> assets,
        IProgress<AssetLoadProgress>? progress = null,
        CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Gets a cached texture if available, otherwise loads it
    /// </summary>
    Task<ITexture> GetOrLoadTextureAsync(string path, CancellationToken cancellationToken = default);
    
    /// <summary>
    /// Unloads all cached assets
    /// </summary>
    void UnloadAll();
}

public class AssetLoader : IAssetLoader, IDisposable
{
    private readonly ILogger<AssetLoader> _logger;
    private readonly ITextureLoader _textureLoader;
    
    // Thread-safe cache
    private readonly ConcurrentDictionary<string, ITexture> _textureCache = new();
    private readonly ConcurrentDictionary<string, Task<ITexture>> _loadingTextures = new();
    
    private bool _disposed;

    public AssetLoader(
        ILogger<AssetLoader> logger,
        ITextureLoader textureLoader)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _textureLoader = textureLoader ?? throw new ArgumentNullException(nameof(textureLoader));
    }
    
    public async Task<ITexture> LoadTextureAsync(
        string path,
        TextureScaleMode scaleMode = TextureScaleMode.Linear,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        _logger.LogDebug("Loading texture asynchronously: {Path}", path);
        
        progress?.Report(0f);

        // TextureLoader handles threading internally
        var texture = await _textureLoader.LoadTextureAsync(path, scaleMode, cancellationToken);
        
        progress?.Report(1f);
        
        _logger.LogInformation("Texture loaded: {Path} ({Width}x{Height})", 
            path, texture.Width, texture.Height);
        
        return texture;
    }

    public async Task<ITexture> GetOrLoadTextureAsync(string path, CancellationToken cancellationToken = default)
    {
        // Fast path: check cache first
        if (_textureCache.TryGetValue(path, out var cachedTexture))
        {
            _logger.LogTrace("Texture cache hit: {Path}", path);
            return cachedTexture;
        }

        // Ensure only one load operation per path
        var loadTask = _loadingTextures.GetOrAdd(path, _ =>
        {
            return LoadAndCacheTextureAsync(path, cancellationToken);
        });

        return await loadTask;
    }

    private async Task<ITexture> LoadAndCacheTextureAsync(string path, CancellationToken cancellationToken)
    {
        try
        {
            var texture = await LoadTextureAsync(path, cancellationToken: cancellationToken);
            _textureCache[path] = texture;
            return texture;
        }
        finally
        {
            _loadingTextures.TryRemove(path, out _);
        }
    }

    public async Task PreloadAssetsAsync(
        IEnumerable<AssetDescriptor> assets,
        IProgress<AssetLoadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var assetList = assets.ToList();
        var totalCount = assetList.Count;
        var loadedCount = 0;
        var failedCount = 0;
        
        _logger.LogInformation("Preloading {Count} assets in parallel", totalCount);
        
        var sw = Stopwatch.StartNew();
        
        // Load assets in parallel with controlled concurrency
        var options = new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        };

        await Parallel.ForEachAsync(assetList, options, async (asset, ct) =>
        {
            try
            {
                switch (asset.Type)
                {
                    case AssetType.Texture:
                        await GetOrLoadTextureAsync(asset.Path, ct);
                        break;
                    
                    // Add more asset types here (audio, fonts, etc.)
                    default:
                        _logger.LogWarning("Unknown asset type: {Type}", asset.Type);
                        break;
                }
                
                Interlocked.Increment(ref loadedCount);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load asset: {Path}", asset.Path);
                Interlocked.Increment(ref failedCount);
            }
            
            // Report progress
            var currentProgress = new AssetLoadProgress
            {
                TotalAssets = totalCount,
                LoadedAssets = loadedCount,
                FailedAssets = failedCount,
                CurrentAsset = asset.Path
            };
            
            progress?.Report(currentProgress);
        });
        
        sw.Stop();
        _logger.LogInformation(
            "Asset preloading complete: {Loaded}/{Total} loaded, {Failed} failed in {ElapsedMs}ms",
            loadedCount, totalCount, failedCount, sw.ElapsedMilliseconds);
    }

    public void UnloadAll()
    {
        _logger.LogInformation("Unloading all cached assets");
        
        foreach (var texture in _textureCache.Values)
        {
            _textureLoader.UnloadTexture(texture);
        }
        
        _textureCache.Clear();
        _loadingTextures.Clear();
    }

    public void Dispose()
    {
        if (_disposed) return;
        
        UnloadAll();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

// Supporting types remain the same
public record AssetDescriptor(AssetType Type, string Path);

public enum AssetType
{
    Texture,
    Audio,
    Font,
    Shader,
    Data
}

public record AssetLoadProgress
{
    public int TotalAssets { get; init; }
    public int LoadedAssets { get; init; }
    public int FailedAssets { get; init; }
    public string CurrentAsset { get; init; } = string.Empty;
    public float ProgressPercent => TotalAssets > 0 ? (float)LoadedAssets / TotalAssets : 0f;
}