namespace Brine2D
{
    /// <summary>
    /// The type of a file.
    /// </summary>
    // TODO: Requires Review
    public enum FileType
    {
        /// <summary>
        /// Regular file.
        /// </summary>
        File,
        /// <summary>
        /// Directory.
        /// </summary>
        Directory,
        /// <summary>
        /// Symbolic link.
        /// </summary>
        Symlink,
        /// <summary>
        /// Something completely different like a device.
        /// </summary>
        Other,
    }
}
