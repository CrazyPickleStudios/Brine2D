namespace Brine2D
{
    /// <summary>
    /// Compressed image data formats.
    /// </summary>
    // TODO: Requires Review
    public enum CompressedImageFormat
    {
        /// <summary>
        /// The DXT1 format. RGB data at 4 bits per pixel (compared to 32 bits for ImageData and regular Images.) Suitable for fully opaque images on desktop systems.
        /// </summary>
        DXT1,
        /// <summary>
        /// The DXT3 format. RGBA data at 8 bits per pixel. Smooth variations in opacity do not mix well with this format.
        /// </summary>
        DXT3,
        /// <summary>
        /// The DXT5 format. RGBA data at 8 bits per pixel. Recommended for images with varying opacity on desktop systems.
        /// </summary>
        DXT5,
        /// <summary>
        /// The BC4 format (also known as 3Dc+ or ATI1.) Stores just the red channel, at 4 bits per pixel.
        /// </summary>
        BC4,
        /// <summary>
        /// The signed variant of the BC4 format. Same as above but pixel values in the texture are in the range of [-1, 1] instead of [0, 1] in shaders.
        /// </summary>
        BC4s,
        /// <summary>
        /// The BC5 format (also known as 3Dc or ATI2.) Stores red and green channels at 8 bits per pixel.
        /// </summary>
        BC5,
        /// <summary>
        /// The signed variant of the BC5 format.
        /// </summary>
        BC5s,
        /// <summary>
        /// The BC6H format. Stores half-precision floating-point RGB data in the range of [0, 65504] at 8 bits per pixel. Suitable for HDR images on desktop systems.
        /// </summary>
        BC6h,
        /// <summary>
        /// The signed variant of the BC6H format. Stores RGB data in the range of [-65504, +65504].
        /// </summary>
        BC6hs,
        /// <summary>
        /// The BC7 format (also known as BPTC.) Stores RGB or RGBA data at 8 bits per pixel.
        /// </summary>
        BC7,
        /// <summary>
        /// The ETC1 format. RGB data at 4 bits per pixel. Suitable for fully opaque images on older Android devices.
        /// </summary>
        ETC1,
        /// <summary>
        /// The RGB variant of the ETC2 format. RGB data at 4 bits per pixel. Suitable for fully opaque images on newer mobile devices.
        /// </summary>
        ETC2rgb,
        /// <summary>
        /// The RGBA variant of the ETC2 format. RGBA data at 8 bits per pixel. Recommended for images with varying opacity on newer mobile devices.
        /// </summary>
        ETC2rgba,
        /// <summary>
        /// The RGBA variant of the ETC2 format where pixels are either fully transparent or fully opaque. RGBA data at 4 bits per pixel.
        /// </summary>
        ETC2rgba1,
        /// <summary>
        /// The single-channel variant of the EAC format. Stores just the red channel, at 4 bits per pixel.
        /// </summary>
        EACr,
        /// <summary>
        /// The signed single-channel variant of the EAC format. Same as above but pixel values in the texture are in the range of [-1, 1] instead of [0, 1] in shaders.
        /// </summary>
        EACrs,
        /// <summary>
        /// The two-channel variant of the EAC format. Stores red and green channels at 8 bits per pixel.
        /// </summary>
        EACrg,
        /// <summary>
        /// The signed two-channel variant of the EAC format.
        /// </summary>
        EACrgs,
        /// <summary>
        /// The 2 bit per pixel RGB variant of the PVRTC1 format. Stores RGB data at 2 bits per pixel. Textures compressed with PVRTC1 formats must be square and power-of-two sized.
        /// </summary>
        PVR1rgb2,
        /// <summary>
        /// The 4 bit per pixel RGB variant of the PVRTC1 format. Stores RGB data at 4 bits per pixel.
        /// </summary>
        PVR1rgb4,
        /// <summary>
        /// The 2 bit per pixel RGBA variant of the PVRTC1 format.
        /// </summary>
        PVR1rgba2,
        /// <summary>
        /// The 4 bit per pixel RGBA variant of the PVRTC1 format.
        /// </summary>
        PVR1rgba4,
        /// <summary>
        /// The 4x4 pixels per block variant of the ASTC format. RGBA data at 8 bits per pixel.
        /// </summary>
        ASTC4x4,
        /// <summary>
        /// The 5x4 pixels per block variant of the ASTC format. RGBA data at 6.4 bits per pixel.
        /// </summary>
        ASTC5x4,
        /// <summary>
        /// The 5x5 pixels per block variant of the ASTC format. RGBA data at 5.12 bits per pixel.
        /// </summary>
        ASTC5x5,
        /// <summary>
        /// The 6x5 pixels per block variant of the ASTC format. RGBA data at 4.27 bits per pixel.
        /// </summary>
        ASTC6x5,
        /// <summary>
        /// The 6x6 pixels per block variant of the ASTC format. RGBA data at 3.56 bits per pixel.
        /// </summary>
        ASTC6x6,
        /// <summary>
        /// The 8x5 pixels per block variant of the ASTC format. RGBA data at 3.2 bits per pixel.
        /// </summary>
        ASTC8x5,
        /// <summary>
        /// The 8x6 pixels per block variant of the ASTC format. RGBA data at 2.67 bits per pixel.
        /// </summary>
        ASTC8x6,
        /// <summary>
        /// The 8x8 pixels per block variant of the ASTC format. RGBA data at 2 bits per pixel.
        /// </summary>
        ASTC8x8,
        /// <summary>
        /// The 10x5 pixels per block variant of the ASTC format. RGBA data at 2.56 bits per pixel.
        /// </summary>
        ASTC10x5,
        /// <summary>
        /// The 10x6 pixels per block variant of the ASTC format. RGBA data at 2.13 bits per pixel.
        /// </summary>
        ASTC10x6,
        /// <summary>
        /// The 10x8 pixels per block variant of the ASTC format. RGBA data at 1.6 bits per pixel.
        /// </summary>
        ASTC10x8,
        /// <summary>
        /// The 10x10 pixels per block variant of the ASTC format. RGBA data at 1.28 bits per pixel.
        /// </summary>
        ASTC10x10,
        /// <summary>
        /// The 12x10 pixels per block variant of the ASTC format. RGBA data at 1.07 bits per pixel.
        /// </summary>
        ASTC12x10,
        /// <summary>
        /// The 12x12 pixels per block variant of the ASTC format. RGBA data at 0.89 bits per pixel.
        /// </summary>
        ASTC12x12,
    }
}
