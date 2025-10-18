namespace Brine2D
{
    /// <summary>
    /// How the image wraps inside a large Quad.
    /// </summary>
    // TODO: Requires Review
    public enum WrapMode
    {
        /// <summary>
        /// Clamp the texture. Appears only once. The area outside the texture's normal range is colored based on the edge pixels of the texture.
        /// </summary>
        Clamp,
        /// <summary>
        /// Repeat the texture. Fills the whole available extent.
        /// </summary>
        Repeat,
        /// <summary>
        /// Repeat the texture, flipping it each time it repeats. May produce better visual results than the repeat mode when the texture doesn't seamlessly tile.
        /// </summary>
        Mirroredrepeat,
        /// <summary>
        /// Clamp the texture. Fills the area outside the texture's normal range with transparent black (or opaque black for textures with no alpha channel.)
        /// </summary>
        Clampzero,
    }
}
