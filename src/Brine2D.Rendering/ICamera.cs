using System.Numerics;

namespace Brine2D.Rendering;

/// <summary>
/// Represents a 2D camera for viewing the game world.
/// </summary>
public interface ICamera
{
    /// <summary>
    /// Gets or sets the camera position in world space.
    /// </summary>
    Vector2 Position { get; set; }

    /// <summary>
    /// Gets or sets the camera zoom level (1.0 = normal, 2.0 = 2x zoom in).
    /// </summary>
    float Zoom { get; set; }

    /// <summary>
    /// Gets or sets the camera rotation in degrees.
    /// </summary>
    float Rotation { get; set; }

    /// <summary>
    /// Gets the viewport width in pixels.
    /// </summary>
    int ViewportWidth { get; }

    /// <summary>
    /// Gets the viewport height in pixels.
    /// </summary>
    int ViewportHeight { get; }

    /// <summary>
    /// Converts a world position to screen position.
    /// </summary>
    Vector2 WorldToScreen(Vector2 worldPosition);

    /// <summary>
    /// Converts a screen position to world position.
    /// </summary>
    Vector2 ScreenToWorld(Vector2 screenPosition);

    /// <summary>
    /// Gets the view matrix for transforming world coordinates to camera space.
    /// </summary>
    Matrix4x4 GetViewMatrix();

    /// <summary>
    /// Sets the viewport size (typically called when window resizes).
    /// </summary>
    void SetViewport(int width, int height);
}