using System.Numerics;
using Brine2D.ECS;
using Brine2D.Systems.Rendering;

namespace Brine2D.Tests.Systems.Rendering;

public class CameraFollowComponentTests : TestBase
{
    #region Default Values

    [Fact]
    public void Constructor_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Assert
        Assert.Equal("main", cameraFollow.CameraName);
        Assert.Equal(5f, cameraFollow.Smoothing);
        Assert.Equal(5f, cameraFollow.ZoomSmoothing);
        Assert.Equal(Vector2.Zero, cameraFollow.Offset);
        Assert.True(cameraFollow.FollowX);
        Assert.True(cameraFollow.FollowY);
        Assert.Equal(Vector2.Zero, cameraFollow.Deadzone);
        Assert.True(cameraFollow.IsActive);
        Assert.Equal(0, cameraFollow.Priority);
    }

    #endregion

    #region Camera Name

    [Fact]
    public void CameraName_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act
        cameraFollow.CameraName = "minimap";

        // Assert
        Assert.Equal("minimap", cameraFollow.CameraName);
    }

    [Fact]
    public void CameraName_DefaultIsMain()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Assert
        Assert.Equal("main", cameraFollow.CameraName);
    }

    [Fact]
    public void CameraName_SetNull_ThrowsArgumentNullException()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => cameraFollow.CameraName = null!);
    }

    [Fact]
    public void CameraName_SetEmpty_ThrowsArgumentException()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => cameraFollow.CameraName = "");
    }

    [Fact]
    public void CameraName_SetWhitespace_ThrowsArgumentException()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act & Assert
        Assert.Throws<ArgumentException>(() => cameraFollow.CameraName = "   ");
    }

    [Fact]
    public void CameraName_RejectsInvalidValue_RetainsPreviousName()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;
        cameraFollow.CameraName = "minimap";

        // Act
        Assert.Throws<ArgumentException>(() => cameraFollow.CameraName = "");

        // Assert
        Assert.Equal("minimap", cameraFollow.CameraName);
    }

    #endregion

    #region Smoothing

    [Fact]
    public void Smoothing_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act
        cameraFollow.Smoothing = 10f;

        // Assert
        Assert.Equal(10f, cameraFollow.Smoothing);
    }

    [Fact]
    public void Smoothing_CanBeZero_ForInstantFollow()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act
        cameraFollow.Smoothing = 0f;

        // Assert
        Assert.Equal(0f, cameraFollow.Smoothing);
    }

    [Fact]
    public void Smoothing_HigherValue_MeansSnappierFollow()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act
        cameraFollow.Smoothing = 20f;

        // Assert - Just verify it's set
        Assert.Equal(20f, cameraFollow.Smoothing);
    }

    [Fact]
    public void Smoothing_Negative_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => cameraFollow.Smoothing = -1f);
    }

    [Fact]
    public void Smoothing_RejectsNegative_RetainsPreviousValue()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;
        cameraFollow.Smoothing = 10f;

        // Act
        Assert.Throws<ArgumentOutOfRangeException>(() => cameraFollow.Smoothing = -5f);

        // Assert
        Assert.Equal(10f, cameraFollow.Smoothing);
    }

    #endregion

    #region Zoom Smoothing

    [Fact]
    public void ZoomSmoothing_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act
        cameraFollow.ZoomSmoothing = 10f;

        // Assert
        Assert.Equal(10f, cameraFollow.ZoomSmoothing);
    }

    [Fact]
    public void ZoomSmoothing_CanBeZero_ForInstantZoom()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act
        cameraFollow.ZoomSmoothing = 0f;

        // Assert
        Assert.Equal(0f, cameraFollow.ZoomSmoothing);
    }

    [Fact]
    public void ZoomSmoothing_Negative_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => cameraFollow.ZoomSmoothing = -1f);
    }

    #endregion

    #region Offset

    [Fact]
    public void Offset_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act
        cameraFollow.Offset = new Vector2(50, 100);

        // Assert
        Assert.Equal(new Vector2(50, 100), cameraFollow.Offset);
    }

    [Fact]
    public void Offset_CanBeNegative()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act
        cameraFollow.Offset = new Vector2(-50, -100);

        // Assert
        Assert.Equal(new Vector2(-50, -100), cameraFollow.Offset);
    }

    #endregion

    #region Follow Axes

    [Fact]
    public void FollowX_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act
        cameraFollow.FollowX = false;

        // Assert
        Assert.False(cameraFollow.FollowX);
    }

    [Fact]
    public void FollowY_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act
        cameraFollow.FollowY = false;

        // Assert
        Assert.False(cameraFollow.FollowY);
    }

    [Fact]
    public void FollowX_CanBeToggled()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act & Assert
        cameraFollow.FollowX = false;
        Assert.False(cameraFollow.FollowX);

        cameraFollow.FollowX = true;
        Assert.True(cameraFollow.FollowX);
    }

    [Fact]
    public void FollowY_CanBeToggled()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act & Assert
        cameraFollow.FollowY = false;
        Assert.False(cameraFollow.FollowY);

        cameraFollow.FollowY = true;
        Assert.True(cameraFollow.FollowY);
    }

    [Fact]
    public void FollowXAndY_CanBothBeDisabled()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act
        cameraFollow.FollowX = false;
        cameraFollow.FollowY = false;

        // Assert
        Assert.False(cameraFollow.FollowX);
        Assert.False(cameraFollow.FollowY);
    }

    #endregion

    #region Deadzone

    [Fact]
    public void Deadzone_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act
        cameraFollow.Deadzone = new Vector2(50, 30);

        // Assert
        Assert.Equal(new Vector2(50, 30), cameraFollow.Deadzone);
    }

    [Fact]
    public void Deadzone_DefaultIsZero()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Assert
        Assert.Equal(Vector2.Zero, cameraFollow.Deadzone);
    }

    [Fact]
    public void Deadzone_NegativeX_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => cameraFollow.Deadzone = new Vector2(-1, 10));
    }

    [Fact]
    public void Deadzone_NegativeY_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => cameraFollow.Deadzone = new Vector2(10, -1));
    }

    [Fact]
    public void Deadzone_BothNegative_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => cameraFollow.Deadzone = new Vector2(-5, -5));
    }

    [Fact]
    public void Deadzone_RejectsNegative_RetainsPreviousValue()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;
        cameraFollow.Deadzone = new Vector2(50, 30);

        // Act
        Assert.Throws<ArgumentOutOfRangeException>(() => cameraFollow.Deadzone = new Vector2(-1, 10));

        // Assert
        Assert.Equal(new Vector2(50, 30), cameraFollow.Deadzone);
    }

    #endregion

    #region Active State

    [Fact]
    public void IsActive_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act
        cameraFollow.IsActive = false;

        // Assert
        Assert.False(cameraFollow.IsActive);
    }

    [Fact]
    public void IsActive_CanBeToggled()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act & Assert
        cameraFollow.IsActive = false;
        Assert.False(cameraFollow.IsActive);

        cameraFollow.IsActive = true;
        Assert.True(cameraFollow.IsActive);
    }

    #endregion

    #region Priority

    [Fact]
    public void Priority_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act
        cameraFollow.Priority = 10;

        // Assert
        Assert.Equal(10, cameraFollow.Priority);
    }

    [Fact]
    public void Priority_CanBeNegative()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act
        cameraFollow.Priority = -5;

        // Assert
        Assert.Equal(-5, cameraFollow.Priority);
    }

    [Fact]
    public void Priority_DefaultIsZero()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Assert
        Assert.Equal(0, cameraFollow.Priority);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void CameraFollowComponent_PlatformerSetup_WorksCorrectly()
    {
        // Arrange & Act - Typical 2D platformer camera
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.Smoothing = 8f;
                c.FollowX = true;
                c.FollowY = false; // Don't follow Y in platformers
                c.Offset = new Vector2(0, 50); // Look slightly ahead
            });

        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Assert
        Assert.Equal(8f, cameraFollow.Smoothing);
        Assert.True(cameraFollow.FollowX);
        Assert.False(cameraFollow.FollowY);
        Assert.Equal(new Vector2(0, 50), cameraFollow.Offset);
    }

    [Fact]
    public void CameraFollowComponent_TopDownSetup_WorksCorrectly()
    {
        // Arrange & Act - Top-down game camera
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.Smoothing = 5f;
                c.FollowX = true;
                c.FollowY = true;
                c.Deadzone = new Vector2(100, 100);
            });

        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Assert
        Assert.Equal(5f, cameraFollow.Smoothing);
        Assert.True(cameraFollow.FollowX);
        Assert.True(cameraFollow.FollowY);
        Assert.Equal(new Vector2(100, 100), cameraFollow.Deadzone);
    }

    [Fact]
    public void CameraFollowComponent_MinimapSetup_WorksCorrectly()
    {
        // Arrange & Act - Minimap camera
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.CameraName = "minimap";
                c.Smoothing = 0f; // Instant follow
                c.Priority = -1; // Lower priority than main camera
            });

        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Assert
        Assert.Equal("minimap", cameraFollow.CameraName);
        Assert.Equal(0f, cameraFollow.Smoothing);
        Assert.Equal(-1, cameraFollow.Priority);
    }

    [Fact]
    public void CameraFollowComponent_CompleteSetup_WorksCorrectly()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.CameraName = "custom";
                c.Smoothing = 12f;
                c.Offset = new Vector2(100, 50);
                c.FollowX = true;
                c.FollowY = false;
                c.Deadzone = new Vector2(80, 40);
                c.IsActive = true;
                c.Priority = 5;
            });

        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Assert
        Assert.Equal("custom", cameraFollow.CameraName);
        Assert.Equal(12f, cameraFollow.Smoothing);
        Assert.Equal(new Vector2(100, 50), cameraFollow.Offset);
        Assert.True(cameraFollow.FollowX);
        Assert.False(cameraFollow.FollowY);
        Assert.Equal(new Vector2(80, 40), cameraFollow.Deadzone);
        Assert.True(cameraFollow.IsActive);
        Assert.Equal(5, cameraFollow.Priority);
    }

    #endregion

    #region Target Zoom

    [Fact]
    public void TargetZoom_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        // Act
        cameraFollow.TargetZoom = 2f;

        // Assert
        Assert.Equal(2f, cameraFollow.TargetZoom);
    }

    [Fact]
    public void TargetZoom_Negative_ThrowsArgumentOutOfRangeException()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        Assert.Throws<ArgumentOutOfRangeException>(() => cameraFollow.TargetZoom = -1f);
    }

    [Fact]
    public void TargetZoom_Zero_ThrowsArgumentOutOfRangeException()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;

        Assert.Throws<ArgumentOutOfRangeException>(() => cameraFollow.TargetZoom = 0f);
    }

    [Fact]
    public void TargetZoom_Null_IsAllowed()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;
        cameraFollow.TargetZoom = 2f;

        cameraFollow.TargetZoom = null;

        Assert.Null(cameraFollow.TargetZoom);
    }

    [Fact]
    public void TargetZoom_RejectsInvalid_RetainsPreviousValue()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<CameraFollowComponent>();
        var cameraFollow = entity.GetComponent<CameraFollowComponent>()!;
        cameraFollow.TargetZoom = 3f;

        Assert.Throws<ArgumentOutOfRangeException>(() => cameraFollow.TargetZoom = -1f);

        Assert.Equal(3f, cameraFollow.TargetZoom);
    }

    #endregion
}