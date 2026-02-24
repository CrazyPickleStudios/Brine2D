using Brine2D.Audio;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace Brine2D.Assets;

/// <summary>
/// Unified async asset loading with caching.
/// All asset types (textures, sounds, music, fonts) go through one service.
/// Inject <see cref="IAssetLoader"/> into your scene or system constructor.
/// No content pipeline, no build step: drag files into your assets folder and load them.
/// </summary>
public interface IAssetLoader
{
    // ---- Textures ----

    /// <summary>Loads a texture. Does not cache. Use <see cref="GetOrLoadTextureAsync"/> for scenes.</summary>
    Task<ITexture> LoadTextureAsync(
        string path,
        TextureScaleMode scaleMode = TextureScaleMode.Linear,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a cached texture, loading it on first request.</summary>
    Task<ITexture> GetOrLoadTextureAsync(string path, CancellationToken cancellationToken = default);

    // ---- Audio ----

    /// <summary>Returns a cached sound effect, loading it on first request.</summary>
    Task<ISoundEffect> GetOrLoadSoundAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Returns cached music, loading it on first request.</summary>
    Task<IMusic> GetOrLoadMusicAsync(string path, CancellationToken cancellationToken = default);

    // ---- Fonts ----

    /// <summary>
    /// Returns a cached font, loading it on first request.
    /// The (path, size) pair is the cache key; the same file at different sizes is two separate entries.
    /// </summary>
    Task<Font> GetOrLoadFontAsync(string path, int size, CancellationToken cancellationToken = default);

    // ---- Manifest preloading ----

    /// <summary>
    /// Resolves all <see cref="AssetRef{T}"/> fields declared on <paramref name="manifest"/>
    /// in parallel, reporting progress as each asset completes.
    /// Call this in <c>OnLoadAsync</c>. Assets are safe to access from <c>OnEnter</c> onwards.
    /// </summary>
    Task PreloadAsync(
        AssetManifest manifest,
        IProgress<AssetLoadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>Preloads a list of descriptors in parallel (legacy / JSON manifest path).</summary>
    Task PreloadAssetsAsync(
        IEnumerable<AssetDescriptor> assets,
        IProgress<AssetLoadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>Unloads all cached assets and frees GPU/audio resources.</summary>
    void UnloadAll();
}

/// <inheritdoc cref="IAssetLoader"/>
public class AssetLoader : IAssetLoader, IDisposable
{
    private readonly ILogger<AssetLoader> _logger;
    private readonly ITextureLoader _textureLoader;
    private readonly IAudioService _audioService;
    private readonly IFontLoader _fontLoader;

    // Per-type thread-safe caches
    private readonly ConcurrentDictionary<string, ITexture>         _textureCache = new();
    private readonly ConcurrentDictionary<string, Task<ITexture>>   _loadingTextures = new();
    private readonly ConcurrentDictionary<string, ISoundEffect>      _soundCache = new();
    private readonly ConcurrentDictionary<string, Task<ISoundEffect>> _loadingSounds = new();
    private readonly ConcurrentDictionary<string, IMusic>            _musicCache = new();
    private readonly ConcurrentDictionary<string, Task<IMusic>>      _loadingMusic = new();
    private readonly ConcurrentDictionary<string, Font>              _fontCache = new();
    private readonly ConcurrentDictionary<string, Task<Font>>        _loadingFonts = new();

    private bool _disposed;

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

    // ---- Textures ----

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
        return _loadingTextures.GetOrAdd(path, _ => LoadAndCache(
            path,
            ct => LoadTextureAsync(path, cancellationToken: ct),
            _textureCache, _loadingTextures,
            cancellationToken));
    }

    // ---- Audio ----

    public Task<ISoundEffect> GetOrLoadSoundAsync(string path, CancellationToken cancellationToken = default)
    {
        if (_soundCache.TryGetValue(path, out var cached)) return Task.FromResult(cached);
        return _loadingSounds.GetOrAdd(path, _ => LoadAndCache(
            path,
            ct => _audioService.LoadSoundAsync(path, ct),
            _soundCache, _loadingSounds,
            cancellationToken));
    }

    public Task<IMusic> GetOrLoadMusicAsync(string path, CancellationToken cancellationToken = default)
    {
        if (_musicCache.TryGetValue(path, out var cached)) return Task.FromResult(cached);
        return _loadingMusic.GetOrAdd(path, _ => LoadAndCache(
            path,
            ct => _audioService.LoadMusicAsync(path, ct),
            _musicCache, _loadingMusic,
            cancellationToken));
    }

    // ---- Fonts ----

    public Task<Font> GetOrLoadFontAsync(string path, int size, CancellationToken cancellationToken = default)
    {
        var key = $"{path}:{size}";
        if (_fontCache.TryGetValue(key, out var cached)) return Task.FromResult(cached);
        return _loadingFonts.GetOrAdd(key, _ => LoadAndCache(
            key,
            ct => _fontLoader.LoadFontAsync(path, size, ct),
            _fontCache, _loadingFonts,
            cancellationToken));
    }

    // ---- Manifest preloading ----

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
        var list = assets.ToList();
        await RunParallelPreload(
            list.Count,
            list.Select(a => (a.Path, LoadFunc: (Func<CancellationToken, Task>)(ct => LoadDescriptorAsync(a, ct)))),
            progress,
            cancellationToken);
    }

    // ---- Shared parallel loader ----

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
        AssetType.Texture  => GetOrLoadTextureAsync(asset.Path, ct),
        AssetType.Audio    => GetOrLoadSoundAsync(asset.Path, ct),
        AssetType.Music    => GetOrLoadMusicAsync(asset.Path, ct),
        AssetType.Font     => Task.CompletedTask, // fonts need a size; use AssetManifest or GetOrLoadFontAsync directly
        _                  => Task.CompletedTask
    };

    // ---- Generic cache helper ----

    private static async Task<T> LoadAndCache<T>(
        string key,
        Func<CancellationToken, Task<T>> load,
        ConcurrentDictionary<string, T> cache,
        ConcurrentDictionary<string, Task<T>> inflight,
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
            inflight.TryRemove(key, out _);
        }
    }

    // ---- Unload ----

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
        if (_disposed) return;
        UnloadAll();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

// ---- Supporting types ----

public record AssetDescriptor(AssetType Type, string Path);

public enum AssetType { Texture, Audio, Music, Font, Shader, Data }

public record AssetLoadProgress
{
    public int TotalAssets  { get; init; }
    public int LoadedAssets { get; init; }
    public int FailedAssets { get; init; }
    public string CurrentAsset { get; init; } = string.Empty;
    public float ProgressPercent => TotalAssets > 0 ? (float)LoadedAssets / TotalAssets : 0f;
}