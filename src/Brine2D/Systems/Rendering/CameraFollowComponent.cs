using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Rendering;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// Component that makes a camera follow this entity.
/// Used by CameraSystem to control camera position.
/// </summary>
public class CameraFollowComponent : Component
{
    private string _cameraName = ICameraManager.MainCameraName;
    private float _smoothing = 5f;
    private float _zoomSmoothing = 5f;
    private Vector2 _deadzone;
    private float? _targetZoom;

    /// <summary>
    /// Name of the camera to control (default: <see cref="ICameraManager.MainCameraName"/>).
    /// Allows different entities to control different cameras (e.g., minimap, split-screen).
    /// </summary>
    public string CameraName
    {
        get => _cameraName;
        set
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(value);
            _cameraName = value;
        }
    }

    /// <summary>
    /// Follow speed. Higher = snappier. 0 = instant snap.
    /// Typical values: 2 (dreamy), 5 (responsive), 15 (tight), 0 (instant).
    /// Frame-rate independent.
    /// </summary>
    public float Smoothing
    {
        get => _smoothing;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _smoothing = value;
        }
    }

    /// <summary>
    /// Zoom smoothing speed. Higher = snappier. 0 = instant snap.
    /// Typical values: 2 (dreamy), 5 (responsive), 15 (tight), 0 (instant).
    /// Frame-rate independent. Only applied when <see cref="TargetZoom"/> is set.
    /// </summary>
    public float ZoomSmoothing
    {
        get => _zoomSmoothing;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _zoomSmoothing = value;
        }
    }

    /// <summary>
    /// Target zoom level. When set, the camera smoothly adjusts zoom using <see cref="ZoomSmoothing"/>.
    /// When <c>null</c> (default), zoom is not controlled by this component.
    /// </summary>
    public float? TargetZoom
    {
        get => _targetZoom;
        set
        {
            if (value.HasValue)
                ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value.Value);
            _targetZoom = value;
        }
    }

    /// <summary>
    /// Offset from entity position in world space.
    /// </summary>
    public Vector2 Offset { get; set; } = Vector2.Zero;

    /// <summary>
    /// Whether to follow on X axis.
    /// </summary>
    public bool FollowX { get; set; } = true;

    /// <summary>
    /// Whether to follow on Y axis.
    /// </summary>
    public bool FollowY { get; set; } = true;

    /// <summary>
    /// Deadzone (camera won't move if entity is within this distance from center).
    /// When the entity exits the deadzone, the camera follows to maintain the entity
    /// at the deadzone edge rather than snapping directly to the entity's position.
    /// Both components must be non-negative.
    /// </summary>
    public Vector2 Deadzone
    {
        get => _deadzone;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value.X, "Deadzone.X");
            ArgumentOutOfRangeException.ThrowIfNegative(value.Y, "Deadzone.Y");
            _deadzone = value;
        }
    }

    /// <summary>
    /// Whether this component participates in camera follow selection.
    /// When multiple entities are active for the same camera, the one with the highest
    /// <see cref="Priority"/> wins.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Priority (higher priority targets override lower ones if multiple are active for same camera).
    /// </summary>
    public int Priority { get; set; } = 0;

    /// <summary>
    /// Optional world boundaries to clamp the camera within after following.
    /// When set, <see cref="CameraSystem"/> calls <see cref="ICamera.ClampToBounds"/> each frame.
    /// </summary>
    public Rectangle? WorldBounds { get; set; }
}