namespace Brine2D.Core.Content;

/// <summary>
///     Central service for locating, loading, caching, and unloading content assets.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>
///                 Uses registered <see cref="IAssetLoader" /> instances to deserialize assets and registered
///                 <see cref="IContentFileProvider" /> instances to locate and open content streams.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Caches loaded assets by logical content path and requested type; subsequent loads return the
///                 cached instance.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Implementations should support concurrent loads for different assets and typically
///                 de-duplicate concurrent loads of the same asset.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Unloading disposes assets that implement <see cref="IDisposable" />; handling
///                 <see cref="IAsyncDisposable" /> is implementation-specific.
///             </description>
///         </item>
///         <item>
///             <description>
///                 Thread-safety is implementation-specific; unless stated otherwise, assume single-threaded use
///                 (main/game thread).
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <seealso cref="IAssetLoader" />
/// <seealso cref="IContentFileProvider" />
/// <seealso cref="ContentLoadContext" />
/// <example>
///     <code>
///     // Setup
///     var content = new ContentManager();
///     content.AddFileProvider(new PhysicalFileProvider("Content", "Assets"));
///     content.AddLoader(new StringLoader());
/// 
///     // Load (cached on first call)
///     var text = content.Load&lt;string&gt;("docs/readme.txt");
/// 
///     // Async load
///     using var cts = new CancellationTokenSource();
///     var image = await content.LoadAsync&lt;ITexture2D&gt;("images/player.png", cts.Token);
/// 
///     // TryGet without I/O
///     if (content.TryGet&lt;string&gt;("docs/readme.txt", out var cached))
///     {
///         // use cached
///     }
/// 
///     // Unload one or all
///     content.Unload&lt;string&gt;("docs/readme.txt");
///     content.UnloadAll();
///     </code>
/// </example>
public interface IContentManager
{
    /// <summary>
    ///     Registers an <see cref="IContentFileProvider" /> used to resolve and open content files.
    ///     Providers are queried in registration order unless specified otherwise by the implementation.
    /// </summary>
    /// <param name="provider">The file provider instance to register.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="provider" /> is null.</exception>
    void AddFileProvider(IContentFileProvider provider);

    /// <summary>
    ///     Registers an <see cref="IAssetLoader" /> capable of loading one or more asset types.
    ///     Implementations may prefer the most recently added loader when multiple loaders can handle a path.
    /// </summary>
    /// <param name="loader">The loader instance to register.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="loader" /> is null.</exception>
    void AddLoader(IAssetLoader loader);

    /// <summary>
    ///     Loads an asset of type <typeparamref name="T" /> from the given logical content <paramref name="path" />.
    ///     If the asset is already cached, returns the cached instance.
    /// </summary>
    /// <typeparam name="T">The expected asset type to load.</typeparam>
    /// <param name="path">Logical content path (provider-specific resolution applies).</param>
    /// <returns>The loaded asset instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="path" /> is null or empty.</exception>
    /// <exception cref="FileNotFoundException">Thrown if no registered provider can resolve/open <paramref name="path" />.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if no suitable <see cref="IAssetLoader" /> can load <typeparamref name="T" /> for the specified path,
    ///     or if the deserialized asset cannot be cast to <typeparamref name="T" />.
    /// </exception>
    T Load<T>(string path);

    /// <summary>
    ///     Asynchronously loads an asset of type <typeparamref name="T" /> from the given logical content
    ///     <paramref name="path" />. If the asset is already cached, returns the cached instance.
    /// </summary>
    /// <typeparam name="T">The expected asset type to load.</typeparam>
    /// <param name="path">Logical content path (provider-specific resolution applies).</param>
    /// <param name="ct">A token to observe while waiting for the task to complete.</param>
    /// <returns>A <see cref="ValueTask{TResult}" /> that completes with the loaded asset.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="path" /> is null or empty.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="ct" />.</exception>
    /// <exception cref="FileNotFoundException">Thrown if no registered provider can resolve/open <paramref name="path" />.</exception>
    /// <exception cref="InvalidOperationException">
    ///     Thrown if no suitable <see cref="IAssetLoader" /> can load <typeparamref name="T" /> for the specified path,
    ///     or if the deserialized asset cannot be cast to <typeparamref name="T" />.
    /// </exception>
    ValueTask<T> LoadAsync<T>(string path, CancellationToken ct = default);

    /// <summary>
    ///     Attempts to get a previously loaded and cached asset of type <typeparamref name="T" /> for the given path.
    ///     Does not trigger any I/O or new loads.
    /// </summary>
    /// <typeparam name="T">The asset type expected in the cache.</typeparam>
    /// <param name="path">Logical content path used when the asset was loaded.</param>
    /// <param name="asset">When this method returns, contains the cached asset if found; otherwise, default.</param>
    /// <returns>True if the asset is found in cache; otherwise, false.</returns>
    bool TryGet<T>(string path, out T asset);

    /// <summary>
    ///     Unloads a previously loaded asset of type <typeparamref name="T" /> for the given path,
    ///     removing it from the cache and disposing it if it implements <see cref="IDisposable" />.
    /// </summary>
    /// <typeparam name="T">The asset type to unload.</typeparam>
    /// <param name="path">Logical content path used when the asset was loaded.</param>
    /// <returns>True if an asset was found and removed; otherwise, false.</returns>
    bool Unload<T>(string path);

    /// <summary>
    ///     Unloads all cached assets, disposing any that implement <see cref="IDisposable" />.
    ///     After this call, no assets remain in the cache.
    /// </summary>
    void UnloadAll();
}