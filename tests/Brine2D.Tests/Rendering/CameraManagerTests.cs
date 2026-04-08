using Brine2D.Rendering;
using FluentAssertions;
using NSubstitute;
using Xunit;

namespace Brine2D.Tests.Rendering;

public class CameraManagerTests
{
    [Fact]
    public void ForEachCamera_RemoveDuringIteration_DoesNotThrow()
    {
        // Arrange
        var manager = new CameraManager();
        var camera1 = Substitute.For<ICamera>();
        var camera2 = Substitute.For<ICamera>();
        manager.RegisterCamera("a", camera1);
        manager.RegisterCamera("b", camera2);

        // Act & Assert
        var act = () => manager.ForEachCamera(manager, static (mgr, _) => mgr.RemoveCamera("a"));
        act.Should().NotThrow();
    }

    [Fact]
    public void ForEachCamera_AddDuringIteration_DoesNotThrow()
    {
        // Arrange
        var manager = new CameraManager();
        var camera1 = Substitute.For<ICamera>();
        var cameraToAdd = Substitute.For<ICamera>();
        manager.RegisterCamera("a", camera1);

        // Act & Assert
        var act = () => manager.ForEachCamera((mgr: manager, cam: cameraToAdd), static (state, _) =>
            state.mgr.RegisterCamera("added", state.cam));
        act.Should().NotThrow();
    }

    [Fact]
    public void ForEachCamera_VisitsAllCameras()
    {
        // Arrange
        var manager = new CameraManager();
        var camera1 = Substitute.For<ICamera>();
        var camera2 = Substitute.For<ICamera>();
        manager.RegisterCamera("a", camera1);
        manager.RegisterCamera("b", camera2);
        var visited = new List<ICamera>();

        // Act
        manager.ForEachCamera(visited, static (list, camera) => list.Add(camera));

        // Assert
        visited.Should().HaveCount(2);
        visited.Should().Contain(camera1);
        visited.Should().Contain(camera2);
    }

    [Fact]
    public void ForEachCamera_EmptyManager_DoesNotInvokeAction()
    {
        // Arrange
        var manager = new CameraManager();
        var invoked = false;

        // Act
        manager.ForEachCamera(0, (_, _) => invoked = true);

        // Assert
        invoked.Should().BeFalse();
    }

    [Fact]
    public void RegisterCamera_DisposingOldCamera_ShouldNotRemoveReplacement()
    {
        // Arrange
        var manager = new CameraManager();
        var oldCamera = new Camera2D(800, 600);
        var newCamera = new Camera2D(800, 600);
        manager.RegisterCamera("main", oldCamera);

        // Act
        manager.RegisterCamera("main", newCamera);
        oldCamera.Dispose();

        // Assert
        manager.GetCamera("main").Should().BeSameAs(newCamera);
        manager.MainCamera.Should().BeSameAs(newCamera);
    }
}