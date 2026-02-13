using Brine2D.ECS;
using Brine2D.ECS.Systems;
using Brine2D.Rendering;
using NSubstitute;

namespace Brine2D.Tests.ECS.Systems;

public class RenderPipelineTests : TestBase
{
    #region AddSystem

    [Fact]
    public void AddSystem_AddsSystemToPipeline()
    {
        // Arrange
        var pipeline = new RenderPipeline();
        var system = new TestRenderSystem();

        // Act
        pipeline.AddSystem(system);

        // Assert
        Assert.Single(pipeline.Systems);
        Assert.Contains(system, pipeline.Systems);
    }

    [Fact]
    public void AddSystem_MultipleSystem_AddsAll()
    {
        // Arrange
        var pipeline = new RenderPipeline();
        var system1 = new TestRenderSystem();
        var system2 = new TestRenderSystem();
        var system3 = new TestRenderSystem();

        // Act
        pipeline.AddSystem(system1)
                .AddSystem(system2)
                .AddSystem(system3);

        // Assert
        Assert.Equal(3, pipeline.Systems.Count);
    }

    [Fact]
    public void AddSystem_DuringExecution_DefersAddition()
    {
        // Arrange
        var pipeline = new RenderPipeline();
        var world = CreateTestWorld();
        var renderer = Substitute.For<IRenderer>();

        var system1 = new TestRenderSystem();
        var system2 = new TestRenderSystem();

        // System1 will try to add system2 during execution
        system1.OnRenderAction = (r, w) =>
        {
            pipeline.AddSystem(system2);
        };

        pipeline.AddSystem(system1);

        // Act
        pipeline.Execute(renderer, world);

        // Assert - system2 should be added after execution completes
        Assert.Equal(2, pipeline.Systems.Count);
        Assert.Contains(system2, pipeline.Systems);
    }

    #endregion

    #region RemoveSystem

    [Fact]
    public void RemoveSystem_RemovesSystemFromPipeline()
    {
        // Arrange
        var pipeline = new RenderPipeline();
        var system = new TestRenderSystem();
        pipeline.AddSystem(system);

        // Act
        var result = pipeline.RemoveSystem(system);

        // Assert
        Assert.True(result);
        Assert.Empty(pipeline.Systems);
    }

    [Fact]
    public void RemoveSystem_NonExistentSystem_ReturnsFalse()
    {
        // Arrange
        var pipeline = new RenderPipeline();
        var system = new TestRenderSystem();

        // Act
        var result = pipeline.RemoveSystem(system);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RemoveSystem_DuringExecution_DefersRemoval()
    {
        // Arrange
        var pipeline = new RenderPipeline();
        var world = CreateTestWorld();
        var renderer = Substitute.For<IRenderer>();

        var system1 = new TestRenderSystem();
        var system2 = new TestRenderSystem();

        // System1 will try to remove system2 during execution
        system1.OnRenderAction = (r, w) =>
        {
            pipeline.RemoveSystem(system2);
        };

        pipeline.AddSystem(system1).AddSystem(system2);

        // Act
        pipeline.Execute(renderer, world);

        // Assert - system2 should be removed after execution completes
        Assert.Single(pipeline.Systems);
        Assert.DoesNotContain(system2, pipeline.Systems);
    }

    #endregion

    #region Clear

    [Fact]
    public void Clear_RemovesAllSystems()
    {
        // Arrange
        var pipeline = new RenderPipeline();
        pipeline.AddSystem(new TestRenderSystem());
        pipeline.AddSystem(new TestRenderSystem());
        pipeline.AddSystem(new TestRenderSystem());

        // Act
        pipeline.Clear();

        // Assert
        Assert.Empty(pipeline.Systems);
    }

    #endregion

    #region Execute - Ordering

    [Fact]
    public void Execute_SystemsExecuteInOrder()
    {
        // Arrange
        var pipeline = new RenderPipeline();
        var world = CreateTestWorld();
        var renderer = Substitute.For<IRenderer>();

        var executionOrder = new List<int>();

        var system1 = new TestRenderSystem { RenderOrder = 10 };
        system1.OnRenderAction = (r, w) => executionOrder.Add(1);

        var system2 = new TestRenderSystem { RenderOrder = 5 };
        system2.OnRenderAction = (r, w) => executionOrder.Add(2);

        var system3 = new TestRenderSystem { RenderOrder = 15 };
        system3.OnRenderAction = (r, w) => executionOrder.Add(3);

        // Add in random order
        pipeline.AddSystem(system1);
        pipeline.AddSystem(system3);
        pipeline.AddSystem(system2);

        // Act
        pipeline.Execute(renderer, world);

        // Assert - Should execute in RenderOrder: 5, 10, 15 -> 2, 1, 3
        Assert.Equal(new[] { 2, 1, 3 }, executionOrder);
    }

    [Fact]
    public void Execute_CallsRenderOnAllSystems()
    {
        // Arrange
        var pipeline = new RenderPipeline();
        var world = CreateTestWorld();
        var renderer = Substitute.For<IRenderer>();

        var system1 = new TestRenderSystem();
        var system2 = new TestRenderSystem();
        var system3 = new TestRenderSystem();

        pipeline.AddSystem(system1).AddSystem(system2).AddSystem(system3);

        // Act
        pipeline.Execute(renderer, world);

        // Assert
        Assert.True(system1.RenderCalled);
        Assert.True(system2.RenderCalled);
        Assert.True(system3.RenderCalled);
    }

    [Fact]
    public void Execute_PassesCorrectParameters()
    {
        // Arrange
        var pipeline = new RenderPipeline();
        var world = CreateTestWorld();
        var renderer = Substitute.For<IRenderer>();

        var system = new TestRenderSystem();
        pipeline.AddSystem(system);

        // Act
        pipeline.Execute(renderer, world);

        // Assert
        Assert.NotNull(system.LastRenderer);
        Assert.NotNull(system.LastWorld);
        Assert.Equal(renderer, system.LastRenderer);
        Assert.Equal(world, system.LastWorld);
    }

    #endregion

    #region Execute - Error Handling

    [Fact]
    public void Execute_SystemThrowsException_ContinuesExecution()
    {
        // Arrange
        var pipeline = new RenderPipeline();
        var world = CreateTestWorld();
        var renderer = Substitute.For<IRenderer>();

        var system1 = new TestRenderSystem();
        var system2 = new TestRenderSystem();
        var system3 = new TestRenderSystem();

        system2.OnRenderAction = (r, w) => throw new Exception("Test exception");

        pipeline.AddSystem(system1).AddSystem(system2).AddSystem(system3);

        // Act - Should not throw
        pipeline.Execute(renderer, world);

        // Assert - Other systems should still execute
        Assert.True(system1.RenderCalled);
        Assert.True(system3.RenderCalled);
    }

    #endregion

    #region DisableSystems / EnableAllSystems

    [Fact]
    public void DisableSystems_SkipsNamedSystems()
    {
        // Arrange
        var pipeline = new RenderPipeline();
        var world = CreateTestWorld();
        var renderer = Substitute.For<IRenderer>();

        var system1 = new TestRenderSystem();
        var system2 = new TestRenderSystem();
        var system3 = new TestRenderSystem();

        pipeline.AddSystem(system1).AddSystem(system2).AddSystem(system3);

        // Act - Disable by type name
        pipeline.DisableSystems(new[] { "TestRenderSystem" });
        pipeline.Execute(renderer, world);

        // Assert - All should be disabled (same type name)
        Assert.False(system1.RenderCalled);
        Assert.False(system2.RenderCalled);
        Assert.False(system3.RenderCalled);
    }

    [Fact]
    public void EnableAllSystems_ReEnablesDisabledSystems()
    {
        // Arrange
        var pipeline = new RenderPipeline();
        var world = CreateTestWorld();
        var renderer = Substitute.For<IRenderer>();

        var system = new TestRenderSystem();
        pipeline.AddSystem(system);
        pipeline.DisableSystems(new[] { "TestRenderSystem" });

        // Act - Re-enable
        pipeline.EnableAllSystems();
        pipeline.Execute(renderer, world);

        // Assert
        Assert.True(system.RenderCalled);
    }

    #endregion

    #region Systems Property

    [Fact]
    public void Systems_ReturnsReadOnlyList()
    {
        // Arrange
        var pipeline = new RenderPipeline();
        pipeline.AddSystem(new TestRenderSystem());

        // Act
        var systems = pipeline.Systems;

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<IRenderSystem>>(systems);
    }

    [Fact]
    public void Systems_ReflectsCurrentState()
    {
        // Arrange
        var pipeline = new RenderPipeline();

        // Act & Assert - Initially empty
        Assert.Empty(pipeline.Systems);

        // Add system
        var system = new TestRenderSystem();
        pipeline.AddSystem(system);
        Assert.Single(pipeline.Systems);

        // Remove system
        pipeline.RemoveSystem(system);
        Assert.Empty(pipeline.Systems);
    }

    #endregion

    #region Test Helper Systems

    private class TestRenderSystem : IRenderSystem
    {
        public int RenderOrder { get; set; } = 0;
        public bool RenderCalled { get; private set; }
        public IRenderer? LastRenderer { get; private set; }
        public IEntityWorld? LastWorld { get; private set; }
        public Action<IRenderer, IEntityWorld>? OnRenderAction { get; set; }

        public void Render(IRenderer renderer, IEntityWorld world)
        {
            RenderCalled = true;
            LastRenderer = renderer;
            LastWorld = world;
            OnRenderAction?.Invoke(renderer, world);
        }
    }

    #endregion
}