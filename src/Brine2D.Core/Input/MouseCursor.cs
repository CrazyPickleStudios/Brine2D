namespace Brine2D.Core.Input;

/// <summary>
///     Defines standard mouse cursor types used by Brine2D across platforms.
/// </summary>
/// <remarks>
///     Values map to typical OS cursor shapes; actual appearance may vary by platform and theme.
/// </remarks>
public enum MouseCursor
{
    /// <summary>
    ///     Inherit the platform's default cursor for the current context.
    /// </summary>
    Default = 0,

    /// <summary>
    ///     Standard arrow pointer.
    /// </summary>
    Arrow = 1,

    /// <summary>
    ///     Text selection (I-beam) cursor.
    /// </summary>
    IBeam,

    /// <summary>
    ///     Precision selection crosshair.
    /// </summary>
    Crosshair,

    /// <summary>
    ///     Hand pointer, typically used for links or clickable items.
    /// </summary>
    Hand,

    /// <summary>
    ///     Vertical resize (north-south).
    /// </summary>
    ResizeNS,

    /// <summary>
    ///     Horizontal resize (west-east).
    /// </summary>
    ResizeWE,

    /// <summary>
    ///     Diagonal resize (northwest-southeast).
    /// </summary>
    ResizeNWSE,

    /// <summary>
    ///     Diagonal resize (northeast-southwest).
    /// </summary>
    ResizeNESW,

    /// <summary>
    ///     Move cursor (all directions).
    /// </summary>
    ResizeAll,

    /// <summary>
    ///     Operation not allowed (prohibited/no-drop).
    /// </summary>
    NotAllowed,

    /// <summary>
    ///     Busy/wait cursor (hourglass/spinner).
    /// </summary>
    Wait
}