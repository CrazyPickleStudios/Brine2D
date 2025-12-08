namespace Brine2D.Input;

/// <summary>
///     Provides read-only access to connected gamepads.
/// </summary>
public interface IGamepads
{
    /// <summary>
    ///     Gets the collection of connected gamepads as a read-only list.
    /// </summary>
    IReadOnlyList<IGamepad> Pads { get; }
}