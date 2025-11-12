using Brine2D.Core.Graphics;
using Brine2D.Core.Math;

namespace Brine2D.Core.Graphics.Text;

/// <summary>
/// Public text rendering abstraction. Implementations may be dynamic (atlas) or static (bitmap).
/// </summary>
public interface IFont
{
    /// <summary>Unscaled base line height in pixels.</summary>
    int LineHeight { get; }

    /// <summary>Extra pixels added to each line (can be negative, clamped to keep final height >= 1).</summary>
    int ExtraLineSpacing { get; set; }

    /// <summary>Number of spaces a tab represents for advance calculations.</summary>
    int TabSpaces { get; set; }

    /// <summary>Maximum number of atlas pages allowed (0 = unlimited). Hint only; implementations may ignore.</summary>
    int MaxPages { get; set; }

    /// <summary>Measure unwrapped multiline text.</summary>
    (int width, int height) Measure(string text);

    /// <summary>Measure text with word wrapping given a maximum width (pixels).</summary>
    (int width, int height) MeasureWrapped(string text, int maxWidth);

    /// <summary>Draw unwrapped text at (x,y).</summary>
    void DrawString(ISpriteRenderer sprites, string text, int x, int y, Color color, float scale = 1f);

    /// <summary>Draw wrapped text at (x,y) constrained to maxWidth with horizontal alignment.</summary>
    void DrawStringWrapped(ISpriteRenderer sprites, string text, int x, int y, int maxWidth, TextAlign align, Color color, float scale = 1f);

    /// <summary>Pre-cache printable ASCII (32..126) glyphs.</summary>
    void PrewarmAscii();

    /// <summary>Pre-cache glyphs used in the provided text.</summary>
    void Prewarm(string text);

    /// <summary>Pre-cache glyphs in the inclusive Unicode scalar range.</summary>
    void PrewarmRange(int startInclusive, int endInclusive);

    /// <summary>Clears all cached glyphs/atlases (implementation-specific).</summary>
    void ClearCache();
}