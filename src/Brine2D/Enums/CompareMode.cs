namespace Brine2D
{
    /// <summary>
    /// Different types of stencil test and depth test comparisons.
    /// </summary>
    // TODO: Requires Review
    public enum CompareMode
    {
        /// <summary>
        /// 
        /// </summary>
        Equal,
        /// <summary>
        /// 
        /// </summary>
        Notequal,
        /// <summary>
        /// 
        /// </summary>
        Less,
        /// <summary>
        /// 
        /// </summary>
        Lequal,
        /// <summary>
        /// 
        /// </summary>
        Gequal,
        /// <summary>
        /// 
        /// </summary>
        Greater,
        /// <summary>
        /// Objects will never be drawn.
        /// </summary>
        Never,
        /// <summary>
        /// Objects will always be drawn. Effectively disables the depth or stencil test.
        /// </summary>
        Always,
    }
}
