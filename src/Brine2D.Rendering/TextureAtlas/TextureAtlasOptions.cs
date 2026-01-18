namespace Brine2D.Rendering.TextureAtlas;

/// <summary>
/// Configuration options for texture atlasing.
/// Part of the ASP.NET-style options pattern.
/// </summary>
public class TextureAtlasOptions
{
    /// <summary>
    /// Maximum width for generated atlases (default: 2048).
    /// </summary>
    public int MaxAtlasWidth { get; set; } = 2048;

    /// <summary>
    /// Maximum height for generated atlases (default: 2048).
    /// </summary>
    public int MaxAtlasHeight { get; set; } = 2048;

    /// <summary>
    /// Padding between sprites in pixels (default: 2).
    /// Prevents texture bleeding with bilinear filtering.
    /// </summary>
    public int Padding { get; set; } = 2;

    /// <summary>
    /// Whether to use power-of-two dimensions (default: true).
    /// </summary>
    public bool UsePowerOfTwo { get; set; } = true;

    /// <summary>
    /// Default texture scale mode for atlases (default: Nearest).
    /// </summary>
    public TextureScaleMode DefaultScaleMode { get; set; } = TextureScaleMode.Nearest;
}