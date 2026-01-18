using System.Numerics;
using Brine2D.Core;
using Brine2D.Core.Animation;
using Brine2D.Core.Pooling;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Systems;
using Brine2D.Rendering;
using Microsoft.Extensions.ObjectPool;

namespace Brine2D.Rendering.ECS;

/// <summary>
/// System that updates and renders particle emitters.
/// Lives in Brine2D.Rendering.ECS because it's the bridge between ECS and Rendering.
/// Now supports textures, rotation, and trails!
/// </summary>
public class ParticleSystem : IUpdateSystem, IRenderSystem
{
    public int UpdateOrder => 250; 
    public int RenderOrder => 100;

    private readonly IEntityWorld _world;
    private readonly Random _random = new();
    private readonly ObjectPool<Particle> _particlePool;

    public ParticleSystem(
        IEntityWorld world, 
        ObjectPoolProvider poolProvider)
    {
        _world = world;
        
        // Create particle pool
        _particlePool = poolProvider.Create(new PoolableObjectPolicy<Particle>());
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
            particle.Life -= deltaTime;

            if (particle.Life <= 0)
            {
                // Return to pool instead of GC
                _particlePool.Return(particle);
                emitter.Particles.RemoveAt(i);
                continue;
            }

            // Apply gravity
            particle.Velocity += emitter.Gravity * deltaTime;

            // Update position
            var oldPosition = particle.Position;
            particle.Position += particle.Velocity * deltaTime;

            // Update rotation
            particle.Rotation += particle.RotationSpeed * deltaTime;

            // Update trail
            if (emitter.EnableTrails && particle.TrailPositions != null)
            {
                particle.TrailPositions[particle.TrailIndex] = oldPosition;
                particle.TrailIndex = (particle.TrailIndex + 1) % particle.TrailPositions.Length;
            }
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

            // Spawn position based on shape
            var spawnPos = transform.WorldPosition + emitter.SpawnOffset;
            spawnPos += GetSpawnOffsetForShape(emitter);

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

            // Random rotation
            var rotation = emitter.InitialRotation;
            if (emitter.InitialRotationVariation > 0)
            {
                rotation += ((float)_random.NextDouble() - 0.5f) * MathF.PI * 2f * emitter.InitialRotationVariation;
            }

            var rotationSpeed = emitter.RotationSpeed;
            if (emitter.RotationSpeedVariation > 0)
            {
                rotationSpeed += ((float)_random.NextDouble() - 0.5f) * emitter.RotationSpeed * emitter.RotationSpeedVariation;
            }

            // Get from pool instead of new
            var particle = _particlePool.Get();
            particle.Position = spawnPos;
            particle.Velocity = velocity;
            particle.Life = lifetime;
            particle.MaxLife = lifetime;
            particle.Size = emitter.StartSize;
            particle.Rotation = rotation;
            particle.RotationSpeed = rotationSpeed;

            // Initialize trail if enabled
            if (emitter.EnableTrails)
            {
                if (particle.TrailPositions == null || particle.TrailPositions.Length != emitter.TrailLength)
                {
                    particle.TrailPositions = new Vector2[emitter.TrailLength];
                }
                
                for (int j = 0; j < particle.TrailPositions.Length; j++)
                {
                    particle.TrailPositions[j] = spawnPos;
                }
                particle.TrailIndex = 0;
            }
            else
            {
                particle.TrailPositions = null;
            }
            
            emitter.Particles.Add(particle);
        }
    }

    private Vector2 GetSpawnOffsetForShape(ParticleEmitterComponent emitter)
    {
        return emitter.Shape switch
        {
            EmitterShape.Point => Vector2.Zero,
            EmitterShape.Circle => GetCircleSpawn(emitter.SpawnRadius),
            EmitterShape.Box => GetBoxSpawn(emitter.ShapeSize),
            EmitterShape.Line => GetLineSpawn(emitter.ShapeSize.X),
            EmitterShape.Cone => GetConeSpawn(emitter.SpawnRadius, emitter.InitialVelocity, emitter.ConeAngle),
            _ => Vector2.Zero
        };
    }

    private Vector2 GetCircleSpawn(float radius)
    {
        if (radius <= 0) return Vector2.Zero;
        
        var angle = (float)(_random.NextDouble() * Math.PI * 2);
        var distance = (float)(_random.NextDouble() * radius);
        
        return new Vector2(
            MathF.Cos(angle) * distance,
            MathF.Sin(angle) * distance);
    }

    private Vector2 GetBoxSpawn(Vector2 size)
    {
        return new Vector2(
            ((float)_random.NextDouble() - 0.5f) * size.X,
            ((float)_random.NextDouble() - 0.5f) * size.Y);
    }

    private Vector2 GetLineSpawn(float length)
    {
        var t = (float)_random.NextDouble();
        return new Vector2(t * length - length / 2f, 0);
    }

    private Vector2 GetConeSpawn(float radius, Vector2 direction, float coneAngleDegrees)
    {
        // Spawn within cone angle
        var baseAngle = MathF.Atan2(direction.Y, direction.X);
        var coneRadians = coneAngleDegrees * (MathF.PI / 180f);
        var angle = baseAngle + ((float)_random.NextDouble() - 0.5f) * coneRadians;
        var distance = (float)_random.NextDouble() * radius;
        
        return new Vector2(
            MathF.Cos(angle) * distance,
            MathF.Sin(angle) * distance);
    }

    public void Render(IRenderer renderer)
    {
        var emitters = _world.GetEntitiesWithComponent<ParticleEmitterComponent>();

        foreach (var entity in emitters)
        {
            var emitter = entity.GetComponent<ParticleEmitterComponent>();
            
            if (emitter is not { IsEnabled: true })
                continue;

            // Set blend mode for this emitter
            renderer.SetBlendMode(emitter.BlendMode);

            foreach (var particle in emitter.Particles)
            {
                var t = 1f - (particle.Life / particle.MaxLife);
                var color = LerpColor(emitter.StartColor, emitter.EndColor, t);
                var size = MathHelper.Lerp(emitter.StartSize, emitter.EndSize, t);

                // Render trails first (behind particle)
                if (emitter.EnableTrails && particle.TrailPositions != null)
                {
                    RenderTrail(renderer, emitter, particle, color, size);
                }

                // Render particle
                if (emitter.ParticleAtlasRegion != null)
                {
                    RenderTexturedParticle(
                        renderer,
                        emitter.ParticleAtlasRegion.AtlasTexture,
                        emitter.ParticleAtlasRegion.SourceRect,
                        particle,
                        color,
                        size);
                }
                else if (emitter.ParticleTexture != null)
                {
                    RenderTexturedParticle(
                        renderer,
                        emitter.ParticleTexture,
                        null,
                        particle,
                        color,
                        size);
                }
                else
                {
                    renderer.DrawCircleFilled(particle.Position.X, particle.Position.Y, size, color);
                }
            }
        }
        
        // Reset to default blend mode
        renderer.SetBlendMode(BlendMode.Alpha);
    }

    private void RenderTexturedParticle(
        IRenderer renderer,
        ITexture texture,
        Rectangle? sourceRect,
        Particle particle,
        Color color,
        float size)
    {
        // Calculate dimensions
        var width = sourceRect?.Width ?? texture.Width;
        var height = sourceRect?.Height ?? texture.Height;
        
        // Scale to particle size (size is radius, so diameter = size * 2)
        var scale = (size * 2f) / Math.Max(width, height);
        var renderWidth = width * scale;
        var renderHeight = height * scale;

        // Calculate position (centered on particle)
        var x = particle.Position.X - renderWidth / 2f;
        var y = particle.Position.Y - renderHeight / 2f;

        if (sourceRect.HasValue)
        {
            var src = sourceRect.Value;
            renderer.DrawTexture(
                texture,
                src.X, src.Y, src.Width, src.Height,
                x, y, renderWidth, renderHeight,
                particle.Rotation,  
                color);             
        }
        else
        {
            renderer.DrawTexture(
                texture, 
                x, y, renderWidth, renderHeight,
                particle.Rotation,  
                color);             
        }
    }

    private void RenderTrail(
        IRenderer renderer,
        ParticleEmitterComponent emitter,
        Particle particle,
        Color baseColor,
        float baseSize)
    {
        if (particle.TrailPositions == null || particle.TrailPositions.Length == 0)
            return;

        var trailLength = particle.TrailPositions.Length;
        
        for (int i = 0; i < trailLength; i++)
        {
            // Calculate which trail segment this is (0 = oldest, trailLength-1 = newest)
            var index = (particle.TrailIndex + i) % trailLength;
            var trailPos = particle.TrailPositions[index];

            // Calculate alpha fade (oldest = most transparent)
            var t = (float)i / trailLength;
            var alpha = MathHelper.Lerp(emitter.TrailEndAlpha, emitter.TrailStartAlpha, t);
            
            // Calculate size fade
            var trailSize = baseSize * (0.5f + t * 0.5f); // 50%-100% of base size

            // Create faded color
            var trailColor = new Color(
                baseColor.R,
                baseColor.G,
                baseColor.B,
                (byte)(baseColor.A * alpha));

            // Render trail segment (always as circle for now)
            renderer.DrawCircleFilled(trailPos.X, trailPos.Y, trailSize, trailColor);
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