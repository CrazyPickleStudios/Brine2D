namespace Brine2D
{
    /// <summary>
    /// Line join style.
    /// </summary>
    // TODO: Requires Review
    public enum LineJoin
    {
        /// <summary>
        /// The ends of the line segments beveled in an angle so that they join seamlessly.
        /// </summary>
        Miter,
        /// <summary>
        /// No cap applied to the ends of the line segments.
        /// </summary>
        None,
        /// <summary>
        /// Flattens the point where line segments join together.
        /// </summary>
        Bevel,
    }
}
