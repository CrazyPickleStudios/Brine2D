namespace Brine2D
{
    /// <summary>
    /// How a Mesh's vertices are used when drawing.
    /// </summary>
    // TODO: Requires Review
    public enum MeshDrawMode
    {
        /// <summary>
        /// The vertices create a "fan" shape with the first vertex acting as the hub point. Can be easily used to draw simple convex polygons.
        /// </summary>
        Fan,
        /// <summary>
        /// The vertices create a series of connected triangles using vertices 1, 2, 3, then 3, 2, 4 (note the order), then 3, 4, 5, and so on.
        /// </summary>
        Strip,
        /// <summary>
        /// The vertices create unconnected triangles.
        /// </summary>
        Triangles,
        /// <summary>
        /// The vertices are drawn as unconnected points (see love.graphics.setPointSize.)
        /// </summary>
        Points,
    }
}
