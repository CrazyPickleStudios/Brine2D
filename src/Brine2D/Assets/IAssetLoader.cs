using Brine2D.Audio;
using Brine2D.Rendering;

namespace Brine2D.Assets;

/// <summary>
/// Unified async asset loading with caching.
/// All asset types (textures, sounds, music, fonts) go through one service.
/// Inject <see cref="IAssetLoader"/> into your scene or system constructor.
/// No content pipeline, no build step: drag files into your assets folder and load them.
/// </summary>
/// <remarks>
/// <para>
/// <strong>Scoped lifetime:</strong> When resolved from a scene's DI scope (the default
/// registration), all loaded assets and manifests are tracked and released automatically
/// when the scope is disposed during scene transitions. Manual <c>Release*</c> and
/// <c>Unload</c> calls are still supported for mid-scene cleanup but are no longer required.
/// </para>
/// <para>
/// <strong>Cancellation:</strong> Cancelling the <c>CancellationToken</c> passed to a
/// <c>GetOrLoad*Async</c> call stops the caller's await but does not abort the underlying
/// I/O. Because multiple callers and manifest preloads can share a single in-flight load,
/// the load runs to completion and the result is cached normally. To remove a cached asset,
/// use the corresponding <c>Release*</c> method or <see cref="Unload"/>.
/// </para>
/// <para>
/// <strong>Direct-load reference counting:</strong> Each <c>GetOrLoad*Async</c> call
/// increments a per-key direct reference count. Call the corresponding <c>Release*</c>
/// method once for each <c>GetOrLoad*Async</c> call to release the asset. The asset is
/// freed only when both the direct count and all manifest references reach zero.
/// If a <c>GetOrLoad*Async</c> call fails (the returned task faults or is cancelled),
/// the reference count is rolled back automatically; only successful loads count.
/// </para>
/// </remarks>
public interface IAssetLoader
{
    /// <summary>
    /// Returns a cached texture, loading it on first request.
    /// The (path, scaleMode) pair is the cache key; the same file at different scale modes is two separate entries.
    /// </summary>
    Task<ITexture> GetOrLoadTextureAsync(
        string path,
        TextureScaleMode scaleMode = TextureScaleMode.Linear,
        CancellationToken cancellationToken = default);

    /// <summary>Returns a cached sound effect, loading it on first request.</summary>
    Task<ISoundEffect> GetOrLoadSoundAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>Returns cached music, loading it on first request.</summary>
    Task<IMusic> GetOrLoadMusicAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a cached font, loading it on first request.
    /// The (path, size) pair is the cache key; the same file at different sizes is two separate entries.
    /// </summary>
    Task<IFont> GetOrLoadFontAsync(string path, int size, CancellationToken cancellationToken = default);

    /// <summary>
    /// Decrements the direct-load reference count for the given texture. Each call to
    /// <see cref="GetOrLoadTextureAsync"/> adds one direct reference; call this method
    /// once per load to release it. The texture is unloaded and GPU resources are freed
    /// only when both the direct count and all manifest references reach zero.
    /// Has no effect if the texture was only loaded via a manifest.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the asset's total reference count reached zero and it was freed;
    /// <see langword="false"/> if still referenced or not a direct load.
    /// </returns>
    bool ReleaseTexture(string path, TextureScaleMode scaleMode = TextureScaleMode.Linear);

    /// <summary>
    /// Decrements the direct-load reference count for the given sound effect. Each call to
    /// <see cref="GetOrLoadSoundAsync"/> adds one direct reference; call this method
    /// once per load to release it. The sound is unloaded and audio resources are freed
    /// only when both the direct count and all manifest references reach zero.
    /// Has no effect if the sound was only loaded via a manifest.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the asset's total reference count reached zero and it was freed;
    /// <see langword="false"/> if still referenced or not a direct load.
    /// </returns>
    bool ReleaseSound(string path);

    /// <summary>
    /// Decrements the direct-load reference count for the given music. Each call to
    /// <see cref="GetOrLoadMusicAsync"/> adds one direct reference; call this method
    /// once per load to release it. The music is unloaded and audio resources are freed
    /// only when both the direct count and all manifest references reach zero.
    /// Has no effect if the music was only loaded via a manifest.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the asset's total reference count reached zero and it was freed;
    /// <see langword="false"/> if still referenced or not a direct load.
    /// </returns>
    bool ReleaseMusic(string path);

    /// <summary>
    /// Decrements the direct-load reference count for the given font. Each call to
    /// <see cref="GetOrLoadFontAsync"/> adds one direct reference; call this method
    /// once per load to release it. The font is unloaded and its resources are freed
    /// only when both the direct count and all manifest references reach zero.
    /// Has no effect if the font was only loaded via a manifest.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> if the asset's total reference count reached zero and it was freed;
    /// <see langword="false"/> if still referenced or not a direct load.
    /// </returns>
    bool ReleaseFont(string path, int size);

    /// <summary>
    /// Resolves all <see cref="AssetRef{T}"/> fields declared on <paramref name="manifest"/>
    /// in parallel, reporting progress as each asset completes.
    /// Call this in <c>OnLoadAsync</c>. Assets are safe to access from <c>OnEnter</c> onwards.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <strong>Partial failure:</strong> If some assets fail while others succeed, an
    /// <see cref="AggregateException"/> is thrown containing only the failures. Successfully
    /// loaded assets remain cached and their <see cref="AssetRef{T}.Value"/> is set.
    /// Calling <c>PreloadAsync</c> again on the same manifest retries only the unresolved
    /// refs (resolved refs short-circuit), making transient errors (network timeouts, file
    /// locks) recoverable without reloading the entire manifest.
    /// </para>
    /// <para>
    /// If the failure is non-recoverable, call <see cref="Unload"/> to release the
    /// partially loaded assets before retrying or transitioning scenes.
    /// </para>
    /// <para>
    /// <strong>Cancellation:</strong> If the <paramref name="cancellationToken"/> is triggered,
    /// the manifest tracking and its reference counts are rolled back automatically so no
    /// phantom references remain. A subsequent call to <c>PreloadAsync</c> on the same
    /// manifest will re-track and retry from scratch.
    /// </para>
    /// </remarks>
    Task PreloadAsync(
        AssetManifest manifest,
        IProgress<AssetLoadProgress>? progress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Resets all <see cref="AssetRef{T}"/> fields on <paramref name="manifest"/> and
    /// decrements internal reference counts. Assets whose count reaches zero are unloaded
    /// from the cache and their GPU/audio resources are freed. Assets still referenced by
    /// another tracked manifest or by a direct <c>GetOrLoad*Async</c> call remain cached.
    /// <para>
    /// In-flight loads for keys that reach zero will be cancelled and their native resources
    /// will be disposed automatically.
    /// </para>
    /// </summary>
    void Unload(AssetManifest manifest);

    /// <summary>
    /// Unloads all cached assets and frees GPU/audio resources.
    /// <para>
    /// <strong>Manifests:</strong> Any <see cref="AssetManifest"/> instances previously passed
    /// to <see cref="PreloadAsync"/> are unloaded and their <see cref="AssetRef{T}"/>
    /// fields are reset. Assets still referenced by other scopes remain cached.
    /// </para>
    /// </summary>
    void UnloadAll();
}
