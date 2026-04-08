namespace Brine2D.Rendering;

/// <summary>
/// Determines how camera shake offset is applied relative to zoom and rotation.
/// </summary>
public enum ShakeSpace
{
    /// <summary>
    /// Shake offset is applied in world space (before zoom/rotation).
    /// At higher zoom levels, the shake appears amplified on screen.
    /// </summary>
    World,

    /// <summary>
    /// Shake offset is applied in screen space (after zoom/rotation).
    /// The shake feels consistent regardless of zoom or rotation.
    /// </summary>
    Screen
}