using Brine2D.Audio;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Brine2D.Assets;

/// <inheritdoc cref="IAssetLoader"/>
public class AssetLoader : IAssetLoader, IDisposable
{
    private readonly ILogger<AssetLoader> _logger;
    private readonly ITextureLoader _textureLoader;
    private readonly IAudioService _audioService;
    private readonly IFontLoader _fontLoader;

    // Per-type completed-asset caches (keyed by path or "path:size" for fonts)
    private readonly ConcurrentDictionary<string, ITexture>    _textureCache = new();
    private readonly ConcurrentDictionary<string, ISoundEffect> _soundCache   = new();
    private readonly ConcurrentDictionary<string, IMusic>       _musicCache   = new();
    private readonly ConcurrentDictionary<string, Font>         _fontCache    = new();

    // In-flight load deduplication. Lazy<Task<T>> guarantees the load factory is invoked
    // exactly once per key even when multiple concurrent callers race on the same path —
    // ConcurrentDictionary.GetOrAdd does NOT guarantee a single factory invocation.
    private readonly ConcurrentDictionary<string, Lazy<Task<ITexture>>>    _loadingTextures = new();
    private readonly ConcurrentDictionary<string, Lazy<Task<ISoundEffect>>> _loadingSounds   = new();
    private readonly ConcurrentDictionary<string, Lazy<Task<IMusic>>>       _loadingMusic    = new();
    private readonly ConcurrentDictionary<string, Lazy<Task<Font>>>         _loadingFonts    = new();

    // 0 = live, 1 = disposed. Interlocked ensures the check-and-set is atomic so two
    // concurrent Dispose() calls cannot both pass the guard and double-free native resources.
    private int _disposed;

    public AssetLoader(
        ILogger<AssetLoader> logger,
        ITextureLoader textureLoader,
        IAudioService audioService,
        IFontLoader fontLoader)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _textureLoader = textureLoader ?? throw new ArgumentNullException(nameof(textureLoader));
        _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
        _fontLoader = fontLoader ?? throw new ArgumentNullException(nameof(fontLoader));
    }
    
    public async Task<ITexture> LoadTextureAsync(
        string path,
        TextureScaleMode scaleMode = TextureScaleMode.Linear,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path cannot be null or empty", nameof(path));

        progress?.Report(0f);
        var texture = await _textureLoader.LoadTextureAsync(path, scaleMode, cancellationToken);
        progress?.Report(1f);

        _logger.LogDebug("Texture loaded: {Path} ({Width}x{Height})", path, texture.Width, texture.Height);
        return texture;
    }

    public Task<ITexture> GetOrLoadTextureAsync(string path, CancellationToken cancellationToken = default)
    {
        if (_textureCache.TryGetValue(path, out var cached)) return Task.FromResult(cached);
        // The Lazy factory always runs with CancellationToken.None so the underlying load
        // completes and caches regardless of which caller started it. Concurrent callers
        // waiting on the same Lazy are therefore never cancelled by proxy when an unrelated
        // caller cancels its own token.
        var task = _loadingTextures.GetOrAdd(path, _ => new Lazy<Task<ITexture>>(
            () => LoadAndCache(
                path,
                ct => LoadTextureAsync(path, cancellationToken: ct),
                _textureCache, _loadingTextures,
                CancellationToken.None))).Value;
        // Apply the caller's token as a wait-deadline only; it does not cancel the load itself.
        return cancellationToken.CanBeCanceled ? task.WaitAsync(cancellationToken) : task;
    }
    
    public Task<ISoundEffect> GetOrLoadSoundAsync(string path, CancellationToken cancellationToken = default)
    {
        if (_soundCache.TryGetValue(path, out var cached)) return Task.FromResult(cached);
        var task = _loadingSounds.GetOrAdd(path, _ => new Lazy<Task<ISoundEffect>>(
            () => LoadAndCache(
                path,
                ct => _audioService.LoadSoundAsync(path, ct),
                _soundCache, _loadingSounds,
                CancellationToken.None))).Value;
        return cancellationToken.CanBeCanceled ? task.WaitAsync(cancellationToken) : task;
    }

    public Task<IMusic> GetOrLoadMusicAsync(string path, CancellationToken cancellationToken = default)
    {
        if (_musicCache.TryGetValue(path, out var cached)) return Task.FromResult(cached);
        var task = _loadingMusic.GetOrAdd(path, _ => new Lazy<Task<IMusic>>(
            () => LoadAndCache(
                path,
                ct => _audioService.LoadMusicAsync(path, ct),
                _musicCache, _loadingMusic,
                CancellationToken.None))).Value;
        return cancellationToken.CanBeCanceled ? task.WaitAsync(cancellationToken) : task;
    }
    
    public Task<Font> GetOrLoadFontAsync(string path, int size, CancellationToken cancellationToken = default)
    {
        var key = $"{path}:{size}";
        if (_fontCache.TryGetValue(key, out var cached)) return Task.FromResult(cached);
        var task = _loadingFonts.GetOrAdd(key, _ => new Lazy<Task<Font>>(
            () => LoadAndCache(
                key,
                ct => _fontLoader.LoadFontAsync(path, size, ct),
                _fontCache, _loadingFonts,
                CancellationToken.None))).Value;
        return cancellationToken.CanBeCanceled ? task.WaitAsync(cancellationToken) : task;
    }
    
    public async Task PreloadAsync(
        AssetManifest manifest,
        IProgress<AssetLoadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(manifest);

        var refs = manifest.GetAll();
        await RunParallelPreload(
            refs.Count,
            refs.Select(r => (r.Path, LoadFunc: (Func<CancellationToken, Task>)(ct => r.LoadAsync(this, ct)))),
            progress,
            cancellationToken);
    }

    public async Task PreloadAssetsAsync(
        IEnumerable<AssetDescriptor> assets,
        IProgress<AssetLoadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(assets);

        var list = assets.ToList();
        await RunParallelPreload(
            list.Count,
            list.Select(a => (a.Path, LoadFunc: (Func<CancellationToken, Task>)(ct => LoadDescriptorAsync(a, ct)))),
            progress,
            cancellationToken);
    }
    
    private async Task RunParallelPreload(
        int total,
        IEnumerable<(string Path, Func<CancellationToken, Task> LoadFunc)> items,
        IProgress<AssetLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        var loaded = 0;
        var failed = 0;
        var sw = Stopwatch.StartNew();

        _logger.LogInformation("Preloading {Count} assets", total);

        await Parallel.ForEachAsync(items, new ParallelOptions
        {
            MaxDegreeOfParallelism = Environment.ProcessorCount,
            CancellationToken = cancellationToken
        }, async (item, ct) =>
        {
            try
            {
                await item.LoadFunc(ct);
                Interlocked.Increment(ref loaded);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preload asset: {Path}", item.Path);
                Interlocked.Increment(ref failed);
            }

            progress?.Report(new AssetLoadProgress
            {
                TotalAssets = total,
                LoadedAssets = loaded,
                FailedAssets = failed,
                CurrentAsset = item.Path
            });
        });

        _logger.LogInformation(
            "Preload complete: {Loaded}/{Total} loaded, {Failed} failed in {Ms}ms",
            loaded, total, failed, sw.ElapsedMilliseconds);
    }

    private Task LoadDescriptorAsync(AssetDescriptor asset, CancellationToken ct) => asset.Type switch
    {
        AssetType.Texture => GetOrLoadTextureAsync(asset.Path, ct),
        AssetType.Audio   => GetOrLoadSoundAsync(asset.Path, ct),
        AssetType.Music   => GetOrLoadMusicAsync(asset.Path, ct),
        AssetType.Font    => WarnSkipFont(asset.Path),
        _                 => Task.CompletedTask
    };

    /// <summary>
    /// Fonts require a size parameter and cannot be described by path alone.
    /// Logs a warning so the caller is informed rather than silently skipping.
    /// </summary>
    private Task WarnSkipFont(string path)
    {
        _logger.LogWarning(
            "Skipping preload of font '{Path}': fonts require a size parameter. " +
            "Use AssetManifest or call GetOrLoadFontAsync(path, size) directly.", path);
        return Task.CompletedTask;
    }
    
    /// <summary>
    /// Loads an asset, writes it to <paramref name="cache"/>, and removes the in-flight
    /// entry from <paramref name="inflight"/> so future callers hit the completed cache directly.
    /// Always called with <see cref="CancellationToken.None"/>; cancellation is applied
    /// externally per-caller via <see cref="Task.WaitAsync(CancellationToken)"/>.
    /// </summary>
    private static async Task<T> LoadAndCache<T>(
        string key,
        Func<CancellationToken, Task<T>> load,
        ConcurrentDictionary<string, T> cache,
        ConcurrentDictionary<string, Lazy<Task<T>>> inflight,
        CancellationToken ct)
    {
        try
        {
            var value = await load(ct);
            cache[key] = value;
            return value;
        }
        finally
        {
            // Remove the Lazy wrapper once the load is settled (success or failure) so
            // subsequent callers skip the inflight dict and either hit the cache or retry.
            inflight.TryRemove(key, out _);
        }
    }
    
    public void UnloadAll()
    {
        foreach (var t in _textureCache.Values) _textureLoader.UnloadTexture(t);
        foreach (var s in _soundCache.Values)   _audioService.UnloadSound(s);
        foreach (var f in _fontCache.Values)    _fontLoader.UnloadFont(f);
        // Music is typically streamed; unload via audio service if it supports it

        _textureCache.Clear(); _loadingTextures.Clear();
        _soundCache.Clear();   _loadingSounds.Clear();
        _musicCache.Clear();   _loadingMusic.Clear();
        _fontCache.Clear();    _loadingFonts.Clear();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;
        UnloadAll();
        GC.SuppressFinalize(this);
    }
}