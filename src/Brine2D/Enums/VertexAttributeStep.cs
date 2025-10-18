namespace Brine2D
{
    /// <summary>
    /// The frequency at which a vertex shader fetches the vertex attribute's data from the Mesh when it's drawn.
    /// </summary>
    // TODO: Requires Review
    public enum VertexAttributeStep
    {
        /// <summary>
        /// The vertex attribute will have a unique value for each vertex in the Mesh within a single instance.
        /// </summary>
        Pervertex,
        /// <summary>
        /// The vertex attribute will have a unique value for each instance of the Mesh.
        /// </summary>
        Perinstance,
    }
}
