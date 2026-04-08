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
        system.Render(world, mockRenderer);

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
        system.Render(world, mockRenderer);

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
        system.Render(world, mockRenderer);

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
        system.Render(world, mockRenderer);

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
        system.Render(world, mockRenderer);

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
        var mockCamera = Substitute.For<ICamera>();
        var mockTexture = Substitute.For<ITexture>();
        mockTexture.Width.Returns(32);
        mockTexture.Height.Returns(32);

        mockCamera.Position.Returns(Vector2.Zero);
        mockCamera.ViewportWidth.Returns(800);
        mockCamera.ViewportHeight.Returns(600);
        mockCamera.Zoom.Returns(1f);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(10000, 10000))
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0, 0))
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader, mockCamera);

        // Act
        system.Render(world, mockRenderer);

        // Assert
        Assert.Equal(1, system.GetBatchStats().RenderedCount);
        Assert.Equal(2, system.GetTotalSpriteCount());
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
        system.Render(world, mockRenderer);
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
        system.Render(world, mockRenderer);

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
}