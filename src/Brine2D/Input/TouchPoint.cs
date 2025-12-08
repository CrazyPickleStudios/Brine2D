namespace Brine2D.Input;

/// <summary>
///     Represents a single touch point, including its identifier and position.
/// </summary>
public readonly struct TouchPoint
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="TouchPoint" /> struct.
    /// </summary>
    /// <param name="id">A unique identifier for the touch point.</param>
    /// <param name="x">The X coordinate of the touch point.</param>
    /// <param name="y">The Y coordinate of the touch point.</param>
    public TouchPoint(long id, float x, float y)
    {
        Id = id;
        X = x;
        Y = y;
    }

    /// <summary>
    ///     Gets the unique identifier for this touch point.
    /// </summary>
    public long Id { get; }

    /// <summary>
    ///     Gets the X coordinate of this touch point.
    /// </summary>
    public float X { get; }

    /// <summary>
    ///     Gets the Y coordinate of this touch point.
    /// </summary>
    public float Y { get; }
}