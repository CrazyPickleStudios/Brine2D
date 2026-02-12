namespace Brine2D.Rendering.Text;

/// <summary>
/// Horizontal text alignment.
/// </summary>
public enum TextAlignment
{
    /// <summary>
    /// Align text to the left edge.
    /// </summary>
    Left,
    
    /// <summary>
    /// Center text horizontally.
    /// </summary>
    Center,
    
    /// <summary>
    /// Align text to the right edge.
    /// </summary>
    Right,
    
    /// <summary>
    /// Justify text (stretch to fill width). Not yet implemented.
    /// </summary>
    Justify
}

/// <summary>
/// Vertical text alignment.
/// </summary>
public enum VerticalAlignment
{
    /// <summary>
    /// Align to the top edge.
    /// </summary>
    Top,
    
    /// <summary>
    /// Center vertically.
    /// </summary>
    Middle,
    
    /// <summary>
    /// Align to the bottom edge.
    /// </summary>
    Bottom
}