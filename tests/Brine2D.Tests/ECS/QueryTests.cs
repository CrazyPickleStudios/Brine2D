using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Systems.Rendering;
using FluentAssertions;
using System.Numerics;
using Xunit;

namespace Brine2D.Tests.ECS;

public class QueryTests
{
    [Fact]
    public void ShouldFindEntitiesWithSpecificComponent()
    {
        // Arrange
        var world = new EntityWorld();
        var entity1 = world.CreateEntity();
        entity1.AddComponent(new TransformComponent());

        var entity2 = world.CreateEntity();
        entity2.AddComponent(new SpriteComponent());

        // Act
        var results = world.Query().With<TransformComponent>().Execute();

        // Assert
        results.Should().ContainSingle();
        results.First().Should().Be(entity1);
    }

    [Fact]
    public void ShouldFilterEntitiesBySpatialRadius()
    {
        // Arrange
        var world = new EntityWorld();
        var center = world.CreateEntity();
        center.AddComponent(new TransformComponent { Position = new Vector2(0, 0) });

        var nearby = world.CreateEntity();
        nearby.AddComponent(new TransformComponent { Position = new Vector2(5, 0) });

        var farAway = world.CreateEntity();
        farAway.AddComponent(new TransformComponent { Position = new Vector2(100, 0) });

        // Act
        var results = world.Query().With<TransformComponent>()
            .WithinRadius(new Vector2(0, 0), 10f)
            .Execute();

        // Assert
        results.Should().HaveCount(2); // center + nearby
        results.Should().Contain(center);
        results.Should().Contain(nearby);
        results.Should().NotContain(farAway);
    }

    [Fact]
    public void ShouldFilterEntitiesByMultipleComponents()
    {
        // Arrange
        var world = new EntityWorld();
        var entity1 = world.CreateEntity();
        entity1.AddComponent(new TransformComponent());
        entity1.AddComponent(new SpriteComponent());

        var entity2 = world.CreateEntity();
        entity2.AddComponent(new TransformComponent());

        var entity3 = world.CreateEntity();
        entity3.AddComponent(new SpriteComponent());

        // Act
        var results = world.Query().With<TransformComponent>().With<SpriteComponent>().Execute();

        // Assert
        results.Should().ContainSingle();
        results.First().Should().Be(entity1);
    }

    [Fact]
    public void ShouldReturnCorrectResultsForCachedQueries()
    {
        // Arrange
        var world = new EntityWorld();
        var entity1 = world.CreateEntity();
        entity1.AddComponent(new TransformComponent());

        var entity2 = world.CreateEntity();
        entity2.AddComponent(new TransformComponent());

        var cachedQuery = world.CreateCachedQuery<TransformComponent>();
        var results = cachedQuery.Execute();

        // Assert
        results.Should().HaveCount(2);
        results.Should().Contain(entity1);
        results.Should().Contain(entity2);

        // Remove one entity and check cache updates
        world.DestroyEntity(entity1);
        var updatedResults = cachedQuery.Execute();

        // Assert
        updatedResults.Should().HaveCount(1);
        updatedResults.Should().Contain(entity2);
        updatedResults.Should().NotContain(entity1);
    }
}