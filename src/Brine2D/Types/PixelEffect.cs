namespace Brine2D
{
    /// <summary>
    /// <para>A PixelEffect is used for advanced hardware-accelerated pixel manipulation. These effects are written in a language based on GLSL (OpenGL Shading Language) with a few things simplified for easier coding.</para>
/// <para>Potential uses for pixel effects include HDR/bloom, motion blur, grayscale/invert/sepia/any kind of color effect, reflection/refraction, distortions, and much more!</para>
    /// </summary>
    // TODO: Requires Review
    public class PixelEffect
    {
        /// <summary>
        /// <para>Returns any warning messages from compiling the pixel effect code. This can be used for debugging your pixel effects if there's anything the graphics hardware doesn't like.</para>
        /// </summary>
        /// <param name="warnings">Warning messages (if any).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>warnings</term><description>Warning messages (if any).</description></item>
        /// </list>
        /// </returns>
        public string GetWarnings(string warnings) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sends one or more values to a special (extern) variable inside the pixel effect. Extern variables have to be marked using the extern keyword, e.g.</para>
        /// <para>The corresponding send calls would be</para>
        /// </summary>
        /// <param name="name">Name of the number to send to the pixel effect.</param>
        /// <param name="number">Number to send to store in the extern.</param>
        /// <param name="">Additional numbers to send in case the extern is an array.</param>
        // TODO: public void Send(string name, double number, object) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sends one or more values to a special (extern) variable inside the pixel effect. Extern variables have to be marked using the extern keyword, e.g.</para>
        /// <para>The corresponding send calls would be</para>
        /// </summary>
        /// <param name="name">Name of the vector to send to the pixel effect.</param>
        /// <param name="vector">Numbers to send to the extern as a vector. The number of elements in the table determines the type of the vector (e.g. two numbers -&amp;gt; vec2). At least two and at most four numbers can be used.</param>
        /// <param name="">Additional vectors to send in case the extern is an array. All vectors need to be of the same size (e.g. only vec3's)</param>
        // TODO: public void Send(string name, object vector, object) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sends one or more values to a special (extern) variable inside the pixel effect. Extern variables have to be marked using the extern keyword, e.g.</para>
        /// <para>The corresponding send calls would be</para>
        /// </summary>
        /// <param name="name">Name of the matrix to send to the pixel effect.</param>
        /// <param name="matrix">2x2, 3x3, or 4x4 matrix to send to the extern. Using table form:</param>
        /// <param name="">Additional matrices of the same type as to store in the extern array.</param>
        // TODO: public void Send(string name, object matrix, object) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sends one or more values to a special (extern) variable inside the pixel effect. Extern variables have to be marked using the extern keyword, e.g.</para>
        /// <para>The corresponding send calls would be</para>
        /// </summary>
        /// <param name="name">Name of the Image to send to the pixel effect.</param>
        /// <param name="image">Image to send to the extern.</param>
        /// <param name="">Additional images in case the extern is an array.</param>
        // TODO: public void Send(string name, object image, object) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sends one or more values to a special (extern) variable inside the pixel effect. Extern variables have to be marked using the extern keyword, e.g.</para>
        /// <para>The corresponding send calls would be</para>
        /// </summary>
        /// <param name="name">Name of the Canvas to send to the pixel effect.</param>
        /// <param name="canvas">Canvas to send to the extern. The pixel effect type is .</param>
        /// <param name="">Additional canvases to send to the extern array.</param>
        // TODO: public void Send(string name, object canvas, object) => throw new NotImplementedException();
    }
}
