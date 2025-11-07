using System.Text;

namespace Brine2D.Core.Content.Loaders;

/// <summary>
///     Asset loader that reads text content into a <see cref="string" /> using a specified <see cref="Encoding" />.
/// </summary>
/// <remarks>
///     <list type="bullet">
///         <item>
///             <description>Defaults to UTF-8 when no encoding is provided (BOMs are honored if present).</description>
///         </item>
///         <item>
///             <description>
///                 <see cref="CanLoad(string)" /> returns <see langword="true" />; existence/resolution is delegated to
///                 providers.
///             </description>
///         </item>
///         <item>
///             <description>Provides both synchronous and asynchronous load paths.</description>
///         </item>
///     </list>
/// </remarks>
public sealed class StringLoader : AssetLoader<string>
{
    /// <summary>
    ///     Encoding used for reading text streams.
    /// </summary>
    /// <remarks>
    ///     Defaults to <see cref="UTF8Encoding" /> without BOM to avoid prefixing the returned string with a BOM.
    /// </remarks>
    private readonly Encoding _encoding;

    /// <summary>
    ///     Creates a new <see cref="StringLoader" />.
    /// </summary>
    /// <param name="encoding">
    ///     Optional text encoding. If null, uses UTF-8 without BOM (<see cref="UTF8Encoding(bool)" /> with emitIdentifier:
    ///     false).
    /// </param>
    public StringLoader(Encoding? encoding = null)
    {
        _encoding = encoding ?? new UTF8Encoding(false);
    }

    /// <summary>
    ///     Indicates whether this loader can attempt to load the provided path.
    /// </summary>
    /// <param name="path">Logical or physical path to the content.</param>
    /// <returns>
    ///     Always true. Actual resolution and existence checks are performed by <see cref="ContentLoadContext" /> and its
    ///     providers.
    /// </returns>
    public override bool CanLoad(string path)
    {
        return true;
    }

    /// <summary>
    ///     Synchronously loads the entire content at <paramref name="path" /> as a string.
    /// </summary>
    /// <param name="context">The content load context that opens streams from registered providers.</param>
    /// <param name="path">Logical or physical path to the content.</param>
    /// <returns>The full text content.</returns>
    /// <exception cref="System.IO.IOException">Thrown if the underlying stream cannot be opened or read.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown if access is denied by the provider.</exception>
    public override string LoadTyped(ContentLoadContext context, string path)
    {
        // Open a readable stream from the content providers.
        using var s = context.OpenRead(path);
        // Wrap the stream in a StreamReader using the configured encoding; detectEncodingFromByteOrderMarks = true.
        using var sr = new StreamReader(s, _encoding, true);
        // Read entire content and return. Disposes reader and stream afterward.
        return sr.ReadToEnd();
    }

    /// <summary>
    ///     Asynchronously loads the entire content at <paramref name="path" /> as a string.
    /// </summary>
    /// <param name="context">The content load context that opens streams from registered providers.</param>
    /// <param name="path">Logical or physical path to the content.</param>
    /// <param name="ct">Cancellation token to cancel the read operation.</param>
    /// <returns>A task producing the full text content.</returns>
    /// <exception cref="System.IO.IOException">Thrown if the underlying stream cannot be opened or read.</exception>
    /// <exception cref="System.UnauthorizedAccessException">Thrown if access is denied by the provider.</exception>
    public override async ValueTask<string> LoadTypedAsync(ContentLoadContext context, string path,
        CancellationToken ct)
    {
        // Open the stream; dispose asynchronously when complete.
        await using var s = context.OpenRead(path);
        // Use the same encoding configuration as sync path.
        using var sr = new StreamReader(s, _encoding, true);
        // Read entire content with cancellation support; ConfigureAwait(false) for library-friendly usage.
        return await sr.ReadToEndAsync(ct).ConfigureAwait(false);
    }
}