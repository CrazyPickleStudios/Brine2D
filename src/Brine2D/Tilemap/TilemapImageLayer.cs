using Brine2D.Core;

namespace Brine2D.Tilemap;

/// <summary>
/// Represents an image layer from a Tiled map — a full-image background or foreground layer
/// that is not composed of tiles.
/// </summary>
/// <remarks>
/// Image layers are not rendered automatically by <c>TilemapRenderer</c>. The intended usage
/// is to read <see cref="ImagePath"/> and load it manually as a sprite or background, applying
/// the layer's <see cref="Opacity"/>, <see cref="TintColor"/>, and parallax properties yourself.
/// </remarks>
public class TilemapImageLayer
{
    /// <summary>
    /// Layer name as set in Tiled.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Absolute path to the image file referenced by this layer.
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Rendering order relative to other layers (lower renders first/behind).
    /// Shares the same counter as tile layers so document order is preserved.
    /// </summary>
    public byte ZOrder { get; set; }

    /// <summary>
    /// Whether this layer is visible.
    /// </summary>
    public bool Visible { get; set; } = true;

    /// <summary>
    /// Layer opacity (0.0 = invisible, 1.0 = fully visible).
    /// </summary>
    public float Opacity { get; set; } = 1.0f;

    /// <summary>
    /// Pixel offset applied to the layer horizontally (Tiled offsetx).
    /// </summary>
    public float OffsetX { get; set; }

    /// <summary>
    /// Pixel offset applied to the layer vertically (Tiled offsety).
    /// </summary>
    public float OffsetY { get; set; }

    /// <summary>
    /// Parallax scroll multiplier on the X axis (Tiled parallaxx). Default is 1.0.
    /// </summary>
    public float ParallaxX { get; set; } = 1.0f;

    /// <summary>
    /// Parallax scroll multiplier on the Y axis (Tiled parallaxy). Default is 1.0.
    /// </summary>
    public float ParallaxY { get; set; } = 1.0f;

    /// <summary>
    /// Tint color multiplied with the image when rendering (Tiled tintcolor).
    /// Defaults to <see cref="Color.White"/> (no tint).
    /// </summary>
    public Color TintColor { get; set; } = Color.White;

    /// <summary>
    /// Custom properties defined on this layer in Tiled.
    /// </summary>
    public Dictionary<string, string> Properties { get; set; } = new();
}
