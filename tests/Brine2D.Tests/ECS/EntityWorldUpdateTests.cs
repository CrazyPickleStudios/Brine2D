using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Systems;

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
}