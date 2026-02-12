using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Rendering;
using Brine2D.Systems.Physics;
using NSubstitute;

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
        Assert.NotEqual(Guid.Empty, entity1.Id);
        Assert.NotEqual(Guid.Empty, entity2.Id);
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
        world.Flush(); // Process deferred operations

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

        // Assert - Should be inactive immediately (before Flush)
        Assert.False(entity.IsActive);
    }

    [Fact]
    public void DestroyEntity_PreventsDoubleQueuing()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        world.Flush();

        // Act - Try to destroy twice
        world.DestroyEntity(entity);
        world.DestroyEntity(entity); // Should not throw or cause issues
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
        var testEntity = (TestEntity)entity;

        // Act
        world.DestroyEntity(entity);
        world.Flush();

        // Assert
        Assert.True(testEntity.OnDestroyCalled);
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
        var found = world.GetEntityById(Guid.NewGuid());

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
        Assert.Equal(entity1.Id, found.Id); // Should return first one
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
        var entity1 = world.CreateEntity("Entity1");
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

    #region Update and Render

    [Fact]
    public void Update_CallsOnUpdateForActiveEntities()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity<TestEntity>();
        world.Flush();
        var testEntity = (TestEntity)entity;

        var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

        // Act
        world.Update(gameTime);

        // Assert
        Assert.True(testEntity.OnUpdateCalled);
    }

    [Fact]
    public void Update_DoesNotCallOnUpdateForInactiveEntities()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity<TestEntity>();
        entity.IsActive = false;
        world.Flush();
        var testEntity = (TestEntity)entity;

        var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

        // Act
        world.Update(gameTime);

        // Assert
        Assert.False(testEntity.OnUpdateCalled);
    }

    [Fact]
    public void Update_CallsOnUpdateForEnabledComponents()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;

        var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

        // Act
        world.Update(gameTime);

        // Assert
        Assert.True(component.OnUpdateCalled);
    }

    [Fact]
    public void Update_DoesNotCallOnUpdateForDisabledComponents()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;
        component.IsEnabled = false;

        var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

        // Act
        world.Update(gameTime);

        // Assert
        Assert.False(component.OnUpdateCalled);
    }

    [Fact]
    public void Update_ProcessesDeferredOperations()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));

        // Act
        world.Update(gameTime);

        // Assert - Entity should be in world after update
        Assert.Contains(entity, world.Entities);
    }

    [Fact]
    public void Render_CallsOnRenderForActiveEntities()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity<TestEntity>();
        world.Flush();
        var testEntity = (TestEntity)entity;
        var mockRenderer = Substitute.For<IRenderer>();

        // Act
        world.Render(mockRenderer);

        // Assert
        Assert.True(testEntity.OnRenderCalled);
    }

    [Fact]
    public void Render_CallsOnRenderForEnabledComponents()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TestComponent>();
        world.Flush();
        var component = entity.GetComponent<TestComponent>()!;
        var mockRenderer = Substitute.For<IRenderer>();

        // Act
        world.Render(mockRenderer);

        // Assert
        Assert.True(component.OnRenderCalled);
    }

    #endregion

    #region Clear and Flush

    [Fact]
    public void Clear_QueuesAllEntitiesForDestruction()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        world.Flush();

        // Act
        world.Clear();

        // Assert
        Assert.Empty(world.Entities);
    }

    [Fact]
    public void Clear_DeactivatesAllEntitiesImmediately()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity();
        var entity2 = world.CreateEntity();
        world.Flush();

        // Act
        world.Clear();

        // Assert
        Assert.False(entity1.IsActive);
        Assert.False(entity2.IsActive);
    }

    [Fact]
    public void Flush_ProcessesPendingOperations()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Assert - Entity not in list yet
        Assert.Empty(world.Entities);

        // Act
        world.Flush();

        // Assert - Entity now in list
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

        // Act - Create, flush, then destroy in same cycle
        var entity1 = world.CreateEntity("Temp1");
        var entity2 = world.CreateEntity("Temp2");
        world.Flush(); // Process creations

        Assert.Equal(2, world.Entities.Count);

        world.DestroyEntity(entity1);
        var entity3 = world.CreateEntity("Temp3"); // Create while destroying
        world.Flush(); // Process both operations

        // Assert - entity2 and entity3 should remain, entity1 should be gone
        Assert.Equal(2, world.Entities.Count);
        Assert.DoesNotContain(entity1, world.Entities);
        Assert.Contains(entity2, world.Entities);
        Assert.Contains(entity3, world.Entities);
    }

    #endregion

    #region Service Provider

    [Fact]
    public void GetService_UnregisteredService_ReturnsNull()
    {
        // Arrange
        var world = CreateTestWorld();

        // Act - IRenderer is not registered in test setup
        var service = world.GetService<IRenderer>();

        // Assert
        Assert.Null(service);
    }

    [Fact]
    public void GetRequiredService_UnregisteredService_ThrowsException()
    {
        // Arrange
        var world = CreateTestWorld();

        // Act & Assert - IRenderer is not registered in test setup
        var exception = Assert.Throws<InvalidOperationException>(() =>
            world.GetRequiredService<IRenderer>());

        Assert.Contains("IRenderer", exception.Message);
        Assert.Contains("not registered", exception.Message);
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
        var firstResults = query.Execute().ToList();
        Assert.Single(firstResults);

        // Act - Add component to new entity
        var entity2 = world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        // Assert - Query should see new entity
        var secondResults = query.Execute().ToList();
        Assert.Equal(2, secondResults.Count);
    }

    [Fact]
    public void NotifyComponentRemoved_TriggersQueryInvalidation()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        var query = world.Query().With<TransformComponent>();
        var firstResults = query.Execute().ToList();
        Assert.Single(firstResults);

        // Act - Remove component
        entity.RemoveComponent<TransformComponent>();
        world.Flush();

        // Assert - Query should not see entity anymore
        var secondResults = query.Execute().ToList();
        Assert.Empty(secondResults);
    }

    #endregion

    #region Entities Property

    [Fact]
    public void Entities_IsReadOnly()
    {
        // Arrange
        var world = CreateTestWorld();

        // Act
        var entities = world.Entities;

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<Entity>>(entities);
    }

    [Fact]
    public void Entities_ReflectsCurrentState()
    {
        // Arrange
        var world = CreateTestWorld();

        // Act & Assert - Initially empty
        Assert.Empty(world.Entities);

        // Add entity
        var entity = world.CreateEntity();
        world.Flush();
        Assert.Single(world.Entities);

        // Remove entity
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

        // Create entities
        var player = world.CreateEntity("Player")
            .AddComponent<TransformComponent>(t => t.LocalPosition = new System.Numerics.Vector2(100, 100))
            .AddComponent<VelocityComponent>(v => v.MaxSpeed = 200f)
            .AddTag("Player");

        var enemy = world.CreateEntity("Enemy")
            .AddComponent<TransformComponent>()
            .AddTag("Enemy");

        world.Flush();

        // Assert creation
        Assert.Equal(2, world.Entities.Count);
        Assert.NotNull(world.GetEntityByName("Player"));
        Assert.Single(world.GetEntitiesByTag("Player"));
        Assert.Equal(2, world.GetEntitiesWithComponent<TransformComponent>().Count());

        // Update
        var gameTime = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));
        world.Update(gameTime);

        // Query
        var movableEntities = world.Query()
            .With<TransformComponent>()
            .With<VelocityComponent>()
            .Execute()
            .ToList();
        Assert.Single(movableEntities);

        // Destroy
        world.DestroyEntity(enemy);
        world.Flush();
        Assert.Single(world.Entities);
        Assert.Null(world.GetEntityByName("Enemy"));

        // Clear
        world.Clear();
        Assert.Empty(world.Entities);
    }

    #endregion

    #region Test Helper Classes

    private class TestEntity : Entity
    {
        public bool OnInitializeCalled { get; private set; }
        public bool OnUpdateCalled { get; private set; }
        public bool OnRenderCalled { get; private set; }
        public bool OnDestroyCalled { get; private set; }

        public override void OnInitialize()
        {
            OnInitializeCalled = true;
        }

        public override void OnUpdate(GameTime gameTime)
        {
            OnUpdateCalled = true;
        }

        public override void OnRender(IRenderer renderer)
        {
            OnRenderCalled = true;
        }

        public override void OnDestroy()
        {
            OnDestroyCalled = true;
            base.OnDestroy();
        }
    }

    private class TestComponent : Component
    {
        public bool OnUpdateCalled { get; private set; }
        public bool OnRenderCalled { get; private set; }

        protected internal override void OnUpdate(GameTime gameTime)
        {
            OnUpdateCalled = true;
        }

        protected internal override void OnRender(IRenderer renderer)
        {
            OnRenderCalled = true;
        }
    }

    #endregion
}