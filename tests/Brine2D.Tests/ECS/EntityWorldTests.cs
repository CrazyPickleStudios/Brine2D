using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Rendering;
using Brine2D.Systems.Physics;
using Xunit;

namespace Brine2D.Tests.ECS;

public class EntityWorldTests : TestBase
{
    #region Entity Creation

    [Fact]
    public void CreateEntity_CreatesEntityWithUniqueId()
    {
        // Arrange
        var world = CreateTestWorld();

        // Act
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();

        // Assert
        Assert.NotEqual(0, entity1.Id); // 0 is the invalid/null sentinel
        Assert.NotEqual(0, entity2.Id);
        Assert.NotEqual(entity1.Id, entity2.Id);
    }

    [Fact]
    public void CreateEntity_WithName_SetsNameCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();

        // Act
        var entity = world.CreateEntity("Player");

        // Assert
        Assert.Equal("Player", entity.Name);
    }

    [Fact]
    public void CreateEntity_SetsWorldReference()
    {
        // Arrange
        var world = CreateTestWorld();

        // Act
        var entity = world.CreateEntity();

        // Assert
        Assert.Equal(world, entity.World);
    }

    [Fact]
    public void CreateEntity_AddsToEntitiesList()
    {
        // Arrange
        var world = CreateTestWorld();

        // Act
        var entity = world.CreateEntity("TestEntity");
        world.Flush();

        // Assert
        Assert.Contains(entity, world.Entities);
    }

    [Fact]
    public void CreateEntity_Generic_CreatesTypedEntity()
    {
        // Arrange
        var world = CreateTestWorld();

        // Act
        var entity = world.CreateEntity<TestEntity>("CustomEntity");
        world.Flush();

        // Assert
        Assert.IsType<TestEntity>(entity);
        Assert.Equal("CustomEntity", entity.Name);
        Assert.Contains(entity, world.Entities);
    }

    #endregion

    #region Entity Destruction

    [Fact]
    public void DestroyEntity_RemovesEntityFromWorld()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity("ToDestroy");
        world.Flush();

        // Act
        world.DestroyEntity(entity);
        world.Flush();

        // Assert
        Assert.DoesNotContain(entity, world.Entities);
    }

    [Fact]
    public void DestroyEntity_DeactivatesEntityImmediately()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        world.Flush();

        // Act
        world.DestroyEntity(entity);

        // Assert - inactive before Flush
        Assert.False(entity.IsActive);
    }

    [Fact]
    public void DestroyEntity_PreventsDoubleQueuing()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        world.Flush();

        // Act
        world.DestroyEntity(entity);
        world.DestroyEntity(entity);
        world.Flush();

        // Assert
        Assert.DoesNotContain(entity, world.Entities);
    }

    [Fact]
    public void DestroyEntity_CallsOnDestroy()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity<TestEntity>();
        world.Flush();

        // Act
        world.DestroyEntity(entity);
        world.Flush();

        // Assert
        Assert.True(((TestEntity)entity).OnDestroyCalled);
    }

    #endregion

    #region Entity Queries

    [Fact]
    public void GetEntityById_ExistingEntity_ReturnsEntity()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity("FindMe");
        world.Flush();

        // Act
        var found = world.GetEntityById(entity.Id);

        // Assert
        Assert.NotNull(found);
        Assert.Equal(entity.Id, found.Id);
    }

    [Fact]
    public void GetEntityById_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var world = CreateTestWorld();

        // Act
        var found = world.GetEntityById(-1);

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public void GetEntityByName_ExistingEntity_ReturnsEntity()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Player");
        world.Flush();

        // Act
        var found = world.GetEntityByName("Player");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Player", found.Name);
    }

    [Fact]
    public void GetEntityByName_NonExistingEntity_ReturnsNull()
    {
        // Arrange
        var world = CreateTestWorld();

        // Act
        var found = world.GetEntityByName("NonExistent");

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public void GetEntityByName_MultipleWithSameName_ReturnsFirst()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity("Duplicate");
        var entity2 = world.CreateEntity("Duplicate");
        world.Flush();

        // Act
        var found = world.GetEntityByName("Duplicate");

        // Assert
        Assert.NotNull(found);
        Assert.Equal(entity1.Id, found.Id);
    }

    [Fact]
    public void GetEntitiesByTag_ReturnsOnlyTaggedEntities()
    {
        // Arrange
        var world = CreateTestWorld();
        var enemy1 = world.CreateEntity("Enemy1").AddTag("Enemy");
        var enemy2 = world.CreateEntity("Enemy2").AddTag("Enemy");
        var player = world.CreateEntity("Player").AddTag("Player");
        world.Flush();

        // Act
        var enemies = world.GetEntitiesByTag("Enemy").ToList();

        // Assert
        Assert.Equal(2, enemies.Count);
        Assert.Contains(enemy1, enemies);
        Assert.Contains(enemy2, enemies);
        Assert.DoesNotContain(player, enemies);
    }

    [Fact]
    public void GetEntitiesByTag_OnlyReturnsActiveEntities()
    {
        // Arrange
        var world = CreateTestWorld();
        var activeEnemy = world.CreateEntity("Active").AddTag("Enemy");
        var inactiveEnemy = world.CreateEntity("Inactive").AddTag("Enemy");
        inactiveEnemy.IsActive = false;
        world.Flush();

        // Act
        var enemies = world.GetEntitiesByTag("Enemy").ToList();

        // Assert
        Assert.Single(enemies);
        Assert.Contains(activeEnemy, enemies);
    }

    [Fact]
    public void GetEntitiesWithComponent_ReturnsOnlyEntitiesWithComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity().AddComponent<TransformComponent>();
        var entity2 = world.CreateEntity().AddComponent<TransformComponent>();
        var entity3 = world.CreateEntity().AddComponent<VelocityComponent>();
        world.Flush();

        // Act
        var withTransform = world.GetEntitiesWithComponent<TransformComponent>().ToList();

        // Assert
        Assert.Equal(2, withTransform.Count);
        Assert.Contains(entity1, withTransform);
        Assert.Contains(entity2, withTransform);
        Assert.DoesNotContain(entity3, withTransform);
    }

    [Fact]
    public void GetEntitiesWithComponents_ReturnsOnlyEntitiesWithBothComponents()
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
        var withBoth = world.GetEntitiesWithComponents<TransformComponent, VelocityComponent>().ToList();

        // Assert
        Assert.Single(withBoth);
        Assert.Contains(entity1, withBoth);
    }

    [Fact]
    public void FindEntity_WithPredicate_ReturnsFirstMatch()
    {
        // Arrange
        var world = CreateTestWorld();
        world.CreateEntity("Entity1");
        var entity2 = world.CreateEntity("Entity2");
        world.Flush();

        // Act
        var found = world.FindEntity(e => e.Name == "Entity2");

        // Assert
        Assert.NotNull(found);
        Assert.Equal("Entity2", found.Name);
    }

    [Fact]
    public void FindEntity_NoMatch_ReturnsNull()
    {
        // Arrange
        var world = CreateTestWorld();
        world.CreateEntity("Entity1");
        world.Flush();

        // Act
        var found = world.FindEntity(e => e.Name == "NonExistent");

        // Assert
        Assert.Null(found);
    }

    #endregion

    #region Update

    [Fact]
    public void Update_ProcessesDeferredOperations()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        Assert.Empty(world.Entities);

        // Act
        world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016)));

        // Assert
        Assert.Contains(entity, world.Entities);
    }

    #endregion

    #region Clear and Flush

    [Fact]
    public void Clear_RemovesAllEntities()
    {
        // Arrange
        var world = CreateTestWorld();
        world.CreateEntity();
        world.CreateEntity();
        world.Flush();

        // Act
        world.Clear();

        // Assert
        Assert.Empty(world.Entities);
    }
    
    [Fact]
    public void Flush_ProcessesPendingOperations()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        Assert.Empty(world.Entities);

        // Act
        world.Flush();

        // Assert
        Assert.Contains(entity, world.Entities);
    }

    [Fact]
    public void Flush_CallsOnInitializeForNewEntities()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity<TestEntity>();

        // Act
        world.Flush();

        // Assert
        Assert.True(((TestEntity)entity).OnInitializeCalled);
    }

    [Fact]
    public void Flush_HandlesMultipleCreationsAndDeletions()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity("Temp1");
        var entity2 = world.CreateEntity("Temp2");
        world.Flush();
        Assert.Equal(2, world.Entities.Count);

        // Act
        world.DestroyEntity(entity1);
        var entity3 = world.CreateEntity("Temp3");
        world.Flush();

        // Assert
        Assert.Equal(2, world.Entities.Count);
        Assert.DoesNotContain(entity1, world.Entities);
        Assert.Contains(entity2, world.Entities);
        Assert.Contains(entity3, world.Entities);
    }

    #endregion

    #region Query Creation

    [Fact]
    public void Query_ReturnsNewQueryInstance()
    {
        // Arrange
        var world = CreateTestWorld();

        // Act
        var query = world.Query();

        // Assert
        Assert.NotNull(query);
    }

    [Fact]
    public void Query_CanBeChained()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddTag("Player");
        world.Flush();

        // Act
        var results = world.Query()
            .With<TransformComponent>()
            .WithTag("Player")
            .Execute()
            .ToList();

        // Assert
        Assert.Single(results);
        Assert.Contains(entity, results);
    }

    [Fact]
    public void CreateCachedQuery_ReturnsBuilder()
    {
        // Arrange
        var world = CreateTestWorld();

        // Act
        var builder = world.CreateCachedQuery<TransformComponent>();

        // Assert
        Assert.NotNull(builder);
    }

    [Fact]
    public void CreateCachedQuery_WithTwoComponents_ReturnsBuilder()
    {
        // Arrange
        var world = CreateTestWorld();

        // Act
        var builder = world.CreateCachedQuery<TransformComponent, VelocityComponent>();

        // Assert
        Assert.NotNull(builder);
    }

    [Fact]
    public void CreateCachedQuery_WithThreeComponents_ReturnsBuilder()
    {
        // Arrange
        var world = CreateTestWorld();

        // Act
        var builder = world.CreateCachedQuery<TransformComponent, VelocityComponent, TestComponent>();

        // Assert
        Assert.NotNull(builder);
    }

    #endregion

    #region Component Notifications

    [Fact]
    public void NotifyComponentAdded_TriggersQueryInvalidation()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        var query = world.Query().With<TransformComponent>();
        Assert.Single(query.Execute().ToList());

        // Act
        var entity2 = world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        // Assert
        Assert.Equal(2, query.Execute().ToList().Count);
    }

    [Fact]
    public void NotifyComponentRemoved_TriggersQueryInvalidation()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        var query = world.Query().With<TransformComponent>();
        Assert.Single(query.Execute().ToList());

        // Act
        entity.RemoveComponent<TransformComponent>();
        world.Flush();

        // Assert
        Assert.Empty(query.Execute().ToList());
    }

    #endregion

    #region Entities Property

    [Fact]
    public void Entities_IsReadOnly()
    {
        var world = CreateTestWorld();
        Assert.IsAssignableFrom<IReadOnlyList<Entity>>(world.Entities);
    }

    [Fact]
    public void Entities_ReflectsCurrentState()
    {
        // Arrange
        var world = CreateTestWorld();
        Assert.Empty(world.Entities);

        // Add
        var entity = world.CreateEntity();
        world.Flush();
        Assert.Single(world.Entities);

        // Remove
        world.DestroyEntity(entity);
        world.Flush();
        Assert.Empty(world.Entities);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void EntityWorld_CompleteLifecycle_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();

        var player = world.CreateEntity("Player")
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 100))
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 200f)
            .AddTag("Player");

        var enemy = world.CreateEntity("Enemy")
            .AddComponent<TransformComponent>()
            .AddTag("Enemy");

        world.Flush();

        Assert.Equal(2, world.Entities.Count);
        Assert.NotNull(world.GetEntityByName("Player"));
        Assert.Single(world.GetEntitiesByTag("Player"));
        Assert.Equal(2, world.GetEntitiesWithComponent<TransformComponent>().Count());

        world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016)));

        var movableEntities = world.Query()
            .With<TransformComponent>()
            .With<VelocityComponent>()
            .Execute()
            .ToList();
        Assert.Single(movableEntities);

        world.DestroyEntity(enemy);
        world.Flush();
        Assert.Single(world.Entities);
        Assert.Null(world.GetEntityByName("Enemy"));

        world.Clear();
        Assert.Empty(world.Entities);
    }

    #endregion

    #region Test Helper Classes

    private class TestEntity : Entity
    {
        public bool OnInitializeCalled { get; private set; }
        public bool OnDestroyCalled { get; private set; }

        public override void OnInitialize() => OnInitializeCalled = true;

        public override void OnDestroy()
        {
            OnDestroyCalled = true;
            base.OnDestroy();
        }
    }

    private class TestComponent : Component { }

    #endregion
}