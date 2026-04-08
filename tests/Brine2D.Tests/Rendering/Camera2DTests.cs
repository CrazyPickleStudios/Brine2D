using System.Numerics;
using Brine2D.Core;
using Brine2D.Rendering;
using FluentAssertions;
using NSubstitute;
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
        camera.Zoom.Should().Be(0.01f);
    }

    [Fact]
    public void ShouldClampNegativeZoom()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        camera.Zoom = -5f;

        // Assert
        camera.Zoom.Should().Be(0.01f);
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

    [Fact]
    public void ShouldClampZoomToMaximum()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        camera.Zoom = 1000f;

        // Assert
        camera.Zoom.Should().Be(100f);
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
        camera.Rotation.Should().Be(450f);
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

    [Fact]
    public void ShouldClampNegativeViewportDimensionsToZero()
    {
        // Arrange & Act
        var camera = new Camera2D(-100, -200);

        // Assert
        camera.ViewportWidth.Should().Be(0);
        camera.ViewportHeight.Should().Be(0);
    }

    [Fact]
    public void SetViewport_ShouldClampNegativeDimensionsToZero()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        camera.SetViewport(-50, -10);

        // Assert
        camera.ViewportWidth.Should().Be(0);
        camera.ViewportHeight.Should().Be(0);
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
        screenPos.X.Should().BeApproximately(600, 1f);
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

        // Act
        var screenPos = camera.WorldToScreen(originalWorld);
        var backToWorld = camera.ScreenToWorld(screenPos);

        // Assert
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
        viewMatrix.IsIdentity.Should().BeFalse();
    }

    [Fact]
    public void ShouldUpdateViewMatrixWhenCameraChanges()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        var matrix1 = camera.GetViewMatrix();

        // Act
        camera.Position = new Vector2(100, 100);
        var matrix2 = camera.GetViewMatrix();

        // Assert
        matrix1.Should().NotBe(matrix2);
    }

    [Fact]
    public void ShouldCacheViewMatrixWhenUnchanged()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = new Vector2(50, 50);

        // Act
        var matrix1 = camera.GetViewMatrix();
        var matrix2 = camera.GetViewMatrix();

        // Assert
        matrix1.Should().Be(matrix2);
    }

    [Fact]
    public void ShouldInvalidateCacheOnMove()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        var matrix1 = camera.GetViewMatrix();

        // Act
        camera.Move(new Vector2(10, 0));
        var matrix2 = camera.GetViewMatrix();

        // Assert
        matrix1.Should().NotBe(matrix2);
    }

    [Fact]
    public void ShouldInvalidateCacheOnSetViewport()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        var matrix1 = camera.GetViewMatrix();

        // Act
        camera.SetViewport(1024, 768);
        var matrix2 = camera.GetViewMatrix();

        // Assert
        matrix1.Should().NotBe(matrix2);
    }

    #endregion

    #region Smooth Follow Tests

    [Fact]
    public void FollowSmooth_ShouldSnapWhenSmoothingIsZero()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = Vector2.Zero;

        // Act
        camera.FollowSmooth(new Vector2(500, 300), smoothing: 0f, deltaTime: 0.016f);

        // Assert
        camera.Position.Should().Be(new Vector2(500, 300));
    }

    [Fact]
    public void FollowSmooth_ShouldMoveTowrdTargetWithSmoothing()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = Vector2.Zero;
        var target = new Vector2(100, 0);

        // Act
        camera.FollowSmooth(target, smoothing: 5f, deltaTime: 1f / 60f);

        // Assert
        camera.Position.X.Should().BeGreaterThan(0f);
        camera.Position.X.Should().BeLessThan(target.X);
    }

    [Fact]
    public void FollowSmooth_ShouldConvergeOverMultipleFrames()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = Vector2.Zero;
        var target = new Vector2(100, 100);

        // Act
        for (var i = 0; i < 600; i++)
            camera.FollowSmooth(target, smoothing: 5f, deltaTime: 1f / 60f);

        // Assert
        camera.Position.X.Should().BeApproximately(target.X, 0.01f);
        camera.Position.Y.Should().BeApproximately(target.Y, 0.01f);
    }

    #endregion

    #region Smooth Zoom Tests

    [Fact]
    public void ZoomSmooth_ShouldSnapWhenSmoothingIsZero()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        camera.ZoomSmooth(targetZoom: 3f, smoothing: 0f, deltaTime: 0.016f);

        // Assert
        camera.Zoom.Should().Be(3f);
    }

    [Fact]
    public void ZoomSmooth_ShouldApproachTargetWithSmoothing()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Zoom = 1f;

        // Act
        camera.ZoomSmooth(targetZoom: 4f, smoothing: 5f, deltaTime: 1f / 60f);

        // Assert
        camera.Zoom.Should().BeGreaterThan(1f);
        camera.Zoom.Should().BeLessThan(4f);
    }

    #endregion

    #region Shake Tests

    [Fact]
    public void Shake_ShouldProduceOffsetDuringDuration()
    {
        // Arrange
        var camera = new Camera2D(800, 600, shakeSeed: 42);
        camera.Shake(20f, 1f);

        // Act
        camera.UpdateShake(0.1f);
        var matrixDuringShake = camera.GetViewMatrix();

        // Assert - View matrix should differ from a shake-free camera at the same position
        var clean = new Camera2D(800, 600);
        var matrixNoShake = clean.GetViewMatrix();
        matrixDuringShake.Should().NotBe(matrixNoShake);
    }

    [Fact]
    public void Shake_ShouldDecayToZeroAfterDuration()
    {
        // Arrange
        var camera = new Camera2D(800, 600, shakeSeed: 42);
        camera.Shake(20f, 0.5f);

        // Act
        camera.UpdateShake(1f);
        var matrixAfterShake = camera.GetViewMatrix();

        // Assert - Offset should be zero, matching a clean camera
        var clean = new Camera2D(800, 600);
        var matrixNoShake = clean.GetViewMatrix();
        matrixAfterShake.Should().Be(matrixNoShake);
    }

    [Fact]
    public void Shake_StrongerShakeShouldOverrideWeaker()
    {
        // Arrange
        var camera = new Camera2D(800, 600, shakeSeed: 42);
        camera.Shake(5f, 1f);
        camera.UpdateShake(0.1f);

        // Act
        camera.Shake(50f, 1f);
        camera.UpdateShake(0.01f);
        var matrixStrong = camera.GetViewMatrix();

        // Assert - The view matrix should reflect the stronger shake
        var weak = new Camera2D(800, 600, shakeSeed: 42);
        weak.Shake(5f, 1f);
        weak.UpdateShake(0.11f);
        var matrixWeak = weak.GetViewMatrix();

        matrixStrong.Should().NotBe(matrixWeak);
    }

    [Fact]
    public void Shake_DeterministicWithSameSeed()
    {
        // Arrange
        var camera1 = new Camera2D(800, 600, shakeSeed: 99);
        var camera2 = new Camera2D(800, 600, shakeSeed: 99);

        // Act
        camera1.Shake(10f, 1f);
        camera1.UpdateShake(0.05f);
        camera2.Shake(10f, 1f);
        camera2.UpdateShake(0.05f);

        // Assert
        camera1.GetViewMatrix().Should().Be(camera2.GetViewMatrix());
    }

    [Fact]
    public void CancelShake_ShouldResetShakeOffset()
    {
        // Arrange
        var camera = new Camera2D(800, 600, shakeSeed: 42);
        camera.Shake(20f, 1f);
        camera.UpdateShake(0.1f);

        var clean = new Camera2D(800, 600);
        var matrixNoShake = clean.GetViewMatrix();

        // Act
        camera.CancelShake();
        camera.UpdateShake(0f);

        // Assert
        camera.GetViewMatrix().Should().Be(matrixNoShake);
    }

    [Fact]
    public void CancelShake_ShouldBeIdempotentWithNoActiveShake()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        var matrixBefore = camera.GetViewMatrix();

        // Act
        camera.CancelShake();

        // Assert
        camera.GetViewMatrix().Should().Be(matrixBefore);
    }

    #endregion

    #region Visible Bounds Tests

    [Fact]
    public void GetVisibleBounds_ShouldReturnViewportSizedRectAtDefaultZoom()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = new Vector2(400, 300);

        // Act
        var bounds = camera.GetVisibleBounds();

        // Assert
        bounds.Width.Should().BeApproximately(800, 0.01f);
        bounds.Height.Should().BeApproximately(600, 0.01f);
        bounds.Center.X.Should().BeApproximately(400, 0.01f);
        bounds.Center.Y.Should().BeApproximately(300, 0.01f);
    }

    [Fact]
    public void GetVisibleBounds_ShouldShrinkWithHigherZoom()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = Vector2.Zero;
        camera.Zoom = 2f;

        // Act
        var bounds = camera.GetVisibleBounds();

        // Assert
        bounds.Width.Should().BeApproximately(400, 0.01f);
        bounds.Height.Should().BeApproximately(300, 0.01f);
    }

    [Fact]
    public void GetVisibleBounds_ShouldExpandWithRotation()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = Vector2.Zero;
        var boundsNoRotation = camera.GetVisibleBounds();

        // Act
        camera.Rotation = 30f;
        var boundsRotated = camera.GetVisibleBounds();

        // Assert
        boundsRotated.Width.Should().BeGreaterThan(boundsNoRotation.Width);
        boundsRotated.Height.Should().BeGreaterThan(boundsNoRotation.Height);
    }

    #endregion

    #region Visibility Tests

    [Fact]
    public void IsVisible_ShouldReturnTrueForPointInsideBounds()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = Vector2.Zero;

        // Act & Assert
        camera.IsVisible(new Vector2(10, 10)).Should().BeTrue();
    }

    [Fact]
    public void IsVisible_ShouldReturnFalseForPointOutsideBounds()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = Vector2.Zero;

        // Act & Assert
        camera.IsVisible(new Vector2(5000, 5000)).Should().BeFalse();
    }

    [Fact]
    public void IsVisible_ShouldReturnTrueForFractionalPointOnEdge()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = Vector2.Zero;
        var bounds = camera.GetVisibleBounds();

        // Act - A point just inside the left-top edge
        var point = new Vector2(bounds.X + 0.5f, bounds.Y + 0.5f);

        // Assert
        camera.IsVisible(point).Should().BeTrue();
    }

    [Fact]
    public void IsVisible_ShouldReturnTrueForOverlappingRectangle()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = Vector2.Zero;

        // Act & Assert
        camera.IsVisible(new Rectangle(-10, -10, 100, 100)).Should().BeTrue();
    }

    [Fact]
    public void IsVisible_ShouldReturnFalseForDistantRectangle()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = Vector2.Zero;

        // Act & Assert
        camera.IsVisible(new Rectangle(5000, 5000, 10, 10)).Should().BeFalse();
    }

    #endregion

    #region Clamp Tests

    [Fact]
    public void ClampToBounds_ShouldNotMoveCameraWithinBounds()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = new Vector2(500, 400);
        var worldBounds = new Rectangle(0, 0, 2000, 2000);

        // Act
        camera.ClampToBounds(worldBounds);

        // Assert
        camera.Position.X.Should().Be(500);
        camera.Position.Y.Should().Be(400);
    }

    [Fact]
    public void ClampToBounds_ShouldClampCameraOutsideBounds()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = new Vector2(-500, -500);
        var worldBounds = new Rectangle(0, 0, 2000, 2000);

        // Act
        camera.ClampToBounds(worldBounds);

        // Assert
        camera.Position.X.Should().BeGreaterThanOrEqualTo(0f);
        camera.Position.Y.Should().BeGreaterThanOrEqualTo(0f);
    }

    [Fact]
    public void ClampToBounds_ShouldClampToRightEdge()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = new Vector2(5000, 300);
        var worldBounds = new Rectangle(0, 0, 2000, 2000);

        // Act
        camera.ClampToBounds(worldBounds);

        // Assert
        var visibleBounds = camera.GetVisibleBounds();
        visibleBounds.Right.Should().BeLessThanOrEqualTo(2000f + 0.01f);
    }

    [Fact]
    public void ClampToBounds_ShouldCenterWhenWorldSmallerThanViewport()
    {
        // Arrange
        var camera = new Camera2D(800, 600);
        camera.Position = new Vector2(5000, 5000);
        var worldBounds = new Rectangle(0, 0, 400, 300);

        // Act
        camera.ClampToBounds(worldBounds);

        // Assert
        camera.Position.X.Should().BeApproximately(200f, 0.01f);
        camera.Position.Y.Should().BeApproximately(150f, 0.01f);
    }

    [Fact]
    public void ClampToBounds_ShouldCenterAxisIndependently()
    {
        // Arrange - viewport wider than world but shorter
        var camera = new Camera2D(800, 200);
        camera.Position = new Vector2(5000, 5000);
        var worldBounds = new Rectangle(0, 0, 400, 2000);

        // Act
        camera.ClampToBounds(worldBounds);

        // Assert - X centers because viewport is wider, Y clamps normally
        camera.Position.X.Should().BeApproximately(200f, 0.01f);
        camera.Position.Y.Should().BeLessThanOrEqualTo(2000f - 100f + 0.01f);
    }

    #endregion

    #region Registration and Disposal Tests

    [Fact]
    public void Dispose_ShouldUnregisterFromManager()
    {
        // Arrange
        var mockManager = Substitute.For<ICameraManager>();
        var camera = new Camera2D(800, 600);
        camera.TrackRegistration(mockManager, "test");
        mockManager.GetCamera("test").Returns(camera);

        // Act
        camera.Dispose();

        // Assert
        mockManager.Received(1).RemoveCamera("test");
    }

    [Fact]
    public void Dispose_ShouldNotThrowWithoutRegistration()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act & Assert
        var act = () => camera.Dispose();
        act.Should().NotThrow();
    }

    [Fact]
    public void Dispose_ShouldBeIdempotent()
    {
        // Arrange
        var mockManager = Substitute.For<ICameraManager>();
        var camera = new Camera2D(800, 600);
        camera.TrackRegistration(mockManager, "test");
        mockManager.GetCamera("test").Returns(camera);

        // Act
        camera.Dispose();
        camera.Dispose();

        // Assert
        mockManager.Received(1).RemoveCamera("test");
    }

    [Fact]
    public void Dispose_ShouldNotRemoveReplacementCamera()
    {
        // Arrange
        var mockManager = Substitute.For<ICameraManager>();
        var oldCamera = new Camera2D(800, 600);
        var newCamera = new Camera2D(800, 600);
        oldCamera.TrackRegistration(mockManager, "main");
        newCamera.TrackRegistration(mockManager, "main");
        mockManager.GetCamera("main").Returns(newCamera);

        // Act
        oldCamera.Dispose();

        // Assert
        mockManager.DidNotReceive().RemoveCamera("main");
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
        camera.Zoom.Should().Be(0.01f);
    }

    [Fact]
    public void ShouldHandleVeryLargeZoom()
    {
        // Arrange
        var camera = new Camera2D(800, 600);

        // Act
        camera.Zoom = 100f;

        // Assert
        camera.Zoom.Should().Be(100f);
    }

    #endregion
}