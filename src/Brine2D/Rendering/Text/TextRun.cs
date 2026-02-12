using Brine2D.Core;

namespace Brine2D.Rendering.Text;

/// <summary>
/// Represents a contiguous span of text with uniform styling.
/// </summary>
public sealed class TextRun
{
    public required string Text { get; init; }
    public Color Color { get; init; } = Color.White;
    public IFont? Font { get; init; }
    public float FontSize { get; init; } = 16f;
    public TextStyle Style { get; init; } = TextStyle.Normal;
    
    /// <summary>
    /// Start index in the original markup string.
    /// </summary>
    public int SourceIndex { get; init; }
}

/// <summary>
/// Text styling flags (can be combined).
/// </summary>
[Flags]
public enum TextStyle
{
    Normal = 0,
    Bold = 1 << 0,
    Italic = 1 << 1,
    Underline = 1 << 2,
    Strikethrough = 1 << 3
}