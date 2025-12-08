namespace Brine2D.Input;

/// <summary>
///     Represents a touch input source that provides a read-only collection of touch points.
/// </summary>
public interface ITouch
{
    /// <summary>
    ///     Gets the current set of touch points captured by the touch input source.
    ///     The list is read-only and represents the current frame or state of touches.
    /// </summary>
    IReadOnlyList<TouchPoint> Points { get; }
}