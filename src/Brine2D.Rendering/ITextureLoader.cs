namespace Brine2D.Rendering;

/// <summary>
/// Service for loading and managing textures.
/// </summary>
public interface ITextureLoader : IDisposable
{
    /// <summary>
    /// Loads a texture from a file path.
    /// </summary>
    /// <param name="path">Path to the image file.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The loaded texture.</returns>
    Task<ITexture> LoadTextureAsync(string path, CancellationToken cancellationToken = default);

    /// <summary>
    /// Loads a texture from a file path synchronously.
    /// </summary>
    /// <param name="path">Path to the image file.</param>
    /// <returns>The loaded texture.</returns>
    ITexture LoadTexture(string path);

    /// <summary>
    /// Creates a blank texture with the specified dimensions.
    /// </summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <returns>The created texture.</returns>
    ITexture CreateTexture(int width, int height);

    /// <summary>
    /// Unloads a texture and frees its resources.
    /// </summary>
    /// <param name="texture">The texture to unload.</param>
    void UnloadTexture(ITexture texture);
}