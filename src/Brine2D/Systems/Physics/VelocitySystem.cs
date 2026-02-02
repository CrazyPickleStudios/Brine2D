using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Systems;

namespace Brine2D.Systems.Physics;

/// <summary>
/// System that applies velocity to transform positions.
/// Handles basic movement and friction.
/// </summary>
public class VelocitySystem : IUpdateSystem
{
    public string Name => "VelocitySystem";
    public int UpdateOrder => 100; 

    public VelocitySystem()
    {
    }

    public void Update(GameTime gameTime, IEntityWorld world)
    {
        var deltaTime = (float)gameTime.DeltaTime;
        var entities = world.GetEntitiesWithComponents<TransformComponent, VelocityComponent>();

        foreach (var entity in entities)
        {
            var transform = entity.GetComponent<TransformComponent>();
            var velocity = entity.GetComponent<VelocityComponent>();

            if (transform == null || velocity == null || !velocity.IsEnabled)
                continue;

            // Apply velocity to position
            transform.Position += velocity.Velocity * deltaTime;

            // Apply friction
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

            // Clamp to max speed
            if (velocity.MaxSpeed > 0)
            {
                var speed = velocity.Velocity.Length();
                if (speed > velocity.MaxSpeed)
                {
                    velocity.Velocity = velocity.Velocity * (velocity.MaxSpeed / speed);
                }
            }
        }
    }
}