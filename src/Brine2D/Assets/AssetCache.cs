using Brine2D.Audio;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;
using System.Buffers;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace Brine2D.Assets;

/// <summary>
/// Internal singleton that owns the asset caches, ref counts and native resource lifecycle.
/// All public API goes through <see cref="AssetLoader"/> (the scoped <see cref="IAssetLoader"/> wrapper).
/// </summary>
internal sealed class AssetCache : IAsyncDisposable
{
    private sealed class TypedCache<TKey, T>(Action<T> unload, Func<TKey, RefCountKey> toRefKey) where TKey : notnull
    {
        public readonly Dictionary<TKey, T> Loaded = new();
        public readonly Dictionary<TKey, (long Id, TaskCompletionSource<T> Tcs)> Inflight = new();

        public void UnloadValue(T value) => unload(value);

        public RefCountKey ToRefKey(TKey key) => toRefKey(key);

        public bool TryRemove(TKey key, [MaybeNullWhen(false)] out T value)
            => Loaded.Remove(key, out value);

        public List<T>? DrainLoaded()
        {
            if (Loaded.Count == 0) return null;
            List<T> values = [.. Loaded.Values];
            Loaded.Clear();
            return values;
        }

        public void CancelAndRemoveInflight(TKey key)
        {
            if (Inflight.Remove(key, out var entry))
                entry.Tcs.TrySetCanceled();
        }

        public void CancelAllInflight()
        {
            foreach (var entry in Inflight.Values)
                entry.Tcs.TrySetCanceled();
            Inflight.Clear();
        }
    }

    private struct PendingUnloads
    {
        public List<ITexture>? Textures;
        public List<ISoundEffect>? Sounds;
        public List<IMusic>? Music;
        public List<IFont>? Fonts;
    }

    private readonly ILogger<AssetCache> _logger;
    private readonly ITextureLoader _textureLoader;
    private readonly ISoundLoader _soundLoader;
    private readonly IMusicLoader _musicLoader;
    private readonly IFontLoader _fontLoader;
    private readonly AssetOptions _options;

    private readonly TypedCache<(string Path, TextureScaleMode Scale), ITexture> _textures;
    private readonly TypedCache<string, ISoundEffect>                            _sounds;
    private readonly TypedCache<string, IMusic>                                  _music;
    private readonly TypedCache<(string Path, int Size), IFont>                  _fonts;

    private readonly HashSet<AssetManifest> _trackedManifests = new();
    private readonly Dictionary<RefCountKey, int> _refCounts = new();
    private readonly Dictionary<RefCountKey, int> _directRefs = new();
    private readonly Dictionary<AssetManifest, CancellationTokenSource> _activePreloads = new();
    private readonly Lock _stateLock = new();
    private readonly CancellationTokenSource _disposeCts = new();

    private const int DisposeTimeoutSeconds = 5;

    private TaskCompletionSource? _drainTcs;
    private long _nextLoadId;
    private int _pendingLoads;
    private int _disposed;

    public AssetCache(
        ILogger<AssetCache> logger,
        ITextureLoader textureLoader,
        ISoundLoader soundLoader,
        IMusicLoader musicLoader,
        IFontLoader fontLoader,
        AssetOptions? options = null)
    {
        ArgumentNullException.ThrowIfNull(logger);
        ArgumentNullException.ThrowIfNull(textureLoader);
        ArgumentNullException.ThrowIfNull(soundLoader);
        ArgumentNullException.ThrowIfNull(musicLoader);
        ArgumentNullException.ThrowIfNull(fontLoader);

        _logger = logger;
        _textureLoader = textureLoader;
        _soundLoader = soundLoader;
        _musicLoader = musicLoader;
        _fontLoader = fontLoader;
        _options = options ?? new AssetOptions();

        _textures = new(textureLoader.UnloadTexture, k => RefCountKey.ForTexture(k.Path, k.Scale));
        _sounds = new(soundLoader.UnloadSound, RefCountKey.ForSound);
        _music = new(musicLoader.UnloadMusic, RefCountKey.ForMusic);
        _fonts = new(fontLoader.UnloadFont, k => RefCountKey.ForFont(k.Path, k.Size));
    }

    // Paths are lowercased for case-insensitive caching. On case-sensitive file systems,
    // asset files must use lowercase names.
    internal static string NormalizePath(string path)
    {
        if (path.Length == 0)
            throw new ArgumentException("Asset path is empty.", nameof(path));

        foreach (var c in path.AsSpan())
        {
            if (char.IsControl(c))
                throw new ArgumentException(
                    $"Asset path contains control character U+{(int)c:X4}.", nameof(path));
        }

        if (!path.Contains('\\')
            && path[0] != '/'
            && path[^1] != '/'
            && !path.Contains("//", StringComparison.Ordinal)
            && !path.Contains("./", StringComparison.Ordinal)
            && !path.EndsWith('.'))
        {
            return LowerInvariantIfNeeded(path);
        }

        var normalized = path.Contains('\\') ? path.Replace('\\', '/') : path;
        normalized = normalized.TrimStart('/');
        normalized = normalized.TrimEnd('/');

        if (normalized.Length == 0)
            throw new ArgumentException($"Asset path '{path}' resolves to an empty path.", nameof(path));

        if (!normalized.Contains("//", StringComparison.Ordinal)
            && !normalized.Contains("./", StringComparison.Ordinal)
            && !normalized.EndsWith('.'))
        {
            return LowerInvariantIfNeeded(normalized);
        }

        var segments = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
        var result = ArrayPool<string>.Shared.Rent(segments.Length);
        var resultCount = 0;

        try
        {
            foreach (var seg in segments)
            {
                if (seg == ".")
                    continue;

                if (seg is ".." || seg.EndsWith('.'))
                    throw new ArgumentException(
                        $"Asset path '{path}' contains an invalid segment '{seg}'.", nameof(path));

                result[resultCount++] = seg;
            }

            if (resultCount == 0)
                throw new ArgumentException($"Asset path '{path}' resolves to an empty path.", nameof(path));

            return LowerInvariantIfNeeded(string.Join('/', result.AsSpan(0, resultCount)));
        }
        finally
        {
            ArrayPool<string>.Shared.Return(result, clearArray: true);
        }
    }

    private static string LowerInvariantIfNeeded(string value)
    {
        foreach (var c in value.AsSpan())
        {
            if (char.IsUpper(c))
                return value.ToLowerInvariant();
        }

        return value;
    }

    private void IncrementDirectRef(RefCountKey key)
    {
        _directRefs.TryGetValue(key, out var directCount);
        _directRefs[key] = directCount + 1;
        IncrementRefCount(key);
    }

    private void IncrementRefCount(RefCountKey key)
    {
        _refCounts.TryGetValue(key, out var count);
        _refCounts[key] = count + 1;
    }

    internal void TrackDirectRef(RefCountKey key)
    {
        ThrowIfDisposed();
        lock (_stateLock) { IncrementDirectRef(key); }
    }

    internal Task<ITexture> GetOrLoadTextureAsync(
        string path,
        TextureScaleMode scaleMode = TextureScaleMode.Linear,
        CancellationToken cancellationToken = default)
    {
        return GetOrLoadAsync(
            (path, scaleMode), _textures,
            ct => _textureLoader.LoadTextureAsync(path, scaleMode, ct),
            cancellationToken);
    }

    internal Task<ISoundEffect> GetOrLoadSoundAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        return GetOrLoadAsync(
            path, _sounds,
            ct => _soundLoader.LoadSoundAsync(path, ct),
            cancellationToken);
    }

    internal Task<IMusic> GetOrLoadMusicAsync(
        string path,
        CancellationToken cancellationToken = default)
    {
        return GetOrLoadAsync(
            path, _music,
            ct => _musicLoader.LoadMusicAsync(path, ct),
            cancellationToken);
    }

    internal Task<IFont> GetOrLoadFontAsync(
        string path,
        int size,
        CancellationToken cancellationToken = default)
    {
        return GetOrLoadAsync(
            (path, size), _fonts,
            ct => _fontLoader.LoadFontAsync(path, size, ct),
            cancellationToken);
    }

    /// <summary>
    /// Returns a cached asset, joins an in-flight load, or starts a new one.
    /// The lock is held only for dictionary lookups/writes — never across I/O.
    /// </summary>
    private Task<T> GetOrLoadAsync<TKey, T>(
        TKey key,
        TypedCache<TKey, T> cache,
        Func<CancellationToken, Task<T>> load,
        CancellationToken callerCt) where TKey : notnull
    {
        lock (_stateLock)
        {
            if (cache.Loaded.TryGetValue(key, out var cached))
                return Task.FromResult(cached);

            if (cache.Inflight.TryGetValue(key, out var existing))
                return callerCt.CanBeCanceled ? existing.Tcs.Task.WaitAsync(callerCt) : existing.Tcs.Task;

            callerCt.ThrowIfCancellationRequested();

            var loadId = ++_nextLoadId;
            var tcs = new TaskCompletionSource<T>(TaskCreationOptions.RunContinuationsAsynchronously);
            cache.Inflight[key] = (loadId, tcs);
            Interlocked.Increment(ref _pendingLoads);
            _ = ExecuteLoadAsync(key, cache, load, loadId, tcs);

            return callerCt.CanBeCanceled ? tcs.Task.WaitAsync(callerCt) : tcs.Task;
        }
    }

    /// <summary>
    /// Runs the I/O load on the thread pool and writes the result to the cache under
    /// the lock. If the inflight entry was removed (by <see cref="Unload"/> or
    /// <see cref="UnloadAll"/>) while the load was in progress, the native resource
    /// is freed immediately outside the lock and the <see cref="TaskCompletionSource{T}"/>
    /// is cancelled.
    /// </summary>
    private async Task ExecuteLoadAsync<TKey, T>(
        TKey key,
        TypedCache<TKey, T> cache,
        Func<CancellationToken, Task<T>> load,
        long loadId,
        TaskCompletionSource<T> tcs) where TKey : notnull
    {
        // Yield so the caller's lock scope completes before I/O begins.
        // Without this, a synchronously-completing load would re-enter _stateLock.
        await Task.Yield();
        try
        {
            var value = await load(_disposeCts.Token).ConfigureAwait(false);

            bool committed;
            lock (_stateLock)
            {
                if (cache.Inflight.TryGetValue(key, out var entry) && entry.Id == loadId)
                {
                    cache.Inflight.Remove(key);

                    // If all references were released while this load was in flight
                    // (e.g., scope disposed before the inflight entry was created),
                    // the entry is orphaned — skip caching and free immediately.
                    if (_refCounts.ContainsKey(cache.ToRefKey(key)))
                    {
                        cache.Loaded[key] = value;
                        tcs.SetResult(value);
                        committed = true;
                    }
                    else
                    {
                        committed = false;
                    }
                }
                else
                {
                    committed = false;
                }
            }

            if (!committed)
            {
                SafeUnload(value, cache.UnloadValue);
                tcs.TrySetCanceled();
            }
        }
        catch (OperationCanceledException)
        {
            lock (_stateLock)
            {
                if (cache.Inflight.TryGetValue(key, out var entry) && entry.Id == loadId)
                    cache.Inflight.Remove(key);
            }

            tcs.TrySetCanceled();
        }
        catch (Exception ex)
        {
            lock (_stateLock)
            {
                if (cache.Inflight.TryGetValue(key, out var entry) && entry.Id == loadId)
                    cache.Inflight.Remove(key);
            }

            tcs.TrySetException(ex);
        }
        finally
        {
            if (Interlocked.Decrement(ref _pendingLoads) == 0)
                Volatile.Read(ref _drainTcs)?.TrySetResult();
        }
    }

    internal bool TryGetTexture(string path, TextureScaleMode scaleMode, [NotNullWhen(true)] out ITexture? texture)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        path = NormalizePath(path);
        lock (_stateLock) { return _textures.Loaded.TryGetValue((path, scaleMode), out texture!); }
    }

    internal bool TryGetTexture(string path, [NotNullWhen(true)] out ITexture? texture)
        => TryGetTexture(path, TextureScaleMode.Linear, out texture);

    internal bool TryGetSound(string path, [NotNullWhen(true)] out ISoundEffect? sound)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        path = NormalizePath(path);
        lock (_stateLock) { return _sounds.Loaded.TryGetValue(path, out sound!); }
    }

    internal bool TryGetMusic(string path, [NotNullWhen(true)] out IMusic? music)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        path = NormalizePath(path);
        lock (_stateLock) { return _music.Loaded.TryGetValue(path, out music!); }
    }

    internal bool TryGetFont(string path, int size, [NotNullWhen(true)] out IFont? font)
    {
        ThrowIfDisposed();
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(size);
        path = NormalizePath(path);
        lock (_stateLock) { return _fonts.Loaded.TryGetValue((path, size), out font!); }
    }

    private bool DecrementDirectRef(RefCountKey key, ref PendingUnloads pending)
    {
        if (!_directRefs.TryGetValue(key, out var directCount))
            return false;

        if (--directCount <= 0)
            _directRefs.Remove(key);
        else
            _directRefs[key] = directCount;

        if (!_refCounts.TryGetValue(key, out var count))
        {
            Debug.Fail($"Ref-count key '{key}' missing during Release — bookkeeping inconsistency.");
            return false;
        }

        if (--count <= 0)
        {
            _refCounts.Remove(key);
            CollectUnloadByRefKey(key, ref pending);
            return true;
        }

        _refCounts[key] = count;
        return false;
    }

    internal bool ReleaseDirectRef(RefCountKey key)
    {
        ThrowIfDisposed();
        PendingUnloads pending = default;
        bool freed;
        lock (_stateLock) { freed = DecrementDirectRef(key, ref pending); }
        ExecutePendingUnloads(ref pending);
        return freed;
    }

    internal async Task PreloadAsync(
        AssetManifest manifest,
        IProgress<AssetLoadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(manifest);

        var refs = manifest.GetAll();
        bool alreadyTracked;
        var preloadCts = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken, _disposeCts.Token);

        lock (_stateLock)
        {
            if (_trackedManifests.Add(manifest))
            {
                alreadyTracked = false;
                foreach (var key in manifest.GetUniqueKeys())
                    IncrementRefCount(key);
            }
            else
            {
                alreadyTracked = true;
            }

            if (_activePreloads.TryGetValue(manifest, out var previous))
                previous.Cancel();

            _activePreloads[manifest] = preloadCts;
        }

        try
        {
            if (alreadyTracked)
            {
                var allResolved = true;
                for (var i = 0; i < refs.Count; i++)
                {
                    if (!refs[i].IsLoaded) { allResolved = false; break; }
                }

                if (allResolved)
                    return;

                _logger.LogInformation(
                    "Retrying PreloadAsync on manifest ({Type}) with unresolved assets",
                    manifest.GetType().Name);
            }

            var items = new (string Path, Func<CancellationToken, Task> LoadFunc)[refs.Count];
            for (var i = 0; i < refs.Count; i++)
            {
                var r = refs[i];
                items[i] = (r.Path, ct => r.LoadAsync(this, _logger, ct));
            }

            await RunParallelPreload(items, progress, preloadCts.Token).ConfigureAwait(false);
        }
        finally
        {
            lock (_stateLock)
            {
                if (_activePreloads.TryGetValue(manifest, out var current) && current == preloadCts)
                    _activePreloads.Remove(manifest);
            }

            preloadCts.Dispose();
        }
    }

    private async Task RunParallelPreload(
        (string Path, Func<CancellationToken, Task> LoadFunc)[] items,
        IProgress<AssetLoadProgress>? progress,
        CancellationToken cancellationToken)
    {
        long packedCounters = 0;
        var errors = new ConcurrentQueue<Exception>();
        var sw = Stopwatch.StartNew();

        _logger.LogInformation("Preloading {Count} assets", items.Length);

        await Parallel.ForEachAsync(items, new ParallelOptions
        {
            MaxDegreeOfParallelism = _options.EffectiveParallelism,
            CancellationToken = cancellationToken
        }, async (item, ct) =>
        {
            var itemFailed = false;
            try
            {
                await item.LoadFunc(ct).ConfigureAwait(false);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to preload asset: {Path}", item.Path);
                itemFailed = true;
                errors.Enqueue(ex);
            }

            // packedCounters layout: [63..32] = total attempted, [31..0] = total failed
            var delta = itemFailed ? (1L << 32) | 1L : 1L << 32;
            var snapshot = Interlocked.Add(ref packedCounters, delta);
            var currentAttempted = (int)(snapshot >>> 32);
            var currentFailed = (int)(snapshot & 0xFFFF_FFFFL);

            progress?.Report(new AssetLoadProgress
            {
                TotalAssets = items.Length,
                SucceededAssets = currentAttempted - currentFailed,
                FailedAssets = currentFailed,
                LastCompletedAsset = item.Path
            });
        }).ConfigureAwait(false);

        var final = Volatile.Read(ref packedCounters);
        var totalAttempted = (int)(final >>> 32);
        var totalFailed = (int)(final & 0xFFFF_FFFFL);

        _logger.LogInformation(
            "Preload complete: {Loaded}/{Total} loaded, {Failed} failed in {Ms}ms",
            totalAttempted - totalFailed, items.Length, totalFailed, sw.ElapsedMilliseconds);

        if (!errors.IsEmpty)
        {
            throw new AggregateException(
                $"{errors.Count} asset(s) failed to load.", errors);
        }
    }

    internal void Unload(AssetManifest manifest)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(manifest);

        var refs = manifest.GetAll();
        var uniqueKeys = manifest.GetUniqueKeys();

        PendingUnloads pending = default;
        lock (_stateLock)
        {
            if (!_trackedManifests.Remove(manifest))
                return;

            if (_activePreloads.Remove(manifest, out var preloadCts))
                preloadCts.Cancel(); // PreloadAsync's finally block owns disposal

            foreach (var key in uniqueKeys)
            {
                if (!_refCounts.TryGetValue(key, out var count))
                {
                    Debug.Fail($"Ref-count key '{key}' missing during Unload — bookkeeping inconsistency.");
                    continue;
                }

                count--;
                if (count <= 0)
                {
                    _refCounts.Remove(key);

                    for (var i = 0; i < refs.Count; i++)
                    {
                        if (refs[i].RefKey == key && !refs[i].IsLoaded)
                        {
                            _logger.LogWarning(
                                "Unloading manifest ref '{Path}' that was never resolved; " +
                                "in-flight loads for this key will be cancelled",
                                refs[i].Path);
                        }
                    }

                    CollectUnloadByRefKey(key, ref pending);
                }
                else
                {
                    _refCounts[key] = count;
                }
            }
        }

        for (var i = 0; i < refs.Count; i++)
            refs[i].Reset();

        ExecutePendingUnloads(ref pending);
    }

    private void CollectUnloadByRefKey(RefCountKey key, ref PendingUnloads pending)
    {
        switch (key.Kind)
        {
            case AssetType.Texture:
                var texKey = (key.Path, (TextureScaleMode)key.Discriminator);
                _textures.CancelAndRemoveInflight(texKey);
                if (_textures.TryRemove(texKey, out var tex))
                    (pending.Textures ??= []).Add(tex);
                break;
            case AssetType.Sound:
                _sounds.CancelAndRemoveInflight(key.Path);
                if (_sounds.TryRemove(key.Path, out var snd))
                    (pending.Sounds ??= []).Add(snd);
                break;
            case AssetType.Music:
                _music.CancelAndRemoveInflight(key.Path);
                if (_music.TryRemove(key.Path, out var mus))
                    (pending.Music ??= []).Add(mus);
                break;
            case AssetType.Font:
                var fontKey = (key.Path, key.Discriminator);
                _fonts.CancelAndRemoveInflight(fontKey);
                if (_fonts.TryRemove(fontKey, out var font))
                    (pending.Fonts ??= []).Add(font);
                break;
            default:
                Debug.Fail($"Unhandled AssetType '{key.Kind}' in CollectUnloadByRefKey.");
                break;
        }
    }

    internal void UnloadAll()
    {
        ThrowIfDisposed();
        PendingUnloads pending = default;
        List<AssetManifest>? manifests;
        lock (_stateLock)
        {
            _textures.CancelAllInflight();
            _sounds.CancelAllInflight();
            _music.CancelAllInflight();
            _fonts.CancelAllInflight();

            pending.Textures = _textures.DrainLoaded();
            pending.Sounds = _sounds.DrainLoaded();
            pending.Music = _music.DrainLoaded();
            pending.Fonts = _fonts.DrainLoaded();

            manifests = CancelPreloadsAndClearState();
        }

        ResetManifestRefs(manifests);
        ExecutePendingUnloads(ref pending);
    }

    /// <summary>
    /// Cancels active preloads, snapshots tracked manifests, and clears all ref-count
    /// bookkeeping. Must be called under <see cref="_stateLock"/>. Returns the manifests
    /// whose <see cref="IAssetRef"/>s need to be reset outside the lock.
    /// </summary>
    private List<AssetManifest>? CancelPreloadsAndClearState()
    {
        foreach (var cts in _activePreloads.Values)
            cts.Cancel(); // PreloadAsync's finally block owns disposal
        _activePreloads.Clear();

        List<AssetManifest>? manifests = _trackedManifests.Count > 0 ? [.. _trackedManifests] : null;
        _trackedManifests.Clear();
        _directRefs.Clear();
        _refCounts.Clear();

        return manifests;
    }

    private static void ResetManifestRefs(List<AssetManifest>? manifests)
    {
        if (manifests is null) return;
        foreach (var manifest in manifests)
        {
            foreach (var assetRef in manifest.GetAll())
                assetRef.Reset();
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1) return;

        _disposeCts.Cancel();

        PendingUnloads pending = default;
        List<AssetManifest>? manifests;
        lock (_stateLock)
        {
            _textures.CancelAllInflight();
            _sounds.CancelAllInflight();
            _music.CancelAllInflight();
            _fonts.CancelAllInflight();

            pending.Textures = _textures.DrainLoaded();
            pending.Sounds = _sounds.DrainLoaded();
            pending.Music = _music.DrainLoaded();
            pending.Fonts = _fonts.DrainLoaded();

            manifests = CancelPreloadsAndClearState();
        }

        ResetManifestRefs(manifests);
        ExecutePendingUnloads(ref pending);

        if (Volatile.Read(ref _pendingLoads) > 0)
        {
            var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Volatile.Write(ref _drainTcs, tcs);

            if (Volatile.Read(ref _pendingLoads) == 0)
                tcs.TrySetResult();

            try
            {
                await tcs.Task.WaitAsync(TimeSpan.FromSeconds(DisposeTimeoutSeconds)).ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                _logger.LogWarning(
                    "DisposeAsync timed out after {Seconds}s with {Pending} load(s) still in flight",
                    DisposeTimeoutSeconds, Volatile.Read(ref _pendingLoads));
            }
        }

        _disposeCts.Dispose();
    }

    private void ExecutePendingUnloads(ref PendingUnloads pending)
    {
        SafeUnloadAll(pending.Textures, _textureLoader.UnloadTexture);
        SafeUnloadAll(pending.Sounds, _soundLoader.UnloadSound);
        SafeUnloadAll(pending.Music, _musicLoader.UnloadMusic);
        SafeUnloadAll(pending.Fonts, _fontLoader.UnloadFont);
    }

    private void SafeUnloadAll<T>(List<T>? values, Action<T> unload)
    {
        if (values is null) return;

        foreach (var value in values)
            SafeUnload(value, unload);
    }

    private void SafeUnload<T>(T value, Action<T> unload)
    {
        try
        {
            unload(value);
        }
        catch (Exception ex)
        {
            try { _logger.LogError(ex, "Failed to unload {Type} resource during cleanup", typeof(T).Name); }
            catch { /* Never skip remaining unloads because logging failed */ }
        }
    }

    private void ThrowIfDisposed()
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) == 1, this);
    }

    internal void LogRefCounts()
    {
        lock (_stateLock)
        {
            if (_refCounts.Count == 0 && _directRefs.Count == 0)
            {
                _logger.LogDebug("Asset ref counts: (empty)");
                return;
            }

            foreach (var (key, count) in _refCounts)
            {
                _directRefs.TryGetValue(key, out var direct);
                _logger.LogDebug(
                    "RefCount {Key}: total={Total}, direct={Direct}, manifest={Manifest}",
                    key, count, direct, count - direct);
            }
        }
    }
}