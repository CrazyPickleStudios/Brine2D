namespace Brine2D.Rendering;

/// <summary>
/// Opt-in contract for cameras that support screen shake.
/// The built-in <see cref="Camera2D"/> implements this. Custom <see cref="ICamera"/>
/// implementations that don't need shake can skip this entirely.
/// </summary>
public interface IShakableCamera : ICamera
{
    /// <summary>
    /// Triggers a camera shake that decays over the specified duration.
    /// </summary>
    /// <param name="intensity">Maximum pixel offset at peak intensity.</param>
    /// <param name="duration">Duration in seconds before shake fully decays.</param>
    void Shake(float intensity, float duration);

    /// <summary>
    /// Immediately cancels any active camera shake and resets the shake offset.
    /// </summary>
    void CancelShake();

    /// <summary>
    /// Advances shake state. Called by <see cref="Systems.Rendering.CameraSystem"/> each frame.
    /// </summary>
    internal void UpdateShake(float deltaTime);
}