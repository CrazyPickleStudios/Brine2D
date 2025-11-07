namespace Brine2D.Core.Content;

/// <summary>
///     Provides an abstraction for locating and opening read-only content files
///     from one or more backing stores (e.g., file system, embedded resources, archives).
/// </summary>
/// <remarks>
///     Implementations should be safe for concurrent read access.
///     All methods should treat <paramref name="path" /> as a logical content path
///     (often relative) and handle any provider-specific resolution logic internally.
/// </remarks>
public interface IContentFileProvider
{
    /// <summary>
    ///     Determines whether a content file exists for the given logical <paramref name="path" />.
    /// </summary>
    /// <param name="path">The logical content path to check.</param>
    /// <returns>
    ///     True if the file can be resolved by this provider; otherwise, false.
    /// </returns>
    bool FileExists(string path);

    /// <summary>
    ///     Resolves the given logical <paramref name="path" /> to a concrete, provider-specific path.
    /// </summary>
    /// <param name="path">The logical content path to resolve.</param>
    /// <returns>
    ///     A concrete path (for example, an absolute file system path) if resolvable; otherwise, null.
    ///     Providers that do not map to a real file system path should return null.
    /// </returns>
    string? ResolvePath(string path);

    /// <summary>
    ///     Attempts to open a readable stream for the content at the given logical <paramref name="path" />.
    /// </summary>
    /// <param name="path">The logical content path to open for reading.</param>
    /// <param name="stream">
    ///     When this method returns true, contains an open readable <see cref="Stream" /> that the caller must dispose.
    ///     When this method returns false, implementations should assign <see cref="Stream.Null" /> to this parameter.
    /// </param>
    /// <returns>
    ///     True if a readable stream was opened successfully; otherwise, false.
    /// </returns>
    bool TryOpenRead(string path, out Stream stream);
}