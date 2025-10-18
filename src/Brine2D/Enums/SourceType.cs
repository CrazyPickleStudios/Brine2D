namespace Brine2D
{
    /// <summary>
    /// Types of audio sources.
    /// </summary>
    // TODO: Requires Review
    public enum SourceType
    {
        /// <summary>
        /// The whole audio is decoded.
        /// </summary>
        Static,
        /// <summary>
        /// The audio is decoded in chunks when needed.
        /// </summary>
        Stream,
        /// <summary>
        /// The audio must be manually queued by the user.
        /// </summary>
        Queue,
    }
}
