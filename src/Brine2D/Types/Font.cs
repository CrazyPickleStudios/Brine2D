namespace Brine2D
{
    /// <summary>
    /// <para>Defines the shape of characters that can be drawn onto the screen.</para>
    /// </summary>
    // TODO: Requires Review
    public class Font
    {
        /// <summary>
        /// <para>Gets the ascent of the Font.</para>
        /// <para>The ascent spans the distance between the baseline and the top of the glyph that reaches farthest from the baseline.</para>
        /// </summary>
        /// <param name="ascent">The ascent of the Font in pixels.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>ascent</term><description>The ascent of the Font in pixels.</description></item>
        /// </list>
        /// </returns>
        public double GetAscent(double ascent) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the baseline of the Font.</para>
        /// <para>Most scripts share the notion of a baseline: an imaginary horizontal line on which characters rest. In some scripts, parts of glyphs lie below the baseline.</para>
        /// </summary>
        /// <param name="baseline">The baseline of the Font in pixels.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>baseline</term><description>The baseline of the Font in pixels.</description></item>
        /// </list>
        /// </returns>
        public double GetBaseline(double baseline) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the DPI scale factor of the Font.</para>
        /// <para>The DPI scale factor represents relative pixel density. A DPI scale factor of 2 means the font's glyphs have twice the pixel density in each dimension (4 times as many pixels in the same area) compared to a font with a DPI scale factor of 1.</para>
        /// <para>The font size of TrueType fonts is scaled internally by the font's specified DPI scale factor. By default, LÖVE uses the screen's DPI scale factor when creating TrueType fonts.</para>
        /// </summary>
        /// <param name="dpiscale">The DPI scale factor of the Font.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>dpiscale</term><description>The DPI scale factor of the Font.</description></item>
        /// </list>
        /// </returns>
        public double GetDPIScale(double dpiscale) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the descent of the Font.</para>
        /// <para>The descent spans the distance between the baseline and the lowest descending glyph in a typeface.</para>
        /// </summary>
        /// <param name="descent">The descent of the Font in pixels.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>descent</term><description>The descent of the Font in pixels.</description></item>
        /// </list>
        /// </returns>
        public double GetDescent(double descent) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the filter mode for a font.</para>
        /// </summary>
        /// <param name="min">Filter mode used when minifying the font.</param>
        /// <param name="mag">Filter mode used when magnifying the font.</param>
        /// <param name="anisotropy">Maximum amount of anisotropic filtering used.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>min</term><description>Filter mode used when minifying the font.</description></item>
        /// <item><term>mag</term><description>Filter mode used when magnifying the font.</description></item>
        /// <item><term>anisotropy</term><description>Maximum amount of anisotropic filtering used.</description></item>
        /// </list>
        /// </returns>
        public (FilterMode min, FilterMode mag, double anisotropy) GetFilter(FilterMode min, FilterMode mag, double anisotropy) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the height of the Font.</para>
        /// <para>The height of the font is the size including any spacing needed for a single line.</para>
        /// </summary>
        /// <param name="height">The height of the Font in pixels.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>height</term><description>The height of the Font in pixels.</description></item>
        /// </list>
        /// </returns>
        public double GetHeight(double height) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the kerning between two characters in the Font.</para>
        /// <para>Kerning is normally handled automatically in love.graphics.print, Text objects, Font:getWidth, Font:getWrap, etc. This function is useful when stitching text together manually.</para>
        /// </summary>
        /// <param name="leftchar">The left character.</param>
        /// <param name="rightchar">The right character.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>kerning</term><description>The kerning amount to add to the spacing between the two characters. May be negative.</description></item>
        /// </list>
        /// </returns>
        public double GetKerning(string leftchar, string rightchar) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the kerning between two characters in the Font.</para>
        /// <para>Kerning is normally handled automatically in love.graphics.print, Text objects, Font:getWidth, Font:getWrap, etc. This function is useful when stitching text together manually.</para>
        /// </summary>
        /// <param name="leftglyph">The unicode number for the left glyph.</param>
        /// <param name="rightglyph">The unicode number for the right glyph.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>kerning</term><description>The kerning amount to add to the spacing between the two glyphs. May be negative.</description></item>
        /// </list>
        /// </returns>
        public double GetKerning(double leftglyph, double rightglyph) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the line height.</para>
        /// <para>This will be the value previously set by Font:setLineHeight, or 1.0 by default.</para>
        /// </summary>
        /// <param name="height">The current line height.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>height</term><description>The current line height.</description></item>
        /// </list>
        /// </returns>
        public double GetLineHeight(double height) => throw new NotImplementedException();
        /// <summary>
        /// <para>Determines the maximum width (accounting for newlines) taken by the given string.</para>
        /// </summary>
        /// <param name="text">A string.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>width</term><description>The width of the text.</description></item>
        /// </list>
        /// </returns>
        public double GetWidth(string text) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the Font can render a character or string.</para>
        /// </summary>
        /// <param name="text">A UTF-8 encoded unicode string.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>hasglyph</term><description>Whether the font can render all the UTF-8 characters in the string.</description></item>
        /// </list>
        /// </returns>
        public bool HasGlyphs(string text) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the Font can render a character or string.</para>
        /// </summary>
        /// <param name="character1">A unicode character.</param>
        /// <param name="character2">Another unicode character.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>hasglyph</term><description>Whether the font can render all the glyphs represented by the characters.</description></item>
        /// </list>
        /// </returns>
        public bool HasGlyphs(string character1, string character2) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the Font can render a character or string.</para>
        /// </summary>
        /// <param name="codepoint1">A unicode codepoint number.</param>
        /// <param name="codepoint2">Another unicode codepoint number.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>hasglyph</term><description>Whether the font can render all the glyphs represented by the codepoint numbers.</description></item>
        /// </list>
        /// </returns>
        public bool HasGlyphs(double codepoint1, double codepoint2) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the fallback fonts. When the Font doesn't contain a glyph, it will substitute the glyph from the next subsequent fallback Fonts. This is akin to setting a "font stack" in Cascading Style Sheets (CSS).</para>
        /// </summary>
        /// <param name="fallbackfont1">The first fallback Font to use.</param>
        /// <param name="">Additional fallback Fonts.</param>
        // TODO: public void SetFallbacks(object fallbackfont1, object) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the filter mode for a font.</para>
        /// </summary>
        /// <param name="min">How to scale a font down.</param>
        /// <param name="mag">How to scale a font up.</param>
        /// <param name="anisotropy">Maximum amount of anisotropic filtering used.</param>
        // TODO: public void SetFilter(FilterMode min, FilterMode mag = min, double anisotropy = 1) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the filter mode for a font.</para>
        /// </summary>
        public void NewFont() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the line height.</para>
        /// <para>When rendering the font in lines the actual height will be determined by the line height multiplied by the height of the font. The default is 1.0.</para>
        /// </summary>
        /// <param name="height">The new line height.</param>
        public void SetLineHeight(double height) => throw new NotImplementedException();
        /// <summary>
        /// <para>Destroys the object's Lua reference. The object will be completely deleted if it's not referenced by any other LÖVE object or thread.</para>
        /// <para>This method can be used to immediately clean up resources without waiting for Lua's garbage collector.</para>
        /// </summary>
        /// <param name="success">True if the object was released by this call, false if it had been previously released.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>success</term><description>True if the object was released by this call, false if it had been previously released.</description></item>
        /// </list>
        /// </returns>
        public bool Release(bool success) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the type of the object as a string.</para>
        /// </summary>
        /// <param name="type">The type as a string.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>type</term><description>The type as a string.</description></item>
        /// </list>
        /// </returns>
        public string Type(string type) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the type of the object as a string.</para>
        /// </summary>
        // TODO: public void NewImage() => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks whether an object is of a certain type. If the object has the type with the specified name in its hierarchy, this function will return true.</para>
        /// </summary>
        /// <param name="name">The name of the type to check for.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>b</term><description>True if the object is of the specified type, false otherwise.</description></item>
        /// </list>
        /// </returns>
        public bool TypeOf(string name) => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks whether an object is of a certain type. If the object has the type with the specified name in its hierarchy, this function will return true.</para>
        /// </summary>
        // TODO: public void NewImage() => throw new NotImplementedException();
    }
}
