using System.Numerics;
using Brine2D.Rendering;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.Rendering;

public class Camera2DTests
{
    #region Initialization Tests

    [Fact]
    public void ShouldInitializeWithViewportDimensions()
    {
        // Arrange & Act
        var camera = new Camera2D(800, 600);

        // Assert
        camera.ViewportWidth.Should().Be(800);
        camera.ViewportHeight.Should().Be(600);
    }

    [Fact]
    public void ShouldInitializeWithDefaultValues()
    {
        // Arrange & Act
        var camera = new Camera2D(800, 600);

        // Assert
        camera.Position.Should().Be(Vector2.Zero);
        camera.Zoom.Should().Be(1.0f);
        camera.Rotation.Should().Be(0f);
    }

    #endregion

    #region Position Tests

    [Fact]
    public void ShouldSetAndGetPosition()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        camera.Position = new Vector2(100, 200);

        // Assert
        camera.Position.X.Should().Be(100);
        camera.Position.Y.Should().Be(200);
    }

    [Fact]
    public void ShouldMoveByOffset()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = new Vector2(100, 100);

        // Act
        camera.Move(new Vector2(50, -25));

        // Assert
        camera.Position.X.Should().Be(150);
        camera.Position.Y.Should().Be(75);
    }

    [Fact]
    public void ShouldCenterOnTarget()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        camera.CenterOn(new Vector2(500, 300));

        // Assert
        camera.Position.Should().Be(new Vector2(500, 300));
    }

    [Fact]
    public void ShouldLerpTowardsTarget()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = new Vector2(0, 0);

        // Act - Lerp halfway to target
        camera.LerpTo(new Vector2(100, 100), 0.5f);

        // Assert
        camera.Position.X.Should().BeApproximately(50, 0.01f);
        camera.Position.Y.Should().BeApproximately(50, 0.01f);
    }

    #endregion

    #region Zoom Tests

    [Fact]
    public void ShouldSetAndGetZoom()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        camera.Zoom = 2.0f;

        // Assert
        camera.Zoom.Should().Be(2.0f);
    }

    [Fact]
    public void ShouldClampZoomToMinimum()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        camera.Zoom = 0f;

        // Assert
        camera.Zoom.Should().Be(0.01f); // Clamped to minimum
    }

    [Fact]
    public void ShouldClampNegativeZoom()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        camera.Zoom = -5f;

        // Assert
        camera.Zoom.Should().Be(0.01f); // Clamped to minimum
    }

    [Fact]
    public void ShouldAllowZoomGreaterThanOne()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        camera.Zoom = 3.5f;

        // Assert
        camera.Zoom.Should().Be(3.5f);
    }

    #endregion

    #region Rotation Tests

    [Fact]
    public void ShouldSetAndGetRotation()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        camera.Rotation = 45f;

        // Assert
        camera.Rotation.Should().Be(45f);
    }

    [Fact]
    public void ShouldAllowNegativeRotation()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        camera.Rotation = -90f;

        // Assert
        camera.Rotation.Should().Be(-90f);
    }

    [Fact]
    public void ShouldAllowRotationGreaterThan360()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        camera.Rotation = 450f;

        // Assert
        camera.Rotation.Should().Be(450f); // No automatic normalization
    }

    #endregion

    #region Viewport Tests

    [Fact]
    public void ShouldUpdateViewport()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        camera.SetViewport(1920, 1080);

        // Assert
        camera.ViewportWidth.Should().Be(1920);
        camera.ViewportHeight.Should().Be(1080);
    }

    [Fact]
    public void ShouldCalculateViewportCenter()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        var center = camera.ViewportCenter;

        // Assert
        center.X.Should().Be(400);
        center.Y.Should().Be(300);
    }

    [Fact]
    public void ShouldUpdateViewportCenterAfterResize()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        camera.SetViewport(1000, 800);
        var center = camera.ViewportCenter;

        // Assert
        center.X.Should().Be(500);
        center.Y.Should().Be(400);
    }

    #endregion

    #region Coordinate Transformation Tests

    [Fact]
    public void ShouldTransformWorldToScreen()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = Vector2.Zero;
        camera.Zoom = 1.0f;

        // Act
        var screenPos = camera.WorldToScreen(Vector2.Zero);

        // Assert - World origin should be at viewport center
        screenPos.X.Should().BeApproximately(400, 0.01f);
        screenPos.Y.Should().BeApproximately(300, 0.01f);
    }

    [Fact]
    public void ShouldTransformScreenToWorld()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = Vector2.Zero;
        camera.Zoom = 1.0f;

        // Act
        var worldPos = camera.ScreenToWorld(new Vector2(400, 300)); // Viewport center

        // Assert - Viewport center should map to world origin
        worldPos.X.Should().BeApproximately(0, 0.01f);
        worldPos.Y.Should().BeApproximately(0, 0.01f);
    }

    [Fact]
    public void ShouldTransformWithCameraOffset()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = new Vector2(100, 50);
        camera.Zoom = 1.0f;

        // Act
        var screenPos = camera.WorldToScreen(new Vector2(100, 50));

        // Assert - Camera position should be at viewport center
        screenPos.X.Should().BeApproximately(400, 0.01f);
        screenPos.Y.Should().BeApproximately(300, 0.01f);
    }

    [Fact]
    public void ShouldTransformWithZoom()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = Vector2.Zero;
        camera.Zoom = 2.0f;

        // Act - A point 100 units from origin should appear 200 pixels from center (2x zoom)
        var screenPos = camera.WorldToScreen(new Vector2(100, 0));

        // Assert
        screenPos.X.Should().BeApproximately(600, 1f); // 400 (center) + 200 (100 * 2)
        screenPos.Y.Should().BeApproximately(300, 1f);
    }

    [Fact]
    public void ShouldRoundTripWorldScreenWorld()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = new Vector2(123, 456);
        camera.Zoom = 1.5f;
        var originalWorld = new Vector2(789, 321);

        // Act - Convert to screen and back
        var screenPos = camera.WorldToScreen(originalWorld);
        var backToWorld = camera.ScreenToWorld(screenPos);

        // Assert - Should get back original position
        backToWorld.X.Should().BeApproximately(originalWorld.X, 0.1f);
        backToWorld.Y.Should().BeApproximately(originalWorld.Y, 0.1f);
    }

    #endregion

    #region View Matrix Tests

    [Fact]
    public void ShouldGenerateViewMatrix()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        var viewMatrix = camera.GetViewMatrix();

        // Assert
        viewMatrix.Should().NotBeNull();
        viewMatrix.IsIdentity.Should().BeFalse(); // Should have transformations
    }

    [Fact]
    public void ShouldUpdateViewMatrixWhenCameraChanges()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        var matrix1 = camera.GetViewMatrix();

        // Act - Change camera position
        camera.Position = new Vector2(100, 100);
        var matrix2 = camera.GetViewMatrix();

        // Assert
        matrix1.Should().NotBe(matrix2);
    }

    #endregion

    #region Edge Cases

    [Fact]
    public void ShouldHandleZeroViewportDimensions()
    {
        // Arrange & Act
        var camera = new Camera2D(0, 0);

        // Assert
        camera.ViewportWidth.Should().Be(0);
        camera.ViewportHeight.Should().Be(0);
        camera.ViewportCenter.Should().Be(Vector2.Zero);
    }

    [Fact]
    public void ShouldHandleVerySmallZoom()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        camera.Zoom = 0.001f;

        // Assert
        camera.Zoom.Should().Be(0.01f); // Clamped
    }

    [Fact]
    public void ShouldHandleVeryLargeZoom()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        camera.Zoom = 1000f;

        // Assert
        camera.Zoom.Should().Be(1000f); // No upper limit
    }

    [Fact]
    public void ShouldHandleLerpWithZeroSmoothing()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = Vector2.Zero;

        // Act
        camera.LerpTo(new Vector2(100, 100), 0f);

        // Assert - Should not move
        camera.Position.Should().Be(Vector2.Zero);
    }

    [Fact]
    public void ShouldHandleLerpWithFullSmoothing()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = Vector2.Zero;

        // Act
        camera.LerpTo(new Vector2(100, 100), 1f);

        // Assert - Should move fully to target
        camera.Position.Should().Be(new Vector2(100, 100));
    }

    #endregion
}