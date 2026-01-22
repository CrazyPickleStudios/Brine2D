using System.Collections.Generic;
using System.Linq;

namespace Brine2D.Rendering;

/// <summary>
/// Default implementation of camera manager.
/// Manages multiple cameras for different rendering viewports.
/// </summary>
public class CameraManager : ICameraManager
{
    private readonly Dictionary<string, ICamera> _cameras = new();
    private ICamera? _mainCamera;

    public ICamera? MainCamera
    {
        get => _mainCamera;
        set => _mainCamera = value;
    }

    public void RegisterCamera(string name, ICamera camera)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Camera name cannot be null or empty", nameof(name));

        if (camera == null)
            throw new ArgumentNullException(nameof(camera));

        _cameras[name] = camera;

        // First camera becomes main camera if not set
        if (_mainCamera == null)
        {
            _mainCamera = camera;
        }
    }

    public ICamera? GetCamera(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return null;

        return _cameras.TryGetValue(name, out var camera) ? camera : null;
    }

    public bool RemoveCamera(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        if (_cameras.TryGetValue(name, out var camera))
        {
            _cameras.Remove(name);

            // Reset main camera if it was removed
            if (_mainCamera == camera)
            {
                _mainCamera = _cameras.Values.FirstOrDefault();
            }

            return true;
        }

        return false;
    }

    public IReadOnlyDictionary<string, ICamera> GetAllCameras()
    {
        return _cameras;
    }

    public bool HasCamera(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return _cameras.ContainsKey(name);
    }
}