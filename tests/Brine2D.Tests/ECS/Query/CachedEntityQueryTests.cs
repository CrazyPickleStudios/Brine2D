using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Query;
using Brine2D.Systems.Physics;

namespace Brine2D.Tests.ECS.Query;

public class CachedEntityQueryTests : TestBase
{
    #region Single Component Query

    [Fact]
    public void CachedQuery_SingleComponent_ReturnsMatchingEntities()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity().AddComponent<TransformComponent>();
        var entity2 = world.CreateEntity().AddComponent<VelocityComponent>();
        var entity3 = world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        // Act
        var query = world.CreateCachedQuery<TransformComponent>().Build();
        var results = query.Execute().ToList();

        // Assert
        Assert.Equal(2, results.Count);
        Assert.Contains(entity1, results);
        Assert.Contains(entity3, results);
    }

    [Fact]
    public void CachedQuery_SingleComponent_CachesResults()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        var query = world.CreateCachedQuery<TransformComponent>().Build();

        // Act - Execute twice
        var results1 = query.Execute().ToList();
        var results2 = query.Execute().ToList();

        // Assert - Should return same cached results
        Assert.Equal(results1.Count, results2.Count);
        Assert.Equal(results1[0], results2[0]);
    }

    [Fact]
    public void CachedQuery_SingleComponent_InvalidatesOnEntityAdd()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        var query = world.CreateCachedQuery<TransformComponent>().Build();
        var initialResults = query.Execute().ToList();
        Assert.Single(initialResults);

        // Act - Add new entity with matching component
        var entity2 = world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        var newResults = query.Execute().ToList();

        // Assert - Cache should have invalidated and updated
        Assert.Equal(2, newResults.Count);
        Assert.Contains(entity1, newResults);
        Assert.Contains(entity2, newResults);
    }

    [Fact]
    public void CachedQuery_SingleComponent_InvalidatesOnEntityRemove()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity().AddComponent<TransformComponent>();
        var entity2 = world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        var query = world.CreateCachedQuery<TransformComponent>().Build();
        var initialResults = query.Execute().ToList();
        Assert.Equal(2, initialResults.Count);

        // Act - Remove entity
        world.DestroyEntity(entity1);
        world.Flush();

        var newResults = query.Execute().ToList();

        // Assert - Cache should have invalidated and updated
        Assert.Single(newResults);
        Assert.Contains(entity2, newResults);
    }

    [Fact]
    public void CachedQuery_SingleComponent_InvalidatesOnComponentAdd()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity().AddComponent<TransformComponent>();
        var entity2 = world.CreateEntity(); // No component yet
        world.Flush();

        var query = world.CreateCachedQuery<TransformComponent>().Build();
        var initialResults = query.Execute().ToList();
        Assert.Single(initialResults);

        // Act - Add matching component to entity2
        entity2.AddComponent<TransformComponent>();
        world.Flush();

        var newResults = query.Execute().ToList();

        // Assert - Cache should include new entity
        Assert.Equal(2, newResults.Count);
        Assert.Contains(entity1, newResults);
        Assert.Contains(entity2, newResults);
    }

    [Fact]
    public void CachedQuery_SingleComponent_InvalidatesOnComponentRemove()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity().AddComponent<TransformComponent>();
        var entity2 = world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        var query = world.CreateCachedQuery<TransformComponent>().Build();
        var initialResults = query.Execute().ToList();
        Assert.Equal(2, initialResults.Count);

        // Act - Remove component from entity1
        entity1.RemoveComponent<TransformComponent>();
        world.Flush();

        var newResults = query.Execute().ToList();

        // Assert - Cache should have updated
        Assert.Single(newResults);
        Assert.Contains(entity2, newResults);
    }

    #endregion

    #region Two Component Query

    [Fact]
    public void CachedQuery_TwoComponents_ReturnsOnlyEntitiesWithBoth()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<VelocityComponent>();
        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>();
        var entity3 = world.CreateEntity()
            .AddComponent<VelocityComponent>();
        world.Flush();

        // Act
        var query = world.CreateCachedQuery<TransformComponent, VelocityComponent>().Build();
        var results = query.Execute().ToList();

        // Assert
        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void CachedQuery_TwoComponents_CachesResults()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<VelocityComponent>();
        world.Flush();

        var query = world.CreateCachedQuery<TransformComponent, VelocityComponent>().Build();

        // Act - Execute twice
        var results1 = query.Execute().ToList();
        var results2 = query.Execute().ToList();

        // Assert - Should use cached results
        Assert.Single(results1);
        Assert.Single(results2);
        Assert.Equal(results1[0], results2[0]);
    }

    [Fact]
    public void CachedQuery_TwoComponents_InvalidatesCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<VelocityComponent>();
        world.Flush();

        var query = world.CreateCachedQuery<TransformComponent, VelocityComponent>().Build();
        var initialResults = query.Execute().ToList();
        Assert.Single(initialResults);

        // Act - Add new matching entity
        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<VelocityComponent>();
        world.Flush();

        var newResults = query.Execute().ToList();

        // Assert
        Assert.Equal(2, newResults.Count);
    }

    #endregion

    #region Three Component Query

    [Fact]
    public void CachedQuery_ThreeComponents_ReturnsOnlyEntitiesWithAll()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<VelocityComponent>()
            .AddComponent<TestComponent>();
        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<VelocityComponent>();
        world.Flush();

        // Act
        var query = world.CreateCachedQuery<TransformComponent, VelocityComponent, TestComponent>().Build();
        var results = query.Execute().ToList();

        // Assert
        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void CachedQuery_ThreeComponents_CachesResults()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<VelocityComponent>()
            .AddComponent<TestComponent>();
        world.Flush();

        var query = world.CreateCachedQuery<TransformComponent, VelocityComponent, TestComponent>().Build();

        // Act - Execute twice
        var results1 = query.Execute().ToList();
        var results2 = query.Execute().ToList();

        // Assert
        Assert.Single(results1);
        Assert.Single(results2);
        Assert.Equal(results1[0], results2[0]);
    }

    #endregion

    #region Builder Pattern with Filters

    [Fact]
    public void CachedQuery_WithTag_FiltersCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddTag("Player");
        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddTag("Enemy");
        world.Flush();

        // Act
        var query = world.CreateCachedQuery<TransformComponent>()
            .WithTag("Player")
            .Build();
        var results = query.Execute().ToList();

        // Assert
        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void CachedQuery_WithMultipleTags_FiltersCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddTags("Enemy", "Flying");
        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddTag("Enemy");
        world.Flush();

        // Act
        var query = world.CreateCachedQuery<TransformComponent>()
            .WithTag("Enemy")
            .WithTag("Flying")
            .Build();
        var results = query.Execute().ToList();

        // Assert
        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void CachedQuery_WithPredicate_FiltersCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity("Player1").AddComponent<TransformComponent>();
        var entity2 = world.CreateEntity("Enemy1").AddComponent<TransformComponent>();
        world.Flush();

        // Act
        var query = world.CreateCachedQuery<TransformComponent>()
            .Where(e => e.Name.StartsWith("Player"))
            .Build();
        var results = query.Execute().ToList();

        // Assert
        Assert.Single(results);
        Assert.Contains(entity1, results);
    }

    [Fact]
    public void CachedQuery_IncludeInactive_IncludesDisabledEntities()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity().AddComponent<TransformComponent>();
        var entity2 = world.CreateEntity().AddComponent<TransformComponent>();
        entity2.IsActive = false;
        world.Flush();

        // Act
        var queryActive = world.CreateCachedQuery<TransformComponent>().Build();
        var queryAll = world.CreateCachedQuery<TransformComponent>()
            .IncludeInactive()
            .Build();

        var activeResults = queryActive.Execute().ToList();
        var allResults = queryAll.Execute().ToList();

        // Assert
        Assert.Single(activeResults);
        Assert.Equal(2, allResults.Count);
    }

    #endregion

    #region Manual Invalidation

    [Fact]
    public void Invalidate_ManualCall_ForcesRefresh()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        var query = world.CreateCachedQuery<TransformComponent>().Build();
        var initialResults = query.Execute().ToList();
        Assert.Single(initialResults);

        // Act - Manually invalidate and check
        query.Invalidate();
        
        // Note: Just invalidating doesn't change results, but next Execute will refresh
        var results = query.Execute().ToList();

        // Assert
        Assert.Single(results);
    }

    #endregion

    #region Count Method

    [Fact]
    public void Count_WithCachedResults_ReturnsCount()
    {
        // Arrange
        var world = CreateTestWorld();
        world.CreateEntity().AddComponent<TransformComponent>();
        world.CreateEntity().AddComponent<TransformComponent>();
        world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        var query = world.CreateCachedQuery<TransformComponent>().Build();

        // Act
        query.Execute(); // Cache results
        var count = query.Count();

        // Assert
        Assert.Equal(3, count);
    }

    [Fact]
    public void Count_BeforeExecution_ReturnsZero()
    {
        // Arrange
        var world = CreateTestWorld();
        world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        var query = world.CreateCachedQuery<TransformComponent>().Build();

        // Act - Count without executing
        var count = query.Count();

        // Assert - Returns 0 because cache is dirty
        Assert.Equal(0, count);
    }

    [Fact]
    public void Count_AfterInvalidation_ReturnsZero()
    {
        // Arrange
        var world = CreateTestWorld();
        world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        var query = world.CreateCachedQuery<TransformComponent>().Build();
        query.Execute(); // Cache results
        
        // Act
        query.Invalidate();
        var count = query.Count();

        // Assert - Returns 0 because cache is dirty
        Assert.Equal(0, count);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CachedQuery_MultipleQueries_EachMaintainsOwnCache()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity().AddComponent<TransformComponent>();
        var entity2 = world.CreateEntity().AddComponent<VelocityComponent>();
        world.Flush();

        // Act
        var query1 = world.CreateCachedQuery<TransformComponent>().Build();
        var query2 = world.CreateCachedQuery<VelocityComponent>().Build();

        var results1 = query1.Execute().ToList();
        var results2 = query2.Execute().ToList();

        // Assert - Each query has independent cache
        Assert.Single(results1);
        Assert.Single(results2);
        Assert.Contains(entity1, results1);
        Assert.Contains(entity2, results2);
    }

    [Fact]
    public void CachedQuery_ComplexScenario_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var player = world.CreateEntity("Player")
            .AddComponent<TransformComponent>()
            .AddComponent<VelocityComponent>()
            .AddTag("Player");
        
        var enemy = world.CreateEntity("Enemy")
            .AddComponent<TransformComponent>()
            .AddComponent<VelocityComponent>()
            .AddTag("Enemy");
        
        world.Flush();

        // Act
        var playerQuery = world.CreateCachedQuery<TransformComponent, VelocityComponent>()
            .WithTag("Player")
            .Build();
        
        var enemyQuery = world.CreateCachedQuery<TransformComponent, VelocityComponent>()
            .WithTag("Enemy")
            .Build();

        var playerResults = playerQuery.Execute().ToList();
        var enemyResults = enemyQuery.Execute().ToList();

        // Assert
        Assert.Single(playerResults);
        Assert.Single(enemyResults);
        Assert.Contains(player, playerResults);
        Assert.Contains(enemy, enemyResults);

        // Add new enemy
        var enemy2 = world.CreateEntity("Enemy2")
            .AddComponent<TransformComponent>()
            .AddComponent<VelocityComponent>()
            .AddTag("Enemy");
        world.Flush();

        // Player query should still have 1, enemy query should have 2
        playerResults = playerQuery.Execute().ToList();
        enemyResults = enemyQuery.Execute().ToList();

        Assert.Single(playerResults);
        Assert.Equal(2, enemyResults.Count);
    }

    #endregion

    #region Test Helper Component

    private class TestComponent : Component
    {
    }

    #endregion
}