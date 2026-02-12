namespace Brine2D.Rendering;

/// <summary>
/// Low-level texture loading interface.
/// <para>
/// <strong>For most use cases, use <see cref="IAssetLoader"/> instead!</strong>
/// </para>
/// <para>
/// This interface is for advanced scenarios like:
/// - Custom texture loading implementations
/// - Framework/system-level texture management
/// - Direct texture operations without caching
/// </para>
/// </summary>
/// <remarks>
/// <strong>Prefer IAssetLoader for scene asset loading</strong> - it provides:
/// - Automatic caching
/// - Progress tracking
/// - Parallel loading
/// - Thread-safe operations
/// </remarks>
public interface ITextureLoader : IDisposable
{
    /// <summary>
    /// Loads a texture from a file path asynchronously.
    /// </summary>
    /// <remarks>
    /// Consider using <see cref="IAssetLoader.LoadTextureAsync"/> instead for automatic caching.
    /// </remarks>
    Task<ITexture> LoadTextureAsync(string path, TextureScaleMode scaleMode = TextureScaleMode.Linear, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a texture from a file path synchronously.
    /// </summary>
    ITexture LoadTexture(string path, TextureScaleMode scaleMode = TextureScaleMode.Linear);

    /// <summary>
    /// Creates a blank texture with the specified dimensions.
    /// </summary>
    ITexture CreateTexture(int width, int height, TextureScaleMode scaleMode = TextureScaleMode.Linear);

    /// <summary>
    /// Unloads a texture and frees its resources.
    /// </summary>
    void UnloadTexture(ITexture texture);
}