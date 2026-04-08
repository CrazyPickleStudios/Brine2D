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
/// Bridge between ECS and Rendering - lives in Brine2D.Systems.Rendering.
/// </summary>
public class CameraSystem : UpdateSystemBase, IDisposable
{
    public override int UpdateOrder => 500;

    private readonly ICameraManager _cameraManager;
    private WeakReference<IEntityWorld>? _queryWorld;
    private CachedEntityQuery<CameraFollowComponent>? _followQuery;
    private readonly Dictionary<string, (Entity Entity, CameraFollowComponent Follow)> _bestTargets = new();

    public CameraSystem(ICameraManager cameraManager)
    {
        _cameraManager = cameraManager;
    }

    public override void Update(IEntityWorld world, GameTime gameTime)
    {
        if (_followQuery == null ||
            !_queryWorld!.TryGetTarget(out var cachedWorld) ||
            cachedWorld != world)
        {
            _followQuery?.Dispose();
            _followQuery = world.CreateCachedQuery<CameraFollowComponent>().Build();
            _queryWorld = new WeakReference<IEntityWorld>(world);
        }

        var deltaTime = (float)gameTime.DeltaTime;

        _cameraManager.ForEachCamera(deltaTime, static (dt, camera) =>
        {
            if (camera is IShakableCamera shakable)
                shakable.UpdateShake(dt);
        });

        _bestTargets.Clear();
        foreach (var (entity, follow) in _followQuery)
        {
            if (!follow.IsActive) continue;
            if (!_bestTargets.TryGetValue(follow.CameraName, out var current) ||
                follow.Priority > current.Follow.Priority ||
                (follow.Priority == current.Follow.Priority && entity.Id < current.Entity.Id))
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

            var targetPos = transform.Position + follow.Offset;

            var currentPos = camera.Position;
            var delta = targetPos - currentPos;

            if (Math.Abs(delta.X) < follow.Deadzone.X)
                targetPos.X = currentPos.X;
            else
                targetPos.X -= Math.Sign(delta.X) * follow.Deadzone.X;

            if (Math.Abs(delta.Y) < follow.Deadzone.Y)
                targetPos.Y = currentPos.Y;
            else
                targetPos.Y -= Math.Sign(delta.Y) * follow.Deadzone.Y;

            if (!follow.FollowX)
                targetPos.X = currentPos.X;
            if (!follow.FollowY)
                targetPos.Y = currentPos.Y;

            camera.FollowSmooth(targetPos, follow.Smoothing, deltaTime);

            if (follow.TargetZoom is { } targetZoom)
                camera.ZoomSmooth(targetZoom, follow.ZoomSmoothing, deltaTime);

            if (follow.WorldBounds is { } bounds)
                camera.ClampToBounds(bounds);
        }
    }

    public void Dispose()
    {
        _followQuery?.Dispose();
        _followQuery = null;
        _queryWorld = null;
    }
}