namespace Brine2D
{
    /// <summary>
    /// Different ways alpha affects color blending.
    /// </summary>
    // TODO: Requires Review
    public enum BlendAlphaMode
    {
        /// <summary>
        /// The RGB values of what's drawn are multiplied by the alpha values of those colors during blending. This is the default alpha mode.
        /// </summary>
        Alphamultiply,
        /// <summary>
        /// The RGB values of what's drawn are not multiplied by the alpha values of those colors during blending. For most blend modes to work correctly with this alpha mode, the colors of a drawn object need to have had their RGB values multiplied by their alpha values at some point previously ("premultiplied alpha").
        /// </summary>
        Premultiplied,
    }
}
