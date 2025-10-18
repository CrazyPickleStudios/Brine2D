namespace Brine2D
{
    /// <summary>
    /// Types of hardware cursors.
    /// </summary>
    // TODO: Requires Review
    public enum CursorType
    {
        /// <summary>
        /// The cursor is using a custom image.
        /// </summary>
        Image,
        /// <summary>
        /// An arrow pointer.
        /// </summary>
        Arrow,
        /// <summary>
        /// An I-beam, normally used when mousing over editable or selectable text.
        /// </summary>
        Ibeam,
        /// <summary>
        /// Wait graphic.
        /// </summary>
        Wait,
        /// <summary>
        /// Small wait cursor with an arrow pointer.
        /// </summary>
        Waitarrow,
        /// <summary>
        /// Crosshair symbol.
        /// </summary>
        Crosshair,
        /// <summary>
        /// Double arrow pointing to the top-left and bottom-right.
        /// </summary>
        Sizenwse,
        /// <summary>
        /// Double arrow pointing to the top-right and bottom-left.
        /// </summary>
        Sizenesw,
        /// <summary>
        /// Double arrow pointing left and right.
        /// </summary>
        Sizewe,
        /// <summary>
        /// Double arrow pointing up and down.
        /// </summary>
        Sizens,
        /// <summary>
        /// Four-pointed arrow pointing up, down, left, and right.
        /// </summary>
        Sizeall,
        /// <summary>
        /// Slashed circle or crossbones.
        /// </summary>
        No,
        /// <summary>
        /// Hand symbol.
        /// </summary>
        Hand,
    }
}
