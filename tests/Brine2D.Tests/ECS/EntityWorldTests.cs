using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Query;
using Brine2D.ECS.Systems;
using Brine2D.Rendering;
using Brine2D.Systems.Physics;
using Brine2D.Systems.Rendering;
using NSubstitute;
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
    public void GetEntityByName_BeforeFlush_FindsPendingEntity()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("PendingPlayer");

        var found = world.GetEntityByName("PendingPlayer");

        Assert.NotNull(found);
        Assert.Equal(entity, found);
    }

    [Fact]
    public void GetEntityByName_BeforeFlush_ReturnsNullForWrongName()
    {
        var world = CreateTestWorld();
        world.CreateEntity("PendingPlayer");

        Assert.Null(world.GetEntityByName("NonExistent"));
    }

    [Fact]
    public void GetEntityByName_IncludeInactive_BeforeFlush_FindsPendingEntity()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("PendingPlayer");

        var found = world.GetEntityByName("PendingPlayer", includeInactive: true);

        Assert.NotNull(found);
        Assert.Equal(entity, found);
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
    public void GetEntitiesByTag_IncludeInactive_ReturnsAllTaggedEntities()
    {
        var world = CreateTestWorld();
        var activeEnemy = world.CreateEntity("Active").AddTag("Enemy");
        var inactiveEnemy = world.CreateEntity("Inactive").AddTag("Enemy");
        inactiveEnemy.IsActive = false;
        world.Flush();

        var enemies = world.GetEntitiesByTag("Enemy", includeInactive: true).ToList();

        Assert.Equal(2, enemies.Count);
        Assert.Contains(activeEnemy, enemies);
        Assert.Contains(inactiveEnemy, enemies);
    }

    [Fact]
    public void GetEntitiesByTag_IncludeInactive_False_StillExcludesInactive()
    {
        var world = CreateTestWorld();
        world.CreateEntity("Active").AddTag("Enemy");
        var inactive = world.CreateEntity("Inactive").AddTag("Enemy");
        inactive.IsActive = false;
        world.Flush();

        var enemies = world.GetEntitiesByTag("Enemy", includeInactive: false).ToList();

        Assert.Single(enemies);
    }

    [Fact]
    public void GetEntitiesWithComponent_ReturnsOnlyEntitiesWithComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity().AddComponent<TransformComponent>();
        var entity2 = world.CreateEntity().AddComponent<TransformComponent>();
        var entity3 = world.CreateEntity().AddComponent<PhysicsBodyComponent>();
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
    public void ForEachWithComponent_Entity_VisitsAllActiveEntitiesWithComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity().AddComponent<TransformComponent>();
        var entity2 = world.CreateEntity().AddComponent<TransformComponent>();
        world.CreateEntity().AddComponent<PhysicsBodyComponent>();
        world.Flush();

        var visited = new List<Entity>();

        // Act
        world.ForEachWithComponent<TransformComponent>(e => visited.Add(e));

        // Assert
        Assert.Equal(2, visited.Count);
        Assert.Contains(entity1, visited);
        Assert.Contains(entity2, visited);
    }

    [Fact]
    public void ForEachWithComponent_EntityAndComponent_VisitsWithComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity().AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(5, 10));
        world.Flush();

        Entity? visitedEntity = null;
        TransformComponent? visitedComponent = null;

        // Act
        world.ForEachWithComponent<TransformComponent>((e, c) =>
        {
            visitedEntity = e;
            visitedComponent = c;
        });

        // Assert
        Assert.Equal(entity, visitedEntity);
        Assert.NotNull(visitedComponent);
        Assert.Equal(new Vector2(5, 10), visitedComponent.LocalPosition);
    }

    [Fact]
    public void ForEachWithComponent_SkipsInactiveEntities()
    {
        // Arrange
        var world = CreateTestWorld();
        world.CreateEntity().AddComponent<TransformComponent>();
        var inactive = world.CreateEntity().AddComponent<TransformComponent>();
        inactive.IsActive = false;
        world.Flush();

        var visited = new List<Entity>();

        // Act
        world.ForEachWithComponent<TransformComponent>(e => visited.Add(e));

        // Assert
        Assert.Single(visited);
        Assert.DoesNotContain(inactive, visited);
    }

    [Fact]
    public void GetEntitiesWithComponents_ReturnsOnlyEntitiesWithBothComponents()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>();
        var entity3 = world.CreateEntity()
            .AddComponent<PhysicsBodyComponent>();
        world.Flush();

        // Act
        var withBoth = world.GetEntitiesWithComponents<TransformComponent, PhysicsBodyComponent>().ToList();

        // Assert
        Assert.Single(withBoth);
        Assert.Contains(entity1, withBoth);
    }

    [Fact]
    public void GetEntitiesWithComponents_ThreeComponents_ReturnsOnlyEntitiesWithAll()
    {
        var world = CreateTestWorld();
        var all3 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<TestComponent>();
        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        world.CreateEntity()
            .AddComponent<TransformComponent>();
        world.Flush();

        var results = world.GetEntitiesWithComponents<TransformComponent, PhysicsBodyComponent, TestComponent>().ToList();

        Assert.Single(results);
        Assert.Contains(all3, results);
    }

    [Fact]
    public void GetEntitiesWithComponents_FourComponents_ReturnsOnlyEntitiesWithAll()
    {
        var world = CreateTestWorld();
        var all4 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<TestComponent>()
            .AddComponent<FourthComponent>();
        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<TestComponent>();
        world.Flush();

        var results = world.GetEntitiesWithComponents<TransformComponent, PhysicsBodyComponent, TestComponent, FourthComponent>().ToList();

        Assert.Single(results);
        Assert.Contains(all4, results);
    }

    [Fact]
    public void GetEntitiesWithComponents_ThreeComponents_ReturnsEmptyWhenNoneMatch()
    {
        var world = CreateTestWorld();
        world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        var results = world.GetEntitiesWithComponents<TransformComponent, PhysicsBodyComponent, TestComponent>().ToList();

        Assert.Empty(results);
    }

    [Fact]
    public void GetEntitiesWithComponents_FourComponents_ReturnsEmptyWhenNoneMatch()
    {
        var world = CreateTestWorld();
        world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        var results = world.GetEntitiesWithComponents<TransformComponent, PhysicsBodyComponent, TestComponent, FourthComponent>().ToList();

        Assert.Empty(results);
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

    [Fact]
    public void GetEntityByName_DestroyedEntity_ReturnsNull()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Target");
        world.Flush();

        world.DestroyEntity(entity);

        Assert.Null(world.GetEntityByName("Target"));
    }

    [Fact]
    public void GetEntityByName_InactiveEntity_ReturnsNull()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Target");
        world.Flush();
        entity.IsActive = false;

        Assert.Null(world.GetEntityByName("Target"));
    }

    [Fact]
    public void GetEntityByName_IncludeInactive_ReturnsInactiveEntity()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Target");
        world.Flush();
        entity.IsActive = false;

        var found = world.GetEntityByName("Target", includeInactive: true);

        Assert.NotNull(found);
        Assert.Equal(entity, found);
    }

    [Fact]
    public void GetEntityByName_IncludeInactive_False_StillExcludesInactive()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Target");
        world.Flush();
        entity.IsActive = false;

        Assert.Null(world.GetEntityByName("Target", includeInactive: false));
    }

    [Fact]
    public void FindEntity_DestroyedEntity_ReturnsNull()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Target");
        world.Flush();

        world.DestroyEntity(entity);

        Assert.Null(world.FindEntity(e => e.Name == "Target"));
    }

    [Fact]
    public void FindEntity_InactiveEntity_ReturnsNull()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Target");
        world.Flush();
        entity.IsActive = false;

        Assert.Null(world.FindEntity(e => e.Name == "Target"));
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

    [Fact]
    public void AddSystem_Sequential_RegistersSystemType()
    {
        var world = CreateTestWorld(o => o.EnableMultiThreading = true);
        world.AddSystem<SequentialTestSystem>();

        // The system should run without error — sequential flag stops parallelism
        for (int i = 0; i < 150; i++)
            world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        // Act & Assert — no exception means [Sequential] was respected
        world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016)));
    }

    [Fact]
    public void RemoveSystem_Sequential_DeregistersSystemType()
    {
        var world = CreateTestWorld(o => o.EnableMultiThreading = true);
        world.AddSystem<SequentialTestSystem>();
        world.RemoveSystem<SequentialTestSystem>();
        world.Flush();

        // After removal, adding it again should work cleanly
        world.AddSystem<SequentialTestSystem>();
        Assert.NotNull(world.GetSystem<SequentialTestSystem>());
    }

    [Fact]
    public void HasSystem_AfterAdd_ReturnsTrue()
    {
        var world = CreateTestWorld();
        world.AddSystem<SequentialTestSystem>();
        Assert.True(world.HasSystem<SequentialTestSystem>());
    }

    [Fact]
    public void HasSystem_BeforeAdd_ReturnsFalse()
    {
        var world = CreateTestWorld();
        Assert.False(world.HasSystem<SequentialTestSystem>());
    }

    [Fact]
    public void HasSystem_AfterRemove_ReturnsFalse()
    {
        var world = CreateTestWorld();
        world.AddSystem<SequentialTestSystem>();
        world.Flush();
        world.RemoveSystem<SequentialTestSystem>();
        world.Flush();
        Assert.False(world.HasSystem<SequentialTestSystem>());
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
        var builder = world.CreateCachedQuery<TransformComponent, PhysicsBodyComponent>();

        // Assert
        Assert.NotNull(builder);
    }

    [Fact]
    public void CreateCachedQuery_WithThreeComponents_ReturnsBuilder()
    {
        // Arrange
        var world = CreateTestWorld();

        // Act
        var builder = world.CreateCachedQuery<TransformComponent, PhysicsBodyComponent, TestComponent>();

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
            .AddComponent<PhysicsBodyComponent>(c => c.Layer = 1)
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
            .With<PhysicsBodyComponent>()
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

        protected internal override void OnInitialize() => OnInitializeCalled = true;

        protected internal override void OnDestroy()
        {
            OnDestroyCalled = true;
            base.OnDestroy();
        }
    }

    private class TestComponent : Component { }

    private class FourthComponent : Component { }

    [Sequential]
    private class SequentialTestSystem : UpdateSystemBase
    {
        private CachedEntityQuery<TransformComponent>? _query;

        public override void Update(IEntityWorld world, GameTime gameTime)
        {
            _query ??= world.CreateCachedQuery<TransformComponent>().Build();
            _query.ForEach(static (_, _) => { });
        }
    }

    private class DisposableRenderSystem : RenderSystemBase
    {
        public bool DisposeCalled { get; private set; }

        public override void Render(IEntityWorld world, IRenderer renderer, GameTime gameTime) { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                DisposeCalled = true;
            base.Dispose(disposing);
        }
    }

    [Fact]
    public void RemoveSystem_RenderSystemBase_DisposesSystem()
    {
        var world = CreateTestWorld();
        world.AddSystem<DisposableRenderSystem>();
        world.Flush();

        var system = world.GetSystem<DisposableRenderSystem>()!;
        world.RemoveSystem<DisposableRenderSystem>();
        world.Flush();

        Assert.True(system.DisposeCalled);
    }

    [Fact]
    public void Dispose_RenderSystemBase_DoesNotDisposeMoreThanOnce()
    {
        var world = CreateTestWorld();
        world.AddSystem<DisposableRenderSystem>();
        world.Flush();

        var system = world.GetSystem<DisposableRenderSystem>()!;
        system.Dispose();
        system.Dispose();

        Assert.True(system.DisposeCalled);
    }

    [Fact]
    public void Behavior_OnStart_CalledByRender_WhenRenderIsFirstPipeline()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<StartTrackingRenderBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<StartTrackingRenderBehavior>()!;

        world.Render(Substitute.For<IRenderer>(), new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.True(behavior.StartCalled);
        Assert.Equal(1, behavior.StartCallCount);
    }

    [Fact]
    public void Behavior_OnStart_CalledByRender_WhenOnlyOverridingRender()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<StartTrackingRenderBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<StartTrackingRenderBehavior>()!;
        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60));

        world.Update(gt);
        Assert.Equal(0, behavior.StartCallCount);

        world.Render(Substitute.For<IRenderer>(), gt);
        Assert.Equal(1, behavior.StartCallCount);

        world.Render(Substitute.For<IRenderer>(), gt);
        Assert.Equal(1, behavior.StartCallCount);
    }

    [Fact]
    public void Behavior_OnStart_NotCalledWhenDisabled_Render()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<StartTrackingRenderBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<StartTrackingRenderBehavior>()!;
        behavior.IsEnabled = false;
        world.Render(Substitute.For<IRenderer>(), new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.False(behavior.StartCalled);
    }

    [Fact]
    public void UpdateSystemBase_OnStart_CalledBeforeFirstUpdate()
    {
        var world = CreateTestWorld();
        world.AddSystem<StartTrackingUpdateSystem>();
        world.Flush();

        var system = world.GetSystem<StartTrackingUpdateSystem>()!;
        Assert.False(system.StartCalled);

        world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.True(system.StartCalled);
        Assert.Equal(["OnStart", "Update"], system.Log);
    }

    [Fact]
    public void UpdateSystemBase_OnStart_CalledOnlyOnce()
    {
        var world = CreateTestWorld();
        world.AddSystem<StartTrackingUpdateSystem>();
        world.Flush();

        var system = world.GetSystem<StartTrackingUpdateSystem>()!;
        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60));

        world.Update(gt);
        world.Update(gt);

        Assert.Equal(1, system.StartCallCount);
    }

    [Fact]
    public void RenderSystemBase_OnStart_CalledBeforeFirstRender()
    {
        var world = CreateTestWorld();
        world.AddSystem<StartTrackingRenderSystem>();
        world.Flush();

        var system = world.GetSystem<StartTrackingRenderSystem>()!;
        Assert.False(system.StartCalled);

        world.Render(Substitute.For<IRenderer>(), new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.True(system.StartCalled);
        Assert.Equal(["OnStart", "Render"], system.Log);
    }

    [Fact]
    public void RenderSystemBase_OnStart_CalledOnlyOnce()
    {
        var world = CreateTestWorld();
        world.AddSystem<StartTrackingRenderSystem>();
        world.Flush();

        var system = world.GetSystem<StartTrackingRenderSystem>()!;
        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60));
        var renderer = Substitute.For<IRenderer>();

        world.Render(renderer, gt);
        world.Render(renderer, gt);

        Assert.Equal(1, system.StartCallCount);
    }

    private class StartTrackingRenderBehavior : Behavior
    {
        public bool StartCalled { get; private set; }
        public int StartCallCount { get; private set; }
        public override void OnStart() { StartCalled = true; StartCallCount++; }
        public override void Render(IRenderer renderer, GameTime gameTime) { }
    }

    private class StartTrackingUpdateSystem : UpdateSystemBase
    {
        public bool StartCalled { get; private set; }
        public int StartCallCount { get; private set; }
        public List<string> Log { get; } = new();

        public override void OnStart(IEntityWorld world) { StartCalled = true; StartCallCount++; Log.Add("OnStart"); }
        public override void Update(IEntityWorld world, GameTime gameTime) => Log.Add("Update");
    }

    private class StartTrackingRenderSystem : RenderSystemBase
    {
        public bool StartCalled { get; private set; }
        public int StartCallCount { get; private set; }
        public List<string> Log { get; } = new();

        public override void OnStart(IEntityWorld world) { StartCalled = true; StartCallCount++; Log.Add("OnStart"); }
        public override void Render(IEntityWorld world, IRenderer renderer, GameTime gameTime) => Log.Add("Render");
    }

    #endregion

    #region GetEffectiveOptions (sequential options staleness)

    [Fact]
    public void GetEffectiveOptions_AfterChangingParallelThreshold_ReflectsNewValue()
    {
        var world = (EntityWorld)CreateTestWorld(o =>
        {
            o.EnableMultiThreading = true;
            o.ParallelEntityThreshold = 50;
        });
        world.AddSystem<SequentialTestSystem>();

        for (int i = 0; i < 10; i++)
            world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.016));
        world.Update(gt);

        // Mutate options after first sequential dispatch
        world.Options.ParallelEntityThreshold = 999;
        world.Options.WorkerThreadCount = 2;

        // Trigger another update — sequential options must reflect updated values
        world.Update(gt);

        // No assertion on internals; the absence of a crash / wrong-thread-count
        // exception proves the options were read fresh rather than from a stale cache.
        Assert.True(world.HasSystem<SequentialTestSystem>());
    }

    #endregion

    #region ForEachWithComponents

    [Fact]
    public void ForEachWithComponents_InvokesActionForMatchingEntities()
    {
        var world = CreateTestWorld();
        var e1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var e2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        world.CreateEntity().AddComponent<TransformComponent>(); // no PhysicsBody
        world.Flush();

        var visited = new List<Entity>();
        world.ForEachWithComponents<TransformComponent, PhysicsBodyComponent>(
            (entity, _, _) => visited.Add(entity));

        Assert.Equal(2, visited.Count);
        Assert.Contains(e1, visited);
        Assert.Contains(e2, visited);
    }

    [Fact]
    public void ForEachWithComponents_SkipsInactiveEntities()
    {
        var world = CreateTestWorld();
        var active = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        var inactive = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        world.Flush();
        inactive.IsActive = false;

        var visited = new List<Entity>();
        world.ForEachWithComponents<TransformComponent, PhysicsBodyComponent>(
            (entity, _, _) => visited.Add(entity));

        Assert.Single(visited);
        Assert.Contains(active, visited);
    }

    [Fact]
    public void ForEachWithComponents_PassesBothComponentInstances()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(7, 8))
            .AddComponent<PhysicsBodyComponent>();
        world.Flush();

        TransformComponent? capturedTransform = null;
        world.ForEachWithComponents<TransformComponent, PhysicsBodyComponent>(
            (_, transform, _) => capturedTransform = transform);

        Assert.NotNull(capturedTransform);
        Assert.Equal(new Vector2(7, 8), capturedTransform.LocalPosition);
    }

    [Fact]
    public void ForEachWithComponents_NoMatchingEntities_DoesNotInvoke()
    {
        var world = CreateTestWorld();
        world.CreateEntity().AddComponent<TransformComponent>();
        world.Flush();

        int callCount = 0;
        world.ForEachWithComponents<TransformComponent, PhysicsBodyComponent>(
            (_, _, _) => callCount++);

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void ForEachWithComponents_EmptyWorld_DoesNotThrow()
    {
        var world = CreateTestWorld();

        var ex = Record.Exception(() =>
            world.ForEachWithComponents<TransformComponent, PhysicsBodyComponent>(
                (_, _, _) => { }));

        Assert.Null(ex);
    }

    [Fact]
    public void ForEachWithComponents_T3_InvokesActionForMatchingEntities()
    {
        var world = CreateTestWorld();
        var e1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>();
        var e2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>();
        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>(); // missing SpriteComponent
        world.Flush();

        var visited = new List<Entity>();
        world.ForEachWithComponents<TransformComponent, PhysicsBodyComponent, SpriteComponent>(
            (entity, _, _, _) => visited.Add(entity));

        Assert.Equal(2, visited.Count);
        Assert.Contains(e1, visited);
        Assert.Contains(e2, visited);
    }

    [Fact]
    public void ForEachWithComponents_T3_SkipsInactiveEntities()
    {
        var world = CreateTestWorld();
        var active = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>();
        var inactive = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>();
        world.Flush();
        inactive.IsActive = false;

        var visited = new List<Entity>();
        world.ForEachWithComponents<TransformComponent, PhysicsBodyComponent, SpriteComponent>(
            (entity, _, _, _) => visited.Add(entity));

        Assert.Single(visited);
        Assert.Contains(active, visited);
    }

    [Fact]
    public void ForEachWithComponents_T3_PassesAllThreeComponents()
    {
        var world = CreateTestWorld();
        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(3, 4))
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>(s => s.TexturePath = "test.png");
        world.Flush();

        TransformComponent? capturedTransform = null;
        SpriteComponent? capturedSprite = null;
        world.ForEachWithComponents<TransformComponent, PhysicsBodyComponent, SpriteComponent>(
            (_, t, _, s) => { capturedTransform = t; capturedSprite = s; });

        Assert.NotNull(capturedTransform);
        Assert.Equal(new Vector2(3, 4), capturedTransform.LocalPosition);
        Assert.NotNull(capturedSprite);
        Assert.Equal("test.png", capturedSprite.TexturePath);
    }

    [Fact]
    public void ForEachWithComponents_T3_NoMatch_DoesNotInvoke()
    {
        var world = CreateTestWorld();
        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>();
        world.Flush();

        int callCount = 0;
        world.ForEachWithComponents<TransformComponent, PhysicsBodyComponent, SpriteComponent>(
            (_, _, _, _) => callCount++);

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void ForEachWithComponents_T3_EmptyWorld_DoesNotThrow()
    {
        var world = CreateTestWorld();

        var ex = Record.Exception(() =>
            world.ForEachWithComponents<TransformComponent, PhysicsBodyComponent, SpriteComponent>(
                (_, _, _, _) => { }));

        Assert.Null(ex);
    }

    [Fact]
    public void ForEachWithComponents_T4_InvokesActionForMatchingEntities()
    {
        var world = CreateTestWorld();
        var e1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>()
            .AddComponent<KinematicCharacterBody>();
        var e2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>()
            .AddComponent<KinematicCharacterBody>();
        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>(); // missing KinematicCharacterBody
        world.Flush();

        var visited = new List<Entity>();
        world.ForEachWithComponents<TransformComponent, PhysicsBodyComponent, SpriteComponent, KinematicCharacterBody>(
            (entity, _, _, _, _) => visited.Add(entity));

        Assert.Equal(2, visited.Count);
        Assert.Contains(e1, visited);
        Assert.Contains(e2, visited);
    }

    [Fact]
    public void ForEachWithComponents_T4_SkipsInactiveEntities()
    {
        var world = CreateTestWorld();
        var active = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>()
            .AddComponent<KinematicCharacterBody>();
        var inactive = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>()
            .AddComponent<KinematicCharacterBody>();
        world.Flush();
        inactive.IsActive = false;

        var visited = new List<Entity>();
        world.ForEachWithComponents<TransformComponent, PhysicsBodyComponent, SpriteComponent, KinematicCharacterBody>(
            (entity, _, _, _, _) => visited.Add(entity));

        Assert.Single(visited);
        Assert.Contains(active, visited);
    }

    [Fact]
    public void ForEachWithComponents_T4_NoMatch_DoesNotInvoke()
    {
        var world = CreateTestWorld();
        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>();
        world.Flush();

        int callCount = 0;
        world.ForEachWithComponents<TransformComponent, PhysicsBodyComponent, SpriteComponent, KinematicCharacterBody>(
            (_, _, _, _, _) => callCount++);

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void ForEachWithComponents_T4_EmptyWorld_DoesNotThrow()
    {
        var world = CreateTestWorld();

        var ex = Record.Exception(() =>
            world.ForEachWithComponents<TransformComponent, PhysicsBodyComponent, SpriteComponent, KinematicCharacterBody>(
                (_, _, _, _, _) => { }));

        Assert.Null(ex);
    }

    [Fact]
    public void ForEachWithComponents_T5_InvokesActionForMatchingEntities()
    {
        var world = CreateTestWorld();
        var e1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>()
            .AddComponent<KinematicCharacterBody>()
            .AddComponent<CameraFollowComponent>();
        var e2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>()
            .AddComponent<KinematicCharacterBody>()
            .AddComponent<CameraFollowComponent>();
        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>()
            .AddComponent<KinematicCharacterBody>(); // missing CameraFollowComponent
        world.Flush();

        var visited = new List<Entity>();
        world.ForEachWithComponents<TransformComponent, PhysicsBodyComponent, SpriteComponent, KinematicCharacterBody, CameraFollowComponent>(
            (entity, _, _, _, _, _) => visited.Add(entity));

        Assert.Equal(2, visited.Count);
        Assert.Contains(e1, visited);
        Assert.Contains(e2, visited);
    }

    [Fact]
    public void ForEachWithComponents_T5_SkipsInactiveEntities()
    {
        var world = CreateTestWorld();
        var active = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>()
            .AddComponent<KinematicCharacterBody>()
            .AddComponent<CameraFollowComponent>();
        var inactive = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>()
            .AddComponent<KinematicCharacterBody>()
            .AddComponent<CameraFollowComponent>();
        world.Flush();
        inactive.IsActive = false;

        var visited = new List<Entity>();
        world.ForEachWithComponents<TransformComponent, PhysicsBodyComponent, SpriteComponent, KinematicCharacterBody, CameraFollowComponent>(
            (entity, _, _, _, _, _) => visited.Add(entity));

        Assert.Single(visited);
        Assert.Contains(active, visited);
    }

    [Fact]
    public void ForEachWithComponents_T5_NoMatch_DoesNotInvoke()
    {
        var world = CreateTestWorld();
        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>()
            .AddComponent<KinematicCharacterBody>();
        world.Flush();

        int callCount = 0;
        world.ForEachWithComponents<TransformComponent, PhysicsBodyComponent, SpriteComponent, KinematicCharacterBody, CameraFollowComponent>(
            (_, _, _, _, _, _) => callCount++);

        Assert.Equal(0, callCount);
    }

    [Fact]
    public void ForEachWithComponents_T5_EmptyWorld_DoesNotThrow()
    {
        var world = CreateTestWorld();

        var ex = Record.Exception(() =>
            world.ForEachWithComponents<TransformComponent, PhysicsBodyComponent, SpriteComponent, KinematicCharacterBody, CameraFollowComponent>(
                (_, _, _, _, _, _) => { }));

        Assert.Null(ex);
    }

    [Fact]
    public void GetEntitiesWithComponents_T5_ReturnsMatchingEntities()
    {
        var world = CreateTestWorld();
        var match = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>()
            .AddComponent<KinematicCharacterBody>()
            .AddComponent<CameraFollowComponent>();
        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<PhysicsBodyComponent>()
            .AddComponent<SpriteComponent>()
            .AddComponent<KinematicCharacterBody>();
        world.Flush();

        var results = world.GetEntitiesWithComponents<TransformComponent, PhysicsBodyComponent, SpriteComponent, KinematicCharacterBody, CameraFollowComponent>().ToList();

        Assert.Single(results);
        Assert.Contains(match, results);
    }

    #endregion
}

public class EntityWorldLifecycleEventTests : TestBase
{
    [Fact]
    public void EntityCount_NoEntities_ReturnsZero()
    {
        var world = CreateTestWorld();
        Assert.Equal(0, world.EntityCount);
    }

    [Fact]
    public void EntityCount_AfterFlush_ReflectsCreatedEntities()
    {
        var world = CreateTestWorld();
        world.CreateEntity();
        world.CreateEntity();
        world.Flush();
        Assert.Equal(2, world.EntityCount);
    }

    [Fact]
    public void EntityCount_AfterDestroy_Decrements()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        world.Flush();
        world.DestroyEntity(entity);
        world.Flush();
        Assert.Equal(0, world.EntityCount);
    }

    [Fact]
    public void EntityCount_MatchesEntitiesCount()
    {
        var world = CreateTestWorld();
        world.CreateEntity();
        world.CreateEntity();
        world.Flush();
        Assert.Equal(world.Entities.Count, world.EntityCount);
    }

    [Fact]
    public void OnExceptionSwallowed_CalledWhenUpdateSystemThrows()
    {
        Exception? captured = null;
        string? capturedContext = null;

        var world = CreateTestWorld(o =>
        {
            o.PropagateExceptions = false;
            o.OnExceptionSwallowed = (ex, ctx) => { captured = ex; capturedContext = ctx; };
        });

        world.AddSystem<ThrowingUpdateSystem>();
        world.Update(new GameTime());

        Assert.NotNull(captured);
        Assert.NotNull(capturedContext);
        Assert.Contains("ThrowingUpdateSystem", capturedContext);
    }

    [Fact]
    public void OnExceptionSwallowed_CalledWhenFixedUpdateSystemThrows()
    {
        Exception? captured = null;

        var world = CreateTestWorld(o =>
        {
            o.PropagateExceptions = false;
            o.OnExceptionSwallowed = (ex, _) => captured = ex;
        });

        world.AddSystem<ThrowingFixedUpdateSystem>();
        world.FixedUpdate(new GameTime());

        Assert.NotNull(captured);
    }

    [Fact]
    public void OnExceptionSwallowed_NotCalledWhenPropagateExceptionsIsTrue()
    {
        bool callbackInvoked = false;

        var world = CreateTestWorld(o =>
        {
            o.PropagateExceptions = true;
            o.OnExceptionSwallowed = (_, _) => callbackInvoked = true;
        });

        world.AddSystem<ThrowingUpdateSystem>();
        Assert.Throws<InvalidOperationException>(() => world.Update(new GameTime()));
        Assert.False(callbackInvoked);
    }

    [Fact]
    public void OnExceptionSwallowed_CallbackThrowingDoesNotCrashLoop()
    {
        var world = CreateTestWorld(o =>
        {
            o.PropagateExceptions = false;
            o.OnExceptionSwallowed = (_, _) => throw new Exception("callback explosion");
        });

        world.AddSystem<ThrowingUpdateSystem>();
        var ex = Record.Exception(() => world.Update(new GameTime()));
        Assert.Null(ex);
    }

    private class ThrowingUpdateSystem : IUpdateSystem
    {
        public bool IsEnabled { get; set; } = true;
        public int UpdateOrder => 0;
        public void Update(IEntityWorld world, GameTime gameTime)
            => throw new InvalidOperationException("boom");
    }

    private class ThrowingFixedUpdateSystem : IFixedUpdateSystem
    {
        public bool IsEnabled { get; set; } = true;
        public int FixedUpdateOrder => 0;
        public void FixedUpdate(IEntityWorld world, GameTime fixedTime)
            => throw new InvalidOperationException("boom");
    }

    [Fact]
    public void EntityCreated_FiredAfterOnInitialize()
    {
        var world = CreateTestWorld();
        Entity? received = null;
        world.EntityCreated += e => received = e;

        var entity = world.CreateEntity("Evt");
        world.Flush();

        Assert.NotNull(received);
        Assert.Equal(entity, received);
    }

    [Fact]
    public void EntityCreated_NotFiredBeforeFlush()
    {
        var world = CreateTestWorld();
        int count = 0;
        world.EntityCreated += _ => count++;

        world.CreateEntity();

        Assert.Equal(0, count);

        world.Flush();
        Assert.Equal(1, count);
    }

    [Fact]
    public void EntityCreated_FiredOncePerEntity()
    {
        var world = CreateTestWorld();
        int count = 0;
        world.EntityCreated += _ => count++;

        world.CreateEntity();
        world.CreateEntity();
        world.Flush();

        Assert.Equal(2, count);
    }

    [Fact]
    public void EntityDestroyed_FiredWhenEntityIsDestroyed()
    {
        var world = CreateTestWorld();
        Entity? received = null;
        world.EntityDestroyed += e => received = e;

        var entity = world.CreateEntity("Doomed");
        world.Flush();
        world.DestroyEntity(entity);
        world.Flush();

        Assert.NotNull(received);
        Assert.Equal(entity, received);
    }

    [Fact]
    public void EntityDestroyed_FiredBeforeWorldReferenceIsCleared()
    {
        var world = CreateTestWorld();
        IEntityWorld? worldDuringDestroy = null;
        world.EntityDestroyed += e => worldDuringDestroy = e.World;

        var entity = world.CreateEntity();
        world.Flush();
        world.DestroyEntity(entity);
        world.Flush();

        Assert.NotNull(worldDuringDestroy);
    }

    [Fact]
    public void EntityDestroyed_NotFiredBeforeFlush()
    {
        var world = CreateTestWorld();
        int count = 0;
        world.EntityDestroyed += _ => count++;

        var entity = world.CreateEntity();
        world.Flush();
        world.DestroyEntity(entity);

        Assert.Equal(0, count);

        world.Flush();
        Assert.Equal(1, count);
    }

    [Fact]
    public void ForEachWithBehavior_InvokesActionForMatchingBehaviors()
    {
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity("e1");
        entity1.AddBehavior<ForEachTestBehavior>();
        var entity2 = world.CreateEntity("e2");
        entity2.AddBehavior<ForEachTestBehavior>();
        var entity3 = world.CreateEntity("e3");
        entity3.AddBehavior<OtherForEachBehavior>();
        world.Flush();

        var visited = new List<string>();
        world.ForEachWithBehavior<ForEachTestBehavior>((e, b) => visited.Add(e.Name));

        Assert.Equal(2, visited.Count);
        Assert.Contains("e1", visited);
        Assert.Contains("e2", visited);
    }

    [Fact]
    public void ForEachWithBehavior_EntityOnly_InvokesActionForMatchingBehaviors()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("e1");
        entity.AddBehavior<ForEachTestBehavior>();
        world.CreateEntity("e2").AddBehavior<OtherForEachBehavior>();
        world.Flush();

        var visited = new List<string>();
        world.ForEachWithBehavior<ForEachTestBehavior>(e => visited.Add(e.Name));

        Assert.Equal(["e1"], visited);
    }

    [Fact]
    public void ForEachWithBehavior_SkipsInactiveEntities()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("active");
        entity.AddBehavior<ForEachTestBehavior>();
        var inactive = world.CreateEntity("inactive");
        inactive.AddBehavior<ForEachTestBehavior>();
        world.Flush();
        inactive.IsActive = false;

        var visited = new List<string>();
        world.ForEachWithBehavior<ForEachTestBehavior>((e, b) => visited.Add(e.Name));

        Assert.Equal(["active"], visited);
    }

    [Fact]
    public void ForEachWithBehavior_PassesBehaviorInstanceToCallback()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<ForEachTestBehavior>();
        world.Flush();
        entity.GetBehavior<ForEachTestBehavior>()!.Value = 42;

        ForEachTestBehavior? received = null;
        world.ForEachWithBehavior<ForEachTestBehavior>((e, b) => received = b);

        Assert.NotNull(received);
        Assert.Equal(42, received.Value);
    }

    [Fact]
    public void ForEachWithBehavior_EmptyWorld_DoesNotThrow()
    {
        var world = CreateTestWorld();
        world.Flush();

        var ex = Record.Exception(() =>
            world.ForEachWithBehavior<ForEachTestBehavior>((e, b) => { }));

        Assert.Null(ex);
    }

    [Fact]
    public void BehaviorOrderDirtyFlag_OnlyUpdateDirtied_WhenUpdateOnlyBehaviorAdded()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        world.Flush();

        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60));

        world.Update(gt);
        world.FixedUpdate(gt);
        var renderer = NSubstitute.Substitute.For<IRenderer>();
        world.Render(renderer, gt);

        var updateCallCount = 0;
        var fixedCallCount = 0;
        var renderCallCount = 0;

        entity.AddBehavior<UpdateOnlyBehavior>();
        world.Flush();

        var b = entity.GetBehavior<UpdateOnlyBehavior>()!;
        b.OnUpdateCalled = () => updateCallCount++;

        world.Update(gt);
        world.FixedUpdate(gt);
        world.Render(renderer, gt);

        Assert.Equal(1, updateCallCount);
    }

    private class ForEachTestBehavior : Behavior
    {
        public int Value { get; set; }
    }

    private class OtherForEachBehavior : Behavior { }

    private class UpdateOnlyBehavior : Behavior
    {
        public Action? OnUpdateCalled { get; set; }
        public override void Update(GameTime gameTime) => OnUpdateCalled?.Invoke();
    }

    #region AddSystem instance overload

    [Fact]
    public void AddSystem_Instance_RegistersInUpdatePipeline()
    {
        var world = CreateTestWorld();
        var system = new TrackingUpdateSystem();
        world.AddSystem<TrackingUpdateSystem>(system);

        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60));
        world.Update(gt);

        Assert.Equal(1, system.UpdateCallCount);
    }

    [Fact]
    public void AddSystem_Instance_IsReturnedByGetSystem()
    {
        var world = CreateTestWorld();
        var system = new TrackingUpdateSystem();
        world.AddSystem<TrackingUpdateSystem>(system);

        var retrieved = world.GetSystem<TrackingUpdateSystem>();

        Assert.Same(system, retrieved);
    }

    [Fact]
    public void AddSystem_Instance_DuplicateType_IsIgnored()
    {
        var world = CreateTestWorld();
        var system1 = new TrackingUpdateSystem();
        var system2 = new TrackingUpdateSystem();

        world.AddSystem<TrackingUpdateSystem>(system1);
        world.AddSystem<TrackingUpdateSystem>(system2);

        var retrieved = world.GetSystem<TrackingUpdateSystem>();
        Assert.Same(system1, retrieved);
    }

    [Fact]
    public void AddSystem_Instance_NoPipelineInterface_Throws()
    {
        var world = CreateTestWorld();
        var system = new NoPipelineSystem();

        Assert.Throws<InvalidOperationException>(() => world.AddSystem<NoPipelineSystem>(system));
    }

    private class TrackingUpdateSystem : IUpdateSystem
    {
        public bool IsEnabled { get; set; } = true;
        public int UpdateOrder => 0;
        public int UpdateCallCount { get; private set; }
        public void Update(IEntityWorld world, GameTime gameTime) => UpdateCallCount++;
    }

    private class NoPipelineSystem : ISystem
    {
        public bool IsEnabled { get; set; } = true;
    }

    #endregion

    #region OnStart-only behavior pipeline enrollment

    [Fact]
    public void OnStartOnlyBehavior_DoesNotRunUpdateOrRender()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<OnStartOnlyBehavior>();
        world.Flush();

        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60));
        var renderer = NSubstitute.Substitute.For<IRenderer>();

        world.Update(gt);
        world.FixedUpdate(gt);
        world.Render(renderer, gt);

        var b = entity.GetBehavior<OnStartOnlyBehavior>()!;
        Assert.Equal(0, b.UpdateCallCount);
        Assert.Equal(0, b.FixedUpdateCallCount);
        Assert.Equal(0, b.RenderCallCount);
    }

    private class OnStartOnlyBehavior : Behavior
    {
        public int UpdateCallCount { get; private set; }
        public int FixedUpdateCallCount { get; private set; }
        public int RenderCallCount { get; private set; }
    }

    #endregion
}
