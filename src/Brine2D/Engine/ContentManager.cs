using System.Collections.Concurrent;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine;

public sealed class ContentManager : IContentManager
{
    private readonly ConcurrentDictionary<string, Entry> _cache = new(StringComparer.OrdinalIgnoreCase);
    private readonly ILogger<ContentManager> _logger;
    private readonly IAssetLoaderRegistry _registry;

    public ContentManager(IAssetLoaderRegistry registry, ILogger<ContentManager> logger)
    {
        _registry = registry;
        _logger = logger;
    }

    public void Clear()
    {
        foreach (var kv in _cache)
        {
            (kv.Value.Asset as IDisposable)?.Dispose();
        }

        _cache.Clear();
    }

    public void Dispose()
    {
        Clear();
    }

    public T Load<T>(string key, string path)
        where T : class, IDisposable
    {
        if (_cache.TryGetValue(key, out var existing))
        {
            existing.RefCount++;

            return (T)existing.Asset;
        }

        var loader = _registry.GetLoader<T>();
        var asset = loader.LoadAsync(path).GetAwaiter().GetResult();

        _cache[key] = new Entry { Asset = asset, RefCount = 1 };

        return asset;
    }

    public T Load<T>(string path) where T : class, IDisposable
    {
        return Load<T>(path, path);
    }

    public async Task<T> LoadAsync<T>(string key, string path, CancellationToken ct = default)
        where T : class, IDisposable
    {
        if (_cache.TryGetValue(key, out var existing))
        {
            existing.RefCount++;
            return (T)existing.Asset;
        }

        var loader = _registry.GetLoader<T>();
        var asset = await loader.LoadAsync(path, ct).ConfigureAwait(false);

        _cache[key] = new Entry { Asset = asset, RefCount = 1 };

        return asset;
    }

    public Task<T> LoadAsync<T>(string path, CancellationToken ct = default)
        where T : class, IDisposable
    {
        return LoadAsync<T>(path, path, ct);
    }

    public bool TryGet<T>(string key, out T? asset) where T : class, IDisposable
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            asset = (T)entry.Asset;

            return true;
        }

        asset = null;

        return false;
    }

    public async Task<T?> TryLoadAsync<T>(string key, string path, CancellationToken ct = default)
        where T : class, IDisposable
    {
        try
        {
            return await LoadAsync<T>(key, path, ct).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            _logger.LogInformation("Content load canceled: {Path}", path);

            return null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load content: {Path}", path);

            return null;
        }
    }

    public Task<T?> TryLoadAsync<T>(string path, CancellationToken ct = default)
        where T : class, IDisposable
    {
        return TryLoadAsync<T>(path, path, ct);
    }

    public void Unload(string key)
    {
        if (_cache.TryGetValue(key, out var entry))
        {
            entry.RefCount--;

            if (entry.RefCount <= 0 && _cache.TryRemove(key, out var removed))
            {
                (removed.Asset as IDisposable)?.Dispose();
            }
        }
    }

    private sealed class Entry
    {
        public object Asset = null!;
        public int RefCount;
    }
}