namespace Brine2D
{
    /// <summary>
    /// The different modes you can open a File in.
    /// </summary>
    // TODO: Requires Review
    public enum FileMode
    {
        /// <summary>
        /// Open a file for read.
        /// </summary>
        R,
        /// <summary>
        /// Open a file for write.
        /// </summary>
        W,
        /// <summary>
        /// Open a file for append.
        /// </summary>
        A,
        /// <summary>
        /// Do not open a file (represents a closed file.)
        /// </summary>
        C,
    }
}
