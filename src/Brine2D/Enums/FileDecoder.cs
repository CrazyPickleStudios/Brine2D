namespace Brine2D
{
    /// <summary>
    /// How to decode a given FileData.
    /// </summary>
    // TODO: Requires Review
    public enum FileDecoder
    {
        /// <summary>
        /// The data is unencoded.
        /// </summary>
        File,
        /// <summary>
        /// The data is base64-encoded.
        /// </summary>
        Base64,
    }
}
