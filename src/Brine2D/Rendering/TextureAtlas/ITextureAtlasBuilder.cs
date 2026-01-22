namespace Brine2D.Rendering.TextureAtlas;

/// <summary>
/// Builder for creating texture atlases from individual textures.
/// Follows ASP.NET's fluent builder pattern for a familiar developer experience.
/// Automatically splits into multiple atlases if textures don't fit in one atlas.
/// </summary>
public interface ITextureAtlasBuilder
{
    /// <summary>
    /// Sets the name for the atlas being built.
    /// </summary>
    /// <param name="name">Atlas name.</param>
    ITextureAtlasBuilder WithName(string name);

    /// <summary>
    /// Adds a texture to be packed into the atlas.
    /// </summary>
    /// <param name="path">Path to the texture file.</param>
    /// <param name="name">Optional custom name (defaults to filename without extension).</param>
    ITextureAtlasBuilder AddTexture(string path, string? name = null);

    /// <summary>
    /// Adds all textures from a folder to be packed into the atlas.
    /// </summary>
    /// <param name="folderPath">Path to the folder containing textures.</param>
    /// <param name="pattern">Optional file pattern (e.g., "*.png").</param>
    /// <param name="recursive">Whether to search subfolders.</param>
    ITextureAtlasBuilder AddFolder(string folderPath, string pattern = "*.png", bool recursive = false);

    /// <summary>
    /// Sets the maximum size for each atlas texture.
    /// </summary>
    /// <param name="maxWidth">Maximum width in pixels (default: 2048).</param>
    /// <param name="maxHeight">Maximum height in pixels (default: 2048).</param>
    ITextureAtlasBuilder WithMaxSize(int maxWidth, int maxHeight);

    /// <summary>
    /// Sets the padding between sprites in the atlas.
    /// Padding prevents texture bleeding when using bilinear filtering.
    /// </summary>
    /// <param name="padding">Padding in pixels (default: 2).</param>
    ITextureAtlasBuilder WithPadding(int padding);

    /// <summary>
    /// Sets whether to use power-of-two dimensions for the atlas.
    /// Power-of-two textures are more compatible with older GPUs.
    /// </summary>
    /// <param name="usePowerOfTwo">True to use power-of-two dimensions (default: true).</param>
    ITextureAtlasBuilder WithPowerOfTwo(bool usePowerOfTwo = true);

    /// <summary>
    /// Sets the texture scale mode for the atlas.
    /// </summary>
    /// <param name="scaleMode">Scale mode (Nearest or Linear).</param>
    ITextureAtlasBuilder WithScaleMode(TextureScaleMode scaleMode);

    /// <summary>
    /// Builds the texture atlas collection asynchronously.
    /// Loads all added textures, packs them, and creates atlas(es).
    /// Automatically creates multiple atlases if textures don't fit in one.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created texture atlas collection.</returns>
    Task<ITextureAtlasCollection> BuildAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Clears all added textures and resets builder state.
    /// </summary>
    ITextureAtlasBuilder Clear();
}