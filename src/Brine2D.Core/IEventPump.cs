namespace Brine2D.Core;

/// <summary>
/// Platform event pump abstraction.
/// Processes platform-specific events (window, input, etc.) and routes them to the event bus.
/// </summary>
public interface IEventPump
{
    /// <summary>
    /// Processes all pending platform events for this frame.
    /// Should be called once per frame, early in the game loop.
    /// </summary>
    void ProcessEvents();
}