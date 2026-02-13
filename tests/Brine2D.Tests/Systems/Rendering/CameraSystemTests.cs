using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Rendering;
using Brine2D.Systems.Rendering;
using NSubstitute;

namespace Brine2D.Tests.Systems.Rendering;

public class CameraSystemTests : TestBase
{
    [Fact]
    public void Update_FollowsEntityWithCameraFollowComponent()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockCameraManager = Substitute.For<ICameraManager>();
        var mockCamera = Substitute.For<ICamera>();
        mockCamera.Position.Returns(Vector2.Zero);
        mockCameraManager.GetCamera("main").Returns(mockCamera);

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 50))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.Smoothing = 0; // Instant follow for testing
            });

        world.Flush();

        var system = new CameraSystem(mockCameraManager);
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        // Act
        system.Update(gameTime, world);

        // Assert
        mockCamera.Received(1).Position = new Vector2(100, 50);
    }

    [Fact]
    public void Update_WithOffset_AppliesOffset()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockCameraManager = Substitute.For<ICameraManager>();
        var mockCamera = Substitute.For<ICamera>();
        mockCamera.Position.Returns(Vector2.Zero);
        mockCameraManager.GetCamera("main").Returns(mockCamera);

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 100))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.Offset = new Vector2(10, -5);
                c.Smoothing = 0;
            });

        world.Flush();

        var system = new CameraSystem(mockCameraManager);
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        // Act
        system.Update(gameTime, world);

        // Assert
        mockCamera.Received(1).Position = new Vector2(110, 95);
    }

    [Fact]
    public void Update_FollowXOnly_OnlyFollowsXAxis()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockCameraManager = Substitute.For<ICameraManager>();
        var mockCamera = Substitute.For<ICamera>();
        mockCamera.Position.Returns(new Vector2(0, 50)); // Y at 50
        mockCameraManager.GetCamera("main").Returns(mockCamera);

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 200))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.FollowX = true;
                c.FollowY = false; // Don't follow Y
                c.Smoothing = 0;
            });

        world.Flush();

        var system = new CameraSystem(mockCameraManager);
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        // Act
        system.Update(gameTime, world);

        // Assert - X should update, Y should stay at 50
        mockCamera.Received(1).Position = new Vector2(100, 50);
    }

    [Fact]
    public void Update_FollowYOnly_OnlyFollowsYAxis()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockCameraManager = Substitute.For<ICameraManager>();
        var mockCamera = Substitute.For<ICamera>();
        mockCamera.Position.Returns(new Vector2(50, 0)); // X at 50
        mockCameraManager.GetCamera("main").Returns(mockCamera);

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(200, 100))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.FollowX = false; // Don't follow X
                c.FollowY = true;
                c.Smoothing = 0;
            });

        world.Flush();

        var system = new CameraSystem(mockCameraManager);
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        // Act
        system.Update(gameTime, world);

        // Assert - Y should update, X should stay at 50
        mockCamera.Received(1).Position = new Vector2(50, 100);
    }

    [Fact]
    public void Update_WithDeadzone_DoesNotMoveIfWithinDeadzone()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockCameraManager = Substitute.For<ICameraManager>();
        var mockCamera = Substitute.For<ICamera>();
        mockCamera.Position.Returns(new Vector2(100, 100));
        mockCameraManager.GetCamera("main").Returns(mockCamera);

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(105, 105)) // 5 units away
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.Deadzone = new Vector2(10, 10); // 10 unit deadzone
                c.Smoothing = 0;
            });

        world.Flush();

        var system = new CameraSystem(mockCameraManager);
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        // Act
        system.Update(gameTime, world);

        // Assert - Camera should stay at (100, 100) because target is within deadzone
        mockCamera.Received(1).Position = new Vector2(100, 100);
    }

    [Fact]
    public void Update_MultipleTargets_FollowsHighestPriority()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockCameraManager = Substitute.For<ICameraManager>();
        var mockCamera = Substitute.For<ICamera>();
        mockCamera.Position.Returns(Vector2.Zero);
        mockCameraManager.GetCamera("main").Returns(mockCamera);

        var lowPriority = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(50, 50))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.Priority = 1;
                c.Smoothing = 0;
            });

        var highPriority = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 100))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.Priority = 10; // Higher priority
                c.Smoothing = 0;
            });

        world.Flush();

        var system = new CameraSystem(mockCameraManager);
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        // Act
        system.Update(gameTime, world);

        // Assert - Should follow high priority target
        mockCamera.Received(1).Position = new Vector2(100, 100);
    }

    [Fact]
    public void Update_DifferentCameraNames_ControlsDifferentCameras()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockCameraManager = Substitute.For<ICameraManager>();
        
        var mainCamera = Substitute.For<ICamera>();
        mainCamera.Position.Returns(Vector2.Zero);
        mockCameraManager.GetCamera("main").Returns(mainCamera);

        var minimapCamera = Substitute.For<ICamera>();
        minimapCamera.Position.Returns(Vector2.Zero);
        mockCameraManager.GetCamera("minimap").Returns(minimapCamera);

        var mainTarget = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 100))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.CameraName = "main";
                c.Smoothing = 0;
            });

        var minimapTarget = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(200, 200))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.CameraName = "minimap";
                c.Smoothing = 0;
            });

        world.Flush();

        var system = new CameraSystem(mockCameraManager);
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        // Act
        system.Update(gameTime, world);

        // Assert
        mainCamera.Received(1).Position = new Vector2(100, 100);
        minimapCamera.Received(1).Position = new Vector2(200, 200);
    }

    [Fact]
    public void Update_InactiveFollow_Ignored()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockCameraManager = Substitute.For<ICameraManager>();
        var mockCamera = Substitute.For<ICamera>();
        mockCameraManager.GetCamera("main").Returns(mockCamera);

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 100))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.IsActive = false; // Inactive
                c.Smoothing = 0;
            });

        world.Flush();

        var system = new CameraSystem(mockCameraManager);
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        // Act
        system.Update(gameTime, world);

        // Assert - Camera position should not be set
        mockCamera.DidNotReceive().Position = Arg.Any<Vector2>();
    }

    [Fact]
    public void Update_NoTransform_Ignored()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockCameraManager = Substitute.For<ICameraManager>();
        var mockCamera = Substitute.For<ICamera>();
        mockCameraManager.GetCamera("main").Returns(mockCamera);

        var entity = world.CreateEntity()
            .AddComponent<CameraFollowComponent>(c => c.Smoothing = 0);
        // No TransformComponent

        world.Flush();

        var system = new CameraSystem(mockCameraManager);
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        // Act
        system.Update(gameTime, world);

        // Assert
        mockCamera.DidNotReceive().Position = Arg.Any<Vector2>();
    }

    [Fact]
    public void Update_CameraNotFound_Ignored()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockCameraManager = Substitute.For<ICameraManager>();
        mockCameraManager.GetCamera("nonexistent").Returns((ICamera?)null);

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 100))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.CameraName = "nonexistent";
                c.Smoothing = 0;
            });

        world.Flush();

        var system = new CameraSystem(mockCameraManager);
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        // Act - Should not throw
        system.Update(gameTime, world);

        // Assert - No exception
        Assert.True(true);
    }

    [Fact]
    public void UpdateOrder_IsCorrect()
    {
        // Arrange
        var mockCameraManager = Substitute.For<ICameraManager>();
        var system = new CameraSystem(mockCameraManager);

        // Act & Assert
        Assert.Equal(500, system.UpdateOrder);
    }

    [Fact]
    public void Name_IsCorrect()
    {
        // Arrange
        var mockCameraManager = Substitute.For<ICameraManager>();
        var system = new CameraSystem(mockCameraManager);

        // Act & Assert
        Assert.Equal("CameraSystem", system.Name);
    }
}