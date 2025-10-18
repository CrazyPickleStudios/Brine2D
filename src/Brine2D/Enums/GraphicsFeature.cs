namespace Brine2D
{
    /// <summary>
    /// Graphics features that can be checked for with love.graphics.getSupported.
    /// </summary>
    // TODO: Requires Review
    public enum GraphicsFeature
    {
        /// <summary>
        /// Whether the "clampzero" WrapMode is supported.
        /// </summary>
        Clampzero,
        /// <summary>
        /// Whether the "lighten" and "darken" BlendModes are supported.
        /// </summary>
        Lighten,
        /// <summary>
        /// Whether multiple Canvases with different formats can be used in the same love.graphics.setCanvas call.
        /// </summary>
        Multicanvasformats,
        /// <summary>
        /// Whether GLSL 3 Shaders can be used.
        /// </summary>
        Glsl3,
        /// <summary>
        /// Whether mesh instancing is supported.
        /// </summary>
        Instancing,
        /// <summary>
        /// Whether textures with non-power-of-two dimensions can use mipmapping and the 'repeat' WrapMode.
        /// </summary>
        Fullnpot,
        /// <summary>
        /// Whether pixel shaders can use "highp" 32 bit floating point numbers (as opposed to just 16 bit or lower precision).
        /// </summary>
        Pixelshaderhighp,
        /// <summary>
        /// Whether shaders can use the dFdx, dFdy, and fwidth functions for computing derivatives.
        /// </summary>
        Shaderderivatives,
    }
}
