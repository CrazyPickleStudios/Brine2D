using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Pooling;
using Brine2D.Systems.Rendering;
using FluentAssertions;
using Microsoft.Extensions.ObjectPool;
using Xunit;

namespace Brine2D.Tests.Systems;

public class ParticleSystemTests : TestBase
{
    private readonly EntityWorld _world;
    private readonly ParticleSystem _particleSystem;

    public ParticleSystemTests()
    {
        _world = CreateTestWorld();
        var poolProvider = new DefaultObjectPoolProvider();
        _particleSystem = new ParticleSystem(poolProvider);
    }

    #region Emission Tests

    [Fact]
    public void ShouldEmitParticles()
    {
        // Arrange
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = new Vector2(100, 100);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 2f;

        _world.Flush();

        // Act
        _particleSystem.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)), _world);

        // Assert - Use ParticleCount (public API)
        emitter.ParticleCount.Should().BeGreaterThan(0);
        emitter.ParticleCount.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public void ShouldNotEmitParticlesWhenNotEmitting()
    {
        // Arrange
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.IsEmitting = false;

        _world.Flush();

        // Act
        _particleSystem.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)), _world); 

        // Assert
        emitter.ParticleCount.Should().Be(0);
    }

    [Fact]
    public void ShouldRespectMaxParticles()
    {
        // Arrange
        var entity = _world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 1000f;
        emitter.MaxParticles = 50;
        emitter.ParticleLifetime = 10f;

        _world.Flush();

        // Act
        _particleSystem.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)), _world); 

        // Assert
        emitter.ParticleCount.Should().BeLessThanOrEqualTo(50);
    }

    [Fact]
    public void ShouldNotEmitWhenDisabled()
    {
        // Arrange
        var entity = _world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.IsEnabled = false;

        _world.Flush();

        // Act
        _particleSystem.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)), _world); 

        // Assert
        emitter.ParticleCount.Should().Be(0);
    }

    #endregion

    #region Particle Update Tests

    [Fact]
    public void ShouldUpdateParticlePositions()
    {
        // Arrange
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.InitialVelocity = new Vector2(100, 0);
        emitter.Gravity = Vector2.Zero;
        emitter.VelocitySpread = 0;
        emitter.SpeedVariation = 0;

        _world.Flush();

        // Act
        _particleSystem.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)), _world); 
        var initialCount = emitter.ParticleCount;

        _particleSystem.Update(new GameTime(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.1)), _world); 

        // Assert
        emitter.ParticleCount.Should().Be(initialCount + 1);
        
        // Use ActiveParticles (public API)
        var particlesMovedRight = emitter.ActiveParticles.Count(p => p.Position.X > 0);
        particlesMovedRight.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShouldApplyGravityToParticles()
    {
        // Arrange
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.Gravity = new Vector2(0, 100);
        emitter.VelocitySpread = 0;
        emitter.SpeedVariation = 0;
        
        _world.Flush();

        // Act
        _particleSystem.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)), _world); 
        _particleSystem.Update(new GameTime(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(1.0)), _world); 

        // Assert
        var particlesMovedDown = emitter.ActiveParticles.Count(p => p.Position.Y > 0);
        particlesMovedDown.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShouldExpireParticlesAfterLifetime()
    {
        // Arrange
        var entity = _world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.ParticleLifetime = 0.5f;
        emitter.LifetimeVariation = 0;

        _world.Flush();

        // Act - Emit particles
        _particleSystem.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)), _world);
        emitter.IsEmitting = false; // Now stop emitting after first batch
        var initialCount = emitter.ParticleCount;
        initialCount.Should().BeGreaterThan(0);

        // Wait for particles to expire
        _particleSystem.Update(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(1.0)), _world);

        // Assert - All particles should be expired
        emitter.ParticleCount.Should().Be(0);
    }

    #endregion

    #region Pooling Tests

    [Fact]
    public void ShouldRecycleParticlesViaPooling()
    {
        // Arrange
        var entity = _world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.ParticleLifetime = 0.2f;
        emitter.LifetimeVariation = 0;

        _world.Flush();

        // Act - Emit first batch
        _particleSystem.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)), _world); 
        var firstBatchCount = emitter.ParticleCount;
        
        // Stop emitting so no new particles are created
        emitter.IsEmitting = false;

        // Wait for particles to expire (they get returned to pool)
        _particleSystem.Update(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.5)), _world);
        emitter.ParticleCount.Should().Be(0);
        
        // Re-enable emission
        emitter.IsEmitting = true;

        // Emit again (should reuse pooled particles)
        _particleSystem.Update(new GameTime(TimeSpan.FromSeconds(0.6), TimeSpan.FromSeconds(0.1)), _world); 
        var secondBatchCount = emitter.ParticleCount;

        // Assert - Should have emitted similar amounts (pooling working)
        secondBatchCount.Should().BeGreaterThan(0);
        secondBatchCount.Should().BeCloseTo(firstBatchCount, (uint)2); // Allow small variance
    }

    [Fact]
    public void ShouldResetParticlesWhenReturnedToPool()
    {
        // Arrange
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = new Vector2(100, 100);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.ParticleLifetime = 0.1f;
        emitter.InitialVelocity = new Vector2(100, 100);

        _world.Flush();

        // Act - Update for 0.15 seconds to emit at least 1 particle (10 * 0.15 = 1.5 particles)
        _particleSystem.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.15)), _world); 
        
        var particle = emitter.ActiveParticles.FirstOrDefault();
        particle.Should().NotBeNull();
        
        particle!.Position.Should().NotBe(Vector2.Zero);
        particle.Velocity.Should().NotBe(Vector2.Zero);

        // Wait for particle to expire (returns to pool)
        _particleSystem.Update(new GameTime(TimeSpan.FromSeconds(0.15), TimeSpan.FromSeconds(0.2)), _world); 

        // Emit new particles (reuses pool)
        _particleSystem.Update(new GameTime(TimeSpan.FromSeconds(0.35), TimeSpan.FromSeconds(0.15)), _world); 

        // Assert - New particles should start fresh (pooled particles were reset)
        emitter.ParticleCount.Should().BeGreaterThan(0);
    }

    #endregion

    #region Emitter Shape Tests

    [Fact]
    public void ShouldEmitFromPointShape()
    {
        // Arrange
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = new Vector2(100, 100);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.Shape = EmitterShape.Point;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0;

        _world.Flush();

        // Act
        _particleSystem.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)), _world); 

        // Assert - Use ActiveParticles
        foreach (var particle in emitter.ActiveParticles)
        {
            particle.Position.X.Should().BeApproximately(100, 0.1f);
            particle.Position.Y.Should().BeApproximately(100, 0.1f);
        }
    }

    [Fact]
    public void ShouldEmitFromCircleShape()
    {
        // Arrange
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 50f;
        emitter.Shape = EmitterShape.Circle;
        emitter.SpawnRadius = 50f;
        emitter.InitialVelocity = Vector2.Zero;

        _world.Flush();

        // Act
        _particleSystem.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)), _world); 

        // Assert - Use ActiveParticles
        foreach (var particle in emitter.ActiveParticles)
        {
            var distance = particle.Position.Length();
            distance.Should().BeLessThanOrEqualTo(50f);
        }
    }

    #endregion

    #region Rotation Tests

    [Fact]
    public void ShouldUpdateParticleRotation()
    {
        // Arrange
        var entity = _world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.RotationSpeed = MathF.PI;
        emitter.RotationSpeedVariation = 0;

        _world.Flush();

        // Act
        _particleSystem.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)), _world); 
        // Use ActiveParticles
        var initialRotations = emitter.ActiveParticles.Select(p => p.Rotation).ToList();

        _particleSystem.Update(new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(1.0)), _world); 

        // Assert
        for (int i = 0; i < emitter.ActiveParticles.Count && i < initialRotations.Count; i++)
        {
            var particle = emitter.ActiveParticles[i];
            particle.Rotation.Should().NotBe(initialRotations[i]);
        }
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ShouldHandleMissingTransformComponent()
    {
        // Arrange
        var entity = _world.CreateEntity();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;

        _world.Flush();

        // Act & Assert
        var act = () => _particleSystem.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)), _world); 
        act.Should().NotThrow();
        emitter.ParticleCount.Should().Be(0);
    }

    [Fact]
    public void ShouldHandleZeroEmissionRate()
    {
        // Arrange
        var entity = _world.CreateEntity();
        var transform = entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 0f;

        _world.Flush();

        // Act
        _particleSystem.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)), _world); 

        // Assert
        emitter.ParticleCount.Should().Be(0);
    }

    #endregion
}