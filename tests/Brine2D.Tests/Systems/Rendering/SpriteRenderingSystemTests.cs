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

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(100, 50))
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act
        system.Render(mockRenderer, world);

        // Assert - System uses batching, so we verify stats instead
        var stats = system.GetBatchStats();
        Assert.Equal(1, stats.RenderedCount);
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

        var entity1 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        var entity2 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        var entity3 = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act
        system.Render(mockRenderer, world);

        // Assert
        var stats = system.GetBatchStats();
        Assert.Equal(3, stats.RenderedCount);
    }

    [Fact]
    public void Render_EntityWithoutTransform_Skips()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockRenderer = Substitute.For<IRenderer>();
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var mockTexture = Substitute.For<ITexture>();

        var entity = world.CreateEntity()
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);
        // No TransformComponent

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act
        system.Render(mockRenderer, world);

        // Assert - Should be culled (no transform)
        var stats = system.GetBatchStats();
        Assert.Equal(0, stats.RenderedCount);
        Assert.Equal(1, system.GetTotalSpriteCount());
    }

    [Fact]
    public void Render_NullTexture_Skips()
    {
        // Arrange
        var world = CreateTestWorld();
        var mockRenderer = Substitute.For<IRenderer>();
        var mockTextureLoader = Substitute.For<ITextureLoader>();

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s => s.Texture = null);

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act
        system.Render(mockRenderer, world);

        // Assert - No texture means not added to batch
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

        var entity = world.CreateEntity()
            .AddComponent<TransformComponent>()
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        world.Flush();

        var sprite = entity.GetComponent<SpriteComponent>()!;
        sprite.IsEnabled = false;

        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act
        system.Render(mockRenderer, world);

        // Assert - Disabled components are culled
        var stats = system.GetBatchStats();
        Assert.Equal(0, stats.RenderedCount);
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

        // Camera at (0,0) with viewport 800x600, zoom 1
        mockCamera.Position.Returns(Vector2.Zero);
        mockCamera.ViewportWidth.Returns(800);
        mockCamera.ViewportHeight.Returns(600);
        mockCamera.Zoom.Returns(1f);

        // Sprite way off screen
        var offscreenEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(10000, 10000))
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        // Sprite on screen
        var onscreenEntity = world.CreateEntity()
            .AddComponent<TransformComponent>(t => t.LocalPosition = new Vector2(0, 0))
            .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader, mockCamera);

        // Act
        system.Render(mockRenderer, world);

        // Assert - Only onscreen sprite rendered
        var stats = system.GetBatchStats();
        Assert.Equal(1, stats.RenderedCount); // 1 visible
        Assert.Equal(2, system.GetTotalSpriteCount()); // 2 total
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
                s.Texture = null; // Not loaded yet
            });

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act
        await system.LoadTexturesAsync(world);

        // Assert - Texture should be loaded
        await mockTextureLoader.Received(1).LoadTextureAsync(
            "sprite.png", 
            TextureScaleMode.Nearest, 
            Arg.Any<CancellationToken>());
        
        var sprite = entity.GetComponent<SpriteComponent>()!;
        Assert.Equal(mockTexture, sprite.Texture);
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

        // Two entities sharing same texture path
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

        // Assert - Texture loaded only once, reused for both sprites
        await mockTextureLoader.Received(1).LoadTextureAsync(
            "shared.png", 
            Arg.Any<TextureScaleMode>(), 
            Arg.Any<CancellationToken>());
        
        var sprite1 = entity1.GetComponent<SpriteComponent>()!;
        var sprite2 = entity2.GetComponent<SpriteComponent>()!;
        Assert.Equal(mockTexture, sprite1.Texture);
        Assert.Equal(mockTexture, sprite2.Texture);
        Assert.Same(sprite1.Texture, sprite2.Texture); // Same instance
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

        for (int i = 0; i < 5; i++)
        {
            world.CreateEntity()
                .AddComponent<TransformComponent>()
                .AddComponent<SpriteComponent>(s => s.Texture = mockTexture);
        }

        world.Flush();

        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act
        system.Render(mockRenderer, world);
        var stats = system.GetBatchStats();

        // Assert
        Assert.Equal(5, stats.RenderedCount);
        Assert.True(stats.DrawCalls > 0); // At least one draw call
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
        system.Render(mockRenderer, world);

        // Assert
        Assert.Equal(10, system.GetTotalSpriteCount());
    }

    [Fact]
    public void RenderOrder_IsCorrect()
    {
        // Arrange
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act & Assert
        Assert.Equal(0, system.RenderOrder);
    }

    [Fact]
    public void Name_IsCorrect()
    {
        // Arrange
        var mockTextureLoader = Substitute.For<ITextureLoader>();
        var system = new SpriteRenderingSystem(mockTextureLoader);

        // Act & Assert
        Assert.Equal("SpriteRenderingSystem", system.Name);
    }
}