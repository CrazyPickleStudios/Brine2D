using System.Numerics;

namespace Brine2D.Rendering;

/// <summary>
/// Stateless interpolation helpers for any <see cref="ICamera"/>.
/// </summary>
public static class CameraExtensions
{
    /// <summary>
    /// Smoothly moves the camera towards a target position using exponential decay.
    /// </summary>
    /// <param name="camera">The camera to move.</param>
    /// <param name="targetPosition">Target world position.</param>
    /// <param name="smoothing">Smoothing factor. Higher = snappier. ≤ 0 = instant snap.</param>
    /// <param name="deltaTime">Time since last frame in seconds.</param>
    public static void FollowSmooth(this ICamera camera, Vector2 targetPosition, float smoothing, float deltaTime)
    {
        if (smoothing <= 0f) { camera.Position = targetPosition; return; }
        var lerpFactor = 1f - MathF.Exp(-smoothing * deltaTime);
        camera.Position = Vector2.Lerp(camera.Position, targetPosition, lerpFactor);
    }

    /// <summary>
    /// Smoothly adjusts the camera zoom towards a target value using exponential decay.
    /// </summary>
    /// <param name="camera">The camera to zoom.</param>
    /// <param name="targetZoom">Target zoom level (1.0 = normal, 2.0 = 2× zoom in).</param>
    /// <param name="smoothing">Smoothing factor. Higher = snappier. ≤ 0 = instant snap.</param>
    /// <param name="deltaTime">Time since last frame in seconds.</param>
    public static void ZoomSmooth(this ICamera camera, float targetZoom, float smoothing, float deltaTime)
    {
        if (smoothing <= 0f) { camera.Zoom = targetZoom; return; }
        var lerpFactor = 1f - MathF.Exp(-smoothing * deltaTime);
        camera.Zoom = float.Lerp(camera.Zoom, targetZoom, lerpFactor);
    }
}