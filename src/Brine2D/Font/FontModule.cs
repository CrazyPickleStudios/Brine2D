using System;

namespace Brine2D.Font;

// TODO: Needs review
public sealed class FontModule
{
/// <summary>
        /// <para>Creates a new BMFont Rasterizer.</para>
        /// </summary>
        /// <param name="imageData">The image data containing the drawable pictures of font glyphs.</param>
        /// <param name="glyphs">The sequence of glyphs in the ImageData.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rasterizer</term><description>The rasterizer.</description></item>
        /// </list>
        /// </returns>
    public object NewBMFontRasterizer(object imageData, string glyphs) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new BMFont Rasterizer.</para>
        /// </summary>
        /// <param name="fileName">The path to file containing the drawable pictures of font glyphs.</param>
        /// <param name="glyphs">The sequence of glyphs in the ImageData.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rasterizer</term><description>The rasterizer.</description></item>
        /// </list>
        /// </returns>
    public object NewBMFontRasterizer(string fileName, string glyphs) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new FontData.</para>
        /// </summary>
        /// <param name="rasterizer">The Rasterizer containing the font.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>fontData</term><description>The FontData.</description></item>
        /// </list>
        /// </returns>
    public object NewFontData(object rasterizer) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new GlyphData.</para>
        /// </summary>
        /// <param name="rasterizer">The Rasterizer containing the font.</param>
        /// <param name="glyph">The character code of the glyph.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>glyphData</term><description>The GlyphData.</description></item>
        /// </list>
        /// </returns>
    public object NewGlyphData(object rasterizer, double glyph) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new Image Rasterizer.</para>
        /// </summary>
        /// <param name="imageData">Font image data.</param>
        /// <param name="glyphs">String containing font glyphs.</param>
        /// <param name="extraSpacing">Font extra spacing.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rasterizer</term><description>The rasterizer.</description></item>
        /// </list>
        /// </returns>
    public object NewImageRasterizer(object imageData, string glyphs, double extraSpacing = 0) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new Rasterizer.</para>
        /// </summary>
        /// <param name="filename">The font file.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rasterizer</term><description>The rasterizer.</description></item>
        /// </list>
        /// </returns>
    public object NewRasterizer(string filename) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new Rasterizer.</para>
        /// </summary>
        /// <param name="data">The FileData of the font file.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rasterizer</term><description>The rasterizer.</description></item>
        /// </list>
        /// </returns>
    public object NewRasterizer(object data) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new Rasterizer.</para>
        /// </summary>
        /// <param name="size">The font size.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rasterizer</term><description>The rasterizer.</description></item>
        /// </list>
        /// </returns>
    public object NewRasterizer(double size = 12) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new Rasterizer.</para>
        /// </summary>
        /// <param name="fileName">Path to font file.</param>
        /// <param name="size">The font size.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rasterizer</term><description>The rasterizer.</description></item>
        /// </list>
        /// </returns>
    public object NewRasterizer(string fileName, double size = 12) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new Rasterizer.</para>
        /// </summary>
        /// <param name="fileData">File data containing font.</param>
        /// <param name="size">The font size.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rasterizer</term><description>The rasterizer.</description></item>
        /// </list>
        /// </returns>
    public object NewRasterizer(object fileData, double size = 12) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new Rasterizer.</para>
        /// </summary>
        /// <param name="imageData">The image data containing the drawable pictures of font glyphs.</param>
        /// <param name="glyphs">The sequence of glyphs in the ImageData.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rasterizer</term><description>The rasterizer.</description></item>
        /// </list>
        /// </returns>
    public object NewRasterizer(object imageData, string glyphs) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new Rasterizer.</para>
        /// </summary>
        /// <param name="fileName">The path to file containing the drawable pictures of font glyphs.</param>
        /// <param name="glyphs">The sequence of glyphs in the ImageData.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rasterizer</term><description>The rasterizer.</description></item>
        /// </list>
        /// </returns>
    public object NewRasterizer(string fileName, string glyphs) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new TrueType Rasterizer.</para>
        /// </summary>
        /// <param name="size">The font size.</param>
        /// <param name="hinting">True Type hinting mode.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rasterizer</term><description>The rasterizer.</description></item>
        /// </list>
        /// </returns>
    public object NewTrueTypeRasterizer(double size = 12, HintingMode hinting = HintingMode.Normal) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new TrueType Rasterizer.</para>
        /// </summary>
        /// <param name="fileName">Path to font file.</param>
        /// <param name="size">The font size.</param>
        /// <param name="hinting">True Type hinting mode.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rasterizer</term><description>The rasterizer.</description></item>
        /// </list>
        /// </returns>
    public object NewTrueTypeRasterizer(string fileName, double size = 12, HintingMode hinting = HintingMode.Normal) => throw new NotImplementedException();

/// <summary>
        /// <para>Creates a new TrueType Rasterizer.</para>
        /// </summary>
        /// <param name="fileData">File data containing font.</param>
        /// <param name="size">The font size.</param>
        /// <param name="hinting">True Type hinting mode.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>rasterizer</term><description>The rasterizer.</description></item>
        /// </list>
        /// </returns>
    public object NewTrueTypeRasterizer(object fileData, double size = 12, HintingMode hinting = HintingMode.Normal) => throw new NotImplementedException();

}
