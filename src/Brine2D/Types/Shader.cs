namespace Brine2D
{
    /// <summary>
    /// <para>A Shader is used for advanced hardware-accelerated pixel or vertex manipulation. These effects are written in a language based on GLSL (OpenGL Shading Language) with a few things simplified for easier coding.</para>
/// <para>Potential uses for shaders include HDR/bloom, motion blur, grayscale/invert/sepia/any kind of color effect, reflection/refraction, distortions, bump mapping, and much more! Here is a collection of basic shaders and good starting point to learn: https://github.com/vrld/moonshine</para>
/// <para>To use the most recent version of GLSL, you must put #pragma language glsl3 at the top of your shader files. See love.graphics.newShader for details.</para>
    /// </summary>
    // TODO: Requires Review
    public class Shader
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
        /// <para>Gets information about an 'extern' ('uniform') variable in the shader.</para>
        /// <para>Returns nil if the variable name doesn't exist in the shader, or if the video driver's shader compiler has determined that the variable doesn't affect the final output of the shader.</para>
        /// </summary>
        /// <param name="name">The name of the extern variable.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>type</term><description>The base type of the variable.</description></item>
        /// <item><term>components</term><description>The number of components in the variable (e.g. 2 for a vec2 or mat2.)</description></item>
        /// <item><term>arrayelements</term><description>The number of elements in the array if the variable is an array, or 1 if not.</description></item>
        /// </list>
        /// </returns>
        public (object type, double components, double arrayelements) GetExternVariable(string name) => throw new NotImplementedException();
        /// <summary>
        /// <para>Returns any warning and error messages from compiling the shader code. This can be used for debugging your shaders if there's anything the graphics hardware doesn't like.</para>
        /// </summary>
        /// <param name="warnings">Warning and error messages (if any).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>warnings</term><description>Warning and error messages (if any).</description></item>
        /// </list>
        /// </returns>
        public string GetWarnings(string warnings) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether a uniform / extern variable exists in the Shader.</para>
        /// <para>If a graphics driver's shader compiler determines that a uniform / extern variable doesn't affect the final output of the shader, it may optimize the variable out. This function will return false in that case.</para>
        /// </summary>
        /// <param name="name">The name of the uniform variable.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>hasuniform</term><description>Whether the uniform exists in the shader and affects its final output.</description></item>
        /// </list>
        /// </returns>
        public bool HasUniform(string name) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sends one or more values to a special (uniform) variable inside the shader. Uniform variables have to be marked using the uniform or extern keyword, e.g.</para>
        /// <para>The corresponding send calls would be</para>
        /// <para>Uniform / extern variables are read-only in the shader code and remain constant until modified by a Shader:send call. Uniform variables can be accessed in both the Vertex and Pixel components of a shader, as long as the variable is declared in each.</para>
        /// </summary>
        /// <param name="name">Name of the number to send to the shader.</param>
        /// <param name="number">Number to send to store in the uniform variable.</param>
        /// <param name="">Additional numbers to send if the uniform variable is an array.</param>
        // TODO: public void Send(string name, double number, object) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sends one or more values to a special (uniform) variable inside the shader. Uniform variables have to be marked using the uniform or extern keyword, e.g.</para>
        /// <para>The corresponding send calls would be</para>
        /// <para>Uniform / extern variables are read-only in the shader code and remain constant until modified by a Shader:send call. Uniform variables can be accessed in both the Vertex and Pixel components of a shader, as long as the variable is declared in each.</para>
        /// </summary>
        /// <param name="name">Name of the vector to send to the shader.</param>
        /// <param name="vector">Numbers to send to the uniform variable as a vector. The number of elements in the table determines the type of the vector (e.g. two numbers -&amp;gt; vec2). At least two and at most four numbers can be used.</param>
        /// <param name="">Additional vectors to send if the uniform variable is an array. All vectors need to be of the same size (e.g. only vec3's).</param>
        // TODO: public void Send(string name, object vector, object) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sends one or more values to a special (uniform) variable inside the shader. Uniform variables have to be marked using the uniform or extern keyword, e.g.</para>
        /// <para>The corresponding send calls would be</para>
        /// <para>Uniform / extern variables are read-only in the shader code and remain constant until modified by a Shader:send call. Uniform variables can be accessed in both the Vertex and Pixel components of a shader, as long as the variable is declared in each.</para>
        /// </summary>
        /// <param name="name">Name of the matrix to send to the shader.</param>
        /// <param name="matrix">2x2, 3x3, or 4x4 matrix to send to the uniform variable. Using table form: or (since version ) . The order in 0.10.2 is column-major; starting in it's row-major instead.</param>
        /// <param name="">Additional matrices of the same type as to store in a uniform array.</param>
        // TODO: public void Send(string name, object matrix, object) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sends one or more values to a special (uniform) variable inside the shader. Uniform variables have to be marked using the uniform or extern keyword, e.g.</para>
        /// <para>The corresponding send calls would be</para>
        /// <para>Uniform / extern variables are read-only in the shader code and remain constant until modified by a Shader:send call. Uniform variables can be accessed in both the Vertex and Pixel components of a shader, as long as the variable is declared in each.</para>
        /// </summary>
        /// <param name="name">Name of the to send to the shader.</param>
        /// <param name="texture">Texture ( or ) to send to the uniform variable.</param>
        public void Send(string name, object texture) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sends one or more values to a special (uniform) variable inside the shader. Uniform variables have to be marked using the uniform or extern keyword, e.g.</para>
        /// <para>The corresponding send calls would be</para>
        /// <para>Uniform / extern variables are read-only in the shader code and remain constant until modified by a Shader:send call. Uniform variables can be accessed in both the Vertex and Pixel components of a shader, as long as the variable is declared in each.</para>
        /// </summary>
        /// <param name="name">Name of the boolean to send to the shader.</param>
        /// <param name="boolean">Boolean to send to store in the uniform variable.</param>
        /// <param name="">Additional booleans to send if the uniform variable is an array.</param>
        // TODO: public void Send(string name, bool boolean, object) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sends one or more values to a special (uniform) variable inside the shader. Uniform variables have to be marked using the uniform or extern keyword, e.g.</para>
        /// <para>The corresponding send calls would be</para>
        /// <para>Uniform / extern variables are read-only in the shader code and remain constant until modified by a Shader:send call. Uniform variables can be accessed in both the Vertex and Pixel components of a shader, as long as the variable is declared in each.</para>
        /// </summary>
        /// <param name="name">Name of the matrix to send to the shader.</param>
        /// <param name="matrixlayout">The layout (row- or column-major) of the matrix.</param>
        /// <param name="matrix">2x2, 3x3, or 4x4 matrix to send to the uniform variable. Using table form: or .</param>
        /// <param name="">Additional matrices of the same type as to store in a uniform array.</param>
        // TODO: public void Send(string name, object matrixlayout, object matrix, object) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sends one or more values to a special (uniform) variable inside the shader. Uniform variables have to be marked using the uniform or extern keyword, e.g.</para>
        /// <para>The corresponding send calls would be</para>
        /// <para>Uniform / extern variables are read-only in the shader code and remain constant until modified by a Shader:send call. Uniform variables can be accessed in both the Vertex and Pixel components of a shader, as long as the variable is declared in each.</para>
        /// </summary>
        /// <param name="name">Name of the uniform to send to the shader.</param>
        /// <param name="data">Data object containing the values to send.</param>
        /// <param name="offset">Offset in bytes from the start of the Data object.</param>
        /// <param name="size">Size in bytes of the data to send. If nil, as many bytes as the specified uniform uses will be copied.</param>
        // TODO: public void Send(string name, object data, double offset = 0, double size = all) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sends one or more values to a special (uniform) variable inside the shader. Uniform variables have to be marked using the uniform or extern keyword, e.g.</para>
        /// <para>The corresponding send calls would be</para>
        /// <para>Uniform / extern variables are read-only in the shader code and remain constant until modified by a Shader:send call. Uniform variables can be accessed in both the Vertex and Pixel components of a shader, as long as the variable is declared in each.</para>
        /// </summary>
        /// <param name="name">Name of the uniform matrix to send to the shader.</param>
        /// <param name="data">Data object containing the values to send.</param>
        /// <param name="matrixlayout">The layout (row- or column-major) of the matrix in memory.</param>
        /// <param name="offset">Offset in bytes from the start of the Data object.</param>
        /// <param name="size">Size in bytes of the data to send. If nil, as many bytes as the specified uniform uses will be copied.</param>
        // TODO: public void Send(string name, object data, object matrixlayout, double offset = 0, double size = all) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sends one or more colors to a special (uniform) vec3 or vec4 variable inside the shader. The color components must be in the range of [0, 1]. The colors are gamma-corrected if global gamma-correction is enabled.</para>
        /// <para>Uniforms must be marked using the uniform keyword, e.g.</para>
        /// <para>The corresponding sendColor call would be</para>
        /// <para>Uniforms can be accessed in both the Vertex and Pixel stages of a shader, as long as the variable is declared in each.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        /// <param name="name">The name of the color uniform to send to in the shader.</param>
        /// <param name="color">A table with red, green, blue, and optional alpha color components in the range of [0, 1] to send to the uniform as a vector.</param>
        /// <param name="">Additional colors to send in case the uniform is an array. All colors need to be of the same size (e.g. only vec3's).</param>
        // TODO: public void SendColor(string name, object color, object) => throw new NotImplementedException();
    }
}
