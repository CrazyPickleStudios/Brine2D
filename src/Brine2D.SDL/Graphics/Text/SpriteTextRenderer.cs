using System;
using Brine2D.Core.Graphics;
using Brine2D.Core.Graphics.Text;
using Brine2D.Core.Math;

namespace Brine2D.SDL.Graphics.Text;

/// <summary>
///     Thin helper that routes text-related operations through an <see cref="ISpriteRenderer"/>.
///     This class centralizes font measuring, drawing, and pre-warming operations while
///     keeping call-sites free of direct sprite renderer usage.
/// </summary>
internal sealed class SpriteTextRenderer
{
    // Backing sprite renderer used for all font draw calls.
    private readonly ISpriteRenderer _sprites;

    /// <summary>
    ///     Initializes a new instance that will use the provided sprite renderer for drawing text.
    /// </summary>
    /// <param name="sprites">Sprite renderer used by font draw operations. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sprites"/> is null.</exception>
    public SpriteTextRenderer(ISpriteRenderer sprites) =>
        _sprites = sprites ?? throw new ArgumentNullException(nameof(sprites));

    /// <summary>
    ///     Measures an unwrapped multiline string using the provided font.
    /// </summary>
    /// <param name="font">Font used to measure the text. Cannot be null.</param>
    /// <param name="text">Text to measure (may be null or empty depending on font implementation).</param>
    /// <returns>Tuple containing measured width and height in pixels.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="font"/> is null.</exception>
    public (int width, int height) Measure(IFont font, string text) =>
        font?.Measure(text) ?? throw new ArgumentNullException(nameof(font));

    /// <summary>
    ///     Measures text when word-wrapping to a maximum width.
    /// </summary>
    /// <param name="font">Font used for measurement. Cannot be null.</param>
    /// <param name="text">Text to measure.</param>
    /// <param name="maxWidth">Maximum line width in pixels used to wrap lines.</param>
    /// <returns>Measured width and height in pixels after wrapping.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="font"/> is null.</exception>
    public (int width, int height) MeasureWrapped(IFont font, string text, int maxWidth) =>
        font?.MeasureWrapped(text, maxWidth) ?? throw new ArgumentNullException(nameof(font));

    /// <summary>
    ///     Draws unwrapped text at the specified screen/world position using the stored sprite renderer.
    /// </summary>
    /// <param name="font">Font instance used to draw. Cannot be null.</param>
    /// <param name="text">Text to draw.</param>
    /// <param name="x">Destination X coordinate (pixels or world units depending on sprite batch).</param>
    /// <param name="y">Destination Y coordinate (pixels or world units depending on sprite batch).</param>
    /// <param name="color">Tint color applied to glyphs.</param>
    /// <param name="scale">Scale multiplier applied to glyph geometry. Defaults to 1.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="font"/> is null.</exception>
    public void DrawString(IFont font, string text, int x, int y, Color color, float scale = 1f)
    {
        if (font == null) throw new ArgumentNullException(nameof(font));
        // Delegate actual drawing to the font implementation which knows glyph layout.
        font.DrawString(_sprites, text, x, y, color, scale);
    }

    /// <summary>
    ///     Draws wrapped text constrained to <paramref name="maxWidth"/> and horizontally aligned.
    /// </summary>
    /// <param name="font">Font instance used to draw. Cannot be null.</param>
    /// <param name="text">Text to draw.</param>
    /// <param name="x">Left/top origin for drawing (interpretation depends on sprite batch).</param>
    /// <param name="y">Top origin for drawing.</param>
    /// <param name="maxWidth">Maximum width in pixels for wrapping.</param>
    /// <param name="align">Horizontal alignment for wrapped lines.</param>
    /// <param name="color">Tint color applied to glyphs.</param>
    /// <param name="scale">Scale multiplier applied to glyph geometry. Defaults to 1.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="font"/> is null.</exception>
    public void DrawStringWrapped(IFont font, string text, int x, int y, int maxWidth, TextAlign align, Color color, float scale = 1f)
    {
        if (font == null) throw new ArgumentNullException(nameof(font));
        font.DrawStringWrapped(_sprites, text, x, y, maxWidth, align, color, scale);
    }

    /// <summary>
    ///     Draws text with a simple one-pass shadow: shadow is drawn first at an offset, then the main text.
    /// </summary>
    /// <param name="font">Font instance used to draw. Cannot be null.</param>
    /// <param name="text">Text to draw.</param>
    /// <param name="x">X coordinate for main text.</param>
    /// <param name="y">Y coordinate for main text.</param>
    /// <param name="color">Main text tint color.</param>
    /// <param name="shadowColor">Shadow tint color.</param>
    /// <param name="shadowDx">Horizontal shadow offset in pixels. Defaults to 1.</param>
    /// <param name="shadowDy">Vertical shadow offset in pixels. Defaults to 1.</param>
    /// <param name="scale">Scale multiplier applied to glyph geometry. Defaults to 1.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="font"/> is null.</exception>
    public void DrawStringShadow(IFont font, string text, int x, int y, Color color, Color shadowColor, int shadowDx = 1, int shadowDy = 1, float scale = 1f)
    {
        if (font == null) throw new ArgumentNullException(nameof(font));
        // Draw shadow first using the offset, then draw the main text on top.
        font.DrawString(_sprites, text, x + shadowDx, y + shadowDy, shadowColor, scale);
        font.DrawString(_sprites, text, x, y, color, scale);
    }

    /// <summary>
    ///     Draws wrapped text with a shadow by drawing the wrapped shadow first and then the main wrapped text.
    /// </summary>
    /// <param name="font">Font instance used to draw. Cannot be null.</param>
    /// <param name="text">Text to draw.</param>
    /// <param name="x">Left/top origin for drawing.</param>
    /// <param name="y">Top origin for drawing.</param>
    /// <param name="maxWidth">Maximum width in pixels for wrapping.</param>
    /// <param name="align">Horizontal alignment for wrapped lines.</param>
    /// <param name="color">Main text tint color.</param>
    /// <param name="shadowColor">Shadow tint color.</param>
    /// <param name="shadowDx">Horizontal shadow offset in pixels. Defaults to 1.</param>
    /// <param name="shadowDy">Vertical shadow offset in pixels. Defaults to 1.</param>
    /// <param name="scale">Scale multiplier applied to glyph geometry. Defaults to 1.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="font"/> is null.</exception>
    public void DrawStringWrappedShadow(IFont font, string text, int x, int y, int maxWidth, TextAlign align, Color color, Color shadowColor, int shadowDx = 1, int shadowDy = 1, float scale = 1f)
    {
        if (font == null) throw new ArgumentNullException(nameof(font));
        // Shadow version of wrapped drawing: draw the offset shadow first, then the main text.
        font.DrawStringWrapped(_sprites, text, x + shadowDx, y + shadowDy, maxWidth, align, shadowColor, scale);
        font.DrawStringWrapped(_sprites, text, x, y, maxWidth, align, color, scale);
    }

    /// <summary>
    ///     Requests the font implementation to pre-cache printable ASCII glyphs (32..126).
    ///     Useful to avoid hitches when first displaying common characters.
    /// </summary>
    /// <param name="font">Font instance to prewarm. Cannot be null.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="font"/> is null.</exception>
    public void PrewarmAscii(IFont font)
    {
        if (font == null) throw new ArgumentNullException(nameof(font));
        font.PrewarmAscii();
    }

    /// <summary>
    ///     Requests the font implementation to pre-cache glyphs used in the given text.
    ///     This can reduce runtime glyph uploads or atlas creation when showing new strings.
    /// </summary>
    /// <param name="font">Font instance to prewarm. Cannot be null.</param>
    /// <param name="text">Text whose glyphs should be pre-cached.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="font"/> is null.</exception>
    public void Prewarm(IFont font, string text)
    {
        if (font == null) throw new ArgumentNullException(nameof(font));
        font.Prewarm(text);
    }

    /// <summary>
    ///     Requests the font implementation to pre-cache glyphs in an inclusive Unicode scalar range.
    ///     Useful for preparing glyphs for a specific language or symbol set.
    /// </summary>
    /// <param name="font">Font instance to prewarm. Cannot be null.</param>
    /// <param name="startInclusive">Start of range (inclusive).</param>
    /// <param name="endInclusive">End of range (inclusive).</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="font"/> is null.</exception>
    public void PrewarmRange(IFont font, int startInclusive, int endInclusive)
    {
        if (font == null) throw new ArgumentNullException(nameof(font));
        font.PrewarmRange(startInclusive, endInclusive);
    }
}
