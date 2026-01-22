namespace Brine2D.Rendering;

/// <summary>
/// Provides context for creating textures specific to the renderer implementation.
/// Similar to ASP.NET's HttpContext or DbContext pattern - abstracts away implementation details.
/// </summary>
public interface ITextureContext
{
    /// <summary>
    /// Creates a texture from an SDL surface.
    /// </summary>
    /// <param name="surface">The SDL surface handle containing image data.</param>
    /// <param name="width">Width of the texture.</param>
    /// <param name="height">Height of the texture.</param>
    /// <param name="scaleMode">Texture scaling mode.</param>
    /// <returns>A new texture instance.</returns>
    ITexture CreateTextureFromSurface(nint surface, int width, int height, TextureScaleMode scaleMode);
    
    /// <summary>
    /// Creates a blank texture for rendering targets.
    /// </summary>
    /// <param name="width">Width in pixels.</param>
    /// <param name="height">Height in pixels.</param>
    /// <param name="scaleMode">Texture scaling mode.</param>
    /// <returns>A new blank texture instance.</returns>
    ITexture CreateBlankTexture(int width, int height, TextureScaleMode scaleMode);
    
    /// <summary>
    /// Releases a texture's GPU resources.
    /// </summary>
    /// <param name="texture">The texture to release.</param>
    void ReleaseTexture(ITexture texture);
}