using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Systems;
using System.Numerics;
using FeatureDemos.Components; // Add this

namespace FeatureDemos.Scenes.Performance;

public class BenchmarkSystem : UpdateSystemBase
{
    public string Name => "BenchmarkSystem";
    public int UpdateOrder => 50;
    
    public override void Update(IEntityWorld world, GameTime gameTime)
    {
        var deltaTime = (float)gameTime.DeltaTime;
        
        // Intentional CPU load; shows up in the F4 system profiler
        world.Query()
            .With<TransformComponent>()
            .With<MovementComponent>()
            .ForEach((Entity entity, TransformComponent transform, MovementComponent movement) =>
            {
                var result = 0.0;
                for (int i = 0; i < 1000; i++)
                {
                    result += Math.Sin(transform.Position.X + i) * Math.Cos(transform.Position.Y + i);
                }
                
                transform.Position += movement.Velocity * deltaTime * (float)result * 0.0001f;
                
                if (transform.Position.X < -2000 || transform.Position.X > 3280)
                    movement.Velocity = new Vector2(-movement.Velocity.X, movement.Velocity.Y);
                
                if (transform.Position.Y < -2000 || transform.Position.Y > 2720)
                    movement.Velocity = new Vector2(movement.Velocity.X, -movement.Velocity.Y);
            });
    }
}