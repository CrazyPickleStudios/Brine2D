namespace Brine2D
{
    /// <summary>
    /// The different types of Shapes, as returned by Shape:getType.
    /// </summary>
    // TODO: Requires Review
    public enum ShapeType
    {
        /// <summary>
        /// The Shape is a CircleShape.
        /// </summary>
        Circle,
        /// <summary>
        /// The Shape is a PolygonShape.
        /// </summary>
        Polygon,
        /// <summary>
        /// The Shape is a EdgeShape.
        /// </summary>
        Edge,
        /// <summary>
        /// The Shape is a ChainShape.
        /// </summary>
        Chain,
    }
}
