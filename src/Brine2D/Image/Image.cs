namespace Brine2D.Image
{
    /// <summary>
    /// <para>Drawable image type.</para>
    /// </summary>
    // TODO: Requires Review
    public class Image
    {
        /// <summary>
        /// <para>Gets the original ImageData or CompressedData used to create the Image.</para>
        /// <para>All Images keep a reference to the Data that was used to create the Image. The Data is used to refresh the Image when love.window.setMode or Image:refresh is called.</para>
        /// </summary>
        /// <param name="data">The original ImageData used to create the Image, if the image is not compressed.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>data</term><description>The original ImageData used to create the Image, if the image is not compressed.</description></item>
        /// </list>
        /// </returns>
        public object GetData(object data) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the original ImageData or CompressedData used to create the Image.</para>
        /// <para>All Images keep a reference to the Data that was used to create the Image. The Data is used to refresh the Image when love.window.setMode or Image:refresh is called.</para>
        /// </summary>
        /// <param name="data">The original CompressedData used to create the Image, if the image is compressed.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>data</term><description>The original CompressedData used to create the Image, if the image is compressed.</description></item>
        /// </list>
        /// </returns>
        // TODO: public object GetData(object data) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the original ImageData or CompressedData used to create the Image.</para>
        /// <para>All Images keep a reference to the Data that was used to create the Image. The Data is used to refresh the Image when love.window.setMode or Image:refresh is called.</para>
        /// </summary>
        // TODO:  public void NewImage() => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the Image was created from CompressedData.</para>
        /// <para>Compressed images take up less space in VRAM, and drawing a compressed image will generally be more efficient than drawing one created from raw pixel data.</para>
        /// </summary>
        /// <param name="compressed">Whether the Image is stored as a compressed texture on the GPU.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>compressed</term><description>Whether the Image is stored as a compressed texture on the GPU.</description></item>
        /// </list>
        /// </returns>
        public bool IsCompressed(bool compressed) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the Image was created with the linear (non-gamma corrected) flag set to true.</para>
        /// <para>This method always returns false when gamma-correct rendering is not enabled.</para>
        /// </summary>
        /// <param name="linear">Whether the Image's internal pixel format is linear (not gamma corrected), when gamma-correct rendering is enabled.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>linear</term><description>Whether the Image's internal pixel format is linear (not gamma corrected), when gamma-correct rendering is enabled.</description></item>
        /// </list>
        /// </returns>
        public bool IsFormatLinear(bool linear) => throw new NotImplementedException();
        /// <summary>
        /// <para>Reloads the Image's contents from the ImageData or CompressedData used to create the image.</para>
        /// </summary>
        public void Refresh() => throw new NotImplementedException();
        /// <summary>
        /// <para>Reloads the Image's contents from the ImageData or CompressedData used to create the image.</para>
        /// </summary>
        // TODO: public void NewImageData() => throw new NotImplementedException();
        /// <summary>
        /// <para>Replace the contents of an Image.</para>
        /// </summary>
        /// <param name="data">The new to replace the contents with.</param>
        /// <param name="slice">Which to replace, if applicable; the value is ignored otherwise.</param>
        /// <param name="mipmap">The mimap level to replace, if the Image has mipmaps.</param>
        /// <param name="x">The x-offset in pixels from the top-left of the image to replace. The given ImageData's width plus this value must not be greater than the pixel width of the Image's specified mipmap level.</param>
        /// <param name="y">The y-offset in pixels from the top-left of the image to replace. The given ImageData's height plus this value must not be greater than the pixel height of the Image's specified mipmap level.</param>
        /// <param name="reloadmipmaps">Whether to generate new mipmaps after replacing the Image's pixels. True by default if the Image was created with automatically generated mipmaps, false by default otherwise.</param>
        // TODO:  public void ReplacePixels(object data, double slice = null, double mipmap = 1, double x = 0, double y = 0, bool reloadmipmaps = false) => throw new NotImplementedException();
        /// <summary>
        /// <para>Replace the contents of an Image.</para>
        /// </summary>
        public void NewImageData() => throw new NotImplementedException();
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
        // TODO:  public void NewImage() => throw new NotImplementedException();
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
        /// <summary>
        /// <para>Gets the DPI scale factor of the Texture.</para>
        /// <para>The DPI scale factor represents relative pixel density. A DPI scale factor of 2 means the texture has twice the pixel density in each dimension (4 times as many pixels in the same area) compared to a texture with a DPI scale factor of 1.</para>
        /// <para>For example, a texture with pixel dimensions of 100x100 with a DPI scale factor of 2 will be drawn as if it was 50x50. This is useful with high-dpi /  retina displays to easily allow swapping out higher or lower pixel density Images and Canvases without needing any extra manual scaling logic.</para>
        /// </summary>
        /// <param name="dpiscale">The DPI scale factor of the Texture.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>dpiscale</term><description>The DPI scale factor of the Texture.</description></item>
        /// </list>
        /// </returns>
        public double GetDPIScale(double dpiscale) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the depth of a Volume Texture. Returns 1 for 2D, Cubemap, and Array textures.</para>
        /// </summary>
        /// <param name="depth">The depth of the volume Texture.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>depth</term><description>The depth of the volume Texture.</description></item>
        /// </list>
        /// </returns>
        public double GetDepth(double depth) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the comparison mode used when sampling from a depth texture in a shader.</para>
        /// <para>Depth texture comparison modes are advanced low-level functionality typically used with shadow mapping in 3D.</para>
        /// </summary>
        /// <param name="compare">The comparison mode used when sampling from this texture in a shader, or nil if setDepthSampleMode has not been called on this Texture.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>compare</term><description>The comparison mode used when sampling from this texture in a shader, or nil if setDepthSampleMode has not been called on this Texture.</description></item>
        /// </list>
        /// </returns>
        // TODO:  public CompareMode GetDepthSampleMode(CompareMode compare = null) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the width and height of the Texture.</para>
        /// </summary>
        /// <param name="width">The width of the Texture, in pixels.</param>
        /// <param name="height">The height of the Texture, in pixels.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>width</term><description>The width of the Texture, in pixels.</description></item>
        /// <item><term>height</term><description>The height of the Texture, in pixels.</description></item>
        /// </list>
        /// </returns>
        public (double width, double height) GetDimensions(double width, double height) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the filter mode of the Texture.</para>
        /// </summary>
        /// <param name="min">Filter mode to use when minifying the texture (rendering it at a smaller size on-screen than its size in pixels).</param>
        /// <param name="mag">Filter mode to use when magnifying the texture (rendering it at a larger size on-screen than its size in pixels).</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>min</term><description>Filter mode to use when minifying the texture (rendering it at a smaller size on-screen than its size in pixels).</description></item>
        /// <item><term>mag</term><description>Filter mode to use when magnifying the texture (rendering it at a larger size on-screen than its size in pixels).</description></item>
        /// </list>
        /// </returns>
        public (FilterMode min, FilterMode mag) GetFilter(FilterMode min, FilterMode mag) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the filter mode of the Texture.</para>
        /// </summary>
        /// <param name="min">Filter mode to use when minifying the texture (rendering it at a smaller size on-screen than its size in pixels).</param>
        /// <param name="mag">Filter mode to use when magnifying the texture (rendering it at a larger size on-screen than its size in pixels).</param>
        /// <param name="anisotropy">Maximum amount of anisotropic filtering used.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>min</term><description>Filter mode to use when minifying the texture (rendering it at a smaller size on-screen than its size in pixels).</description></item>
        /// <item><term>mag</term><description>Filter mode to use when magnifying the texture (rendering it at a larger size on-screen than its size in pixels).</description></item>
        /// <item><term>anisotropy</term><description>Maximum amount of anisotropic filtering used.</description></item>
        /// </list>
        /// </returns>
        public (FilterMode min, FilterMode mag, double anisotropy) GetFilter(FilterMode min, FilterMode mag, double anisotropy) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the pixel format of the Texture.</para>
        /// </summary>
        /// <param name="format">The pixel format the Texture was created with.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>format</term><description>The pixel format the Texture was created with.</description></item>
        /// </list>
        /// </returns>
        public PixelFormat GetFormat(PixelFormat format) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the height of the Texture.</para>
        /// </summary>
        /// <param name="height">The height of the Texture, in pixels.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>height</term><description>The height of the Texture, in pixels.</description></item>
        /// </list>
        /// </returns>
        public double GetHeight(double height) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the number of layers / slices in an Array Texture. Returns 1 for 2D, Cubemap, and Volume textures.</para>
        /// </summary>
        /// <param name="layers">The number of layers in the Array Texture.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>layers</term><description>The number of layers in the Array Texture.</description></item>
        /// </list>
        /// </returns>
        public double GetLayerCount(double layers) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the number of mipmaps contained in the Texture. If the texture was not created with mipmaps, it will return 1.</para>
        /// </summary>
        /// <param name="mipmaps">The number of mipmaps in the Texture.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>mipmaps</term><description>The number of mipmaps in the Texture.</description></item>
        /// </list>
        /// </returns>
        public double GetMipmapCount(double mipmaps) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the mipmap filter mode for a Texture. Prior to 11.0 this method only worked on Images.</para>
        /// </summary>
        /// <param name="mode">The filter mode used in between mipmap levels. if mipmap filtering is not enabled.</param>
        /// <param name="sharpness">Value used to determine whether the image should use more or less detailed mipmap levels than normal when drawing.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>mode</term><description>The filter mode used in between mipmap levels. if mipmap filtering is not enabled.</description></item>
        /// <item><term>sharpness</term><description>Value used to determine whether the image should use more or less detailed mipmap levels than normal when drawing.</description></item>
        /// </list>
        /// </returns>
        public (FilterMode mode, double sharpness) GetMipmapFilter(FilterMode mode, double sharpness) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the width and height in pixels of the Texture.</para>
        /// <para>Texture:getDimensions gets the dimensions of the texture in units scaled by the texture's DPI scale factor, rather than pixels. Use getDimensions for calculations related to drawing the texture (calculating an origin offset, for example), and getPixelDimensions only when dealing specifically with pixels, for example when using Canvas:newImageData.</para>
        /// </summary>
        /// <param name="pixelwidth">The width of the Texture, in pixels.</param>
        /// <param name="pixelheight">The height of the Texture, in pixels.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>pixelwidth</term><description>The width of the Texture, in pixels.</description></item>
        /// <item><term>pixelheight</term><description>The height of the Texture, in pixels.</description></item>
        /// </list>
        /// </returns>
        public (double pixelwidth, double pixelheight) GetPixelDimensions(double pixelwidth, double pixelheight) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the height in pixels of the Texture.</para>
        /// <para>Texture:getHeight gets the height of the texture in units scaled by the texture's DPI scale factor, rather than pixels. Use getHeight for calculations related to drawing the texture (calculating an origin offset, for example), and getPixelHeight only when dealing specifically with pixels, for example when using Canvas:newImageData.</para>
        /// </summary>
        /// <param name="pixelheight">The height of the Texture, in pixels.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>pixelheight</term><description>The height of the Texture, in pixels.</description></item>
        /// </list>
        /// </returns>
        public double GetPixelHeight(double pixelheight) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the width in pixels of the Texture.</para>
        /// <para>Texture:getWidth gets the width of the texture in units scaled by the texture's DPI scale factor, rather than pixels. Use getWidth for calculations related to drawing the texture (calculating an origin offset, for example), and getPixelWidth only when dealing specifically with pixels, for example when using Canvas:newImageData.</para>
        /// </summary>
        /// <param name="pixelwidth">The width of the Texture, in pixels.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>pixelwidth</term><description>The width of the Texture, in pixels.</description></item>
        /// </list>
        /// </returns>
        public double GetPixelWidth(double pixelwidth) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the type of the Texture.</para>
        /// </summary>
        /// <param name="texturetype">The type of the Texture.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>texturetype</term><description>The type of the Texture.</description></item>
        /// </list>
        /// </returns>
        public TextureType GetTextureType(TextureType texturetype) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the width of the Texture.</para>
        /// </summary>
        /// <param name="width">The width of the Texture, in pixels.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>width</term><description>The width of the Texture, in pixels.</description></item>
        /// </list>
        /// </returns>
        public double GetWidth(double width) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets the wrapping properties of a Texture.</para>
        /// <para>This function returns the currently set horizontal and vertical wrapping modes for the texture.</para>
        /// </summary>
        /// <param name="horiz">Horizontal wrapping mode of the texture.</param>
        /// <param name="vert">Vertical wrapping mode of the texture.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>horiz</term><description>Horizontal wrapping mode of the texture.</description></item>
        /// <item><term>vert</term><description>Vertical wrapping mode of the texture.</description></item>
        /// </list>
        /// </returns>
        public (WrapMode horiz, WrapMode vert) GetWrap(WrapMode horiz, WrapMode vert) => throw new NotImplementedException();
        /// <summary>
        /// <para>Gets whether the Texture can be drawn and sent to a Shader.</para>
        /// <para>Canvases created with stencil and/or depth PixelFormats are not readable by default, unless readable=true is specified in the settings table passed into love.graphics.newCanvas.</para>
        /// <para>Non-readable Canvases can still be rendered to.</para>
        /// </summary>
        /// <param name="readable">Whether the Texture is readable.</param>
        /// <returns>
        /// <list type="bullet">
        /// <item><term>readable</term><description>Whether the Texture is readable.</description></item>
        /// </list>
        /// </returns>
        public bool IsReadable(bool readable) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the comparison mode used when sampling from a depth texture in a shader.</para>
        /// <para>Depth texture comparison modes are advanced low-level functionality typically used with shadow mapping in 3D.</para>
        /// <para>When using a depth texture with a comparison mode set in a shader, it must be declared as a sampler2DShadow and used in a GLSL 3 Shader. The result of accessing the texture in the shader will return a float between 0 and 1, proportional to the number of samples (up to 4 samples will be used if bilinear filtering is enabled) that passed the test set by the comparison operation.</para>
        /// <para>Depth texture comparison can only be used with readable depth-formatted Canvases.</para>
        /// </summary>
        /// <param name="compare">The comparison mode used when sampling from this texture in a shader.</param>
        public void SetDepthSampleMode(CompareMode compare) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the comparison mode used when sampling from a depth texture in a shader.</para>
        /// <para>Depth texture comparison modes are advanced low-level functionality typically used with shadow mapping in 3D.</para>
        /// <para>When using a depth texture with a comparison mode set in a shader, it must be declared as a sampler2DShadow and used in a GLSL 3 Shader. The result of accessing the texture in the shader will return a float between 0 and 1, proportional to the number of samples (up to 4 samples will be used if bilinear filtering is enabled) that passed the test set by the comparison operation.</para>
        /// <para>Depth texture comparison can only be used with readable depth-formatted Canvases.</para>
        /// </summary>
        public void SetDepthSampleMode() => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the filter mode of the Texture.</para>
        /// </summary>
        /// <param name="min">Filter mode to use when minifying the texture (rendering it at a smaller size on-screen than its size in pixels).</param>
        /// <param name="mag">Filter mode to use when magnifying the texture (rendering it at a larger size on-screen than its size in pixels).</param>
        // TODO: public void SetFilter(FilterMode min, FilterMode mag = min) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the mipmap filter mode for a Texture. Prior to 11.0 this method only worked on Images.</para>
        /// <para>Mipmapping is useful when drawing a texture at a reduced scale. It can improve performance and reduce aliasing issues.</para>
        /// <para>In 0.10.0 and newer, the texture must be created with the mipmaps flag enabled for the mipmap filter to have any effect. In versions prior to 0.10.0 it's best to call this method directly after creating the image with love.graphics.newImage, to avoid bugs in certain graphics drivers.</para>
        /// <para>Due to hardware restrictions and driver bugs, in versions prior to 0.10.0 images that weren't loaded from a CompressedData must have power-of-two dimensions (64x64, 512x256, etc.) to use mipmaps.</para>
        /// </summary>
        /// <param name="filtermode">The filter mode to use in between mipmap levels. "nearest" will often give better performance.</param>
        /// <param name="sharpness">A positive sharpness value makes the texture use a more detailed mipmap level when drawing, at the expense of performance. A negative value does the reverse.</param>
        public void SetMipmapFilter(FilterMode filtermode, double sharpness = 0) => throw new NotImplementedException();
        /// <summary>
        /// <para>Sets the wrapping properties of a Texture.</para>
        /// <para>This function sets the way a Texture is repeated when it is drawn with a Quad that is larger than the texture's extent, or when a custom Shader is used which uses texture coordinates outside of [0, 1]. A texture may be clamped or set to repeat in both horizontal and vertical directions.</para>
        /// <para>Clamped textures appear only once (with the edges of the texture stretching to fill the extent of the Quad), whereas repeated ones repeat as many times as there is room in the Quad.</para>
        /// </summary>
        /// <param name="horiz">Horizontal wrapping mode of the texture.</param>
        /// <param name="vert">Vertical wrapping mode of the texture.</param>
        // TODO: public void SetWrap(WrapMode horiz, WrapMode vert = horiz) => throw new NotImplementedException();
    }
}
