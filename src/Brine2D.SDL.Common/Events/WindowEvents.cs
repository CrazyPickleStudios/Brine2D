namespace Brine2D.SDL.Common.Events;

/// <summary>
/// Raised when the window is resized.
/// </summary>
public record WindowResizedEvent(int Width, int Height);

/// <summary>
/// Raised when the window is minimized.
/// </summary>
public record WindowMinimizedEvent();

/// <summary>
/// Raised when the window is restored from minimized state.
/// </summary>
public record WindowRestoredEvent();

/// <summary>
/// Raised when the window gains focus.
/// </summary>
public record WindowFocusGainedEvent();

/// <summary>
/// Raised when the window loses focus.
/// </summary>
public record WindowFocusLostEvent();

