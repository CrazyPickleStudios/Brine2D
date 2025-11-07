namespace Brine2D.Core.Content;

/// <summary>
///     Provides read-only access to content files via a chain of <see cref="IContentFileProvider" />s.
///     Providers are queried in the order they were supplied; the first provider that can satisfy
///     the request is used. This context does not cache results and does not manage provider lifetimes.
/// </summary>
/// <remarks>
///     Typical usage is to create a context with multiple providers (e.g., file system, embedded resources)
///     and then resolve/open content through this unified façade.
/// </remarks>
public sealed class ContentLoadContext
{
    private readonly IReadOnlyList<IContentFileProvider> _providers;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ContentLoadContext" /> class.
    /// </summary>
    /// <param name="providers">
    ///     The ordered set of content file providers to probe. The order is preserved and significant.
    /// </param>
    internal ContentLoadContext(IReadOnlyList<IContentFileProvider> providers)
    {
        _providers = providers;
    }

    /// <summary>
    ///     Opens a readable stream for the specified content <paramref name="path" /> by probing providers in order.
    /// </summary>
    /// <param name="path">The logical content path to open.</param>
    /// <returns>
    ///     A readable <see cref="Stream" /> positioned at the start of the content. The caller is responsible for disposing
    ///     it.
    /// </returns>
    /// <exception cref="FileNotFoundException">
    ///     Thrown if none of the providers can open the specified path.
    /// </exception>
    public Stream OpenRead(string path)
    {
        // Probe providers in order; return the first stream that opens successfully.
        foreach (var p in _providers)
        {
            if (p.TryOpenRead(path, out var stream))
            {
                return stream;
            }
        }

        // If no provider can open the path, throw.
        throw new FileNotFoundException($"Content file not found: '{path}'");
    }

    /// <summary>
    ///     Attempts to resolve the physical or canonical path corresponding to the logical content <paramref name="path" />.
    /// </summary>
    /// <param name="path">The logical content path to resolve.</param>
    /// <returns>
    ///     The first non-null resolved path returned by a provider; otherwise, <c>null</c> if none can resolve it.
    /// </returns>
    public string? ResolvePath(string path)
    {
        foreach (var p in _providers)
        {
            var resolved = p.ResolvePath(path);
            if (resolved is not null)
            {
                return resolved;
            }
        }

        return null;
    }

    /// <summary>
    ///     Tries to open a readable stream for the specified content <paramref name="path" />.
    /// </summary>
    /// <param name="path">The logical content path to open.</param>
    /// <param name="stream">
    ///     When this method returns, contains a readable <see cref="Stream" /> if the operation succeeded;
    ///     otherwise, <see cref="Stream.Null" />.
    /// </param>
    /// <returns><c>true</c> if a provider opened the stream; otherwise, <c>false</c>.</returns>
    public bool TryOpenRead(string path, out Stream stream)
    {
        foreach (var p in _providers)
        {
            if (p.TryOpenRead(path, out stream))
            {
                return true;
            }
        }

        // Standardize the out parameter on failure.
        stream = Stream.Null;
        return false;
    }
}