namespace Brine2D.Rendering;

/// <summary>
/// Texture scaling/filtering mode.
/// </summary>
public enum TextureScaleMode
{
    /// <summary>
    /// Linear filtering (smooth scaling) - default for photos/smooth graphics.
    /// </summary>
    Linear,

    /// <summary>
    /// Nearest neighbor filtering (pixel-perfect scaling) - best for pixel art.
    /// </summary>
    Nearest
}