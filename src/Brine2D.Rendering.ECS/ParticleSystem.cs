using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Systems;
using Brine2D.Rendering;

namespace Brine2D.Rendering.ECS;

/// <summary>
/// System that updates and renders particle emitters.
/// Lives in Brine2D.Rendering.ECS because it's the bridge between ECS and Rendering.
/// </summary>
public class ParticleSystem : IUpdateSystem, IRenderSystem
{
    public int UpdateOrder => 250; 
    public int RenderOrder => 100;

    private readonly IEntityWorld _world;
    private readonly Random _random = new();

    public ParticleSystem(IEntityWorld world)
    {
        _world = world;
    }

    public void Update(GameTime gameTime)
    {
        var deltaTime = (float)gameTime.DeltaTime;
        var emitters = _world.GetEntitiesWithComponent<ParticleEmitterComponent>();

        foreach (var entity in emitters)
        {
            var emitter = entity.GetComponent<ParticleEmitterComponent>();
            var transform = entity.GetComponent<TransformComponent>();

            if (emitter == null || !emitter.IsEnabled)
                continue;

            // Update existing particles
            UpdateParticles(emitter, deltaTime);

            // Emit new particles
            if (emitter.IsEmitting && transform != null)
            {
                EmitParticles(emitter, transform, deltaTime);
            }
        }
    }

    private void UpdateParticles(ParticleEmitterComponent emitter, float deltaTime)
    {
        for (int i = emitter.Particles.Count - 1; i >= 0; i--)
        {
            var particle = emitter.Particles[i];

            // Update lifetime
            particle.Life -= deltaTime;

            if (particle.Life <= 0)
            {
                emitter.Particles.RemoveAt(i);
                continue;
            }

            // Apply gravity
            particle.Velocity += emitter.Gravity * deltaTime;

            // Update position
            particle.Position += particle.Velocity * deltaTime;
        }
    }

    private void EmitParticles(ParticleEmitterComponent emitter, TransformComponent transform, float deltaTime)
    {
        emitter.EmissionTimer += deltaTime;

        var particlesToEmit = (int)(emitter.EmissionRate * emitter.EmissionTimer);
        emitter.EmissionTimer -= particlesToEmit / emitter.EmissionRate;

        for (int i = 0; i < particlesToEmit; i++)
        {
            if (emitter.Particles.Count >= emitter.MaxParticles)
                break;

            // Random spawn position within radius
            var spawnPos = transform.WorldPosition + emitter.SpawnOffset;
            if (emitter.SpawnRadius > 0)
            {
                var angle = (float)(_random.NextDouble() * Math.PI * 2);
                var distance = (float)(_random.NextDouble() * emitter.SpawnRadius);
                spawnPos += new Vector2(
                    MathF.Cos(angle) * distance,
                    MathF.Sin(angle) * distance);
            }

            // Random velocity
            var baseAngle = MathF.Atan2(emitter.InitialVelocity.Y, emitter.InitialVelocity.X);
            var spreadRadians = emitter.VelocitySpread * (MathF.PI / 180f);
            var randomAngle = baseAngle + ((float)_random.NextDouble() - 0.5f) * spreadRadians;

            var speed = emitter.InitialVelocity.Length();
            var speedMult = 1f + ((float)_random.NextDouble() - 0.5f) * emitter.SpeedVariation;
            speed *= speedMult;

            var velocity = new Vector2(
                MathF.Cos(randomAngle) * speed,
                MathF.Sin(randomAngle) * speed);

            // Random lifetime
            var lifetime = emitter.ParticleLifetime;
            lifetime += ((float)_random.NextDouble() - 0.5f) * emitter.LifetimeVariation * emitter.ParticleLifetime;

            emitter.Particles.Add(new Particle
            {
                Position = spawnPos,
                Velocity = velocity,
                Life = lifetime,
                MaxLife = lifetime,
                Size = emitter.StartSize
            });
        }
    }

    public void Render(IRenderer renderer)
    {
        var emitters = _world.GetEntitiesWithComponent<ParticleEmitterComponent>();

        foreach (var entity in emitters)
        {
            var emitter = entity.GetComponent<ParticleEmitterComponent>();

            if (emitter == null || !emitter.IsEnabled)
                continue;

            foreach (var particle in emitter.Particles)
            {
                // Calculate interpolation factor (0 = start, 1 = end)
                var t = 1f - (particle.Life / particle.MaxLife);

                // Lerp color
                var color = LerpColor(emitter.StartColor, emitter.EndColor, t);

                // Lerp size
                var size = MathHelper.Lerp(emitter.StartSize, emitter.EndSize, t);

                // Draw particle as circle
                renderer.DrawCircle(particle.Position.X, particle.Position.Y, size, color);
            }
        }
    }

    private Color LerpColor(Color start, Color end, float t)
    {
        return new Color(
            (byte)MathHelper.Lerp(start.R, end.R, t),
            (byte)MathHelper.Lerp(start.G, end.G, t),
            (byte)MathHelper.Lerp(start.B, end.B, t),
            (byte)MathHelper.Lerp(start.A, end.A, t));
    }
}

/// <summary>
/// Math helper for lerping.
/// </summary>
internal static class MathHelper
{
    public static float Lerp(float a, float b, float t)
    {
        return a + (b - a) * Math.Clamp(t, 0f, 1f);
    }
}