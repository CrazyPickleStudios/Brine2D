using System.Text.Json;

namespace Brine2D.Core.Content.Loaders;

/// <summary>
///     Asset loader that deserializes JSON assets into <typeparamref name="T" /> using System.Text.Json.
/// </summary>
/// <typeparam name="T">The target type to deserialize into. Must be non-nullable.</typeparam>
/// <remarks>
///     If no <see cref="JsonSerializerOptions" /> are provided, this loader uses sensible defaults:
///     case-insensitive property names, comment skipping, and allowing trailing commas.
/// </remarks>
public sealed class JsonLoader<T> : AssetLoader<T>
    where T : notnull
{
    /// <summary>
    ///     Serializer options used for all JSON (de)serialization performed by this loader.
    /// </summary>
    private readonly JsonSerializerOptions _options;

    /// <summary>
    ///     Creates a new <see cref="JsonLoader{T}" /> with optional serializer options.
    /// </summary>
    /// <param name="options">
    ///     Custom <see cref="JsonSerializerOptions" /> to use. If <c>null</c>, defaults are used:
    ///     <list type="bullet">
    ///         <item>PropertyNameCaseInsensitive = true</item>
    ///         <item>ReadCommentHandling = JsonCommentHandling.Skip</item>
    ///         <item>AllowTrailingCommas = true</item>
    ///     </list>
    /// </param>
    public JsonLoader(JsonSerializerOptions? options = null)
    {
        _options = options ?? new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            ReadCommentHandling = JsonCommentHandling.Skip,
            AllowTrailingCommas = true
        };
    }

    /// <summary>
    ///     Determines whether this loader can handle the specified path based on its extension.
    /// </summary>
    /// <param name="path">The asset path to inspect.</param>
    /// <returns><c>true</c> if the path ends with <c>.json</c> (case-insensitive); otherwise, <c>false</c>.</returns>
    public override bool CanLoad(string path)
    {
        return path.EndsWith(".json", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    ///     Synchronously loads and deserializes the JSON asset at the given path into <typeparamref name="T" />.
    /// </summary>
    /// <param name="context">The content loading context providing file access.</param>
    /// <param name="path">The path to the JSON asset.</param>
    /// <returns>The deserialized <typeparamref name="T" /> instance.</returns>
    /// <exception cref="JsonException">Thrown when the JSON is invalid for the target type.</exception>
    /// <exception cref="NotSupportedException">Thrown when the content type is not supported by the serializer.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs while reading the stream.</exception>
    public override T LoadTyped(ContentLoadContext context, string path)
    {
        // Open the asset stream and deserialize synchronously.
        using var s = context.OpenRead(path);
        return JsonSerializer.Deserialize<T>(s, _options)!;
    }

    /// <summary>
    ///     Asynchronously loads and deserializes the JSON asset at the given path into <typeparamref name="T" />.
    /// </summary>
    /// <param name="context">The content loading context providing file access.</param>
    /// <param name="path">The path to the JSON asset.</param>
    /// <param name="ct">A token to observe while waiting for the operation to complete.</param>
    /// <returns>A task that resolves to the deserialized <typeparamref name="T" /> instance.</returns>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled via <paramref name="ct" />.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid for the target type.</exception>
    /// <exception cref="NotSupportedException">Thrown when the content type is not supported by the serializer.</exception>
    /// <exception cref="IOException">Thrown when an I/O error occurs while reading the stream.</exception>
    public override async ValueTask<T> LoadTypedAsync(ContentLoadContext context, string path, CancellationToken ct)
    {
        // Open the asset stream and deserialize asynchronously, observing cancellation.
        await using var s = context.OpenRead(path);
        return (await JsonSerializer.DeserializeAsync<T>(s, _options, ct).ConfigureAwait(false))!;
    }
}