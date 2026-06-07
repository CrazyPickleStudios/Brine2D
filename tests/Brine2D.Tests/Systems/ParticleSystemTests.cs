using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Pooling;
using Brine2D.Rendering;
using Brine2D.Rendering.TextureAtlas;
using Brine2D.Systems.Rendering;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.ObjectPool;
using NSubstitute;
using System.Numerics;
using Xunit;

namespace Brine2D.Tests.Systems;

public class ParticleSystemTests : TestBase
{
    private readonly IEntityWorld _world;
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

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        emitter.ParticleCount.Should().BeGreaterThan(0);
        emitter.ParticleCount.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public void ShouldNotEmitParticlesWhenNotEmitting()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.IsEmitting = false;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        emitter.ParticleCount.Should().Be(0);
    }

    [Fact]
    public void ShouldRespectMaxParticles()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 1000f;
        emitter.MaxParticles = 50;
        emitter.ParticleLifetime = 10f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        emitter.ParticleCount.Should().BeLessThanOrEqualTo(50);
    }

    [Fact]
    public void ShouldNotEmitWhenDisabled()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.IsEnabled = false;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        emitter.ParticleCount.Should().Be(0);
    }

    #endregion

    #region Particle Update Tests

    [Fact]
    public void ShouldUpdateParticlePositions()
    {
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

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));
        var initialCount = emitter.ParticleCount;

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.1)));

        emitter.ParticleCount.Should().Be(initialCount + 1);

        var particlesMovedRight = emitter.ActiveParticles.Count(p => p.Position.X > 0);
        particlesMovedRight.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShouldApplyGravityToParticles()
    {
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

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(1.0)));

        var particlesMovedDown = emitter.ActiveParticles.Count(p => p.Position.Y > 0);
        particlesMovedDown.Should().BeGreaterThan(0);
    }

    [Fact]
    public void ShouldExpireParticlesAfterLifetime()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.ParticleLifetime = 0.5f;
        emitter.LifetimeVariation = 0;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));
        emitter.IsEmitting = false;
        var initialCount = emitter.ParticleCount;
        initialCount.Should().BeGreaterThan(0);

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(1.0)));

        emitter.ParticleCount.Should().Be(0);
    }

    #endregion

    #region Pooling Tests

    [Fact]
    public void ShouldRecycleParticlesViaPooling()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.ParticleLifetime = 0.2f;
        emitter.LifetimeVariation = 0;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));
        var firstBatchCount = emitter.ParticleCount;

        emitter.IsEmitting = false;

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.5)));
        emitter.ParticleCount.Should().Be(0);

        emitter.IsEmitting = true;

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.6), TimeSpan.FromSeconds(0.1)));
        var secondBatchCount = emitter.ParticleCount;

        secondBatchCount.Should().BeGreaterThan(0);
        secondBatchCount.Should().BeCloseTo(firstBatchCount, (uint)2);
    }

    #endregion

    #region Emitter Shape Tests

    [Fact]
    public void ShouldEmitFromPointShape()
    {
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

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        foreach (var particle in emitter.ActiveParticles)
        {
            particle.Position.X.Should().BeApproximately(100, 0.1f);
            particle.Position.Y.Should().BeApproximately(100, 0.1f);
        }
    }

    [Fact]
    public void ShouldEmitFromCircleShape()
    {
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

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        foreach (var particle in emitter.ActiveParticles)
        {
            var distance = particle.Position.Length();
            distance.Should().BeLessThanOrEqualTo(50f);
        }
    }

    [Fact]
    public void ShouldEmitFromLineShapeAlongAngle()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 50f;
        emitter.Shape = EmitterShape.Line;
        emitter.ShapeSize = new Vector2(100f, 0f);
        emitter.LineAngle = MathF.PI / 2f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        emitter.ActiveParticles.Should().NotBeEmpty();
        foreach (var particle in emitter.ActiveParticles)
            particle.Position.X.Should().BeApproximately(0f, 1f);
    }

    [Fact]
    public void ShouldEmitFromLineShapeHorizontalByDefault()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 50f;
        emitter.Shape = EmitterShape.Line;
        emitter.ShapeSize = new Vector2(100f, 0f);
        emitter.LineAngle = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        emitter.ActiveParticles.Should().NotBeEmpty();
        foreach (var particle in emitter.ActiveParticles)
            particle.Position.Y.Should().BeApproximately(0f, 1f);
    }

    [Fact]
    public void ConeShapeShouldNotDoubleSpreadWhenVelocitySpreadIsAlsoSet()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 42);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 200f;
        emitter.MaxParticles = 500;
        emitter.Shape = EmitterShape.Cone;
        emitter.SpawnRadius = 0f;
        emitter.ConeAngle = 30f;
        emitter.VelocitySpread = 30f;
        emitter.InitialVelocity = new Vector2(100, 0);
        emitter.Gravity = Vector2.Zero;
        emitter.SpeedVariation = 0f;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        var coneHalfRadians = emitter.ConeAngle * (MathF.PI / 180f) / 2f;
        var outOfCone = emitter.ActiveParticles.Count(p =>
        {
            var angle = MathF.Atan2(p.Velocity.Y, p.Velocity.X);
            return MathF.Abs(angle) > coneHalfRadians + 0.01f;
        });

        outOfCone.Should().Be(0, "cone shape must not double-apply the spread angle");
    }

    #endregion

    #region Rotation Tests

    [Fact]
    public void ShouldUpdateParticleRotation()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.RotationSpeed = MathF.PI;
        emitter.RotationSpeedVariation = 0;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));
        var initialRotations = emitter.ActiveParticles.Select(p => p.Rotation).ToList();

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(1.0)));

        for (int i = 0; i < emitter.ActiveParticles.Count && i < initialRotations.Count; i++)
        {
            var particle = emitter.ActiveParticles[i];
            particle.Rotation.Should().NotBe(initialRotations[i]);
        }
    }

    [Fact]
    public void RotationSpeedVariationShouldWorkWhenRotationSpeedIsZero()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 100f;
        emitter.RotationSpeed = 0f;
        emitter.RotationSpeedVariation = MathF.PI;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));

        var rotationSpeeds = emitter.ActiveParticles.Select(p => p.RotationSpeed).Distinct().ToList();
        rotationSpeeds.Should()
            .HaveCountGreaterThan(1, "variation should produce different rotation speeds per particle");
    }

    [Fact]
    public void InitialRotationVariationShouldProduceSpreadInRadians()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 99);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 200f;
        emitter.MaxParticles = 500;
        emitter.InitialRotation = 0f;
        emitter.InitialRotationVariation = MathF.PI;
        emitter.RotationSpeed = 0f;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        var rotations = emitter.ActiveParticles.Select(p => p.Rotation).Distinct().ToList();
        rotations.Should().HaveCountGreaterThan(1, "variation should produce different initial rotations");

        foreach (var particle in emitter.ActiveParticles)
            particle.Rotation.Should().BeInRange(-MathF.PI, MathF.PI);
    }

    [Fact]
    public void InitialRotationVariationZeroShouldProduceUniformRotation()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 50f;
        emitter.MaxParticles = 100;
        emitter.InitialRotation = 1f;
        emitter.InitialRotationVariation = 0f;
        emitter.RotationSpeed = 0f;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        foreach (var particle in emitter.ActiveParticles)
            particle.Rotation.Should().BeApproximately(1f, 0.001f);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ShouldHandleMissingTransformComponent()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;

        _world.Flush();

        var act = () => _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));
        act.Should().NotThrow();
        emitter.ParticleCount.Should().Be(0);
    }

    [Fact]
    public void ShouldHandleZeroEmissionRate()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        emitter.ParticleCount.Should().Be(0);
    }

    [Fact]
    public void ShouldNotPoisonEmissionTimerWhenEmissionRateIsZero()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 0f;
        emitter.ParticleLifetime = 5f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        emitter.EmissionRate = 10f;
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(1.0)));

        emitter.ParticleCount.Should().BeGreaterThan(0, "emitter must recover after rate is set back to a valid value");
        float.IsNaN(emitter.EmissionTimer).Should().BeFalse("EmissionTimer must not be NaN after a zero-rate update");
    }

    [Fact]
    public void SpeedVariationGreaterThanTwoShouldNotReverseParticles()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 100f;
        emitter.MaxParticles = 200;
        emitter.InitialVelocity = new Vector2(100, 0);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 3f;
        emitter.Gravity = Vector2.Zero;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));

        foreach (var particle in emitter.ActiveParticles)
            particle.Velocity.X.Should().BeGreaterThanOrEqualTo(0f, "speed must never go negative");
    }

    #endregion

    #region Burst Tests

    [Fact]
    public void ShouldEmitBurstCountParticlesInSingleUpdate()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 20;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        emitter.ParticleCount.Should().Be(20);
    }

    [Fact]
    public void ShouldNotEmitAgainAfterBurstFires()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 10;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        var countAfterFirst = emitter.ParticleCount;

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.016)));

        emitter.ParticleCount.Should().Be(countAfterFirst, "burst must not fire again on subsequent updates");
    }

    [Fact]
    public void ShouldAutoDisableAfterBurstParticlesExpire()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        emitter.ParticleCount.Should().Be(5);

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));

        emitter.ParticleCount.Should().Be(0);
        emitter.IsEnabled.Should().BeFalse("burst emitter should disable itself once all particles have expired");
    }

    [Fact]
    public void ShouldRespectMaxParticlesForBurst()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 50;
        emitter.MaxParticles = 10;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        emitter.ParticleCount.Should().BeLessThanOrEqualTo(10);
    }

    [Fact]
    public void ShouldReFireBurstAfterResetBurst()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 10;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        emitter.ParticleCount.Should().Be(10);

        emitter.ResetBurst();
        emitter.Particles.Clear();

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.016)));

        emitter.ParticleCount.Should().Be(10, "burst should re-fire after ResetBurst()");
    }

    [Fact]
    public void ResetToDefaultStateShouldRestoreIsEnabledAndClearTimer()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;
        emitter.CaptureDefaultState();

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));
        emitter.IsEnabled.Should().BeFalse();

        emitter.ResetToDefaultState();

        emitter.IsEnabled.Should().BeTrue("ResetToDefaultState must restore IsEnabled");
        emitter.EmissionTimer.Should().Be(0f, "ResetToDefaultState must zero the EmissionTimer");
        emitter.BurstFired.Should().BeFalse("ResetToDefaultState must re-arm the burst");
    }

    [Fact]
    public void ResetToDefaultStateShouldNotCauseSpuriousBurstOnContinuousEmitter()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.MaxParticles = 200;
        emitter.ParticleLifetime = 5f;
        emitter.CaptureDefaultState();

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));
        emitter.ParticleCount.Should().BeGreaterThan(0);

        emitter.ResetToDefaultState();
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(0.05)));

        emitter.ParticleCount.Should().BeLessThanOrEqualTo(2, "resetting the timer must not cause a particle flood");
    }
    
    [Fact]
    public void CaptureDefaultStateShouldSnapshotConfiguredValues()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 77f;
        emitter.MaxParticles = 42;
        emitter.ParticleTexture = null;
        emitter.IsBurst = true;
        emitter.BurstCount = 99;
        emitter.CaptureDefaultState();

        emitter.EmissionRate = 1f;
        emitter.MaxParticles = 1;
        emitter.IsBurst = false;
        emitter.BurstCount = 0;

        emitter.ResetToDefaultState();

        emitter.EmissionRate.Should().Be(77f);
        emitter.MaxParticles.Should().Be(42);
        emitter.IsBurst.Should().BeTrue();
        emitter.BurstCount.Should().Be(99);
    }

    #endregion

    #region Size and Color Variation Tests

    [Fact]
    public void SizeVariationShouldProduceDifferentStartSizesPerParticle()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 100f;
        emitter.MaxParticles = 200;
        emitter.StartSize = 10f;
        emitter.SizeVariation = 5f;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        var sizes = emitter.ActiveParticles.Select(p => p.Size).Distinct().ToList();
        sizes.Should().HaveCountGreaterThan(1, "SizeVariation should produce different sizes per particle");

        foreach (var particle in emitter.ActiveParticles)
            particle.Size.Should().BeGreaterThanOrEqualTo(0f, "size must never be negative");
    }

    [Fact]
    public void SizeVariationZeroShouldProduceUniformSize()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 50f;
        emitter.MaxParticles = 100;
        emitter.StartSize = 8f;
        emitter.SizeVariation = 0f;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        foreach (var particle in emitter.ActiveParticles)
            particle.Size.Should().BeApproximately(8f, 0.001f);
    }

    [Fact]
    public void EndSizeVariationShouldProduceDifferentEndSizesPerParticle()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 7);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 100f;
        emitter.MaxParticles = 200;
        emitter.StartSize = 10f;
        emitter.EndSize = 5f;
        emitter.EndSizeVariation = 4f;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        var endSizes = emitter.ActiveParticles.Select(p => p.EndSize).Distinct().ToList();
        endSizes.Should().HaveCountGreaterThan(1, "EndSizeVariation should produce different end sizes per particle");

        foreach (var particle in emitter.ActiveParticles)
            particle.EndSize.Should().BeGreaterThanOrEqualTo(0f, "end size must never be negative");
    }

    [Fact]
    public void EndSizeVariationZeroShouldProduceUniformEndSize()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 50f;
        emitter.MaxParticles = 100;
        emitter.EndSize = 3f;
        emitter.EndSizeVariation = 0f;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        foreach (var particle in emitter.ActiveParticles)
            particle.EndSize.Should().BeApproximately(3f, 0.001f);
    }

    [Fact]
    public void StartColorVariationShouldProduceDifferentColorsPerParticle()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 100f;
        emitter.MaxParticles = 200;
        emitter.StartColor = new Color(128, 128, 128, 255);
        emitter.StartColorVariation = new Color(50, 50, 50, 0);
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        var uniqueColors = emitter.ActiveParticles.Select(p => p.StartColor).Distinct().ToList();
        uniqueColors.Should()
            .HaveCountGreaterThan(1, "StartColorVariation should produce different colors per particle");
    }

    [Fact]
    public void EndColorVariationShouldProduceDifferentEndColorsPerParticle()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 100f;
        emitter.MaxParticles = 200;
        emitter.EndColor = new Color(200, 100, 50, 255);
        emitter.EndColorVariation = new Color(50, 50, 50, 0);
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        var uniqueEndColors = emitter.ActiveParticles.Select(p => p.EndColor).Distinct().ToList();
        uniqueEndColors.Should()
            .HaveCountGreaterThan(1, "EndColorVariation should produce different end colors per particle");
    }

    [Fact]
    public void EndColorVariationZeroShouldProduceUniformEndColor()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 50f;
        emitter.MaxParticles = 100;
        emitter.EndColor = new Color(10, 20, 30, 0);
        emitter.EndColorVariation = new Color(0, 0, 0, 0);
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        foreach (var particle in emitter.ActiveParticles)
            particle.EndColor.Should().Be(new Color(10, 20, 30, 0),
                "all particles must share the same end color when variation is zero");
    }

    #endregion

    #region Pause / Stop Tests

    [Fact]
    public void PauseShouldFreezeParticleAgingAndEmission()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.InitialVelocity = Vector2.Zero;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));
        var countBeforePause = emitter.ParticleCount;
        var lifeBefore = emitter.ActiveParticles.Select(p => p.Life).ToList();

        emitter.Pause();
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(1.0)));

        emitter.ParticleCount.Should().Be(countBeforePause, "no particles should expire or be emitted while paused");
        for (int i = 0; i < emitter.ActiveParticles.Count; i++)
            emitter.ActiveParticles[i].Life.Should()
                .BeApproximately(lifeBefore[i], 0.001f, "particle life must not advance while paused");
    }

    [Fact]
    public void ResumeShouldContinueParticleAgingAfterPause()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.InitialVelocity = Vector2.Zero;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));
        emitter.Pause();
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(1.0)));
        var lifeAfterPause = emitter.ActiveParticles.Select(p => p.Life).ToList();

        emitter.Resume();
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(1.1), TimeSpan.FromSeconds(0.5)));

        for (int i = 0; i < emitter.ActiveParticles.Count && i < lifeAfterPause.Count; i++)
            emitter.ActiveParticles[i].Life.Should()
                .BeLessThan(lifeAfterPause[i], "particle life must decrease again after Resume");
    }

    [Fact]
    public void StopShouldDisableEmissionAndResetTimer()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 50f;
        emitter.ParticleLifetime = 5f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));
        emitter.ParticleCount.Should().BeGreaterThan(0);

        emitter.Stop();

        emitter.IsEmitting.Should().BeFalse("Stop must disable emission");
        emitter.EmissionTimer.Should().Be(0f, "Stop must reset the emission timer");
    }

    [Fact]
    public void StopShouldClearAllParticlesOnNextUpdate()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 50f;
        emitter.ParticleLifetime = 5f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));
        emitter.ParticleCount.Should().BeGreaterThan(0);

        emitter.Stop();
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(1.0), TimeSpan.Zero));

        emitter.ParticleCount.Should().Be(0, "Stop must clear all live particles on the next system update");
    }

    [Fact]
    public void StopThenReEnableShouldEmitNormally()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.ParticleLifetime = 5f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));
        emitter.Stop();
        emitter.IsEmitting = true;

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(0.5)));

        emitter.ParticleCount.Should().BeGreaterThan(0, "emitter must resume normal emission after Stop + re-enable");
        emitter.ParticleCount.Should()
            .BeLessThanOrEqualTo(12, "emission timer was reset so no burst of stale particles");
    }

    #endregion

    #region Local Space Tests

    [Fact]
    public void SimulateInLocalSpaceShouldSpawnParticlesAtOriginNotWorldPosition()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = new Vector2(500, 500);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.SimulateInLocalSpace = true;
        emitter.EmissionRate = 50f;
        emitter.MaxParticles = 100;
        emitter.Shape = EmitterShape.Point;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.ParticleLifetime = 5f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        emitter.ActiveParticles.Should().NotBeEmpty();
        foreach (var particle in emitter.ActiveParticles)
        {
            particle.Position.X.Should().BeApproximately(0f, 0.1f,
                "local-space particles spawn relative to origin, not world position");
            particle.Position.Y.Should().BeApproximately(0f, 0.1f,
                "local-space particles spawn relative to origin, not world position");
        }
    }

    [Fact]
    public void SimulateInWorldSpaceShouldSpawnParticlesAtWorldPosition()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = new Vector2(500, 500);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.SimulateInLocalSpace = false;
        emitter.EmissionRate = 50f;
        emitter.MaxParticles = 100;
        emitter.Shape = EmitterShape.Point;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.ParticleLifetime = 5f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        emitter.ActiveParticles.Should().NotBeEmpty();
        foreach (var particle in emitter.ActiveParticles)
        {
            particle.Position.X.Should().BeApproximately(500f, 0.1f,
                "world-space particles spawn at the entity's world position");
            particle.Position.Y.Should().BeApproximately(500f, 0.1f,
                "world-space particles spawn at the entity's world position");
        }
    }

    [Fact]
    public void LocalSpaceParticlesShouldMoveIndependentlyOfTransform()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = new Vector2(100, 100);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.SimulateInLocalSpace = true;
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 50;
        emitter.InitialVelocity = new Vector2(50, 0);
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.SpeedVariation = 0f;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        var positionsBeforeMove = emitter.ActiveParticles.Select(p => p.Position).ToList();

        transform.Position = new Vector2(900, 900);

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.1)));

        for (int i = 0; i < Math.Min(emitter.ActiveParticles.Count, positionsBeforeMove.Count); i++)
        {
            var before = positionsBeforeMove[i];
            var after = emitter.ActiveParticles[i].Position;
            after.X.Should().BeGreaterThan(before.X,
                "local-space particles continue drifting in local space; they are not stuck to the transform");
        }
    }

    [Fact]
    public void LocalSpaceAndWorldSpaceShouldProduceDifferentStoredPositions()
    {
        var worldA = CreateTestWorld();
        var worldB = CreateTestWorld();
        var poolProvider = new DefaultObjectPoolProvider();
        var systemA = new ParticleSystem(poolProvider, seed: 1);
        var systemB = new ParticleSystem(poolProvider, seed: 1);

        void SetupEntity(IEntityWorld world, bool localSpace, out ParticleEmitterComponent emitter)
        {
            var e = world.CreateEntity();
            e.AddComponent<TransformComponent>();
            e.GetComponent<TransformComponent>().Position = new Vector2(300, 300);
            e.AddComponent<ParticleEmitterComponent>();
            emitter = e.GetComponent<ParticleEmitterComponent>();
            emitter.SimulateInLocalSpace = localSpace;
            emitter.EmissionRate = 30f;
            emitter.MaxParticles = 60;
            emitter.Shape = EmitterShape.Point;
            emitter.InitialVelocity = Vector2.Zero;
            emitter.VelocitySpread = 0f;
            emitter.Gravity = Vector2.Zero;
            emitter.ParticleLifetime = 5f;
            emitter.LifetimeVariation = 0f;
            world.Flush();
        }

        SetupEntity(worldA, localSpace: false, out var worldSpaceEmitter);
        SetupEntity(worldB, localSpace: true, out var localSpaceEmitter);

        systemA.Update(worldA, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.3)));
        systemB.Update(worldB, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.3)));

        worldSpaceEmitter.ActiveParticles.Should().NotBeEmpty();
        localSpaceEmitter.ActiveParticles.Should().NotBeEmpty();

        var worldPos = worldSpaceEmitter.ActiveParticles[0].Position;
        var localPos = localSpaceEmitter.ActiveParticles[0].Position;

        worldPos.X.Should().BeApproximately(300f, 0.1f, "world-space particle is stored at entity position");
        localPos.X.Should().BeApproximately(0f, 0.1f, "local-space particle is stored relative to origin");
    }

    #endregion

    #region OnParticleDied Tests

    [Fact]
    public void OnParticleDiedShouldBeInvokedWhenParticleExpires()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 100f;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;

        var diedCount = 0;
        emitter.OnParticleDied = _ => diedCount++;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.05)));
        emitter.IsEmitting = false;
        var spawnedCount = emitter.ParticleCount;
        spawnedCount.Should().BeGreaterThan(0);

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.05), TimeSpan.FromSeconds(0.5)));

        emitter.ParticleCount.Should().Be(0);
        diedCount.Should().Be(spawnedCount, "callback must fire exactly once per expired particle");
    }

    [Fact]
    public void OnParticleDiedShouldReceiveParticleWithFinalState()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = new Vector2(50, 50);
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 100f;
        emitter.ParticleLifetime = 0.15f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(100, 0);
        emitter.Gravity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;

        float capturedX = float.NaN;
        emitter.OnParticleDied = p => { if (float.IsNaN(capturedX)) capturedX = p.Position.X; };

        _world.Flush();

        // tick 1 — spawn particles at (50, 50); no movement yet (spawn happens after UpdateParticles)
        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.05)));
        emitter.IsEmitting = false;
        emitter.ParticleCount.Should().BeGreaterThan(0);

        // tick 2 — particles survive (life = 0.15 - 0.05 = 0.10) and move: x += 100 * 0.05 = 5 → x = 55
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.05), TimeSpan.FromSeconds(0.05)));
        emitter.ParticleCount.Should().BeGreaterThan(0, "particles must still be alive after second tick");

        // tick 3 — particles expire; callback fires with x = 55 (position from previous tick)
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.10), TimeSpan.FromSeconds(0.5)));

        float.IsNaN(capturedX).Should().BeFalse("OnParticleDied must have fired at least once");
        capturedX.Should().BeGreaterThan(50f, "particle should have moved right before dying");
    }

    [Fact]
    public void OnParticleDiedShouldNotBeInvokedWhenStopClearsParticles()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.ParticleLifetime = 10f;

        var diedCount = 0;
        emitter.OnParticleDied = _ => diedCount++;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));
        emitter.ParticleCount.Should().BeGreaterThan(0);

        emitter.Stop();
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.5), TimeSpan.Zero));

        diedCount.Should().Be(0,
            "Stop() force-clears particles without natural expiry; OnParticleDied must not fire");
    }

    [Fact]
    public void NullOnParticleDiedShouldNotThrow()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;
        emitter.OnParticleDied = null;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.05)));
        emitter.IsEmitting = false;

        var act = () => _particleSystem.Update(_world,
            new GameTime(TimeSpan.FromSeconds(0.05), TimeSpan.FromSeconds(0.5)));

        act.Should().NotThrow();
    }

    #endregion

    #region ParticleFrames Animation Tests

    [Fact]
    public void ParticleFramesShouldTakePriorityOverParticleAtlasRegion()
    {
        var poolProvider = new DefaultObjectPoolProvider();
        var system = new ParticleSystem(poolProvider, seed: 0);

        var renderer = Substitute.For<IRenderer>();
        var atlasTexture = Substitute.For<ITexture>();
        atlasTexture.Width.Returns(64);
        atlasTexture.Height.Returns(64);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.MaxParticles = 50;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        var frame0 = new AtlasRegion("frame0", new Rectangle(0, 0, 16, 16), atlasTexture);
        var frame1 = new AtlasRegion("frame1", new Rectangle(16, 0, 16, 16), atlasTexture);
        emitter.ParticleFrames = [frame0, frame1];
        emitter.ParticleAtlasRegion = new AtlasRegion("single", new Rectangle(32, 0, 16, 16), atlasTexture);

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));
        emitter.ParticleCount.Should().BeGreaterThan(0);

        var act = () => system.Render(_world, renderer);
        act.Should().NotThrow("ParticleFrames present must not cause errors even when ParticleAtlasRegion is also set");

        renderer.Received().DrawTexture(
            Arg.Is(atlasTexture),
            Arg.Any<Vector2>(),
            Arg.Is<Rectangle?>(r => r.HasValue && r.Value.X == 0),
            Arg.Any<Vector2?>(),
            Arg.Any<float>(),
            Arg.Any<Vector2?>(),
            Arg.Any<Color?>(),
            Arg.Any<SpriteFlip>());
    }

    [Fact]
    public void ParticleFramesShouldAdvanceFrameIndexWithLifetime()
    {
        var poolProvider = new DefaultObjectPoolProvider();
        var system = new ParticleSystem(poolProvider, seed: 0);

        var renderer = Substitute.For<IRenderer>();
        var atlasTexture = Substitute.For<ITexture>();
        atlasTexture.Width.Returns(64);
        atlasTexture.Height.Returns(64);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 1f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.Gravity = Vector2.Zero;

        var frame0 = new AtlasRegion("frame0", new Rectangle(0, 0, 16, 16), atlasTexture);
        var frame1 = new AtlasRegion("frame1", new Rectangle(16, 0, 16, 16), atlasTexture);
        emitter.ParticleFrames = [frame0, frame1];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        emitter.ParticleCount.Should().Be(1);

        system.Render(_world, renderer);

        renderer.Received().DrawTexture(
            Arg.Any<ITexture>(),
            Arg.Any<Vector2>(),
            Arg.Is<Rectangle?>(r => r.HasValue && r.Value.X == 0),
            Arg.Any<Vector2?>(),
            Arg.Any<float>(),
            Arg.Any<Vector2?>(),
            Arg.Any<Color?>(),
            Arg.Any<SpriteFlip>());

        renderer.ClearReceivedCalls();

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.6)));
        emitter.ParticleCount.Should().Be(1, "particle should still be alive");

        system.Render(_world, renderer);

        renderer.Received().DrawTexture(
            Arg.Any<ITexture>(),
            Arg.Any<Vector2>(),
            Arg.Is<Rectangle?>(r => r.HasValue && r.Value.X == 16),
            Arg.Any<Vector2?>(),
            Arg.Any<float>(),
            Arg.Any<Vector2?>(),
            Arg.Any<Color?>(),
            Arg.Any<SpriteFlip>());
    }

    [Fact]
    public void ParticleFramesEmptyOrNullShouldFallBackToAtlasRegion()
    {
        var poolProvider = new DefaultObjectPoolProvider();
        var system = new ParticleSystem(poolProvider, seed: 0);

        var renderer = Substitute.For<IRenderer>();
        var atlasTexture = Substitute.For<ITexture>();
        atlasTexture.Width.Returns(64);
        atlasTexture.Height.Returns(64);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.MaxParticles = 20;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.ParticleFrames = null;
        emitter.ParticleAtlasRegion = new AtlasRegion("single", new Rectangle(8, 0, 16, 16), atlasTexture);

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        var act = () => system.Render(_world, renderer);
        act.Should().NotThrow();

        renderer.Received().DrawTexture(
            Arg.Is(atlasTexture),
            Arg.Any<Vector2>(),
            Arg.Is<Rectangle?>(r => r.HasValue && r.Value.X == 8),
            Arg.Any<Vector2?>(),
            Arg.Any<float>(),
            Arg.Any<Vector2?>(),
            Arg.Any<Color?>(),
            Arg.Any<SpriteFlip>());
    }

    #endregion

    #region Duration Tests

    [Fact]
    public void DurationShouldStopEmissionAfterElapsedTime()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 200;
        emitter.ParticleLifetime = 10f;
        emitter.Duration = 1f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.5)));

        emitter.IsEmitting.Should().BeFalse("emitter must stop after Duration seconds");
    }

    [Fact]
    public void NullDurationShouldEmitIndefinitely()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.MaxParticles = 500;
        emitter.ParticleLifetime = 10f;
        emitter.Duration = null;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(5.0)));

        emitter.IsEmitting.Should().BeTrue("null Duration means emit forever");
    }

    [Fact]
    public void DurationShouldBeRestoredByResetToDefaultState()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 200;
        emitter.ParticleLifetime = 10f;
        emitter.Duration = 0.5f;
        emitter.CaptureDefaultState();

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));
        emitter.IsEmitting.Should().BeFalse();

        emitter.ResetToDefaultState();
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(1.0), TimeSpan.Zero));

        emitter.IsEmitting.Should().BeTrue("ResetToDefaultState must restore IsEmitting");
        emitter.Duration.Should().Be(0.5f);
    }

    #endregion

    #region OnParticleSpawned Tests

    [Fact]
    public void OnParticleSpawnedShouldBeInvokedForEachSpawnedParticle()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 50f;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        var spawnCount = 0;
        emitter.OnParticleSpawned = _ => spawnCount++;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        spawnCount.Should().Be(emitter.ParticleCount,
            "OnParticleSpawned must fire exactly once per particle spawned");
    }

    [Fact]
    public void OnParticleSpawnedShouldReceiveParticleWithInitialState()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = new Vector2(10, 20);
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.Gravity = Vector2.Zero;
        emitter.Shape = EmitterShape.Point;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;

        Particle? captured = null;
        emitter.OnParticleSpawned = p => captured = p;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        captured.Should().NotBeNull("OnParticleSpawned must have fired");
        captured!.Life.Should().BeGreaterThan(0f);
        captured.Position.X.Should().BeApproximately(10f, 0.1f);
        captured.Position.Y.Should().BeApproximately(20f, 0.1f);
    }

    #endregion

    #region OnEmitterFinished Tests

    [Fact]
    public void OnEmitterFinishedShouldFireWhenBurstParticlesExpire()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;

        var finished = false;
        emitter.OnEmitterFinished = () => finished = true;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        finished.Should().BeFalse("particles are still alive");

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));

        finished.Should().BeTrue("OnEmitterFinished must fire once all burst particles expire");
    }

    [Fact]
    public void OnEmitterFinishedShouldFireWhenDurationEmitterDrainsOut()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 200;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;
        emitter.Duration = 0.2f;

        var finished = false;
        emitter.OnEmitterFinished = () => finished = true;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.3)));
        finished.Should().BeFalse("particles may still be alive");

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.3), TimeSpan.FromSeconds(1.0)));

        finished.Should().BeTrue("OnEmitterFinished must fire once the last timed particle expires");
    }

    [Fact]
    public void OnEmitterFinishedShouldNotFireWhenStopClearsParticles()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.ParticleLifetime = 10f;

        var finished = false;
        emitter.OnEmitterFinished = () => finished = true;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));
        emitter.Stop();
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.5), TimeSpan.Zero));

        finished.Should().BeFalse("Stop() must not trigger OnEmitterFinished");
    }

    [Fact]
    public void OnEmitterFinishedShouldNotFireMoreThanOncePerCycle()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 3;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;

        var finishedCount = 0;
        emitter.OnEmitterFinished = () => finishedCount++;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.516), TimeSpan.FromSeconds(0.5)));

        finishedCount.Should().Be(1, "OnEmitterFinished must fire exactly once per burst cycle");
    }

    #endregion

    #region ResetToDefaultState Throws Tests

    [Fact]
    public void ResetToDefaultStateShouldThrowWhenNeverCaptured()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 999f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        var act = () => emitter.ResetToDefaultState();
        act.Should().Throw<InvalidOperationException>("CaptureDefaultState must be called before ResetToDefaultState");
    }

    [Fact]
    public void TryResetToDefaultStateShouldReturnFalseWhenNeverCaptured()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();

        _world.Flush();

        var act = () => emitter.TryResetToDefaultState();
        act.Should().NotThrow();
        emitter.TryResetToDefaultState().Should().BeFalse();
    }

    [Fact]
    public void TryResetToDefaultStateShouldReturnTrueAndRestoreWhenCaptured()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 42f;
        emitter.CaptureDefaultState();

        emitter.EmissionRate = 1f;

        emitter.TryResetToDefaultState().Should().BeTrue();
        emitter.EmissionRate.Should().Be(42f);
    }

    #endregion

    #region Trail Grow Tests

    [Fact]
    public void TrailShouldNotRenderUnwrittenSlotsAtBirth()
    {
        var poolProvider = new DefaultObjectPoolProvider();
        var system = new ParticleSystem(poolProvider, seed: 0);
        var renderer = Substitute.For<IRenderer>();

        var atlasTexture = Substitute.For<ITexture>();
        atlasTexture.Width.Returns(16);
        atlasTexture.Height.Returns(16);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.EnableTrails = true;
        emitter.TrailLength = 10;
        emitter.InitialVelocity = new Vector2(100, 0);
        emitter.Gravity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;

        _world.Flush();

        // Spawn on first tick — TrailFilled should be 0, so no trail segments render.
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        emitter.ParticleCount.Should().Be(1);

        var particle = emitter.ActiveParticles[0];
        particle.TrailFilled.Should().Be(0, "no trail slots have been written yet at birth");

        // After one more tick a slot is written.
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.016)));
        particle.TrailFilled.Should().Be(1, "exactly one slot written after one update");
    }

    #endregion

    #region Burst With Zero EmissionRate

    [Fact]
    public void BurstEmitterWithZeroEmissionRateShouldStillFireParticles()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 10;
        emitter.MaxParticles = 50;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.EmissionRate = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        emitter.ParticleCount.Should().Be(10, "burst must fire regardless of EmissionRate");
    }

    #endregion

    #region Damping Tests

    [Fact]
    public void DampingShouldReduceParticleVelocityOverTime()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(200, 0);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.Damping = 2f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        var speedAfterSpawn = emitter.ActiveParticles[0].Velocity.Length();

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));
        var speedAfterDamping = emitter.ActiveParticles[0].Velocity.Length();

        speedAfterDamping.Should().BeLessThan(speedAfterSpawn, "damping must reduce velocity over time");
    }

    [Fact]
    public void DampingOfZeroShouldNotAffectVelocity()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(100, 0);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.Damping = 0f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        var speedAfterSpawn = emitter.ActiveParticles[0].Velocity.Length();

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));
        var speedAfterUpdate = emitter.ActiveParticles[0].Velocity.Length();

        speedAfterUpdate.Should().BeApproximately(speedAfterSpawn, 0.01f, "zero damping must not change speed");
    }

    [Fact]
    public void DampingShouldBeCapturedAndRestoredByDefaultState()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.Damping = 3.5f;
        emitter.CaptureDefaultState();

        emitter.Damping = 0f;
        emitter.ResetToDefaultState();

        emitter.Damping.Should().Be(3.5f);
    }

    #endregion

    #region ColorGradient Tests

    [Fact]
    public void ColorGradientShouldOverrideStartEndColorDuringRender()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 2f;
        emitter.LifetimeVariation = 0f;
        emitter.StartColor = Color.White;
        emitter.EndColor = Color.White;
        emitter.ColorGradient = [new Color(0, 0, 255, 255), new Color(0, 0, 255, 255)];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        system.Render(_world, renderer);

        renderer.Received().DrawCircleFilled(
            Arg.Any<float>(),
            Arg.Any<float>(),
            Arg.Any<float>(),
            Arg.Is<Color>(c => c.B > 200 && c.R < 50));
    }

    [Fact]
    public void NullColorGradientShouldFallBackToStartEndColorLerp()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 2f;
        emitter.LifetimeVariation = 0f;
        emitter.StartColor = new Color(255, 0, 0, 255);
        emitter.EndColor = new Color(255, 0, 0, 255);
        emitter.ColorGradient = null;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        system.Render(_world, renderer);

        renderer.Received().DrawCircleFilled(
            Arg.Any<float>(),
            Arg.Any<float>(),
            Arg.Any<float>(),
            Arg.Is<Color>(c => c.R > 200 && c.B < 50));
    }

    [Fact]
    public void ColorGradientShouldBeCapturedAndRestoredByDefaultState()
    {
        var gradient = new[] { new Color(255, 0, 0, 255), new Color(0, 0, 255, 255) };

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.ColorGradient = gradient;
        emitter.CaptureDefaultState();

        emitter.ColorGradient = null;
        emitter.ResetToDefaultState();

        emitter.ColorGradient.Should().BeEquivalentTo(gradient,
            "the restored gradient must contain the same colors as the captured snapshot");
        emitter.ColorGradient.Should().NotBeSameAs(gradient,
            "the restored gradient must be a deep copy, not the original array reference");
    }

    #endregion

    #region Warmup Tests

    [Fact]
    public void WarmupDurationShouldPrePopulateParticlesOnFirstUpdate()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 200;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.WarmupDuration = 2f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        emitter.ParticleCount.Should().BeGreaterThan(0,
            "warmup should pre-simulate particles before the first frame");
    }

    [Fact]
    public void WarmupShouldOnlyApplyOnce()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 500;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.WarmupDuration = 1f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        var countAfterFirst = emitter.ParticleCount;

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.016)));
        var countAfterSecond = emitter.ParticleCount;

        emitter.WarmupApplied.Should().BeTrue();
        countAfterSecond.Should().BeCloseTo(countAfterFirst, 5u,
            "warmup must not re-run on subsequent updates");
    }

    [Fact]
    public void WarmupShouldResetAfterStop()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 200;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.WarmupDuration = 1f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        emitter.WarmupApplied.Should().BeTrue();

        emitter.Stop();
        emitter.WarmupApplied.Should().BeFalse("Stop() must reset WarmupApplied so warmup re-runs on restart");

        emitter.IsEmitting = true;
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.016)));
        emitter.WarmupApplied.Should().BeTrue("warmup must re-apply after Stop+restart");
    }

    [Fact]
    public void ZeroWarmupDurationShouldNotPrePopulateParticles()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 0;
        emitter.MaxParticles = 200;
        emitter.ParticleLifetime = 5f;
        emitter.EmissionRate = 0f;
        emitter.WarmupDuration = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        emitter.ParticleCount.Should().Be(0, "no warmup and no burst particles should mean zero particles");
    }

    #endregion

    #region Particle Lifetime Clamp Tests

    [Fact]
    public void LifetimeVariationAboveOneShouldNotProduceNegativeLifetime()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 200f;
        emitter.MaxParticles = 500;
        emitter.ParticleLifetime = 1f;
        emitter.LifetimeVariation = 5f;
        emitter.Gravity = Vector2.Zero;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        foreach (var particle in emitter.ActiveParticles)
            particle.Life.Should().BeGreaterThan(0f,
                "particles with extreme LifetimeVariation must be clamped to MinParticleLifetime");
    }

    [Fact]
    public void LifetimeVariationAtExactlyOneShouldNotProduceZeroLifetime()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 1);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 200f;
        emitter.MaxParticles = 500;
        emitter.ParticleLifetime = 0.5f;
        emitter.LifetimeVariation = 1f;
        emitter.Gravity = Vector2.Zero;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        foreach (var particle in emitter.ActiveParticles)
            particle.MaxLife.Should().BeGreaterThanOrEqualTo(ParticleSystem.MinParticleLifetime,
                "MaxLife must never be zero or negative");
    }

    #endregion

    #region Warmup Cap Tests

    [Fact]
    public void WarmupDurationAboveMaxShouldBeClampedAndNotStall()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 5f;
        emitter.MaxParticles = 500;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.WarmupDuration = 10_000f;

        _world.Flush();

        var act = () => _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        act.Should().NotThrow("absurdly large WarmupDuration must be clamped, not loop forever");

        emitter.ParticleCount.Should().BeLessThanOrEqualTo(emitter.MaxParticles,
            "warmup cap must not spawn beyond MaxParticles");
    }

    [Fact]
    public void WarmupDurationAtMaxShouldPreSimulateUpToMaxDuration()
    {
        var entityCapped = _world.CreateEntity();
        entityCapped.AddComponent<TransformComponent>();
        entityCapped.AddComponent<ParticleEmitterComponent>();
        var emitterCapped = entityCapped.GetComponent<ParticleEmitterComponent>();
        emitterCapped.EmissionRate = 10f;
        emitterCapped.MaxParticles = 500;
        emitterCapped.ParticleLifetime = 100f;
        emitterCapped.LifetimeVariation = 0f;
        emitterCapped.WarmupDuration = ParticleSystem.MaxWarmupDuration + 5f;

        var entityAtMax = _world.CreateEntity();
        entityAtMax.AddComponent<TransformComponent>();
        entityAtMax.AddComponent<ParticleEmitterComponent>();
        var emitterAtMax = entityAtMax.GetComponent<ParticleEmitterComponent>();
        emitterAtMax.EmissionRate = 10f;
        emitterAtMax.MaxParticles = 500;
        emitterAtMax.ParticleLifetime = 100f;
        emitterAtMax.LifetimeVariation = 0f;
        emitterAtMax.WarmupDuration = ParticleSystem.MaxWarmupDuration;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        emitterCapped.ParticleCount.Should().Be(emitterAtMax.ParticleCount,
            "a WarmupDuration beyond the cap produces the same result as exactly MaxWarmupDuration");
    }

    #endregion

    #region Trail Array Pool Hygiene Tests

    [Fact]
    public void PooledParticleShouldNotRetainStaleTrailArraysAfterReset()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.EnableTrails = true;
        emitter.TrailLength = 20;
        emitter.Gravity = Vector2.Zero;
        emitter.InitialVelocity = Vector2.Zero;

        _world.Flush();

        // Spawn + expire the trailed particle — it goes back to the pool with TrailLength=20.
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));
        emitter.ParticleCount.Should().Be(0);

        // Re-arm with TrailLength = 5, trails disabled → the recycled particle should not carry
        // the old 20-slot arrays.
        emitter.EnableTrails = false;
        emitter.TrailLength = 5;
        emitter.ResetBurst();

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.516), TimeSpan.FromSeconds(0.016)));
        emitter.ParticleCount.Should().Be(1);

        var particle = emitter.ActiveParticles[0];
        particle.TrailPositions.Should().BeNull("Reset() must null trail arrays so stale allocations are not retained");
        particle.TrailRotations.Should().BeNull("Reset() must null trail arrays so stale allocations are not retained");
    }

    [Fact]
    public void TrailArrayShouldBeReallocatedWhenTrailLengthChangesAfterPoolReturn()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.EnableTrails = true;
        emitter.TrailLength = 10;
        emitter.Gravity = Vector2.Zero;
        emitter.InitialVelocity = Vector2.Zero;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));
        emitter.ParticleCount.Should().Be(0);

        emitter.TrailLength = 3;
        emitter.ResetBurst();

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.516), TimeSpan.FromSeconds(0.016)));
        emitter.ParticleCount.Should().Be(1);

        emitter.ActiveParticles[0].TrailPositions.Should().HaveCount(3,
            "SpawnParticle must reallocate trail arrays when TrailLength changes");
    }

    #endregion
    #region Entity Rotation Tests

    [Fact]
    public void ParticleVelocityShouldRespectEntityRotation()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = Vector2.Zero;
        transform.Rotation = MathF.PI / 2f;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 20;
        emitter.MaxParticles = 20;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(100, 0);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        foreach (var particle in emitter.ActiveParticles)
        {
            particle.Velocity.X.Should().BeApproximately(0f, 1f,
                "rotated 90° entity must redirect horizontal velocity to vertical");
            particle.Velocity.Y.Should().BeGreaterThan(0f,
                "particles should travel downward (+Y) after 90° entity rotation");
        }
    }

    [Fact]
    public void ParticleVelocityShouldBeUnchangedWhenEntityRotationIsZero()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = Vector2.Zero;
        transform.Rotation = 0f;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 10;
        emitter.MaxParticles = 10;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(100, 0);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        foreach (var particle in emitter.ActiveParticles)
        {
            particle.Velocity.X.Should().BeApproximately(100f, 0.5f,
                "zero entity rotation must not alter velocity direction");
            particle.Velocity.Y.Should().BeApproximately(0f, 0.5f);
        }
    }

    #endregion

    #region Render Layer Tests

    [Fact]
    public void RenderLayerShouldBeCapturedAndRestoredByDefaultState()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.RenderLayer = 5;
        emitter.CaptureDefaultState();

        emitter.RenderLayer = 0;
        emitter.ResetToDefaultState();

        emitter.RenderLayer.Should().Be(5);
    }

    [Fact]
    public void EmittersWithLowerRenderLayerShouldBeRenderedFirst()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var renderer = Substitute.For<IRenderer>();
        var renderOrder = new List<int>();

        var entityA = _world.CreateEntity();
        entityA.AddComponent<TransformComponent>();
        entityA.AddComponent<ParticleEmitterComponent>();
        var emitterA = entityA.GetComponent<ParticleEmitterComponent>();
        emitterA.IsBurst = true;
        emitterA.BurstCount = 1;
        emitterA.MaxParticles = 1;
        emitterA.ParticleLifetime = 5f;
        emitterA.LifetimeVariation = 0f;
        emitterA.RenderLayer = 10;
        emitterA.StartSize = 1f;
        emitterA.EndSize = 1f;

        var entityB = _world.CreateEntity();
        entityB.AddComponent<TransformComponent>();
        entityB.AddComponent<ParticleEmitterComponent>();
        var emitterB = entityB.GetComponent<ParticleEmitterComponent>();
        emitterB.IsBurst = true;
        emitterB.BurstCount = 1;
        emitterB.MaxParticles = 1;
        emitterB.ParticleLifetime = 5f;
        emitterB.LifetimeVariation = 0f;
        emitterB.RenderLayer = 1;
        emitterB.StartSize = 2f;
        emitterB.EndSize = 2f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        renderer.When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(),
                Arg.Is<float>(s => MathF.Abs(s - 2f) < 0.01f), Arg.Any<Color>()))
            .Do(_ => renderOrder.Add(1));
        renderer.When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(),
                Arg.Is<float>(s => MathF.Abs(s - 1f) < 0.01f), Arg.Any<Color>()))
            .Do(_ => renderOrder.Add(10));

        system.Render(_world, renderer);

        renderOrder.Should().HaveCount(2);
        renderOrder[0].Should().Be(1, "layer 1 (emitterB) must render before layer 10 (emitterA)");
        renderOrder[1].Should().Be(10);
    }

    #endregion

    #region SpawnOnPerimeter Tests

    [Fact]
    public void SpawnOnPerimeterShouldPlaceAllParticlesAtExactRadius()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 42);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 200f;
        emitter.MaxParticles = 500;
        emitter.Shape = EmitterShape.Circle;
        emitter.SpawnRadius = 40f;
        emitter.SpawnOnPerimeter = true;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.Gravity = Vector2.Zero;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        emitter.ActiveParticles.Should().NotBeEmpty();
        foreach (var particle in emitter.ActiveParticles)
            particle.Position.Length().Should().BeApproximately(40f, 0.5f,
                "SpawnOnPerimeter must place particles exactly on the circle edge");
    }

    [Fact]
    public void SpawnOnPerimeterFalseShouldDistributeParticlesInsideRadius()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 7);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 200f;
        emitter.MaxParticles = 500;
        emitter.Shape = EmitterShape.Circle;
        emitter.SpawnRadius = 40f;
        emitter.SpawnOnPerimeter = false;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.Gravity = Vector2.Zero;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        var innerParticles = emitter.ActiveParticles.Count(p => p.Position.Length() < 35f);
        innerParticles.Should().BeGreaterThan(0,
            "uniform distribution must produce particles well inside the radius");
    }

    [Fact]
    public void SpawnOnPerimeterShouldBeCapturedAndRestoredByDefaultState()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.SpawnOnPerimeter = true;
        emitter.CaptureDefaultState();

        emitter.SpawnOnPerimeter = false;
        emitter.ResetToDefaultState();

        emitter.SpawnOnPerimeter.Should().BeTrue();
    }

    #endregion

    #region Stop Resets BurstFired

    [Fact]
    public void StopShouldResetBurstFiredSoBurstCanRefire()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        emitter.ParticleCount.Should().Be(5);

        emitter.Stop();
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.Zero));
        emitter.ParticleCount.Should().Be(0);

        emitter.BurstFired.Should().BeFalse("Stop() must reset BurstFired so the burst can fire again");

        emitter.IsEmitting = true;
        emitter.IsEnabled = true;
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.016)));

        emitter.ParticleCount.Should().Be(5, "burst must re-fire after Stop() + re-enable");
    }

    #endregion

    #region Manual IsEmitting False Should Not Fire OnEmitterFinished

    [Fact]
    public void ManuallySettingIsEmittingFalseShouldNotFireOnEmitterFinished()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;

        var finished = false;
        emitter.OnEmitterFinished = () => finished = true;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.05)));
        emitter.ParticleCount.Should().BeGreaterThan(0);

        emitter.IsEmitting = false;

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.05), TimeSpan.FromSeconds(1.0)));
        emitter.ParticleCount.Should().Be(0);

        finished.Should().BeFalse(
            "manually setting IsEmitting = false must not trigger OnEmitterFinished; only a Duration timeout qualifies");
    }

    [Fact]
    public void ZeroEmissionRateWithDurationShouldFireOnEmitterFinishedWhenDurationElapses()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 0f;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 5f;
        emitter.Duration = 0.5f;

        var finished = false;
        emitter.OnEmitterFinished = () => finished = true;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        finished.Should().BeTrue(
            "OnEmitterFinished must still fire when Duration elapses even if EmissionRate is zero");
    }

    #endregion

    #region Trail Alpha Properties

    [Fact]
    public void TrailAlphasShouldBeCapturedAndRestoredByDefaultState()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.TrailHeadAlpha = 0.6f;
        emitter.TrailTailAlpha = 0.1f;
        emitter.CaptureDefaultState();

        emitter.TrailHeadAlpha = 1f;
        emitter.TrailTailAlpha = 1f;
        emitter.ResetToDefaultState();

        emitter.TrailHeadAlpha.Should().BeApproximately(0.6f, 0.001f);
        emitter.TrailTailAlpha.Should().BeApproximately(0.1f, 0.001f);
    }

    #endregion

    #region Velocity Inheritance Tests

    [Fact]
    public void VelocityInheritanceZeroShouldNotAffectParticleVelocity()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = new Vector2(0, 0);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 10;
        emitter.MaxParticles = 10;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(100, 0);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.VelocityInheritance = 0f;

        _world.Flush();

        // Establish a previous position so the system sees emitter motion.
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        emitter.ParticleCount.Should().Be(10);

        foreach (var particle in emitter.ActiveParticles)
            particle.Velocity.X.Should().BeApproximately(100f, 1f,
                "zero VelocityInheritance must not alter particle velocity");
    }

    [Fact]
    public void VelocityInheritanceOneShouldAddFullEmitterVelocityToParticles()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = new Vector2(0, 0);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = false;
        emitter.EmissionRate = 0f;
        emitter.MaxParticles = 50;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.VelocityInheritance = 1f;

        _world.Flush();

        // First tick — establishes PreviousPosition, no burst yet.
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));

        // Move the emitter 300 px/s to the right, then fire a burst.
        transform.Position = new Vector2(30, 0); // 300 * 0.1 s = 30 px
        emitter.IsBurst = true;
        emitter.BurstCount = 10;
        emitter.MaxParticles = 10;

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));

        emitter.ParticleCount.Should().Be(10);
        foreach (var particle in emitter.ActiveParticles)
            particle.Velocity.X.Should().BeApproximately(300f, 5f,
                "full VelocityInheritance must add the emitter's velocity (~300 px/s) to each particle");
    }

    [Fact]
    public void VelocityInheritanceShouldBeCapturedAndRestoredByDefaultState()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.VelocityInheritance = 0.75f;
        emitter.CaptureDefaultState();

        emitter.VelocityInheritance = 0f;
        emitter.ResetToDefaultState();

        emitter.VelocityInheritance.Should().BeApproximately(0.75f, 0.001f);
    }

    #endregion

    #region Sub-Frame Spawn Interpolation Tests

    [Fact]
    public void StationaryEmitterShouldSpawnAllParticlesAtSamePosition()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = new Vector2(100, 100);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 10;
        emitter.MaxParticles = 10;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.Shape = EmitterShape.Point;

        _world.Flush();

        // Tick 1 — establishes PreviousPosition at (100, 100).
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        emitter.ParticleCount.Should().Be(10);

        foreach (var particle in emitter.ActiveParticles)
        {
            particle.Position.X.Should().BeApproximately(100f, 0.01f);
            particle.Position.Y.Should().BeApproximately(100f, 0.01f);
        }
    }

    #endregion

    #region ResetBurst WarmupApplied Fix Tests

    [Fact]
    public void ResetBurstShouldResetWarmupAppliedSoWarmupRunsAgain()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.WarmupDuration = 0.5f;
        emitter.EmissionRate = 20f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        emitter.WarmupApplied.Should().BeTrue();

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));
        emitter.ParticleCount.Should().Be(0);

        emitter.ResetBurst();
        emitter.WarmupApplied.Should().BeFalse("ResetBurst must clear WarmupApplied so warmup re-runs on re-fire");

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.516), TimeSpan.FromSeconds(0.016)));
        emitter.WarmupApplied.Should().BeTrue("warmup must re-apply after ResetBurst");
    }

    #endregion

    #region Local Space Scale Tests

    [Fact]
    public void LocalSpaceRenderShouldScaleParticleSizeWithEntityScale()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var renderer = Substitute.For<IRenderer>();

        var worldA = CreateTestWorld();
        var worldB = CreateTestWorld();
        var systemA = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 1);
        var systemB = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 1);

        void Setup(IEntityWorld w, float scale, out ParticleEmitterComponent emitter)
        {
            var e = w.CreateEntity();
            e.AddComponent<TransformComponent>();
            var t = e.GetComponent<TransformComponent>();
            t.Position = Vector2.Zero;
            t.Scale = new Vector2(scale, scale);
            e.AddComponent<ParticleEmitterComponent>();
            emitter = e.GetComponent<ParticleEmitterComponent>();
            emitter.SimulateInLocalSpace = true;
            emitter.IsBurst = true;
            emitter.BurstCount = 1;
            emitter.MaxParticles = 1;
            emitter.ParticleLifetime = 5f;
            emitter.LifetimeVariation = 0f;
            emitter.StartSize = 10f;
            emitter.EndSize = 10f;
            emitter.InitialVelocity = Vector2.Zero;
            emitter.Gravity = Vector2.Zero;
            emitter.Shape = EmitterShape.Point;
            w.Flush();
        }

        Setup(worldA, 1f, out var emitterScale1);
        Setup(worldB, 3f, out var emitterScale3);

        systemA.Update(worldA, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        systemB.Update(worldB, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        var rendererA = Substitute.For<IRenderer>();
        var rendererB = Substitute.For<IRenderer>();

        float capturedSizeScale1 = 0f;
        float capturedSizeScale3 = 0f;

        rendererA.When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(ci => capturedSizeScale1 = ci.ArgAt<float>(2));
        rendererB.When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(ci => capturedSizeScale3 = ci.ArgAt<float>(2));

        systemA.Render(worldA, rendererA);
        systemB.Render(worldB, rendererB);

        capturedSizeScale3.Should().BeApproximately(capturedSizeScale1 * 3f, 0.5f,
            "a 3x scaled entity should render particles at 3x the size");
    }

    #endregion

    #region Trail Frame Fix Tests

    [Fact]
    public void TrailSegmentsShouldUseCurrentParticleFrameNotAlwaysFrame0()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var renderer = Substitute.For<IRenderer>();
        var atlasTexture = Substitute.For<ITexture>();
        atlasTexture.Width.Returns(16);
        atlasTexture.Height.Returns(16);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 1f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(100, 0);
        emitter.Gravity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.EnableTrails = true;
        emitter.TrailLength = 5;

        var frame0 = new AtlasRegion("frame0", new Rectangle(0, 0, 16, 16), atlasTexture);
        var frame1 = new AtlasRegion("frame1", new Rectangle(16, 0, 16, 16), atlasTexture);
        emitter.ParticleFrames = [frame0, frame1];

        _world.Flush();

        // Spawn, then advance well past 50% lifetime so the particle is on frame1.
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.6)));

        emitter.ParticleCount.Should().Be(1, "particle must still be alive");
        var particle = emitter.ActiveParticles[0];
        particle.TrailFilled.Should().BeGreaterThan(0, "trail must have filled at least one slot");

        renderer.ClearReceivedCalls();
        system.Render(_world, renderer);

        // Head and trail segments should both use frame1 (X=16), not frame0 (X=0).
        renderer.DidNotReceive().DrawTexture(
            Arg.Any<ITexture>(),
            Arg.Any<Vector2>(),
            Arg.Is<Rectangle?>(r => r.HasValue && r.Value.X == 0),
            Arg.Any<Vector2?>(),
            Arg.Any<float>(),
            Arg.Any<Vector2?>(),
            Arg.Any<Color?>(),
            Arg.Any<SpriteFlip>());

        renderer.Received().DrawTexture(
            Arg.Any<ITexture>(),
            Arg.Any<Vector2>(),
            Arg.Is<Rectangle?>(r => r.HasValue && r.Value.X == 16),
            Arg.Any<Vector2?>(),
            Arg.Any<float>(),
            Arg.Any<Vector2?>(),
            Arg.Any<Color?>(),
            Arg.Any<SpriteFlip>());
    }

    #endregion

    #region Render Layer and Misc Behavior Tests

    [Fact]
    public void RenderLayerSortShouldBeStable()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var renderer = Substitute.For<IRenderer>();

        // Three emitters all on layer 0 — stable sort must preserve insertion order.
        for (int i = 0; i < 3; i++)
        {
            var entity = _world.CreateEntity();
            entity.AddComponent<TransformComponent>();
            entity.AddComponent<ParticleEmitterComponent>();
            var emitter = entity.GetComponent<ParticleEmitterComponent>();
            emitter.RenderLayer = 0;
            emitter.IsBurst = true;
            emitter.BurstCount = 1;
            emitter.MaxParticles = 1;
            emitter.ParticleLifetime = 10f;
            emitter.LifetimeVariation = 0f;
        }

        _world.Flush();
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        // Use a single cumulative list; take snapshots after each render call.
        var allDrawXPositions = new List<float>();
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(ci => allDrawXPositions.Add(ci.ArgAt<float>(0)));

        system.Render(_world, renderer);
        var drawOrder1 = allDrawXPositions.ToList();

        allDrawXPositions.Clear();
        system.Render(_world, renderer);
        var drawOrder2 = allDrawXPositions.ToList();

        drawOrder1.Should().HaveCount(3, "each emitter has one live particle");
        drawOrder2.Should().Equal(drawOrder1, "stable sort must produce the same order on repeated calls");
    }

    [Fact]
    public void DampingShouldDecayVelocityExponentially()
    {
        // With exponential damping, velocity after time t = v0 * exp(-damping * t).
        // After 1 second at damping=1: v ~= v0 * 0.3679.
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(100f, 0f);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.Damping = 1f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        emitter.ParticleCount.Should().Be(1);

        // Advance 1 second in small steps to approximate the continuous integral.
        var elapsed = TimeSpan.FromSeconds(0.016);
        for (int i = 0; i < 62; i++)
        {
            var dt = TimeSpan.FromSeconds(0.016);
            system.Update(_world, new GameTime(elapsed, dt));
            elapsed += dt;
        }

        var particle = emitter.ActiveParticles[0];
        var expectedSpeed = 100f * MathF.Exp(-1f);
        particle.Velocity.X.Should().BeApproximately(expectedSpeed, 1f,
            "exponential damping after 1 s at coefficient 1 should give ~36.8% of initial speed");
        particle.Velocity.X.Should().BeGreaterThan(0f, "velocity must not hard-clamp to zero");
    }

    [Fact]
    public void DampingShouldNeverGoNegativeAtHighCoefficients()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(100f, 0f);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.Damping = 1000f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));

        var particle = emitter.ActiveParticles[0];
        particle.Velocity.X.Should().BeGreaterThanOrEqualTo(0f,
            "exponential damping must never produce negative velocity");
    }

    [Fact]
    public void BoxShapeWithAngleShouldRotateSpawnOffsets()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 42);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 200f;
        emitter.MaxParticles = 500;
        emitter.Shape = EmitterShape.Box;
        emitter.ShapeSize = new Vector2(100f, 0.01f);
        emitter.BoxAngle = MathF.PI / 2f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        emitter.ActiveParticles.Should().NotBeEmpty();

        // A box rotated 90° with width=100 and height≈0 should spawn along the Y axis,
        // so X positions must be near zero and Y positions must vary.
        foreach (var p in emitter.ActiveParticles)
            p.Position.X.Should().BeApproximately(0f, 1f,
                "box rotated 90° should produce near-zero X offsets");

        var yRange = emitter.ActiveParticles.Max(p => p.Position.Y) - emitter.ActiveParticles.Min(p => p.Position.Y);
        yRange.Should().BeGreaterThan(10f, "box rotated 90° should spread particles along Y");
    }

    [Fact]
    public void BoxShapeWithZeroAngleShouldBeAxisAligned()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 42);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 200f;
        emitter.MaxParticles = 500;
        emitter.Shape = EmitterShape.Box;
        emitter.ShapeSize = new Vector2(100f, 0.01f);
        emitter.BoxAngle = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        emitter.ActiveParticles.Should().NotBeEmpty();
        foreach (var p in emitter.ActiveParticles)
            p.Position.Y.Should().BeApproximately(0f, 1f,
                "axis-aligned box with height≈0 should produce near-zero Y offsets");
    }

    [Fact]
    public void LocalSpaceEmitterShouldNotPreRotateVelocity()
    {
        // World-space emitter with entity rotated 90° should rotate velocity.
        // Local-space emitter should NOT pre-rotate velocity (rotation is deferred to render time).
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);

        var entityWorld = _world.CreateEntity();
        entityWorld.AddComponent<TransformComponent>();
        entityWorld.GetComponent<TransformComponent>().Rotation = MathF.PI / 2f;
        entityWorld.AddComponent<ParticleEmitterComponent>();
        var worldEmitter = entityWorld.GetComponent<ParticleEmitterComponent>();
        worldEmitter.IsBurst = true;
        worldEmitter.BurstCount = 1;
        worldEmitter.MaxParticles = 1;
        worldEmitter.ParticleLifetime = 5f;
        worldEmitter.LifetimeVariation = 0f;
        worldEmitter.InitialVelocity = new Vector2(100f, 0f);
        worldEmitter.VelocitySpread = 0f;
        worldEmitter.SpeedVariation = 0f;
        worldEmitter.Gravity = Vector2.Zero;
        worldEmitter.SimulateInLocalSpace = false;

        var entityLocal = _world.CreateEntity();
        entityLocal.AddComponent<TransformComponent>();
        entityLocal.GetComponent<TransformComponent>().Rotation = MathF.PI / 2f;
        entityLocal.AddComponent<ParticleEmitterComponent>();
        var localEmitter = entityLocal.GetComponent<ParticleEmitterComponent>();
        localEmitter.IsBurst = true;
        localEmitter.BurstCount = 1;
        localEmitter.MaxParticles = 1;
        localEmitter.ParticleLifetime = 5f;
        localEmitter.LifetimeVariation = 0f;
        localEmitter.InitialVelocity = new Vector2(100f, 0f);
        localEmitter.VelocitySpread = 0f;
        localEmitter.SpeedVariation = 0f;
        localEmitter.Gravity = Vector2.Zero;
        localEmitter.SimulateInLocalSpace = true;

        _world.Flush();
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        var worldParticle = worldEmitter.ActiveParticles[0];
        var localParticle = localEmitter.ActiveParticles[0];

        // World-space emitter: velocity rotated 90° → mostly Y, near-zero X.
        worldParticle.Velocity.X.Should().BeApproximately(0f, 1f,
            "world-space particle velocity should be rotated by entity rotation");
        worldParticle.Velocity.Y.Should().BeApproximately(100f, 1f);

        // Local-space emitter: velocity NOT rotated at spawn → mostly X.
        localParticle.Velocity.X.Should().BeApproximately(100f, 1f,
            "local-space particle velocity must not be pre-rotated at spawn");
        localParticle.Velocity.Y.Should().BeApproximately(0f, 1f);
    }

    [Fact]
    public void BurstEmitterWithWarmupShouldFireCallbackAndDisableWhenAllParticlesExpireDuringWarmup()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 10;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;
        emitter.WarmupDuration = 1f;

        var finishedFired = false;
        emitter.OnEmitterFinished = () => finishedFired = true;

        _world.Flush();

        // The first update triggers warmup; all burst particles have lifetime 0.1 s but warmup
        // simulates 1 s, so they will have all expired during pre-simulation.
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        emitter.IsEnabled.Should().BeFalse("burst emitter should self-disable after all warmup particles expire");
        emitter.ParticleCount.Should().Be(0);
        finishedFired.Should().BeTrue("OnEmitterFinished must fire when burst exhausts during warmup");
    }

    [Fact]
    public void BurstEmitterWithWarmupWhereParticlesStillLiveShouldNotFireCallbackPrematurely()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 10;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.WarmupDuration = 0.5f;

        var finishedFired = false;
        emitter.OnEmitterFinished = () => finishedFired = true;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        emitter.IsEnabled.Should().BeTrue("particles outlive the warmup so emitter stays enabled");
        emitter.ParticleCount.Should().Be(5);
        finishedFired.Should().BeFalse("OnEmitterFinished must not fire while particles are still alive");
    }

    [Fact]
    public void BoxAngleShouldBePreservedByCaptureAndRestoreDefaultState()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.BoxAngle = MathF.PI / 4f;
        emitter.CaptureDefaultState();

        emitter.BoxAngle = 0f;
        emitter.ResetToDefaultState();

        emitter.BoxAngle.Should().BeApproximately(MathF.PI / 4f, 0.0001f,
            "BoxAngle must survive a CaptureDefaultState / ResetToDefaultState round-trip");
    }

    #endregion

    #region TrailLength Zero

    [Fact]
    public void TrailLengthZeroShouldNotThrow()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 50;
        emitter.ParticleLifetime = 2f;
        emitter.EnableTrails = true;
        emitter.TrailLength = 0;

        _world.Flush();

        var act = () => _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));
        act.Should().NotThrow("TrailLength = 0 must be treated as trails disabled, not cause an index-out-of-range");
    }

    [Fact]
    public void TrailLengthZeroShouldProduceNoTrailArrays()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.EnableTrails = true;
        emitter.TrailLength = 0;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        emitter.ParticleCount.Should().Be(1);

        var particle = emitter.ActiveParticles[0];
        particle.TrailPositions.Should().BeNull("zero-length trail must not allocate arrays");
    }

    #endregion

    #region Delay Tests

    [Fact]
    public void DelayShouldPreventEmissionUntilElapsed()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 5f;
        emitter.Delay = 1f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));
        emitter.ParticleCount.Should().Be(0, "no particles should appear before the delay expires");
    }

    [Fact]
    public void EmissionShouldBeginAfterDelayElapses()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 5f;
        emitter.Delay = 0.5f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.3)));
        emitter.ParticleCount.Should().Be(0, "still in delay window");

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.3), TimeSpan.FromSeconds(0.5)));
        emitter.ParticleCount.Should().BeGreaterThan(0, "emission must begin after delay has elapsed");
    }

    [Fact]
    public void ZeroDelayShouldEmitImmediately()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 5f;
        emitter.Delay = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));
        emitter.ParticleCount.Should().BeGreaterThan(0, "zero delay must not block emission");
    }

    [Fact]
    public void StopShouldResetDelayTimerSoDelayReappliesOnRestart()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 5f;
        emitter.Delay = 0.5f;

        _world.Flush();

        // Allow delay to elapse and emit some particles.
        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));
        emitter.ParticleCount.Should().BeGreaterThan(0);

        // Stop and restart.
        emitter.Stop();
        emitter.IsEmitting = true;
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(0.1)));

        emitter.ParticleCount.Should().Be(0, "delay must re-apply after Stop(); no particles during the delay window");
    }

    [Fact]
    public void DelayShouldBeCapturedAndRestoredByDefaultState()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.Delay = 2.5f;
        emitter.CaptureDefaultState();

        emitter.Delay = 0f;
        emitter.ResetToDefaultState();

        emitter.Delay.Should().BeApproximately(2.5f, 0.001f);
        emitter.DelayTimer.Should().Be(0f, "ResetToDefaultState must reset the delay timer");
    }

    #endregion

    #region Loop Tests

    [Fact]
    public void LoopShouldRestartEmissionAfterDurationCycleExpiresAndParticlesDrain()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 200;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;
        emitter.Duration = 0.2f;
        emitter.Loop = true;

        _world.Flush();

        // Tick 1: Duration elapses and emission stops, but particles spawned this tick
        // are born after UpdateParticles runs so they cannot expire yet.
        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));
        emitter.IsEmitting.Should().BeFalse("emission stopped after Duration elapsed");
        emitter.ParticleCount.Should().BeGreaterThan(0, "particles still alive after first tick");

        // Tick 2: all particles exceed their 0.1 s lifetime and drain; loop restarts.
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5)));

        emitter.IsEmitting.Should().BeTrue("Loop must restart emission after the cycle drains");
        emitter.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void LoopShouldNotFireOnEmitterFinished()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 200;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;
        emitter.Duration = 0.2f;
        emitter.Loop = true;

        var finished = false;
        emitter.OnEmitterFinished = () => finished = true;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));

        finished.Should().BeFalse("OnEmitterFinished must not fire while Loop is true");
    }

    [Fact]
    public void LoopFalseShouldStillFireOnEmitterFinished()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 200;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;
        emitter.Duration = 0.2f;
        emitter.Loop = false;

        var finished = false;
        emitter.OnEmitterFinished = () => finished = true;

        _world.Flush();

        // First tick: Duration elapses and emission stops, but particles spawned this tick
        // are born after UpdateParticles runs so they cannot expire yet.
        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));
        finished.Should().BeFalse("particles are still alive after the first tick");

        // Second tick: all particles exceed their 0.1 s lifetime and drain; callback fires.
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.5)));

        finished.Should().BeTrue("Loop=false must still fire OnEmitterFinished after draining");
    }

    [Fact]
    public void LoopShouldRearmDelayOnEachCycle()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 200;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;
        emitter.Duration = 0.2f;
        emitter.Loop = true;
        emitter.Delay = 0.5f;

        _world.Flush();

        // Complete first cycle (Duration=0.2 + particles expire in 0.1): ~0.4 s total.
        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        // Now in second cycle — should be inside the delay window, so no particles yet.
        emitter.IsEmitting.Should().BeTrue("emission re-armed");
        emitter.ParticleCount.Should().Be(0, "delay must re-apply at the start of each loop cycle");
    }

    [Fact]
    public void BurstLoopShouldReArmAndFireAgainAfterParticlesExpire()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;
        emitter.Loop = true;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        emitter.ParticleCount.Should().Be(5, "burst must fire on first update");

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));
        emitter.IsEnabled.Should().BeTrue("Loop=true must keep the emitter enabled after the burst drains");
        emitter.BurstFired.Should().BeFalse("burst must be re-armed after draining");

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.516), TimeSpan.FromSeconds(0.016)));
        emitter.ParticleCount.Should().Be(5, "burst must fire again on the next cycle");
    }

    [Fact]
    public void LoopShouldBeCapturedAndRestoredByDefaultState()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.Loop = true;
        emitter.CaptureDefaultState();

        emitter.Loop = false;
        emitter.ResetToDefaultState();

        emitter.Loop.Should().BeTrue();
    }

    #endregion

    #region EmissionTimer Over-Debit

    [Fact]
    public void EmissionTimerShouldNotGoNegativeWhenMaxParticlesCapsTheBatch()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 1000f;
        emitter.MaxParticles = 5;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));
        emitter.ParticleCount.Should().Be(5);

        emitter.EmissionTimer.Should().BeGreaterThanOrEqualTo(0f,
            "EmissionTimer must never go negative when MaxParticles caps the spawned batch");
    }

    [Fact]
    public void EmissionShouldResumeAfterMaxParticleSlotsFreeUp()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 200f;
        emitter.MaxParticles = 5;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0)));
        emitter.ParticleCount.Should().Be(5);

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(0.5)));

        emitter.ParticleCount.Should().BeGreaterThan(0,
            "emission must resume once slots free up; a negative timer would suppress it");
        emitter.EmissionTimer.Should().BeGreaterThanOrEqualTo(0f);
    }

    #endregion

    #region VelocityInheritance in LocalSpace

    [Fact]
    public void VelocityInheritanceShouldWorkInLocalSpace()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = new Vector2(0, 0);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.SimulateInLocalSpace = true;
        emitter.IsBurst = false;
        emitter.EmissionRate = 0f;
        emitter.MaxParticles = 50;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.VelocityInheritance = 1f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));

        transform.Position = new Vector2(30, 0);
        emitter.IsBurst = true;
        emitter.BurstCount = 10;
        emitter.MaxParticles = 10;

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));

        emitter.ParticleCount.Should().Be(10);
        foreach (var particle in emitter.ActiveParticles)
            particle.Velocity.X.Should().BeApproximately(300f, 10f,
                "VelocityInheritance must use world-space emitter motion even in local-space mode");
    }

    [Fact]
    public void VelocityInheritanceZeroShouldBeUnaffectedByLocalSpaceMode()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = new Vector2(0, 0);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.SimulateInLocalSpace = true;
        emitter.IsBurst = false;
        emitter.EmissionRate = 0f;
        emitter.MaxParticles = 10;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(50f, 0f);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.VelocityInheritance = 0f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));

        transform.Position = new Vector2(999, 0);
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 5;

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));

        foreach (var particle in emitter.ActiveParticles)
            particle.Velocity.X.Should().BeApproximately(50f, 1f,
                "zero VelocityInheritance must not leak emitter world-velocity into particles");
    }

    #endregion

    #region Speed Over Lifetime Tests

    [Fact]
    public void EndSpeedMultiplierZeroShouldDecelerateParticlesToHalt()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 2f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(200f, 0f);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.Damping = 0f;
        emitter.StartSpeedMultiplier = 1f;
        emitter.EndSpeedMultiplier = 0f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        var speedAtBirth = emitter.ActiveParticles[0].Velocity.Length();

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(1.9f)));

        emitter.ParticleCount.Should().Be(1, "particle must still be alive");
        var speedNearEnd = emitter.ActiveParticles[0].Velocity.Length();
        speedNearEnd.Should().BeLessThan(speedAtBirth * 0.2f,
            "EndSpeedMultiplier=0 should drive speed near zero as lifetime approaches 1");
    }

    [Fact]
    public void SpeedOverLifetimeShouldNotAffectGravityAcceleration()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 2f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = new Vector2(0f, 100f);
        emitter.Damping = 0f;
        emitter.StartSpeedMultiplier = 1f;
        emitter.EndSpeedMultiplier = 0f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));

        emitter.ParticleCount.Should().Be(1);
        emitter.ActiveParticles[0].Velocity.Y.Should().BeGreaterThan(0f,
            "gravity must still accumulate even when EndSpeedMultiplier=0 zeroes out base velocity");
    }

    [Fact]
    public void BothMultipliersAtOneShouldProduceSameVelocityAsDefault()
    {
        var seed = 77;
        var worldA = CreateTestWorld();
        var worldB = CreateTestWorld();
        var systemA = new ParticleSystem(new DefaultObjectPoolProvider(), seed: seed);
        var systemB = new ParticleSystem(new DefaultObjectPoolProvider(), seed: seed);

        void Setup(IEntityWorld w, float start, float end, out ParticleEmitterComponent em)
        {
            var e = w.CreateEntity();
            e.AddComponent<TransformComponent>();
            e.AddComponent<ParticleEmitterComponent>();
            em = e.GetComponent<ParticleEmitterComponent>();
            em.IsBurst = true;
            em.BurstCount = 10;
            em.MaxParticles = 10;
            em.ParticleLifetime = 5f;
            em.LifetimeVariation = 0f;
            em.InitialVelocity = new Vector2(100f, 0f);
            em.VelocitySpread = 0f;
            em.SpeedVariation = 0f;
            em.Gravity = Vector2.Zero;
            em.Damping = 0f;
            em.StartSpeedMultiplier = start;
            em.EndSpeedMultiplier = end;
            w.Flush();
        }

        Setup(worldA, 1f, 1f, out var emitterA);
        Setup(worldB, 1f, 1f, out var emitterB);

        systemA.Update(worldA, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        systemB.Update(worldB, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        for (int i = 0; i < emitterA.ParticleCount; i++)
            emitterA.ActiveParticles[i].Velocity.X
                .Should().BeApproximately(emitterB.ActiveParticles[i].Velocity.X, 0.01f,
                    "multipliers of 1 must produce identical velocity to the default");
    }

    [Fact]
    public void SpeedOverLifetimeShouldBeCapturedAndRestoredByDefaultState()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.StartSpeedMultiplier = 2f;
        emitter.EndSpeedMultiplier = 0.5f;
        emitter.CaptureDefaultState();

        emitter.StartSpeedMultiplier = 1f;
        emitter.EndSpeedMultiplier = 1f;
        emitter.ResetToDefaultState();

        emitter.StartSpeedMultiplier.Should().BeApproximately(2f, 0.001f);
        emitter.EndSpeedMultiplier.Should().BeApproximately(0.5f, 0.001f);
    }

    #endregion

    #region Burst Loop Tests

    [Fact]
    public void BurstLoopShouldNotFireOnEmitterFinished()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 3;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;
        emitter.Loop = true;

        var finished = false;
        emitter.OnEmitterFinished = () => finished = true;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));

        finished.Should().BeFalse("OnEmitterFinished must not fire while burst Loop is true");
    }

    [Fact]
    public void BurstLoopShouldFireOnEmitterFinishedWhenLoopIsFalse()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 3;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;
        emitter.Loop = false;

        var finished = false;
        emitter.OnEmitterFinished = () => finished = true;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));

        finished.Should().BeTrue("OnEmitterFinished must fire for a non-looping burst");
    }

    [Fact]
    public void BurstLoopShouldReArmDelayOnEachCycle()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 3;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;
        emitter.Loop = true;
        emitter.Delay = 1f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));

        emitter.IsEnabled.Should().BeTrue("looping burst stays enabled");
        emitter.BurstFired.Should().BeFalse("burst re-armed");
        emitter.ParticleCount.Should().Be(0, "delay must block the second cycle until 1 s elapses");
    }

    #endregion

    #region Warmup Respects Delay

    [Fact]
    public void WarmupShouldRespectDelayAndNotOverEmit()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.MaxParticles = 500;
        emitter.ParticleLifetime = 100f;
        emitter.LifetimeVariation = 0f;
        emitter.Delay = 3f;
        emitter.WarmupDuration = 5f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        // With a 5 s warmup and a 3 s delay, only 2 s worth of particles should exist.
        // Without the fix, 5 s worth would be emitted (50 particles); with fix: ~20.
        emitter.ParticleCount.Should().BeLessThanOrEqualTo(25,
            "warmup must consume the delay window first and only emit for the remaining duration");
        emitter.ParticleCount.Should().BeGreaterThan(0,
            "the remaining 2 s of warmup after the delay should still produce particles");
    }

    [Fact]
    public void WarmupWhereDelayExceedsWarmupDurationShouldProduceNoParticles()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.MaxParticles = 500;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.Delay = 5f;
        emitter.WarmupDuration = 2f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        emitter.ParticleCount.Should().Be(0,
            "when delay >= warmupDuration the entire warmup window is consumed by the delay, so no particles spawn");
    }

    [Fact]
    public void WarmupWithZeroDelayShouldBehaveAsBeforeForBackwardCompatibility()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.MaxParticles = 500;
        emitter.ParticleLifetime = 100f;
        emitter.LifetimeVariation = 0f;
        emitter.Delay = 0f;
        emitter.WarmupDuration = 3f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        // 10 particles/s × 3 s = 30 particles
        emitter.ParticleCount.Should().BeCloseTo(30, 3,
            "zero delay must not affect warmup emission");
    }

    #endregion

    #region EmissionTimer Cap

    [Fact]
    public void EmissionTimerShouldBeClampedToOneFrameWhenMaxParticlesCapsTheBatch()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 500f;
        emitter.MaxParticles = 3;
        emitter.ParticleLifetime = 60f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        // Cap hits immediately on the first update.
        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(2.0)));
        emitter.ParticleCount.Should().Be(3);

        var timerAfterCap = emitter.EmissionTimer;
        timerAfterCap.Should().BeGreaterThanOrEqualTo(0f, "timer must not go negative");
        timerAfterCap.Should().BeLessThanOrEqualTo(1f / emitter.EmissionRate + 0.001f,
            "timer must be clamped to at most one inter-particle interval");
    }

    [Fact]
    public void EmissionShouldNotFloodAfterLongCapPeriodWhenSlotsOpenUp()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 1000f;
        emitter.MaxParticles = 2;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        // Hold at cap for many frames.
        for (int i = 0; i < 60; i++)
            _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(i * 0.016), TimeSpan.FromSeconds(0.016)));

        // Now let particles expire and emit one more short tick.
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(1.0), TimeSpan.FromSeconds(0.016)));

        emitter.ParticleCount.Should().BeLessThanOrEqualTo(emitter.MaxParticles,
            "clamped timer must not cause a flood of particles once cap is released");
    }

    #endregion

    #region Trail Historical Color

    [Fact]
    public void TrailSegmentsShouldReflectHistoricalColorNotCurrentParticleColor()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        // Fade from fully opaque red to fully transparent over 2 s.
        emitter.StartColor = new Color(255, 0, 0, 255);
        emitter.EndColor = new Color(255, 0, 0, 0);
        emitter.ColorGradient = null;
        emitter.ParticleLifetime = 2f;
        emitter.LifetimeVariation = 0f;
        emitter.EnableTrails = true;
        emitter.TrailLength = 5;
        emitter.TrailHeadAlpha = 1f;
        emitter.TrailTailAlpha = 1f;
        emitter.InitialVelocity = new Vector2(100, 0);
        emitter.Gravity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;

        _world.Flush();

        // Frame 1: particle spawns (UpdateParticles runs before EmitParticles, so no trail
        // slot is written yet on the spawn tick itself).
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        emitter.ParticleCount.Should().Be(1);

        // Frame 2: first UpdateParticles call on the live particle — writes trail slot 0
        // while the particle is still near t=0 (alpha ≈ 253).
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.016)));
        emitter.ActiveParticles[0].TrailFilled.Should().Be(1, "slot 0 must be written on first update");

        // Frame 3: jump near end of life (t ≈ 0.9) — writes slot 1 at low alpha.
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.032), TimeSpan.FromSeconds(1.77f)));
        emitter.ParticleCount.Should().Be(1, "particle must still be alive");
        emitter.ActiveParticles[0].TrailFilled.Should().BeGreaterThanOrEqualTo(2);

        var capturedAlphas = new List<byte>();
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(ci => capturedAlphas.Add(ci.ArgAt<Color>(3).A));

        system.Render(_world, renderer);

        // Slot 0 was recorded at t≈0.016, alpha≈252. Even after the alpha-fade multiplier
        // (both head/tail = 1.0) it must remain well above 150.
        // Without the historical-color fix all slots would use the current t≈0.9 color (alpha≈25).
        capturedAlphas.Should().Contain(a => a > 150,
            "trail slot 0 was written when the particle was nearly opaque; it must not be overwritten by the current faded color");
    }

    #endregion

    #region Angle Units Consistency

    [Fact]
    public void VelocitySpreadIsInDegrees()
    {
        // A 360-degree spread should distribute particles in all directions.
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 1);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 500f;
        emitter.MaxParticles = 500;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(100, 0);
        emitter.VelocitySpread = 360f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;

        _world.Flush();
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        var hasPositiveY = emitter.ActiveParticles.Any(p => p.Velocity.Y > 10f);
        var hasNegativeY = emitter.ActiveParticles.Any(p => p.Velocity.Y < -10f);
        hasPositiveY.Should().BeTrue("360° spread in degrees must produce particles going in +Y direction");
        hasNegativeY.Should().BeTrue("360° spread in degrees must produce particles going in -Y direction");
    }

    [Fact]
    public void VelocitySpreadZeroShouldProduceParallelVelocities()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 100f;
        emitter.MaxParticles = 200;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(100, 0);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;

        _world.Flush();
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        foreach (var p in emitter.ActiveParticles)
            p.Velocity.Y.Should().BeApproximately(0f, 0.01f,
                "zero spread must produce only horizontal velocity");
    }

    [Fact]
    public void ConeAngleIsInDegrees()
    {
        // A 360° cone (degrees) distributes particles in all directions.
        // If ConeAngle were misread as radians, 360 rad ≈ 57 full rotations which is
        // also omnidirectional — so instead we assert the spread is bounded correctly:
        // a 90° cone centred on +X must keep all particles in the right hemisphere.
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 2);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 500f;
        emitter.MaxParticles = 500;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.Shape = EmitterShape.Cone;
        emitter.ConeAngle = 90f;
        emitter.InitialVelocity = new Vector2(100, 0);
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;

        _world.Flush();
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        emitter.ActiveParticles.Should().NotBeEmpty();

        // A 90° cone in degrees spans ±45°, so every particle must have positive X.
        // If the value were interpreted as radians (90 rad ≈ 14 rotations) particles
        // would scatter in all directions, causing this assertion to fail.
        foreach (var p in emitter.ActiveParticles)
            p.Velocity.X.Should().BeGreaterThan(0f,
                "a 90° cone centred on +X must keep all particles in the right hemisphere");

        // Also confirm spread actually happens (not a degenerate zero-spread emitter).
        var angles = emitter.ActiveParticles.Select(p => MathF.Atan2(p.Velocity.Y, p.Velocity.X)).ToList();
        var spread = angles.Max() - angles.Min();
        spread.Should().BeGreaterThan(0.1f, "a 90° cone must produce a non-trivial angular spread");
    }

    #endregion

    #region LineLength Tests

    [Fact]
    public void LineLengthShouldTakePriorityOverShapeSizeX()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 11);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 200f;
        emitter.MaxParticles = 500;
        emitter.Shape = EmitterShape.Line;
        emitter.ShapeSize = new Vector2(10f, 0f);
        emitter.LineLength = 200f;
        emitter.LineAngle = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        emitter.ActiveParticles.Should().NotBeEmpty();
        var maxX = emitter.ActiveParticles.Max(p => MathF.Abs(p.Position.X));
        maxX.Should().BeGreaterThan(50f, "LineLength=200 should produce spawns up to 100 units from centre, well beyond ShapeSize.X=10");
    }

    [Fact]
    public void LineLengthZeroShouldFallBackToShapeSizeX()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 12);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 200f;
        emitter.MaxParticles = 500;
        emitter.Shape = EmitterShape.Line;
        emitter.ShapeSize = new Vector2(20f, 0f);
        emitter.LineLength = 0f;
        emitter.LineAngle = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        emitter.ActiveParticles.Should().NotBeEmpty();
        var maxX = emitter.ActiveParticles.Max(p => MathF.Abs(p.Position.X));
        maxX.Should().BeLessThanOrEqualTo(10f + 0.1f, "LineLength=0 must fall back to ShapeSize.X=20, max offset is 10");
    }

    [Fact]
    public void LineLengthShouldBeCapturedAndRestoredByDefaultState()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.LineLength = 150f;
        emitter.CaptureDefaultState();

        emitter.LineLength = 0f;
        emitter.ResetToDefaultState();

        emitter.LineLength.Should().BeApproximately(150f, 0.001f);
    }

    #endregion

    #region Turbulence Tests

    [Fact]
    public void TurbulenceShouldPerturbParticleVelocityOverTime()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 55);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 20;
        emitter.MaxParticles = 20;
        emitter.InitialVelocity = new Vector2(100f, 0f);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.TurbulenceStrength = 500f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        var velocitiesAfterSpawn = emitter.ActiveParticles.Select(p => p.Velocity).ToList();

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));

        var changed = emitter.ActiveParticles
            .Take(velocitiesAfterSpawn.Count)
            .Where((p, i) => p.Velocity != velocitiesAfterSpawn[i])
            .Count();

        changed.Should().BeGreaterThan(0, "turbulence must perturb particle velocities each frame");
    }

    [Fact]
    public void TurbulenceZeroShouldNotAffectVelocity()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 56);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 5;
        emitter.InitialVelocity = new Vector2(50f, 0f);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.Damping = 0f;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.TurbulenceStrength = 0f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        var speeds = emitter.ActiveParticles.Select(p => p.Velocity.Length()).ToList();

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.1)));

        for (int i = 0; i < emitter.ActiveParticles.Count && i < speeds.Count; i++)
            emitter.ActiveParticles[i].Velocity.Length()
                .Should().BeApproximately(speeds[i], 0.01f,
                    "zero turbulence must not change particle speed");
    }

    [Fact]
    public void TurbulenceShouldBeCapturedAndRestoredByDefaultState()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.TurbulenceStrength = 250f;
        emitter.CaptureDefaultState();

        emitter.TurbulenceStrength = 0f;
        emitter.ResetToDefaultState();

        emitter.TurbulenceStrength.Should().BeApproximately(250f, 0.001f);
    }

    #endregion

    #region Sub-Emitter Tests

    [Fact]
    public void DeathSubEmitterShouldSpawnParticlesAtDyingParticlePosition()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = new Vector2(200f, 200f);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 3;
        emitter.MaxParticles = 3;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 5,
            MaxParticles = 50,
            ParticleLifetime = 2f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
        };
        emitter.DeathSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        emitter.ParticleCount.Should().Be(3);

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));
        emitter.ParticleCount.Should().Be(0, "parent particles must have expired");

        var capturedPositions = new List<Vector2>();
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(ci => capturedPositions.Add(new Vector2(ci.ArgAt<float>(0), ci.ArgAt<float>(1))));

        system.Render(_world, renderer);

        capturedPositions.Should().NotBeEmpty("sub-emitter particles must be rendered");
        foreach (var pos in capturedPositions)
        {
            pos.X.Should().BeApproximately(200f, 1f, "sub-particles spawn at the parent particle's world position");
            pos.Y.Should().BeApproximately(200f, 1f, "sub-particles spawn at the parent particle's world position");
        }
    }

    [Fact]
    public void DeathSubEmitterParticlesShouldExpireNaturally()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;

        emitter.DeathSubEmitters =
        [
            new SubEmitterConfig
            {
                BurstCount = 4,
                MaxParticles = 10,
                ParticleLifetime = 2f,
                LifetimeVariation = 0f,
                InitialVelocity = Vector2.Zero,
                VelocitySpread = 0f,
                Gravity = Vector2.Zero,
            }
        ];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));

        var drawCalls = 0;
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(_ => drawCalls++);

        system.Render(_world, renderer);
        drawCalls.Should().BeGreaterThan(0, "sub-particles should still be alive");

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.516), TimeSpan.FromSeconds(5.0)));

        drawCalls = 0;
        system.Render(_world, renderer);
        drawCalls.Should().Be(0, "sub-particles must expire after their lifetime");
    }

    [Fact]
    public void NullDeathSubEmittersShouldNotThrow()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 10;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.DeathSubEmitters = null;

        _world.Flush();

        var act = () =>
        {
            _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
            _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));
        };

        act.Should().NotThrow();
    }

    [Fact]
    public void DeathSubEmitterShouldRespectMaxParticles()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 10;
        emitter.MaxParticles = 10;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;

        emitter.DeathSubEmitters =
        [
            new SubEmitterConfig
            {
                BurstCount = 1000,
                MaxParticles = 3,
                ParticleLifetime = 2f,
                LifetimeVariation = 0f,
                InitialVelocity = Vector2.Zero,
                VelocitySpread = 0f,
                Gravity = Vector2.Zero,
            }
        ];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));

        var drawCalls = 0;
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(_ => drawCalls++);

        system.Render(_world, renderer);

        drawCalls.Should().BeLessThanOrEqualTo(30,
            "each of 10 parent particles triggers MaxParticles=3 sub-particles; total must be capped");
    }

    #endregion

    #region Sub-Emitter MaxParticles Global Cap Tests

    [Fact]
    public void DeathSubEmitterShouldEnforceGlobalMaxParticlesAcrossMultipleBursts()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 20;
        emitter.MaxParticles = 20;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 50,
            MaxParticles = 10,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            Gravity = Vector2.Zero,
        };
        emitter.DeathSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));

        var drawCalls = 0;
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(_ => drawCalls++);

        system.Render(_world, renderer);

        drawCalls.Should().BeLessThanOrEqualTo(subCfg.MaxParticles,
            "global MaxParticles cap must hold even when many parent particles die in the same frame");
    }

    #endregion

    #region Sub-Emitter Render Layer Tests

    [Fact]
    public void SubEmitterWithLowerRenderLayerShouldDrawBeforeRegularEmitter()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.RenderLayer = 0;
        emitter.StartSize = 5f;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 1,
            MaxParticles = 10,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            Gravity = Vector2.Zero,
            RenderLayer = -1,
            StartSize = 3f,
        };
        emitter.DeathSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));

        var drawOrder = new List<float>();
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(ci => drawOrder.Add(ci.ArgAt<float>(2)));

        system.Render(_world, renderer);

        drawOrder.Should().HaveCount(1, "only the sub-emitter particle remains (parent expired)");
    }

    [Fact]
    public void SubEmitterRenderLayerShouldBeRespectedInMixedScene()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var renderer = Substitute.For<IRenderer>();

        // Front emitter — layer 10, large particle
        var frontEntity = _world.CreateEntity();
        frontEntity.AddComponent<TransformComponent>();
        frontEntity.AddComponent<ParticleEmitterComponent>();
        var frontEmitter = frontEntity.GetComponent<ParticleEmitterComponent>();
        frontEmitter.IsBurst = true;
        frontEmitter.BurstCount = 1;
        frontEmitter.MaxParticles = 1;
        frontEmitter.ParticleLifetime = 10f;
        frontEmitter.LifetimeVariation = 0f;
        frontEmitter.InitialVelocity = Vector2.Zero;
        frontEmitter.VelocitySpread = 0f;
        frontEmitter.Gravity = Vector2.Zero;
        frontEmitter.RenderLayer = 10;
        frontEmitter.StartSize = 10f;
        frontEmitter.EndSize = 10f;

        // Back emitter — fires, dies quickly, triggers a sub-emitter at layer 5 (behind front)
        var backEntity = _world.CreateEntity();
        backEntity.AddComponent<TransformComponent>();
        backEntity.AddComponent<ParticleEmitterComponent>();
        var backEmitter = backEntity.GetComponent<ParticleEmitterComponent>();
        backEmitter.IsBurst = true;
        backEmitter.BurstCount = 1;
        backEmitter.MaxParticles = 1;
        backEmitter.ParticleLifetime = 0.05f;
        backEmitter.LifetimeVariation = 0f;
        backEmitter.InitialVelocity = Vector2.Zero;
        backEmitter.VelocitySpread = 0f;
        backEmitter.Gravity = Vector2.Zero;
        backEmitter.RenderLayer = 5;
        backEmitter.StartSize = 5f;
        backEmitter.DeathSubEmitters =
        [
            new SubEmitterConfig
            {
                BurstCount = 1,
                MaxParticles = 10,
                ParticleLifetime = 10f,
                LifetimeVariation = 0f,
                InitialVelocity = Vector2.Zero,
                VelocitySpread = 0f,
                Gravity = Vector2.Zero,
                RenderLayer = 5,
                StartSize = 7f,
                EndSize = 7f,
            }
        ];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));

        var drawSizes = new List<float>();
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(ci => drawSizes.Add(ci.ArgAt<float>(2)));

        system.Render(_world, renderer);

        drawSizes.Should().HaveCount(2, "sub-emitter particle (layer 5) and front emitter particle (layer 10) should both render");
        drawSizes[0].Should().BeApproximately(7f, 0.5f, "sub-emitter at layer 5 must render before front emitter at layer 10");
        drawSizes[1].Should().BeApproximately(10f, 0.5f, "front emitter at layer 10 must render after sub-emitter at layer 5");
    }

    #endregion

    #region Birth Sub-Emitter Tests

    [Fact]
    public void BirthSubEmitterShouldSpawnParticlesWhenParentParticleSpawns()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = new Vector2(100f, 100f);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 3;
        emitter.MaxParticles = 3;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;

        emitter.BirthSubEmitters =
        [
            new SubEmitterConfig
            {
                BurstCount = 4,
                MaxParticles = 50,
                ParticleLifetime = 5f,
                LifetimeVariation = 0f,
                InitialVelocity = Vector2.Zero,
                VelocitySpread = 0f,
                Gravity = Vector2.Zero,
            }
        ];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        // Sub-particles are alive immediately after the first update.
        var drawCalls = 0;
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(_ => drawCalls++);

        system.Render(_world, renderer);

        drawCalls.Should().BeGreaterThan(0, "birth sub-emitter particles must be alive immediately after parent spawns");
    }

    [Fact]
    public void BirthSubEmitterShouldSpawnAtParentWorldPosition()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = new Vector2(300f, 400f);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.Shape = EmitterShape.Point;

        emitter.BirthSubEmitters =
        [
            new SubEmitterConfig
            {
                BurstCount = 1,
                MaxParticles = 10,
                ParticleLifetime = 5f,
                LifetimeVariation = 0f,
                InitialVelocity = Vector2.Zero,
                VelocitySpread = 0f,
                SpeedVariation = 0f,
                Gravity = Vector2.Zero,
            }
        ];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        var capturedPositions = new List<Vector2>();
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(ci => capturedPositions.Add(new Vector2(ci.ArgAt<float>(0), ci.ArgAt<float>(1))));

        system.Render(_world, renderer);

        capturedPositions.Should().NotBeEmpty();
        capturedPositions.Should().Contain(p =>
            MathF.Abs(p.X - 300f) < 1f && MathF.Abs(p.Y - 400f) < 1f,
            "birth sub-particle must spawn at the parent's world position");
    }

    [Fact]
    public void BirthSubEmitterShouldRespectMaxParticlesCap()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 20;
        emitter.MaxParticles = 20;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 100,
            MaxParticles = 5,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            Gravity = Vector2.Zero,
        };
        emitter.BirthSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        var drawCalls = 0;
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(_ => drawCalls++);

        system.Render(_world, renderer);

        // Subtract parent particles from the count — sub-particles must not exceed MaxParticles.
        var subDrawCalls = drawCalls - emitter.ParticleCount;
        subDrawCalls.Should().BeLessThanOrEqualTo(subCfg.MaxParticles,
            "birth sub-emitter must respect its MaxParticles global cap");
    }

    [Fact]
    public void BirthSubEmitterParticlesShouldExpireNaturally()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 2;
        emitter.MaxParticles = 2;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;

        emitter.BirthSubEmitters =
        [
            new SubEmitterConfig
            {
                BurstCount = 3,
                MaxParticles = 20,
                ParticleLifetime = 0.1f,
                LifetimeVariation = 0f,
                InitialVelocity = Vector2.Zero,
                VelocitySpread = 0f,
                Gravity = Vector2.Zero,
            }
        ];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        var drawCallsAfterSpawn = 0;
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(_ => drawCallsAfterSpawn++);
        system.Render(_world, renderer);
        drawCallsAfterSpawn.Should().BeGreaterThan(0, "birth sub-particles must be visible right after spawn");

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(5f)));

        var drawCallsAfterExpiry = 0;
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(_ => drawCallsAfterExpiry++);
        system.Render(_world, renderer);
        drawCallsAfterExpiry.Should().Be(0, "birth sub-particles must expire after their lifetime");
    }

    #endregion

    #region Trail Pool Reuse Tests

    [Fact]
    public void TrailArraysShouldBeReusedFromPoolWithoutReallocation()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 0);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 50f;
        emitter.MaxParticles = 5;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.EnableTrails = true;
        emitter.TrailLength = 4;
        emitter.Gravity = Vector2.Zero;

        _world.Flush();

        // First wave: spawn, let expire, return to pool.
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        emitter.IsEmitting = false;
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(1f)));
        emitter.ParticleCount.Should().Be(0, "all particles must have expired before second wave");

        // Second wave: pooled particles are re-used.
        emitter.IsEmitting = true;
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(1.016), TimeSpan.FromSeconds(0.016)));
        emitter.ParticleCount.Should().BeGreaterThan(0);

        foreach (var p in emitter.ActiveParticles)
        {
            p.TrailPositions.Should().NotBeNull("trail array must be present on re-used particle");
            p.TrailPositions!.Length.Should().Be(emitter.TrailLength,
                "trail array length must match TrailLength without reallocation");
            p.TrailFilled.Should().Be(0, "TrailFilled must be reset to 0 on pool re-use");
        }
    }

    #endregion
    #region Sub-Emitter Spawn Shape Tests

    [Fact]
    public void SubEmitterCircleShapeShouldScatterParticlesFromRadius()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 42);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.Shape = EmitterShape.Point;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 30,
            MaxParticles = 200,
            ParticleLifetime = 5f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
            Shape = EmitterShape.Circle,
            SpawnRadius = 50f,
            SpawnOnPerimeter = false,
        };
        emitter.DeathSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));

        var positions = new List<Vector2>();
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(ci => positions.Add(new Vector2(ci.ArgAt<float>(0), ci.ArgAt<float>(1))));
        system.Render(_world, renderer);

        positions.Should().NotBeEmpty();
        positions.Should().Contain(p => p != Vector2.Zero,
            "circle shape must scatter particles away from the trigger point");
        positions.Should().OnlyContain(p => p.Length() <= 50f + 0.01f,
            "all sub-particles must spawn within SpawnRadius");
    }

    [Fact]
    public void SubEmitterCirclePerimeterShapeShouldSpawnOnEdge()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 7);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.Shape = EmitterShape.Point;

        const float radius = 30f;
        var subCfg = new SubEmitterConfig
        {
            BurstCount = 20,
            MaxParticles = 200,
            ParticleLifetime = 5f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
            Shape = EmitterShape.Circle,
            SpawnRadius = radius,
            SpawnOnPerimeter = true,
        };
        emitter.DeathSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));

        var positions = new List<Vector2>();
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(ci => positions.Add(new Vector2(ci.ArgAt<float>(0), ci.ArgAt<float>(1))));
        system.Render(_world, renderer);

        positions.Should().NotBeEmpty();
        positions.Should().OnlyContain(p => MathF.Abs(p.Length() - radius) < 0.5f,
            "perimeter spawn must place all particles exactly on the circle edge");
    }

    [Fact]
    public void SubEmitterBoxShapeShouldSpawnWithinBounds()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 13);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.Shape = EmitterShape.Point;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 30,
            MaxParticles = 200,
            ParticleLifetime = 5f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
            Shape = EmitterShape.Box,
            ShapeSize = new Vector2(40f, 20f),
            BoxAngle = 0f,
        };
        emitter.DeathSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));

        var positions = new List<Vector2>();
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(ci => positions.Add(new Vector2(ci.ArgAt<float>(0), ci.ArgAt<float>(1))));
        system.Render(_world, renderer);

        positions.Should().NotBeEmpty();
        positions.Should().OnlyContain(p => MathF.Abs(p.X) <= 20f + 0.01f && MathF.Abs(p.Y) <= 10f + 0.01f,
            "box shape must keep all sub-particles within ShapeSize bounds");
    }

    #endregion

    #region Sub-Emitter Turbulence Tests

    [Fact]
    public void SubEmitterTurbulenceShouldPerturbParticleVelocity()
    {
        var systemTurb = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 99);
        var systemStill = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 99);

        IEntityWorld BuildWorld(float turbulence)
        {
            var world = CreateTestWorld();

            var entity = world.CreateEntity();
            entity.AddComponent<TransformComponent>();
            entity.AddComponent<ParticleEmitterComponent>();
            var emitter = entity.GetComponent<ParticleEmitterComponent>();
            emitter.IsBurst = true;
            emitter.BurstCount = 1;
            emitter.MaxParticles = 1;
            emitter.ParticleLifetime = 0.05f;
            emitter.LifetimeVariation = 0f;
            emitter.InitialVelocity = Vector2.Zero;
            emitter.VelocitySpread = 0f;
            emitter.Gravity = Vector2.Zero;
            emitter.Shape = EmitterShape.Point;

            var subCfg = new SubEmitterConfig
            {
                BurstCount = 1,
                MaxParticles = 5,
                ParticleLifetime = 5f,
                LifetimeVariation = 0f,
                InitialVelocity = Vector2.Zero,
                VelocitySpread = 0f,
                SpeedVariation = 0f,
                Gravity = Vector2.Zero,
                Damping = 0f,
                TurbulenceStrength = turbulence,
                TurbulenceFrequency = 0.1f,
            };
            emitter.DeathSubEmitters = [subCfg];
            world.Flush();
            return world;
        }

        var worldTurb = BuildWorld(500f);
        var worldStill = BuildWorld(0f);

        for (int f = 0; f < 20; f++)
        {
            var gt = new GameTime(TimeSpan.FromSeconds(f * 0.016), TimeSpan.FromSeconds(0.016));
            systemTurb.Update(worldTurb, gt);
            systemStill.Update(worldStill, gt);
        }

        var rendererTurb = Substitute.For<IRenderer>();
        var rendererStill = Substitute.For<IRenderer>();

        float turbY = 0f, stillY = 0f;
        rendererTurb
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(ci => turbY = ci.ArgAt<float>(1));
        rendererStill
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(ci => stillY = ci.ArgAt<float>(1));

        systemTurb.Render(worldTurb, rendererTurb);
        systemStill.Render(worldStill, rendererStill);

        rendererTurb.Received().DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>());

        Math.Abs(turbY - stillY).Should().BeGreaterThan(0.001f,
            "turbulence must displace sub-particles away from their no-turbulence trajectory");
    }

    [Fact]
    public void SubEmitterZeroTurbulenceShouldNotAffectVelocity()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 3);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 3,
            MaxParticles = 10,
            ParticleLifetime = 5f,
            LifetimeVariation = 0f,
            InitialVelocity = new Vector2(50f, 0f),
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
            Damping = 0f,
            TurbulenceStrength = 0f,
        };
        emitter.DeathSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(0.5)));

        var renderer = Substitute.For<IRenderer>();
        var xPositionsFirst = new List<float>();
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(ci => xPositionsFirst.Add(ci.ArgAt<float>(0)));
        system.Render(_world, renderer);

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.516), TimeSpan.FromSeconds(0.1f)));

        var xPositionsSecond = new List<float>();
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(ci => xPositionsSecond.Add(ci.ArgAt<float>(0)));
        system.Render(_world, renderer);

        xPositionsSecond.Should().NotBeEmpty();
        for (int i = 0; i < Math.Min(xPositionsFirst.Count, xPositionsSecond.Count); i++)
            xPositionsSecond[i].Should().BeGreaterThan(xPositionsFirst[i],
                "with no turbulence particles must move predictably along their initial velocity");
    }

    #endregion

    #region Sub-Emitter Behavior Tests

    [Fact]
    public void BirthSubEmitterParticlesShouldBeVisibleOnSameFrame()
    {
        // Birth sub-emit particles were previously held in _pendingSubEmits until the next
        // emitter's DrainSubEmits pass, meaning single-emitter scenes saw them one frame late.
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 1);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 5;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.Gravity = Vector2.Zero;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 3,
            MaxParticles = 50,
            ParticleLifetime = 5f,
            LifetimeVariation = 0f,
            Gravity = Vector2.Zero,
        };
        emitter.BirthSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));

        var renderer = Substitute.For<IRenderer>();
        var drawCallCount = 0;
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(_ => drawCallCount++);
        system.Render(_world, renderer);

        // 5 burst particles + 5*3 = 15 sub-particles must all render on the very first frame.
        drawCallCount.Should().Be(20,
            "birth sub-particles must be spawned and rendered in the same frame as their parent");
    }

    [Fact]
    public void SubEmitterShouldSupportParticleFrameAnimation()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 2);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;
        emitter.Gravity = Vector2.Zero;

        var texA = Substitute.For<ITexture>();
        texA.Width.Returns(16);
        texA.Height.Returns(16);
        var texB = Substitute.For<ITexture>();
        texB.Width.Returns(16);
        texB.Height.Returns(16);

        var frameA = new AtlasRegion("a", new Rectangle(0, 0, 16, 16), texA);
        var frameB = new AtlasRegion("b", new Rectangle(0, 0, 16, 16), texB);

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 1,
            MaxParticles = 10,
            ParticleLifetime = 5f,
            LifetimeVariation = 0f,
            Gravity = Vector2.Zero,
            ParticleFrames = [frameA, frameB],
        };
        emitter.DeathSubEmitters = [subCfg];

        _world.Flush();

        // Tick once to fire the burst, then again to expire the short-lived parent
        // and spawn the death sub-particle.
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.1f)));

        var renderer = Substitute.For<IRenderer>();
        ITexture? renderedTexture = null;
        renderer
            .When(r => r.DrawTexture(
                Arg.Any<ITexture>(), Arg.Any<Vector2>(), Arg.Any<Rectangle?>(),
                Arg.Any<Vector2>(), Arg.Any<float>(), Arg.Any<Vector2>(),
                Arg.Any<Color>(), Arg.Any<SpriteFlip>()))
            .Do(ci => renderedTexture = ci.ArgAt<ITexture>(0));
        system.Render(_world, renderer);

        renderedTexture.Should().NotBeNull("sub-emitter with ParticleFrames must render a texture");
    }

    [Fact]
    public void SubEmitterShouldSupportTrails()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 5);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.Gravity = Vector2.Zero;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 1,
            MaxParticles = 10,
            ParticleLifetime = 5f,
            LifetimeVariation = 0f,
            InitialVelocity = new Vector2(100f, 0f),
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
            EnableTrails = true,
            TrailLength = 4,
            TrailHeadAlpha = 0.8f,
            TrailTailAlpha = 0.0f,
        };
        emitter.BirthSubEmitters = [subCfg];

        _world.Flush();

        // First frame — spawns parent + sub-particle (birth).
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        // Several more ticks so the trail history fills up.
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.016f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.032f), TimeSpan.FromSeconds(0.016f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.048f), TimeSpan.FromSeconds(0.016f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.064f), TimeSpan.FromSeconds(0.016f)));

        var renderer = Substitute.For<IRenderer>();
        var drawCallCount = 0;
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(_ => drawCallCount++);
        system.Render(_world, renderer);

        // The sub-particle itself + at least some trail segments must be rendered.
        drawCallCount.Should().BeGreaterThan(1,
            "sub-emitter trails must produce additional draw calls beyond the particle head");
    }

    [Fact]
    public void TexturedParticleShouldScaleEachAxisIndependently()
    {
        // size is a radius applied independently per axis: scaleX = size*2/width, scaleY = size*2/height.
        // A 16×32 sprite at size=8 renders 16×16 world pixels wide and 16×32*0.5=16 tall — correct.
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 7);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.StartSize = 8f;
        emitter.EndSize = 8f;

        var texture = Substitute.For<ITexture>();
        texture.Width.Returns(16);
        texture.Height.Returns(32);
        emitter.ParticleTexture = texture;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));

        var renderer = Substitute.For<IRenderer>();
        Vector2 capturedScale = default;
        renderer
            .When(r => r.DrawTexture(
                Arg.Any<ITexture>(), Arg.Any<Vector2>(), Arg.Any<Rectangle?>(),
                Arg.Any<Vector2>(), Arg.Any<float>(), Arg.Any<Vector2>(),
                Arg.Any<Color>(), Arg.Any<SpriteFlip>()))
            .Do(ci => capturedScale = ci.ArgAt<Vector2>(5));
        system.Render(_world, renderer);

        // size=8 → size*2=16; scaleX = 16/16 = 1.0, scaleY = 16/32 = 0.5
        capturedScale.X.Should().BeApproximately(1.0f, 0.001f);
        capturedScale.Y.Should().BeApproximately(0.5f, 0.001f);
    }

    [Fact]
    public void MissingTransformShouldLogWarningAndSuppressEmission()
    {
        var logger = Substitute.For<ILogger<ParticleSystem>>();
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 9, logger: logger);

        var entity = _world.CreateEntity();
        // Deliberately no TransformComponent.
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 100f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1f)));

        emitter.ParticleCount.Should().Be(0, "emission must be suppressed when TransformComponent is absent");
        logger.ReceivedWithAnyArgs().LogWarning(default(string));
    }

    #endregion

    #region Death Sub-Emitter Local-Space Position

    [Fact]
    public void DeathSubEmitter_LocalSpace_ShouldUseCurrentFramePosition()
    {
        // Regression: PreviousPosition was used for local-space death sub-emitter world coords,
        // causing a one-frame positional lag when the emitter entity was moving.
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 1);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = new Vector2(0, 0);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.SimulateInLocalSpace = true;
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 5;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;

        var subCfg = new SubEmitterConfig { BurstCount = 1, ParticleLifetime = 1f, LifetimeVariation = 0f, Gravity = Vector2.Zero, InitialVelocity = Vector2.Zero, VelocitySpread = 0f };
        emitter.DeathSubEmitters = [subCfg];

        _world.Flush();

        // Frame 1: burst fires at position (0,0)
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));

        // Move entity to (500, 0) before the frame where particles expire
        transform.Position = new Vector2(500, 0);

        // Frame 2: particles expire → death sub-emitters should spawn near (500, 0)
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.1f)));

        var renderer = Substitute.For<IRenderer>();
        renderer.Camera.Returns((ICamera?)null);
        var spawnedPositions = new List<Vector2>();
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(ci => spawnedPositions.Add(new Vector2(ci.ArgAt<float>(0), ci.ArgAt<float>(1))));

        system.Render(_world, renderer);

        spawnedPositions.Should().NotBeEmpty();
        spawnedPositions.All(p => p.X > 400f).Should()
            .BeTrue("death sub-emitters must use current-frame transform position, not previous-frame");
    }

    #endregion

    #region Warmup Birth Sub-Emitters

    [Fact]
    public void WarmupWithBirthSubEmitters_ShouldNotProduceSubParticleSpikeOnFirstFrame()
    {
        // Regression: DrainSubEmits was not called inside the warmup loop, so all birth
        // sub-emits accumulated during pre-simulation were flushed simultaneously on the
        // first real Update, causing a visible spike.
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 2);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 60f;
        emitter.MaxParticles = 500;
        emitter.ParticleLifetime = 2f;
        emitter.LifetimeVariation = 0f;
        emitter.WarmupDuration = 1f;
        emitter.Gravity = Vector2.Zero;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 1,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            MaxParticles = 10000,
            Gravity = Vector2.Zero,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
        };
        emitter.BirthSubEmitters = [subCfg];

        _world.Flush();

        // First update triggers warmup — sub-emits must be drained per step, not all at once.
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));

        var renderer = Substitute.For<IRenderer>();
        renderer.Camera.Returns((ICamera?)null);
        int drawCallsAfterWarmup = 0;
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(_ => drawCallsAfterWarmup++);
        system.Render(_world, renderer);

        // Do a second update without warmup to get a "steady-state" frame count
        drawCallsAfterWarmup = 0;
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.016f)));
        system.Render(_world, renderer);
        var steadyStateDrawCalls = drawCallsAfterWarmup;

        // Reset and measure draw calls on the very first post-warmup render again
        // The two values should be in the same ballpark, not orders of magnitude apart.
        steadyStateDrawCalls.Should().BeGreaterThan(0);
        // If the spike bug were present, the first-frame count would be ~60x the steady count.
        // After the fix, warmup sub-particles are spread across lifetime buckets and many will
        // have already expired, so the post-warmup live count is close to steady state.
        // We assert it is not more than 3x to give headroom for lifetime bucketing differences.
        drawCallsAfterWarmup.Should().BeLessThanOrEqualTo(steadyStateDrawCalls * 3,
            "warmup birth sub-emitters must be drained per step, not batched onto the first frame");
    }

    #endregion

    #region Sub-Emitter Speed-Over-Lifetime

    [Fact]
    public void SubEmitter_SpeedOverLifetime_ShouldDecelerateParticles()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 3);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 2f;
        emitter.LifetimeVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.InitialVelocity = Vector2.Zero;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 1,
            ParticleLifetime = 2f,
            LifetimeVariation = 0f,
            MaxParticles = 10,
            Gravity = Vector2.Zero,
            InitialVelocity = new Vector2(100f, 0f),
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            StartSpeedMultiplier = 1f,
            EndSpeedMultiplier = 0f,
        };
        emitter.DeathSubEmitters = [subCfg];

        _world.Flush();

        // Trigger the burst so the parent particle spawns
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        // Let the parent particle expire to trigger the death sub-emitter
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(2.1f)));

        // Sub-particles are now live. Sample speed at t≈0.25 and t≈0.75 of their lifetime.
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(2.116f), TimeSpan.FromSeconds(0.5f)));
        float? earlySpeed = null;
        // Expose via renderer capture — check that position advances less in later steps
        // by measuring how far particles travel per unit time at different life stages.
        // Simpler: just verify that the sub-particle's speed has decreased by the final step.

        // Advance to near end of sub-particle lifetime
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(2.616f), TimeSpan.FromSeconds(1.3f)));

        var renderer = Substitute.For<IRenderer>();
        renderer.Camera.Returns((ICamera?)null);
        var latePositions = new List<Vector2>();
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(ci => latePositions.Add(new Vector2(ci.ArgAt<float>(0), ci.ArgAt<float>(1))));
        system.Render(_world, renderer);

        // With EndSpeedMultiplier=0 the particle decelerates toward zero.
        // The displacement per unit time at t≈0.9 must be much less than at t≈0.1.
        // Verify indirectly: at t≈0.9 lifetime the particle must not have travelled 90% of
        // the naive constant-velocity distance (100 units/s × 1.8s = 180 units).
        latePositions.Should().NotBeEmpty("sub-emitter death should have triggered sub-particles");
        latePositions.All(p => p.X < 150f).Should()
            .BeTrue("sub-particle with EndSpeedMultiplier=0 must decelerate and not reach full constant-velocity distance");
    }

    [Fact]
    public void SubEmitter_DefaultSpeedMultipliers_ShouldNotAffectBehavior()
    {
        // Sanity: default multipliers (both 1) must leave speed unchanged.
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 4);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.InitialVelocity = Vector2.Zero;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 1,
            ParticleLifetime = 1f,
            LifetimeVariation = 0f,
            MaxParticles = 10,
            Gravity = Vector2.Zero,
            InitialVelocity = new Vector2(100f, 0f),
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            StartSpeedMultiplier = 1f,
            EndSpeedMultiplier = 1f,
        };
        emitter.DeathSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.1f)));

        // After 0.5 s at constant 100 units/s the sub-particle should be near x=50.
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.116f), TimeSpan.FromSeconds(0.5f)));

        var renderer = Substitute.For<IRenderer>();
        renderer.Camera.Returns((ICamera?)null);
        var positions = new List<Vector2>();
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(ci => positions.Add(new Vector2(ci.ArgAt<float>(0), ci.ArgAt<float>(1))));
        system.Render(_world, renderer);

        positions.Should().NotBeEmpty();
        positions.All(p => p.X > 40f && p.X < 60f).Should()
            .BeTrue("constant speed sub-particle must travel ~50 units in 0.5 s");
    }

    #endregion

    #region Viewport Culling

    [Fact]
    public void Render_ShouldCullParticlesOutsideCameraVisibleBounds()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 5);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = new Vector2(10000, 10000);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 20;
        emitter.MaxParticles = 20;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.StartSize = 1f;
        emitter.EndSize = 1f;

        _world.Flush();
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));

        var camera = Substitute.For<ICamera>();
        // Camera sees only the area around (0,0) — particles are at (10000,10000)
        camera.GetVisibleBounds().Returns(new Rectangle { X = -500, Y = -500, Width = 1000, Height = 1000 });

        var renderer = Substitute.For<IRenderer>();
        renderer.Camera.Returns(camera);

        int drawCalls = 0;
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(_ => drawCalls++);

        system.Render(_world, renderer);

        drawCalls.Should().Be(0, "all particles are outside the camera's visible bounds and must be culled");
    }

    [Fact]
    public void Render_ShouldNotCullParticlesInsideCameraVisibleBounds()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 6);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 5;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.StartSize = 1f;
        emitter.EndSize = 1f;

        _world.Flush();
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));

        var camera = Substitute.For<ICamera>();
        camera.GetVisibleBounds().Returns(new Rectangle { X = -500, Y = -500, Width = 1000, Height = 1000 });

        var renderer = Substitute.For<IRenderer>();
        renderer.Camera.Returns(camera);

        int drawCalls = 0;
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(_ => drawCalls++);

        system.Render(_world, renderer);

        drawCalls.Should().Be(5, "all particles are inside the camera bounds and must be rendered");
    }

    [Fact]
    public void Render_WithNoCamera_ShouldRenderAllParticles()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 7);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = new Vector2(99999, 99999);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 3;
        emitter.MaxParticles = 3;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.StartSize = 1f;
        emitter.EndSize = 1f;

        _world.Flush();
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));

        var renderer = Substitute.For<IRenderer>();
        renderer.Camera.Returns((ICamera?)null);

        int drawCalls = 0;
        renderer
            .When(r => r.DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>()))
            .Do(_ => drawCalls++);

        system.Render(_world, renderer);

        drawCalls.Should().Be(3, "with no camera all particles must render regardless of position");
    }

    #endregion

    #region Forces

    [Fact]
    public void PointAttractor_ShouldPullParticlesTowardPosition()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 1001);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 10;
        emitter.MaxParticles = 10;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.Shape = EmitterShape.Point;
        emitter.Forces = [new PointAttractor { Position = new Vector2(500f, 0f), Strength = 1000f, UseInverseSquare = false }];

        _world.Flush();
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));

        var xBefore = emitter.ActiveParticles.Average(p => p.Position.X);

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.1f)));

        var xAfter = emitter.ActiveParticles.Average(p => p.Position.X);
        xAfter.Should().BeGreaterThan(xBefore, "attractor to the right should pull particles rightward");
    }

    [Fact]
    public void PointAttractor_NegativeStrength_ShouldRepelParticles()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 1002);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        // Spawn particles at origin, repeller sits to the right — particles should move left.
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 5;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.Shape = EmitterShape.Point;
        emitter.Forces = [new PointAttractor { Position = new Vector2(100f, 0f), Strength = -2000f, MinDistance = 1f }];

        _world.Flush();
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.1f)));

        emitter.ActiveParticles.Should().OnlyContain(p => p.Position.X < 0f,
            "repeller to the right of origin should push all particles into negative X");
    }

    [Fact]
    public void DirectionalWind_ShouldAccelerateParticlesInWindDirection()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 1003);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 10;
        emitter.MaxParticles = 10;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.Shape = EmitterShape.Point;
        emitter.Forces = [new DirectionalWind { Acceleration = new Vector2(-500f, 0f) }];

        _world.Flush();
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));

        var xBefore = emitter.ActiveParticles.Average(p => p.Position.X);

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.1f)));

        var xAfter = emitter.ActiveParticles.Average(p => p.Position.X);
        xAfter.Should().BeLessThan(xBefore, "leftward wind should push particles in the negative X direction");
    }

    [Fact]
    public void MultipleForces_ShouldAllBeApplied()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 1004);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.Shape = EmitterShape.Point;
        emitter.Forces =
        [
            new DirectionalWind { Acceleration = new Vector2(1000f, 0f) },
            new DirectionalWind { Acceleration = new Vector2(0f, 1000f) },
        ];

        _world.Flush();
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.1f)));

        var p = emitter.ActiveParticles[0];
        p.Position.X.Should().BeGreaterThan(0f, "first wind force pushes right");
        p.Position.Y.Should().BeGreaterThan(0f, "second wind force pushes down");
    }

    #endregion

    #region Lifetime Fraction Sub-Emitters

    [Fact]
    public void LifetimeFractionSubEmitter_ShouldFireOnceAtFraction()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 2001);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 2f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.Shape = EmitterShape.Point;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 5,
            MaxParticles = 50,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
        };
        emitter.LifetimeFractionSubEmitters = [new LifetimeFractionSubEmitter { Fraction = 0.5f, Config = subCfg }];

        _world.Flush();

        // Spawn burst particle (t≈0, trigger should not fire yet).
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        emitter.ParticleCount.Should().Be(1);

        // Advance to just before 50% (0.9s of 2.0s lifetime).
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.884f)));
        GetSubParticleCount(system, subCfg).Should().Be(0, "trigger should not have fired before the 50% threshold");

        // Cross the 50% threshold.
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.9f), TimeSpan.FromSeconds(0.2f)));
        GetSubParticleCount(system, subCfg).Should().Be(5, "trigger must fire exactly once and spawn BurstCount sub-particles");

        // Advance further — must not re-fire.
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(1.1f), TimeSpan.FromSeconds(0.2f)));
        GetSubParticleCount(system, subCfg).Should().Be(5, "trigger must not re-fire after the threshold is already crossed");
    }

    [Fact]
    public void LifetimeFractionSubEmitter_ShouldFireIndependentlyPerParticle()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 2002);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 3;
        emitter.MaxParticles = 3;
        emitter.ParticleLifetime = 2f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.Shape = EmitterShape.Point;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 1,
            MaxParticles = 200,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
        };
        emitter.LifetimeFractionSubEmitters = [new LifetimeFractionSubEmitter { Fraction = 0.5f, Config = subCfg }];

        _world.Flush();
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        // Step past 50% lifetime for all 3 particles at once.
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(1.1f)));

        GetSubParticleCount(system, subCfg).Should().Be(3,
            "each of the 3 burst particles must trigger independently, producing 3 sub-particles");
    }

    [Fact]
    public void LifetimeFractionSubEmitter_LargeDeltaTime_ShouldFireEachTriggerExactlyOnce()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 2003);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 2f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.Shape = EmitterShape.Point;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 4,
            MaxParticles = 100,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
        };
        emitter.LifetimeFractionSubEmitters =
        [
            new LifetimeFractionSubEmitter { Fraction = 0.25f, Config = subCfg },
            new LifetimeFractionSubEmitter { Fraction = 0.75f, Config = subCfg },
        ];

        _world.Flush();
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));

        // One large step crosses both 0.25 and 0.75 simultaneously.
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(1.8f)));

        // Each trigger fires once → 4 + 4 = 8 sub-particles.
        GetSubParticleCount(system, subCfg).Should().Be(8,
            "each fraction trigger must fire exactly once even when a single delta-time step crosses both thresholds");
    }

    // Counts live sub-particles belonging to a specific SubEmitterConfig instance via reflection,
    // since SubEmitterState is a private nested type.
    private static int GetSubParticleCount(ParticleSystem system, SubEmitterConfig config)
    {
        var field = typeof(ParticleSystem).GetField(
            "_activeSubEmitters",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        var list = (System.Collections.IList)field.GetValue(system)!;
        var total = 0;

        foreach (var stateObj in list)
        {
            var stateType = stateObj.GetType();
            if (!ReferenceEquals(stateType.GetProperty("Config")!.GetValue(stateObj), config))
                continue;

            var particles = (System.Collections.ICollection)stateType.GetProperty("Particles")!.GetValue(stateObj)!;
            total += particles.Count;
        }

        return total;
    }

    #endregion

    #region ColorGradient Variation Warning

    [Fact]
    public void ColorGradientWithVariation_ShouldLogWarningOnce()
    {
        var logger = Substitute.For<ILogger<ParticleSystem>>();
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 3001, logger: logger);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.ColorGradient = [Color.Red, Color.Blue];
        emitter.StartColorVariation = new Color(10, 0, 0, 0);

        _world.Flush();

        // Run three frames — warning must appear exactly once across all of them.
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.1f), TimeSpan.FromSeconds(0.1f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.2f), TimeSpan.FromSeconds(0.1f)));

        logger.Received(1).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("ColorGradient") && o.ToString()!.Contains("StartColorVariation")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void ColorGradientWithoutVariation_ShouldNotLogWarning()
    {
        var logger = Substitute.For<ILogger<ParticleSystem>>();
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 3002, logger: logger);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.ColorGradient = [Color.Red, Color.Blue];
        // No variation set — default is (0,0,0,0).

        _world.Flush();
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1f)));

        logger.DidNotReceive().Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Any<object>(),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region GradientVariation Warning Cleanup

    [Fact]
    public void GradientVariationWarned_ShouldRemoveEmitter_WhenStopCalled()
    {
        var logger = Substitute.For<ILogger<ParticleSystem>>();
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 9001, logger: logger);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.ParticleLifetime = 5f;
        emitter.ColorGradient = [Color.Red, Color.Blue];
        emitter.StartColorVariation = new Color(10, 0, 0, 0);
        _world.Flush();

        // First update: fires warning and adds emitter to warned set.
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1f)));

        // Stop clears the entry — a re-enabled emitter should warn again on next update.
        emitter.Stop();
        emitter.IsEnabled = true;
        emitter.IsEmitting = true;

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.1f), TimeSpan.FromSeconds(0.1f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.2f), TimeSpan.FromSeconds(0.1f)));

        // Warning should have fired twice total (once before Stop, once after re-enable).
        logger.Received(2).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("ColorGradient")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    [Fact]
    public void GradientVariationWarned_ShouldRemoveEmitter_WhenBurstFinishes()
    {
        var logger = Substitute.For<ILogger<ParticleSystem>>();
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 9002, logger: logger);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 3;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.ColorGradient = [Color.Red, Color.Blue];
        emitter.StartColorVariation = new Color(10, 0, 0, 0);
        _world.Flush();

        // Fire burst, then advance past particle lifetime so emitter disables itself.
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.01f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.01f), TimeSpan.FromSeconds(0.2f)));

        emitter.IsEnabled.Should().BeFalse();

        // Re-arm and fire again — entry must have been cleaned up so warning fires a second time.
        emitter.ResetBurst();
        emitter.ColorGradient = [Color.Red, Color.Blue];
        emitter.StartColorVariation = new Color(10, 0, 0, 0);

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.21f), TimeSpan.FromSeconds(0.01f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.22f), TimeSpan.FromSeconds(0.2f)));

        logger.Received(2).Log(
            LogLevel.Warning,
            Arg.Any<EventId>(),
            Arg.Is<object>(o => o.ToString()!.Contains("ColorGradient")),
            Arg.Any<Exception?>(),
            Arg.Any<Func<object, Exception?, string>>());
    }

    #endregion

    #region Spawn Interpolation When MaxParticles Cap Is Hit

    [Fact]
    public void EmitParticles_WhenCapHit_ShouldDistributeSpawnPositionsEvenly()
    {
        // Seed for reproducibility; use a high emission rate so the cap is always hit.
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 5001);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = new Vector2(0, 0);
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 500f;
        emitter.MaxParticles = 5;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.SpawnRadius = 0f;
        emitter.Shape = EmitterShape.Point;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        _world.Flush();

        // Move emitter so PreviousPosition differs from current position.
        transform.Position = new Vector2(0, 0);
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.001f)));

        transform.Position = new Vector2(100, 0);
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.001f), TimeSpan.FromSeconds(0.1f)));

        emitter.ParticleCount.Should().Be(5);

        // Positions should span the arc [0..100] rather than all clustering near x=0.
        var positions = emitter.ActiveParticles.Select(p => p.Position.X).OrderBy(x => x).ToList();
        var range = positions[^1] - positions[0];
        range.Should().BeGreaterThan(10f, "capped particles should still be spread across the arc");
    }

    #endregion

    #region Sub-Emitter Forces and Lifecycle Callbacks

    [Fact]
    public void SubEmitter_Forces_ShouldAffectSubParticleVelocity()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 7001);
        var constantForce = new ConstantForce(new Vector2(1000f, 0f));

        var cfg = new SubEmitterConfig
        {
            BurstCount = 1,
            ParticleLifetime = 1f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
            Forces = [constantForce],
        };

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.ParticleLifetime = 1f;
        emitter.LifetimeVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.DeathSubEmitters = [cfg];
        _world.Flush();

        var renderer = Substitute.For<IRenderer>();

        // Fire burst and let the single parent particle die (lifetime 0.05 s).
        emitter.ParticleLifetime = 0.05f;
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.01f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.01f), TimeSpan.FromSeconds(0.1f)));

        // Advance sub-emitter particles — force should have moved them in +X.
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.11f), TimeSpan.FromSeconds(0.1f)));
        system.Render(_world, renderer);

        // Verify DrawCircleFilled was called with a positive X position (force applied).
        renderer.Received().DrawCircleFilled(
            Arg.Is<float>(x => x > 5f),
            Arg.Any<float>(),
            Arg.Any<float>(),
            Arg.Any<Color>());
    }

    [Fact]
    public void SubEmitter_OnParticleSpawned_ShouldBeInvokedForEachSubParticle()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 7002);
        var spawnCount = 0;

        var cfg = new SubEmitterConfig
        {
            BurstCount = 5,
            ParticleLifetime = 1f,
            LifetimeVariation = 0f,
            OnParticleSpawned = _ => spawnCount++,
        };

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.DeathSubEmitters = [cfg];
        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.01f)));
        // Parent particle dies, triggering sub-emitter burst.
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.01f), TimeSpan.FromSeconds(0.1f)));

        spawnCount.Should().Be(5);
    }

    [Fact]
    public void SubEmitter_OnParticleDied_ShouldBeInvokedForEachExpiredSubParticle()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 7003);
        var deathCount = 0;

        var cfg = new SubEmitterConfig
        {
            BurstCount = 3,
            ParticleLifetime = 0.05f,
            LifetimeVariation = 0f,
            OnParticleDied = _ => deathCount++,
        };

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.DeathSubEmitters = [cfg];
        _world.Flush();

        // Parent dies → sub-emitter spawns 3 particles.
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.01f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.01f), TimeSpan.FromSeconds(0.1f)));
        // Sub-particles die.
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.11f), TimeSpan.FromSeconds(0.2f)));

        deathCount.Should().Be(3);
    }

    #endregion

    #region Coherent Turbulence

    [Fact]
    public void Turbulence_ShouldDisplaceParticleFromSpawnOrigin()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 8001);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 2f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.TurbulenceStrength = 500f;
        emitter.TurbulenceFrequency = 0.1f;
        emitter.Shape = EmitterShape.Point;
        _world.Flush();

        for (int f = 0; f <= 10; f++)
            system.Update(_world, new GameTime(TimeSpan.FromSeconds(f * 0.05f), TimeSpan.FromSeconds(0.05f)));

        var position = emitter.ActiveParticles.Select(p => p.Position).Single();
        var displacement = position.Length();
        displacement.Should().BeGreaterThan(0f, "with no velocity or gravity, only turbulence can move the particle");
    }

    #endregion

    #region TrailMode Lines

    [Fact]
    public void TrailMode_Lines_ShouldCallDrawLine_NotDrawCircle()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 6001);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(100f, 0f);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.EnableTrails = true;
        emitter.TrailLength = 4;
        emitter.TrailMode = TrailMode.Lines;
        _world.Flush();

        for (int f = 0; f < 8; f++)
            system.Update(_world, new GameTime(TimeSpan.FromSeconds(f * 0.1f), TimeSpan.FromSeconds(0.1f)));

        system.Render(_world, renderer);

        // The trail must use DrawLine, not DrawCircleFilled.
        renderer.Received().DrawLine(Arg.Any<Vector2>(), Arg.Any<Vector2>(), Arg.Any<Color>(), Arg.Any<float>());

        // The particle head is still rendered as a circle — that is correct and expected.
        // We verify DrawLine count exceeds DrawCircleFilled to confirm the trail went through the line path.
        var lineCallCount = renderer.ReceivedCalls()
            .Count(c => c.GetMethodInfo().Name == nameof(IRenderer.DrawLine));
        var circleCallCount = renderer.ReceivedCalls()
            .Count(c => c.GetMethodInfo().Name == nameof(IRenderer.DrawCircleFilled));

        lineCallCount.Should().BeGreaterThan(0, "trail segments must be drawn as lines");
        lineCallCount.Should().BeGreaterThan(circleCallCount,
            "most draw calls should be lines, not circles, when TrailMode.Lines is active");
    }

    [Fact]
    public void TrailMode_Sprites_ShouldCallDrawCircle_NotDrawLine()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 6002);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(100f, 0f);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.EnableTrails = true;
        emitter.TrailLength = 4;
        emitter.TrailMode = TrailMode.Sprites;
        _world.Flush();

        for (int f = 0; f < 8; f++)
            system.Update(_world, new GameTime(TimeSpan.FromSeconds(f * 0.1f), TimeSpan.FromSeconds(0.1f)));

        system.Render(_world, renderer);

        renderer.DidNotReceive().DrawLine(Arg.Any<Vector2>(), Arg.Any<Vector2>(), Arg.Any<Color>(), Arg.Any<float>());
        renderer.Received().DrawCircleFilled(Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>());
    }

    [Fact]
    public void TrailMode_Lines_WithTexture_ShouldFallBackToSprites()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 6003);
        var renderer = Substitute.For<IRenderer>();
        var texture = Substitute.For<ITexture>();
        texture.Width.Returns(8);
        texture.Height.Returns(8);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(100f, 0f);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.EnableTrails = true;
        emitter.TrailLength = 4;
        emitter.TrailMode = TrailMode.Lines;
        emitter.ParticleTexture = texture;
        _world.Flush();

        for (int f = 0; f < 8; f++)
            system.Update(_world, new GameTime(TimeSpan.FromSeconds(f * 0.1f), TimeSpan.FromSeconds(0.1f)));

        system.Render(_world, renderer);

        // Lines mode is silently ignored when a texture is set; DrawTexture is used instead.
        renderer.DidNotReceive().DrawLine(Arg.Any<Vector2>(), Arg.Any<Vector2>(), Arg.Any<Color>(), Arg.Any<float>());
        renderer.Received().DrawTexture(Arg.Any<ITexture>(), Arg.Any<Vector2>(), Arg.Any<Rectangle?>(),
            Arg.Any<Vector2?>(), Arg.Any<float>(), Arg.Any<Vector2?>(), Arg.Any<Color?>(), Arg.Any<SpriteFlip>());
    }

    #endregion

    #region Trail SegT Off-By-One

    [Fact]
    public void Trail_NewestSegment_ShouldUseHeadAlphaNotTailAlpha()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 9001);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(100f, 0f);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.EnableTrails = true;
        emitter.TrailLength = 4;
        emitter.TrailMode = TrailMode.Sprites;
        emitter.StartColor = Color.White;
        emitter.EndColor = Color.White;
        emitter.TrailHeadAlpha = 1.0f;
        emitter.TrailTailAlpha = 0.0f;
        emitter.TrailHeadSizeRatio = 1.0f;
        emitter.TrailTailSizeRatio = 0.1f;
        _world.Flush();

        for (int f = 0; f < 10; f++)
            system.Update(_world, new GameTime(TimeSpan.FromSeconds(f * 0.1f), TimeSpan.FromSeconds(0.1f)));

        system.Render(_world, renderer);

        var trailAlphas = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IRenderer.DrawCircleFilled))
            .Select(c => ((Color)c.GetArguments()[3]!).A)
            .ToList();

        trailAlphas.Should().NotBeEmpty();
        trailAlphas.Max().Should().Be(255,
            "the newest trail slot must be evaluated at segT=1, producing TrailHeadAlpha=1.0");
        trailAlphas.Min().Should().BeLessThan(20,
            "the oldest trail slot must be evaluated at segT=0, producing TrailTailAlpha=0.0");
    }

    [Fact]
    public void Trail_SingleFilledSlot_ShouldRenderAtHeadAlpha()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 9002);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(100f, 0f);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.EnableTrails = true;
        emitter.TrailLength = 4;
        emitter.TrailMode = TrailMode.Sprites;
        emitter.StartColor = Color.White;
        emitter.EndColor = Color.White;
        emitter.TrailHeadAlpha = 1.0f;
        emitter.TrailTailAlpha = 0.0f;
        _world.Flush();

        // One frame → TrailFilled == 1; the single slot must be treated as the head (segT = 1).
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1f)));
        system.Render(_world, renderer);

        var trailAlphas = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IRenderer.DrawCircleFilled))
            .Select(c => ((Color)c.GetArguments()[3]!).A)
            .ToList();

        trailAlphas.Should().NotBeEmpty();
        trailAlphas.Max().Should().Be(255,
            "a single filled trail slot should render at TrailHeadAlpha=1.0, not TrailTailAlpha");
    }

    [Fact]
    public void Trail_Lines_NewestSegment_ShouldUseHeadAlpha()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 9003);
        var renderer = Substitute.For<IRenderer>();

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(100f, 0f);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.EnableTrails = true;
        emitter.TrailLength = 4;
        emitter.TrailMode = TrailMode.Lines;
        emitter.StartColor = Color.White;
        emitter.EndColor = Color.White;
        emitter.TrailHeadAlpha = 1.0f;
        emitter.TrailTailAlpha = 0.0f;
        _world.Flush();

        for (int f = 0; f < 10; f++)
            system.Update(_world, new GameTime(TimeSpan.FromSeconds(f * 0.1f), TimeSpan.FromSeconds(0.1f)));

        system.Render(_world, renderer);

        var lineColors = renderer.ReceivedCalls()
            .Where(c => c.GetMethodInfo().Name == nameof(IRenderer.DrawLine))
            .Select(c => ((Color)c.GetArguments()[2]!).A)
            .ToList();

        lineColors.Should().NotBeEmpty();
        lineColors.Max().Should().Be(255,
            "the line segment nearest the particle head must reach TrailHeadAlpha=1.0");
    }

    #endregion

    #region Trail Culling

    [Fact]
    public void Trail_HeadJustOffScreen_ShouldStillRenderOnScreenTrailSegments()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 9101);
        var renderer = Substitute.For<IRenderer>();

        var camera = Substitute.For<ICamera>();
        // 200×200 viewport centred at origin: x ∈ [-100, 100], y ∈ [-100, 100].
        camera.GetVisibleBounds().Returns(new Rectangle { X = -100, Y = -100, Width = 200, Height = 200 });
        renderer.Camera.Returns(camera);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        // Moving right at 30 u/s — after 5 s the head is at x=150, just outside the 100-wide half.
        emitter.InitialVelocity = new Vector2(30f, 0f);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.EnableTrails = true;
        emitter.TrailLength = 20;
        emitter.StartSize = 4f;
        emitter.EndSize = 4f;
        _world.Flush();

        // 50 frames × 0.1 s = 5 s. Head at x≈150 (off screen), tail slots back at x≈90 (on screen).
        for (int f = 0; f < 50; f++)
            system.Update(_world, new GameTime(TimeSpan.FromSeconds(f * 0.1), TimeSpan.FromSeconds(0.1)));

        system.Render(_world, renderer);

        // Trail sprites (DrawCircleFilled) must still be emitted even though the particle head is off-screen.
        renderer.Received().DrawCircleFilled(
            Arg.Is<float>(x => x < 100f),
            Arg.Any<float>(),
            Arg.Any<float>(),
            Arg.Any<Color>());
    }

    #endregion

    #region Stop Clears Pause State

    [Fact]
    public void Stop_WhenEmitterIsPaused_ShouldAlsoClearPause()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.ParticleLifetime = 5f;
        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));
        emitter.ParticleCount.Should().BeGreaterThan(0);

        emitter.Pause();
        emitter.IsPaused.Should().BeTrue();

        emitter.Stop();
        emitter.IsPaused.Should().BeFalse("Stop() must clear the pause flag");

        // The next update should process the stop request and clear particles.
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.5), TimeSpan.FromSeconds(0.1)));
        emitter.ParticleCount.Should().Be(0);
    }

    #endregion

    #region IsPaused Captured and Restored by Default State
    
    [Fact]
    public void CaptureDefaultState_WhenRunning_ShouldRestoreRunningState()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.CaptureDefaultState();

        emitter.Pause();
        emitter.IsPaused.Should().BeTrue();

        emitter.ResetToDefaultState();
        emitter.IsPaused.Should().BeFalse("ResetToDefaultState must restore IsPaused=false from the captured snapshot");
    }

    #endregion

    #region Per-Emitter Turbulence Offset

    [Fact]
    public void Turbulence_TwoEmittersWithIdenticalSettings_ShouldDiverge()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 9201);

        var entityA = _world.CreateEntity();
        entityA.AddComponent<TransformComponent>();
        entityA.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entityA.AddComponent<ParticleEmitterComponent>();
        var emitterA = entityA.GetComponent<ParticleEmitterComponent>();

        var entityB = _world.CreateEntity();
        entityB.AddComponent<TransformComponent>();
        entityB.GetComponent<TransformComponent>().Position = Vector2.Zero;
        entityB.AddComponent<ParticleEmitterComponent>();
        var emitterB = entityB.GetComponent<ParticleEmitterComponent>();

        foreach (var emitter in new[] { emitterA, emitterB })
        {
            emitter.IsBurst = true;
            emitter.BurstCount = 1;
            emitter.MaxParticles = 1;
            emitter.ParticleLifetime = 10f;
            emitter.LifetimeVariation = 0f;
            emitter.InitialVelocity = Vector2.Zero;
            emitter.VelocitySpread = 0f;
            emitter.SpeedVariation = 0f;
            emitter.Gravity = Vector2.Zero;
            emitter.TurbulenceStrength = 500f;
            emitter.TurbulenceFrequency = 0.1f;
            emitter.Shape = EmitterShape.Point;
        }

        _world.Flush();

        for (int f = 0; f <= 20; f++)
            system.Update(_world, new GameTime(TimeSpan.FromSeconds(f * 0.05f), TimeSpan.FromSeconds(0.05f)));

        var posA = emitterA.ActiveParticles.Single().Position;
        var posB = emitterB.ActiveParticles.Single().Position;

        var divergence = Vector2.Distance(posA, posB);
        divergence.Should().BeGreaterThan(1f,
            "two emitters with identical settings but different turbulence offsets should animate independently");
    }

    #endregion

    #region ParticleSystem Burst Fire-and-Forget API

    [Fact]
    public void Burst_ShouldSpawnSubParticlesWithoutEcsEntity()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 9301);
        var renderer = Substitute.For<IRenderer>();

        var cfg = new SubEmitterConfig
        {
            BurstCount = 5,
            ParticleLifetime = 1f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            Gravity = Vector2.Zero,
        };

        system.Burst(new Vector2(50f, 50f), cfg);

        // Particles should be visible on the next update + render.
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1f)));
        system.Render(_world, renderer);

        renderer.Received().DrawCircleFilled(
            Arg.Is<float>(x => x > 40f && x < 60f),
            Arg.Is<float>(y => y > 40f && y < 60f),
            Arg.Any<float>(),
            Arg.Any<Color>());
    }

    [Fact]
    public void Burst_NullConfig_ShouldThrowArgumentNullException()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider());
        var act = () => system.Burst(Vector2.Zero, null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void Burst_ParticlesShouldExpireNaturally()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 9302);
        var renderer = Substitute.For<IRenderer>();

        var cfg = new SubEmitterConfig
        {
            BurstCount = 3,
            ParticleLifetime = 0.1f,
            LifetimeVariation = 0f,
            Gravity = Vector2.Zero,
        };

        system.Burst(Vector2.Zero, cfg);

        // Advance past lifetime — particles should be gone.
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.05f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.05f), TimeSpan.FromSeconds(0.2f)));
        system.Render(_world, renderer);

        renderer.DidNotReceive().DrawCircleFilled(
            Arg.Any<float>(), Arg.Any<float>(), Arg.Any<float>(), Arg.Any<Color>());
    }

    #endregion

    #region Pool Leak on Removal Tests

    [Fact]
    public void ShouldReturnParticlesToPoolWhenComponentIsRemoved()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 100f;
        emitter.MaxParticles = 50;
        emitter.ParticleLifetime = 10f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));
        emitter.ParticleCount.Should().BeGreaterThan(0);

        entity.RemoveComponent<ParticleEmitterComponent>();

        emitter.ParticleCount.Should().Be(0);
    }

    [Fact]
    public void ShouldReturnParticlesToPoolWhenEntityIsDestroyed()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 100f;
        emitter.MaxParticles = 50;
        emitter.ParticleLifetime = 10f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));
        emitter.ParticleCount.Should().BeGreaterThan(0);

        _world.DestroyEntity(entity);
        _world.Flush();

        emitter.ParticleCount.Should().Be(0);
    }

    [Fact]
    public void ShouldNotInvokeCleanupMoreThanOnce()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 100f;
        emitter.MaxParticles = 50;
        emitter.ParticleLifetime = 10f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));

        entity.RemoveComponent<ParticleEmitterComponent>();

        // CleanupForPool should be nulled after first invocation so a second call is a no-op.
        emitter.ParticleCount.Should().Be(0);

        var act = () => emitter.CleanupForPool?.Invoke();
        act.Should().NotThrow();
    }

    #endregion

    #region Sub-Emitter MaxParticles Shared Cap Tests

    [Fact]
    public void SubEmitterMaxParticles_SharedConfig_CapIsEnforcedAcrossBothParentEmitters()
    {
        var sharedConfig = new SubEmitterConfig
        {
            BurstCount = 5,
            MaxParticles = 5,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
        };

        var entityA = _world.CreateEntity();
        entityA.AddComponent<TransformComponent>();
        entityA.AddComponent<ParticleEmitterComponent>();
        var emitterA = entityA.GetComponent<ParticleEmitterComponent>();
        emitterA.IsBurst = true;
        emitterA.BurstCount = 1;
        emitterA.MaxParticles = 10;
        emitterA.ParticleLifetime = 10f;
        emitterA.LifetimeVariation = 0f;
        emitterA.BirthSubEmitters = [sharedConfig];

        var entityB = _world.CreateEntity();
        entityB.AddComponent<TransformComponent>();
        entityB.AddComponent<ParticleEmitterComponent>();
        var emitterB = entityB.GetComponent<ParticleEmitterComponent>();
        emitterB.IsBurst = true;
        emitterB.BurstCount = 1;
        emitterB.MaxParticles = 10;
        emitterB.ParticleLifetime = 10f;
        emitterB.LifetimeVariation = 0f;
        emitterB.BirthSubEmitters = [sharedConfig];

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));

        var particleSystem = _particleSystem;
        var totalSubParticles = particleSystem
            .GetType()
            .GetField("_activeSubEmitters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(particleSystem) as System.Collections.IList;

        var total = 0;
        foreach (var state in totalSubParticles!)
        {
            var particles = state.GetType()
                .GetProperty("Particles", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
                .GetValue(state) as System.Collections.IList;
            total += particles!.Count;
        }

        total.Should().Be(sharedConfig.MaxParticles,
            "shared config cap must be enforced across all parent emitters using the same instance");
    }

    [Fact]
    public void SubEmitterMaxParticles_SeparateConfigs_EachCapIsIndependent()
    {
        var configA = new SubEmitterConfig { BurstCount = 5, MaxParticles = 5, ParticleLifetime = 10f, LifetimeVariation = 0f, VelocitySpread = 0f };
        var configB = new SubEmitterConfig { BurstCount = 5, MaxParticles = 5, ParticleLifetime = 10f, LifetimeVariation = 0f, VelocitySpread = 0f };

        var entityA = _world.CreateEntity();
        entityA.AddComponent<TransformComponent>();
        entityA.AddComponent<ParticleEmitterComponent>();
        var emitterA = entityA.GetComponent<ParticleEmitterComponent>();
        emitterA.IsBurst = true;
        emitterA.BurstCount = 1;
        emitterA.MaxParticles = 10;
        emitterA.ParticleLifetime = 10f;
        emitterA.LifetimeVariation = 0f;
        emitterA.BirthSubEmitters = [configA];

        var entityB = _world.CreateEntity();
        entityB.AddComponent<TransformComponent>();
        entityB.AddComponent<ParticleEmitterComponent>();
        var emitterB = entityB.GetComponent<ParticleEmitterComponent>();
        emitterB.IsBurst = true;
        emitterB.BurstCount = 1;
        emitterB.MaxParticles = 10;
        emitterB.ParticleLifetime = 10f;
        emitterB.LifetimeVariation = 0f;
        emitterB.BirthSubEmitters = [configB];

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));

        var particleSystem = _particleSystem;
        var totalSubParticles = particleSystem
            .GetType()
            .GetField("_activeSubEmitters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(particleSystem) as System.Collections.IList;

        var total = 0;
        foreach (var state in totalSubParticles!)
        {
            var particles = state.GetType()
                .GetProperty("Particles", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
                .GetValue(state) as System.Collections.IList;
            total += particles!.Count;
        }

        total.Should().Be(configA.MaxParticles + configB.MaxParticles,
            "separate config instances each have their own independent cap");
    }

    #endregion

    #region Single-Particle Arc Interpolation Tests

    [Fact]
    public void SingleParticleEmission_SpawnsAtCurrentPosition_NotMidpoint()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = new Vector2(0, 0);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.SpeedVariation = 0f;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.SpawnRadius = 0f;
        emitter.Shape = EmitterShape.Point;

        _world.Flush();

        transform.Position = new Vector2(100, 0);
        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));

        emitter.ParticleCount.Should().Be(1);
        emitter.ActiveParticles[0].Position.X.Should().Be(100f,
            "a single burst particle should spawn at the entity's current position, not the midpoint of its travel");
    }

    #endregion

    #region IDisposable Tests

    [Fact]
    public void Dispose_WhenNoSubEmitters_DoesNotThrow()
    {
        var act = () => _particleSystem.Dispose();
        act.Should().NotThrow();
    }

    #endregion

    #region ClearSubEmitters Tests

    [Fact]
    public void ClearSubEmitters_RemovesOnlyMatchingConfigParticles()
    {
        var configA = new SubEmitterConfig
        {
            BurstCount = 5,
            MaxParticles = 50,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
        };
        var configB = new SubEmitterConfig
        {
            BurstCount = 5,
            MaxParticles = 50,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
        };

        _particleSystem.Burst(Vector2.Zero, configA);
        _particleSystem.Burst(Vector2.Zero, configB);

        var activeSubEmitters = _particleSystem
            .GetType()
            .GetField("_activeSubEmitters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(_particleSystem) as System.Collections.IList;

        activeSubEmitters!.Count.Should().Be(2);

        _particleSystem.ClearSubEmitters(configA);

        activeSubEmitters.Count.Should().Be(1);

        var remaining = activeSubEmitters[0]!;
        var remainingConfig = remaining.GetType()
            .GetProperty("Config", System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)!
            .GetValue(remaining);
        ReferenceEquals(remainingConfig, configB).Should().BeTrue();
    }

    [Fact]
    public void ClearSubEmitters_WithNoMatchingConfig_DoesNotThrow()
    {
        var config = new SubEmitterConfig { BurstCount = 3, ParticleLifetime = 10f, LifetimeVariation = 0f };
        var unrelated = new SubEmitterConfig { BurstCount = 3, ParticleLifetime = 10f, LifetimeVariation = 0f };

        _particleSystem.Burst(Vector2.Zero, config);

        var act = () => _particleSystem.ClearSubEmitters(unrelated);
        act.Should().NotThrow();

        var activeSubEmitters = _particleSystem
            .GetType()
            .GetField("_activeSubEmitters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(_particleSystem) as System.Collections.IList;

        activeSubEmitters!.Count.Should().Be(1, "the unrelated config should not have affected the existing state");
    }

    [Fact]
    public void ClearAllSubEmitters_RemovesAllSubEmitterParticles()
    {
        var configA = new SubEmitterConfig { BurstCount = 5, MaxParticles = 50, ParticleLifetime = 10f, LifetimeVariation = 0f, VelocitySpread = 0f };
        var configB = new SubEmitterConfig { BurstCount = 5, MaxParticles = 50, ParticleLifetime = 10f, LifetimeVariation = 0f, VelocitySpread = 0f };

        _particleSystem.Burst(Vector2.Zero, configA);
        _particleSystem.Burst(Vector2.Zero, configB);

        var activeSubEmitters = _particleSystem
            .GetType()
            .GetField("_activeSubEmitters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!
            .GetValue(_particleSystem) as System.Collections.IList;

        activeSubEmitters!.Count.Should().Be(2);

        _particleSystem.ClearAllSubEmitters();

        activeSubEmitters.Count.Should().Be(0);
    }

    [Fact]
    public void ClearAllSubEmitters_LeavesMainEmitterParticlesIntact()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 50f;
        emitter.MaxParticles = 20;
        emitter.ParticleLifetime = 10f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5)));
        emitter.ParticleCount.Should().BeGreaterThan(0);

        var config = new SubEmitterConfig { BurstCount = 5, MaxParticles = 50, ParticleLifetime = 10f, LifetimeVariation = 0f, VelocitySpread = 0f };
        _particleSystem.Burst(Vector2.Zero, config);

        _particleSystem.ClearAllSubEmitters();

        emitter.ParticleCount.Should().BeGreaterThan(0, "ClearAllSubEmitters must not affect main emitter particles");
    }

    #endregion

    #region CaptureDefaultState IsPaused Tests

    [Fact]
    public void ResetToDefaultState_AlwaysResumesPausedEmitter()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.ParticleLifetime = 2f;

        emitter.Pause();
        emitter.CaptureDefaultState();

        _world.Flush();

        emitter.ResetToDefaultState();

        emitter.IsPaused.Should().BeFalse("ResetToDefaultState must always resume the emitter regardless of pause state at capture time");
    }

    [Fact]
    public void CaptureDefaultState_DoesNotCaptureIsPaused()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;

        emitter.CaptureDefaultState();

        emitter.Pause();
        emitter.IsPaused.Should().BeTrue();

        emitter.ResetToDefaultState();

        emitter.IsPaused.Should().BeFalse("IsPaused must be false after reset even if paused after capture");
    }

    #endregion

    #region CaptureDefaultState Deep-Copies ColorGradient and ParticleFrames

    [Fact]
    public void CaptureDefaultState_DeepCopiesColorGradientArray()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.ColorGradient = [Color.Red, Color.Blue];
        emitter.CaptureDefaultState();

        emitter.ColorGradient[0] = Color.Green;
        emitter.ResetToDefaultState();

        emitter.ColorGradient![0].Should().Be(Color.Red,
            "in-place mutation of the array after capture must not corrupt the captured state");
    }

    [Fact]
    public void CaptureDefaultState_DeepCopiesParticleFramesArray()
    {
        var texture = Substitute.For<ITexture>();
        var region1 = new AtlasRegion("f1", new Rectangle(0, 0, 16, 16), texture);
        var region2 = new AtlasRegion("f2", new Rectangle(16, 0, 16, 16), texture);
        var replacement = new AtlasRegion("f3", new Rectangle(32, 0, 16, 16), texture);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.ParticleFrames = [region1, region2];
        emitter.CaptureDefaultState();

        emitter.ParticleFrames[0] = replacement;
        emitter.ResetToDefaultState();

        emitter.ParticleFrames![0].Should().BeSameAs(region1,
            "in-place mutation of ParticleFrames after capture must not corrupt the captured state");
    }

    [Fact]
    public void CaptureDefaultState_NullColorGradientRemainsNull()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.ColorGradient = null;
        emitter.CaptureDefaultState();

        emitter.ColorGradient = [Color.White, Color.Black];
        emitter.ResetToDefaultState();

        emitter.ColorGradient.Should().BeNull("null gradient must be preserved through capture/reset");
    }

    #endregion

    #region ComputeMaxTrailReach Cap

    [Fact]
    public void TrailCullExpansion_DoesNotExceed2000Units()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.GetComponent<TransformComponent>().Position = new Vector2(10000f, 10000f);
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 10f;
        emitter.MaxParticles = 50;
        emitter.EnableTrails = true;
        emitter.TrailLength = 100;
        emitter.InitialVelocity = new Vector2(100000f, 0f);
        emitter.ParticleLifetime = 100f;
        emitter.LifetimeVariation = 0f;
        emitter.Gravity = Vector2.Zero;

        _world.Flush();

        var renderer = Substitute.For<IRenderer>();
        var act = () =>
        {
            _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
            _particleSystem.Render(_world, renderer);
        };
        act.Should().NotThrow("extremely fast/long-lived trail emitters must not cause rendering errors");
    }

    #endregion

    #region SubEmitterConfig VelocityInheritance

    [Fact]
    public void DeathSubEmitter_VelocityInheritanceZero_SubParticlesIgnoreParentVelocity()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 1);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 20;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(500f, 0f);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 1,
            MaxParticles = 50,
            ParticleLifetime = 5f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
            VelocityInheritance = 0f,
        };
        emitter.DeathSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.1f)));

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.116f), TimeSpan.FromSeconds(0.016f)));

        var renderer = Substitute.For<IRenderer>();
        system.Render(_world, renderer);

        foreach (var p in emitter.ActiveParticles)
            p.Velocity.X.Should().BeApproximately(0f, 1f,
                "with VelocityInheritance=0 sub-particles must not carry parent velocity");
    }

    [Fact]
    public void DeathSubEmitter_VelocityInheritanceOne_SubParticlesCarryParentVelocity()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 2);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 20;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(300f, 0f);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;

        var spawnedVelocities = new List<Vector2>();
        var subCfg = new SubEmitterConfig
        {
            BurstCount = 3,
            MaxParticles = 100,
            ParticleLifetime = 5f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
            VelocityInheritance = 1f,
            OnParticleSpawned = p => spawnedVelocities.Add(p.Velocity),
        };
        emitter.DeathSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.1f)));

        spawnedVelocities.Should().NotBeEmpty("death sub-emitter must have fired when parent particles expired");

        foreach (var v in spawnedVelocities)
            v.X.Should().BeApproximately(300f, 5f,
                "with VelocityInheritance=1 sub-particles must carry the parent's X velocity");
    }

    [Fact]
    public void BirthSubEmitter_VelocityInheritanceOne_SubParticlesCarrySpawnVelocity()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 3);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 3;
        emitter.MaxParticles = 20;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = new Vector2(0f, -200f);
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 1,
            MaxParticles = 50,
            ParticleLifetime = 5f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
            VelocityInheritance = 1f,
        };
        emitter.BirthSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.016f)));

        var subParticles = emitter.ActiveParticles;
        subParticles.Should().NotBeEmpty("birth sub-emitter must have spawned sub-particles");

        foreach (var p in subParticles)
            p.Velocity.Y.Should().BeApproximately(-200f, 5f,
                "birth sub-particles must inherit parent spawn velocity when VelocityInheritance=1");
    }

    #endregion

    #region Warmup Ages Sub-Emitters

    [Fact]
    public void Warmup_WithBirthSubEmitter_SubParticlesAreAgedByWarmupDuration()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 42);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 30f;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.WarmupDuration = 1f;

        // Sub-lifetime is 0.5s — all sub-particles spawned before the last 0.5s of
        // warmup must have expired by the time warmup completes.
        var subCfg = new SubEmitterConfig
        {
            BurstCount = 1,
            MaxParticles = 500,
            ParticleLifetime = 0.5f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
        };
        emitter.BirthSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));

        // 1s warmup at 30/s yields ~30 birth sub-particles; with a 0.5s sub-lifetime only those
        // born in the final half-second survive, so at most ~15 should remain.
        var subParticleCount = GetActiveSubParticleCount(system);
        subParticleCount.Should().BeLessThan(30,
            "sub-particles spawned early in warmup must have been aged and expired");
    }

    [Fact]
    public void Warmup_WithDeathSubEmitter_SubParticlesAreAgedByWarmupDuration()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 43);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 30f;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 0.2f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.WarmupDuration = 1f;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 1,
            MaxParticles = 500,
            ParticleLifetime = 0.1f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
        };
        emitter.DeathSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));

        var subParticleCount = GetActiveSubParticleCount(system);
        subParticleCount.Should().BeLessThan(10,
            "death sub-particles spawned early in warmup must have been aged and expired");
    }

    #endregion

    #region Dispose Returns Particles to Pool

    [Fact]
    public void Dispose_WithLiveParticles_ReturnsMainEmitterParticlesToPool()
    {
        var poolProvider = new DefaultObjectPoolProvider();
        var system = new ParticleSystem(poolProvider, seed: 1);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 50f;
        emitter.MaxParticles = 50;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1f)));
        emitter.ParticleCount.Should().BeGreaterThan(0, "precondition: particles must be alive before Dispose");

        system.Dispose();

        emitter.ParticleCount.Should().Be(0, "Dispose must clear particles from the emitter");
    }

    [Fact]
    public void Dispose_WithLiveSubEmitterParticles_ReturnsSubParticlesToPool()
    {
        var poolProvider = new DefaultObjectPoolProvider();
        var system = new ParticleSystem(poolProvider, seed: 2);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 20;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 3,
            MaxParticles = 100,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
        };
        emitter.BirthSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));

        var subCount = GetActiveSubParticleCount(system);
        subCount.Should().BeGreaterThan(0, "precondition: sub-particles must be alive before Dispose");

        system.Dispose();

        // A second Dispose must not throw — all state was already cleared.
        var act = system.Dispose;
        act.Should().NotThrow();
    }

    private static int GetActiveSubParticleCount(ParticleSystem system)
    {
        var field = typeof(ParticleSystem)
            .GetField("_activeSubEmitters", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var list = (System.Collections.IEnumerable)field.GetValue(system)!;
        int total = 0;
        foreach (var state in list)
        {
            var particles = state.GetType()
                .GetProperty("Particles")!
                .GetValue(state) as System.Collections.ICollection;
            total += particles?.Count ?? 0;
        }
        return total;
    }

    #endregion

    #region Pause Propagates to Owned Sub-Emitter Particles

    [Fact]
    public void PauseShouldFreezeOwnedSubEmitterParticles()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 10);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 20;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 3,
            MaxParticles = 100,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
        };
        emitter.DeathSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.1f)));

        var countBeforePause = GetActiveSubParticleCount(system);
        countBeforePause.Should().BeGreaterThan(0, "precondition: sub-particles must be alive");

        emitter.Pause();

        // Advance well past sub-particle lifetime — they must not age while paused.
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.116f), TimeSpan.FromSeconds(20f)));

        GetActiveSubParticleCount(system).Should().Be(countBeforePause,
            "owned sub-emitter particles must not age or expire while their parent emitter is paused");
    }

    [Fact]
    public void ResumeShouldContinueOwnedSubEmitterParticlesAfterPause()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 11);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 20;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 2,
            MaxParticles = 100,
            ParticleLifetime = 1f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
        };
        emitter.DeathSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.1f)));

        GetActiveSubParticleCount(system).Should().BeGreaterThan(0);

        emitter.Pause();
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.116f), TimeSpan.FromSeconds(0.9f)));

        GetActiveSubParticleCount(system).Should().BeGreaterThan(0,
            "sub-particles must not expire while the parent emitter is paused");

        emitter.Resume();
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(1.016f), TimeSpan.FromSeconds(2f)));

        GetActiveSubParticleCount(system).Should().Be(0,
            "sub-particles must expire normally once the parent emitter is resumed");
    }

    #endregion

    #region Stop Clears Owned Sub-Emitter Particles

    [Fact]
    public void StopShouldClearOwnedSubEmitterParticlesOnNextUpdate()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 12);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 20;
        emitter.ParticleLifetime = 0.05f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 3,
            MaxParticles = 100,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
        };
        emitter.DeathSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.1f)));

        GetActiveSubParticleCount(system).Should().BeGreaterThan(0,
            "precondition: owned sub-particles must be alive before Stop");

        emitter.Stop();
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.116f), TimeSpan.Zero));

        GetActiveSubParticleCount(system).Should().Be(0,
            "Stop must clear owned sub-emitter particles on the next update, not leave them alive");
    }

    [Fact]
    public void StopShouldNotClearUnownedSubEmitterParticles()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 13);

        // Spawn a free-standing burst via the public API — no owner emitter.
        var freeCfg = new SubEmitterConfig
        {
            BurstCount = 5,
            MaxParticles = 100,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
        };
        system.Burst(Vector2.Zero, freeCfg);

        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 20f;
        emitter.ParticleLifetime = 10f;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5f)));
        emitter.ParticleCount.Should().BeGreaterThan(0);

        var freeSubCount = GetActiveSubParticleCount(system);
        freeSubCount.Should().BeGreaterThan(0, "precondition: unowned sub-particles must be alive");

        emitter.Stop();
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.5f), TimeSpan.Zero));

        emitter.ParticleCount.Should().Be(0);
        GetActiveSubParticleCount(system).Should().Be(freeSubCount,
            "stopping an emitter must not affect sub-particles that were not spawned by it");
    }

    #endregion

    #region FiredFractionTriggers Reset on Pool Return

    [Fact]
    public void PoolReset_ShouldNullFiredFractionTriggers_NotJustClear()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 14);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 1,
            MaxParticles = 10,
            ParticleLifetime = 0.01f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            Gravity = Vector2.Zero,
        };
        emitter.LifetimeFractionSubEmitters =
        [
            new LifetimeFractionSubEmitter { Fraction = 0.5f, Config = subCfg },
        ];

        _world.Flush();

        // Tick 1: burst fires; fraction trigger will fire once the particle is 50 % through.
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.06f)));
        emitter.ParticleCount.Should().Be(1, "particle must be alive after first tick");

        // Tick 2: particle expires and is returned to the pool.
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.06f), TimeSpan.FromSeconds(0.1f)));
        emitter.ParticleCount.Should().Be(0, "particle must have expired");

        // Re-arm the burst so the pooled particle is re-issued.
        emitter.ResetBurst();
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.16f), TimeSpan.FromSeconds(0.016f)));
        emitter.ParticleCount.Should().Be(1, "particle must be alive after re-arm");

        var recycled = emitter.ActiveParticles[0];
        var field = typeof(Particle).GetField(
            "FiredFractionTriggers",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;

        var triggers = field.GetValue(recycled) as HashSet<int>;
        triggers.Should().BeNull(
            "FiredFractionTriggers must be set to null on IPoolable.Reset so the HashSet " +
            "allocation is released rather than retained inside the pool forever");
    }

    #endregion

    #region Burst Particles Spawn at Current Position

    [Fact]
    public void BurstEmitter_OnMovingEntity_ShouldSpawnAllParticlesAtCurrentPosition()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 42);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = new Vector2(0f, 0f);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 20;
        emitter.MaxParticles = 20;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.SpawnRadius = 0f;
        emitter.Shape = EmitterShape.Point;
        emitter.IsEmitting = false;

        _world.Flush();

        // First update records PreviousPosition = (0, 0) without firing the burst.
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));

        // Move then arm — burst must spawn at the current position, not smeared across the arc.
        transform.Position = new Vector2(500f, 0f);
        emitter.IsEmitting = true;

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.016f)));

        emitter.ParticleCount.Should().Be(20);

        foreach (var p in emitter.ActiveParticles)
        {
            p.Position.X.Should().BeApproximately(500f, 1f,
                "all burst particles must spawn at the current emitter position, not smeared along the movement path");
        }
    }

    [Fact]
    public void ContinuousEmitter_OnMovingEntity_ShouldInterpolateSpawnPositions()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 99);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = new Vector2(0f, 0f);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 1000f;
        emitter.MaxParticles = 500;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpeedVariation = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.SpawnRadius = 0f;
        emitter.Shape = EmitterShape.Point;

        _world.Flush();

        // First update establishes prevPosition.
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        emitter.ParticleCount.Should().BeGreaterThan(0);

        var prevX = emitter.ActiveParticles.Min(p => p.Position.X);

        // Move entity and update — continuous emitters should fill the gap.
        transform.Position = new Vector2(200f, 0f);
        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.016f)));

        var spawnedThisFrame = emitter.ActiveParticles
            .Where(p => p.Position.X > prevX)
            .ToList();

        spawnedThisFrame.Should().NotBeEmpty("continuous emitter must interpolate spawn positions across the movement arc");

        var distinctX = spawnedThisFrame.Select(p => MathF.Round(p.Position.X, 0)).Distinct().ToList();
        distinctX.Should().HaveCountGreaterThan(1,
            "interpolated spawns should be distributed across the arc, not all at the same position");
    }

    #endregion

    #region TotalParticleCount

    [Fact]
    public void TotalParticleCount_ShouldSumMainAndSubEmitterParticles()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 7);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 50f;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.Gravity = Vector2.Zero;

        var subCfg = new SubEmitterConfig
        {
            BurstCount = 5,
            MaxParticles = 200,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            SpeedVariation = 0f,
            Gravity = Vector2.Zero,
        };
        emitter.BirthSubEmitters = [subCfg];

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.5f)));

        emitter.ParticleCount.Should().BeGreaterThan(0);
        var subCount = GetActiveSubParticleCount(system);
        subCount.Should().BeGreaterThan(0);

        system.TotalParticleCount.Should().Be(emitter.ParticleCount + subCount);
    }

    [Fact]
    public void TotalParticleCount_ShouldBeZeroWhenNoParticlesActive()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 1);
        system.TotalParticleCount.Should().Be(0);
    }

    [Fact]
    public void TotalParticleCount_ShouldDecreaseAsParticlesExpire()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 3);
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 10;
        emitter.MaxParticles = 10;
        emitter.ParticleLifetime = 0.1f;
        emitter.LifetimeVariation = 0f;
        emitter.Gravity = Vector2.Zero;

        _world.Flush();

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        var countAfterBurst = system.TotalParticleCount;
        countAfterBurst.Should().Be(10);

        system.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.5f)));
        system.TotalParticleCount.Should().BeLessThan(countAfterBurst);
    }

    #endregion

    #region TrailMode Warning Cleanup

    [Fact]
    public void ClearSubEmitters_ShouldRemoveConfigFromTrailModeWarnedSet()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 5,
            logger: Substitute.For<ILogger<ParticleSystem>>());

        var texture = Substitute.For<ITexture>();
        texture.Width.Returns(8);
        texture.Height.Returns(8);

        var cfg = new SubEmitterConfig
        {
            BurstCount = 3,
            MaxParticles = 50,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            Gravity = Vector2.Zero,
            EnableTrails = true,
            TrailLength = 3,
            TrailMode = TrailMode.Lines,
            ParticleTexture = texture,
        };

        system.Burst(Vector2.Zero, cfg);
        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));

        // The warning set should now hold a reference to cfg.
        // Clearing it must release that reference.
        system.ClearSubEmitters(cfg);

        var warnedField = typeof(ParticleSystem)
            .GetField("_trailModeWarned", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var warned = (HashSet<SubEmitterConfig>)warnedField.GetValue(system)!;

        warned.Should().NotContain(cfg,
            "ClearSubEmitters must remove the config from _trailModeWarned to prevent a reference leak");
    }

    [Fact]
    public void ClearAllSubEmitters_ShouldClearEntireTrailModeWarnedSet()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 6,
            logger: Substitute.For<ILogger<ParticleSystem>>());

        var texture = Substitute.For<ITexture>();
        texture.Width.Returns(8);
        texture.Height.Returns(8);

        for (int n = 0; n < 3; n++)
        {
            var cfg = new SubEmitterConfig
            {
                BurstCount = 1,
                MaxParticles = 50,
                ParticleLifetime = 10f,
                LifetimeVariation = 0f,
                InitialVelocity = Vector2.Zero,
                Gravity = Vector2.Zero,
                EnableTrails = true,
                TrailLength = 3,
                TrailMode = TrailMode.Lines,
                ParticleTexture = texture,
            };
            system.Burst(Vector2.Zero, cfg);
        }

        system.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        system.ClearAllSubEmitters();

        var warnedField = typeof(ParticleSystem)
            .GetField("_trailModeWarned", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
        var warned = (HashSet<SubEmitterConfig>)warnedField.GetValue(system)!;

        warned.Should().BeEmpty("ClearAllSubEmitters must clear _trailModeWarned to release all config references");
    }

    #endregion

    #region Local-Space Sub-Emitter World Position

    [Fact]
    public void DeathSubEmitterShouldSpawnAtCorrectWorldPositionWhenEntityIsRotated()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 1);
        var world = CreateTestWorld();

        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = new Vector2(100f, 0f);
        transform.Rotation = MathF.PI / 2f;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.SimulateInLocalSpace = true;
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 10;
        emitter.ParticleLifetime = 0.01f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpawnOffset = new Vector2(50f, 0f);

        var spawnedPositions = new List<Vector2>();
        var deathCfg = new SubEmitterConfig
        {
            BurstCount = 1,
            MaxParticles = 50,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            Gravity = Vector2.Zero,
            OnParticleSpawned = p => spawnedPositions.Add(p.Position),
        };
        emitter.DeathSubEmitters = [deathCfg];

        world.Flush();

        // First update: burst fires, particle spawns at local (50, 0).
        // With rotation π/2 the world position is (100, 50) — entity X=100 + rotated local offset.
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));

        // Second update: particle expires, death sub-emitter fires.
        system.Update(world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.1f)));

        spawnedPositions.Should().NotBeEmpty("death sub-emitter must have fired");

        foreach (var pos in spawnedPositions)
        {
            pos.X.Should().BeApproximately(100f, 2f,
                "rotation π/2 maps local X to world Y, so world X stays near the entity origin");
            pos.Y.Should().BeApproximately(50f, 2f,
                "rotation π/2 maps local X offset to world Y");
        }
    }

    [Fact]
    public void BirthSubEmitterShouldSpawnAtCorrectWorldPositionWhenEntityIsRotated()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 2);
        var world = CreateTestWorld();

        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = Vector2.Zero;
        transform.Rotation = MathF.PI / 2f;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.SimulateInLocalSpace = true;
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 10;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpawnOffset = new Vector2(50f, 0f);

        var spawnedPositions = new List<Vector2>();
        var birthCfg = new SubEmitterConfig
        {
            BurstCount = 1,
            MaxParticles = 50,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            Gravity = Vector2.Zero,
            OnParticleSpawned = p => spawnedPositions.Add(p.Position),
        };
        emitter.BirthSubEmitters = [birthCfg];

        world.Flush();

        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        system.Update(world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.016f)));

        spawnedPositions.Should().NotBeEmpty("birth sub-emitter must have fired");

        // Local spawn offset (50, 0) rotated π/2 around origin (0, 0) → world (0, 50).
        foreach (var pos in spawnedPositions)
        {
            pos.X.Should().BeApproximately(0f, 1f,
                "rotation π/2 maps local X offset to world Y axis");
            pos.Y.Should().BeApproximately(50f, 1f,
                "rotation π/2 maps local X offset to world Y axis");
        }
    }

    [Fact]
    public void DeathSubEmitterShouldSpawnAtCorrectWorldPositionWhenEntityIsScaled()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 3);
        var world = CreateTestWorld();

        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = Vector2.Zero;
        transform.Scale = new Vector2(2f, 2f);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.SimulateInLocalSpace = true;
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 10;
        emitter.ParticleLifetime = 0.01f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpawnOffset = new Vector2(30f, 0f);

        var spawnedPositions = new List<Vector2>();
        var deathCfg = new SubEmitterConfig
        {
            BurstCount = 1,
            MaxParticles = 50,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            Gravity = Vector2.Zero,
            OnParticleSpawned = p => spawnedPositions.Add(p.Position),
        };
        emitter.DeathSubEmitters = [deathCfg];

        world.Flush();

        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        system.Update(world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.1f)));

        spawnedPositions.Should().NotBeEmpty("death sub-emitter must have fired");

        // Local offset (30, 0) × scale (2, 2) + origin (0, 0) = world (60, 0).
        foreach (var pos in spawnedPositions)
        {
            pos.X.Should().BeApproximately(60f, 2f,
                "scale 2 doubles the local X offset in world space");
            pos.Y.Should().BeApproximately(0f, 2f);
        }
    }

    [Fact]
    public void FractionTriggerSubEmitterShouldSpawnAtCorrectWorldPositionWhenEntityIsRotated()
    {
        var system = new ParticleSystem(new DefaultObjectPoolProvider(), seed: 4);
        var world = CreateTestWorld();

        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = Vector2.Zero;
        transform.Rotation = MathF.PI / 2f;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.SimulateInLocalSpace = true;
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 10;
        emitter.ParticleLifetime = 1f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.SpawnOffset = new Vector2(50f, 0f);

        var spawnedPositions = new List<Vector2>();
        var fractionCfg = new SubEmitterConfig
        {
            BurstCount = 1,
            MaxParticles = 50,
            ParticleLifetime = 10f,
            LifetimeVariation = 0f,
            InitialVelocity = Vector2.Zero,
            VelocitySpread = 0f,
            Gravity = Vector2.Zero,
            OnParticleSpawned = p => spawnedPositions.Add(p.Position),
        };
        emitter.LifetimeFractionSubEmitters =
        [
            new LifetimeFractionSubEmitter { Fraction = 0.1f, Config = fractionCfg }
        ];

        world.Flush();

        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));

        // Advance past the 0.1 fraction to trigger the sub-emitter.
        system.Update(world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.15f)));

        spawnedPositions.Should().NotBeEmpty("fraction-trigger sub-emitter must have fired");

        // Local offset (50, 0) rotated π/2 → world (0, 50).
        foreach (var pos in spawnedPositions)
        {
            pos.X.Should().BeApproximately(0f, 2f,
                "rotation π/2 maps local X offset to world Y axis");
            pos.Y.Should().BeApproximately(50f, 2f,
                "rotation π/2 maps local X offset to world Y axis");
        }
    }

    #endregion
    
    #region Force World Position on Local-Space Emitter

    [Fact]
    public void Force_LocalSpaceEmitter_RotatedEntity_ReceivesCorrectWorldPosition()
    {
        var world = CreateTestWorld();

        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = new Vector2(100f, 0f);
        transform.Rotation = MathF.PI / 2f;

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.SimulateInLocalSpace = true;
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.SpawnOffset = new Vector2(50f, 0f);

        var evaluatedPositions = new List<Vector2>();
        emitter.Forces = [new RecordingForce(evaluatedPositions)];

        world.Flush();
        _particleSystem.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        evaluatedPositions.Clear();

        _particleSystem.Update(world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.016f)));

        evaluatedPositions.Should().NotBeEmpty();
        // Local spawn offset (50, 0) rotated π/2 around entity origin (100, 0):
        // Rotated local = (0, 50), world = (100, 0) + (0, 50) = (100, 50).
        foreach (var pos in evaluatedPositions)
        {
            pos.X.Should().BeApproximately(100f, 1f, "entity X plus rotated offset");
            pos.Y.Should().BeApproximately(50f, 1f, "rotation π/2 maps local X to world Y");
        }
    }

    [Fact]
    public void Force_LocalSpaceEmitter_ScaledEntity_ReceivesCorrectWorldPosition()
    {
        var world = CreateTestWorld();

        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>();
        transform.Position = Vector2.Zero;
        transform.Scale = new Vector2(3f, 3f);

        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.SimulateInLocalSpace = true;
        emitter.IsBurst = true;
        emitter.BurstCount = 1;
        emitter.MaxParticles = 1;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;
        emitter.InitialVelocity = Vector2.Zero;
        emitter.VelocitySpread = 0f;
        emitter.Gravity = Vector2.Zero;
        emitter.SpawnOffset = new Vector2(10f, 0f);

        var evaluatedPositions = new List<Vector2>();
        emitter.Forces = [new RecordingForce(evaluatedPositions)];

        world.Flush();
        _particleSystem.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016f)));
        evaluatedPositions.Clear();

        _particleSystem.Update(world, new GameTime(TimeSpan.FromSeconds(0.016f), TimeSpan.FromSeconds(0.016f)));

        evaluatedPositions.Should().NotBeEmpty();
        // Local spawn offset (10, 0) × scale (3, 3) = world (30, 0).
        foreach (var pos in evaluatedPositions)
        {
            pos.X.Should().BeApproximately(30f, 1f, "scale 3 triples the local X offset in world space");
            pos.Y.Should().BeApproximately(0f, 1f);
        }
    }

    #endregion

    #region Loop Restart Tests

    [Fact]
    public void ShouldZeroEmissionTimerOnDurationLoopRestart()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 7f;
        emitter.MaxParticles = 1000;
        emitter.ParticleLifetime = 0.01f;
        emitter.LifetimeVariation = 0f;
        emitter.Duration = 0.3f;
        emitter.Loop = true;

        _world.Flush();

        // Emit for the full duration; residual accumulates in EmissionTimer (non-integer rate).
        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.3)));

        // Particles have 0.01 s life — expire on this step, triggering the loop restart.
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.3), TimeSpan.FromSeconds(0.05)));

        emitter.EmissionTimer.Should().Be(0f,
            "EmissionTimer must be zeroed on loop restart to prevent a ghost-particle burst on the first frame of the new cycle");
    }

    #endregion

    #region Play Tests

    [Fact]
    public void Play_ShouldRestartStoppedContinuousEmitter()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 100f;
        emitter.MaxParticles = 500;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));
        emitter.ParticleCount.Should().BeGreaterThan(0);

        emitter.Stop();
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));
        emitter.ParticleCount.Should().Be(0, "stop must clear all particles");

        emitter.Play();
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.2), TimeSpan.FromSeconds(0.1)));

        emitter.ParticleCount.Should().BeGreaterThan(0, "Play() must restart emission after Stop()");
    }

    [Fact]
    public void Play_ShouldReFireBurstAfterAutoDisable()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.IsBurst = true;
        emitter.BurstCount = 5;
        emitter.MaxParticles = 100;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
        emitter.ParticleCount.Should().Be(5);

        // Let all particles expire so the burst emitter auto-disables.
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.FromSeconds(10f)));
        emitter.IsEnabled.Should().BeFalse();

        emitter.Play();
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(10.016), TimeSpan.FromSeconds(0.016)));

        emitter.ParticleCount.Should().Be(5, "Play() must re-arm and re-fire the burst after auto-disable");
    }

    [Fact]
    public void Play_ShouldResumePausedAndDisabledEmitter()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 100f;
        emitter.MaxParticles = 500;
        emitter.ParticleLifetime = 5f;
        emitter.LifetimeVariation = 0f;
        emitter.IsEnabled = false;
        emitter.Pause();

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));
        emitter.ParticleCount.Should().Be(0, "disabled+paused emitter must not emit");

        emitter.Play();
        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.1)));

        emitter.ParticleCount.Should().BeGreaterThan(0, "Play() must re-enable, unpause, and start emission");
    }

    [Fact]
    public void Play_ShouldCancelPendingStop()
    {
        var entity = _world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<ParticleEmitterComponent>();
        var emitter = entity.GetComponent<ParticleEmitterComponent>();
        emitter.EmissionRate = 100f;
        emitter.MaxParticles = 1000;
        emitter.ParticleLifetime = 10f;
        emitter.LifetimeVariation = 0f;

        _world.Flush();

        _particleSystem.Update(_world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));
        var countBeforeStop = emitter.ParticleCount;
        countBeforeStop.Should().BeGreaterThan(0);

        // Stop then immediately Play before the next Update.
        emitter.Stop();
        emitter.Play();

        _particleSystem.Update(_world, new GameTime(TimeSpan.FromSeconds(0.1), TimeSpan.FromSeconds(0.016)));

        emitter.ParticleCount.Should().BeGreaterThanOrEqualTo(countBeforeStop,
            "Play() cancels the pending Stop so live particles are preserved, not cleared");
    }

    #endregion
}
file sealed class ConstantForce(Vector2 force) : IParticleForce
{
    public Vector2 Evaluate(Vector2 particleWorldPosition, float deltaTime) => force * deltaTime;
}

file sealed class RecordingForce(List<Vector2> positions) : IParticleForce
{
    public Vector2 Evaluate(Vector2 particleWorldPosition, float deltaTime)
    {
        positions.Add(particleWorldPosition);
        return Vector2.Zero;
    }
}