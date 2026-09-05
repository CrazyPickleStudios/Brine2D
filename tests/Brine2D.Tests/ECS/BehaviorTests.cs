using Brine2D.Audio;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Rendering;
using NSubstitute;

namespace Brine2D.Tests.ECS;

public class BehaviorTests : TestBase
{
    private class PlainBehavior : Behavior { }

    #region Entity

    [Fact]
    public void Entity_BeforeAttach_Throws()
    {
        var behavior = new PlainBehavior();

        Assert.Throws<InvalidOperationException>(() => behavior.Entity);
    }

    [Fact]
    public void Entity_WhileAttached_ReturnsOwningEntity()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<PlainBehavior>();

        var behavior = entity.GetBehavior<PlainBehavior>()!;

        Assert.Same(entity, behavior.Entity);
    }

    [Fact]
    public void Entity_AfterRemoveBehavior_Throws()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<PlainBehavior>();
        var behavior = entity.GetBehavior<PlainBehavior>()!;

        entity.RemoveBehavior<PlainBehavior>();

        Assert.Throws<InvalidOperationException>(() => behavior.Entity);
    }

    [Fact]
    public void Entity_AfterEntityDestroyed_Throws()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<PlainBehavior>();
        var behavior = entity.GetBehavior<PlainBehavior>()!;

        world.DestroyEntity(entity);
        world.Flush();

        Assert.Throws<InvalidOperationException>(() => behavior.Entity);
    }

    #endregion

    #region Ambient framework properties

    [Fact]
    public void Logger_NotStamped_Throws()
    {
        var behavior = new PlainBehavior();

        Assert.Throws<InvalidOperationException>(() => behavior.Logger);
    }

    [Fact]
    public void Renderer_NotStamped_Throws()
    {
        var behavior = new PlainBehavior();

        Assert.Throws<InvalidOperationException>(() => behavior.Renderer);
    }

    [Fact]
    public void Input_NotStamped_Throws()
    {
        var behavior = new PlainBehavior();

        Assert.Throws<InvalidOperationException>(() => behavior.Input);
    }

    [Fact]
    public void Audio_NotStamped_Throws()
    {
        var behavior = new PlainBehavior();

        Assert.Throws<InvalidOperationException>(() => behavior.Audio);
    }

    [Fact]
    public void Game_NotStamped_Throws()
    {
        var behavior = new PlainBehavior();

        Assert.Throws<InvalidOperationException>(() => behavior.Game);
    }

    [Fact]
    public void AmbientProperties_WhenWorldHasFrameworkServices_AreAvailableOnAdd()
    {
        var world = CreateTestWorld(withFrameworkServices: true);
        var entity = world.CreateEntity();

        entity.AddBehavior<PlainBehavior>();
        var behavior = entity.GetBehavior<PlainBehavior>()!;

        Assert.NotNull(behavior.Logger);
        Assert.NotNull(behavior.Renderer);
        Assert.NotNull(behavior.Input);
        Assert.NotNull(behavior.Audio);
        Assert.NotNull(behavior.Game);
    }

    [Fact]
    public void AmbientProperties_WhenWorldHasNoFrameworkServices_Throw()
    {
        var world = CreateTestWorld(withFrameworkServices: false);
        var entity = world.CreateEntity();

        entity.AddBehavior<PlainBehavior>();
        var behavior = entity.GetBehavior<PlainBehavior>()!;

        Assert.Throws<InvalidOperationException>(() => behavior.Logger);
        Assert.Throws<InvalidOperationException>(() => behavior.Renderer);
        Assert.Throws<InvalidOperationException>(() => behavior.Input);
        Assert.Throws<InvalidOperationException>(() => behavior.Audio);
        Assert.Throws<InvalidOperationException>(() => behavior.Game);
    }

    #endregion

    #region Mid-frame detachment (hot loop regression)

    private class RemovingBehavior : Behavior
    {
        public override int UpdateOrder => 0;

        public override void Update(GameTime gameTime)
        {
            Entity.RemoveBehavior<TrailingBehavior>();
        }
    }

    private class TrailingBehavior : Behavior
    {
        public bool UpdateRan { get; private set; }

        public override int UpdateOrder => 1;

        public override void Update(GameTime gameTime)
        {
            UpdateRan = true;
        }
    }

    [Fact]
    public void Update_BehaviorRemovedMidFrameByEarlierBehaviorOnSameEntity_SkippedWithoutThrowing()
    {
        // RemovingBehavior (UpdateOrder 0) removes TrailingBehavior (UpdateOrder 1) from the
        // same entity during Update. TrailingBehavior.Entity is nulled synchronously by
        // RemoveBehavior<T>(), but TrailingBehavior remains in this frame's already-built
        // dispatch list (removal from EntityWorld's list is deferred). The dispatch loop must
        // use the non-throwing internal accessor to skip it instead of throwing.
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddBehavior<RemovingBehavior>();
        entity.AddBehavior<TrailingBehavior>();
        var trailingBehavior = entity.GetBehavior<TrailingBehavior>()!;

        world.Flush();

        var exception = Record.Exception(() => world.Update(new GameTime(TimeSpan.Zero, TimeSpan.FromMilliseconds(16))));

        Assert.Null(exception);
        Assert.False(trailingBehavior.UpdateRan);
    }

    #endregion
}
