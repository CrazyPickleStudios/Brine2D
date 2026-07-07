using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Rendering;
using Xunit;

namespace Brine2D.Tests.ECS;

public class EntityPrefabTests : TestBase
{
    #region AddComponent

    [Fact]
    public void Instantiate_AppliesComponentConfigurations()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("Enemy")
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(10, 20));

        var entity = prefab.Instantiate(world);
        world.Flush();

        var transform = entity.GetComponent<TransformComponent>();
        Assert.NotNull(transform);
        Assert.Equal(new Vector2(10, 20), transform.LocalPosition);
    }

    [Fact]
    public void Instantiate_ComponentConfigure_RunsBeforeOnAdded()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("Test")
            .AddComponent<RecordingComponent>(c => c.ValueSetByPrefab = 99);

        var entity = prefab.Instantiate(world);
        world.Flush();

        var comp = entity.GetComponent<RecordingComponent>()!;
        Assert.Equal(99, comp.ValueAtOnAdded);
    }

    [Fact]
    public void Instantiate_AppliesTagsToEntity()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("Enemy");
        prefab.Tags.Add("Enemy");
        prefab.Tags.Add("Hostile");

        var entity = prefab.Instantiate(world);

        Assert.True(entity.HasTag("Enemy"));
        Assert.True(entity.HasTag("Hostile"));
    }

    [Fact]
    public void AddTag_FluentChain_AppliesTagToInstantiatedEntity()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("Enemy")
            .AddComponent<TransformComponent>()
            .AddTag("Enemy")
            .AddTag("Hostile");

        var entity = prefab.Instantiate(world);

        Assert.True(entity.HasTag("Enemy"));
        Assert.True(entity.HasTag("Hostile"));
    }

    [Fact]
    public void AddTag_DuplicateTag_NotAddedTwice()
    {
        var prefab = new EntityPrefab("Enemy")
            .AddTag("Enemy")
            .AddTag("Enemy");

        Assert.Single(prefab.Tags);
    }

    [Fact]
    public void AddTag_NullOrWhitespace_Ignored()
    {
        var prefab = new EntityPrefab("Enemy")
            .AddTag("")
            .AddTag("   ")
            .AddTag(null!);

        Assert.Empty(prefab.Tags);
    }

    [Fact]
    public void AddTags_FluentChain_AppliesAllTagsToInstantiatedEntity()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("Enemy")
            .AddComponent<TransformComponent>()
            .AddTags("Enemy", "Hostile", "Boss");

        var entity = prefab.Instantiate(world);

        Assert.True(entity.HasTag("Enemy"));
        Assert.True(entity.HasTag("Hostile"));
        Assert.True(entity.HasTag("Boss"));
    }

    [Fact]
    public void AddTags_Duplicates_NotAddedTwice()
    {
        var prefab = new EntityPrefab("Enemy")
            .AddTags("Enemy", "Enemy", "Hostile");

        Assert.Equal(2, prefab.Tags.Count);
    }

    [Fact]
    public void AddTags_ReturnsEntityPrefabForChaining()
    {
        var prefab = new EntityPrefab("Enemy");
        var result = prefab.AddTags("Tag1", "Tag2");

        Assert.Same(prefab, result);
    }

    [Fact]
    public void AddTag_ReturnsEntityPrefabForChaining()
    {
        var prefab = new EntityPrefab("Enemy");
        var result = prefab.AddTag("Tag1");

        Assert.Same(prefab, result);
    }

    [Fact]
    public void Instantiate_WithPosition_OverridesTransformPosition()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("Enemy")
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0, 0));

        var entity = prefab.Instantiate(world, new Vector2(50, 100));

        Assert.Equal(new Vector2(50, 100), entity.GetComponent<TransformComponent>()!.LocalPosition);
    }

    [Fact]
    public void Instantiate_WithPosition_NoTransformComponent_DoesNotThrow()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("NoTransform");

        var exception = Record.Exception(() => prefab.Instantiate(world, new Vector2(50, 100)));

        Assert.Null(exception);
    }

    [Fact]
    public void Instantiate_WithPosition_NoTransformComponent_EntityHasNoTransform()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("NoTransform");

        var entity = prefab.Instantiate(world, new Vector2(50, 100));

        Assert.Null(entity.GetComponent<TransformComponent>());
    }

    [Fact]
    public void Instantiate_WithNameOverride_UsesProvidedName()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("Enemy");

        var entity = prefab.Instantiate(world, name: "Enemy_01");

        Assert.Equal("Enemy_01", entity.Name);
    }

    [Fact]
    public void Instantiate_WithoutNameOverride_UsesPrefabName()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("Enemy");

        var entity = prefab.Instantiate(world);

        Assert.Equal("Enemy", entity.Name);
    }

    [Fact]
    public void Instantiate_MultipleInstancesWithDistinctNames_CanBeFoundByName()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("Coin").AddComponent<TransformComponent>();
        world.Flush();

        var e1 = prefab.Instantiate(world, name: "Coin_01");
        var e2 = prefab.Instantiate(world, name: "Coin_02");
        world.Flush();

        Assert.Equal(e1, world.GetEntityByName("Coin_01"));
        Assert.Equal(e2, world.GetEntityByName("Coin_02"));
    }

    [Fact]
    public void Instantiate_MultipleEntities_AreIndependent()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("Coin").AddComponent<TransformComponent>();

        var e1 = prefab.Instantiate(world, new Vector2(0, 0));
        var e2 = prefab.Instantiate(world, new Vector2(100, 0));
        world.Flush();

        Assert.NotEqual(e1.Id, e2.Id);
        Assert.Equal(new Vector2(0, 0), e1.GetComponent<TransformComponent>()!.LocalPosition);
        Assert.Equal(new Vector2(100, 0), e2.GetComponent<TransformComponent>()!.LocalPosition);
    }

    #endregion

    #region AddBehavior

    [Fact]
    public void AddBehavior_AttachesBehaviorOnInstantiate()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("Player").AddBehavior<TestBehavior>();

        var entity = prefab.Instantiate(world);

        Assert.True(entity.HasBehavior<TestBehavior>());
    }

    [Fact]
    public void AddBehavior_WithConfigure_AppliesConfigurationBeforeAttach()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("Enemy")
            .AddBehavior<ConfigurableTestBehavior>(b => b.Speed = 42f);

        var entity = prefab.Instantiate(world);

        var behavior = entity.GetBehavior<ConfigurableTestBehavior>();
        Assert.NotNull(behavior);
        Assert.Equal(42f, behavior.Speed);
    }

    [Fact]
    public void AddBehavior_WithoutConfigure_AttachesBehaviorWithDefaults()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("Enemy").AddBehavior<ConfigurableTestBehavior>();

        var entity = prefab.Instantiate(world);

        var behavior = entity.GetBehavior<ConfigurableTestBehavior>();
        Assert.NotNull(behavior);
        Assert.Equal(0f, behavior.Speed);
    }

    [Fact]
    public void AddBehavior_MultipleInstantiations_BehaviorsAreIndependentInstances()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("Enemy").AddBehavior<ConfigurableTestBehavior>(b => b.Speed = 10f);

        var e1 = prefab.Instantiate(world);
        var e2 = prefab.Instantiate(world);

        var b1 = e1.GetBehavior<ConfigurableTestBehavior>()!;
        var b2 = e2.GetBehavior<ConfigurableTestBehavior>()!;

        b1.Speed = 99f;

        Assert.Equal(10f, b2.Speed);
    }

    [Fact]
    public void Instantiate_ComponentsAndBehaviors_BothApplied()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("Player")
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(1, 2))
            .AddBehavior<TestBehavior>();

        var entity = prefab.Instantiate(world);
        world.Flush();

        Assert.NotNull(entity.GetComponent<TransformComponent>());
        Assert.True(entity.HasBehavior<TestBehavior>());
    }

    #endregion

    #region AddChildPrefab

    [Fact]
    public void AddChildPrefab_InstantiatesChildParentedToRoot()
    {
        var world = CreateTestWorld();
        var childPrefab = new EntityPrefab("Shadow").AddComponent<TransformComponent>();
        var rootPrefab = new EntityPrefab("Enemy")
            .AddComponent<TransformComponent>()
            .AddChildPrefab(childPrefab);

        var root = rootPrefab.Instantiate(world);
        world.Flush();

        Assert.Single(root.Children);
        Assert.Equal(root, root.Children[0].Parent);
    }

    [Fact]
    public void AddChildPrefab_ChildHasCorrectName()
    {
        var world = CreateTestWorld();
        var childPrefab = new EntityPrefab("Weapon");
        var rootPrefab = new EntityPrefab("Player").AddChildPrefab(childPrefab);

        var root = rootPrefab.Instantiate(world);

        Assert.Equal("Weapon", root.Children[0].Name);
    }

    [Fact]
    public void AddChildPrefab_WithConfigure_AppliesOverrideToChild()
    {
        var world = CreateTestWorld();
        var childPrefab = new EntityPrefab("Weapon").AddComponent<TransformComponent>();
        var rootPrefab = new EntityPrefab("Player")
            .AddChildPrefab(childPrefab, child =>
                child.GetComponent<TransformComponent>()!.LocalPosition = new Vector2(16, 0));

        var root = rootPrefab.Instantiate(world);

        Assert.Equal(new Vector2(16, 0), root.Children[0].GetComponent<TransformComponent>()!.LocalPosition);
    }

    [Fact]
    public void AddChildPrefab_MultipleChildren_AllParented()
    {
        var world = CreateTestWorld();
        var rootPrefab = new EntityPrefab("Enemy")
            .AddChildPrefab(new EntityPrefab("Shadow"))
            .AddChildPrefab(new EntityPrefab("Healthbar"));

        var root = rootPrefab.Instantiate(world);

        Assert.Equal(2, root.Children.Count);
        Assert.All(root.Children, child => Assert.Equal(root, child.Parent));
    }

    [Fact]
    public void AddChildPrefab_MultipleInstantiations_ChildrenAreIndependentInstances()
    {
        var world = CreateTestWorld();
        var childPrefab = new EntityPrefab("Shadow").AddComponent<TransformComponent>();
        var rootPrefab = new EntityPrefab("Enemy").AddChildPrefab(childPrefab);

        var root1 = rootPrefab.Instantiate(world);
        var root2 = rootPrefab.Instantiate(world);

        Assert.NotEqual(root1.Children[0], root2.Children[0]);
    }

    [Fact]
    public void AddChildPrefab_NullPrefab_Throws()
    {
        var prefab = new EntityPrefab("Enemy");

        Assert.Throws<ArgumentNullException>(() => prefab.AddChildPrefab(null!));
    }

    [Fact]
    public void AddChildPrefab_SelfReference_Throws()
    {
        var prefab = new EntityPrefab("Enemy");

        Assert.Throws<ArgumentException>(() => prefab.AddChildPrefab(prefab));
    }

    [Fact]
    public void AddChildPrefab_DirectCycle_Throws()
    {
        var a = new EntityPrefab("A");
        var b = new EntityPrefab("B");
        a.AddChildPrefab(b);

        Assert.Throws<ArgumentException>(() => b.AddChildPrefab(a));
    }

    [Fact]
    public void AddChildPrefab_TransitiveCycle_Throws()
    {
        var a = new EntityPrefab("A");
        var b = new EntityPrefab("B");
        var c = new EntityPrefab("C");
        a.AddChildPrefab(b);
        b.AddChildPrefab(c);

        Assert.Throws<ArgumentException>(() => c.AddChildPrefab(a));
    }

    #endregion

    #region Constructor Validation

    [Fact]
    public void Constructor_EmptyName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new EntityPrefab(""));
    }

    [Fact]
    public void Constructor_WhitespaceName_Throws()
    {
        Assert.Throws<ArgumentException>(() => new EntityPrefab("   "));
    }

    [Fact]
    public void Constructor_NullName_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new EntityPrefab(null!));
    }

    #endregion

    #region PrefabLibrary

    [Fact]
    public void PrefabLibrary_Register_And_Get_Works()
    {
        var library = new PrefabLibrary();
        var prefab = new EntityPrefab("Coin");

        library.Register(prefab);

        Assert.True(library.Has("Coin"));
        Assert.Equal(prefab, library.Get("Coin"));
    }

    [Fact]
    public void PrefabLibrary_Remove_RemovesPrefab()
    {
        var library = new PrefabLibrary();
        library.Register(new EntityPrefab("Coin"));

        library.Remove("Coin");

        Assert.False(library.Has("Coin"));
    }

    [Fact]
    public void PrefabLibrary_GetAll_ReturnsAllRegistered()
    {
        var library = new PrefabLibrary();
        library.Register(new EntityPrefab("Coin"));
        library.Register(new EntityPrefab("Enemy"));

        Assert.Equal(2, library.GetAll().Count);
    }

    [Fact]
    public void PrefabLibrary_Clear_RemovesAll()
    {
        var library = new PrefabLibrary();
        library.Register(new EntityPrefab("Coin"));
        library.Clear();

        Assert.Empty(library.GetAll());
    }

    [Fact]
    public void PrefabLibrary_Instantiate_CreatesEntity()
    {
        var world = CreateTestWorld();
        var library = new PrefabLibrary();
        library.Register(new EntityPrefab("Coin").AddComponent<TransformComponent>());

        var entity = library.Instantiate("Coin", world);

        Assert.NotNull(entity);
        Assert.NotNull(entity.GetComponent<TransformComponent>());
    }

    [Fact]
    public void PrefabLibrary_Instantiate_WithPosition_SetsPosition()
    {
        var world = CreateTestWorld();
        var library = new PrefabLibrary();
        library.Register(new EntityPrefab("Coin").AddComponent<TransformComponent>());

        var entity = library.Instantiate("Coin", world, new Vector2(10, 20));

        Assert.Equal(new Vector2(10, 20), entity.GetComponent<TransformComponent>()!.Position);
    }

    [Fact]
    public void PrefabLibrary_Instantiate_UnknownName_Throws()
    {
        var world = CreateTestWorld();
        var library = new PrefabLibrary();

        Assert.Throws<KeyNotFoundException>(() => library.Instantiate("Missing", world));
    }

    [Fact]
    public void PrefabLibrary_TryInstantiate_ReturnsTrueAndEntity()
    {
        var world = CreateTestWorld();
        var library = new PrefabLibrary();
        library.Register(new EntityPrefab("Coin").AddComponent<TransformComponent>());

        var result = library.TryInstantiate("Coin", world, out var entity);

        Assert.True(result);
        Assert.NotNull(entity);
    }

    [Fact]
    public void PrefabLibrary_TryInstantiate_UnknownName_ReturnsFalse()
    {
        var world = CreateTestWorld();
        var library = new PrefabLibrary();

        var result = library.TryInstantiate("Missing", world, out var entity);

        Assert.False(result);
        Assert.Null(entity);
    }

    [Fact]
    public void PrefabLibrary_Instantiate_WithEntityNameOverride_UsesProvidedName()
    {
        var world = CreateTestWorld();
        var library = new PrefabLibrary();
        library.Register(new EntityPrefab("Coin").AddComponent<TransformComponent>());

        var entity = library.Instantiate("Coin", world, entityName: "Coin_01");

        Assert.Equal("Coin_01", entity.Name);
    }

    [Fact]
    public void PrefabLibrary_TryInstantiate_WithEntityNameOverride_UsesProvidedName()
    {
        var world = CreateTestWorld();
        var library = new PrefabLibrary();
        library.Register(new EntityPrefab("Coin").AddComponent<TransformComponent>());

        library.TryInstantiate("Coin", world, out var entity, entityName: "Coin_99");

        Assert.Equal("Coin_99", entity!.Name);
    }

    [Fact]
    public void PrefabLibrary_Instantiate_WithoutNameOverride_UsesPrefabName()
    {
        var world = CreateTestWorld();
        var library = new PrefabLibrary();
        library.Register(new EntityPrefab("Coin").AddComponent<TransformComponent>());

        var entity = library.Instantiate("Coin", world);

        Assert.Equal("Coin", entity.Name);
    }

    #endregion

    #region Instantiate Scale

    [Fact]
    public void Instantiate_WithScale_AppliesScaleToTransform()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("Enemy").AddComponent<TransformComponent>();
        var expectedScale = new Vector2(2f, 3f);

        var entity = prefab.Instantiate(world, scale: expectedScale);

        var transform = entity.GetComponent<TransformComponent>()!;
        Assert.Equal(expectedScale, transform.LocalScale);
    }

    [Fact]
    public void Instantiate_WithPositionRotationAndScale_AppliesAll()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("Enemy").AddComponent<TransformComponent>();

        var entity = prefab.Instantiate(world,
            position: new Vector2(10, 20),
            rotation: 1.5f,
            scale: new Vector2(0.5f, 0.5f));

        var transform = entity.GetComponent<TransformComponent>()!;
        Assert.Equal(new Vector2(10, 20), transform.LocalPosition);
        Assert.Equal(1.5f, transform.LocalRotation, precision: 4);
        Assert.Equal(new Vector2(0.5f, 0.5f), transform.LocalScale);
    }

    [Fact]
    public void Instantiate_WithScale_NoTransformComponent_DoesNotThrow()
    {
        var world = CreateTestWorld();
        var prefab = new EntityPrefab("NoTransform");

        var ex = Record.Exception(() => prefab.Instantiate(world, scale: new Vector2(2f, 2f)));

        Assert.Null(ex);
    }

    [Fact]
    public void PrefabLibrary_Instantiate_WithScale_AppliesScaleToTransform()
    {
        var world = CreateTestWorld();
        var library = new PrefabLibrary();
        library.Register(new EntityPrefab("Enemy").AddComponent<TransformComponent>());
        var expectedScale = new Vector2(3f, 3f);

        var entity = library.Instantiate("Enemy", world, scale: expectedScale);

        var transform = entity.GetComponent<TransformComponent>()!;
        Assert.Equal(expectedScale, transform.LocalScale);
    }

    [Fact]
    public void PrefabLibrary_TryInstantiate_WithScale_AppliesScaleToTransform()
    {
        var world = CreateTestWorld();
        var library = new PrefabLibrary();
        library.Register(new EntityPrefab("Enemy").AddComponent<TransformComponent>());
        var expectedScale = new Vector2(0.5f, 0.5f);

        library.TryInstantiate("Enemy", world, out var entity, scale: expectedScale);

        var transform = entity!.GetComponent<TransformComponent>()!;
        Assert.Equal(expectedScale, transform.LocalScale);
    }

    #endregion

    #region Test Helper Classes

    private class TestBehavior : Behavior { }

    private class ConfigurableTestBehavior : Behavior
    {
        public float Speed { get; set; }
    }

    private class RecordingComponent : Component
    {
        public int ValueSetByPrefab { get; set; }
        public int ValueAtOnAdded { get; private set; }

        protected internal override void OnAdded() => ValueAtOnAdded = ValueSetByPrefab;
    }

    #endregion
}
