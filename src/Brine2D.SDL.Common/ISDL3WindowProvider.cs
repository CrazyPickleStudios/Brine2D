namespace Brine2D.SDL.Common;

/// <summary>
/// Provides access to the current SDL3 window handle.
/// Implementations must ensure the window handle is always current.
/// </summary>
public interface ISDL3WindowProvider
{
    /// <summary>
    /// Gets the current SDL3 window handle.
    /// Returns nint.Zero if window is not yet created.
    /// </summary>
    nint Window { get; }
}