using System.Numerics;

namespace Brine2D.Rendering;

/// <summary>
/// Constrains camera movement to stay within world bounds.
/// </summary>
public class CameraBounds
{
    public float MinX { get; set; }
    public float MaxX { get; set; }
    public float MinY { get; set; }
    public float MaxY { get; set; }

    public CameraBounds(float minX, float minY, float maxX, float maxY)
    {
        MinX = minX;
        MinY = minY;
        MaxX = maxX;
        MaxY = maxY;
    }

    /// <summary>
    /// Clamps the camera position to stay within bounds.
    /// </summary>
    public Vector2 ClampPosition(Vector2 position, ICamera camera)
    {
        var halfWidth = camera.ViewportWidth / (2f * camera.Zoom);
        var halfHeight = camera.ViewportHeight / (2f * camera.Zoom);

        var clampedX = Math.Clamp(position.X, MinX + halfWidth, MaxX - halfWidth);
        var clampedY = Math.Clamp(position.Y, MinY + halfHeight, MaxY - halfHeight);

        return new Vector2(clampedX, clampedY);
    }
}