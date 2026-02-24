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
            .AddComponent<TransformComponent>(t => t.LocalPosition = Vector2.Zero)
            .AddComponent<VelocityComponent>(v => v.Velocity = new Vector2(100, 50));
        world.Flush();

        var system = new VelocitySystem();

        // Act
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));

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
            .AddComponent<VelocityComponent>(v => v.Velocity = new Vector2(60, 0));
        world.Flush();

        var system = new VelocitySystem();

        // Act
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(1.0 / 60.0)));

        // Assert - 60 * (1/60) = 1
        var transform = entity.GetComponent<TransformComponent>()!;
        Assert.Equal(1f, transform.LocalPosition.X, precision: 2);
    }

    [Fact]
    public void Update_MultipleEntities_UpdatesAll()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = Vector2.Zero)
            .AddComponent<VelocityComponent>(v => v.Velocity = new Vector2(100, 0));

        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = Vector2.Zero)
            .AddComponent<VelocityComponent>(v => v.Velocity = new Vector2(0, 200));

        world.Flush();

        var system = new VelocitySystem();

        // Act
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));

        // Assert
        Assert.Equal(new Vector2(10, 0), entity1.GetComponent<TransformComponent>()!.LocalPosition);
        Assert.Equal(new Vector2(0, 20), entity2.GetComponent<TransformComponent>()!.LocalPosition);
    }

    [Fact]
    public void Update_EntityWithoutVelocity_Ignored()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(5, 5));
        world.Flush();

        var system = new VelocitySystem();

        // Act
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));

        // Assert
        Assert.Equal(new Vector2(5, 5), entity.GetComponent<TransformComponent>()!.LocalPosition);
    }

    [Fact]
    public void Update_EntityWithoutTransform_DoesNotThrow()
    {
        // Arrange
        var world = CreateTestWorld();
        world.CreateEntity()
            .AddComponent<VelocityComponent>(v => v.Velocity = new Vector2(100, 100));
        world.Flush();

        var system = new VelocitySystem();

        // Act & Assert
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));
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

        entity.GetComponent<VelocityComponent>()!.IsEnabled = false;

        var system = new VelocitySystem();

        // Act
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));

        // Assert
        Assert.Equal(Vector2.Zero, entity.GetComponent<TransformComponent>()!.LocalPosition);
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

        // Act
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.1)));

        // Assert
        Assert.Equal(new Vector2(10, 10), entity.GetComponent<TransformComponent>()!.LocalPosition);
    }

    [Fact]
    public void UpdateOrder_IsCorrect()
    {
        Assert.Equal(100, new VelocitySystem().UpdateOrder);
    }
}