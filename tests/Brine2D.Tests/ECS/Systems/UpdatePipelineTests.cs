using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Systems;
using NSubstitute;

namespace Brine2D.Tests.ECS.Systems;

public class UpdatePipelineTests : TestBase
{
    #region AddSystem

    [Fact]
    public void AddSystem_AddsSystemToPipeline()
    {
        // Arrange
        var pipeline = new UpdatePipeline();
        var system = new TestUpdateSystem();

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
        var pipeline = new UpdatePipeline();
        var system1 = new TestUpdateSystem();
        var system2 = new TestUpdateSystem();
        var system3 = new TestUpdateSystem();

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
        var pipeline = new UpdatePipeline();
        var world = CreateTestWorld();
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        var system1 = new TestUpdateSystem();
        var system2 = new TestUpdateSystem();

        // System1 will try to add system2 during execution
        system1.OnUpdateAction = (gt, w) =>
        {
            pipeline.AddSystem(system2);
        };

        pipeline.AddSystem(system1);

        // Act
        pipeline.Execute(gameTime, world);

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
        var pipeline = new UpdatePipeline();
        var system = new TestUpdateSystem();
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
        var pipeline = new UpdatePipeline();
        var system = new TestUpdateSystem();

        // Act
        var result = pipeline.RemoveSystem(system);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void RemoveSystem_DuringExecution_DefersRemoval()
    {
        // Arrange
        var pipeline = new UpdatePipeline();
        var world = CreateTestWorld();
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        var system1 = new TestUpdateSystem();
        var system2 = new TestUpdateSystem();

        // System1 will try to remove system2 during execution
        system1.OnUpdateAction = (gt, w) =>
        {
            pipeline.RemoveSystem(system2);
        };

        pipeline.AddSystem(system1).AddSystem(system2);

        // Act
        pipeline.Execute(gameTime, world);

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
        var pipeline = new UpdatePipeline();
        pipeline.AddSystem(new TestUpdateSystem());
        pipeline.AddSystem(new TestUpdateSystem());
        pipeline.AddSystem(new TestUpdateSystem());

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
        var pipeline = new UpdatePipeline();
        var world = CreateTestWorld();
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        var executionOrder = new List<int>();

        var system1 = new TestUpdateSystem { UpdateOrder = 10 };
        system1.OnUpdateAction = (gt, w) => executionOrder.Add(1);

        var system2 = new TestUpdateSystem { UpdateOrder = 5 };
        system2.OnUpdateAction = (gt, w) => executionOrder.Add(2);

        var system3 = new TestUpdateSystem { UpdateOrder = 15 };
        system3.OnUpdateAction = (gt, w) => executionOrder.Add(3);

        // Add in random order
        pipeline.AddSystem(system1);
        pipeline.AddSystem(system3);
        pipeline.AddSystem(system2);

        // Act
        pipeline.Execute(gameTime, world);

        // Assert - Should execute in UpdateOrder: 5, 10, 15 -> 2, 1, 3
        Assert.Equal(new[] { 2, 1, 3 }, executionOrder);
    }

    [Fact]
    public void Execute_CallsUpdateOnAllSystems()
    {
        // Arrange
        var pipeline = new UpdatePipeline();
        var world = CreateTestWorld();
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        var system1 = new TestUpdateSystem();
        var system2 = new TestUpdateSystem();
        var system3 = new TestUpdateSystem();

        pipeline.AddSystem(system1).AddSystem(system2).AddSystem(system3);

        // Act
        pipeline.Execute(gameTime, world);

        // Assert
        Assert.True(system1.UpdateCalled);
        Assert.True(system2.UpdateCalled);
        Assert.True(system3.UpdateCalled);
    }

    [Fact]
    public void Execute_PassesCorrectParameters()
    {
        // Arrange
        var pipeline = new UpdatePipeline();
        var world = CreateTestWorld();
        var gameTime = new GameTime(TimeSpan.FromSeconds(10), TimeSpan.FromSeconds(0.016));

        var system = new TestUpdateSystem();
        pipeline.AddSystem(system);

        // Act
        pipeline.Execute(gameTime, world);

        // Assert
        Assert.NotNull(system.LastGameTime);
        Assert.NotNull(system.LastWorld);
        Assert.Equal(gameTime, system.LastGameTime);
        Assert.Equal(world, system.LastWorld);
    }

    #endregion

    #region Execute - Error Handling

    [Fact]
    public void Execute_SystemThrowsException_ContinuesExecution()
    {
        // Arrange
        var pipeline = new UpdatePipeline();
        var world = CreateTestWorld();
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        var system1 = new TestUpdateSystem();
        var system2 = new TestUpdateSystem();
        var system3 = new TestUpdateSystem();

        system2.OnUpdateAction = (gt, w) => throw new Exception("Test exception");

        pipeline.AddSystem(system1).AddSystem(system2).AddSystem(system3);

        // Act - Should not throw
        pipeline.Execute(gameTime, world);

        // Assert - Other systems should still execute
        Assert.True(system1.UpdateCalled);
        Assert.True(system3.UpdateCalled);
    }

    #endregion

    #region DisableSystems / EnableAllSystems

    [Fact]
    public void DisableSystems_SkipsNamedSystems()
    {
        // Arrange
        var pipeline = new UpdatePipeline();
        var world = CreateTestWorld();
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        var system1 = new TestUpdateSystem { Name = "System1" };
        var system2 = new TestUpdateSystem { Name = "System2" };
        var system3 = new TestUpdateSystem { Name = "System3" };

        pipeline.AddSystem(system1).AddSystem(system2).AddSystem(system3);

        // Act
        pipeline.DisableSystems(new[] { "System2" });
        pipeline.Execute(gameTime, world);

        // Assert
        Assert.True(system1.UpdateCalled);
        Assert.False(system2.UpdateCalled); // Disabled
        Assert.True(system3.UpdateCalled);
    }

    [Fact]
    public void DisableSystems_MultipleSystemNames_SkipsAll()
    {
        // Arrange
        var pipeline = new UpdatePipeline();
        var world = CreateTestWorld();
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        var system1 = new TestUpdateSystem { Name = "System1" };
        var system2 = new TestUpdateSystem { Name = "System2" };
        var system3 = new TestUpdateSystem { Name = "System3" };

        pipeline.AddSystem(system1).AddSystem(system2).AddSystem(system3);

        // Act
        pipeline.DisableSystems(new[] { "System1", "System3" });
        pipeline.Execute(gameTime, world);

        // Assert
        Assert.False(system1.UpdateCalled);
        Assert.True(system2.UpdateCalled);
        Assert.False(system3.UpdateCalled);
    }

    [Fact]
    public void EnableAllSystems_ReEnablesDisabledSystems()
    {
        // Arrange
        var pipeline = new UpdatePipeline();
        var world = CreateTestWorld();
        var gameTime = new GameTime(TimeSpan.Zero, TimeSpan.FromSeconds(0.016));

        var system1 = new TestUpdateSystem { Name = "System1" };
        var system2 = new TestUpdateSystem { Name = "System2" };

        pipeline.AddSystem(system1).AddSystem(system2);
        pipeline.DisableSystems(new[] { "System1" });

        // Act - Re-enable
        pipeline.EnableAllSystems();
        pipeline.Execute(gameTime, world);

        // Assert - Both should execute
        Assert.True(system1.UpdateCalled);
        Assert.True(system2.UpdateCalled);
    }

    #endregion

    #region Systems Property

    [Fact]
    public void Systems_ReturnsReadOnlyList()
    {
        // Arrange
        var pipeline = new UpdatePipeline();
        pipeline.AddSystem(new TestUpdateSystem());

        // Act
        var systems = pipeline.Systems;

        // Assert
        Assert.IsAssignableFrom<IReadOnlyList<IUpdateSystem>>(systems);
    }

    [Fact]
    public void Systems_ReflectsCurrentState()
    {
        // Arrange
        var pipeline = new UpdatePipeline();

        // Act & Assert - Initially empty
        Assert.Empty(pipeline.Systems);

        // Add system
        var system = new TestUpdateSystem();
        pipeline.AddSystem(system);
        Assert.Single(pipeline.Systems);

        // Remove system
        pipeline.RemoveSystem(system);
        Assert.Empty(pipeline.Systems);
    }

    #endregion

    #region Test Helper Systems

    private class TestUpdateSystem : IUpdateSystem
    {
        public string Name { get; set; } = "TestUpdateSystem";
        public int UpdateOrder { get; set; } = 0;
        public bool UpdateCalled { get; private set; }
        public GameTime? LastGameTime { get; private set; }
        public IEntityWorld? LastWorld { get; private set; }
        public Action<GameTime, IEntityWorld>? OnUpdateAction { get; set; }

        public void Update(GameTime gameTime, IEntityWorld world)
        {
            UpdateCalled = true;
            LastGameTime = gameTime;
            LastWorld = world;
            OnUpdateAction?.Invoke(gameTime, world);
        }
    }

    #endregion
}