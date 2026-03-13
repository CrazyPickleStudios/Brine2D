using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Query;
using Brine2D.ECS.Systems;

namespace Brine2D.Systems.Physics;

/// <summary>
/// Applies velocity to entities that have both a <see cref="TransformComponent"/>
/// and a <see cref="VelocityComponent"/>. Supports friction and max-speed clamping.
/// </summary>
public class VelocitySystem : UpdateSystemBase
{
    private CachedEntityQuery<TransformComponent, VelocityComponent>? _cachedQuery;

    public override int UpdateOrder => SystemUpdateOrder.Physics;

    public override void Update(IEntityWorld world, GameTime gameTime)
    {
        _cachedQuery ??= world.CreateCachedQuery<TransformComponent, VelocityComponent>().Build();

        var deltaTime = (float)gameTime.DeltaTime;

        foreach (var (_, transform, velocity) in _cachedQuery)
        {
            if (!velocity.IsEnabled)
                continue;

            transform.Position += velocity.Velocity * deltaTime;

            if (velocity.Friction > 0)
            {
                var frictionAmount = velocity.Friction * deltaTime;
                var speed = velocity.Velocity.Length();

                if (speed > 0)
                {
                    var newSpeed = Math.Max(0, speed - frictionAmount);
                    velocity.Velocity = velocity.Velocity * (newSpeed / speed);
                }
            }

            if (velocity.MaxSpeed > 0)
            {
                var speed = velocity.Velocity.Length();

                if (speed > velocity.MaxSpeed)
                    velocity.Velocity = velocity.Velocity * (velocity.MaxSpeed / speed);
            }
        }
    }
}