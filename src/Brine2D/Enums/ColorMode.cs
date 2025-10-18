namespace Brine2D
{
    /// <summary>
    /// Controls how drawn images are affected by current color.
    /// </summary>
    // TODO: Requires Review
    public enum ColorMode
    {
        /// <summary>
        /// Images (etc) will be affected by the current color.
        /// </summary>
        Modulate,
        /// <summary>
        /// Replace color mode. Images (etc) will not be affected by current color.
        /// </summary>
        Replace,
        /// <summary>
        /// Colorize images (etc) with the current color. While 'modulate' works like a filter that absorbs colors that differ from the current color (possibly to a point where a color channel is entirely absorbed), 'combine' tints the image in a way that preserves a hint of the original colors.
        /// </summary>
        CombineSince080,
    }
}
