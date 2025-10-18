namespace Brine2D
{
    /// <summary>
    /// <para>A GlyphData represents a drawable symbol of a font Rasterizer.</para>
    /// </summary>
    // TODO: Requires Review
    public class GlyphData
    {
        /// <summary>
        /// <para>Creates a new copy of the Data object.</para>
        /// </summary>
        /// <param name="clone">The new copy.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>clone</term><description>The new copy.</description></item>
        /// </list>
        /// </returns>
        public object Clone(object clone) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets an FFI pointer to the Data.</para>
        /// <para>This function should be preferred instead of Data:getPointer because the latter uses</para>
        /// <para>light userdata which can't store more all possible memory addresses on some new ARM64</para>
        /// <para>architectures, when LuaJIT is used.</para>
        /// </summary>
        /// <param name="pointer">A raw pointer to the Data, or if FFI is unavailable.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>pointer</term><description>A raw pointer to the Data, or if FFI is unavailable.</description></item>
        /// </list>
        /// </returns>
        public object GetFFIPointer(object pointer) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets a pointer to the Data. Can be used with libraries such as LuaJIT's FFI.</para>
        /// </summary>
        /// <param name="userdata">A raw pointer to the Data.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>userdata</term><description>A raw pointer to the Data.</description></item>
        /// </list>
        /// </returns>
        public object GetPointer(object userdata) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the Data's size in bytes.</para>
        /// </summary>
        /// <param name="size">The size of the Data in bytes.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>size</term><description>The size of the Data in bytes.</description></item>
        /// </list>
        /// </returns>
        public double GetSize(double size) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the full Data as a string.</para>
        /// </summary>
        /// <param name="data">The raw data.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>data</term><description>The raw data.</description></item>
        /// </list>
        /// </returns>
        public string GetString(string data) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets glyph advance.</para>
        /// </summary>
        /// <param name="advance">Glyph advance.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>advance</term><description>Glyph advance.</description></item>
        /// </list>
        /// </returns>
        public double GetAdvance(double advance) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets glyph bearing.</para>
        /// </summary>
        /// <param name="bx">Glyph bearing X.</param>
        /// <param name="by">Glyph bearing Y.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>bx</term><description>Glyph bearing X.</description></item>
        /// <item><term>by</term><description>Glyph bearing Y.</description></item>
        /// </list>
        /// </returns>
        public (double bx, double by) GetBearing(double bx, double by) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets glyph bounding box.</para>
        /// </summary>
        /// <param name="x">Glyph position x.</param>
        /// <param name="y">Glyph position y.</param>
        /// <param name="width">Glyph width.</param>
        /// <param name="height">Glyph height.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>Glyph position x.</description></item>
        /// <item><term>y</term><description>Glyph position y.</description></item>
        /// <item><term>width</term><description>Glyph width.</description></item>
        /// <item><term>height</term><description>Glyph height.</description></item>
        /// </list>
        /// </returns>
        public (double x, double y, double width, double height) GetBoundingBox(double x, double y, double width, double height) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets glyph dimensions.</para>
        /// </summary>
        /// <param name="width">Glyph width.</param>
        /// <param name="height">Glyph height.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>width</term><description>Glyph width.</description></item>
        /// <item><term>height</term><description>Glyph height.</description></item>
        /// </list>
        /// </returns>
        public (double width, double height) GetDimensions(double width, double height) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets glyph pixel format.</para>
        /// </summary>
        /// <param name="format">Glyph pixel format.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>format</term><description>Glyph pixel format.</description></item>
        /// </list>
        /// </returns>
        public PixelFormat GetFormat(PixelFormat format) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets glyph number.</para>
        /// </summary>
        /// <param name="glyph">Glyph number.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>glyph</term><description>Glyph number.</description></item>
        /// </list>
        /// </returns>
        public double GetGlyph(double glyph) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets glyph string.</para>
        /// </summary>
        /// <param name="glyph">Glyph string.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>glyph</term><description>Glyph string.</description></item>
        /// </list>
        /// </returns>
        public string GetGlyphString(string glyph) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets glyph height.</para>
        /// </summary>
        /// <param name="height">Glyph height.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>height</term><description>Glyph height.</description></item>
        /// </list>
        /// </returns>
        public double GetHeight(double height) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets glyph width.</para>
        /// </summary>
        /// <param name="width">Glyph width.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>width</term><description>Glyph width.</description></item>
        /// </list>
        /// </returns>
        public double GetWidth(double width) => throw new NotImplementedException();
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
