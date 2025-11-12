namespace Brine2D.SDL.Graphics.Text;

/// <summary>
///     Represents a single shaped glyph produced by the text shaping process.
///     A shaped glyph ties a Unicode codepoint (UTF-32) and shaping information
///     to a font glyph index and metrics used during layout and drawing.
/// </summary>
internal readonly struct ShapedGlyph
{
    /// <summary>
    ///     Unicode code point represented by this glyph (UTF-32).
    /// </summary>
    public readonly uint Codepoint;

    /// <summary>
    ///     Glyph index in the font's glyph table (font-specific identifier).
    /// </summary>
    public readonly int GlyphIndex;

    /// <summary>
    ///     Base advance for the glyph in font design units (unscaled).
    ///     This value is used to compute final pixel advance after scaling.
    /// </summary>
    public readonly int Advance;

    /// <summary>
    ///     Kerning adjustment (unscaled) to apply relative to the previous cluster.
    ///     Positive values move the glyph forward; negative values move it backward.
    /// </summary>
    public readonly int Kerning;

    /// <summary>
    ///     UTF-16 start index into the source string for the cluster that contains this glyph.
    /// </summary>
    public readonly int ClusterStart;

    /// <summary>
    ///     UTF-16 length (number of char units) of the cluster that contains this glyph.
    /// </summary>
    public readonly int ClusterLength;

    /// <summary>
    ///     True when this glyph was produced from a ligature substitution during shaping.
    /// </summary>
    public readonly bool IsLigature;

    /// <summary>
    ///     Script classification (for example Latin, Arabic) used during shaping.
    /// </summary>
    public readonly TextScript Script;

    /// <summary>
    ///     Directionality of this glyph (LeftToRight or RightToLeft) used for layout.
    /// </summary>
    public readonly TextDirection Direction;

    /// <summary>
    ///     Owning font that produced this glyph. Holds font metrics and glyph bitmaps.
    /// </summary>
    public readonly SpriteFont Font;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ShapedGlyph" /> struct.
    /// </summary>
    /// <param name="codepoint">Unicode code point (UTF-32) represented by this glyph.</param>
    /// <param name="glyphIndex">Index of the glyph within the font's glyph table.</param>
    /// <param name="advance">Base advance (unscaled) in font design units.</param>
    /// <param name="kerning">Kerning adjustment relative to the previous cluster (unscaled).</param>
    /// <param name="clusterStart">UTF-16 start index of the source cluster for this glyph.</param>
    /// <param name="clusterLength">UTF-16 length (in char units) of the source cluster.</param>
    /// <param name="isLigature">True when this glyph originates from a ligature substitution.</param>
    /// <param name="script">Script classification used for shaping.</param>
    /// <param name="direction">Directionality used for shaping and layout.</param>
    /// <param name="font">Owning <see cref="SpriteFont" /> that produced this glyph.</param>
    public ShapedGlyph
    (
        uint codepoint,
        int glyphIndex,
        int advance,
        int kerning,
        int clusterStart,
        int clusterLength,
        bool isLigature,
        TextScript script,
        TextDirection direction,
        SpriteFont font
    )
    {
        Codepoint = codepoint;
        GlyphIndex = glyphIndex;
        Advance = advance;
        Kerning = kerning;
        ClusterStart = clusterStart;
        ClusterLength = clusterLength;
        IsLigature = isLigature;
        Script = script;
        Direction = direction;
        Font = font;
    }
}