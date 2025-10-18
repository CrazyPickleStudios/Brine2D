namespace Brine2D
{
    /// <summary>
    /// <para>Raw (decoded) image data.</para>
/// <para>You can't draw ImageData directly to screen. See Image for that.</para>
    /// </summary>
    // TODO: Requires Review
    public class ImageData
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
        /// <para>Gets the width and height of the ImageData in pixels.</para>
        /// </summary>
        /// <param name="width">The width of the in pixels.</param>
        /// <param name="height">The height of the in pixels.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>width</term><description>The width of the in pixels.</description></item>
        /// <item><term>height</term><description>The height of the in pixels.</description></item>
        /// </list>
        /// </returns>
        public (double width, double height) GetDimensions(double width, double height) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the pixel format of the ImageData.</para>
        /// </summary>
        /// <param name="format">The pixel format the ImageData was created with.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>format</term><description>The pixel format the ImageData was created with.</description></item>
        /// </list>
        /// </returns>
        public PixelFormat GetFormat(PixelFormat format) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the height of the ImageData in pixels.</para>
        /// </summary>
        /// <param name="height">The height of the in pixels.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>height</term><description>The height of the in pixels.</description></item>
        /// </list>
        /// </returns>
        public double GetHeight(double height) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the color of a pixel at a specific position in the image.</para>
        /// <para>Valid x and y values start at 0 and go up to image width and height minus 1. Non-integer values are floored.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        /// <param name="x">The position of the pixel on the x-axis.</param>
        /// <param name="y">The position of the pixel on the y-axis.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>r</term><description>The red component (0-1).</description></item>
        /// <item><term>g</term><description>The green component (0-1).</description></item>
        /// <item><term>b</term><description>The blue component (0-1).</description></item>
        /// <item><term>a</term><description>The alpha component (0-1).</description></item>
        /// </list>
        /// </returns>
        public (double r, double g, double b, double a) GetPixel(double x, double y) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the color of a pixel at a specific position in the image.</para>
        /// <para>Valid x and y values start at 0 and go up to image width and height minus 1. Non-integer values are floored.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// </summary>
        public void NewImageData() => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the width of the ImageData in pixels.</para>
        /// </summary>
        /// <param name="width">The width of the in pixels.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>width</term><description>The width of the in pixels.</description></item>
        /// </list>
        /// </returns>
        public double GetWidth(double width) => throw new NotImplementedException();
        /// <summary>
        /// <para>Transform an image by applying a function to every pixel.</para>
        /// <para>This function is a higher-order function. It takes another function as a parameter, and calls it once for each pixel in the ImageData.</para>
        /// <para>The passed function is called with six parameters for each pixel in turn. The parameters are numbers that represent the x and y coordinates of the pixel and its red, green, blue and alpha values. The function should return the new red, green, blue, and alpha values for that pixel.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// <para>This function locks the ImageData until it is done, making it safe to use from multiple Threads, albeit without any performance gains.</para>
        /// </summary>
        /// <param name="pixelFunction">Function to apply to every pixel.</param>
        public void MapPixel(object pixelFunction) => throw new NotImplementedException();
        /// <summary>
        /// <para>Transform an image by applying a function to every pixel.</para>
        /// <para>This function is a higher-order function. It takes another function as a parameter, and calls it once for each pixel in the ImageData.</para>
        /// <para>The passed function is called with six parameters for each pixel in turn. The parameters are numbers that represent the x and y coordinates of the pixel and its red, green, blue and alpha values. The function should return the new red, green, blue, and alpha values for that pixel.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// <para>This function locks the ImageData until it is done, making it safe to use from multiple Threads, albeit without any performance gains.</para>
        /// </summary>
        public void MapPixel() => throw new NotImplementedException();
        /// <summary>
        /// <para>Paste into ImageData from another source ImageData.</para>
        /// </summary>
        /// <param name="source">Source ImageData from which to copy.</param>
        /// <param name="dx">Destination top-left position on x-axis.</param>
        /// <param name="dy">Destination top-left position on y-axis.</param>
        /// <param name="sx">Source top-left position on x-axis.</param>
        /// <param name="sy">Source top-left position on y-axis.</param>
        /// <param name="sw">Source width.</param>
        /// <param name="sh">Source height.</param>
        public void Paste(object source, double dx, double dy, double sx, double sy, double sw, double sh) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the color of a pixel at a specific position in the image.</para>
        /// <para>Valid x and y values start at 0 and go up to image width and height minus 1.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// <para>This function locks the ImageData until it is done, making it safe to use from multiple Threads, albeit without any performance gains.</para>
        /// </summary>
        /// <param name="x">The position of the pixel on the x-axis.</param>
        /// <param name="y">The position of the pixel on the y-axis.</param>
        /// <param name="r">The red component (0-1).</param>
        /// <param name="g">The green component (0-1).</param>
        /// <param name="b">The blue component (0-1).</param>
        /// <param name="a">The alpha component (0-1).</param>
        public void SetPixel(double x, double y, double r, double g, double b, double a) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the color of a pixel at a specific position in the image.</para>
        /// <para>Valid x and y values start at 0 and go up to image width and height minus 1.</para>
        /// <para>In versions prior to 11.0, color component values were within the range of 0 to 255 instead of 0 to 1.</para>
        /// <para>This function locks the ImageData until it is done, making it safe to use from multiple Threads, albeit without any performance gains.</para>
        /// </summary>
        // TODO: public void NewImageData() => throw new NotImplementedException();
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
        public void NewImage() => throw new NotImplementedException();
    }
}
