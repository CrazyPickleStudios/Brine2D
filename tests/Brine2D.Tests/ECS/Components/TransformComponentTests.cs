using System.Numerics;
using Brine2D.ECS;
using Brine2D.ECS.Components;

namespace Brine2D.Tests.ECS.Components;

public class TransformComponentTests : TestBase
{
    #region Local Transform Tests

    [Fact]
    public void Constructor_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>()!;

        // Assert
        Assert.Equal(Vector2.Zero, transform.LocalPosition);
        Assert.Equal(0f, transform.LocalRotation);
        Assert.Equal(Vector2.One, transform.LocalScale);
    }

    [Fact]
    public void AddComponent_WithConfiguration_ConfiguresInline()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t =>
            {
                t.LocalPosition = new Vector2(10, 20);
                t.LocalRotation = MathF.PI / 2;
                t.LocalScale = new Vector2(2, 3);
            });

        var transform = entity.GetComponent<TransformComponent>()!;

        // Assert
        Assert.Equal(new Vector2(10, 20), transform.LocalPosition);
        Assert.Equal(MathF.PI / 2, transform.LocalRotation, precision: 5);
        Assert.Equal(new Vector2(2, 3), transform.LocalScale);
    }

    [Fact]
    public void AddComponent_WithConfiguration_AllowsChaining()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 100))
            .AddTag("Configured");

        // Assert
        Assert.True(entity.HasTag("Configured"));
        Assert.Equal(new Vector2(100, 100), entity.GetComponent<TransformComponent>()!.LocalPosition);
    }

    [Fact]
    public void LocalPosition_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>()!;

        // Act
        transform.LocalPosition = new Vector2(10, 20);

        // Assert
        Assert.Equal(new Vector2(10, 20), transform.LocalPosition);
    }

    [Fact]
    public void LocalRotation_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>()!;

        // Act
        transform.LocalRotation = MathF.PI / 2; // 90 degrees

        // Assert
        Assert.Equal(MathF.PI / 2, transform.LocalRotation, precision: 5);
    }

    [Fact]
    public void LocalScale_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>()!;

        // Act
        transform.LocalScale = new Vector2(2, 3);

        // Assert
        Assert.Equal(new Vector2(2, 3), transform.LocalScale);
    }

    #endregion

    #region World Transform Tests (No Parent)

    [Fact]
    public void Position_WithNoParent_ReturnsSameAsLocalPosition()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 200));

        var transform = entity.GetComponent<TransformComponent>()!;

        // Act & Assert
        Assert.Equal(transform.LocalPosition, transform.Position);
    }

    [Fact]
    public void Rotation_WithNoParent_ReturnsSameAsLocalRotation()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalRotation = MathF.PI);

        var transform = entity.GetComponent<TransformComponent>()!;

        // Act & Assert
        Assert.Equal(transform.LocalRotation, transform.Rotation);
    }

    [Fact]
    public void Scale_WithNoParent_ReturnsSameAsLocalScale()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalScale = new Vector2(2, 2));

        var transform = entity.GetComponent<TransformComponent>()!;

        // Act & Assert
        Assert.Equal(transform.LocalScale, transform.Scale);
    }

    #endregion

    #region Hierarchical Transform Tests

    [Fact]
    public void Position_WithParent_ComputesWorldPosition()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 100));

        var child = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(50, 50));
        
        child.SetParent(parent);

        var childTransform = child.GetComponent<TransformComponent>()!;

        // Act
        var worldPosition = childTransform.Position;

        // Assert
        Assert.Equal(new Vector2(150, 150), worldPosition);
    }

    [Fact]
    public void Rotation_WithParent_AddsParentRotation()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalRotation = MathF.PI / 4); // 45 degrees

        var child = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalRotation = MathF.PI / 4); // 45 degrees
        
        child.SetParent(parent);

        var childTransform = child.GetComponent<TransformComponent>()!;

        // Act
        var worldRotation = childTransform.Rotation;

        // Assert
        Assert.Equal(MathF.PI / 2, worldRotation, precision: 5); // 90 degrees total
    }

    [Fact]
    public void Scale_WithParent_MultipliesParentScale()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalScale = new Vector2(2, 2));

        var child = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalScale = new Vector2(3, 3));
        
        child.SetParent(parent);

        var childTransform = child.GetComponent<TransformComponent>()!;

        // Act
        var worldScale = childTransform.Scale;

        // Assert
        Assert.Equal(new Vector2(6, 6), worldScale);
    }

    [Fact]
    public void Position_WithParentRotation_AppliesRotationToOffset()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity()
            .AddComponent<TransformComponent>(t =>
            {
                t.LocalPosition = new Vector2(100, 100);
                t.LocalRotation = MathF.PI / 2; // 90 degrees (rotate left)
            });

        var child = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(10, 0)); // Offset to the right
        
        child.SetParent(parent);

        var childTransform = child.GetComponent<TransformComponent>()!;

        // Act
        var worldPosition = childTransform.Position;

        // Assert - After 90° rotation, right becomes up
        Assert.Equal(100, worldPosition.X, precision: 2);
        Assert.Equal(110, worldPosition.Y, precision: 2);
    }

    [Fact]
    public void Position_WithParentScale_ScalesOffset()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity()
            .AddComponent<TransformComponent>(t =>
            {
                t.LocalPosition = new Vector2(100, 100);
                t.LocalScale = new Vector2(2, 2);
            });

        var child = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(10, 20));
        
        child.SetParent(parent);

        var childTransform = child.GetComponent<TransformComponent>()!;

        // Act
        var worldPosition = childTransform.Position;

        // Assert - Offset should be doubled
        Assert.Equal(new Vector2(120, 140), worldPosition);
    }

    #endregion

    #region Setting World Properties

    [Fact]
    public void SetPosition_WithNoParent_SetsLocalPosition()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>()!;

        // Act
        transform.Position = new Vector2(100, 200);

        // Assert
        Assert.Equal(new Vector2(100, 200), transform.LocalPosition);
    }

    [Fact]
    public void SetPosition_WithParent_ConvertsToLocalSpace()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 100));

        var child = world.CreateEntity()
            .AddComponent<TransformComponent>();
        
        child.SetParent(parent);

        var childTransform = child.GetComponent<TransformComponent>()!;

        // Act
        childTransform.Position = new Vector2(150, 200); // Set world position

        // Assert
        Assert.Equal(new Vector2(50, 100), childTransform.LocalPosition);
        Assert.Equal(new Vector2(150, 200), childTransform.Position); // World position matches
    }

    [Fact]
    public void SetRotation_WithParent_ConvertsToLocalRotation()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalRotation = MathF.PI / 4); // 45 degrees

        var child = world.CreateEntity()
            .AddComponent<TransformComponent>();
        
        child.SetParent(parent);

        var childTransform = child.GetComponent<TransformComponent>()!;

        // Act
        childTransform.Rotation = MathF.PI / 2; // Set world rotation to 90 degrees

        // Assert
        Assert.Equal(MathF.PI / 4, childTransform.LocalRotation, precision: 5); // Local is 45 degrees
        Assert.Equal(MathF.PI / 2, childTransform.Rotation, precision: 5); // World is 90 degrees
    }

    [Fact]
    public void SetScale_WithParent_ConvertsToLocalScale()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalScale = new Vector2(2, 2));

        var child = world.CreateEntity()
            .AddComponent<TransformComponent>();
        
        child.SetParent(parent);

        var childTransform = child.GetComponent<TransformComponent>()!;

        // Act
        childTransform.Scale = new Vector2(4, 4); // Set world scale to 4x

        // Assert
        Assert.Equal(new Vector2(2, 2), childTransform.LocalScale); // Local is 2x
        Assert.Equal(new Vector2(4, 4), childTransform.Scale); // World is 4x
    }

    #endregion

    #region Transformation Methods

    [Fact]
    public void Translate_AddsToLocalPosition()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(10, 20));

        var transform = entity.GetComponent<TransformComponent>()!;

        // Act
        transform.Translate(new Vector2(5, 10));

        // Assert
        Assert.Equal(new Vector2(15, 30), transform.LocalPosition);
    }

    [Fact]
    public void Translate_Multiple_Accumulates()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>()!;

        // Act
        transform.Translate(new Vector2(10, 0));
        transform.Translate(new Vector2(0, 20));
        transform.Translate(new Vector2(5, 5));

        // Assert
        Assert.Equal(new Vector2(15, 25), transform.LocalPosition);
    }

    [Fact]
    public void Rotate_AddsToLocalRotation()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalRotation = MathF.PI / 4);

        var transform = entity.GetComponent<TransformComponent>()!;

        // Act
        transform.Rotate(MathF.PI / 4);

        // Assert
        Assert.Equal(MathF.PI / 2, transform.LocalRotation, precision: 5);
    }

    [Fact]
    public void Rotate_Multiple_Accumulates()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>()!;

        // Act
        transform.Rotate(MathF.PI / 6); // 30 degrees
        transform.Rotate(MathF.PI / 6); // 30 degrees
        transform.Rotate(MathF.PI / 6); // 30 degrees

        // Assert
        Assert.Equal(MathF.PI / 2, transform.LocalRotation, precision: 5); // 90 degrees total
    }

    #endregion

    #region Transform Matrix Tests

    [Fact]
    public void GetTransformMatrix_WithIdentityTransform_ReturnsIdentity()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<TransformComponent>();
        var transform = entity.GetComponent<TransformComponent>()!;

        // Act
        var matrix = transform.GetTransformMatrix();

        // Assert
        Assert.True(matrix.IsIdentity);
    }

    [Fact]
    public void GetTransformMatrix_WithPosition_IncludesTranslation()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 200));

        var transform = entity.GetComponent<TransformComponent>()!;

        // Act
        var matrix = transform.GetTransformMatrix();

        // Assert
        Assert.Equal(100, matrix.M31, precision: 3);
        Assert.Equal(200, matrix.M32, precision: 3);
    }

    [Fact]
    public void GetTransformMatrix_WithScale_IncludesScale()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalScale = new Vector2(2, 3));

        var transform = entity.GetComponent<TransformComponent>()!;

        // Act
        var matrix = transform.GetTransformMatrix();

        // Assert
        var testPoint = Vector2.Transform(Vector2.One, matrix);
        Assert.Equal(2, testPoint.X, precision: 3);
        Assert.Equal(3, testPoint.Y, precision: 3);
    }

    [Fact]
    public void GetTransformMatrix_WithRotation_IncludesRotation()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalRotation = MathF.PI / 2); // 90 degrees

        var transform = entity.GetComponent<TransformComponent>()!;

        // Act
        var matrix = transform.GetTransformMatrix();

        // Assert - Point to the right should rotate to point up
        var testPoint = Vector2.Transform(new Vector2(1, 0), matrix);
        Assert.Equal(0, testPoint.X, precision: 3);
        Assert.Equal(1, testPoint.Y, precision: 3);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void HierarchyWithoutParentTransform_UsesLocalTransform()
    {
        // Arrange
        var world = CreateTestWorld();
        var parent = world.CreateEntity(); // No transform component
        var child = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(10, 20));
        
        child.SetParent(parent);

        var childTransform = child.GetComponent<TransformComponent>()!;

        // Act & Assert - Should use local position when parent has no transform
        Assert.Equal(new Vector2(10, 20), childTransform.Position);
    }

    [Fact]
    public void MultiLevelHierarchy_ComputesCorrectWorldTransform()
    {
        // Arrange
        var world = CreateTestWorld();
        var grandparent = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 0));

        var parent = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 0));

        var child = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 0));

        parent.SetParent(grandparent);
        child.SetParent(parent);

        var cTransform = child.GetComponent<TransformComponent>()!;

        // Act
        var worldPosition = cTransform.Position;

        // Assert - Should accumulate all parent positions
        Assert.Equal(new Vector2(300, 0), worldPosition);
    }

    #endregion
}