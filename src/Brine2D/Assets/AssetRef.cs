namespace Brine2D.Assets;

/// <summary>
/// Non-generic interface used by <see cref="AssetManifest"/> to collect all typed refs
/// via reflection without knowing the generic parameter.
/// </summary>
internal interface IAssetRef
{
    string Path { get; }
    bool IsLoaded { get; }
    Task LoadAsync(IAssetLoader loader, CancellationToken ct);
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
public sealed class AssetRef<T> : IAssetRef
{
    private readonly Func<IAssetLoader, CancellationToken, Task<T>> _resolve;
    private T? _value;

    /// <summary>Path (or path:size key for fonts) used to identify this asset.</summary>
    public string Path { get; }

    /// <summary>Whether this ref has been resolved by <see cref="IAssetLoader.PreloadAsync"/>.</summary>
    public bool IsLoaded { get; private set; }

    /// <summary>
    /// The loaded asset. Throws <see cref="InvalidOperationException"/> if accessed before loading.
    /// </summary>
    public T Value => IsLoaded
        ? _value!
        : throw new InvalidOperationException(
            $"Asset '{Path}' has not been loaded. " +
            $"Await IAssetLoader.PreloadAsync(manifest) in OnLoadAsync() before accessing assets.");

    internal AssetRef(string path, Func<IAssetLoader, CancellationToken, Task<T>> resolve)
    {
        Path = path;
        _resolve = resolve;
    }

    async Task IAssetRef.LoadAsync(IAssetLoader loader, CancellationToken ct)
    {
        _value = await _resolve(loader, ct);
        IsLoaded = true;
    }

    /// <summary>
    /// Implicit conversion. Use the ref directly wherever T is expected, without <c>.Value</c>.
    /// Throws if the asset has not been loaded yet.
    /// </summary>
    public static implicit operator T(AssetRef<T> assetRef) => assetRef.Value;

    public override string ToString() => $"AssetRef<{typeof(T).Name}>(\"{Path}\", loaded={IsLoaded})";
}