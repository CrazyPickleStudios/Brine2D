namespace Brine2D.Core.Content;

/// <summary>
///     Base class for strongly-typed asset loaders used by the content system.
///     Implementations declare the asset <typeparamref name="T" /> they produce and
///     provide both sync and async loading paths.
/// </summary>
/// <typeparam name="T">
///     The concrete asset type produced by this loader. Must be non-nullable.
/// </typeparam>
public abstract class AssetLoader<T> : IAssetLoader
    where T : notnull
{
    /// <summary>
    ///     Gets the runtime type of the asset this loader produces.
    /// </summary>
    public Type AssetType => typeof(T);

    /// <summary>
    ///     Returns true if this loader can handle the specified path (e.g., by extension or schema).
    /// </summary>
    /// <param name="path">Logical or resolved asset path.</param>
    /// <returns>True if the loader can handle the path; otherwise false.</returns>
    public abstract bool CanLoad(string path);

    /// <summary>
    ///     Synchronously loads an asset of type <typeparamref name="T" />.
    /// </summary>
    /// <param name="context">The content loading context to access file providers.</param>
    /// <param name="path">Logical or resolved asset path.</param>
    /// <returns>The loaded asset instance.</returns>
    /// <remarks>
    ///     Implementations may use <see cref="ContentLoadContext.OpenRead(string)" /> or
    ///     <see cref="ContentLoadContext.TryOpenRead(string, out System.IO.Stream)" /> to access asset data.
    /// </remarks>
    public abstract T LoadTyped(ContentLoadContext context, string path);

    /// <summary>
    ///     Asynchronously loads an asset of type <typeparamref name="T" />.
    /// </summary>
    /// <param name="context">The content loading context to access file providers.</param>
    /// <param name="path">Logical or resolved asset path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="ValueTask{TResult}" /> producing the loaded asset.</returns>
    /// <remarks>
    ///     Prefer truly asynchronous I/O where possible. If not feasible, consider wrapping synchronous work
    ///     appropriately while honoring <paramref name="ct" /> when possible.
    /// </remarks>
    public abstract ValueTask<T> LoadTypedAsync(ContentLoadContext context, string path, CancellationToken ct);

    /// <summary>
    ///     Explicit <see cref="IAssetLoader" /> implementation that forwards to
    ///     <see cref="LoadTyped(ContentLoadContext, string)" />.
    /// </summary>
    /// <param name="context">The content loading context.</param>
    /// <param name="path">Logical or resolved asset path.</param>
    /// <returns>The loaded asset instance as <see cref="object" />.</returns>
    object IAssetLoader.Load(ContentLoadContext context, string path)
    {
        // Forward to strongly-typed load; suppress nullability via '!' since T is constrained to notnull.
        return LoadTyped(context, path)!;
    }

    /// <summary>
    ///     Explicit <see cref="IAssetLoader" /> implementation that forwards to
    ///     <see cref="LoadTypedAsync(ContentLoadContext, string, CancellationToken)" />.
    /// </summary>
    /// <param name="context">The content loading context.</param>
    /// <param name="path">Logical or resolved asset path.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A <see cref="ValueTask{Object}" /> producing the loaded asset as <see cref="object" />.</returns>
    async ValueTask<object> IAssetLoader.LoadAsync(ContentLoadContext context, string path, CancellationToken ct)
    {
        // Forward to strongly-typed async load; suppress nullability via '!' since T is constrained to notnull.
        return await LoadTypedAsync(context, path, ct)!;
    }
}