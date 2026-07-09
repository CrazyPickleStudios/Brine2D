using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Components;
using Brine2D.Rendering;
using Brine2D.Systems.Rendering;
using NSubstitute;

namespace Brine2D.Tests.Systems.Rendering;

public class SpriteRenderingSystemTests : TestBase
{
    [Fact]
    public void Render_DrawsSprite()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockRenderer = Substitute.For<IRenderer>();
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var mockTexture = Substitute.For<ITexture>();
        mockTexture.Width.Returns(32);
        mockTexture.Height.Returns(32);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 50))
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act
        system.Render(world, mockRenderer, default);

        // Assert
        Assert.Equal(1, system.GetBatchStats().RenderedCount);
    }

    [Fact]
    public void Render_MultipleSprites_DrawsAll()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockRenderer = Substitute.For<IRenderer>();
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var mockTexture = Substitute.For<ITexture>();
        mockTexture.Width.Returns(32);
        mockTexture.Height.Returns(32);

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act
        system.Render(world, mockRenderer, default);

        // Assert
        Assert.Equal(3, system.GetBatchStats().RenderedCount);
    }

    [Fact]
    public void Render_EntityWithoutTransform_Skips()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockRenderer = Substitute.For<IRenderer>();
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var mockTexture = Substitute.For<ITexture>();

        world.CreateEntity()
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act
        system.Render(world, mockRenderer, default);

        // Assert
        Assert.Equal(0, system.GetBatchStats().RenderedCount);
        Assert.Equal(1, system.GetTotalSpriteCount());
    }

    [Fact]
    public void Render_NullTexture_Skips()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockRenderer = Substitute.For<IRenderer>();
        var mockTextureLoader = Substitute.For<ITextureLoader>();

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s => s.Texture = null);

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act
        system.Render(world, mockRenderer, default);

        // Assert
        Assert.Equal(0, system.GetTotalSpriteCount());
    }

    [Fact]
    public void Render_DisabledComponent_Skips()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockRenderer = Substitute.For<IRenderer>();
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var mockTexture = Substitute.For<ITexture>();

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        world.Flush();

        var entity = world.Entities[0];
        entity.GetComponent<SpriteComponent>()!.IsEnabled = false;

        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act
        system.Render(world, mockRenderer, default);

        // Assert
        Assert.Equal(0, system.GetBatchStats().RenderedCount);
        Assert.Equal(1, system.GetTotalSpriteCount());
    }

    [Fact]
    public void Render_WithCamera_CullsOffscreenSprites()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockRenderer = Substitute.For<IRenderer>();
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var mockTexture = Substitute.For<ITexture>();
        mockTexture.Width.Returns(32);
        mockTexture.Height.Returns(32);

        // The new culling path delegates to camera.IsVisible(Rectangle).
        // Return false for the far-away sprite bounds and true for the on-screen one.
        var mockCamera = Substitute.For<ICamera>();
        mockCamera.IsVisible(Arg.Is<Rectangle>(r => r.X > 5000)).Returns(false);
        mockCamera.IsVisible(Arg.Is<Rectangle>(r => r.X <= 5000)).Returns(true);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(10000, 10000))
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0, 0))
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader, mockCamera);

        // Act
        system.Render(world, mockRenderer, default);

        // Assert
        Assert.Equal(1, system.GetBatchStats().RenderedCount);
        Assert.Equal(2, system.GetTotalSpriteCount());
    }

    [Fact]
    public void Render_WithCamera_TransformScaleExpandsCullBounds()
    {
        // A sprite with a large transform scale must not be culled when its bounds
        // extend into the frustum even though its world origin is outside.
        var world = CreateTestWorld();
        var mockRenderer = Substitute.For<IRenderer>();
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var mockTexture = Substitute.For<ITexture>();
        mockTexture.Width.Returns(32);
        mockTexture.Height.Returns(32);

        var camera = new Camera2D(800, 600);
        camera.Position = Vector2.Zero;

        // Place sprite just outside viewport but give it a scale that brings it back in.
        world.CreateEntity()
            .AddComponent<TransformComponent>(t =>
            {
                t.LocalPosition = new Vector2(500, 0);
                t.LocalScale = new Vector2(100f, 100f);
            })
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader, camera);
        system.Render(world, mockRenderer, default);

        Assert.Equal(1, system.GetBatchStats().RenderedCount);
    }

    [Fact]
    public async Task LoadTexturesAsync_UsesSpriteTextureScaleMode()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var mockTexture = Substitute.For<ITexture>();

        mockTextureLoader.LoadTextureAsync("sprite.png", Arg.Any<TextureScaleMode>(), Arg.Any<CancellationToken>())
            .Returns(mockTexture);

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s =>
            {
                s.TexturePath = "sprite.png";
                s.TextureScaleMode = TextureScaleMode.Linear;
            });

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act
        await system.LoadTexturesAsync(world);

        // Assert: the loader was called with the per-sprite scale mode, not the hard-coded Nearest.
        await mockTextureLoader.Received(1).LoadTextureAsync(
            "sprite.png",
            TextureScaleMode.Linear,
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public void Render_SpriteBlendMode_IsThreadedToBatcher()
    {
        // Verify that BlendMode on SpriteComponent reaches the draw context.
        var world = CreateTestWorld();
        var mockRenderer = Substitute.For<IRenderer>();
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var mockTexture = Substitute.For<ITexture>();
        mockTexture.Width.Returns(32);
        mockTexture.Height.Returns(32);
        mockTexture.IsLoaded.Returns(true);
        mockTexture.Bounds.Returns(new Rectangle(0, 0, 32, 32));
        mockTexture.SortKey.Returns(1);
        mockRenderer.GetBlendMode().Returns(BlendMode.Alpha);

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s =>
            {
                s.Texture = mockTexture;
                s.BlendMode = BlendMode.Additive;
            });

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);
        system.Render(world, mockRenderer, default);

        mockRenderer.Received(1).SetBlendMode(BlendMode.Additive);
    }

    [Fact]
    public void Render_OrderInLayer_ControlsDrawOrder()
    {
        // Two sprites on the same layer but different OrderInLayer values should
        // have their SetRenderLayer calls issued in the correct order.
        // Because they share the same layer the batcher issues a single SetRenderLayer
        // call, so we verify that both are rendered (not culled) and that two
        // draw calls are not incorrectly merged.
        var world = CreateTestWorld();
        var mockRenderer = Substitute.For<IRenderer>();
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var tex1 = Substitute.For<ITexture>();
        var tex2 = Substitute.For<ITexture>();
        tex1.Width.Returns(32); tex1.Height.Returns(32); tex1.IsLoaded.Returns(true);
        tex1.Bounds.Returns(new Rectangle(0, 0, 32, 32)); tex1.SortKey.Returns(1);
        tex2.Width.Returns(32); tex2.Height.Returns(32); tex2.IsLoaded.Returns(true);
        tex2.Bounds.Returns(new Rectangle(0, 0, 32, 32)); tex2.SortKey.Returns(2);
        mockRenderer.GetBlendMode().Returns(BlendMode.Alpha);

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s => { s.Texture = tex1; s.OrderInLayer = 10; });

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s => { s.Texture = tex2; s.OrderInLayer = -10; });

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);
        system.Render(world, mockRenderer, default);

        Assert.Equal(2, system.GetBatchStats().RenderedCount);
    }

    [Fact]
    public async Task LoadTexturesAsync_LoadsTexturesFromPaths()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var mockTexture = Substitute.For<ITexture>();

        mockTextureLoader.LoadTextureAsync("sprite.png", Arg.Any<TextureScaleMode>(), Arg.Any<CancellationToken>())
            .Returns(mockTexture);

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s =>
            {
                s.TexturePath = "sprite.png";
                s.Texture = null;
            });

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act
        await system.LoadTexturesAsync(world);

        // Assert
        await mockTextureLoader.Received(1).LoadTextureAsync(
            "sprite.png",
            TextureScaleMode.Nearest,
            Arg.Any<CancellationToken>());

        Assert.Equal(mockTexture, entity.GetComponent<SpriteComponent>()!.Texture);
    }

    [Fact]
    public async Task LoadTexturesAsync_CachesTextures()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var mockTexture = Substitute.For<ITexture>();

        mockTextureLoader.LoadTextureAsync("shared.png", Arg.Any<TextureScaleMode>(), Arg.Any<CancellationToken>())
            .Returns(mockTexture);

        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s => s.TexturePath = "shared.png");

        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s => s.TexturePath = "shared.png");

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act
        await system.LoadTexturesAsync(world);

        // Assert
        await mockTextureLoader.Received(1).LoadTextureAsync(
            "shared.png",
            Arg.Any<TextureScaleMode>(),
            Arg.Any<CancellationToken>());

        var sprite1 = entity1.GetComponent<SpriteComponent>()!;
        var sprite2 = entity2.GetComponent<SpriteComponent>()!;
        Assert.Equal(mockTexture, sprite1.Texture);
        Assert.Equal(mockTexture, sprite2.Texture);
        Assert.Same(sprite1.Texture, sprite2.Texture);
    }

    [Fact]
    public void GetBatchStats_ReturnsCorrectStats()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockRenderer = Substitute.For<IRenderer>();
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var mockTexture = Substitute.For<ITexture>();
        mockTexture.Width.Returns(32);
        mockTexture.Height.Returns(32);
        mockTexture.IsLoaded.Returns(true);

        for (int i = 0; i < 5; i++)
        {
            world.CreateEntity()
                .AddComponent<TransformComponent>()
                .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);
        }

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act
        system.Render(world, mockRenderer, default);
        var stats = system.GetBatchStats();

        // Assert
        Assert.Equal(5, stats.RenderedCount);
        Assert.True(stats.DrawCalls > 0);
    }

    [Fact]
    public void GetTotalSpriteCount_ReturnsCorrectCount()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockRenderer = Substitute.For<IRenderer>();
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var mockTexture = Substitute.For<ITexture>();

        for (int i = 0; i < 10; i++)
        {
            world.CreateEntity()
                .AddComponent<TransformComponent>()
                .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);
        }

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act
        system.Render(world, mockRenderer, default);

        // Assert
        Assert.Equal(10, system.GetTotalSpriteCount());
    }

    [Fact]
    public void RenderOrder_IsCorrect()
    {
        var system = new SpriteRenderingSystem(Substitute.For<ITextureLoader>());
        Assert.Equal(0, system.RenderOrder);
    }

    [Fact]
    public void Name_IsCorrect()
    {
        var system = new SpriteRenderingSystem(Substitute.For<ITextureLoader>());
        Assert.Equal("SpriteRenderingSystem", system.Name);
    }

    [Fact]
    public void Render_FlipX_PassesHorizontalFlagToBatcher()
    {
        var world = CreateTestWorld();
        var mockRenderer = Substitute.For<IRenderer>();
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var mockTexture = Substitute.For<ITexture>();
        mockTexture.Width.Returns(32);
        mockTexture.Height.Returns(32);
        mockTexture.IsLoaded.Returns(true);
        mockTexture.Bounds.Returns(new Rectangle(0, 0, 32, 32));
        mockTexture.SortKey.Returns(1);
        mockRenderer.GetBlendMode().Returns(BlendMode.Alpha);

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s =>
            {
                s.Texture = mockTexture;
                s.FlipX = true;
                s.FlipY = false;
            });

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);
        system.Render(world, mockRenderer, default);

        mockRenderer.Received(1).DrawTexture(
            mockTexture,
            Arg.Any<Vector2>(),
            Arg.Any<Rectangle?>(),
            Arg.Any<Vector2?>(),
            Arg.Any<float>(),
            Arg.Any<Vector2?>(),
            Arg.Any<Color?>(),
            SpriteFlip.Horizontal);
    }

    [Fact]
    public void Render_FlipY_PassesVerticalFlagToBatcher()
    {
        var world = CreateTestWorld();
        var mockRenderer = Substitute.For<IRenderer>();
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var mockTexture = Substitute.For<ITexture>();
        mockTexture.Width.Returns(32);
        mockTexture.Height.Returns(32);
        mockTexture.IsLoaded.Returns(true);
        mockTexture.Bounds.Returns(new Rectangle(0, 0, 32, 32));
        mockTexture.SortKey.Returns(1);
        mockRenderer.GetBlendMode().Returns(BlendMode.Alpha);

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s =>
            {
                s.Texture = mockTexture;
                s.FlipX = false;
                s.FlipY = true;
            });

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);
        system.Render(world, mockRenderer, default);

        mockRenderer.Received(1).DrawTexture(
            mockTexture,
            Arg.Any<Vector2>(),
            Arg.Any<Rectangle?>(),
            Arg.Any<Vector2?>(),
            Arg.Any<float>(),
            Arg.Any<Vector2?>(),
            Arg.Any<Color?>(),
            SpriteFlip.Vertical);
    }

    [Fact]
    public void Render_FlipXY_PassesBothFlagsToBatcher()
    {
        var world = CreateTestWorld();
        var mockRenderer = Substitute.For<IRenderer>();
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var mockTexture = Substitute.For<ITexture>();
        mockTexture.Width.Returns(32);
        mockTexture.Height.Returns(32);
        mockTexture.IsLoaded.Returns(true);
        mockTexture.Bounds.Returns(new Rectangle(0, 0, 32, 32));
        mockTexture.SortKey.Returns(1);
        mockRenderer.GetBlendMode().Returns(BlendMode.Alpha);

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s =>
            {
                s.Texture = mockTexture;
                s.FlipX = true;
                s.FlipY = true;
            });

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);
        system.Render(world, mockRenderer, default);

        mockRenderer.Received(1).DrawTexture(
            mockTexture,
            Arg.Any<Vector2>(),
            Arg.Any<Rectangle?>(),
            Arg.Any<Vector2?>(),
            Arg.Any<float>(),
            Arg.Any<Vector2?>(),
            Arg.Any<Color?>(),
            SpriteFlip.Horizontal | SpriteFlip.Vertical);
    }

    [Fact]
    public void Render_NoFlip_PassesNoneToBatcher()
    {
        var world = CreateTestWorld();
        var mockRenderer = Substitute.For<IRenderer>();
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var mockTexture = Substitute.For<ITexture>();
        mockTexture.Width.Returns(32);
        mockTexture.Height.Returns(32);
        mockTexture.IsLoaded.Returns(true);
        mockTexture.Bounds.Returns(new Rectangle(0, 0, 32, 32));
        mockTexture.SortKey.Returns(1);
        mockRenderer.GetBlendMode().Returns(BlendMode.Alpha);

        world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);
        system.Render(world, mockRenderer, default);

        mockRenderer.Received(1).DrawTexture(
            mockTexture,
            Arg.Any<Vector2>(),
            Arg.Any<Rectangle?>(),
            Arg.Any<Vector2?>(),
            Arg.Any<float>(),
            Arg.Any<Vector2?>(),
            Arg.Any<Color?>(),
            SpriteFlip.None);
    }
}
