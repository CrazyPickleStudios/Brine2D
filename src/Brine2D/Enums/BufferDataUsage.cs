namespace Brine2D
{
    /// <summary>
    /// Usage hints for SpriteBatches, Meshes, and GraphicsBuffers to optimize data storage and access.
    /// </summary>
    // TODO: Requires Review
    public enum BufferDataUsage
    {
        /// <summary>
        /// The object's data will change fairly frequently during its lifetime.
        /// </summary>
        Dynamic,
        /// <summary>
        /// The object will not be modified frequently or at all.
        /// </summary>
        Static,
        /// <summary>
        /// The object data will always change every frame.
        /// </summary>
        Stream,
    }
}
