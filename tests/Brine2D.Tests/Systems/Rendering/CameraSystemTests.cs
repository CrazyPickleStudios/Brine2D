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

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 50))
            .AddComponent<CameraFollowComponent>(c => c.Smoothing = 0);

        world.Flush();

        var system = new CameraSystem(mockCameraManager);

        // Act
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

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

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 100))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.Offset = new Vector2(10, -5);
                c.Smoothing = 0;
            });

        world.Flush();

        var system = new CameraSystem(mockCameraManager);

        // Act
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

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
        mockCamera.Position.Returns(new Vector2(0, 50));
        mockCameraManager.GetCamera("main").Returns(mockCamera);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 200))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.FollowX = true;
                c.FollowY = false;
                c.Smoothing = 0;
            });

        world.Flush();

        var system = new CameraSystem(mockCameraManager);

        // Act
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        // Assert
        mockCamera.Received(1).Position = new Vector2(100, 50);
    }

    [Fact]
    public void Update_FollowYOnly_OnlyFollowsYAxis()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockCameraManager = Substitute.For<ICameraManager>();
        var mockCamera = Substitute.For<ICamera>();
        mockCamera.Position.Returns(new Vector2(50, 0));
        mockCameraManager.GetCamera("main").Returns(mockCamera);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(200, 100))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.FollowX = false;
                c.FollowY = true;
                c.Smoothing = 0;
            });

        world.Flush();

        var system = new CameraSystem(mockCameraManager);

        // Act
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        // Assert
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

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(105, 105))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.Deadzone = new Vector2(10, 10);
                c.Smoothing = 0;
            });

        world.Flush();

        var system = new CameraSystem(mockCameraManager);

        // Act
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        // Assert
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

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(50, 50))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.Priority = 1;
                c.Smoothing = 0;
            });

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 100))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.Priority = 10;
                c.Smoothing = 0;
            });

        world.Flush();

        var system = new CameraSystem(mockCameraManager);

        // Act
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        // Assert
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

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 100))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.CameraName = "main";
                c.Smoothing = 0;
            });

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(200, 200))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.CameraName = "minimap";
                c.Smoothing = 0;
            });

        world.Flush();

        var system = new CameraSystem(mockCameraManager);

        // Act
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

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

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 100))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.IsActive = false;
                c.Smoothing = 0;
            });

        world.Flush();

        var system = new CameraSystem(mockCameraManager);

        // Act
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        // Assert
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

        world.CreateEntity()
            .AddComponent<CameraFollowComponent>(c => c.Smoothing = 0);

        world.Flush();

        var system = new CameraSystem(mockCameraManager);

        // Act
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));

        // Assert
        mockCamera.DidNotReceive().Position = Arg.Any<Vector2>();
    }

    [Fact]
    public void Update_CameraNotFound_DoesNotThrow()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockCameraManager = Substitute.For<ICameraManager>();
        mockCameraManager.GetCamera("nonexistent").Returns((ICamera?)null);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 100))
            .AddComponent<CameraFollowComponent>(c =>
            {
                c.CameraName = "nonexistent";
                c.Smoothing = 0;
            });

        world.Flush();

        var system = new CameraSystem(mockCameraManager);

        // Act & Assert
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016)));
    }

    [Fact]
    public void UpdateOrder_IsCorrect()
    {
        var system = new CameraSystem(Substitute.For<ICameraManager>());
        Assert.Equal(500, system.UpdateOrder);
    }

    [Fact]
    public void Name_IsCorrect()
    {
        var system = new CameraSystem(Substitute.For<ICameraManager>());
        Assert.Equal("CameraSystem", system.Name);
    }
}