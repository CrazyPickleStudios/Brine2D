using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Systems;

namespace Brine2D.Tests.ECS;

public class EntityWorldFixedUpdateTests : TestBase
{
    [Fact]
    public void FixedUpdate_CallsFixedUpdateSystemsInOrder()
    {
        var world = CreateTestWorld();
        var log = new List<string>();

        world.AddSystem<EarlyFixedSystem>();
        world.AddSystem<LateFixedSystem>();
        world.Flush();

        var early = world.GetFixedUpdateSystem<EarlyFixedSystem>()!;
        early.Log = log;
        var late = world.GetFixedUpdateSystem<LateFixedSystem>()!;
        late.Log = log;

        world.FixedUpdate(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.Equal(["EarlyFixed", "LateFixed"], log);
    }

    [Fact]
    public void FixedUpdate_SkipsDisabledSystems()
    {
        var world = CreateTestWorld();
        var log = new List<string>();

        world.AddSystem<EarlyFixedSystem>();
        world.Flush();

        var system = world.GetFixedUpdateSystem<EarlyFixedSystem>()!;
        system.Log = log;
        system.IsEnabled = false;

        world.FixedUpdate(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.Empty(log);
    }

    [Fact]
    public void FixedUpdate_CallsBehaviorsAfterSystems()
    {
        var world = CreateTestWorld();
        var log = new List<string>();

        world.AddSystem<EarlyFixedSystem>();
        world.Flush();

        var system = world.GetFixedUpdateSystem<EarlyFixedSystem>()!;
        system.Log = log;

        var entity = world.CreateEntity("Test");
        entity.AddBehavior<FixedUpdateTestBehavior>();
        world.Flush();

        entity.GetBehavior<FixedUpdateTestBehavior>()!.Log = log;

        world.FixedUpdate(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.Equal(["EarlyFixed", "Behavior"], log);
    }

    [Fact]
    public void FixedUpdate_SkipsDisabledBehaviors()
    {
        var world = CreateTestWorld();
        var log = new List<string>();

        var entity = world.CreateEntity("Test");
        entity.AddBehavior<FixedUpdateTestBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<FixedUpdateTestBehavior>()!;
        behavior.Log = log;
        behavior.IsEnabled = false;

        world.FixedUpdate(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.Empty(log);
    }

    [Fact]
    public void FixedUpdate_SkipsBehaviorsOnInactiveEntities()
    {
        var world = CreateTestWorld();
        var log = new List<string>();

        var entity = world.CreateEntity("Test");
        entity.AddBehavior<FixedUpdateTestBehavior>();
        world.Flush();

        entity.GetBehavior<FixedUpdateTestBehavior>()!.Log = log;

        world.DestroyEntity(entity);

        world.FixedUpdate(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.Empty(log);
    }

    [Fact]
    public void FixedUpdate_SortsBehaviorsByFixedUpdateOrder()
    {
        var world = CreateTestWorld();
        var log = new List<string>();

        var entity = world.CreateEntity("Test");
        entity.AddBehavior<LateBehavior>();
        entity.AddBehavior<EarlyBehavior>();
        world.Flush();

        entity.GetBehavior<LateBehavior>()!.Log = log;
        entity.GetBehavior<EarlyBehavior>()!.Log = log;

        world.FixedUpdate(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.Equal(["EarlyBehavior", "LateBehavior"], log);
    }

    [Fact]
    public void FixedUpdate_ProcessesDeferredOperations()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        Assert.Empty(world.Entities);

        world.FixedUpdate(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.Contains(entity, world.Entities);
    }

    [Fact]
    public void AddSystem_RegistersFixedUpdateSystem()
    {
        var world = CreateTestWorld();

        world.AddSystem<EarlyFixedSystem>();

        Assert.True(world.HasFixedUpdateSystem<EarlyFixedSystem>());
        Assert.NotNull(world.GetFixedUpdateSystem<EarlyFixedSystem>());
    }

    [Fact]
    public void FixedUpdateSystems_ReflectsRegisteredSystems()
    {
        var world = CreateTestWorld();

        world.AddSystem<EarlyFixedSystem>();
        world.Flush();

        Assert.Single(world.FixedUpdateSystems);
    }

    [Fact]
    public void RemoveSystem_RemovesFromFixedUpdatePipeline()
    {
        var world = CreateTestWorld();
        var log = new List<string>();

        world.AddSystem<EarlyFixedSystem>();
        world.Flush();

        var system = world.GetFixedUpdateSystem<EarlyFixedSystem>()!;
        system.Log = log;

        world.RemoveSystem<EarlyFixedSystem>();
        world.Flush();

        world.FixedUpdate(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.Empty(log);
        Assert.False(world.HasFixedUpdateSystem<EarlyFixedSystem>());
    }

    [Fact]
    public void AddSystem_DualInterface_RegistersInBothPipelines()
    {
        var world = CreateTestWorld();

        world.AddSystem<DualPipelineSystem>();
        world.Flush();

        Assert.True(world.HasUpdateSystem<DualPipelineSystem>());
        Assert.True(world.HasFixedUpdateSystem<DualPipelineSystem>());
    }

    [Fact]
    public void FixedUpdate_SystemReceivesCorrectGameTime()
    {
        var world = CreateTestWorld();

        world.AddSystem<EarlyFixedSystem>();
        world.Flush();

        var system = world.GetFixedUpdateSystem<EarlyFixedSystem>()!;
        var expectedStep = TimeSpan.FromSeconds(1.0 / 60);
        var expectedTotal = TimeSpan.FromSeconds(2);
        var fixedTime = new GameTime(expectedTotal, expectedStep);

        world.FixedUpdate(fixedTime);

        Assert.Equal(expectedStep, system.LastElapsed);
        Assert.Equal(expectedTotal, system.LastTotal);
    }

    private class EarlyFixedSystem : IFixedUpdateSystem
    {
        public bool IsEnabled { get; set; } = true;
        public int FixedUpdateOrder => SystemFixedUpdateOrder.EarlyFixedUpdate;
        public List<string>? Log { get; set; }
        public TimeSpan LastElapsed { get; private set; }
        public TimeSpan LastTotal { get; private set; }

        public void FixedUpdate(IEntityWorld world, GameTime fixedTime)
        {
            Log?.Add("EarlyFixed");
            LastElapsed = fixedTime.ElapsedTime;
            LastTotal = fixedTime.TotalTime;
        }
    }

    private class LateFixedSystem : IFixedUpdateSystem
    {
        public bool IsEnabled { get; set; } = true;
        public int FixedUpdateOrder => SystemFixedUpdateOrder.LateFixedUpdate;
        public List<string>? Log { get; set; }

        public void FixedUpdate(IEntityWorld world, GameTime fixedTime)
        {
            Log?.Add("LateFixed");
        }
    }

    private class DualPipelineSystem : IUpdateSystem, IFixedUpdateSystem
    {
        public bool IsEnabled { get; set; } = true;
        public int UpdateOrder => SystemUpdateOrder.Update;
        public int FixedUpdateOrder => SystemFixedUpdateOrder.Physics;

        public void Update(IEntityWorld world, GameTime gameTime) { }
        public void FixedUpdate(IEntityWorld world, GameTime fixedTime) { }
    }

    private class FixedUpdateTestBehavior : Behavior
    {
        public List<string>? Log { get; set; }

        public override void FixedUpdate(GameTime fixedTime)
        {
            Log?.Add("Behavior");
        }
    }

    private class EarlyBehavior : Behavior
    {
        public List<string>? Log { get; set; }
        public override int FixedUpdateOrder => 0;

        public override void FixedUpdate(GameTime fixedTime)
        {
            Log?.Add("EarlyBehavior");
        }
    }

    private class LateBehavior : Behavior
    {
        public List<string>? Log { get; set; }
        public override int FixedUpdateOrder => 10;

        public override void FixedUpdate(GameTime fixedTime)
        {
            Log?.Add("LateBehavior");
        }
    }

    private class DisposableFixedSystem : FixedUpdateSystemBase
    {
        public bool DisposeCalled { get; private set; }

        public override void FixedUpdate(IEntityWorld world, GameTime fixedTime) { }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
                DisposeCalled = true;
            base.Dispose(disposing);
        }
    }

    [Fact]
    public void RemoveSystem_FixedUpdateSystemBase_DisposesSystem()
    {
        var world = CreateTestWorld();
        world.AddSystem<DisposableFixedSystem>();
        world.Flush();

        var system = world.GetSystem<DisposableFixedSystem>()!;
        world.RemoveSystem<DisposableFixedSystem>();
        world.Flush();
        world.FixedUpdate(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.True(system.DisposeCalled);
    }

    [Fact]
    public void Dispose_FixedUpdateSystemBase_DoesNotDisposeMoreThanOnce()
    {
        var world = CreateTestWorld();
        world.AddSystem<DisposableFixedSystem>();
        world.Flush();

        var system = world.GetSystem<DisposableFixedSystem>()!;
        system.Dispose();
        system.Dispose();

        Assert.True(system.DisposeCalled);
    }

    [Fact]
    public void Behavior_OnStart_CalledBeforeFirstFixedUpdate()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<StartTrackingFixedBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<StartTrackingFixedBehavior>()!;

        world.FixedUpdate(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.True(behavior.StartCalled);
        Assert.Equal(1, behavior.StartCallCount);
    }

    [Fact]
    public void Behavior_OnStart_CalledOnlyOnce_AcrossFixedUpdateAndUpdate()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<StartTrackingFixedBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<StartTrackingFixedBehavior>()!;
        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60));

        world.FixedUpdate(gt);
        world.Update(gt);
        world.FixedUpdate(gt);
        world.Update(gt);

        Assert.Equal(1, behavior.StartCallCount);
    }

    [Fact]
    public void Behavior_OnStart_NotCalledWhenDisabled_FixedUpdate()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<StartTrackingFixedBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<StartTrackingFixedBehavior>()!;
        behavior.IsEnabled = false;
        world.FixedUpdate(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.False(behavior.StartCalled);
    }

    [Fact]
    public void FixedUpdateSystemBase_OnStart_CalledBeforeFirstFixedUpdate()
    {
        var world = CreateTestWorld();
        world.AddSystem<StartTrackingFixedSystem>();
        world.Flush();

        var system = world.GetSystem<StartTrackingFixedSystem>()!;
        Assert.False(system.StartCalled);

        world.FixedUpdate(new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60)));

        Assert.True(system.StartCalled);
        Assert.Equal(["OnStart", "FixedUpdate"], system.Log);
    }

    [Fact]
    public void FixedUpdateSystemBase_OnStart_CalledOnlyOnce()
    {
        var world = CreateTestWorld();
        world.AddSystem<StartTrackingFixedSystem>();
        world.Flush();

        var system = world.GetSystem<StartTrackingFixedSystem>()!;
        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60));

        world.FixedUpdate(gt);
        world.FixedUpdate(gt);

        Assert.Equal(1, system.StartCallCount);
    }

    private class StartTrackingFixedBehavior : Behavior
    {
        public bool StartCalled { get; private set; }
        public int StartCallCount { get; private set; }
        public override void OnStart() { StartCalled = true; StartCallCount++; }
        public override void FixedUpdate(GameTime fixedTime) { }
    }

    private class StartTrackingFixedSystem : FixedUpdateSystemBase
    {
        public bool StartCalled { get; private set; }
        public int StartCallCount { get; private set; }
        public List<string> Log { get; } = new();

        public override void OnStart(IEntityWorld world) { StartCalled = true; StartCallCount++; Log.Add("OnStart"); }
        public override void FixedUpdate(IEntityWorld world, GameTime fixedTime) => Log.Add("FixedUpdate");
    }

    private class ThrowingOnStartFixedBehavior : Behavior
    {
        public int FixedUpdateCallCount { get; private set; }
        public override void OnStart() => throw new InvalidOperationException("OnStart failed");
        public override void FixedUpdate(GameTime fixedTime) => FixedUpdateCallCount++;
    }

    private class ThrowingOnStartFixedSystemBase : FixedUpdateSystemBase
    {
        public int FixedUpdateCallCount { get; private set; }
        public override void OnStart(IEntityWorld world) => throw new InvalidOperationException("OnStart failed");
        public override void FixedUpdate(IEntityWorld world, GameTime fixedTime) => FixedUpdateCallCount++;
    }

    [Fact]
    public void Behavior_OnStartThrows_PropagateTrue_RethrowsAndSkipsFixedUpdate()
    {
        var world = CreateTestWorld(o => o.PropagateExceptions = true);
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<ThrowingOnStartFixedBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<ThrowingOnStartFixedBehavior>()!;
        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60));

        Assert.Throws<InvalidOperationException>(() => world.FixedUpdate(gt));
        Assert.Equal(0, behavior.FixedUpdateCallCount);
    }

    [Fact]
    public void Behavior_OnStartThrows_PropagateFalse_SwallowsAndSkipsFixedUpdateOnAllSubsequentFrames()
    {
        var world = CreateTestWorld(o => o.PropagateExceptions = false);
        var entity = world.CreateEntity("Test");
        entity.AddBehavior<ThrowingOnStartFixedBehavior>();
        world.Flush();

        var behavior = entity.GetBehavior<ThrowingOnStartFixedBehavior>()!;
        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60));

        world.FixedUpdate(gt);
        world.FixedUpdate(gt);
        world.FixedUpdate(gt);

        Assert.Equal(0, behavior.FixedUpdateCallCount);
    }

    [Fact]
    public void FixedUpdateSystemBase_OnStartThrows_PropagateTrue_RethrowsAndSkipsFixedUpdate()
    {
        var world = CreateTestWorld(o => o.PropagateExceptions = true);
        world.AddSystem<ThrowingOnStartFixedSystemBase>();
        world.Flush();

        var system = world.GetSystem<ThrowingOnStartFixedSystemBase>()!;
        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60));

        Assert.Throws<InvalidOperationException>(() => world.FixedUpdate(gt));
        Assert.Equal(0, system.FixedUpdateCallCount);
    }

    [Fact]
    public void FixedUpdateSystemBase_OnStartThrows_PropagateFalse_SwallowsAndSkipsFixedUpdateOnAllSubsequentFrames()
    {
        var world = CreateTestWorld(o => o.PropagateExceptions = false);
        world.AddSystem<ThrowingOnStartFixedSystemBase>();
        world.Flush();

        var system = world.GetSystem<ThrowingOnStartFixedSystemBase>()!;
        var gt = new GameTime(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1.0 / 60));

        world.FixedUpdate(gt);
        world.FixedUpdate(gt);
        world.FixedUpdate(gt);

        Assert.Equal(0, system.FixedUpdateCallCount);
    }
}
