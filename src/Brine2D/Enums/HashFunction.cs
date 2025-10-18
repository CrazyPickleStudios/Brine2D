namespace Brine2D
{
    /// <summary>
    /// Hash algorithm of hash function.
    /// </summary>
    // TODO: Requires Review
    public enum HashFunction
    {
        /// <summary>
        /// MD5 hash algorithm (16 bytes).
        /// </summary>
        Md5,
        /// <summary>
        /// SHA1 hash algorithm (20 bytes).
        /// </summary>
        Sha1,
        /// <summary>
        /// SHA2 hash algorithm with message digest size of 224 bits (28 bytes).
        /// </summary>
        Sha224,
        /// <summary>
        /// SHA2 hash algorithm with message digest size of 256 bits (32 bytes).
        /// </summary>
        Sha256,
        /// <summary>
        /// SHA2 hash algorithm with message digest size of 384 bits (48 bytes).
        /// </summary>
        Sha384,
        /// <summary>
        /// SHA2 hash algorithm with message digest size of 512 bits (64 bytes).
        /// </summary>
        Sha512,
    }
}
