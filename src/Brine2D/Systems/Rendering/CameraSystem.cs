using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Query;
using Brine2D.ECS.Systems;
using Brine2D.Rendering;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// System that controls cameras to follow entities.
/// Bridge between ECS and Rendering - lives in Brine2D.Rendering.ECS.
/// </summary>
public class CameraSystem : UpdateSystemBase
{
    public string Name => "CameraSystem"; 
    public int UpdateOrder => 500; 

    private readonly ICameraManager _cameraManager;
    private CachedEntityQuery<CameraFollowComponent>? _followQuery;
    private readonly Dictionary<string, (Entity Entity, CameraFollowComponent Follow)> _bestTargets = new();

    public CameraSystem(ICameraManager cameraManager)
    {
        _cameraManager = cameraManager;
    }

    public override void Update(IEntityWorld world, GameTime gameTime)
    {
        _followQuery ??= world.CreateCachedQuery<CameraFollowComponent>().Build();
        var deltaTime = (float)gameTime.DeltaTime;

        foreach (var camera in _cameraManager.GetAllCameras().Values)
            camera.UpdateShake(deltaTime);

        _bestTargets.Clear();
        foreach (var (entity, follow) in _followQuery)
        {
            if (!follow.IsActive) continue;
            if (!_bestTargets.TryGetValue(follow.CameraName, out var current) ||
                follow.Priority > current.Follow.Priority)
                _bestTargets[follow.CameraName] = (entity, follow);
        }

        foreach (var (cameraName, (entity, follow)) in _bestTargets)
        {
            var camera = _cameraManager.GetCamera(cameraName);

            if (camera == null)
                continue;

            var transform = entity.GetComponent<TransformComponent>();

            if (transform == null)
                continue;

            // Calculate target position
            var targetPos = transform.Position + follow.Offset;

            // Apply deadzone
            var currentPos = camera.Position;
            var delta = targetPos - currentPos;

            if (Math.Abs(delta.X) < follow.Deadzone.X)
                targetPos.X = currentPos.X;
            if (Math.Abs(delta.Y) < follow.Deadzone.Y)
                targetPos.Y = currentPos.Y;

            // Apply follow constraints
            if (!follow.FollowX)
                targetPos.X = currentPos.X;
            if (!follow.FollowY)
                targetPos.Y = currentPos.Y;

            // Smoothly move camera
            // Frame-rate independent exponential decay.
            // Smoothing=5 reaches 99% of target in ~0.9s regardless of framerate.
            var lerpFactor = follow.Smoothing > 0f
                ? 1f - MathF.Exp(-follow.Smoothing * deltaTime)
                : 1f;
            camera.Position = Vector2.Lerp(currentPos, targetPos, lerpFactor);

            // Smoothly adjust zoom (only when opted in via ZoomSmoothing > 0)
            if (follow.ZoomSmoothing > 0f)
                camera.ZoomSmooth(follow.TargetZoom, follow.ZoomSmoothing, deltaTime);
        }
    }
}