namespace Brine2D
{
    /// <summary>
    /// Types of textures (2D, cubemap, etc.)
    /// </summary>
    // TODO: Requires Review
    public enum TextureType
    {
        /// <summary>
        /// Regular 2D texture with width and height.
        /// </summary>
        TwoD,
        /// <summary>
        /// Several same-size 2D textures organized into a single object. Similar to a texture atlas / sprite sheet, but avoids sprite bleeding and other issues.
        /// </summary>
        Array,
        /// <summary>
        /// Cubemap texture with 6 faces. Requires a custom shader (and Shader:send) to use. Sampling from a cube texture in a shader takes a 3D direction vector instead of a texture coordinate.
        /// </summary>
        Cube,
        /// <summary>
        /// 3D texture with width, height, and depth. Requires a custom shader to use. Volume textures can have texture filtering applied along the 3rd axis.
        /// </summary>
        Volume,
    }
}
