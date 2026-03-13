using System.Numerics;
using Brine2D.ECS;
using Brine2D.Rendering;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// Component that makes a camera follow this entity.
/// Used by CameraSystem to control camera position.
/// </summary>
public class CameraFollowComponent : Component
{
    /// <summary>
    /// Name of the camera to control (default: <see cref="ICameraManager.MainCameraName"/>).
    /// Allows different entities to control different cameras (e.g., minimap, split-screen).
    /// </summary>
    public string CameraName { get; set; } = ICameraManager.MainCameraName;

    /// <summary>
    /// Follow speed. Higher = snappier. 0 = instant snap.
    /// Typical values: 2 (dreamy), 5 (responsive), 15 (tight), 0 (instant).
    /// Frame-rate independent.
    /// </summary>
    public float Smoothing { get; set; } = 5f;

    /// <summary>
    /// Zoom smoothing speed. Higher = snappier. 0 = zoom not controlled by this component.
    /// Typical values: 2 (dreamy), 5 (responsive), 15 (tight).
    /// Frame-rate independent.
    /// </summary>
    public float ZoomSmoothing { get; set; } = 0f;

    /// <summary>
    /// Target zoom level applied when <see cref="ZoomSmoothing"/> is greater than zero.
    /// </summary>
    public float TargetZoom { get; set; } = 1f;

    /// <summary>
    /// Offset from entity position (screen space).
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
    /// </summary>
    public Vector2 Deadzone { get; set; } = Vector2.Zero;

    /// <summary>
    /// Whether this is the active camera follow target.
    /// Only one entity should have this set to true per camera at a time.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Priority (higher priority targets override lower ones if multiple are active for same camera).
    /// </summary>
    public int Priority { get; set; } = 0;
}