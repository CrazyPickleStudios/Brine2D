using System.Numerics;
using System.Linq;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
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

    public CameraSystem(ICameraManager cameraManager)
    {
        _cameraManager = cameraManager;
    }

    public override void Update(IEntityWorld world, GameTime gameTime)
    {
        var deltaTime = (float)gameTime.DeltaTime;

        // Advance shake on all registered cameras
        foreach (var camera in _cameraManager.GetAllCameras().Values)
            camera.UpdateShake(deltaTime);

        // Group targets by camera name
        var targetsByCamera = world.GetEntitiesWithComponent<CameraFollowComponent>() 
            .Select(e => new { Entity = e, Follow = e.GetComponent<CameraFollowComponent>() })
            .Where(x => x.Follow?.IsActive == true)
            .GroupBy(x => x.Follow!.CameraName);

        foreach (var cameraGroup in targetsByCamera)
        {
            var cameraName = cameraGroup.Key;
            var camera = _cameraManager.GetCamera(cameraName);

            if (camera == null)
                continue;

            // Find the highest priority target for this camera
            var target = cameraGroup
                .OrderByDescending(x => x.Follow!.Priority)
                .FirstOrDefault();

            if (target == null)
                continue;

            var follow = target.Follow!;
            var transform = target.Entity.GetComponent<TransformComponent>();

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