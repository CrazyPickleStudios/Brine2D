using System.Collections.Concurrent;

namespace Brine2D.Core.Content;

/// <summary>
///     Manages loading, caching, and unloading of content assets.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description>Separates cache entries by (asset type, normalized key).</description></item>
///         <item><description>Normalizes paths to ensure stable cache keys (Windows is case-insensitive).</description></item>
///         <item><description>Supports synchronous and asynchronous loading paths.</description></item>
///         <item><description>De-duplicates concurrent async loads via an in-flight map so callers await the same task.</description></item>
///         <item><description>Disposes cached assets that implement <see cref="IDisposable" /> when removed.</description></item>
///     </list>
///     <para><b>Thread safety:</b> This type is not thread-safe. Coordinate access if used from multiple threads.</para>
/// </remarks>
public sealed class ContentManager : IContentManager, IDisposable
{
    // Cache of fully loaded assets by (Type, normalized key).
    private readonly Dictionary<(Type type, string key), object> _cache = new();

    // Tracks currently in-flight async loads; ensures only one load per (Type, key).
    private readonly ConcurrentDictionary<(Type type, string key), Task<object>> _inflight = new();

    // Registered asset loaders (searched from last to first for override precedence).
    private readonly List<IAssetLoader> _loaders = new();

    // Registered file providers used by loaders to resolve and open streams.
    private readonly List<IContentFileProvider> _providers = new();

    /// <summary>
    ///     Registers a file provider used to resolve and open content files.
    /// </summary>
    /// <param name="provider">Provider instance.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="provider" /> is null.</exception>
    public void AddFileProvider(IContentFileProvider provider)
    {
        if (provider is null)
        {
            throw new ArgumentNullException(nameof(provider));
        }

        _providers.Add(provider);
    }

    /// <summary>
    ///     Registers an asset loader. Last-added loader has priority when resolving.
    /// </summary>
    /// <param name="loader">Loader instance.</param>
    /// <exception cref="ArgumentNullException">If <paramref name="loader" /> is null.</exception>
    public void AddLoader(IAssetLoader loader)
    {
        if (loader is null)
        {
            throw new ArgumentNullException(nameof(loader));
        }

        _loaders.Add(loader);
    }

    /// <summary>
    ///     Invalidates (removes) all cached assets that match the normalized key of the provided path,
    ///     across all asset types. Disposes removed assets that implement <see cref="IDisposable" />.
    /// </summary>
    /// <param name="path">Original asset path (will be normalized).</param>
    public void Invalidate(string path)
    {
        var keyNorm = NormalizeKey(path);

        // Collect matching keys first to avoid modifying the dictionary while iterating its keys.
        var toRemove = new List<(Type, string)>();
        foreach (var k in _cache.Keys)
        {
            if (k.key == keyNorm)
            {
                toRemove.Add(k);
            }
        }

        // Remove and dispose any found assets.
        foreach (var k in toRemove)
        {
            if (_cache.Remove(k, out var obj))
            {
                (obj as IDisposable)?.Dispose();
            }
        }
    }

    /// <summary>
    ///     Invalidates (removes) all cached assets whose normalized key starts with the given prefix.
    ///     Disposes removed assets that implement <see cref="IDisposable" />.
    /// </summary>
    /// <param name="prefix">Path prefix (will be normalized).</param>
    public void InvalidatePrefix(string prefix)
    {
        var pref = NormalizeKey(prefix);

        // Collect all keys that start with the prefix.
        var toRemove = new List<(Type, string)>();
        foreach (var k in _cache.Keys)
        {
            if (k.key.StartsWith(pref, StringComparison.Ordinal))
            {
                toRemove.Add(k);
            }
        }

        // Remove and dispose matches.
        foreach (var k in toRemove)
        {
            if (_cache.Remove(k, out var obj))
            {
                (obj as IDisposable)?.Dispose();
            }
        }
    }

    /// <summary>
    ///     Loads an asset synchronously, returning a cached instance if available.
    /// </summary>
    /// <typeparam name="T">Asset type to load.</typeparam>
    /// <param name="path">Asset path, resolved by registered file providers.</param>
    /// <returns>Loaded asset instance.</returns>
    /// <exception cref="InvalidOperationException">If no suitable loader is registered.</exception>
    public T Load<T>(string path)
    {
        var key = MakeKey(typeof(T), path);

        // Return from cache if present.
        if (_cache.TryGetValue(key, out var obj) && obj is T cached)
        {
            return cached;
        }

        // Resolve loader and perform load.
        var loader = ResolveLoader(typeof(T), path);
        var ctx = new ContentLoadContext(_providers);
        var loaded = (T)loader.Load(ctx, path);

        // Cache and return.
        _cache[key] = loaded!;
        return loaded!;
    }

    /// <summary>
    ///     Loads an asset asynchronously, returning a cached instance if available.
    ///     Concurrent calls for the same (Type, key) will await the same in-flight task.
    /// </summary>
    /// <typeparam name="T">Asset type to load.</typeparam>
    /// <param name="path">Asset path, resolved by registered file providers.</param>
    /// <param name="ct">Cancellation token for the async load operation.</param>
    /// <returns>Loaded asset instance.</returns>
    /// <exception cref="InvalidOperationException">If no suitable loader is registered.</exception>
    public async ValueTask<T> LoadAsync<T>(string path, CancellationToken ct = default)
    {
        var key = MakeKey(typeof(T), path);

        // Return from cache if present.
        if (_cache.TryGetValue(key, out var obj) && obj is T cached)
        {
            return cached;
        }

        // Acquire or create a single in-flight task for this key.
        var task = _inflight.GetOrAdd(key, static (k, state) =>
        {
            var (self, path, tType) = (state.self, state.path, state.tType);
            var loader = self.ResolveLoader(tType, path);
            var ctx = new ContentLoadContext(self._providers);

            // Wrap ValueTask in Task to store inside ConcurrentDictionary.
            return self.WrapAsync(loader.LoadAsync(ctx, path, state.ct));
        }, (self: this, path, tType: typeof(T), ct));

        // Await the task, cache the result, and clear inflight entry.
        var result = (T)await task.ConfigureAwait(false);
        _cache[key] = result!;
        _inflight.TryRemove(key, out _);
        return result!;
    }

    /// <summary>
    ///     Attempts to get a cached asset without loading.
    /// </summary>
    /// <typeparam name="T">Asset type.</typeparam>
    /// <param name="path">Asset path (will be normalized).</param>
    /// <param name="asset">Asset if found; otherwise default.</param>
    /// <returns>True if found in cache; otherwise false.</returns>
    public bool TryGet<T>(string path, out T asset)
    {
        var key = MakeKey(typeof(T), path);
        if (_cache.TryGetValue(key, out var obj) && obj is T typed)
        {
            asset = typed;
            return true;
        }

        asset = default!;
        return false;
    }

    /// <summary>
    ///     Removes a cached asset by type and path and disposes it if applicable.
    /// </summary>
    /// <typeparam name="T">Asset type.</typeparam>
    /// <param name="path">Asset path (will be normalized).</param>
    /// <returns>True if the asset was removed; otherwise false.</returns>
    public bool Unload<T>(string path)
    {
        var key = MakeKey(typeof(T), path);
        if (_cache.Remove(key, out var obj))
        {
            (obj as IDisposable)?.Dispose();
            return true;
        }

        return false;
    }

    /// <summary>
    ///     Clears all cached assets and cancels tracking of in-flight tasks.
    ///     Disposes any cached disposable assets.
    /// </summary>
    public void UnloadAll()
    {
        foreach (var kv in _cache.Values)
        {
            (kv as IDisposable)?.Dispose();
        }

        _cache.Clear();
        _inflight.Clear();
    }

    /// <summary>
    ///     Builds the cache key tuple for an asset type and path.
    /// </summary>
    private static (Type, string) MakeKey(Type t, string path)
    {
        return (t, NormalizeKey(path));
    }

    /// <summary>
    ///     Normalizes a content path for consistent cache keys.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>Converts backslashes to forward slashes.</description></item>
    ///         <item><description>Trims leading '.' and '/'.</description></item>
    ///         <item><description>On Windows, lower-cases the path to avoid duplicate keys by case variants.</description></item>
    ///     </list>
    /// </remarks>
    private static string NormalizeKey(string path)
    {
        var norm = path.Replace('\\', '/').TrimStart('.', '/');

        // On Windows, normalize to lowercase to avoid duplicate keys by case variants.
        if (OperatingSystem.IsWindows())
        {
            norm = norm.ToLowerInvariant();
        }

        return norm;
    }

    /// <summary>
    ///     Resolves the most recently registered loader that can load the given path for type <paramref name="t" />.
    /// </summary>
    /// <exception cref="InvalidOperationException">If no matching loader is found.</exception>
    private IAssetLoader ResolveLoader(Type t, string path)
    {
        // Search in reverse registration order to give latest registrations precedence.
        for (var i = _loaders.Count - 1; i >= 0; i--)
        {
            var l = _loaders[i];
            if (l.AssetType == t && l.CanLoad(path))
            {
                return l;
            }
        }

        throw new InvalidOperationException($"No asset loader registered for type '{t.Name}' and path '{path}'.");
    }

    /// <summary>
    ///     Converts a ValueTask-based loader call to Task to store in the inflight dictionary.
    /// </summary>
    private async Task<object> WrapAsync(ValueTask<object> vt)
    {
        return await vt.ConfigureAwait(false);
    }

    /// <summary>
    ///     Disposes the content manager, unloading all assets.
    /// </summary>
    public void Dispose()
    {
        UnloadAll();
    }
}