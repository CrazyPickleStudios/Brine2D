namespace Brine2D
{
    /// <summary>
    /// Different types of arcs that can be drawn.
    /// </summary>
    // TODO: Requires Review
    public enum ArcType
    {
        /// <summary>
        /// The arc is drawn like a slice of pie, with the arc circle connected to the center at its end-points.
        /// </summary>
        Pie,
        /// <summary>
        /// The arc circle's two end-points are unconnected when the arc is drawn as a line. Behaves like the "closed" arc type when the arc is drawn in filled mode.
        /// </summary>
        Open,
        /// <summary>
        /// The arc circle's two end-points are connected to each other.
        /// </summary>
        Closed,
    }
}
