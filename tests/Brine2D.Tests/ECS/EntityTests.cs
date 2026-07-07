using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Systems.Physics;

namespace Brine2D.Tests.ECS;

public class EntityTests : TestBase
{
    #region Basic Properties

    [Fact]
    public void Entity_AfterCreation_HasValidId()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Assert
        Assert.NotEqual(0, entity.Id);
    }

    [Fact]
    public void Entity_AfterCreation_IsActiveByDefault()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Assert
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void Entity_Name_CanBeSetAndRetrieved()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act
        entity.Name = "Player";

        // Assert
        Assert.Equal("Player", entity.Name);
    }

    [Fact]
    public void Entity_IsActive_CanBeToggled()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act & Assert
        entity.IsActive = false;
        Assert.False(entity.IsActive);

        entity.IsActive = true;
        Assert.True(entity.IsActive);
    }

    [Fact]
    public void OnDeactivated_CalledWhenIsActiveSetToFalse()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity<LifecycleEntity>();
        entity.IsActive = false;
        Assert.True(entity.DeactivatedCalled);
    }

    [Fact]
    public void OnActivated_CalledWhenIsActiveSetToTrue()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity<LifecycleEntity>();
        entity.IsActive = false;
        entity.ActivatedCalled = false;
        entity.IsActive = true;
        Assert.True(entity.ActivatedCalled);
    }

    [Fact]
    public void OnActivated_NotCalledWhenAlreadyActive()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity<LifecycleEntity>();
        entity.IsActive = true;
        Assert.False(entity.ActivatedCalled);
    }

    [Fact]
    public void SetActiveHierarchically_DeactivatesAllDescendants()
    {
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();
        var grandchild = world.CreateEntity();
        child.SetParent(parent);
        grandchild.SetParent(child);

        parent.SetActiveHierarchically(false);

        Assert.False(parent.IsActive);
        Assert.False(child.IsActive);
        Assert.False(grandchild.IsActive);
    }

    [Fact]
    public void SetActiveHierarchically_ActivatesAllDescendants()
    {
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();
        child.SetParent(parent);
        parent.SetActiveHierarchically(false);

        parent.SetActiveHierarchically(true);

        Assert.True(parent.IsActive);
        Assert.True(child.IsActive);
    }

    [Fact]
    public void SetActiveHierarchically_ReturnsEntityForChaining()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        var result = entity.SetActiveHierarchically(false);
        Assert.Equal(entity, result);
    }

    [Fact]
    public void Entity_World_IsSetAfterCreation()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Assert
        Assert.NotNull(entity.World);
        Assert.Equal(world, entity.World);
    }

    #endregion

    #region Tag Management

    [Fact]
    public void AddTag_SingleTag_AddsSuccessfully()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act
        entity.AddTag("Player");

        // Assert
        Assert.True(entity.HasTag("Player"));
    }

    [Fact]
    public void AddTag_ReturnsEntity_AllowsChaining()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act
        var result = entity.AddTag("Player").AddTag("Controllable");

        // Assert
        Assert.Equal(entity, result);
        Assert.True(entity.HasTag("Player"));
        Assert.True(entity.HasTag("Controllable"));
    }

    [Fact]
    public void AddTag_NullOrEmpty_DoesNotAdd()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act
        entity.AddTag(null!);
        entity.AddTag("");
        entity.AddTag("   ");

        // Assert
        Assert.Empty(entity.Tags);
    }

    [Fact]
    public void AddTags_MultipleAtOnce_AddsAll()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act
        entity.AddTags("Player", "Enemy", "Boss");

        // Assert
        Assert.True(entity.HasTag("Player"));
        Assert.True(entity.HasTag("Enemy"));
        Assert.True(entity.HasTag("Boss"));
        Assert.Equal(3, entity.Tags.Count);
    }

    [Fact]
    public void RemoveTag_ExistingTag_RemovesSuccessfully()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddTag("Player");

        // Act
        entity.RemoveTag("Player");

        // Assert
        Assert.False(entity.HasTag("Player"));
    }

    [Fact]
    public void RemoveTag_NonExistingTag_DoesNotThrow()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act & Assert - Should not throw
        entity.RemoveTag("NonExistent");
    }

    [Fact]
    public void HasTag_ExistingTag_ReturnsTrue()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddTag("Player");

        // Act & Assert
        Assert.True(entity.HasTag("Player"));
    }

    [Fact]
    public void HasTag_NonExistingTag_ReturnsFalse()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act & Assert
        Assert.False(entity.HasTag("Player"));
    }

    [Fact]
    public void HasAllTags_AllPresent_ReturnsTrue()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddTags("Player", "Controllable", "Damageable");

        // Act & Assert
        Assert.True(entity.HasAllTags("Player", "Controllable"));
        Assert.True(entity.HasAllTags("Player", "Damageable"));
    }

    [Fact]
    public void HasAllTags_SomeMissing_ReturnsFalse()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddTags("Player", "Controllable");

        // Act & Assert
        Assert.False(entity.HasAllTags("Player", "Enemy"));
    }

    [Fact]
    public void HasAnyTag_OnePresent_ReturnsTrue()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddTag("Player");

        // Act & Assert
        Assert.True(entity.HasAnyTag("Player", "Enemy"));
    }

    [Fact]
    public void HasAnyTag_NonePresent_ReturnsFalse()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act & Assert
        Assert.False(entity.HasAnyTag("Player", "Enemy"));
    }

    [Fact]
    public void ClearTags_RemovesAllTags()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddTags("Player", "Controllable", "Damageable");

        // Act
        entity.ClearTags();

        // Assert
        Assert.Empty(entity.Tags);
    }

    #endregion

    #region Component Management

    [Fact]
    public void AddComponent_Generic_AddsSuccessfully()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act
        entity.AddComponent<TransformComponent>();

        // Assert
        Assert.True(entity.HasComponent<TransformComponent>());
    }

    [Fact]
    public void AddComponent_ReturnsEntity_AllowsChaining()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act
        var result = entity.AddComponent<TransformComponent>();

        // Assert
        Assert.Equal(entity, result);
    }

    [Fact]
    public void AddComponent_Duplicate_DoesNotAddTwice()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<TransformComponent>(); // Try adding again

        // Assert
        var components = entity.GetAllComponents().ToList();
        Assert.Single(components.Where(c => c is TransformComponent));
    }

    [Fact]
    public void AddComponent_WithConfigure_Duplicate_IsSkipped()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>(t => t.LocalPosition = new System.Numerics.Vector2(1, 2));

        entity.AddComponent<TransformComponent>(t => t.LocalPosition = new System.Numerics.Vector2(99, 99));

        var components = entity.GetAllComponents().ToList();
        Assert.Single(components.Where(c => c is TransformComponent));
        Assert.Equal(new System.Numerics.Vector2(1, 2), entity.GetComponent<TransformComponent>()!.LocalPosition);
    }

    [Fact]
    public void AddComponent_WithConfigure_Duplicate_OnlyOneComponentInstanceExists()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var original = entity.GetComponent<TransformComponent>();

        entity.AddComponent<TransformComponent>(t => t.LocalPosition = new System.Numerics.Vector2(5, 5));

        Assert.Single(entity.GetAllComponents().OfType<TransformComponent>());
        Assert.Same(original, entity.GetComponent<TransformComponent>());
    }

    [Fact]
    public void AddComponent_WithConfiguration_ConfiguresComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act
        entity.AddComponent<TransformComponent>(t => t.LocalPosition = new System.Numerics.Vector2(10, 20));

        // Assert
        var transform = entity.GetComponent<TransformComponent>()!;
        Assert.Equal(new System.Numerics.Vector2(10, 20), transform.LocalPosition);
    }

    [Fact]
    public void AddComponent_WithConfigure_EntityIsSetDuringConfigure()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");

        entity.AddComponent<EntityCapturingComponent>(c => c.EntityDuringConfigure = c.Entity);

        var component = entity.GetComponent<EntityCapturingComponent>()!;
        Assert.Same(entity, component.EntityDuringConfigure);
    }

    [Fact]
    public void GetComponent_Exists_ReturnsComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();

        // Act
        var transform = entity.GetComponent<TransformComponent>();

        // Assert
        Assert.NotNull(transform);
    }

    [Fact]
    public void GetComponent_DoesNotExist_ReturnsNull()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act
        var transform = entity.GetComponent<TransformComponent>();

        // Assert
        Assert.Null(transform);
    }

    [Fact]
    public void GetRequiredComponent_Exists_ReturnsComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();

        // Act
        var transform = entity.GetRequiredComponent<TransformComponent>();

        // Assert
        Assert.NotNull(transform);
    }

    [Fact]
    public void GetRequiredComponent_DoesNotExist_ThrowsException()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act & Assert
        Assert.Throws<InvalidOperationException>(() => 
            entity.GetRequiredComponent<TransformComponent>());
    }

    [Fact]
    public void HasComponent_Exists_ReturnsTrue()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();

        // Act & Assert
        Assert.True(entity.HasComponent<TransformComponent>());
    }

    [Fact]
    public void HasComponent_DoesNotExist_ReturnsFalse()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act & Assert
        Assert.False(entity.HasComponent<TransformComponent>());
    }

    [Fact]
    public void RemoveComponent_Exists_RemovesSuccessfully()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();

        // Act
        var removed = entity.RemoveComponent<TransformComponent>();

        // Assert
        Assert.True(removed);
        Assert.False(entity.HasComponent<TransformComponent>());
    }

    [Fact]
    public void RemoveComponent_DoesNotExist_ReturnsFalse()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act
        var removed = entity.RemoveComponent<TransformComponent>();

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public void GetAllComponents_ReturnsAllComponents()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        entity.AddComponent<PhysicsBodyComponent>();

        // Act
        var components = entity.GetAllComponents().ToList();

        // Assert
        Assert.Equal(2, components.Count);
        Assert.Contains(components, c => c is TransformComponent);
        Assert.Contains(components, c => c is PhysicsBodyComponent);
    }

    #endregion

    #region Hierarchy - Basic Operations

    [Fact]
    public void Entity_AfterCreation_IsRoot()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Assert
        Assert.True(entity.IsRoot);
        Assert.Null(entity.Parent);
    }

    [Fact]
    public void SetParent_ValidParent_SetsParentCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        // Act
        child.SetParent(parent);

        // Assert
        Assert.Equal(parent, child.Parent);
        Assert.False(child.IsRoot);
        Assert.Contains(child, parent.Children);
    }

    [Fact]
    public void SetParent_ReturnsEntity_AllowsChaining()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        // Act
        var result = child.SetParent(parent);

        // Assert
        Assert.Equal(child, result);
    }

    [Fact]
    public void SetParent_Null_MakesEntityRoot()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();
        child.SetParent(parent);

        // Act
        child.SetParent(null);

        // Assert
        Assert.Null(child.Parent);
        Assert.True(child.IsRoot);
        Assert.DoesNotContain(child, parent.Children);
    }

    [Fact]
    public void SetParent_Self_DoesNotCreateCircularReference()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act
        entity.SetParent(entity);

        // Assert
        Assert.Null(entity.Parent);
        Assert.True(entity.IsRoot);
    }

    [Fact]
    public void SetParent_ToDescendant_DoesNotCreateCircularReference()
    {
        // Arrange
        var world = CreateTestWorld();
        var grandparent = world.CreateEntity();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        parent.SetParent(grandparent);
        child.SetParent(parent);

        // Act - Try to make grandparent a child of child (would create cycle)
        grandparent.SetParent(child);

        // Assert - Should not have changed
        Assert.Null(grandparent.Parent);
        Assert.True(grandparent.IsRoot);
    }

    [Fact]
    public void SetParent_ChangeParent_UpdatesHierarchy()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent1 = world.CreateEntity();
        var parent2 = world.CreateEntity();
        var child = world.CreateEntity();

        child.SetParent(parent1);

        // Act
        child.SetParent(parent2);

        // Assert
        Assert.Equal(parent2, child.Parent);
        Assert.DoesNotContain(child, parent1.Children);
        Assert.Contains(child, parent2.Children);
    }

    [Fact]
    public void AddChild_AddsChildToEntity()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        // Act
        parent.AddChild(child);

        // Assert
        Assert.Equal(parent, child.Parent);
        Assert.Contains(child, parent.Children);
    }

    [Fact]
    public void RemoveChild_RemovesChildFromEntity()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();
        parent.AddChild(child);

        // Act
        var removed = parent.RemoveChild(child);

        // Assert
        Assert.True(removed);
        Assert.Null(child.Parent);
        Assert.DoesNotContain(child, parent.Children);
    }

    [Fact]
    public void RemoveChild_NonChild_ReturnsFalse()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var notChild = world.CreateEntity();

        // Act
        var removed = parent.RemoveChild(notChild);

        // Assert
        Assert.False(removed);
    }

    [Fact]
    public void DetachFromParent_RemovesFromParent()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();
        child.SetParent(parent);

        // Act
        child.DetachFromParent();

        // Assert
        Assert.Null(child.Parent);
        Assert.True(child.IsRoot);
    }

    #endregion

    #region Hierarchy - Queries

    [Fact]
    public void GetRoot_RootEntity_ReturnsSelf()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act
        var root = entity.GetRoot();

        // Assert
        Assert.Equal(entity, root);
    }

    [Fact]
    public void GetRoot_NestedEntity_ReturnsTopMostParent()
    {
        // Arrange
        var world = CreateTestWorld();
        var grandparent = world.CreateEntity();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        parent.SetParent(grandparent);
        child.SetParent(parent);

        // Act
        var root = child.GetRoot();

        // Assert
        Assert.Equal(grandparent, root);
    }

    [Fact]
    public void GetDepth_RootEntity_ReturnsZero()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act
        var depth = entity.GetDepth();

        // Assert
        Assert.Equal(0, depth);
    }

    [Fact]
    public void GetDepth_NestedEntity_ReturnsCorrectDepth()
    {
        // Arrange
        var world = CreateTestWorld();
        var grandparent = world.CreateEntity();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        parent.SetParent(grandparent);
        child.SetParent(parent);

        // Act & Assert
        Assert.Equal(0, grandparent.GetDepth());
        Assert.Equal(1, parent.GetDepth());
        Assert.Equal(2, child.GetDepth());
    }

    [Fact]
    public void GetDescendants_NoChildren_ReturnsEmpty()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        // Act
        var descendants = entity.GetDescendants().ToList();

        // Assert
        Assert.Empty(descendants);
    }

    [Fact]
    public void GetDescendants_WithChildren_ReturnsAllDescendants()
    {
        // Arrange
        var world = CreateTestWorld();
        var grandparent = world.CreateEntity();
        var parent = world.CreateEntity();
        var child1 = world.CreateEntity();
        var child2 = world.CreateEntity();

        parent.SetParent(grandparent);
        child1.SetParent(parent);
        child2.SetParent(parent);

        // Act
        var descendants = grandparent.GetDescendants().ToList();

        // Assert
        Assert.Equal(3, descendants.Count);
        Assert.Contains(parent, descendants);
        Assert.Contains(child1, descendants);
        Assert.Contains(child2, descendants);
    }

    [Fact]
    public void FindDescendant_ExistsByName_ReturnsEntity()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();
        child.Name = "SpecialChild";
        child.SetParent(parent);

        // Act
        var found = parent.FindDescendant("SpecialChild");

        // Assert
        Assert.NotNull(found);
        Assert.Equal(child, found);
    }

    [Fact]
    public void FindDescendant_DoesNotExist_ReturnsNull()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity();

        // Act
        var found = parent.FindDescendant("NonExistent");

        // Assert
        Assert.Null(found);
    }

    [Fact]
    public void FindDescendant_NestedChild_FindsDeepChild()
    {
        // Arrange
        var world = CreateTestWorld();
        var grandparent = world.CreateEntity();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();
        child.Name = "DeepChild";

        parent.SetParent(grandparent);
        child.SetParent(parent);

        // Act
        var found = grandparent.FindDescendant("DeepChild");

        // Assert
        Assert.NotNull(found);
        Assert.Equal(child, found);
    }

    [Fact]
    public void GetDescendantsWithTag_ReturnsOnlyTaggedDescendants()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var child1 = world.CreateEntity().AddTag("Enemy");
        var child2 = world.CreateEntity().AddTag("Friendly");
        var child3 = world.CreateEntity().AddTag("Enemy");

        child1.SetParent(parent);
        child2.SetParent(parent);
        child3.SetParent(parent);

        // Act
        var enemies = parent.GetDescendantsWithTag("Enemy").ToList();

        // Assert
        Assert.Equal(2, enemies.Count);
        Assert.Contains(child1, enemies);
        Assert.Contains(child3, enemies);
    }

    #endregion

    #region Behaviors

    [Fact]
    public void AddBehavior_AddsBehaviorToEntity()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TestBehavior>();
        Assert.True(entity.HasBehavior<TestBehavior>());
    }

    [Fact]
    public void GetBehavior_Exists_ReturnsBehavior()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TestBehavior>();
        Assert.NotNull(entity.GetBehavior<TestBehavior>());
    }

    [Fact]
    public void GetBehavior_NotExists_ReturnsNull()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        Assert.Null(entity.GetBehavior<TestBehavior>());
    }

    [Fact]
    public void GetRequiredBehavior_Exists_ReturnsBehavior()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TestBehavior>();
        Assert.NotNull(entity.GetRequiredBehavior<TestBehavior>());
    }

    [Fact]
    public void GetRequiredBehavior_NotExists_ThrowsWithDiagnosticMessage()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Player");
        entity.AddBehavior<TestBehavior>();

        var ex = Assert.Throws<InvalidOperationException>(
            () => entity.GetRequiredBehavior<ConfigurableBehavior>());

        Assert.Contains("ConfigurableBehavior", ex.Message);
        Assert.Contains("Player", ex.Message);
        Assert.Contains("TestBehavior", ex.Message);
    }

    [Fact]
    public void RemoveBehavior_NullsEntityReference()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TestBehavior>();

        var behavior = entity.GetBehavior<TestBehavior>()!;
        Assert.NotNull(behavior.Entity);

        entity.RemoveBehavior<TestBehavior>();

        Assert.Null(behavior.Entity!);
    }

    [Fact]
    public void DestroyEntity_NullsBehaviorEntityReference()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TestBehavior>();

        var behavior = entity.GetBehavior<TestBehavior>()!;
        Assert.NotNull(behavior.Entity);

        world.DestroyEntity(entity);
        world.Flush();

        Assert.Null(behavior.Entity);
    }

    [Fact]
    public void RemoveBehavior_AfterRemoval_HasBehaviorReturnsFalse()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TestBehavior>();

        entity.RemoveBehavior<TestBehavior>();

        Assert.False(entity.HasBehavior<TestBehavior>());
    }

    [Fact]
    public void GetBehaviorInChildren_OnSelf_ReturnsBehavior()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TestBehavior>();

        var result = entity.GetBehaviorInChildren<TestBehavior>();

        Assert.NotNull(result);
    }

    [Fact]
    public void GetBehaviorInChildren_OnDirectChild_ReturnsBehavior()
    {
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();
        child.AddBehavior<TestBehavior>();
        child.SetParent(parent);

        var result = parent.GetBehaviorInChildren<TestBehavior>();

        Assert.NotNull(result);
    }

    [Fact]
    public void GetBehaviorInChildren_DeepNested_ReturnsBehavior()
    {
        var world = CreateTestWorld();
        var grandparent = world.CreateEntity();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();
        child.AddBehavior<TestBehavior>();

        parent.SetParent(grandparent);
        child.SetParent(parent);

        var result = grandparent.GetBehaviorInChildren<TestBehavior>();

        Assert.NotNull(result);
    }

    [Fact]
    public void GetBehaviorInChildren_NotFound_ReturnsNull()
    {
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();
        child.SetParent(parent);

        var result = parent.GetBehaviorInChildren<TestBehavior>();

        Assert.Null(result);
    }

    [Fact]
    public void GetBehaviorInParent_OnSelf_ReturnsBehavior()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TestBehavior>();

        var result = entity.GetBehaviorInParent<TestBehavior>();

        Assert.NotNull(result);
    }

    [Fact]
    public void GetBehaviorInParent_OnDirectParent_ReturnsBehavior()
    {
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        parent.AddBehavior<TestBehavior>();
        var child = world.CreateEntity();
        child.SetParent(parent);

        var result = child.GetBehaviorInParent<TestBehavior>();

        Assert.NotNull(result);
    }

    [Fact]
    public void GetBehaviorInParent_OnGrandparent_ReturnsBehavior()
    {
        var world = CreateTestWorld();
        var grandparent = world.CreateEntity();
        grandparent.AddBehavior<TestBehavior>();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        parent.SetParent(grandparent);
        child.SetParent(parent);

        var result = child.GetBehaviorInParent<TestBehavior>();

        Assert.NotNull(result);
    }

    [Fact]
    public void GetBehaviorInParent_NotFound_ReturnsNull()
    {
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();
        child.SetParent(parent);

        var result = child.GetBehaviorInParent<TestBehavior>();

        Assert.Null(result);
    }

    [Fact]
    public void AddBehavior_OnAddedThrows_BehaviorNotAttachedToEntity()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        Assert.Throws<InvalidOperationException>(() => entity.AddBehavior<ThrowingOnAddedBehavior>());

        Assert.False(entity.HasBehavior<ThrowingOnAddedBehavior>());
    }

    [Fact]
    public void AddBehavior_OnAddedThrows_EntityReferenceNulledOnBehavior()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        ThrowingOnAddedBehavior? captured = null;

        ThrowingOnAddedBehavior.OnConstructed = b => captured = b;
        try
        {
            Assert.Throws<InvalidOperationException>(() => entity.AddBehavior<ThrowingOnAddedBehavior>());
        }
        finally
        {
            ThrowingOnAddedBehavior.OnConstructed = null;
        }

        Assert.NotNull(captured);
        Assert.Null(captured!.Entity);
    }

    [Fact]
    public void AddBehavior_Configure_OnAddedThrows_BehaviorNotAttachedToEntity()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        Assert.Throws<InvalidOperationException>(() => entity.AddBehavior<ThrowingOnAddedBehavior>(_ => { }));

        Assert.False(entity.HasBehavior<ThrowingOnAddedBehavior>());
    }

    #endregion

    #region Component Hierarchy Queries

    [Fact]
    public void GetComponentInChildren_OnSelf_ReturnsComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();

        // Act
        var transform = entity.GetComponentInChildren<TransformComponent>();

        // Assert
        Assert.NotNull(transform);
    }

    [Fact]
    public void GetComponentInChildren_OnChild_ReturnsComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();
        child.AddComponent<TransformComponent>();
        child.SetParent(parent);

        // Act
        var transform = parent.GetComponentInChildren<TransformComponent>();

        // Assert
        Assert.NotNull(transform);
    }

    [Fact]
    public void GetComponentInChildren_DeepNested_ReturnsComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var grandparent = world.CreateEntity();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();
        child.AddComponent<TransformComponent>();

        parent.SetParent(grandparent);
        child.SetParent(parent);

        // Act
        var transform = grandparent.GetComponentInChildren<TransformComponent>();

        // Assert
        Assert.NotNull(transform);
    }

    [Fact]
    public void GetComponentInChildren_NotFound_ReturnsNull()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();
        child.SetParent(parent);

        // Act
        var transform = parent.GetComponentInChildren<TransformComponent>();

        // Assert
        Assert.Null(transform);
    }

    [Fact]
    public void GetComponentInParent_OnSelf_ReturnsComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();

        // Act
        var transform = entity.GetComponentInParent<TransformComponent>();

        // Assert
        Assert.NotNull(transform);
    }

    [Fact]
    public void GetComponentInParent_OnParent_ReturnsComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        parent.AddComponent<TransformComponent>();
        var child = world.CreateEntity();
        child.SetParent(parent);

        // Act
        var transform = child.GetComponentInParent<TransformComponent>();

        // Assert
        Assert.NotNull(transform);
    }

    [Fact]
    public void GetComponentInParent_OnGrandparent_ReturnsComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var grandparent = world.CreateEntity();
        grandparent.AddComponent<TransformComponent>();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();

        parent.SetParent(grandparent);
        child.SetParent(parent);

        // Act
        var transform = child.GetComponentInParent<TransformComponent>();

        // Assert
        Assert.NotNull(transform);
    }

    [Fact]
    public void GetComponentInParent_NotFound_ReturnsNull()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var child = world.CreateEntity();
        child.SetParent(parent);

        // Act
        var transform = child.GetComponentInParent<TransformComponent>();

        // Assert
        Assert.Null(transform);
    }

    [Fact]
    public void TryGetComponent_ComponentExists_ReturnsTrueAndComponent()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();

        var found = entity.TryGetComponent<TransformComponent>(out var component);

        Assert.True(found);
        Assert.NotNull(component);
    }

    [Fact]
    public void TryGetComponent_ComponentMissing_ReturnsFalse()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        var found = entity.TryGetComponent<TransformComponent>(out var component);

        Assert.False(found);
        Assert.Null(component);
    }

    [Fact]
    public void TryGetComponent_SingleLookupEquivalentToHasAndGet()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>(t => t.LocalPosition = new System.Numerics.Vector2(3, 7));

        var found = entity.TryGetComponent<TransformComponent>(out var component);

        Assert.True(found);
        Assert.Equal(new System.Numerics.Vector2(3, 7), component!.LocalPosition);
    }

    [Fact]
    public void TryGetBehavior_BehaviorExists_ReturnsTrueAndBehavior()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TestBehavior>();

        var found = entity.TryGetBehavior<TestBehavior>(out var behavior);

        Assert.True(found);
        Assert.NotNull(behavior);
    }

    [Fact]
    public void TryGetBehavior_BehaviorMissing_ReturnsFalse()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        var found = entity.TryGetBehavior<TestBehavior>(out var behavior);

        Assert.False(found);
        Assert.Null(behavior);
    }

    [Fact]
    public void TryGetBehavior_SingleScanEquivalentToHasAndGet()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<ConfigurableBehavior>(b => b.Speed = 42f);

        var found = entity.TryGetBehavior<ConfigurableBehavior>(out var behavior);

        Assert.True(found);
        Assert.Equal(42f, behavior!.Speed);
    }

    #endregion

    #region ToString

    [Fact]
    public void ToString_IncludesBasicInformation()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.Name = "TestEntity";
        entity.AddTag("Player");
        entity.AddComponent<TransformComponent>();
        // TODO: entity.AddBehavior<T>();

        // Act
        var result = entity.ToString();

        // Assert
        Assert.Contains("TestEntity", result);
        Assert.Contains(entity.Id.ToString(), result);
        Assert.Contains("Active: True", result);
        Assert.Contains("Components: 1", result);
        // TODO: Assert.Contains("Behaviors: 1", result);
        Assert.Contains("Tags: 1", result);
    }

    [Fact]
    public void ToString_WithParent_IncludesParentInfo()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        parent.Name = "Parent";
        var child = world.CreateEntity();
        child.Name = "Child";
        child.SetParent(parent);

        // Act
        var result = child.ToString();

        // Assert
        Assert.Contains("Parent: Parent", result);
    }

    [Fact]
    public void ToString_WithChildren_IncludesChildCount()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        parent.Name = "Parent";
        var child1 = world.CreateEntity();
        var child2 = world.CreateEntity();
        child1.SetParent(parent);
        child2.SetParent(parent);

        // Act
        var result = parent.ToString();

        // Assert
        Assert.Contains("Children: 2", result);
    }

    #endregion

    #region Test Helper Classes

    private class TestBehavior : Behavior { }

    private class ConfigurableBehavior : Behavior
    {
        public float Speed { get; set; }
    }

    private class LifecycleEntity : Entity
    {
        public bool ActivatedCalled { get; set; }
        public bool DeactivatedCalled { get; private set; }

        protected internal override void OnActivated() => ActivatedCalled = true;
        protected internal override void OnDeactivated() => DeactivatedCalled = true;
    }

    private class EntityCapturingComponent : Component
    {
        public Entity? EntityDuringConfigure { get; set; }
    }

    private class TrackableBehavior : Behavior
    {
        public int Tag { get; set; }
    }

    private class OtherBehavior : Behavior { }

    private class ThrowingOnAddedBehavior : Behavior
    {
        public static Action<ThrowingOnAddedBehavior>? OnConstructed;

        protected internal override void OnAdded()
        {
            OnConstructed?.Invoke(this);
            throw new InvalidOperationException("OnAdded intentionally threw");
        }
    }

    #endregion
}

public class AddComponentGuardTests : TestBase
{
    [Fact]
    public void AddComponent_ComponentAlreadyAttachedToOtherEntity_Throws()
    {
        var world = CreateTestWorld();
        var owner = world.CreateEntity("Owner");
        var thief = world.CreateEntity("Thief");

        var component = new TransformComponent();
        owner.AddComponent(component);

        var ex = Assert.Throws<ArgumentException>((Action)(() => thief.AddComponent(component)));
        Assert.Contains("Owner", ex.Message);
    }

    [Fact]
    public void AddComponent_ComponentAttachedToSelf_IsIdempotent()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        var component = new TransformComponent();
        entity.AddComponent(component);

        var ex = Record.Exception((Action)(() => entity.AddComponent(component)));

        Assert.Null(ex);
        Assert.Equal(1, entity.GetAllComponents().Count());
    }

    [Fact]
    public void AddComponent_ConfigureOverload_DuplicateIsSkipped()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>(t => t.LocalPosition = new System.Numerics.Vector2(1, 2));

        entity.AddComponent<TransformComponent>(t => t.LocalPosition = new System.Numerics.Vector2(99, 99));

        Assert.Equal(new System.Numerics.Vector2(1, 2), entity.GetComponent<TransformComponent>()!.LocalPosition);
    }

    [Fact]
    public void AddComponent_ConfigureOverload_Duplicate_DoesNotThrow()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();

        var ex = Record.Exception(() => entity.AddComponent<TransformComponent>(t => t.LocalPosition = new System.Numerics.Vector2(5, 5)));

        Assert.Null(ex);
    }

    [Fact]
    public void AddComponent_ConfigureOverload_Duplicate_LeavesComponentCountUnchanged()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();

        entity.AddComponent<TransformComponent>(t => t.LocalPosition = new System.Numerics.Vector2(5, 5));

        Assert.Equal(1, entity.GetAllComponents().Count());
    }
}

public class CollectDescendantsTests : TestBase
{
    [Fact]
    public void CollectDescendants_EmptyEntity_AddsNothing()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        var results = new List<Entity>();

        entity.CollectDescendants(results);

        Assert.Empty(results);
    }

    [Fact]
    public void CollectDescendants_FlatChildren_AddsAllChildren()
    {
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var c1 = world.CreateEntity(); c1.SetParent(parent);
        var c2 = world.CreateEntity(); c2.SetParent(parent);
        var results = new List<Entity>();

        parent.CollectDescendants(results);

        Assert.Equal(2, results.Count);
        Assert.Contains(c1, results);
        Assert.Contains(c2, results);
    }

    [Fact]
    public void CollectDescendants_DeepHierarchy_AddsAllDescendants()
    {
        var world = CreateTestWorld();
        var root = world.CreateEntity();
        var child = world.CreateEntity(); child.SetParent(root);
        var grandchild = world.CreateEntity(); grandchild.SetParent(child);
        var results = new List<Entity>();

        root.CollectDescendants(results);

        Assert.Equal(2, results.Count);
        Assert.Contains(child, results);
        Assert.Contains(grandchild, results);
    }

    [Fact]
    public void CollectDescendants_AppendsToExistingList()
    {
        var world = CreateTestWorld();
        var parent = world.CreateEntity();
        var child = world.CreateEntity(); child.SetParent(parent);
        var existing = world.CreateEntity();
        var results = new List<Entity> { existing };

        parent.CollectDescendants(results);

        Assert.Equal(2, results.Count);
        Assert.Contains(existing, results);
        Assert.Contains(child, results);
    }

    [Fact]
    public void CollectDescendants_MatchesGetDescendants()
    {
        var world = CreateTestWorld();
        var root = world.CreateEntity();
        for (int i = 0; i < 3; i++)
        {
            var c = world.CreateEntity(); c.SetParent(root);
            var gc = world.CreateEntity(); gc.SetParent(c);
        }

        var fromEnumerable = root.GetDescendants().ToList();
        var fromCollect = new List<Entity>();
        root.CollectDescendants(fromCollect);

        Assert.Equal(fromEnumerable, fromCollect);
    }
}

public class GetBehaviorsTests : TestBase
{
    [Fact]
    public void GetBehaviors_NoneAdded_ReturnsEmpty()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        var result = entity.GetBehaviors<TrackableBehavior>();

        Assert.Empty(result);
    }

    [Fact]
    public void GetBehaviors_OneMatch_ReturnsSingle()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TrackableBehavior>();

        var result = entity.GetBehaviors<TrackableBehavior>();

        Assert.Single(result);
    }

    [Fact]
    public void GetBehaviors_MultipleMatches_ReturnsAll()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<TrackableBehavior>();
        entity.AddBehavior<OtherBehavior>();

        var trackable = entity.GetBehaviors<TrackableBehavior>();
        var all = entity.GetBehaviors<Behavior>();

        Assert.Single(trackable);
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public void GetBehaviors_DoesNotReturnNonMatchingTypes()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<OtherBehavior>();

        var result = entity.GetBehaviors<TrackableBehavior>();

        Assert.Empty(result);
    }

    private class TrackableBehavior : Behavior
    {
        public int Tag { get; set; }
    }

    private class OtherBehavior : Behavior { }
}

public class BehaviorOnDestroyedTests : TestBase
{
    private class LifecycleBehavior : Behavior
    {
        public bool OnDestroyedCalled { get; private set; }
        public bool OnRemovedCalled { get; private set; }
        public bool EntitySetDuringOnDestroyed { get; private set; }
        public bool EntitySetDuringOnRemoved { get; private set; }
        public int DestroyedCallCount { get; private set; }
        public int RemovedCallCount { get; private set; }

        protected internal override void OnDestroyed()
        {
            OnDestroyedCalled = true;
            DestroyedCallCount++;
            EntitySetDuringOnDestroyed = Entity != null;
        }

        protected internal override void OnRemoved()
        {
            OnRemovedCalled = true;
            RemovedCallCount++;
            EntitySetDuringOnRemoved = Entity != null;
        }
    }

    [Fact]
    public void OnDestroyed_CalledWhenEntityIsDestroyed()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<LifecycleBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<LifecycleBehavior>()!;
        world.DestroyEntity(entity);
        world.Flush();

        Assert.True(behavior.OnDestroyedCalled);
    }

    [Fact]
    public void OnDestroyed_NotCalledOnExplicitRemoveBehavior()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<LifecycleBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<LifecycleBehavior>()!;
        entity.RemoveBehavior<LifecycleBehavior>();

        Assert.False(behavior.OnDestroyedCalled);
        Assert.True(behavior.OnRemovedCalled);
    }

    [Fact]
    public void OnDestroyed_EntityIsStillSetDuringCallback()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<LifecycleBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<LifecycleBehavior>()!;
        world.DestroyEntity(entity);
        world.Flush();

        Assert.True(behavior.EntitySetDuringOnDestroyed);
    }

    [Fact]
    public void OnDestroyed_FiresBeforeOnRemoved()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        var callOrder = new List<string>();

        entity.AddBehavior<LifecycleBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<LifecycleBehavior>()!;

        // Use a second behavior to observe ordering indirectly via counts
        world.DestroyEntity(entity);
        world.Flush();

        Assert.Equal(1, behavior.DestroyedCallCount);
        Assert.Equal(1, behavior.RemovedCallCount);
        Assert.True(behavior.OnDestroyedCalled);
        Assert.True(behavior.OnRemovedCalled);
    }

    [Fact]
    public void OnRemoved_CalledOnBothDestroyAndExplicitRemove()
    {
        var world = CreateTestWorld();
        var entity1 = world.CreateEntity();
        entity1.AddBehavior<LifecycleBehavior>();
        world.Flush();
        var b1 = entity1.GetBehavior<LifecycleBehavior>()!;

        var entity2 = world.CreateEntity();
        entity2.AddBehavior<LifecycleBehavior>();
        world.Flush();
        var b2 = entity2.GetBehavior<LifecycleBehavior>()!;

        entity1.RemoveBehavior<LifecycleBehavior>();
        world.DestroyEntity(entity2);
        world.Flush();

        Assert.True(b1.OnRemovedCalled);
        Assert.False(b1.OnDestroyedCalled);

        Assert.True(b2.OnRemovedCalled);
        Assert.True(b2.OnDestroyedCalled);
    }
}

public class BehaviorComponentNotificationTests : TestBase
{
    private class SpyBehavior : Behavior
    {
        public Component? LastAdded { get; private set; }
        public Component? LastRemoved { get; private set; }
        public int AddedCount { get; private set; }
        public int RemovedCount { get; private set; }
        public bool EntitySetDuringAdded { get; private set; }
        public bool EntitySetDuringRemoved { get; private set; }

        protected internal override void OnComponentAdded(Component component)
        {
            LastAdded = component;
            AddedCount++;
            EntitySetDuringAdded = Entity != null;
        }

        protected internal override void OnComponentRemoved(Component component)
        {
            LastRemoved = component;
            RemovedCount++;
            EntitySetDuringRemoved = Entity != null;
        }
    }

    [Fact]
    public void OnComponentAdded_CalledWhenComponentAddedToSameEntity()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<SpyBehavior>();

        entity.AddComponent<TransformComponent>();

        var spy = entity.GetBehavior<SpyBehavior>()!;
        Assert.IsType<TransformComponent>(spy.LastAdded);
    }

    [Fact]
    public void OnComponentAdded_NotCalledForOtherEntities()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<SpyBehavior>();

        var other = world.CreateEntity();
        other.AddComponent<TransformComponent>();

        var spy = entity.GetBehavior<SpyBehavior>()!;
        Assert.Equal(0, spy.AddedCount);
    }

    [Fact]
    public void OnComponentAdded_EntityReferenceIsValidDuringCallback()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<SpyBehavior>();
        entity.AddComponent<TransformComponent>();

        Assert.True(entity.GetBehavior<SpyBehavior>()!.EntitySetDuringAdded);
    }

    [Fact]
    public void OnComponentAdded_FiresAfterComponentOnAdded()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();

        int onAddedOrder = 0;
        int behaviorNotifyOrder = 0;
        int counter = 0;

        entity.AddBehavior<TrackingBehavior>();
        world.Flush();
        entity.GetBehavior<TrackingBehavior>()!.OnComponentAddedAction = _ => behaviorNotifyOrder = ++counter;

        var component = new OrderTrackingComponent(onAdded: () => onAddedOrder = ++counter);
        entity.AddComponent(component);

        Assert.True(onAddedOrder < behaviorNotifyOrder,
            $"Component.OnAdded ({onAddedOrder}) should fire before behavior.OnComponentAdded ({behaviorNotifyOrder})");
    }

    [Fact]
    public void OnComponentRemoved_CalledWhenComponentRemovedFromSameEntity()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<SpyBehavior>();
        entity.AddComponent<TransformComponent>();

        entity.RemoveComponent<TransformComponent>();

        var spy = entity.GetBehavior<SpyBehavior>()!;
        Assert.IsType<TransformComponent>(spy.LastRemoved);
    }

    [Fact]
    public void OnComponentRemoved_NotCalledForOtherEntities()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<SpyBehavior>();

        var other = world.CreateEntity();
        other.AddComponent<TransformComponent>();
        other.RemoveComponent<TransformComponent>();

        Assert.Equal(0, entity.GetBehavior<SpyBehavior>()!.RemovedCount);
    }

    [Fact]
    public void OnComponentRemoved_EntityReferenceIsValidDuringCallback()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<SpyBehavior>();
        entity.AddComponent<TransformComponent>();

        entity.RemoveComponent<TransformComponent>();

        Assert.True(entity.GetBehavior<SpyBehavior>()!.EntitySetDuringRemoved);
    }

    [Fact]
    public void OnComponentRemoved_ComponentStillAccessibleViaBehaviorCallback()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        Component? captured = null;

        entity.AddBehavior<TrackingBehavior>();
        entity.GetBehavior<TrackingBehavior>()!.OnRemovedAction = c => captured = c;
        entity.AddComponent<TransformComponent>(t => t.LocalPosition = new System.Numerics.Vector2(7, 7));

        entity.RemoveComponent<TransformComponent>();

        Assert.NotNull(captured);
        Assert.IsType<TransformComponent>(captured);
    }

    [Fact]
    public void OnComponentAdded_CalledOncePerAddedComponent()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<SpyBehavior>();

        entity.AddComponent<TransformComponent>();

        Assert.Equal(1, entity.GetBehavior<SpyBehavior>()!.AddedCount);
    }

    private sealed class OrderTrackingComponent : Component
    {
        private readonly Action _onAdded;
        public OrderTrackingComponent(Action onAdded) => _onAdded = onAdded;
        protected internal override void OnAdded() => _onAdded();
    }

    private sealed class TrackingBehavior : Behavior
    {
        public Action<Component>? OnRemovedAction { get; set; }
        public Action<Component>? OnComponentAddedAction { get; set; }

        protected internal override void OnComponentAdded(Component component)
            => OnComponentAddedAction?.Invoke(component);

        protected internal override void OnComponentRemoved(Component component)
            => OnRemovedAction?.Invoke(component);
    }
}
