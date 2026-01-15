namespace Brine2D.Rendering.SDL;

/// <summary>
/// Contains metrics and texture coordinates for a single glyph in a font atlas.
/// </summary>
public struct FontGlyph
{
    /// <summary>
    /// Character this glyph represents.
    /// </summary>
    public char Character { get; set; }
    
    /// <summary>
    /// X coordinate in the atlas texture (pixels).
    /// </summary>
    public int AtlasX { get; set; }
    
    /// <summary>
    /// Y coordinate in the atlas texture (pixels).
    /// </summary>
    public int AtlasY { get; set; }
    
    /// <summary>
    /// Width of the glyph in the atlas (pixels).
    /// </summary>
    public int Width { get; set; }
    
    /// <summary>
    /// Height of the glyph in the atlas (pixels).
    /// </summary>
    public int Height { get; set; }
    
    /// <summary>
    /// Horizontal offset from cursor position to left of glyph (pixels).
    /// </summary>
    public int BearingX { get; set; }
    
    /// <summary>
    /// Vertical offset from baseline to top of glyph (pixels).
    /// </summary>
    public int BearingY { get; set; }
    
    /// <summary>
    /// Horizontal distance to advance to next character (pixels).
    /// </summary>
    public int Advance { get; set; }
}