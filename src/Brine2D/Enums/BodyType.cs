namespace Brine2D
{
    /// <summary>
    /// The types of a Body.
    /// </summary>
    // TODO: Requires Review
    public enum BodyType
    {
        /// <summary>
        /// Static bodies do not move.
        /// </summary>
        Static,
        /// <summary>
        /// Dynamic bodies collide with all bodies.
        /// </summary>
        Dynamic,
        /// <summary>
        /// Kinematic bodies only collide with dynamic bodies.
        /// </summary>
        Kinematic,
    }
}
