namespace Brine2D.Core.Content.Loaders;

/// <summary>
///     Asset loader that reads raw bytes from a content path.
/// </summary>
/// <remarks>
///     This loader makes no assumptions about file format and returns the file contents as-is.
/// </remarks>
public sealed class BytesLoader : AssetLoader<byte[]>
{
    /// <summary>
    ///     Indicates whether this loader can handle the specified path.
    /// </summary>
    /// <param name="path">The content path to check.</param>
    /// <returns>Always <see langword="true" />; raw bytes can be loaded from any path.</returns>
    public override bool CanLoad(string path)
    {
        // This loader accepts any path because it merely returns the raw bytes from the stream.
        return true;
    }

    /// <summary>
    ///     Synchronously loads the content at the given path as a byte array.
    /// </summary>
    /// <param name="context">The content loading context used to resolve and open streams.</param>
    /// <param name="path">The content path to load.</param>
    /// <returns>The file contents as a byte array.</returns>
    public override byte[] LoadTyped(ContentLoadContext context, string path)
    {
        // Open a read-only stream for the requested content path.
        using var s = context.OpenRead(path);

        // Copy the stream into a resizable in-memory buffer.
        using var ms = new MemoryStream();
        s.CopyTo(ms);

        // Return the buffered bytes.
        return ms.ToArray();
    }

    /// <summary>
    ///     Asynchronously loads the content at the given path as a byte array.
    /// </summary>
    /// <param name="context">The content loading context used to resolve and open streams.</param>
    /// <param name="path">The content path to load.</param>
    /// <param name="ct">A cancellation token to observe while waiting for the operation to complete.</param>
    /// <returns>A task that resolves to the file contents as a byte array.</returns>
    public override async ValueTask<byte[]> LoadTypedAsync
    (
        ContentLoadContext context,
        string path,
        CancellationToken ct
    )
    {
        // Open a read-only stream for the requested content path.
        await using var s = context.OpenRead(path);

        // Copy the stream into a resizable in-memory buffer asynchronously.
        using var ms = new MemoryStream();
        await s.CopyToAsync(ms, ct).ConfigureAwait(false);

        // Return the buffered bytes.
        return ms.ToArray();
    }
}