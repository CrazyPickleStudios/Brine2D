namespace Brine2D.Input;

/// <summary>
/// Represents a layer that can intercept and consume input events.
/// Layers are processed in priority order (UI typically has highest priority).
/// </summary>
public interface IInputLayer
{
    /// <summary>
    /// Priority for input processing (higher = processed first).
    /// UI typically uses 1000, game uses 0.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Process keyboard input. Return true to consume the event and prevent lower layers from receiving it.
    /// </summary>
    bool ProcessKeyboardInput(IInputContext input);

    /// <summary>
    /// Process mouse input. Return true to consume the event and prevent lower layers from receiving it.
    /// </summary>
    bool ProcessMouseInput(IInputContext input);
}