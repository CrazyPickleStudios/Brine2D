namespace Brine2D
{
    /// <summary>
    /// Types of system-dependent graphics limits.
    /// </summary>
    // TODO: Requires Review
    public enum GraphicsLimit
    {
        /// <summary>
        /// The maximum size of points.
        /// </summary>
        Pointsize,
        /// <summary>
        /// The maximum width or height of Images and Canvases.
        /// </summary>
        Texturesize,
        /// <summary>
        /// The maximum number of simultaneously active canvases (via love.graphics.setCanvas.)
        /// </summary>
        Multicanvas,
        /// <summary>
        /// The maximum number of antialiasing samples for a Canvas.
        /// </summary>
        Canvasmsaa,
        /// <summary>
        /// The maximum number of layers in an Array texture.
        /// </summary>
        Texturelayers,
        /// <summary>
        /// The maximum width, height, or depth of a Volume texture.
        /// </summary>
        Volumetexturesize,
        /// <summary>
        /// The maximum width or height of a Cubemap texture.
        /// </summary>
        Cubetexturesize,
        /// <summary>
        /// The maximum amount of anisotropic filtering. Texture:setMipmapFilter internally clamps the given anisotropy value to the system's limit.
        /// </summary>
        Anisotropy,
    }
}
