namespace Brine2D
{
    /// <summary>
    /// Controls the canvas texture format.
    /// </summary>
    // TODO: Requires Review
    public enum TextureFormat
    {
        /// <summary>
        /// The default texture format: 8 bits per channel (32 bpp) RGBA. Color channel values range from 0-255 (0-1 in shaders.)
        /// </summary>
        Normal,
        /// <summary>
        /// Only usable in Canvases. The high dynamic range texture format: floating point 16 bits per channel (64 bpp) RGBA. Color channel values inside the Canvas range from -infinity to +infinity.
        /// </summary>
        Hdr,
        /// <summary>
        /// The same as normal, but the texture is interpreted as being in the sRGB color space. It will be decoded from sRGB to linear RGB when drawn or sampled from in a shader. For Canvases, this will also convert everything drawn to the Canvas from linear RGB to sRGB.
        /// </summary>
        Srgb,
    }
}
