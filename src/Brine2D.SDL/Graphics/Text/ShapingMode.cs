namespace Brine2D.SDL.Graphics.Text;

/// <summary>
///     Specifies which shaping algorithm to use when laying out and positioning glyphs
///     for rendered text.
/// </summary>
/// <remarks>
///     Shaping is the process of selecting and positioning glyphs for a run of text.
///     Choosing the appropriate shaping mode affects script support (e.g., Latin vs.
///     Arabic/Indic), ligature handling, and glyph positioning accuracy.
/// </remarks>
internal enum ShapingMode
{
    /// <summary>
    ///     Simple shaping mode.
    ///     Use a minimal, fast shaping strategy suitable for basic Latin and non-complex scripts.
    ///     This mode may not handle complex script features such as contextual shaping,
    ///     reordering, or advanced ligatures correctly.
    /// </summary>
    Simple,

    /// <summary>
    ///     Advanced shaping mode.
    ///     Use a full-featured shaping engine (for example, HarfBuzz) that supports complex
    ///     scripts, contextual substitutions, reordering, and precise glyph positioning.
    ///     This mode is more accurate for international text but may be slower or require
    ///     additional native dependencies.
    /// </summary>
    Advanced
}