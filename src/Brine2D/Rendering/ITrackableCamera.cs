namespace Brine2D.Rendering;

/// <summary>
/// Opt-in contract for cameras that need to clean up their registration with
/// <see cref="ICameraManager"/> when their DI scope is torn down.
/// The built-in <see cref="Camera2D"/> implements this. Custom <see cref="ICamera"/>
/// implementations that want automatic unregistration should implement this interface.
/// </summary>
public interface ITrackableCamera
{
    /// <summary>
    /// Records the <paramref name="cameraManager"/> and <paramref name="key"/> this camera
    /// was registered under so it can unregister itself on disposal.
    /// Called automatically by <see cref="ICameraManager.RegisterCamera"/> when the camera
    /// implements this interface.
    /// </summary>
    void TrackRegistration(ICameraManager cameraManager, string key);
}