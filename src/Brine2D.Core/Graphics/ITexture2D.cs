namespace Brine2D.Core.Graphics;

/// <summary>
///     Represents a 2D texture resource.
/// </summary>
/// <remarks>
///     Implementations are expected to manage underlying GPU or native resources and must release them in
///     <see cref="IDisposable.Dispose" />.
///     The texture dimensions are expressed in pixels, and the pixel layout is described by <see cref="TextureFormat" />.
/// </remarks>
public interface ITexture2D : IDisposable
{
    /// <summary>
    ///     Gets the pixel format of the texture.
    /// </summary>
    /// <remarks>
    ///     The format indicates channel ordering and color space, e.g.,
    ///     <see cref="TextureFormat.R8G8B8A8_UNorm" /> or <see cref="TextureFormat.R8G8B8A8_UNorm_sRGB" />.
    /// </remarks>
    TextureFormat Format { get; }

    /// <summary>
    ///     Gets the height of the texture, in pixels.
    /// </summary>
    int Height { get; }

    /// <summary>
    ///     Gets the width of the texture, in pixels.
    /// </summary>
    int Width { get; }
}