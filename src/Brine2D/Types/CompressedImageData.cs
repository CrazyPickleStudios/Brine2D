namespace Brine2D;

/// <summary>
///     <para>Represents compressed image data designed to stay compressed in RAM.</para>
///     <para>CompressedImageData encompasses standard compressed texture formats such as DXT1, DXT5, and BC5 / 3Dc.</para>
///     <para>You can't draw CompressedImageData directly to the screen. See Image for that.</para>
/// </summary>
public class CompressedImageData : DataObject
{
    /// <summary>
    ///     Gets the width and height of the CompressedImageData.
    /// </summary>
    /// <returns>
    ///     <list type="bullet">
    ///         <item>
    ///             <term>width</term><description>The width of the CompressedImageData.</description>
    ///         </item>
    ///         <item>
    ///             <term>height</term><description>The height of the CompressedImageData.</description>
    ///         </item>
    ///     </list>
    /// </returns>
    public (double width, double height) GetDimensions()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Gets the width and height of the CompressedImageData.
    /// </summary>
    /// <param name="level">A mipmap level. Must be in the range of [1, CompressedImageData:getMipmapCount()].</param>
    /// <returns>
    ///     <list type="bullet">
    ///         <item>
    ///             <term>width</term>
    ///             <description>The width of a specific mipmap level of the CompressedImageData.</description>
    ///         </item>
    ///         <item>
    ///             <term>height</term>
    ///             <description>The height of a specific mipmap level of the CompressedImageData.</description>
    ///         </item>
    ///     </list>
    /// </returns>
    public (double width, double height) GetDimensions(double level)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Gets the format of the CompressedImageData.
    /// </summary>
    /// <returns>
    ///     The format of the CompressedImageData.
    /// </returns>
    public CompressedImageFormat GetFormat()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Gets the height of the CompressedImageData.
    /// </summary>
    /// <returns>
    ///     The height of the CompressedImageData.
    /// </returns>
    public double GetHeight()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Gets the height of the CompressedImageData.
    /// </summary>
    /// <param name="level">A mipmap level. Must be in the range of [1, CompressedImageData:getMipmapCount()].</param>
    /// <returns>
    ///     The height of a specific mipmap level of the CompressedImageData.
    /// </returns>
    public double GetHeight(double level)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Gets the number of mipmap levels in the CompressedImageData. The base mipmap level (original image) is included in
    ///     the count.
    /// </summary>
    /// <remarks>
    ///     Mipmap filtering cannot be activated for an Image created from a CompressedImageData which does not have
    ///     enough mipmap levels to go down to 1x1.For example, a 256x256 image created from a CompressedImageData should have
    ///     8 mipmap levels or Image:setMipmapFilter will error.Most tools which can create compressed textures are able to
    ///     automatically generate mipmaps for them in the same file.
    /// </remarks>
    /// <returns>
    ///     The number of mipmap levels stored in the CompressedImageData.
    /// </returns>
    public double GetMipmapCount()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Gets the width of the CompressedImageData.
    /// </summary>
    /// <returns>
    ///     The width of the CompressedImageData.
    /// </returns>
    public double GetWidth()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Gets the width of the CompressedImageData.
    /// </summary>
    /// <param name="level">A mipmap level. Must be in the range of [1, CompressedImageData:getMipmapCount()].</param>
    /// <returns>
    ///     The width of a specific mipmap level of the CompressedImageData.
    /// </returns>
    public double GetWidth(double level)
    {
        throw new NotImplementedException();
    }
    
    /// <inheritdoc />
    public override DataObject Clone()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override IntPtr GetFFIPointer()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override IntPtr GetPointer()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override double GetSize()
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc />
    public override string GetString(double offset = 0, double size = Double.MaxValue)
    {
        throw new NotImplementedException();
    }
}