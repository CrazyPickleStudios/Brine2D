namespace Brine2D
{
    /// <summary>
    /// <para>A quadrilateral (a polygon with four sides and four corners) with Texture coordinate information.</para>
/// <para>Quads can be used to select part of a texture to draw. In this way, one large texture atlas can be loaded, and then split up into sub-images.</para>
    /// </summary>
    // TODO: Requires Review
    public class Quad
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
        /// <para>Gets the layer specified by Quad:setLayer.</para>
        /// </summary>
        /// <param name="layerindex">Layer index used by the Quad for .</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>layerindex</term><description>Layer index used by the Quad for .</description></item>
        /// </list>
        /// </returns>
        public double GetLayer(double layerindex) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets reference texture dimensions initially specified in love.graphics.newQuad.</para>
        /// </summary>
        /// <param name="sw">The Texture width used by the Quad.</param>
        /// <param name="sh">The Texture height used by the Quad.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>sw</term><description>The Texture width used by the Quad.</description></item>
        /// <item><term>sh</term><description>The Texture height used by the Quad.</description></item>
        /// </list>
        /// </returns>
        public (double sw, double sh) GetTextureDimensions(double sw, double sh) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the current viewport of this Quad.</para>
        /// </summary>
        /// <param name="x">The top-left corner along the x-axis.</param>
        /// <param name="y">The top-left corner along the y-axis.</param>
        /// <param name="w">The width of the viewport.</param>
        /// <param name="h">The height of the viewport.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>x</term><description>The top-left corner along the x-axis.</description></item>
        /// <item><term>y</term><description>The top-left corner along the y-axis.</description></item>
        /// <item><term>w</term><description>The width of the viewport.</description></item>
        /// <item><term>h</term><description>The height of the viewport.</description></item>
        /// </list>
        /// </returns>
        public (double x, double y, double w, double h) GetViewport(double x, double y, double w, double h) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the layer to use in Array Textures.</para>
        /// </summary>
        /// <param name="layerindex">Layer index to be used by the Quad for .</param>
        public void SetLayer(double layerindex) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the texture coordinates according to a viewport.</para>
        /// </summary>
        /// <param name="x">The top-left corner along the x-axis.</param>
        /// <param name="y">The top-left corner along the y-axis.</param>
        /// <param name="w">The width of the viewport.</param>
        /// <param name="h">The height of the viewport.</param>
        public void SetViewport(double x, double y, double w, double h) => throw new NotImplementedException();
    }
}
