namespace Brine2D
{
    /// <summary>
    /// True Type hinting mode.
    /// </summary>
    // TODO: Requires Review
    public enum HintingMode
    {
        /// <summary>
        /// Default hinting. Should be preferred for typical antialiased fonts.
        /// </summary>
        Normal,
        /// <summary>
        /// Results in fuzzier text but can sometimes preserve the original glyph shapes of the text better than normal hinting.
        /// </summary>
        Light,
        /// <summary>
        /// Results in aliased / unsmoothed text with either full opacity or completely transparent pixels. Should be used when antialiasing is not desired for the font.
        /// </summary>
        Mono,
        /// <summary>
        /// Disables hinting for the font. Results in fuzzier text.
        /// </summary>
        None,
    }
}
