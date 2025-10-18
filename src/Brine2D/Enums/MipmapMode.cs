namespace Brine2D
{
    /// <summary>
    /// Controls whether a Canvas has mipmaps, and its behaviour when it does.
    /// </summary>
    // TODO: Requires Review
    public enum MipmapMode
    {
        /// <summary>
        /// The Canvas has no mipmaps.
        /// </summary>
        None,
        /// <summary>
        /// The Canvas has mipmaps. love.graphics.setCanvas can be used to render to a specific mipmap level, or Canvas:generateMipmaps can (re-)compute all mipmap levels based on the base level.
        /// </summary>
        Manual,
        /// <summary>
        /// The Canvas has mipmaps, and all mipmap levels will automatically be recomputed when switching away from the Canvas with love.graphics.setCanvas.
        /// </summary>
        Auto,
    }
}
