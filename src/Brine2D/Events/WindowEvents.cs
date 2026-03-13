namespace Brine2D.Events;

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

/// <summary>
/// Raised when the window is hidden from the user (e.g., system sleep, lock screen, or display off).
/// Rendering is suspended until <see cref="WindowShownEvent"/> is received.
/// </summary>
public record WindowHiddenEvent();

/// <summary>
/// Raised when the window becomes visible again after being hidden.
/// </summary>
public record WindowShownEvent();

