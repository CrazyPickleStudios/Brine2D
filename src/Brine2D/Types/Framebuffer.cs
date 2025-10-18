namespace Brine2D
{
    /// <summary>
    /// <para>A Framebuffer is used for off-screen rendering. Think of it as an invisible screen that you can draw to, but that will not be visible until you draw it to the actual visible screen. It is also known as "render to texture".</para>
/// <para>By drawing things that do not change position often (such as background items) to the Framebuffer, and then drawing the entire Framebuffer instead of each item,  you can reduce the number of draw operations performed each frame.</para>
    /// </summary>
    // TODO: Requires Review
    public class Framebuffer
    {
        /// <summary>
        /// <para>Returns the image data stored in the Framebuffer. Think of it as a screenshot of the hidden screen that is the framebuffer.</para>
        /// </summary>
        /// <param name="data">The image data stored in the framebuffer.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>data</term><description>The image data stored in the framebuffer.</description></item>
        /// </list>
        /// </returns>
        public object GetImageData(object data) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the wrapping properties of a Framebuffer.</para>
        /// <para>This functions returns the currently set horizontal and vertical wrapping modes for the framebuffer.</para>
        /// </summary>
        /// <param name="horiz">Horizontal wrapping mode of the Framebuffer.</param>
        /// <param name="vert">Vertical wrapping mode of the Framebuffer.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>horiz</term><description>Horizontal wrapping mode of the Framebuffer.</description></item>
        /// <item><term>vert</term><description>Vertical wrapping mode of the Framebuffer.</description></item>
        /// </list>
        /// </returns>
        public (WrapMode horiz, WrapMode vert) GetWrap(WrapMode horiz, WrapMode vert) => throw new NotImplementedException();
        /// <summary>
        /// <para>Render to the Framebuffer using a function.</para>
        /// </summary>
        /// <param name="func">A function performing drawing operations.</param>
        public void RenderTo(object func) => throw new NotImplementedException();
        /// <summary>
        /// <para>Render to the Framebuffer using a function.</para>
        /// </summary>
        public void RenderTo() => throw new NotImplementedException();
        /// <summary>
        /// <para>Render to the Framebuffer using a function.</para>
        /// </summary>
        public void Draw() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the wrapping properties of a Framebuffer.</para>
        /// <para>This function sets the way the edges of a Framebuffer are treated if it is scaled or rotated. If the WrapMode is set to 'clamp', the edge will not be interpolated. If set to 'repeat', the edge will be interpolated with the pixels on the opposing side of the framebuffer.</para>
        /// </summary>
        /// <param name="horiz">Horizontal wrapping mode of the framebuffer.</param>
        /// <param name="vert">Vertical wrapping mode of the framebuffer.</param>
        public void SetWrap(WrapMode horiz, WrapMode vert) => throw new NotImplementedException();
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
