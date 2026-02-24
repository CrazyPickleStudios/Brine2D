using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Query;
using Brine2D.ECS.Systems;

namespace Brine2D.Systems.Physics;

/// <summary>
/// System that applies velocity to entities with transform components.
/// Demonstrates performance best practice: cached query with automatic parallelization.
/// </summary>
public class VelocitySystem : UpdateSystemBase
{
    // ✅ Best practice: Cached query - reused every frame with zero allocation!
    private CachedEntityQuery<TransformComponent, VelocityComponent>? _cachedQuery;
    
    public override int UpdateOrder => SystemUpdateOrder.Physics;

    public override void Update(IEntityWorld world, GameTime gameTime)
    {
        // Lazy initialize the cached query (once per system lifetime)
        // The cache invalidates automatically when entities gain/lose components
        _cachedQuery ??= world.CreateCachedQuery<TransformComponent, VelocityComponent>()
            .Build();

        var deltaTime = (float)gameTime.DeltaTime;

        // ✅ Use cached query with ForEach for automatic parallelization
        // ✅ Uses ArrayPool for zero heap allocation
        // ✅ Component pool iteration - only touches entities with these components
        _cachedQuery.ForEach((entity, transform, velocity) =>
        {
            // Skip disabled velocity components
            if (!velocity.IsEnabled)
                return;

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
        });
    }
}