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
        Assert.NotEqual(Guid.Empty, entity.Id);
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
        entity.AddComponent<VelocityComponent>();

        // Act
        var components = entity.GetAllComponents().ToList();

        // Assert
        Assert.Equal(2, components.Count);
        Assert.Contains(components, c => c is TransformComponent);
        Assert.Contains(components, c => c is VelocityComponent);
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

        // Act
        var result = entity.ToString();

        // Assert
        Assert.Contains("TestEntity", result);
        Assert.Contains(entity.Id.ToString(), result);
        Assert.Contains("Active: True", result);
        Assert.Contains("Components: 1", result);
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
}