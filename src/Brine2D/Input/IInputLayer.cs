namespace Brine2D.Input;

/// <summary>
/// Represents a layer that can intercept and consume input events.
/// Layers are processed in priority order (UI typically has highest priority).
/// Every layer is always called each frame; the <c>consumed</c> parameter indicates
/// whether a higher-priority layer already claimed that input category.
/// Layers receiving <c>consumed: true</c> should skip gameplay logic but may
/// perform bookkeeping (e.g., canceling a drag or resetting hover state).
/// </summary>
public interface IInputLayer
{
    /// <summary>
    /// Priority for input processing (higher = processed first).
    /// UI typically uses 1000, game uses 0.
    /// </summary>
    int Priority { get; }

    /// <summary>
    /// Process keyboard input. Return true to consume and prevent lower layers from acting on it.
    /// </summary>
    /// <param name="input">The input context for the current frame.</param>
    /// <param name="consumed">True if a higher-priority layer already consumed keyboard input.</param>
    bool ProcessKeyboardInput(IInputContext input, bool consumed);

    /// <summary>
    /// Process mouse input. Return true to consume and prevent lower layers from acting on it.
    /// </summary>
    /// <param name="input">The input context for the current frame.</param>
    /// <param name="consumed">True if a higher-priority layer already consumed mouse input.</param>
    bool ProcessMouseInput(IInputContext input, bool consumed);

    /// <summary>
    /// Process gamepad input. Return true to consume and prevent lower layers from acting on it.
    /// Defaults to <see langword="false"/>; override when a layer needs to block gamepad input
    /// (e.g., a modal dialog that should intercept confirm/cancel buttons).
    /// </summary>
    /// <param name="input">The input context for the current frame.</param>
    /// <param name="consumed">True if a higher-priority layer already consumed gamepad input.</param>
    bool ProcessGamepadInput(IInputContext input, bool consumed) => false;
}