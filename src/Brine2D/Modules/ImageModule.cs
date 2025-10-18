namespace Brine2D;

/// <summary>
///     Provides an interface to decode encoded image data.
/// </summary>
public sealed class ImageModule
{
    /// <summary>
    ///     Determines whether a file can be loaded as CompressedImageData.
    /// </summary>
    /// <param name="filename">The filename of the potentially compressed image file.</param>
    /// <returns>
    ///     Whether the file can be loaded as CompressedImageData or not.
    /// </returns>
    public bool IsCompressed(string filename)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Determines whether a file can be loaded as CompressedImageData.
    /// </summary>
    /// <param name="fileData">A FileData potentially containing a compressed image.</param>
    /// <returns>
    ///     Whether the FileData can be loaded as CompressedImageData or not.
    /// </returns>
    public bool IsCompressed(FileData fileData)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Create a new CompressedImageData object from a compressed image file. LÖVE supports several compressed texture
    ///     formats, enumerated in the CompressedImageFormat page.
    /// </summary>
    /// <remarks>
    ///     This function can be slow if it is called repeatedly, such as from love.update or love.draw. If you need to use a
    ///     specific resource often, create it once and store it somewhere it can be reused!
    /// </remarks>
    /// <param name="filename">The filename of the compressed image file.</param>
    /// <returns>
    ///     The new CompressedImageData object.
    /// </returns>
    public CompressedImageData NewCompressedData(string filename)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Create a new CompressedImageData object from a compressed image file. LÖVE supports several compressed texture
    ///     formats, enumerated in the CompressedImageFormat page.
    /// </summary>
    /// <remarks>
    ///     This function can be slow if it is called repeatedly, such as from love.update or love.draw. If you need to use a
    ///     specific resource often, create it once and store it somewhere it can be reused!
    /// </remarks>
    /// <param name="fileData">A FileData containing a compressed image.</param>
    /// <returns>
    ///     The new CompressedImageData object.
    /// </returns>
    public CompressedImageData NewCompressedData(FileData fileData)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Creates a new ImageData object.
    /// </summary>
    /// <remarks>
    ///     This function can be slow if it is called repeatedly, such as from love.update or love.draw. If you need to use a
    ///     specific resource often, create it once and store it somewhere it can be reused!
    /// </remarks>
    /// <param name="width">The width of the ImageData.</param>
    /// <param name="height">The height of the ImageData.</param>
    /// <param name="format"> The pixel format of the ImageData.</param>
    /// <param name="rawData">Optional raw byte data to load into the ImageData, in the format specified by format.</param>
    /// <returns>
    ///     The new ImageData object. If data isn't supplied, each pixel's color values (including the alpha values!) will be
    ///     set to zero.
    /// </returns>
    /// TODO: This is supposedly string or Data, not sure what 'data' is though.
    public ImageData NewImageData(double width, double height, PixelFormat format = PixelFormat.RBGA8,
        string? rawData = null)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Creates a new ImageData object.
    /// </summary>
    /// <remarks>
    ///     This function can be slow if it is called repeatedly, such as from love.update or love.draw. If you need to use a
    ///     specific resource often, create it once and store it somewhere it can be reused!
    /// </remarks>
    /// <param name="filename">The filename of the image file.</param>
    /// <returns>
    ///     The new ImageData object.
    /// </returns>
    public ImageData NewImageData(string filename)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Creates a new ImageData object.
    /// </summary>
    /// <remarks>
    ///     This function can be slow if it is called repeatedly, such as from love.update or love.draw. If you need to use a
    ///     specific resource often, create it once and store it somewhere it can be reused!
    /// </remarks>
    /// <param name="filedata">The encoded file data to decode into image data.</param>
    /// <returns>
    ///     The new ImageData object.
    /// </returns>
    public ImageData NewImageData(FileData filedata)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Creates a new ImageData object.
    /// </summary>
    /// <remarks>
    ///     This function can be slow if it is called repeatedly, such as from love.update or love.draw. If you need to use a
    ///     specific resource often, create it once and store it somewhere it can be reused!
    /// </remarks>
    /// <param name="encodeddata">The encoded data to load into the ImageData.</param>
    /// <returns>
    ///     The new ImageData object.
    /// </returns>
    public ImageData NewImageData(Data encodeddata)
    {
        throw new NotImplementedException();
    }
}