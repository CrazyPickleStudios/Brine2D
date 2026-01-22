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
public class CameraSystem : IUpdateSystem
{
    public int UpdateOrder => 500; 

    private readonly IEntityWorld _world;
    private readonly ICameraManager _cameraManager;

    public CameraSystem(IEntityWorld world, ICameraManager cameraManager)
    {
        _world = world;
        _cameraManager = cameraManager;
    }

    public void Update(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.DeltaTime;

        // Group targets by camera name
        var targetsByCamera = _world.GetEntitiesWithComponent<CameraFollowComponent>()
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
            var targetPos = transform.WorldPosition + follow.Offset;

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
            if (follow.Smoothing > 0)
            {
                camera.Position = Vector2.Lerp(currentPos, targetPos, follow.Smoothing * deltaTime);
            }
            else
            {
                camera.Position = targetPos;
            }
        }
    }
}