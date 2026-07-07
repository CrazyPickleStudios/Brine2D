using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Systems;
using Brine2D.Rendering;
using Brine2D.Tilemap;
using FluentAssertions;
using NSubstitute;
using System.Numerics;
using Xunit;

namespace Brine2D.Tests.Tilemap;

public sealed class TilemapSystemTests : TestBase
{
    private static Brine2D.Tilemap.Tilemap MakeMinimalTilemap()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 2, 2);
        var tileset = new Tileset
        {
            FirstGid = 1,
            TileWidth = 16,
            TileHeight = 16,
            Columns = 2,
            Rows = 2,
            ImagePath = "tiles.png"
        };
        tilemap.AddTileset(tileset);

        var layer = new TilemapLayer("Ground", 2, 2) { ZOrder = 0 };
        layer.SetTile(0, 0, new Tile(1));
        tilemap.AddLayer(layer);

        return tilemap;
    }

    [Fact]
    public void TilemapSystem_ImplementsIUpdateSystem()
    {
        typeof(TilemapSystem).Should().Implement<IUpdateSystem>();
    }

    [Fact]
    public void TilemapSystem_ImplementsIRenderSystem()
    {
        typeof(TilemapSystem).Should().Implement<IRenderSystem>();
    }

    [Fact]
    public void TilemapSystem_UpdateOrder_IsAnimationOrder()
    {
        var system = new TilemapSystem(Substitute.For<ITextureLoader>());

        system.UpdateOrder.Should().Be(SystemUpdateOrder.Animation);
    }

    [Fact]
    public void TilemapSystem_RenderOrder_IsTilemapOrder()
    {
        var system = new TilemapSystem(Substitute.For<ITextureLoader>());

        system.RenderOrder.Should().Be(SystemRenderOrder.Tilemap);
    }

    [Fact]
    public void Update_WithTilemapComponent_InitializesAnimator()
    {
        var world = CreateTestWorld();
        var textureLoader = Substitute.For<ITextureLoader>();
        textureLoader.LoadTextureAsync(Arg.Any<string>(), Arg.Any<TextureScaleMode>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ITexture>()));

        var tilemap = MakeMinimalTilemap();
        var component = new TilemapComponent { Tilemap = tilemap };
        world.CreateEntity().AddComponent(component);
        world.Flush();

        var system = new TilemapSystem(textureLoader);
        system.Update(world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.Zero));

        component.Animator.Should().NotBeNull();
    }

    [Fact]
    public void Update_CalledTwice_DoesNotReinitializeAnimator()
    {
        var world = CreateTestWorld();
        var textureLoader = Substitute.For<ITextureLoader>();
        textureLoader.LoadTextureAsync(Arg.Any<string>(), Arg.Any<TextureScaleMode>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ITexture>()));

        var component = new TilemapComponent { Tilemap = MakeMinimalTilemap() };
        world.CreateEntity().AddComponent(component);
        world.Flush();

        var system = new TilemapSystem(textureLoader);
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.Zero));
        var firstAnimator = component.Animator;

        system.Update(world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.Zero));

        component.Animator.Should().BeSameAs(firstAnimator);
    }

    [Fact]
    public void Update_ComponentWithNullTilemap_DoesNotThrow()
    {
        var world = CreateTestWorld();
        world.CreateEntity().AddComponent(new TilemapComponent { Tilemap = null });
        world.Flush();

        var system = new TilemapSystem(Substitute.For<ITextureLoader>());
        var act = () => system.Update(world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.Zero));

        act.Should().NotThrow();
    }

    [Fact]
    public void Update_DisabledComponent_IsSkipped()
    {
        var world = CreateTestWorld();
        var textureLoader = Substitute.For<ITextureLoader>();
        textureLoader.LoadTextureAsync(Arg.Any<string>(), Arg.Any<TextureScaleMode>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ITexture>()));

        var component = new TilemapComponent { Tilemap = MakeMinimalTilemap() };
        component.IsEnabled = false;
        world.CreateEntity().AddComponent(component);
        world.Flush();

        var system = new TilemapSystem(textureLoader);
        system.Update(world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.Zero));

        component.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public void Render_BeforeUpdate_DoesNotThrow()
    {
        var world = CreateTestWorld();
        var component = new TilemapComponent { Tilemap = MakeMinimalTilemap() };
        world.CreateEntity().AddComponent(component);
        world.Flush();

        var system = new TilemapSystem(Substitute.For<ITextureLoader>());
        var renderer = Substitute.For<IRenderer>();

        var act = () => system.Render(world, renderer, default);

        act.Should().NotThrow();
    }

    [Fact]
    public void TilemapComponent_DefaultPositionOffset_IsZero()
    {
        var component = new TilemapComponent();

        component.PositionOffset.Should().Be(Vector2.Zero);
    }

    [Fact]
    public void TilemapComponent_IsLoadedDefaultsFalse()
    {
        var component = new TilemapComponent();

        component.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public void TilemapComponent_IsLoaded_IsPubliclyReadable()
    {
        var component = new TilemapComponent();

        var prop = typeof(TilemapComponent).GetProperty(nameof(TilemapComponent.IsLoaded));
        prop.Should().NotBeNull();
        prop!.GetMethod.Should().NotBeNull();
        prop.GetMethod!.IsPublic.Should().BeTrue();
    }

    [Fact]
    public void Dispose_CalledMultipleTimes_DoesNotThrow()
    {
        var system = new TilemapSystem(Substitute.For<ITextureLoader>());
        system.Dispose();

        var act = () => system.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void Update_WithTilemapComponent_AnimatorInitializedBeforeTextureLoad()
    {
        var world = CreateTestWorld();
        var tcs = new TaskCompletionSource<ITexture>();
        var textureLoader = Substitute.For<ITextureLoader>();
        textureLoader.LoadTextureAsync(Arg.Any<string>(), Arg.Any<TextureScaleMode>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var component = new TilemapComponent { Tilemap = MakeMinimalTilemap() };
        world.CreateEntity().AddComponent(component);
        world.Flush();

        var system = new TilemapSystem(textureLoader);
        system.Update(world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.Zero));

        component.Animator.Should().NotBeNull();
        component.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task Update_AfterTextureLoadCompletes_IsLoadedBecomesTrue()
    {
        var world = CreateTestWorld();
        var tcs = new TaskCompletionSource<ITexture>();
        var textureLoader = Substitute.For<ITextureLoader>();
        textureLoader.LoadTextureAsync(Arg.Any<string>(), Arg.Any<TextureScaleMode>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var component = new TilemapComponent { Tilemap = MakeMinimalTilemap() };
        world.CreateEntity().AddComponent(component);
        world.Flush();

        var system = new TilemapSystem(textureLoader);
        system.Update(world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.Zero));

        component.IsLoaded.Should().BeFalse();

        tcs.SetResult(Substitute.For<ITexture>());
        await Task.Yield();
        await Task.Delay(50);

        component.IsLoaded.Should().BeTrue();
    }

    [Fact]
    public async Task Update_WhenTextureLoadFaults_IsLoadedRemainsFlase()
    {
        var world = CreateTestWorld();
        var tcs = new TaskCompletionSource<ITexture>();
        var textureLoader = Substitute.For<ITextureLoader>();
        textureLoader.LoadTextureAsync(Arg.Any<string>(), Arg.Any<TextureScaleMode>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var component = new TilemapComponent { Tilemap = MakeMinimalTilemap() };
        world.CreateEntity().AddComponent(component);
        world.Flush();

        var system = new TilemapSystem(textureLoader);
        system.Update(world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.Zero));

        tcs.SetException(new IOException("disk error"));
        await Task.Yield();
        await Task.Delay(50);

        component.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public void Update_CalledTwiceWithPendingLoad_DoesNotStartSecondLoad()
    {
        var world = CreateTestWorld();
        var loadCallCount = 0;
        var tcs = new TaskCompletionSource<ITexture>();
        var textureLoader = Substitute.For<ITextureLoader>();
        textureLoader.LoadTextureAsync(Arg.Any<string>(), Arg.Any<TextureScaleMode>(), Arg.Any<CancellationToken>())
            .Returns(_ =>
            {
                loadCallCount++;
                return tcs.Task;
            });

        var component = new TilemapComponent { Tilemap = MakeMinimalTilemap() };
        world.CreateEntity().AddComponent(component);
        world.Flush();

        var system = new TilemapSystem(textureLoader);
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.Zero));
        system.Update(world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.Zero));

        loadCallCount.Should().Be(1);
    }

    [Fact]
    public void Update_TilemapSwapped_ReinitializesAnimator()
    {
        var world = CreateTestWorld();
        var textureLoader = Substitute.For<ITextureLoader>();
        textureLoader.LoadTextureAsync(Arg.Any<string>(), Arg.Any<TextureScaleMode>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(Substitute.For<ITexture>()));

        var component = new TilemapComponent { Tilemap = MakeMinimalTilemap() };
        world.CreateEntity().AddComponent(component);
        world.Flush();

        var system = new TilemapSystem(textureLoader);
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.Zero));
        var firstAnimator = component.Animator;

        component.Tilemap = MakeMinimalTilemap();
        system.Update(world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.Zero));

        component.Animator.Should().NotBeNull();
        component.Animator.Should().NotBeSameAs(firstAnimator);
    }

    [Fact]
    public void Update_TilemapSwapped_ResetsIsLoaded()
    {
        var world = CreateTestWorld();
        var tcs = new TaskCompletionSource<ITexture>();
        var textureLoader = Substitute.For<ITextureLoader>();
        textureLoader.LoadTextureAsync(Arg.Any<string>(), Arg.Any<TextureScaleMode>(), Arg.Any<CancellationToken>())
            .Returns(tcs.Task);

        var component = new TilemapComponent { Tilemap = MakeMinimalTilemap() };
        world.CreateEntity().AddComponent(component);
        world.Flush();

        var system = new TilemapSystem(textureLoader);
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.Zero));

        component.Tilemap = MakeMinimalTilemap();
        system.Update(world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.Zero));

        component.IsLoaded.Should().BeFalse();
    }

    [Fact]
    public async Task Update_TilemapSwapped_StaleLoadDoesNotSetIsLoaded()
    {
        var world = CreateTestWorld();
        var firstTcs = new TaskCompletionSource<ITexture>();
        var secondTcs = new TaskCompletionSource<ITexture>();
        var callCount = 0;
        var textureLoader = Substitute.For<ITextureLoader>();
        textureLoader.LoadTextureAsync(Arg.Any<string>(), Arg.Any<TextureScaleMode>(), Arg.Any<CancellationToken>())
            .Returns(_ => ++callCount == 1 ? firstTcs.Task : secondTcs.Task);

        var component = new TilemapComponent { Tilemap = MakeMinimalTilemap() };
        world.CreateEntity().AddComponent(component);
        world.Flush();

        var system = new TilemapSystem(textureLoader);
        system.Update(world, new GameTime(TimeSpan.Zero, TimeSpan.Zero));

        component.Tilemap = MakeMinimalTilemap();
        system.Update(world, new GameTime(TimeSpan.FromSeconds(0.016), TimeSpan.Zero));

        // Complete the first (stale) load — it should NOT set IsLoaded.
        firstTcs.SetResult(Substitute.For<ITexture>());
        await Task.Yield();
        await Task.Delay(50);

        component.IsLoaded.Should().BeFalse();
    }
}
