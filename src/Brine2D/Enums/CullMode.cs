namespace Brine2D
{
    /// <summary>
    /// How Mesh geometry is culled when rendering.
    /// </summary>
    // TODO: Requires Review
    public enum CullMode
    {
        /// <summary>
        /// Back-facing triangles in Meshes are culled (not rendered). The vertex order of a triangle determines whether it is back- or front-facing.
        /// </summary>
        Back,
        /// <summary>
        /// Front-facing triangles in Meshes are culled.
        /// </summary>
        Front,
        /// <summary>
        /// Both back- and front-facing triangles in Meshes are rendered.
        /// </summary>
        None,
    }
}
