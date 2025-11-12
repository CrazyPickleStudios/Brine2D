namespace Brine2D.Core.Graphics;

/// <summary>
///     Describes the memory layout and color space of texture pixel data.
///     Formats are normalized unsigned integers unless specified otherwise.
/// </summary>
public enum TextureFormat
{
    /// <summary>
    ///     8-bit single-channel (R) normalized [0..1].
    ///     Commonly used for masks, height maps, or single-channel textures.
    /// </summary>
    R8_UNorm,

    /// <summary>
    ///     8-bit two-channel (R,G) normalized [0..1] per channel.
    ///     Useful for data like UV offsets, normals (2-channel), or LUTs.
    /// </summary>
    R8G8_UNorm,

    /// <summary>
    ///     8-bit four-channel (R,G,B,A) normalized [0..1] per channel in linear color space.
    ///     Typical for color textures when linear sampling is required.
    /// </summary>
    R8G8B8A8_UNorm,

    /// <summary>
    ///     8-bit four-channel (R,G,B,A) normalized [0..1] per channel with sRGB transfer function.
    ///     Reads/writes are gamma-corrected; sampling converts to linear for shading.
    ///     Use for color textures authored in sRGB space.
    /// </summary>
    R8G8B8A8_UNorm_sRGB,

    /// <summary>
    ///     8-bit four-channel (B,G,R,A) normalized [0..1] per channel in linear color space.
    ///     Often used by APIs/platforms where BGRA is the native optimal layout.
    /// </summary>
    B8G8R8A8_UNorm,

    /// <summary>
    ///     16-bit float per component (R,G,B,A) in linear space.
    ///     Provides higher precision and range for HDR render targets, light buffers, or high quality intermediate passes.
    ///     Not sRGB encoded; sampling returns linear floats.
    /// </summary>
    R16G16B16A16_Float
}