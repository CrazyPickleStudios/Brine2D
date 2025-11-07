namespace Brine2D.Core.Content;

/// <summary>
///     File system-backed implementation of <see cref="IContentFileProvider" /> that resolves logical content paths
///     against one or more root directories on disk.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item><description>Roots are stored as absolute paths and de-duplicated using case-insensitive comparison.</description></item>
///         <item><description>The first matching file under the configured roots (in insertion order) is used.</description></item>
///         <item><description>Read operations are safe for concurrent access as long as the set of roots is not being mutated.</description></item>
///     </list>
///     <para><b>Thread safety:</b> Mutating methods such as <see cref="AddRoot(string)" /> are not thread-safe when called concurrently with reads. Coordinate access if needed.</para>
/// </remarks>
public sealed class PhysicalFileProvider : IContentFileProvider
{
    // Root directories to probe, stored as absolute paths (case-insensitive uniqueness).
    private readonly List<string> _roots = new();

    /// <summary>
    ///     Creates a provider with an optional set of initial root directories.
    /// </summary>
    /// <param name="roots">
    ///     Root directories to search. Null/empty/whitespace entries are ignored.
    ///     Each root is converted to an absolute path and added once (case-insensitive).
    /// </param>
    public PhysicalFileProvider(params string[] roots)
    {
        foreach (var r in roots)
        {
            if (string.IsNullOrWhiteSpace(r))
            {
                continue;
            }

            // Normalize to absolute paths to avoid ambiguity and enable de-duplication.
            var abs = Path.GetFullPath(r);

            // Maintain case-insensitive uniqueness to behave consistently across file systems.
            if (!_roots.Contains(abs, StringComparer.OrdinalIgnoreCase))
            {
                _roots.Add(abs);
            }
        }
    }

    /// <summary>
    ///     Adds a root directory to probe for content files.
    /// </summary>
    /// <param name="root">
    ///     The root directory to add. Null/empty/whitespace values are ignored.
    ///     The path is converted to an absolute path and added if not already present (case-insensitive).
    /// </param>
    /// <remarks>
    ///     Not thread-safe if called concurrently with read operations on this instance. Prefer configuring roots during startup.
    /// </remarks>
    public void AddRoot(string root)
    {
        if (string.IsNullOrWhiteSpace(root))
        {
            return;
        }

        var abs = Path.GetFullPath(root);
        if (!_roots.Contains(abs, StringComparer.OrdinalIgnoreCase))
        {
            _roots.Add(abs);
        }
    }

    /// <summary>
    ///     Determines whether a content file exists for the logical <paramref name="path" />.
    /// </summary>
    /// <param name="path">The logical content path (e.g., "textures/ui/button.png").</param>
    /// <returns>True if the file can be resolved; otherwise, false.</returns>
    public bool FileExists(string path)
    {
        return ResolvePath(path) is not null;
    }

    /// <summary>
    ///     Resolves a logical content <paramref name="path" /> to a concrete absolute file system path, if it exists.
    /// </summary>
    /// <param name="path">The logical content path (forward or backward slashes accepted).</param>
    /// <returns>
    ///     The first matching absolute file path under the configured roots, or null if not found.
    /// </returns>
    public string? ResolvePath(string path)
    {
        // Normalize the logical path (e.g., remove leading "./" or "/" and unify separators).
        var norm = Normalize(path);

        // Probe roots in insertion order and return the first existing file.
        foreach (var root in _roots)
        {
            var candidate = Path.Combine(root, norm);
            if (File.Exists(candidate))
            {
                return candidate;
            }
        }

        return null;
    }

    /// <summary>
    ///     Attempts to open a readable stream for the content at the given logical <paramref name="path" />.
    /// </summary>
    /// <param name="path">The logical content path to open for reading.</param>
    /// <param name="stream">
    ///     On success, an open readable stream that the caller must dispose; otherwise, <see cref="Stream.Null" />.
    /// </param>
    /// <returns>True if a readable stream was opened successfully; otherwise, false.</returns>
    /// <remarks>
    ///     Returns false when the path cannot be resolved under any root. If the path resolves but the file cannot be opened,
    ///     I/O exceptions from <see cref="File.OpenRead(string)" /> may propagate to the caller.
    /// </remarks>
    /// <exception cref="IOException">Thrown if the file exists but cannot be opened.</exception>
    /// <exception cref="UnauthorizedAccessException">Thrown if access is denied.</exception>
    public bool TryOpenRead(string path, out Stream stream)
    {
        stream = Stream.Null;

        // Resolve path to an absolute file on disk.
        var resolved = ResolvePath(path);
        if (resolved is null)
        {
            return false;
        }

        // Let any IO exceptions propagate to caller if they use the stream; simply return false if not resolvable.
        stream = File.OpenRead(resolved);
        return true;
    }

    /// <summary>
    ///     Normalizes a logical content path for consistent resolution.
    /// </summary>
    /// <remarks>
    ///     <list type="bullet">
    ///         <item><description>Converts backslashes to forward slashes.</description></item>
    ///         <item><description>Trims any leading '.' and '/' characters.</description></item>
    ///     </list>
    /// </remarks>
    /// <param name="path">The logical content path to normalize.</param>
    /// <returns>A normalized relative-style path appropriate for combining with a root.</returns>
    private static string Normalize(string path)
    {
        // Use forward slashes for a consistent internal representation and trim leading separators/current-dir markers.
        var p = path.Replace('\\', '/').TrimStart('.', '/');
        return p;
    }
}