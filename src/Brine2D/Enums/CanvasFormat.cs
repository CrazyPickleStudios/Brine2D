namespace Brine2D
{
    /// <summary>
    /// Canvas texture formats.
    /// </summary>
    // TODO: Requires Review
    public enum CanvasFormat
    {
        /// <summary>
        /// The default Canvas format - usually an alias for the rgba8 format, or the srgb format if gamma-correct rendering is enabled in LÖVE 0.10.0 and newer.
        /// </summary>
        Normal,
        /// <summary>
        /// A format suitable for high dynamic range content - an alias for the rgba16f format, normally.
        /// </summary>
        Hdr,
        /// <summary>
        /// 8 bits per channel (32 bpp) RGBA. Color channel values range from 0-255 (0-1 in shaders).
        /// </summary>
        Rgba8,
        /// <summary>
        /// 4 bits per channel (16 bpp) RGBA.
        /// </summary>
        Rgba4,
        /// <summary>
        /// RGB with 5 bits each, and a 1-bit alpha channel (16 bpp).
        /// </summary>
        Rgb5a1,
        /// <summary>
        /// RGB with 5, 6, and 5 bits each, respectively (16 bpp). There is no alpha channel in this format.
        /// </summary>
        Rgb565,
        /// <summary>
        /// RGB with 10 bits per channel, and a 2-bit alpha channel (32 bpp).
        /// </summary>
        Rgb10a2,
        /// <summary>
        /// Floating point RGBA with 16 bits per channel (64 bpp). Color values can range from [-65504, +65504].
        /// </summary>
        Rgba16f,
        /// <summary>
        /// Floating point RGBA with 32 bits per channel (128 bpp).
        /// </summary>
        Rgba32f,
        /// <summary>
        /// Floating point RGB with 11 bits in the red and green channels, and 10 bits in the blue channel (32 bpp). There is no alpha channel. Color values can range from [0, +65024].
        /// </summary>
        Rg11b10f,
        /// <summary>
        /// The same as rgba8, but the Canvas is interpreted as being in the sRGB color space. Everything drawn to the Canvas will be converted from linear RGB to sRGB. When the Canvas is drawn (or used in a shader), it will be decoded from sRGB to linear RGB. This reduces color banding when doing gamma-correct rendering, since sRGB encoding has more precision than linear RGB for darker colors.
        /// </summary>
        Srgb,
        /// <summary>
        /// Single-channel (red component) format (8 bpp).
        /// </summary>
        R8,
        /// <summary>
        /// Two channels (red and green components) with 8 bits per channel (16 bpp).
        /// </summary>
        Rg8,
        /// <summary>
        /// Floating point single-channel format (16 bpp). Color values can range from [-65504, +65504].
        /// </summary>
        R16f,
        /// <summary>
        /// Floating point two-channel format with 16 bits per channel (32 bpp). Color values can range from [-65504, +65504].
        /// </summary>
        Rg16f,
        /// <summary>
        /// Floating point single-channel format (32 bpp).
        /// </summary>
        R32f,
        /// <summary>
        /// Floating point two-channel format with 32 bits per channel (64 bpp).
        /// </summary>
        Rg32f,
    }
}
