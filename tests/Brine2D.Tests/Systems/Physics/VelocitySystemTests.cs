using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Systems.Physics;

namespace Brine2D.Tests.Systems.Physics;

public class VelocitySystemTests : TestBase
{
    [Fact]
    public void Update_AppliesVelocityToTransform()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0, 0))
            .AddComponent<VelocityComponent>(v => 
            {
                v.Velocity = new Vector2(100, 50);
            });
        world.Flush();

        var system = new VelocitySystem();
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)); // 0.1 seconds

        // Act
        system.Update(gameTime, world);

        // Assert
        var transform = entity.GetComponent<TransformComponent>()!;
        Assert.Equal(new Vector2(10, 5), transform.LocalPosition); // 100*0.1, 50*0.1
    }

    [Fact]
    public void Update_WithDeltaTime_ScalesCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = Vector2.Zero)
            .AddComponent<VelocityComponent>(v => v.Velocity = new Vector2(60, 0)); // 60 units/sec
        world.Flush();

        var system = new VelocitySystem();
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0)); // ~16.67ms

        // Act
        system.Update(gameTime, world);

        // Assert
        var transform = entity.GetComponent<TransformComponent>()!;
        Assert.Equal(1f, transform.LocalPosition.X, precision: 2); // 60 * (1/60) = 1
    }

    [Fact]
    public void Update_MultipleEntities_UpdatesAll()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0, 0))
            .AddComponent<VelocityComponent>(v => v.Velocity = new Vector2(100, 0));
        
        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0, 0))
            .AddComponent<VelocityComponent>(v => v.Velocity = new Vector2(0, 200));
        
        world.Flush();

        var system = new VelocitySystem();
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1));

        // Act
        system.Update(gameTime, world);

        // Assert
        var transform1 = entity1.GetComponent<TransformComponent>()!;
        var transform2 = entity2.GetComponent<TransformComponent>()!;
        
        Assert.Equal(new Vector2(10, 0), transform1.LocalPosition);
        Assert.Equal(new Vector2(0, 20), transform2.LocalPosition);
    }

    [Fact]
    public void Update_EntityWithoutVelocity_Ignored()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(5, 5));
        // No VelocityComponent
        world.Flush();

        var system = new VelocitySystem();
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1));

        // Act
        system.Update(gameTime, world);

        // Assert - Position unchanged
        var transform = entity.GetComponent<TransformComponent>()!;
        Assert.Equal(new Vector2(5, 5), transform.LocalPosition);
    }

    [Fact]
    public void Update_EntityWithoutTransform_Ignored()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.Velocity = new Vector2(100, 100));
        // No TransformComponent
        world.Flush();

        var system = new VelocitySystem();
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1));

        // Act - Should not throw
        system.Update(gameTime, world);

        // Assert - No exception thrown
        Assert.True(true);
    }

    [Fact]
    public void Update_DisabledVelocityComponent_NotApplied()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = Vector2.Zero)
            .AddComponent<VelocityComponent>(v => v.Velocity = new Vector2(100, 100));
        world.Flush();

        var velocity = entity.GetComponent<VelocityComponent>()!;
        velocity.IsEnabled = false;

        var system = new VelocitySystem();
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1));

        // Act
        system.Update(gameTime, world);

        // Assert - Position should not change
        var transform = entity.GetComponent<TransformComponent>()!;
        Assert.Equal(Vector2.Zero, transform.LocalPosition);
    }

    [Fact]
    public void Update_ZeroVelocity_NoMovement()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(10, 10))
            .AddComponent<VelocityComponent>(v => v.Velocity = Vector2.Zero);
        world.Flush();

        var system = new VelocitySystem();
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1));

        // Act
        system.Update(gameTime, world);

        // Assert
        var transform = entity.GetComponent<TransformComponent>()!;
        Assert.Equal(new Vector2(10, 10), transform.LocalPosition);
    }

    [Fact]
    public void UpdateOrder_IsCorrect()
    {
        // Arrange
        var system = new VelocitySystem();

        // Act & Assert
        Assert.Equal(100, system.UpdateOrder);
    }

    [Fact]
    public void Name_IsCorrect()
    {
        // Arrange
        var system = new VelocitySystem();

        // Act & Assert
        Assert.Equal("VelocitySystem", system.Name);
    }
}