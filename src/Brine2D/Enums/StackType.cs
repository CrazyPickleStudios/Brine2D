namespace Brine2D
{
    /// <summary>
    /// Graphics state stack types used with love.graphics.push.
    /// </summary>
    // TODO: Requires Review
    public enum StackType
    {
        /// <summary>
        /// The transformation stack (love.graphics.translate, love.graphics.rotate, etc.)
        /// </summary>
        Transform,
        /// <summary>
        /// All love.graphics state, including transform state.
        /// </summary>
        All,
    }
}
