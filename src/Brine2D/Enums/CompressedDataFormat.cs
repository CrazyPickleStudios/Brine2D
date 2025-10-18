namespace Brine2D
{
    /// <summary>
    /// Compressed data formats.
    /// </summary>
    // TODO: Requires Review
    public enum CompressedDataFormat
    {
        /// <summary>
        /// The LZ4 compression format. Compresses and decompresses very quickly, but the compression ratio is not the best. LZ4-HC is used when compression level 9 is specified. Some benchmarks are available here.
        /// </summary>
        Lz4,
        /// <summary>
        /// The zlib format is DEFLATE-compressed data with a small bit of header data. Compresses relatively slowly and decompresses moderately quickly, and has a decent compression ratio.
        /// </summary>
        Zlib,
        /// <summary>
        /// The gzip format is DEFLATE-compressed data with a slightly larger header than zlib. Since it uses DEFLATE it has the same compression characteristics as the zlib format.
        /// </summary>
        Gzip,
        /// <summary>
        /// Raw DEFLATE-compressed data (no header).
        /// </summary>
        Deflate,
    }
}
