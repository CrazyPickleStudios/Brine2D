using Brine2D.Audio;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace Brine2D.Assets;

/// <summary>
/// Scoped <see cref="IAssetLoader"/> wrapper that tracks all assets loaded within a DI scope
/// and automatically releases them when the scope is disposed. Each scene receives its own
/// instance, so assets are cleaned up during scene transitions without manual
/// <c>Release*</c> or <c>Unload</c> calls.
/// </summary>
internal sealed class AssetLoader : IAssetLoader, IDisposable
{
    private readonly AssetCache _cache;
    private readonly ILogger<AssetLoader> _logger;
    private readonly Dictionary<RefCountKey, int> _directRefs = new();
    private readonly HashSet<AssetManifest> _manifests = new();

    // Lock ordering: _lock is always acquired BEFORE AssetCache._stateLock (via
    // _cache.TrackDirectRef / _cache.ReleaseDirectRef). This is safe because AssetCache
    // has no back-reference to AssetLoader, making the reverse acquisition structurally
    // impossible.
    private readonly Lock _lock = new();
    private int _disposed;

    public AssetLoader(AssetCache cache, ILogger<AssetLoader> logger)
    {
        ArgumentNullException.ThrowIfNull(cache);
        ArgumentNullException.ThrowIfNull(logger);
        _cache = cache;
        _logger = logger;
    }

    public async Task<ITexture> GetOrLoadTextureAsync(
        string path,
        TextureScaleMode scaleMode = TextureScaleMode.Linear,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        path = AssetCache.NormalizePath(path);
        return await GetOrLoadCoreAsync(
            RefCountKey.ForTexture(path, scaleMode),
            ct => _cache.GetOrLoadTextureAsync(path, scaleMode, ct),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<ISoundEffect> GetOrLoadSoundAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        path = AssetCache.NormalizePath(path);
        return await GetOrLoadCoreAsync(
            RefCountKey.ForSound(path),
            ct => _cache.GetOrLoadSoundAsync(path, ct),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<IMusic> GetOrLoadMusicAsync(string path, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        path = AssetCache.NormalizePath(path);
        return await GetOrLoadCoreAsync(
            RefCountKey.ForMusic(path),
            ct => _cache.GetOrLoadMusicAsync(path, ct),
            cancellationToken).ConfigureAwait(false);
    }

    public async Task<IFont> GetOrLoadFontAsync(string path, int size, CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        path = AssetCache.NormalizePath(path);
        return await GetOrLoadCoreAsync(
            RefCountKey.ForFont(path, size),
            ct => _cache.GetOrLoadFontAsync(path, size, ct),
            cancellationToken).ConfigureAwait(false);
    }

    public bool ReleaseTexture(string path, TextureScaleMode scaleMode = TextureScaleMode.Linear)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        path = AssetCache.NormalizePath(path);
        return ReleaseCoreRef(RefCountKey.ForTexture(path, scaleMode));
    }

    public bool ReleaseSound(string path)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        path = AssetCache.NormalizePath(path);
        return ReleaseCoreRef(RefCountKey.ForSound(path));
    }

    public bool ReleaseMusic(string path)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        path = AssetCache.NormalizePath(path);
        return ReleaseCoreRef(RefCountKey.ForMusic(path));
    }

    public bool ReleaseFont(string path, int size)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        path = AssetCache.NormalizePath(path);
        return ReleaseCoreRef(RefCountKey.ForFont(path, size));
    }

    public async Task PreloadAsync(
        AssetManifest manifest,
        IProgress<AssetLoadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        bool newlyTracked;
        lock (_lock)
        {
            ThrowIfDisposed();
            newlyTracked = _manifests.Add(manifest);
        }
        try
        {
            await _cache.PreloadAsync(manifest, progress, cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (newlyTracked)
        {
            // Only cancellation rolls back the manifest. AggregateException from partial
            // failures propagates without rollback so the caller can retry unresolved refs
            // or explicitly call Unload to release the partial results.
            RollbackManifest(manifest);
            throw;
        }
    }

    public void Unload(AssetManifest manifest)
    {
        ThrowIfDisposed();
        lock (_lock)
        {
            ThrowIfDisposed();
            _manifests.Remove(manifest);
        }
        _cache.Unload(manifest);
    }

    public void UnloadAll()
    {
        ThrowIfDisposed();
        ReleaseAllTracked();
    }

    public void Dispose()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        ReleaseAllTracked();
    }

    /// <summary>
    /// Tracks the key on both this scope and the shared cache, executes the load, and
    /// rolls back both ref counts if the load faults or the caller's token is cancelled.
    /// When cancellation wins the race against a completing load, the asset may remain in
    /// the cache with no live ref count until the next load of the same key or
    /// <see cref="AssetCache.DisposeAsync"/>.
    /// </summary>
    private async Task<T> GetOrLoadCoreAsync<T>(
        RefCountKey key,
        Func<CancellationToken, Task<T>> loadFromCache,
        CancellationToken cancellationToken)
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            TrackDirectRef(key);
            _cache.TrackDirectRef(key);
        }
        try
        {
            return await loadFromCache(cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            RollbackDirectRef(key);
            throw;
        }
    }

    private bool ReleaseCoreRef(RefCountKey key)
    {
        lock (_lock)
        {
            ThrowIfDisposed();
            if (!UntrackDirectRef(key))
                return false;
        }
        return _cache.ReleaseDirectRef(key);
    }

    private void ReleaseAllTracked()
    {
        List<AssetManifest>? manifests;
        Dictionary<RefCountKey, int>? refs;

        lock (_lock)
        {
            manifests = _manifests.Count > 0 ? [.. _manifests] : null;
            refs = _directRefs.Count > 0 ? new Dictionary<RefCountKey, int>(_directRefs) : null;
            _manifests.Clear();
            _directRefs.Clear();
        }

        if (manifests is not null)
        {
            foreach (var manifest in manifests)
            {
                try { _cache.Unload(manifest); }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to unload manifest {Type} during scope cleanup",
                        manifest.GetType().Name);
                }
            }
        }

        if (refs is not null)
        {
            foreach (var (key, count) in refs)
            {
                for (var i = 0; i < count; i++)
                {
                    try { _cache.ReleaseDirectRef(key); }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Failed to release {Key} during scope cleanup", key);
                    }
                }
            }
        }
    }

    private void RollbackDirectRef(RefCountKey key)
    {
        bool wasTracked;
        lock (_lock) { wasTracked = UntrackDirectRef(key); }
        if (!wasTracked) return;
        try { _cache.ReleaseDirectRef(key); }
        catch (ObjectDisposedException) { }
    }

    private void RollbackManifest(AssetManifest manifest)
    {
        lock (_lock) { _manifests.Remove(manifest); }
        try { _cache.Unload(manifest); }
        catch (ObjectDisposedException) { }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);
    }

    private void TrackDirectRef(RefCountKey key)
    {
        _directRefs.TryGetValue(key, out var count);
        _directRefs[key] = count + 1;
    }

    private bool UntrackDirectRef(RefCountKey key)
    {
        if (!_directRefs.TryGetValue(key, out var count))
            return false;

        if (--count <= 0)
            _directRefs.Remove(key);
        else
            _directRefs[key] = count;

        return true;
    }
}