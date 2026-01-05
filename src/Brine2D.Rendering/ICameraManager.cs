using System.Collections.Generic;

namespace Brine2D.Rendering;

/// <summary>
/// Manages multiple cameras for different viewports (gameplay, minimap, split-screen, etc.).
/// </summary>
public interface ICameraManager
{
    /// <summary>
    /// Gets or sets the primary/main camera.
    /// This is the default camera used when no specific camera is specified.
    /// </summary>
    ICamera? MainCamera { get; set; }

    /// <summary>
    /// Registers a named camera.
    /// </summary>
    /// <param name="name">Unique name for the camera (e.g., "main", "minimap", "player2").</param>
    /// <param name="camera">The camera instance to register.</param>
    void RegisterCamera(string name, ICamera camera);

    /// <summary>
    /// Gets a camera by name.
    /// </summary>
    /// <param name="name">Name of the camera to retrieve.</param>
    /// <returns>The camera if found, null otherwise.</returns>
    ICamera? GetCamera(string name);

    /// <summary>
    /// Removes a camera by name.
    /// </summary>
    /// <param name="name">Name of the camera to remove.</param>
    /// <returns>True if the camera was removed, false if it didn't exist.</returns>
    bool RemoveCamera(string name);

    /// <summary>
    /// Gets all registered cameras.
    /// </summary>
    IReadOnlyDictionary<string, ICamera> GetAllCameras();

    /// <summary>
    /// Checks if a camera with the specified name exists.
    /// </summary>
    bool HasCamera(string name);
}