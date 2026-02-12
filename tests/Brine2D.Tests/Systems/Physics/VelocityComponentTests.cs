using System.Numerics;
using Brine2D.ECS;
using Brine2D.Systems.Physics;

namespace Brine2D.Tests.Systems.Physics;

public class VelocityComponentTests : TestBase
{
    #region Constructor and Default Values

    [Fact]
    public void Constructor_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<VelocityComponent>();
        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Assert
        Assert.Equal(Vector2.Zero, velocity.Velocity);
        Assert.Equal(0f, velocity.MaxSpeed);
        Assert.Equal(0f, velocity.Friction);
        Assert.True(velocity.ApplyVelocity);
    }

    #endregion

    #region Basic Properties

    [Fact]
    public void Velocity_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<VelocityComponent>();
        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        velocity.Velocity = new Vector2(100, 50);

        // Assert
        Assert.Equal(new Vector2(100, 50), velocity.Velocity);
    }

    [Fact]
    public void MaxSpeed_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<VelocityComponent>();
        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        velocity.MaxSpeed = 200f;

        // Assert
        Assert.Equal(200f, velocity.MaxSpeed);
    }

    [Fact]
    public void Friction_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<VelocityComponent>();
        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        velocity.Friction = 0.5f;

        // Assert
        Assert.Equal(0.5f, velocity.Friction);
    }

    [Fact]
    public void ApplyVelocity_CanBeDisabled()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<VelocityComponent>();
        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        velocity.ApplyVelocity = false;

        // Assert
        Assert.False(velocity.ApplyVelocity);
    }

    #endregion

    #region CurrentSpeed Property

    [Fact]
    public void CurrentSpeed_WithZeroVelocity_ReturnsZero()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.Velocity = Vector2.Zero);

        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act & Assert
        Assert.Equal(0f, velocity.CurrentSpeed);
    }

    [Fact]
    public void CurrentSpeed_ReturnsVelocityMagnitude()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.Velocity = new Vector2(3, 4)); // 3-4-5 triangle

        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        var speed = velocity.CurrentSpeed;

        // Assert
        Assert.Equal(5f, speed, precision: 5);
    }

    [Fact]
    public void CurrentSpeed_WithDiagonalVelocity_CalculatesCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.Velocity = new Vector2(100, 100));

        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        var speed = velocity.CurrentSpeed;

        // Assert
        var expected = MathF.Sqrt(100 * 100 + 100 * 100);
        Assert.Equal(expected, speed, precision: 3);
    }

    #endregion

    #region Accelerate Method

    [Fact]
    public void Accelerate_AddsToVelocity()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.Velocity = new Vector2(50, 0));

        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        velocity.Accelerate(new Vector2(100, 0), 0.5f); // 100 px/s² for 0.5s = 50 px/s

        // Assert
        Assert.Equal(new Vector2(100, 0), velocity.Velocity);
    }

    [Fact]
    public void Accelerate_WithZeroTime_DoesNothing()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.Velocity = new Vector2(50, 50));

        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        velocity.Accelerate(new Vector2(100, 100), 0f);

        // Assert
        Assert.Equal(new Vector2(50, 50), velocity.Velocity);
    }

    [Fact]
    public void Accelerate_Multiple_Accumulates()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<VelocityComponent>();
        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        velocity.Accelerate(new Vector2(100, 0), 0.1f);
        velocity.Accelerate(new Vector2(100, 0), 0.1f);
        velocity.Accelerate(new Vector2(100, 0), 0.1f);

        // Assert
        Assert.Equal(new Vector2(30, 0), velocity.Velocity);
    }

    [Fact]
    public void Accelerate_WithMaxSpeed_ClampsVelocity()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 100f);

        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        velocity.Accelerate(new Vector2(500, 0), 1f); // Would go to 500, but clamped to 100

        // Assert
        Assert.Equal(100f, velocity.Velocity.Length(), precision: 3);
    }

    #endregion

    #region SetDirection Method

    [Fact]
    public void SetDirection_SetsVelocityInDirection()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<VelocityComponent>();
        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        velocity.SetDirection(new Vector2(1, 0), 100f);

        // Assert
        Assert.Equal(new Vector2(100, 0), velocity.Velocity);
    }

    [Fact]
    public void SetDirection_NormalizesDirection()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<VelocityComponent>();
        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act - Direction vector has length 2, but should be normalized
        velocity.SetDirection(new Vector2(2, 0), 100f);

        // Assert
        Assert.Equal(100f, velocity.CurrentSpeed, precision: 3);
        Assert.Equal(new Vector2(100, 0), velocity.Velocity);
    }

    [Fact]
    public void SetDirection_WithZeroVector_DoesNotChangeVelocity()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.Velocity = new Vector2(50, 50));

        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        velocity.SetDirection(Vector2.Zero, 100f);

        // Assert - Velocity should remain unchanged
        Assert.Equal(new Vector2(50, 50), velocity.Velocity);
    }

    [Fact]
    public void SetDirection_DiagonalDirection_CalculatesCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<VelocityComponent>();
        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        velocity.SetDirection(new Vector2(1, 1), 100f);

        // Assert
        Assert.Equal(100f, velocity.CurrentSpeed, precision: 3);
        var normalized = Vector2.Normalize(new Vector2(1, 1)) * 100f;
        Assert.Equal(normalized.X, velocity.Velocity.X, precision: 3);
        Assert.Equal(normalized.Y, velocity.Velocity.Y, precision: 3);
    }

    #endregion

    #region ClampToMaxSpeed Method

    [Fact]
    public void ClampToMaxSpeed_WithZeroMaxSpeed_DoesNotClamp()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<VelocityComponent>(v =>
            {
                v.Velocity = new Vector2(1000, 1000);
                v.MaxSpeed = 0f; // 0 means unlimited
            });

        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        velocity.ClampToMaxSpeed();

        // Assert
        Assert.Equal(new Vector2(1000, 1000), velocity.Velocity);
    }

    [Fact]
    public void ClampToMaxSpeed_BelowMax_DoesNotChange()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<VelocityComponent>(v =>
            {
                v.Velocity = new Vector2(50, 0);
                v.MaxSpeed = 100f;
            });

        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        velocity.ClampToMaxSpeed();

        // Assert
        Assert.Equal(new Vector2(50, 0), velocity.Velocity);
    }

    [Fact]
    public void ClampToMaxSpeed_AboveMax_ClampsToMaxSpeed()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<VelocityComponent>(v =>
            {
                v.Velocity = new Vector2(150, 0);
                v.MaxSpeed = 100f;
            });

        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        velocity.ClampToMaxSpeed();

        // Assert
        Assert.Equal(100f, velocity.CurrentSpeed, precision: 3);
        Assert.Equal(new Vector2(100, 0), velocity.Velocity);
    }

    [Fact]
    public void ClampToMaxSpeed_PreservesDirection()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<VelocityComponent>(v =>
            {
                v.Velocity = new Vector2(300, 400); // Length = 500
                v.MaxSpeed = 100f;
            });

        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        velocity.ClampToMaxSpeed();

        // Assert
        Assert.Equal(100f, velocity.CurrentSpeed, precision: 3);
        // Direction should be preserved (3-4-5 ratio)
        Assert.Equal(60f, velocity.Velocity.X, precision: 3);
        Assert.Equal(80f, velocity.Velocity.Y, precision: 3);
    }

    [Fact]
    public void ClampToMaxSpeed_ExactlyAtMax_DoesNotChange()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<VelocityComponent>(v =>
            {
                v.Velocity = new Vector2(100, 0);
                v.MaxSpeed = 100f;
            });

        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        velocity.ClampToMaxSpeed();

        // Assert
        Assert.Equal(new Vector2(100, 0), velocity.Velocity);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void AccelerateAndClamp_WorksTogether()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 150f);

        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act - Accelerate several times, should get clamped
        velocity.Accelerate(new Vector2(500, 0), 0.1f); // +50
        velocity.Accelerate(new Vector2(500, 0), 0.1f); // +50
        velocity.Accelerate(new Vector2(500, 0), 0.1f); // +50
        velocity.Accelerate(new Vector2(500, 0), 0.1f); // +50 (would be 200, but clamped)

        // Assert
        Assert.Equal(150f, velocity.CurrentSpeed, precision: 3);
    }

    [Fact]
    public void SetDirection_ThenAccelerate_ModifiesCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<VelocityComponent>();
        var velocity = entity.GetComponent<VelocityComponent>()!;

        // Act
        velocity.SetDirection(new Vector2(1, 0), 100f);
        velocity.Accelerate(new Vector2(0, 200), 0.5f); // Add vertical acceleration

        // Assert
        Assert.Equal(new Vector2(100, 100), velocity.Velocity);
    }

    #endregion
}