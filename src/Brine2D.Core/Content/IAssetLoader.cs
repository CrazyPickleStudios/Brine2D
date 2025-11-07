namespace Brine2D.Core.Content;

/// <summary>
///     Defines a loader that turns raw content files into runtime assets.
/// </summary>
/// <remarks>
///     An implementation typically decides what it can load based on file name or extension and uses the provided
///     <see cref="ContentLoadContext" /> to locate and open files from one or more <see cref="IContentFileProvider" />
///     sources.
/// </remarks>
public interface IAssetLoader
{
    /// <summary>
    ///     Gets the concrete <see cref="Type" /> of asset instances produced by this loader.
    /// </summary>
    /// <remarks>
    ///     All successful calls to <see cref="Load(ContentLoadContext, string)" /> and
    ///     <see cref="LoadAsync(ContentLoadContext, string, System.Threading.CancellationToken)" /> must return an object that
    ///     is assignable to this type.
    /// </remarks>
    Type AssetType { get; }

    /// <summary>
    ///     Determines whether this loader can handle the specified content path.
    /// </summary>
    /// <param name="path">A logical or relative asset path (for example, a file name or extension).</param>
    /// <returns>true if this loader can load the asset at <paramref name="path" />; otherwise, false.</returns>
    /// <remarks>
    ///     Implementations should keep this check fast and side-effect free; it should not perform any I/O.
    ///     Typical checks include file name patterns or extensions (e.g., ".png", ".json").
    /// </remarks>
    bool CanLoad(string path);

    /// <summary>
    ///     Loads an asset synchronously from the specified <paramref name="path" />.
    /// </summary>
    /// <param name="context">The content loading context used to resolve and open files.</param>
    /// <param name="path">A logical or relative asset path to load.</param>
    /// <returns>
    ///     The loaded asset instance. The returned object must be assignable to <see cref="AssetType" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context" /> or <paramref name="path" /> is null.</exception>
    /// <exception cref="FileNotFoundException">
    ///     Thrown if the path cannot be resolved or opened via <paramref name="context" />.
    /// </exception>
    /// <exception cref="IOException">Thrown if an I/O error occurs while reading the asset data.</exception>
    /// <remarks>
    ///     Implementations should use <see cref="ContentLoadContext.OpenRead(string)" /> or
    ///     <see cref="ContentLoadContext.TryOpenRead(string, out System.IO.Stream)" /> to access file data.
    ///     Prefer <see cref="LoadAsync(ContentLoadContext, string, System.Threading.CancellationToken)" /> for large assets.
    /// </remarks>
    object Load(ContentLoadContext context, string path);

    /// <summary>
    ///     Loads an asset asynchronously from the specified <paramref name="path" />.
    /// </summary>
    /// <param name="context">The content loading context used to resolve and open files.</param>
    /// <param name="path">A logical or relative asset path to load.</param>
    /// <param name="ct">A cancellation token to observe during the load operation.</param>
    /// <returns>
    ///     A task that completes with the loaded asset instance. The returned object must be assignable to
    ///     <see cref="AssetType" />.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="context" /> or <paramref name="path" /> is null.</exception>
    /// <exception cref="FileNotFoundException">
    ///     Thrown if the path cannot be resolved or opened via <paramref name="context" />.
    /// </exception>
    /// <exception cref="IOException">Thrown if an I/O error occurs while reading the asset data.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="ct" />.</exception>
    /// <remarks>
    ///     Implementations should observe <paramref name="ct" /> for cooperative cancellation and use asynchronous I/O
    ///     where possible. Use <see cref="ContentLoadContext.ResolvePath(string)" /> to map logical paths to provider-backed
    ///     paths.
    /// </remarks>
    ValueTask<object> LoadAsync(ContentLoadContext context, string path, CancellationToken ct);
}