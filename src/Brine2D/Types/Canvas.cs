//namespace Brine2D
//{
//    /// <summary>
//    /// <para>A Canvas is used for off-screen rendering. Think of it as an invisible screen that you can draw to, but that will not be visible until you draw it to the actual visible screen. It is also known as "render to texture".</para>
///// <para>By drawing things that do not change position often (such as background items) to the Canvas, and then drawing the entire Canvas instead of each item,  you can reduce the number of draw operations performed each frame.</para>
///// <para>In versions prior to 0.10.0, not all graphics cards that LÖVE supported could use Canvases. love.graphics.isSupported("canvas") could be used to check for support at runtime.</para>
//    /// </summary>
//    // TODO: Requires Review
//    public class Canvas
//    {
//        /// <summary>
//        /// <para>Clears the contents of a Canvas to a specific color.</para>
//        /// <para>Calling this function directly after the Canvas becomes active (via love.graphics.setCanvas or Canvas:renderTo) is more efficient than calling it when the Canvas isn't active, especially on mobile devices.</para>
//        /// <para>love.graphics.setScissor will restrict the area of the Canvas that this function affects.</para>
//        /// </summary>
//        /// <param name="red">Red component of the clear color (0-255).</param>
//        /// <param name="green">Green component of the clear color (0-255).</param>
//        /// <param name="blue">Blue component of the clear color (0-255).</param>
//        /// <param name="alpha">Alpha component of the clear color (0-255).</param>
//        public void Clear(double red, double green, double blue, double alpha = 255) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Clears the contents of a Canvas to a specific color.</para>
//        /// <para>Calling this function directly after the Canvas becomes active (via love.graphics.setCanvas or Canvas:renderTo) is more efficient than calling it when the Canvas isn't active, especially on mobile devices.</para>
//        /// <para>love.graphics.setScissor will restrict the area of the Canvas that this function affects.</para>
//        /// </summary>
//        /// <param name="rgba">A with the red, green, blue and alpha values as numbers (alpha may be ommitted).</param>
//        public void Clear(object rgba) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Clears the contents of a Canvas to a specific color.</para>
//        /// <para>Calling this function directly after the Canvas becomes active (via love.graphics.setCanvas or Canvas:renderTo) is more efficient than calling it when the Canvas isn't active, especially on mobile devices.</para>
//        /// <para>love.graphics.setScissor will restrict the area of the Canvas that this function affects.</para>
//        /// </summary>
//        public void NewCanvas() => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Generates mipmaps for the Canvas, based on the contents of the highest-resolution mipmap level.</para>
//        /// <para>The Canvas must be created with mipmaps set to a MipmapMode other than "none" for this function to work. It should only be called while the Canvas is not the active render target.</para>
//        /// <para>If the mipmap mode is set to "auto", this function is automatically called inside love.graphics.setCanvas when switching from this Canvas to another Canvas or to the main screen.</para>
//        /// </summary>
//        public void GenerateMipmaps() => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Generates ImageData from the contents of the Canvas. Think of it as taking a screenshot of the hidden screen that is the Canvas.</para>
//        /// </summary>
//        /// <param name="data">The new ImageData made from the Canvas' image.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>data</term><description>The new ImageData made from the Canvas' image.</description></item>
//        /// </list>
//        /// </returns>
//        public object GetImageData(object data) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the number of multisample antialiasing (MSAA) samples used when drawing to the Canvas.</para>
//        /// <para>This may be different than the number used as an argument to love.graphics.newCanvas if the system running LÖVE doesn't support that number.</para>
//        /// </summary>
//        /// <param name="samples">The number of multisample antialiasing samples used by the canvas when drawing to it.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>samples</term><description>The number of multisample antialiasing samples used by the canvas when drawing to it.</description></item>
//        /// </list>
//        /// </returns>
//        public double GetMSAA(double samples) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the MipmapMode this Canvas was created with.</para>
//        /// </summary>
//        /// <param name="mode">The mipmap mode this Canvas was created with.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>mode</term><description>The mipmap mode this Canvas was created with.</description></item>
//        /// </list>
//        /// </returns>
//        public MipmapMode GetMipmapMode(MipmapMode mode) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the pixel at the specified position from a Canvas.</para>
//        /// <para>Valid x and y values start at 0 and go up to canvas width and height minus 1.</para>
//        /// </summary>
//        /// <param name="x">The position of the pixel on the x-axis.</param>
//        /// <param name="y">The position of the pixel on the y-axis.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>r</term><description>The red component (0-255).</description></item>
//        /// <item><term>g</term><description>The green component (0-255).</description></item>
//        /// <item><term>b</term><description>The blue component (0-255).</description></item>
//        /// <item><term>a</term><description>The alpha component (0-255).</description></item>
//        /// </list>
//        /// </returns>
//        public (double r, double g, double b, double a) GetPixel(double x, double y) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Render to the Canvas using a function.</para>
//        /// <para>This is a shortcut to love.graphics.setCanvas:</para>
//        /// <para>is the same as</para>
//        /// </summary>
//        /// <param name="func">A function performing drawing operations.</param>
//        public void RenderTo(object func) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Render to the Canvas using a function.</para>
//        /// <para>This is a shortcut to love.graphics.setCanvas:</para>
//        /// <para>is the same as</para>
//        /// </summary>
//        /// <param name="index">An index to a layer (for array textures and volume textures) or an index to a cubemap face (for cubemap textures).</param>
//        /// <param name="func">A function performing drawing operations.</param>
//        public void RenderTo(double index, object func) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Render to the Canvas using a function.</para>
//        /// <para>This is a shortcut to love.graphics.setCanvas:</para>
//        /// <para>is the same as</para>
//        /// </summary>
//        public void NewCanvas() => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Destroys the object's Lua reference. The object will be completely deleted if it's not referenced by any other LÖVE object or thread.</para>
//        /// <para>This method can be used to immediately clean up resources without waiting for Lua's garbage collector.</para>
//        /// </summary>
//        /// <param name="success">True if the object was released by this call, false if it had been previously released.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>success</term><description>True if the object was released by this call, false if it had been previously released.</description></item>
//        /// </list>
//        /// </returns>
//        public bool Release(bool success) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the type of the object as a string.</para>
//        /// </summary>
//        /// <param name="type">The type as a string.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>type</term><description>The type as a string.</description></item>
//        /// </list>
//        /// </returns>
//        public string Type(string type) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the type of the object as a string.</para>
//        /// </summary>
//        public void NewImage() => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Checks whether an object is of a certain type. If the object has the type with the specified name in its hierarchy, this function will return true.</para>
//        /// </summary>
//        /// <param name="name">The name of the type to check for.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>b</term><description>True if the object is of the specified type, false otherwise.</description></item>
//        /// </list>
//        /// </returns>
//        public bool TypeOf(string name) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Checks whether an object is of a certain type. If the object has the type with the specified name in its hierarchy, this function will return true.</para>
//        /// </summary>
//        public void NewImage() => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the DPI scale factor of the Texture.</para>
//        /// <para>The DPI scale factor represents relative pixel density. A DPI scale factor of 2 means the texture has twice the pixel density in each dimension (4 times as many pixels in the same area) compared to a texture with a DPI scale factor of 1.</para>
//        /// <para>For example, a texture with pixel dimensions of 100x100 with a DPI scale factor of 2 will be drawn as if it was 50x50. This is useful with high-dpi /  retina displays to easily allow swapping out higher or lower pixel density Images and Canvases without needing any extra manual scaling logic.</para>
//        /// </summary>
//        /// <param name="dpiscale">The DPI scale factor of the Texture.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>dpiscale</term><description>The DPI scale factor of the Texture.</description></item>
//        /// </list>
//        /// </returns>
//        public double GetDPIScale(double dpiscale) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the depth of a Volume Texture. Returns 1 for 2D, Cubemap, and Array textures.</para>
//        /// </summary>
//        /// <param name="depth">The depth of the volume Texture.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>depth</term><description>The depth of the volume Texture.</description></item>
//        /// </list>
//        /// </returns>
//        public double GetDepth(double depth) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the comparison mode used when sampling from a depth texture in a shader.</para>
//        /// <para>Depth texture comparison modes are advanced low-level functionality typically used with shadow mapping in 3D.</para>
//        /// </summary>
//        /// <param name="compare">The comparison mode used when sampling from this texture in a shader, or nil if setDepthSampleMode has not been called on this Texture.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>compare</term><description>The comparison mode used when sampling from this texture in a shader, or nil if setDepthSampleMode has not been called on this Texture.</description></item>
//        /// </list>
//        /// </returns>
//        public CompareMode GetDepthSampleMode(CompareMode compare = null) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the width and height of the Texture.</para>
//        /// </summary>
//        /// <param name="width">The width of the Texture, in pixels.</param>
//        /// <param name="height">The height of the Texture, in pixels.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>width</term><description>The width of the Texture, in pixels.</description></item>
//        /// <item><term>height</term><description>The height of the Texture, in pixels.</description></item>
//        /// </list>
//        /// </returns>
//        public (double width, double height) GetDimensions(double width, double height) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the filter mode of the Texture.</para>
//        /// </summary>
//        /// <param name="min">Filter mode to use when minifying the texture (rendering it at a smaller size on-screen than its size in pixels).</param>
//        /// <param name="mag">Filter mode to use when magnifying the texture (rendering it at a larger size on-screen than its size in pixels).</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>min</term><description>Filter mode to use when minifying the texture (rendering it at a smaller size on-screen than its size in pixels).</description></item>
//        /// <item><term>mag</term><description>Filter mode to use when magnifying the texture (rendering it at a larger size on-screen than its size in pixels).</description></item>
//        /// </list>
//        /// </returns>
//        public (FilterMode min, FilterMode mag) GetFilter(FilterMode min, FilterMode mag) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the filter mode of the Texture.</para>
//        /// </summary>
//        /// <param name="min">Filter mode to use when minifying the texture (rendering it at a smaller size on-screen than its size in pixels).</param>
//        /// <param name="mag">Filter mode to use when magnifying the texture (rendering it at a larger size on-screen than its size in pixels).</param>
//        /// <param name="anisotropy">Maximum amount of anisotropic filtering used.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>min</term><description>Filter mode to use when minifying the texture (rendering it at a smaller size on-screen than its size in pixels).</description></item>
//        /// <item><term>mag</term><description>Filter mode to use when magnifying the texture (rendering it at a larger size on-screen than its size in pixels).</description></item>
//        /// <item><term>anisotropy</term><description>Maximum amount of anisotropic filtering used.</description></item>
//        /// </list>
//        /// </returns>
//        public (FilterMode min, FilterMode mag, double anisotropy) GetFilter(FilterMode min, FilterMode mag, double anisotropy) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the pixel format of the Texture.</para>
//        /// </summary>
//        /// <param name="format">The pixel format the Texture was created with.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>format</term><description>The pixel format the Texture was created with.</description></item>
//        /// </list>
//        /// </returns>
//        public PixelFormat GetFormat(PixelFormat format) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the height of the Texture.</para>
//        /// </summary>
//        /// <param name="height">The height of the Texture, in pixels.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>height</term><description>The height of the Texture, in pixels.</description></item>
//        /// </list>
//        /// </returns>
//        public double GetHeight(double height) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the number of layers / slices in an Array Texture. Returns 1 for 2D, Cubemap, and Volume textures.</para>
//        /// </summary>
//        /// <param name="layers">The number of layers in the Array Texture.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>layers</term><description>The number of layers in the Array Texture.</description></item>
//        /// </list>
//        /// </returns>
//        public double GetLayerCount(double layers) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the number of mipmaps contained in the Texture. If the texture was not created with mipmaps, it will return 1.</para>
//        /// </summary>
//        /// <param name="mipmaps">The number of mipmaps in the Texture.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>mipmaps</term><description>The number of mipmaps in the Texture.</description></item>
//        /// </list>
//        /// </returns>
//        public double GetMipmapCount(double mipmaps) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the mipmap filter mode for a Texture. Prior to 11.0 this method only worked on Images.</para>
//        /// </summary>
//        /// <param name="mode">The filter mode used in between mipmap levels. if mipmap filtering is not enabled.</param>
//        /// <param name="sharpness">Value used to determine whether the image should use more or less detailed mipmap levels than normal when drawing.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>mode</term><description>The filter mode used in between mipmap levels. if mipmap filtering is not enabled.</description></item>
//        /// <item><term>sharpness</term><description>Value used to determine whether the image should use more or less detailed mipmap levels than normal when drawing.</description></item>
//        /// </list>
//        /// </returns>
//        public (FilterMode mode, double sharpness) GetMipmapFilter(FilterMode mode, double sharpness) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the width and height in pixels of the Texture.</para>
//        /// <para>Texture:getDimensions gets the dimensions of the texture in units scaled by the texture's DPI scale factor, rather than pixels. Use getDimensions for calculations related to drawing the texture (calculating an origin offset, for example), and getPixelDimensions only when dealing specifically with pixels, for example when using Canvas:newImageData.</para>
//        /// </summary>
//        /// <param name="pixelwidth">The width of the Texture, in pixels.</param>
//        /// <param name="pixelheight">The height of the Texture, in pixels.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>pixelwidth</term><description>The width of the Texture, in pixels.</description></item>
//        /// <item><term>pixelheight</term><description>The height of the Texture, in pixels.</description></item>
//        /// </list>
//        /// </returns>
//        public (double pixelwidth, double pixelheight) GetPixelDimensions(double pixelwidth, double pixelheight) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the height in pixels of the Texture.</para>
//        /// <para>Texture:getHeight gets the height of the texture in units scaled by the texture's DPI scale factor, rather than pixels. Use getHeight for calculations related to drawing the texture (calculating an origin offset, for example), and getPixelHeight only when dealing specifically with pixels, for example when using Canvas:newImageData.</para>
//        /// </summary>
//        /// <param name="pixelheight">The height of the Texture, in pixels.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>pixelheight</term><description>The height of the Texture, in pixels.</description></item>
//        /// </list>
//        /// </returns>
//        public double GetPixelHeight(double pixelheight) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the width in pixels of the Texture.</para>
//        /// <para>Texture:getWidth gets the width of the texture in units scaled by the texture's DPI scale factor, rather than pixels. Use getWidth for calculations related to drawing the texture (calculating an origin offset, for example), and getPixelWidth only when dealing specifically with pixels, for example when using Canvas:newImageData.</para>
//        /// </summary>
//        /// <param name="pixelwidth">The width of the Texture, in pixels.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>pixelwidth</term><description>The width of the Texture, in pixels.</description></item>
//        /// </list>
//        /// </returns>
//        public double GetPixelWidth(double pixelwidth) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the type of the Texture.</para>
//        /// </summary>
//        /// <param name="texturetype">The type of the Texture.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>texturetype</term><description>The type of the Texture.</description></item>
//        /// </list>
//        /// </returns>
//        public TextureType GetTextureType(TextureType texturetype) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the width of the Texture.</para>
//        /// </summary>
//        /// <param name="width">The width of the Texture, in pixels.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>width</term><description>The width of the Texture, in pixels.</description></item>
//        /// </list>
//        /// </returns>
//        public double GetWidth(double width) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets the wrapping properties of a Texture.</para>
//        /// <para>This function returns the currently set horizontal and vertical wrapping modes for the texture.</para>
//        /// </summary>
//        /// <param name="horiz">Horizontal wrapping mode of the texture.</param>
//        /// <param name="vert">Vertical wrapping mode of the texture.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>horiz</term><description>Horizontal wrapping mode of the texture.</description></item>
//        /// <item><term>vert</term><description>Vertical wrapping mode of the texture.</description></item>
//        /// </list>
//        /// </returns>
//        public (WrapMode horiz, WrapMode vert) GetWrap(WrapMode horiz, WrapMode vert) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Gets whether the Texture can be drawn and sent to a Shader.</para>
//        /// <para>Canvases created with stencil and/or depth PixelFormats are not readable by default, unless readable=true is specified in the settings table passed into love.graphics.newCanvas.</para>
//        /// <para>Non-readable Canvases can still be rendered to.</para>
//        /// </summary>
//        /// <param name="readable">Whether the Texture is readable.</param>
//        /// <returns>
//        /// <list type="bullet">
//        /// <item><term>readable</term><description>Whether the Texture is readable.</description></item>
//        /// </list>
//        /// </returns>
//        public bool IsReadable(bool readable) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Sets the comparison mode used when sampling from a depth texture in a shader.</para>
//        /// <para>Depth texture comparison modes are advanced low-level functionality typically used with shadow mapping in 3D.</para>
//        /// <para>When using a depth texture with a comparison mode set in a shader, it must be declared as a sampler2DShadow and used in a GLSL 3 Shader. The result of accessing the texture in the shader will return a float between 0 and 1, proportional to the number of samples (up to 4 samples will be used if bilinear filtering is enabled) that passed the test set by the comparison operation.</para>
//        /// <para>Depth texture comparison can only be used with readable depth-formatted Canvases.</para>
//        /// </summary>
//        /// <param name="compare">The comparison mode used when sampling from this texture in a shader.</param>
//        public void SetDepthSampleMode(CompareMode compare) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Sets the comparison mode used when sampling from a depth texture in a shader.</para>
//        /// <para>Depth texture comparison modes are advanced low-level functionality typically used with shadow mapping in 3D.</para>
//        /// <para>When using a depth texture with a comparison mode set in a shader, it must be declared as a sampler2DShadow and used in a GLSL 3 Shader. The result of accessing the texture in the shader will return a float between 0 and 1, proportional to the number of samples (up to 4 samples will be used if bilinear filtering is enabled) that passed the test set by the comparison operation.</para>
//        /// <para>Depth texture comparison can only be used with readable depth-formatted Canvases.</para>
//        /// </summary>
//        public void SetDepthSampleMode() => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Sets the filter mode of the Texture.</para>
//        /// </summary>
//        /// <param name="min">Filter mode to use when minifying the texture (rendering it at a smaller size on-screen than its size in pixels).</param>
//        /// <param name="mag">Filter mode to use when magnifying the texture (rendering it at a larger size on-screen than its size in pixels).</param>
//        public void SetFilter(FilterMode min, FilterMode mag = min) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Sets the mipmap filter mode for a Texture. Prior to 11.0 this method only worked on Images.</para>
//        /// <para>Mipmapping is useful when drawing a texture at a reduced scale. It can improve performance and reduce aliasing issues.</para>
//        /// <para>In 0.10.0 and newer, the texture must be created with the mipmaps flag enabled for the mipmap filter to have any effect. In versions prior to 0.10.0 it's best to call this method directly after creating the image with love.graphics.newImage, to avoid bugs in certain graphics drivers.</para>
//        /// <para>Due to hardware restrictions and driver bugs, in versions prior to 0.10.0 images that weren't loaded from a CompressedData must have power-of-two dimensions (64x64, 512x256, etc.) to use mipmaps.</para>
//        /// </summary>
//        /// <param name="filtermode">The filter mode to use in between mipmap levels. "nearest" will often give better performance.</param>
//        /// <param name="sharpness">A positive sharpness value makes the texture use a more detailed mipmap level when drawing, at the expense of performance. A negative value does the reverse.</param>
//        public void SetMipmapFilter(FilterMode filtermode, double sharpness = 0) => throw new NotImplementedException();
//        /// <summary>
//        /// <para>Sets the wrapping properties of a Texture.</para>
//        /// <para>This function sets the way a Texture is repeated when it is drawn with a Quad that is larger than the texture's extent, or when a custom Shader is used which uses texture coordinates outside of [0, 1]. A texture may be clamped or set to repeat in both horizontal and vertical directions.</para>
//        /// <para>Clamped textures appear only once (with the edges of the texture stretching to fill the extent of the Quad), whereas repeated ones repeat as many times as there is room in the Quad.</para>
//        /// </summary>
//        /// <param name="horiz">Horizontal wrapping mode of the texture.</param>
//        /// <param name="vert">Vertical wrapping mode of the texture.</param>
//        public void SetWrap(WrapMode horiz, WrapMode vert = horiz) => throw new NotImplementedException();
//    }
//}
