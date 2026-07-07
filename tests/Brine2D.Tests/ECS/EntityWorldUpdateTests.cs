using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Systems;
using Brine2D.Rendering;

namespace Brine2D.Tests.ECS;

public class EntityWorldUpdateTests : TestBase
{
    [Fact]
    public void Update_CallsUpdateSystemsInOrder()
    {
        var world = CreateTestWorld();
        var log = new List<string>();

        world.AddSystem<EarlyUpdateSystem>();
        world.AddSystem<LateUpdateSystem>();
        world.Flush();

        world.GetUpdateSystem<EarlyUpdateSystem>()!.Log = log;
        world.GetUpdateSystem<LateUpdateSystem>()!.Log = log;

        world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.Equal(["EarlyUpdate", "LateUpdate"], log);
    }

    [Fact]
    public void Update_SkipsDisabledSystems()
    {
        var world = CreateTestWorld();
        var log = new List<string>();

        world.AddSystem<EarlyUpdateSystem>();
        world.Flush();

        var system = world.GetUpdateSystem<EarlyUpdateSystem>()!;
        system.Log = log;
        system.IsEnabled = false;

        world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.Empty(log);
    }

    [Fact]
    public void Update_CallsBehaviorsAfterSystems()
    {
        var world = CreateTestWorld();
        var log = new List<string>();

        world.AddSystem<EarlyUpdateSystem>();
        world.Flush();

        world.GetUpdateSystem<EarlyUpdateSystem>()!.Log = log;

        var entity = world.CreateEntity("Test");
        entity.AddBehavior<UpdateTestBehavior>();
        world.Flush();

        entity.GetBehavior<UpdateTestBehavior>()!.Log = log;

        world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.Equal(["EarlyUpdate", "Behavior"], log);
    }

    [Fact]
    public void Update_SkipsDisabledBehaviors()
    {
        var world = CreateTestWorld();
        var log = new List<string>();

        var entity = world.CreateEntity("Test");
        entity.AddBehavior<UpdateTestBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<UpdateTestBehavior>()!;
        behavior.Log = log;
        behavior.IsEnabled = false;

        world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.Empty(log);
    }

    [Fact]
    public void Update_SkipsBehaviorsOnInactiveEntities()
    {
        var world = CreateTestWorld();
        var log = new List<string>();

        var entity = world.CreateEntity("Test");
        entity.AddBehavior<UpdateTestBehavior>();
        world.Flush();

        entity.GetBehavior<UpdateTestBehavior>()!.Log = log;

        world.DestroyEntity(entity);

        world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.Empty(log);
    }

    [Fact]
    public void Update_SortsBehaviorsByUpdateOrder()
    {
        var world = CreateTestWorld();
        var log = new List<string>();

        var entity = world.CreateEntity("Test");
        entity.AddBehavior<LateBehavior>();
        entity.AddBehavior<EarlyBehavior>();
        world.Flush();

        entity.GetBehavior<LateBehavior>()!.Log = log;
        entity.GetBehavior<EarlyBehavior>()!.Log = log;

        world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.Equal(["EarlyBehavior", "LateBehavior"], log);
    }

    [Fact]
    public void Update_SystemReceivesCorrectGameTime()
    {
        var world = CreateTestWorld();

        world.AddSystem<EarlyUpdateSystem>();
        world.Flush();

        var system = world.GetUpdateSystem<EarlyUpdateSystem>()!;
        var expectedStep = TimeSpan.FromSeconds(1.0 / 60);
        var expectedTotal = TimeSpan.FromSeconds(5);
        var gameTime = new GameTime(expectedTotal, expectedStep);

        world.Update(gameTime);

        Assert.Equal(expectedStep, system.LastElapsed);
        Assert.Equal(expectedTotal, system.LastTotal);
    }

    [Fact]
    public void RemoveSystem_RemovesFromUpdatePipeline()
    {
        var world = CreateTestWorld();
        var log = new List<string>();

        world.AddSystem<EarlyUpdateSystem>();
        world.Flush();

        var system = world.GetUpdateSystem<EarlyUpdateSystem>()!;
        system.Log = log;

        world.RemoveSystem<EarlyUpdateSystem>();
        world.Flush();

        world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.Empty(log);
        Assert.False(world.HasUpdateSystem<EarlyUpdateSystem>());
    }

    [Fact]
    public void RemoveSystem_DuringUpdate_DoesNotExecuteInSameFrame()
    {
        var world = CreateTestWorld();
        var log = new List<string>();

        world.AddSystem<EarlyUpdateSystem>();
        world.AddSystem<SelfRemovingSystem>();
        world.Flush();

        world.GetUpdateSystem<EarlyUpdateSystem>()!.Log = log;
        world.GetUpdateSystem<SelfRemovingSystem>()!.Log = log;

        world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        // SelfRemovingSystem runs first and removes EarlyUpdateSystem; EarlyUpdateSystem must NOT run this frame
        Assert.DoesNotContain("EarlyUpdate", log);
        Assert.Contains("SelfRemoving", log);
    }

    private class EarlyUpdateSystem : IUpdateSystem
    {
        public bool IsEnabled { get; set; } = true;
        public int UpdateOrder => SystemUpdateOrder.EarlyUpdate;
        public List<string>? Log { get; set; }
        public TimeSpan LastElapsed { get; private set; }
        public TimeSpan LastTotal { get; private set; }

        public void Update(IEntityWorld world, GameTime gameTime)
        {
            Log?.Add("EarlyUpdate");
            LastElapsed = gameTime.ElapsedTime;
            LastTotal = gameTime.TotalTime;
        }
    }

    // Order = -200 (PreInput) so it runs before EarlyUpdateSystem (-100)
    private class SelfRemovingSystem : IUpdateSystem
    {
        public bool IsEnabled { get; set; } = true;
        public int UpdateOrder => SystemUpdateOrder.PreInput;
        public List<string>? Log { get; set; }

        public void Update(IEntityWorld world, GameTime gameTime)
        {
            Log?.Add("SelfRemoving");
            world.RemoveSystem<EarlyUpdateSystem>();
        }
    }

    private class LateUpdateSystem : IUpdateSystem
    {
        public bool IsEnabled { get; set; } = true;
        public int UpdateOrder => SystemUpdateOrder.LateUpdate;
        public List<string>? Log { get; set; }

        public void Update(IEntityWorld world, GameTime gameTime)
        {
            Log?.Add("LateUpdate");
        }
    }

    private class UpdateTestBehavior : Behavior
    {
        public List<string>? Log { get; set; }

        public override void Update(GameTime gameTime)
        {
            Log?.Add("Behavior");
        }
    }

    private class EarlyBehavior : Behavior
    {
        public List<string>? Log { get; set; }
        public override int UpdateOrder => 0;

        public override void Update(GameTime gameTime)
        {
            Log?.Add("EarlyBehavior");
        }
    }

    private class LateBehavior : Behavior
    {
        public List<string>? Log { get; set; }
        public override int UpdateOrder => 10;

        public override void Update(GameTime gameTime)
        {
            Log?.Add("LateBehavior");
        }
    }

    private class DisposableUpdateSystem : UpdateSystemBase
    {
        public bool DisposeCalled { get; private set; }

        public override void Update(IEntityWorld world, GameTime gameTime) { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                DisposeCalled = true;
            base.Dispose(disposing);
        }
    }

    [Fact]
    public void RemoveSystem_UpdateSystemBase_DisposesSystem()
    {
        var world = CreateTestWorld();
        world.AddSystem<DisposableUpdateSystem>();
        world.Flush();

        var system = world.GetSystem<DisposableUpdateSystem>()!;
        world.RemoveSystem<DisposableUpdateSystem>();
        world.Flush();
        world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.True(system.DisposeCalled);
    }

    [Fact]
    public void Dispose_UpdateSystemBase_DoesNotDisposeMoreThanOnce()
    {
        var world = CreateTestWorld();
        world.AddSystem<DisposableUpdateSystem>();
        world.Flush();

        var system = world.GetSystem<DisposableUpdateSystem>()!;
        system.Dispose();
        system.Dispose();

        Assert.True(system.DisposeCalled);
    }

    [Fact]
    public void Update_PropagateExceptions_False_DoesNotThrowOnSystemException()
    {
        var world = CreateTestWorld(o => o.PropagateExceptions = false);
        world.AddSystem<ThrowingUpdateSystem>();
        world.Flush();

        var ex = Record.Exception(() =>
            world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60))));

        Assert.Null(ex);
    }

    [Fact]
    public void Update_PropagateExceptions_True_ThrowsOnSystemException()
    {
        var world = CreateTestWorld(o => o.PropagateExceptions = true);
        world.AddSystem<ThrowingUpdateSystem>();
        world.Flush();

        Assert.Throws<InvalidOperationException>(() =>
            world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60))));
    }

    [Fact]
    public void Update_PropagateExceptions_True_ThrowsOnBehaviorException()
    {
        var world = CreateTestWorld(o => o.PropagateExceptions = true);
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<ThrowingBehavior>();
        world.Flush();

        Assert.Throws<InvalidOperationException>(() =>
            world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60))));
    }

    [Fact]
    public void Update_PropagateExceptions_False_DoesNotThrowOnBehaviorException()
    {
        var world = CreateTestWorld(o => o.PropagateExceptions = false);
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<ThrowingBehavior>();
        world.Flush();

        var ex = Record.Exception(() =>
            world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60))));

        Assert.Null(ex);
    }

    [Fact]
    public void Behavior_OnStart_CalledOnFirstUpdate()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<StartTrackingBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<StartTrackingBehavior>()!;
        Assert.False(behavior.StartCalled);

        world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.True(behavior.StartCalled);
    }

    [Fact]
    public void Behavior_OnStart_CalledOnlyOnce()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<StartTrackingBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<StartTrackingBehavior>()!;
        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60));

        world.Update(gt);
        world.Update(gt);
        world.Update(gt);

        Assert.Equal(1, behavior.StartCallCount);
    }

    [Fact]
    public void Behavior_OnStart_RunsBeforeUpdate()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<StartOrderBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<StartOrderBehavior>()!;
        world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.Equal(["OnStart", "Update"], behavior.Log);
    }

    [Fact]
    public void Behavior_OnStart_NotCalledWhenDisabled()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<StartTrackingBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<StartTrackingBehavior>()!;
        behavior.IsEnabled = false;
        world.Update(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.False(behavior.StartCalled);
    }

    private class ThrowingUpdateSystem : IUpdateSystem
    {
        public bool IsEnabled { get; set; } = true;
        public int UpdateOrder => 0;
        public void Update(IEntityWorld world, GameTime gameTime)
            => throw new InvalidOperationException("system crash");
    }

    private class ThrowingBehavior : Behavior
    {
        public override void Update(GameTime gameTime)
            => throw new InvalidOperationException("behavior crash");
    }

    private class StartTrackingBehavior : Behavior
    {
        public bool StartCalled { get; private set; }
        public int StartCallCount { get; private set; }
        public override void OnStart() { StartCalled = true; StartCallCount++; }
    }

    private class StartOrderBehavior : Behavior
    {
        public List<string> Log { get; } = new();
        public override void OnStart() => Log.Add("OnStart");
        public override void Update(GameTime gameTime) => Log.Add("Update");
    }

    private class RenderOnlyBehavior : Behavior
    {
        public bool StartCalled { get; private set; }
        public int StartCallCount { get; private set; }
        public List<string> Log { get; } = new();
        public override void OnStart() { StartCalled = true; StartCallCount++; Log.Add("OnStart"); }
        public override void Render(IRenderer renderer, GameTime gameTime) => Log.Add("Render");
    }

    [Fact]
    public void Behavior_OnStart_CalledOnFirstRender_WhenNoUpdateOccurs()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<RenderOnlyBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<RenderOnlyBehavior>()!;
        Assert.False(behavior.StartCalled);

        var renderer = NSubstitute.Substitute.For<IRenderer>();
        world.Render(renderer, new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.True(behavior.StartCalled);
    }

    [Fact]
    public void Behavior_OnStart_CalledOnlyOnce_WhenFirstTickIsRender()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<RenderOnlyBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<RenderOnlyBehavior>()!;
        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60));
        var renderer = NSubstitute.Substitute.For<IRenderer>();

        world.Render(renderer, gt);
        world.Render(renderer, gt);
        world.Update(gt);

        Assert.Equal(1, behavior.StartCallCount);
    }

    [Fact]
    public void Behavior_OnStart_RunsBeforeRender()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<RenderOnlyBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<RenderOnlyBehavior>()!;
        var renderer = NSubstitute.Substitute.For<IRenderer>();
        world.Render(renderer, new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.Equal(["OnStart", "Render"], behavior.Log);
    }

    [Fact]
    public void Behavior_OnStart_NotCalledDuringRender_WhenDisabled()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<RenderOnlyBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<RenderOnlyBehavior>()!;
        behavior.IsEnabled = false;

        var renderer = NSubstitute.Substitute.For<IRenderer>();
        world.Render(renderer, new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.False(behavior.StartCalled);
    }

    private class ThrowingOnStartBehavior : Behavior
    {
        public int UpdateCallCount { get; private set; }
        public override void OnStart() => throw new InvalidOperationException("OnStart failed");
        public override void Update(GameTime gameTime) => UpdateCallCount++;
    }

    private class ThrowingOnStartUpdateSystemBase : UpdateSystemBase
    {
        public int UpdateCallCount { get; private set; }
        public override void OnStart(IEntityWorld world) => throw new InvalidOperationException("OnStart failed");
        public override void Update(IEntityWorld world, GameTime gameTime) => UpdateCallCount++;
    }

    [Fact]
    public void Behavior_OnStartThrows_PropagateTrue_RethrowsAndSkipsUpdate()
    {
        var world = CreateTestWorld(o => o.PropagateExceptions = true);
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<ThrowingOnStartBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<ThrowingOnStartBehavior>()!;
        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60));

        Assert.Throws<InvalidOperationException>(() => world.Update(gt));
        Assert.Equal(0, behavior.UpdateCallCount);
    }

    [Fact]
    public void Behavior_OnStartThrows_PropagateFalse_SwallowsAndSkipsUpdateOnAllSubsequentFrames()
    {
        var world = CreateTestWorld(o => o.PropagateExceptions = false);
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<ThrowingOnStartBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<ThrowingOnStartBehavior>()!;
        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60));

        world.Update(gt);
        world.Update(gt);
        world.Update(gt);

        Assert.Equal(0, behavior.UpdateCallCount);
    }

    [Fact]
    public void UpdateSystemBase_OnStartThrows_PropagateTrue_RethrowsAndSkipsUpdate()
    {
        var world = CreateTestWorld(o => o.PropagateExceptions = true);
        world.AddSystem<ThrowingOnStartUpdateSystemBase>();
        world.Flush();

        var system = world.GetSystem<ThrowingOnStartUpdateSystemBase>()!;
        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60));

        Assert.Throws<InvalidOperationException>(() => world.Update(gt));
        Assert.Equal(0, system.UpdateCallCount);
    }

    [Fact]
    public void UpdateSystemBase_OnStartThrows_PropagateFalse_SwallowsAndSkipsUpdateOnAllSubsequentFrames()
    {
        var world = CreateTestWorld(o => o.PropagateExceptions = false);
        world.AddSystem<ThrowingOnStartUpdateSystemBase>();
        world.Flush();

        var system = world.GetSystem<ThrowingOnStartUpdateSystemBase>()!;
        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60));

        world.Update(gt);
        world.Update(gt);
        world.Update(gt);

        Assert.Equal(0, system.UpdateCallCount);
    }
}