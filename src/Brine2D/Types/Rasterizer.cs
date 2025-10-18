namespace Brine2D
{
    /// <summary>
    /// <para>A Rasterizer handles font rendering, containing the font data (image or TrueType font) and drawable glyphs.</para>
    /// </summary>
    // TODO: Requires Review
    public class Rasterizer
    {
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
        /// <summary>
        /// <para>Gets font advance.</para>
        /// </summary>
        /// <param name="advance">Font advance.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>advance</term><description>Font advance.</description></item>
        /// </list>
        /// </returns>
        public double GetAdvance(double advance) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets ascent height.</para>
        /// </summary>
        /// <param name="height">Ascent height.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>height</term><description>Ascent height.</description></item>
        /// </list>
        /// </returns>
        public double GetAscent(double height) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets descent height.</para>
        /// </summary>
        /// <param name="height">Descent height.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>height</term><description>Descent height.</description></item>
        /// </list>
        /// </returns>
        public double GetDescent(double height) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets number of glyphs in font.</para>
        /// </summary>
        /// <param name="count">Glyphs count.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>count</term><description>Glyphs count.</description></item>
        /// </list>
        /// </returns>
        public double GetGlyphCount(double count) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets glyph data of a specified glyph.</para>
        /// </summary>
        /// <param name="glyph">Glyph</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>glyphData</term><description>Glyph data</description></item>
        /// </list>
        /// </returns>
        public object GetGlyphData(string glyph) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets glyph data of a specified glyph.</para>
        /// </summary>
        /// <param name="glyphNumber">Glyph number</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>glyphData</term><description>Glyph data</description></item>
        /// </list>
        /// </returns>
        public object GetGlyphData(double glyphNumber) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets font height.</para>
        /// </summary>
        /// <param name="height">Font height</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>height</term><description>Font height</description></item>
        /// </list>
        /// </returns>
        public double GetHeight(double height) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets line height of a font.</para>
        /// </summary>
        /// <param name="height">Line height of a font.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>height</term><description>Line height of a font.</description></item>
        /// </list>
        /// </returns>
        public double GetLineHeight(double height) => throw new NotImplementedException();
        /// <summary>
        /// <para>Checks if font contains specified glyphs.</para>
        /// </summary>
        /// <param name="or">Glyph</param>
        /// <param name="or2">Glyph</param>
        /// <param name="or3">Additional glyphs</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>hasGlyphs</term><description>Whatever font contains specified glyphs.</description></item>
        /// </list>
        /// </returns>
        public bool HasGlyphs(string or, string or2, string or3) => throw new NotImplementedException();
    }
}
