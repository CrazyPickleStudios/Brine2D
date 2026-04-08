using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Brine2D.Rendering;

/// <summary>
/// Default implementation of camera manager.
/// Manages multiple cameras for different rendering viewports.
/// </summary>
public class CameraManager : ICameraManager
{
    private readonly Dictionary<string, ICamera> _cameras = new();
    private readonly ReadOnlyDictionary<string, ICamera> _readOnlyView;
    private ICamera? _mainCamera;
    private ICamera[] _iterationBuffer = [];

    public CameraManager()
    {
        _readOnlyView = _cameras.AsReadOnly();
    }

    public ICamera? MainCamera => _mainCamera;

    /// <summary>
    /// Registers a named camera. When <paramref name="name"/> equals
    /// <see cref="ICameraManager.MainCameraName"/>, also sets <see cref="MainCamera"/>.
    /// If the camera implements <see cref="ITrackableCamera"/>, its registration is
    /// automatically tracked for disposal-based cleanup.
    /// </summary>
    public void RegisterCamera(string name, ICamera camera)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(camera);

        _cameras[name] = camera;

        if (name == ICameraManager.MainCameraName)
            _mainCamera = camera;

        if (camera is ITrackableCamera trackable)
            trackable.TrackRegistration(this, name);
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

            if (_mainCamera == camera)
                _mainCamera = null;

            return true;
        }

        return false;
    }

    public IReadOnlyDictionary<string, ICamera> GetAllCameras() => _readOnlyView;

    public void ForEachCamera<TState>(TState state, Action<TState, ICamera> action)
    {
        var count = _cameras.Count;
        if (count == 0) return;

        if (_iterationBuffer.Length < count)
            _iterationBuffer = new ICamera[count];
        else if (count > 0 && _iterationBuffer.Length > count * 4)
            _iterationBuffer = new ICamera[count];

        var buffer = _iterationBuffer;
        _cameras.Values.CopyTo(buffer, 0);

        try
        {
            for (var i = 0; i < count; i++)
                action(state, buffer[i]);
        }
        finally
        {
            Array.Clear(buffer, 0, count);
        }
    }

    public bool HasCamera(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            return false;

        return _cameras.ContainsKey(name);
    }
}