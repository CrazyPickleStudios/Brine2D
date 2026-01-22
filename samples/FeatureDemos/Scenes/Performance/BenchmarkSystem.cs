using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Systems;
using Brine2D.Systems.Physics;
using System.Numerics;

namespace FeatureDemos.Scenes.Performance;

public class BenchmarkSystem : IUpdateSystem
{
    private readonly IEntityWorld _world;
    
    public string Name => "BenchmarkSystem";
    public int UpdateOrder => 50;
    
    public BenchmarkSystem(IEntityWorld world)
    {
        _world = world;
    }
    
    public void Update(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.DeltaTime;
        
        // Heavy CPU workload - now it will show in F4!
        _world.Query()
            .With<TransformComponent>()
            .With<VelocityComponent>()
            .ForEach((Entity entity, TransformComponent transform, VelocityComponent velocity) =>
            {
                var result = 0.0;
                for (int i = 0; i < 1000; i++)
                {
                    result += Math.Sin(transform.Position.X + i) * Math.Cos(transform.Position.Y + i);
                }
                
                transform.Position += velocity.Velocity * deltaTime * (float)result * 0.0001f;
                
                if (transform.Position.X < -2000 || transform.Position.X > 3280)
                    velocity.Velocity = new Vector2(-velocity.Velocity.X, velocity.Velocity.Y);
                
                if (transform.Position.Y < -2000 || transform.Position.Y > 2720)
                    velocity.Velocity = new Vector2(velocity.Velocity.X, -velocity.Velocity.Y);
            });
    }
}