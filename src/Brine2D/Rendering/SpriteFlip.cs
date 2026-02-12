namespace Brine2D.Rendering;

/// <summary>
/// Flags for flipping sprites horizontally and/or vertically.
/// </summary>
[Flags]
public enum SpriteFlip
{
    /// <summary>
    /// No flipping (default).
    /// </summary>
    None = 0,
    
    /// <summary>
    /// Flip horizontally (mirror left-right).
    /// </summary>
    Horizontal = 1,
    
    /// <summary>
    /// Flip vertically (mirror top-bottom).
    /// </summary>
    Vertical = 2,
    
    /// <summary>
    /// Flip both horizontally and vertically (180° rotation equivalent).
    /// </summary>
    Both = Horizontal | Vertical
}