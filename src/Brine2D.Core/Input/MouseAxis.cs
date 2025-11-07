namespace Brine2D.Core.Input;

/// <summary>
///     Identifies mouse-related axes for relative movement and wheel scrolling per frame.
/// </summary>
/// <remarks>
///     Move axes represent relative cursor motion since the previous frame.
///     Wheel axes represent scroll deltas since the previous frame. Horizontal wheel may be zero on devices that do not
///     support it.
/// </remarks>
public enum MouseAxis
{
    /// <summary>
    ///     Horizontal mouse movement delta for the current frame.
    /// </summary>
    /// <seealso cref="IMouse.DeltaX" />
    MoveX,

    /// <summary>
    ///     Vertical mouse movement delta for the current frame.
    /// </summary>
    /// <seealso cref="IMouse.DeltaY" />
    MoveY,

    /// <summary>
    ///     Horizontal scroll wheel delta for the current frame.
    /// </summary>
    /// <remarks>May be zero on devices without a horizontal scroll wheel.</remarks>
    /// <seealso cref="IMouse.WheelX" />
    WheelX,

    /// <summary>
    ///     Vertical scroll wheel delta for the current frame.
    /// </summary>
    /// <seealso cref="IMouse.WheelY" />
    WheelY
}