namespace Brine2D.Mouse;

/// <summary>
///     Types of hardware cursors.
/// </summary>
public enum CursorType
{
    /// <summary>
    ///     The cursor is using a custom image.
    /// </summary>
    Image,

    /// <summary>
    ///     An arrow pointer.
    /// </summary>
    Arrow,

    /// <summary>
    ///     An I-beam, normally used when mousing over editable or selectable text.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    IBeam,

    /// <summary>
    ///     Wait graphic.
    /// </summary>
    Wait,

    /// <summary>
    ///     Small wait cursor with an arrow pointer.
    /// </summary>
    WaitArrow,

    /// <summary>
    ///     Crosshair symbol.
    /// </summary>
    Crosshair,

    /// <summary>
    ///     Double arrow pointing to the top-left and bottom-right.
    /// </summary>
    // ReSharper disable once IdentifierTypo
    // ReSharper disable once InconsistentNaming
    SizeNWSE,

    /// <summary>
    ///     Double arrow pointing to the top-right and bottom-left.
    /// </summary>
    // ReSharper disable once IdentifierTypo
    // ReSharper disable once InconsistentNaming
    SizeNESW,

    /// <summary>
    ///     Double arrow pointing left and right.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    SizeWE,

    /// <summary>
    ///     Double arrow pointing up and down.
    /// </summary>
    // ReSharper disable once InconsistentNaming
    SizeNS,

    /// <summary>
    ///     Four-pointed arrow pointing up, down, left, and right.
    /// </summary>
    SizeAll,

    /// <summary>
    ///     Slashed circle or crossbones.
    /// </summary>
    No,

    /// <summary>
    ///     Hand symbol.
    /// </summary>
    Hand
}