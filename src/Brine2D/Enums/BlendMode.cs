namespace Brine2D
{
    /// <summary>
    /// Different ways to do color blending.
    /// </summary>
    // TODO: Requires Review
    public enum BlendMode
    {
        /// <summary>
        /// Alpha blending (normal). The alpha of what's drawn determines its opacity.
        /// </summary>
        Alpha,
        /// <summary>
        /// The colors of what's drawn completely replace what was on the screen, with no additional blending. The BlendAlphaMode specified in love.graphics.setBlendMode still affects what happens.
        /// </summary>
        Replace,
        /// <summary>
        /// 'Screen' blending.
        /// </summary>
        Screen,
        /// <summary>
        /// The pixel colors of what's drawn are added to the pixel colors already on the screen. The alpha of the screen is not modified.
        /// </summary>
        Add,
        /// <summary>
        /// The pixel colors of what's drawn are subtracted from the pixel colors already on the screen. The alpha of the screen is not modified.
        /// </summary>
        Subtract,
        /// <summary>
        /// The pixel colors of what's drawn are multiplied with the pixel colors already on the screen (darkening them). The alpha of drawn objects is multiplied with the alpha of the screen rather than determining how much the colors on the screen are affected, even when the "alphamultiply" BlendAlphaMode is used.
        /// </summary>
        Multiply,
        /// <summary>
        /// The pixel colors of what's drawn are compared to the existing pixel colors, and the larger of the two values for each color component is used. Only works when the "premultiplied" BlendAlphaMode is used in love.graphics.setBlendMode.
        /// </summary>
        Lighten,
        /// <summary>
        /// The pixel colors of what's drawn are compared to the existing pixel colors, and the smaller of the two values for each color component is used. Only works when the "premultiplied" BlendAlphaMode is used in love.graphics.setBlendMode.
        /// </summary>
        Darken,
    }
}
