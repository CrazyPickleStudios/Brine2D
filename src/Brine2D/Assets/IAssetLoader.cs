using Brine2D.Audio;
using Brine2D.Rendering;

namespace Brine2D.Assets;

/// <summary>
/// Unified async asset loading with caching.
/// All asset types (textures, sounds, music, fonts) go through one service.
/// Inject <see cref="IAssetLoader"/> into your scene or system constructor.
/// No content pipeline, no build step: drag files into your assets folder and load them.
/// </summary>
public interface IAssetLoader
{
    /// <summary>Loads a texture. Does not cache. Use <see cref="GetOrLoadTextureAsync"/> for scenes.</summary>
    Task<ITexture> LoadTextureAsync(
        string path,
        TextureScaleMode scaleMode = TextureScaleMode.Linear,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a cached texture, loading it on first request.</summary>
    Task<ITexture> GetOrLoadTextureAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Returns a cached sound effect, loading it on first request.</summary>
    Task<ISoundEffect> GetOrLoadSoundAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Returns cached music, loading it on first request.</summary>
    Task<IMusic> GetOrLoadMusicAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a cached font, loading it on first request.
    /// The (path, size) pair is the cache key; the same file at different sizes is two separate entries.
    /// </summary>
    Task<Font> GetOrLoadFontAsync(string path, int size, CancellationToken cancellationToken = default);

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
