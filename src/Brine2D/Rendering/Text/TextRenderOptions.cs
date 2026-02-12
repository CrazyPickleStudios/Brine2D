using System.Numerics;
using Brine2D.Core;

namespace Brine2D.Rendering.Text;

/// <summary>
/// Options for text rendering with rich formatting support.
/// </summary>
public sealed class TextRenderOptions
{
    /// <summary>
    /// Default color if not specified in markup.
    /// </summary>
    public Color Color { get; init; } = Color.White;
    
    /// <summary>
    /// Default font (null = use renderer's default font).
    /// </summary>
    public IFont? Font { get; init; }
    
    /// <summary>
    /// Base font size in points.
    /// </summary>
    public float FontSize { get; init; } = 16f;
    
    /// <summary>
    /// Maximum width before wrapping (null = no wrap).
    /// </summary>
    public float? MaxWidth { get; init; }
    
    /// <summary>
    /// Horizontal alignment within the max width.
    /// </summary>
    public TextAlignment HorizontalAlign { get; init; } = TextAlignment.Left;
    
    /// <summary>
    /// Vertical alignment within a bounding box (requires MaxHeight).
    /// </summary>
    public VerticalAlignment VerticalAlign { get; init; } = VerticalAlignment.Top;
    
    /// <summary>
    /// Maximum height for vertical alignment (null = no constraint).
    /// </summary>
    public float? MaxHeight { get; init; }
    
    /// <summary>
    /// Line spacing multiplier (1.0 = normal, 1.5 = 150% spacing).
    /// </summary>
    public float LineSpacing { get; init; } = 1.2f;
    
    /// <summary>
    /// Whether to parse markup tags.
    /// If false, text is rendered as-is (including tags).
    /// </summary>
    public bool ParseMarkup { get; init; } = false;
    
    /// <summary>
    /// The markup parser to use when ParseMarkup is true.
    /// If null, a default BBCode parser will be used.
    /// </summary>
    public IMarkupParser? MarkupParser { get; init; }
    
    /// <summary>
    /// Shadow offset (null = no shadow).
    /// </summary>
    public Vector2? ShadowOffset { get; init; }
    
    /// <summary>
    /// Shadow color (only used if ShadowOffset is set).
    /// </summary>
    public Color ShadowColor { get; init; } = new Color(0, 0, 0, 128);
    
    /// <summary>
    /// Outline thickness in pixels (0 = no outline).
    /// </summary>
    public float OutlineThickness { get; init; } = 0f;
    
    /// <summary>
    /// Outline color (only used if OutlineThickness > 0).
    /// </summary>
    public Color OutlineColor { get; init; } = Color.Black;
}