namespace Brine2D
{
    /// <summary>
    /// How the image is filtered when scaling.
    /// </summary>
    // TODO: Requires Review
    public enum FilterMode
    {
        /// <summary>
        /// Scale image with linear interpolation.
        /// </summary>
        Linear,
        /// <summary>
        /// Scale image with nearest neighbor interpolation.
        /// </summary>
        Nearest,
    }
}
