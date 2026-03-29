using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;

namespace Brine2D.Assets;

/// <summary>
/// Non-generic interface used by <see cref="AssetManifest"/> to collect all typed refs
/// via reflection without knowing the generic parameter.
/// </summary>
internal interface IAssetRef
{
    string Path { get; }
    RefCountKey RefKey { get; }
    bool IsLoaded { get; }
    Task LoadAsync(AssetCache cache, ILogger logger, CancellationToken ct);
    void Reset();
}

/// <summary>
/// A typed, lazy asset reference. Declare fields of this type on an <see cref="AssetManifest"/>
/// subclass, then call <see cref="IAssetLoader.PreloadAsync"/> to resolve them all in parallel.
/// </summary>
/// <example>
/// <code>
/// public class GameAssets : AssetManifest
/// {
///     public readonly AssetRef&lt;ITexture&gt;    Player    = Texture("assets/images/player.png");
///     public readonly AssetRef&lt;ISoundEffect&gt; JumpSound = Sound("assets/audio/jump.wav");
///     public readonly AssetRef&lt;IMusic&gt;       Theme     = Music("assets/audio/theme.ogg");
///     public readonly AssetRef&lt;Font&gt;         UIFont    = Font("assets/fonts/ui.ttf", size: 16);
/// }
///
/// // In OnLoadAsync:
/// await _assets.PreloadAsync(_manifest, progress: loadingScreen.Progress, ct);
///
/// // After loading, implicit conversion; no .Value needed:
/// sprite.Texture = _manifest.Player;
/// </code>
/// </example>
public sealed class AssetRef<T> : IAssetRef where T : class
{
    private readonly Func<AssetCache, CancellationToken, Task<T>> _resolve;
    private readonly RefCountKey _refKey;
    private readonly Lock _writeLock = new();
    private T? _value;
    private int _version;

    /// <summary>Path used to identify this asset (display/logging only).</summary>
    public string Path { get; }

    RefCountKey IAssetRef.RefKey => _refKey;

    /// <summary>Whether this ref has been resolved by <see cref="IAssetLoader.PreloadAsync"/>.</summary>
    public bool IsLoaded => Volatile.Read(ref _value) is not null;

    /// <summary>
    /// The loaded asset. Throws <see cref="InvalidOperationException"/> if accessed before loading.
    /// </summary>
    public T Value
    {
        get
        {
            var v = Volatile.Read(ref _value);
            if (v is null)
                throw new InvalidOperationException(
                    $"Asset '{Path}' has not been loaded. " +
                    $"Await IAssetLoader.PreloadAsync(manifest) in OnLoadAsync() before accessing assets.");
            return v;
        }
    }

    /// <summary>
    /// Atomically checks whether this ref has been resolved and retrieves the value.
    /// Use this instead of <see cref="IsLoaded"/> + <see cref="Value"/> when a concurrent
    /// <see cref="IAssetLoader.Unload"/> could reset the ref between the two reads.
    /// </summary>
    public bool TryGetValue([MaybeNullWhen(false)] out T value)
    {
        value = Volatile.Read(ref _value);
        return value is not null;
    }

    internal AssetRef(
        string path,
        RefCountKey refKey,
        Func<AssetCache, CancellationToken, Task<T>> resolve)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);
        ArgumentNullException.ThrowIfNull(resolve);
        Path = path;
        _refKey = refKey;
        _resolve = resolve;
    }

    async Task IAssetRef.LoadAsync(AssetCache cache, ILogger logger, CancellationToken ct)
    {
        int versionBefore;
        lock (_writeLock)
        {
            if (_value is not null) return;
            versionBefore = _version;
        }

        var result = await _resolve(cache, ct).ConfigureAwait(false);

        lock (_writeLock)
        {
            if (_version != versionBefore)
            {
                logger.LogDebug(
                    "AssetRef<{Type}>('{Path}') was reset while its load was in flight; discarding result",
                    typeof(T).Name, Path);
                return;
            }

            _value = result;
        }
    }

    void IAssetRef.Reset()
    {
        lock (_writeLock)
        {
            _version++;
            _value = null;
        }
    }

    /// <summary>
    /// Implicit conversion so the ref can be used directly wherever <typeparamref name="T"/>
    /// is expected, without <c>.Value</c>. Throws <see cref="InvalidOperationException"/> if
    /// the asset has not been loaded; only use this in code paths that run after
    /// <see cref="IAssetLoader.PreloadAsync"/> has completed. For safe access in code that
    /// may race with <see cref="IAssetLoader.Unload"/>, prefer <see cref="TryGetValue"/>.
    /// </summary>
    public static implicit operator T(AssetRef<T> assetRef) => assetRef.Value;
}