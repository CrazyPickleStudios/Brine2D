namespace Brine2D.SDL.Graphics.Text;

/// <summary>
///     Represents a set of OpenType shaping features that can be enabled or disabled
///     when shaping text for rendering.
/// </summary>
/// <remarks>
///     This struct is intentionally non-readonly to allow future dynamic toggling of
///     individual features at runtime. It could be converted to an immutable form if
///     desired by making the struct readonly and all fields readonly and initializing
///     via the constructor.
/// </remarks>
internal struct ShapingFeatures
{
    /// <summary>
    ///     Enable standard and discretionary ligature formation (for example "fi", "fl").
    /// </summary>
    public bool Ligatures;

    /// <summary>
    ///     Enable kerning adjustments between glyphs where the font provides kerning info.
    /// </summary>
    public bool Kerning;

    /// <summary>
    ///     Enable contextual alternates — substitution of glyphs depending on surrounding context.
    /// </summary>
    public bool ContextualAlternates;

    /// <summary>
    ///     Enable mark positioning (positioning of combining marks relative to base glyphs).
    /// </summary>
    public bool MarkPositioning;

    /// <summary>
    ///     Enable general glyph substitution features (GSUB) used by the shaper.
    /// </summary>
    public bool GlyphSubstitution;

    /// <summary>
    ///     Initializes a new instance of <see cref="ShapingFeatures" />.
    /// </summary>
    /// <param name="ligatures">Enable ligature formation.</param>
    /// <param name="kerning">Enable kerning adjustments.</param>
    /// <param name="contextualAlternates">Enable contextual alternates.</param>
    /// <param name="markPositioning">Enable mark positioning.</param>
    /// <param name="glyphSubstitution">Enable glyph substitution (GSUB).</param>
    public ShapingFeatures(
        bool ligatures,
        bool kerning,
        bool contextualAlternates,
        bool markPositioning,
        bool glyphSubstitution)
    {
        Ligatures = ligatures;
        Kerning = kerning;
        ContextualAlternates = contextualAlternates;
        MarkPositioning = markPositioning;
        GlyphSubstitution = glyphSubstitution;
    }

    /// <summary>
    ///     The default set of shaping features enabled by the renderer.
    ///     All features are enabled by default to maximize typographic correctness.
    /// </summary>
    public static ShapingFeatures Default => new(
        true,
        true,
        true,
        true,
        true);
}