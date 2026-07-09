using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.ECS.Components.Joints;
using Brine2D.ECS.Serialization;
using Brine2D.ECS.Systems;
using Brine2D.Rendering;
using Brine2D.Systems.Rendering;
using NSubstitute;
using Xunit;

namespace Brine2D.Tests.ECS.Serialization;

public class EntitySerializerTests : TestBase
{
    private readonly EntitySerializer _serializer = new();

    #region Snapshot — naming

    [Fact]
    public void CreateSnapshot_StoresIsActiveNotIsEnabled()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.IsActive = false;
        world.Flush();

        var snapshot = _serializer.CreateSnapshot(entity);

        Assert.False(snapshot.IsActive);
    }

    #endregion

    #region RestoreWorldFromSnapshot — system pipeline preserved

    [Fact]
    public void RestoreWorldFromSnapshot_DoesNotDestroyRegisteredSystems()
    {
        var world = CreateTestWorld();
        world.AddSystem<StubUpdateSystem>();
        world.Flush();

        var snapshot = _serializer.CreateWorldSnapshot(world);
        _serializer.RestoreWorldFromSnapshot(world, snapshot);
        world.Flush();

        Assert.NotNull(world.GetSystem<StubUpdateSystem>());
    }

    [Fact]
    public void RestoreWorldFromSnapshot_ClearsEntitiesBetweenRestores()
    {
        var world = CreateTestWorld();
        world.CreateEntity("A").AddComponent<TransformComponent>();
        world.Flush();

        var snapshot = _serializer.CreateWorldSnapshot(world);

        world.CreateEntity("B").AddComponent<TransformComponent>();
        world.Flush();

        _serializer.RestoreWorldFromSnapshot(world, snapshot);
        world.Flush();

        Assert.Single(world.Entities);
        Assert.Equal("A", world.Entities[0].Name);
    }

    #endregion

    #region RestoreComponent — property values restored correctly

    [Fact]
    public void RestoreEntity_RestoresComponentPropertyValues()
    {
        var world = CreateTestWorld();
        var original = world.CreateEntity("Player");
        original.AddComponent<TransformComponent>(t =>
        {
            t.LocalPosition = new Vector2(42f, 99f);
            t.LocalRotation = 1.5f;
        });
        world.Flush();

        var snapshot = _serializer.CreateSnapshot(original);

        var world2 = CreateTestWorld();
        var restored = _serializer.RestoreEntity(world2, snapshot);
        world2.Flush();

        var t = restored.GetComponent<TransformComponent>();
        Assert.NotNull(t);
        Assert.Equal(new Vector2(42f, 99f), t.LocalPosition);
        Assert.Equal(1.5f, t.LocalRotation);
    }

    [Fact]
    public void RestoreEntity_DoesNotProduceDefaultValuedComponents()
    {
        var world = CreateTestWorld();
        var original = world.CreateEntity("Enemy");
        original.AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(10f, 20f));
        world.Flush();

        var snapshot = _serializer.CreateSnapshot(original);

        var world2 = CreateTestWorld();
        var restored = _serializer.RestoreEntity(world2, snapshot);
        world2.Flush();

        var t = restored.GetComponent<TransformComponent>();
        Assert.NotNull(t);
        Assert.NotEqual(Vector2.Zero, t.LocalPosition);
    }

    [Fact]
    public void RestoreEntity_RestoresTags()
    {
        var world = CreateTestWorld();
        var original = world.CreateEntity("Tagged");
        original.AddTag("Enemy").AddTag("Boss");
        world.Flush();

        var snapshot = _serializer.CreateSnapshot(original);

        var world2 = CreateTestWorld();
        var restored = _serializer.RestoreEntity(world2, snapshot);
        world2.Flush();

        Assert.True(restored.HasTag("Enemy"));
        Assert.True(restored.HasTag("Boss"));
    }

    [Fact]
    public void RestoreEntity_RestoresIsActive()
    {
        var world = CreateTestWorld();
        var original = world.CreateEntity("Inactive");
        original.IsActive = false;
        world.Flush();

        var snapshot = _serializer.CreateSnapshot(original);

        var world2 = CreateTestWorld();
        var restored = _serializer.RestoreEntity(world2, snapshot);
        world2.Flush();

        Assert.False(restored.IsActive);
    }

    [Fact]
    public void RestoreEntity_OnAdded_ReceivesRestoredPropertyValues()
    {
        var world = CreateTestWorld();
        var original = world.CreateEntity("Spy");
        original.AddComponent<SpyComponent>(c => c.TrackedValue = 77);
        world.Flush();

        var snapshot = _serializer.CreateSnapshot(original);

        var world2 = CreateTestWorld();
        var restored = _serializer.RestoreEntity(world2, snapshot);
        world2.Flush();

        var comp = restored.GetComponent<SpyComponent>();
        Assert.NotNull(comp);
        Assert.Equal(77, comp.ValueSeenInOnAdded);
    }

    #endregion

    #region CreateSnapshot — behavior warning

    [Fact]
    public void CreateSnapshot_WithBehaviors_DoesNotThrow()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("WithBehavior");
        entity.AddBehavior<StubBehavior>();
        world.Flush();

        var exception = Record.Exception(() => _serializer.CreateSnapshot(entity));

        Assert.Null(exception);
    }

    [Fact]
    public void CreateSnapshot_WithBehaviors_StillSerializesComponents()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("WithBehavior");
        entity.AddComponent<TransformComponent>(t => t.LocalPosition = new System.Numerics.Vector2(5f, 10f));
        entity.AddBehavior<StubBehavior>();
        world.Flush();

        var snapshot = _serializer.CreateSnapshot(entity);

        Assert.NotEmpty(snapshot.Components);
    }

    [Fact]
    public void CreateSnapshot_WithBehaviors_BehaviorsNotPersistedInSnapshot()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("WithBehavior");
        entity.AddBehavior<StubBehavior>();
        world.Flush();

        var snapshot = _serializer.CreateSnapshot(entity);

        Assert.Empty(snapshot.Components);
    }

    #endregion

    #region EntitySnapshot.Components — type safety

    [Fact]
    public void EntitySnapshot_Components_IsJsonElementDictionary()
    {
        var snapshot = new EntitySnapshot();
        Assert.IsType<Dictionary<string, System.Text.Json.JsonElement>>(snapshot.Components);
    }

    [Fact]
    public void RestoreEntity_FromManuallyBuiltSnapshot_DoesNotThrowInvalidCast()
    {
        var world = CreateTestWorld();
        var original = world.CreateEntity("Manual");
        original.AddComponent<TransformComponent>(t => t.LocalPosition = new System.Numerics.Vector2(1f, 2f));
        world.Flush();

        var json = _serializer.Options;
        var snapshot = _serializer.CreateSnapshot(original);

        var world2 = CreateTestWorld();
        var exception = Record.Exception(() =>
        {
            var restored = _serializer.RestoreEntity(world2, snapshot);
            world2.Flush();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void CreateSnapshot_ComponentsValuesAreJsonElements()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("TypeCheck");
        entity.AddComponent<TransformComponent>();
        world.Flush();

        var snapshot = _serializer.CreateSnapshot(entity);

        Assert.NotEmpty(snapshot.Components);
        foreach (var value in snapshot.Components.Values)
            Assert.IsType<System.Text.Json.JsonElement>(value);
    }

    #endregion

    #region FindComponentType — resilience

    [Fact]
    public void RestoreEntity_WithUnknownComponentType_SkipsComponentWithoutThrowing()
    {
        var world = CreateTestWorld();
        var snapshot = new EntitySnapshot
        {
            Name = "Ghost",
            IsActive = true
        };
        snapshot.Components["Brine2D.Tests.DoesNotExist.FakeComponent"] =
            System.Text.Json.JsonDocument.Parse("{}").RootElement;

        var exception = Record.Exception(() =>
        {
            var entity = _serializer.RestoreEntity(world, snapshot);
            world.Flush();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void RestoreEntity_WithUnknownComponentType_EntityIsStillCreated()
    {
        var world = CreateTestWorld();
        var snapshot = new EntitySnapshot { Name = "Partial" };
        snapshot.Components["Brine2D.Tests.DoesNotExist.FakeComponent"] =
            System.Text.Json.JsonDocument.Parse("{}").RootElement;

        var entity = _serializer.RestoreEntity(world, snapshot);
        world.Flush();

        Assert.NotNull(entity);
        Assert.Equal("Partial", entity.Name);
    }

    #endregion

    #region Snapshot — Id and ParentId fields

    [Fact]
    public void CreateSnapshot_RecordsEntityId()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("IdCheck");
        world.Flush();

        var snapshot = _serializer.CreateSnapshot(entity);

        Assert.Equal(entity.Id, snapshot.Id);
    }

    [Fact]
    public void CreateSnapshot_RecordsZeroParentId_WhenRootEntity()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Root");
        world.Flush();

        var snapshot = _serializer.CreateSnapshot(entity);

        Assert.Equal(0L, snapshot.ParentId);
    }

    [Fact]
    public void CreateSnapshot_RecordsParentId_WhenChildEntity()
    {
        var world = CreateTestWorld();
        var parent = world.CreateEntity("Parent");
        var child = world.CreateEntity("Child");
        child.SetParent(parent);
        world.Flush();

        var snapshot = _serializer.CreateSnapshot(child);

        Assert.Equal(parent.Id, snapshot.ParentId);
    }

    #endregion

    #region RestoreWorldFromSnapshot — hierarchy

    [Fact]
    public void RestoreWorldFromSnapshot_RestoresParentChildRelationship()
    {
        var world = CreateTestWorld();
        var parent = world.CreateEntity("Parent");
        var child = world.CreateEntity("Child");
        child.SetParent(parent);
        world.Flush();

        var worldSnapshot = _serializer.CreateWorldSnapshot(world);

        var world2 = CreateTestWorld();
        _serializer.RestoreWorldFromSnapshot(world2, worldSnapshot);
        world2.Flush();

        var restoredChild = world2.GetEntityByName("Child");
        var restoredParent = world2.GetEntityByName("Parent");
        Assert.NotNull(restoredChild);
        Assert.NotNull(restoredParent);
        Assert.Equal(restoredParent, restoredChild!.Parent);
    }

    [Fact]
    public void RestoreWorldFromSnapshot_RootEntitiesHaveNoParent()
    {
        var world = CreateTestWorld();
        world.CreateEntity("RootA");
        world.CreateEntity("RootB");
        world.Flush();

        var worldSnapshot = _serializer.CreateWorldSnapshot(world);

        var world2 = CreateTestWorld();
        _serializer.RestoreWorldFromSnapshot(world2, worldSnapshot);
        world2.Flush();

        foreach (var entity in world2.Entities)
            Assert.Null(entity.Parent);
    }

    [Fact]
    public void RestoreWorldFromSnapshot_RestoresMultiLevelHierarchy()
    {
        var world = CreateTestWorld();
        var grandparent = world.CreateEntity("Grandparent");
        var parent = world.CreateEntity("Parent");
        var child = world.CreateEntity("Child");
        parent.SetParent(grandparent);
        child.SetParent(parent);
        world.Flush();

        var worldSnapshot = _serializer.CreateWorldSnapshot(world);

        var world2 = CreateTestWorld();
        _serializer.RestoreWorldFromSnapshot(world2, worldSnapshot);
        world2.Flush();

        var restoredChild = world2.GetEntityByName("Child");
        var restoredParent = world2.GetEntityByName("Parent");
        var restoredGrandparent = world2.GetEntityByName("Grandparent");
        Assert.Equal(restoredParent, restoredChild!.Parent);
        Assert.Equal(restoredGrandparent, restoredParent!.Parent);
    }

    [Fact]
    public void RestoreWorldFromSnapshot_RestoredEntitiesHaveFreshIds()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("IdRemap");
        world.Flush();
        var originalId = entity.Id;

        var worldSnapshot = _serializer.CreateWorldSnapshot(world);

        var world2 = CreateTestWorld();
        _serializer.RestoreWorldFromSnapshot(world2, worldSnapshot);
        world2.Flush();

        var restored = world2.GetEntityByName("IdRemap");
        Assert.NotNull(restored);
        Assert.NotEqual(originalId, restored!.Id);
    }

    #endregion

    #region Helpers

    private sealed class StubUpdateSystem : UpdateSystemBase
    {
        public override void Update(IEntityWorld world, GameTime gameTime) { }
    }

    private sealed class StubBehavior : Behavior { }

    private sealed class SpyComponent : Component
    {
        public int TrackedValue { get; set; }
        public int ValueSeenInOnAdded { get; private set; }

        protected internal override void OnAdded() => ValueSeenInOnAdded = TrackedValue;
    }

    #endregion
}

public class ComponentTypeRegistryTests : TestBase
{
    [Fact]
    public void IsRegistered_ReturnsFalse_WhenTypeNotRegistered()
    {
        var registry = new ComponentTypeRegistry();
        Assert.False(registry.IsRegistered<TransformComponent>());
    }

    [Fact]
    public void IsRegistered_ReturnsTrue_AfterRegisterWithReflection()
    {
        var registry = new ComponentTypeRegistry();
#pragma warning disable IL2026, IL3050
        registry.Register<TransformComponent>();
#pragma warning restore IL2026, IL3050
        Assert.True(registry.IsRegistered<TransformComponent>());
    }

    [Fact]
    public void TrySerialize_ReturnsFalse_ForUnregisteredType()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddComponent<TransformComponent>();
        world.Flush();

        var registry = new ComponentTypeRegistry();
        var component = entity.GetComponent<TransformComponent>()!;

        Assert.False(registry.TrySerialize(component, out _));
    }

    [Fact]
    public void TrySerialize_ReturnsTrue_ForRegisteredType()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(1f, 2f));
        world.Flush();

        var registry = new ComponentTypeRegistry();
#pragma warning disable IL2026, IL3050
        registry.Register<TransformComponent>();
#pragma warning restore IL2026, IL3050

        var component = entity.GetComponent<TransformComponent>()!;
        Assert.True(registry.TrySerialize(component, out var element));
        Assert.NotEqual(default, element);
    }

    [Fact]
    public void TryDeserializeAndAttach_ReturnsFalse_ForUnregisteredType()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        world.Flush();

        var registry = new ComponentTypeRegistry();
        var data = System.Text.Json.JsonDocument.Parse("{}").RootElement;

        Assert.False(registry.TryDeserializeAndAttach("Brine2D.ECS.Components.TransformComponent", data, entity));
    }

    [Fact]
    public void TryDeserializeAndAttach_AttachesComponent_ForRegisteredType()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        world.Flush();

        var registry = new ComponentTypeRegistry();
#pragma warning disable IL2026, IL3050
        registry.Register<TransformComponent>();
#pragma warning restore IL2026, IL3050

        var json = """{"localPosition":{"x":5,"y":10},"localRotation":0,"localScale":{"x":1,"y":1}}""";
        var data = System.Text.Json.JsonDocument.Parse(json).RootElement;

        var attached = registry.TryDeserializeAndAttach("Brine2D.ECS.Components.TransformComponent", data, entity);

        Assert.True(attached);
        Assert.True(entity.HasComponent<TransformComponent>());
    }

    [Fact]
    public void RegisterBrineComponents_RegistersAtLeastCoreComponents()
    {
        var registry = new ComponentTypeRegistry();
#pragma warning disable IL2026, IL3050
        registry.RegisterBrineComponents();
#pragma warning restore IL2026, IL3050

        Assert.True(registry.IsRegistered<TransformComponent>());
        Assert.True(registry.IsRegistered<PhysicsBodyComponent>());
        Assert.True(registry.IsRegistered<KinematicCharacterBody>());
    }

    [Fact]
    public void RegisterBrineComponents_CountIsGreaterThanZero()
    {
        var registry = new ComponentTypeRegistry();
#pragma warning disable IL2026, IL3050
        var count = registry.RegisterBrineComponents();
#pragma warning restore IL2026, IL3050

        Assert.True(count > 0);
        Assert.Equal(count, registry.Count);
    }

    [Fact]
    public void RegisterAllComponents_RegistersTypesFromAssembly()
    {
        var registry = new ComponentTypeRegistry();
#pragma warning disable IL2026, IL3050
        registry.RegisterAllComponents(typeof(TransformComponent).Assembly);
#pragma warning restore IL2026, IL3050

        Assert.True(registry.IsRegistered<TransformComponent>());
        Assert.True(registry.IsRegistered<SpriteComponent>());
    }

    [Fact]
    public void RegisterAllComponents_SkipsAbstractTypes()
    {
        var registry = new ComponentTypeRegistry();
#pragma warning disable IL2026, IL3050
        registry.RegisterAllComponents(typeof(TransformComponent).Assembly);
#pragma warning restore IL2026, IL3050

        Assert.False(registry.IsRegistered("Brine2D.ECS.Components.Joints.JointComponent"));
        Assert.False(registry.IsRegistered("Brine2D.Systems.Rendering.ShapeComponent"));
    }

    [Fact]
    public void RegisterAllComponents_ConcreteJointSubclassesAreRegistered()
    {
        var registry = new ComponentTypeRegistry();
#pragma warning disable IL2026, IL3050
        registry.RegisterAllComponents(typeof(TransformComponent).Assembly);
#pragma warning restore IL2026, IL3050

        Assert.True(registry.IsRegistered<RevoluteJointComponent>());
        Assert.True(registry.IsRegistered<DistanceJointComponent>());
    }

    [Fact]
    public void RegisterAllComponents_ReturnedCountMatchesRegistryCount()
    {
        var registry = new ComponentTypeRegistry();
#pragma warning disable IL2026, IL3050
        var returned = registry.RegisterAllComponents(typeof(TransformComponent).Assembly);
#pragma warning restore IL2026, IL3050

        Assert.Equal(returned, registry.Count);
    }

    [Fact]
    public void AotSerializer_WithRegisterBrineComponents_CanRoundTripTransform()
    {
        var registry = new ComponentTypeRegistry();
#pragma warning disable IL2026, IL3050
        registry.RegisterBrineComponents();
#pragma warning restore IL2026, IL3050

        var serializer = new AotEntitySerializer(registry);
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(11f, 22f));
        world.Flush();

        var snapshot = serializer.CreateSnapshot(entity);
        var world2 = CreateTestWorld();
        var restored = serializer.RestoreEntity(world2, snapshot);
        world2.Flush();

        Assert.Equal(new Vector2(11f, 22f), restored.GetComponent<TransformComponent>()!.LocalPosition);
    }
}

public class AotEntitySerializerTests : TestBase
{
    private static ComponentTypeRegistry BuildRegistry()
    {
        var registry = new ComponentTypeRegistry();
#pragma warning disable IL2026, IL3050
        registry.Register<TransformComponent>();
#pragma warning restore IL2026, IL3050
        return registry;
    }

    private AotEntitySerializer BuildSerializer() => new(BuildRegistry());

    [Fact]
    public void CreateSnapshot_StoresName()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Hero");
        world.Flush();

        var snap = BuildSerializer().CreateSnapshot(entity);

        Assert.Equal("Hero", snap.Name);
    }

    [Fact]
    public void CreateSnapshot_StoresIsActive()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Inactive");
        entity.IsActive = false;
        world.Flush();

        var snap = BuildSerializer().CreateSnapshot(entity);

        Assert.False(snap.IsActive);
    }

    [Fact]
    public void CreateSnapshot_SkipsUnregisteredComponent_WithoutThrowing()
    {
        var registry = new ComponentTypeRegistry();
        var serializer = new AotEntitySerializer(registry);

        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddComponent<TransformComponent>();
        world.Flush();

        var snap = serializer.CreateSnapshot(entity);

        Assert.Empty(snap.Components);
    }

    [Fact]
    public void CreateSnapshot_SerializesRegisteredComponent()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(3f, 7f));
        world.Flush();

        var snap = BuildSerializer().CreateSnapshot(entity);

        Assert.NotEmpty(snap.Components);
    }

    [Fact]
    public void RestoreEntity_RestoresComponentPropertyValues()
    {
        var world = CreateTestWorld();
        var original = world.CreateEntity("Player");
        original.AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(42f, 99f));
        world.Flush();

        var serializer = BuildSerializer();
        var snapshot = serializer.CreateSnapshot(original);

        var world2 = CreateTestWorld();
        var restored = serializer.RestoreEntity(world2, snapshot);
        world2.Flush();

        var t = restored.GetComponent<TransformComponent>();
        Assert.NotNull(t);
        Assert.Equal(new Vector2(42f, 99f), t.LocalPosition);
    }

    [Fact]
    public void RestoreEntity_SkipsUnregisteredComponent_WithoutThrowing()
    {
        var world = CreateTestWorld();
        var snapshot = new EntitySnapshot { Name = "Ghost" };
        snapshot.Components["Brine2D.ECS.Components.TransformComponent"] =
            System.Text.Json.JsonDocument.Parse("{}").RootElement;

        var registry = new ComponentTypeRegistry();
        var serializer = new AotEntitySerializer(registry);

        var exception = Record.Exception(() =>
        {
            var entity = serializer.RestoreEntity(world, snapshot);
            world.Flush();
        });

        Assert.Null(exception);
    }

    [Fact]
    public void RestoreEntity_RestoresTags()
    {
        var world = CreateTestWorld();
        var original = world.CreateEntity("Tagged");
        original.AddTag("Enemy").AddTag("Boss");
        world.Flush();

        var serializer = BuildSerializer();
        var snapshot = serializer.CreateSnapshot(original);

        var world2 = CreateTestWorld();
        var restored = serializer.RestoreEntity(world2, snapshot);
        world2.Flush();

        Assert.True(restored.HasTag("Enemy"));
        Assert.True(restored.HasTag("Boss"));
    }

    [Fact]
    public void RestoreWorldFromSnapshot_DoesNotDestroyRegisteredSystems()
    {
        var world = CreateTestWorld();
        world.AddSystem<AotStubUpdateSystem>();
        world.Flush();

        var serializer = BuildSerializer();
        var snapshot = serializer.CreateWorldSnapshot(world);
        serializer.RestoreWorldFromSnapshot(world, snapshot);
        world.Flush();

        Assert.NotNull(world.GetSystem<AotStubUpdateSystem>());
    }

    [Fact]
    public void RestoreWorldFromSnapshot_ClearsEntitiesBetweenRestores()
    {
        var world = CreateTestWorld();
        world.CreateEntity("A").AddComponent<TransformComponent>();
        world.Flush();

        var serializer = BuildSerializer();
        var snapshot = serializer.CreateWorldSnapshot(world);

        world.CreateEntity("B").AddComponent<TransformComponent>();
        world.Flush();

        serializer.RestoreWorldFromSnapshot(world, snapshot);
        world.Flush();

        Assert.Single(world.Entities);
        Assert.Equal("A", world.Entities[0].Name);
    }

    [Fact]
    public void RestoreWorldFromSnapshot_RestoresParentChildRelationship()
    {
        var world = CreateTestWorld();
        var parent = world.CreateEntity("Parent");
        var child = world.CreateEntity("Child");
        child.SetParent(parent);
        world.Flush();

        var serializer = BuildSerializer();
        var worldSnapshot = serializer.CreateWorldSnapshot(world);

        var world2 = CreateTestWorld();
        serializer.RestoreWorldFromSnapshot(world2, worldSnapshot);
        world2.Flush();

        var restoredChild = world2.GetEntityByName("Child");
        var restoredParent = world2.GetEntityByName("Parent");
        Assert.NotNull(restoredChild);
        Assert.NotNull(restoredParent);
        Assert.Equal(restoredParent, restoredChild!.Parent);
    }

    [Fact]
    public void RestoreWorldFromSnapshot_RestoredEntitiesHaveFreshIds()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("IdRemap");
        world.Flush();
        var originalId = entity.Id;

        var serializer = BuildSerializer();
        var worldSnapshot = serializer.CreateWorldSnapshot(world);

        var world2 = CreateTestWorld();
        serializer.RestoreWorldFromSnapshot(world2, worldSnapshot);
        world2.Flush();

        var restored = world2.GetEntityByName("IdRemap");
        Assert.NotNull(restored);
        Assert.NotEqual(originalId, restored!.Id);
    }

    [Fact]
    public async Task SaveAndLoadWorldAsync_RoundTripsEntities()
    {
        var world = CreateTestWorld();
        world.CreateEntity("Alpha").AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(1f, 2f));
        world.CreateEntity("Beta");
        world.Flush();

        var serializer = BuildSerializer();
        var path = Path.Combine(Path.GetTempPath(), $"brine2d_test_{Guid.NewGuid():N}.json");

        try
        {
            await serializer.SaveWorldAsync(world, path);

            var world2 = CreateTestWorld();
            await serializer.LoadAndRestoreWorldAsync(world2, path);
            world2.Flush();

            Assert.Equal(2, world2.Entities.Count);
            var alpha = world2.GetEntityByName("Alpha");
            Assert.NotNull(alpha);
            Assert.Equal(new Vector2(1f, 2f), alpha!.GetComponent<TransformComponent>()!.LocalPosition);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    private sealed class AotStubUpdateSystem : UpdateSystemBase
    {
        public override void Update(IEntityWorld world, GameTime gameTime) { }
    }
}
