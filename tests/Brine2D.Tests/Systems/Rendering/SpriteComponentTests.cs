using System.Numerics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Rendering;
using Brine2D.Systems.Rendering;
using NSubstitute;

namespace Brine2D.Tests.Systems.Rendering;

public class SpriteComponentTests : TestBase
{
    #region Default Values

    [Fact]
    public void Constructor_DefaultValues_AreCorrect()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;

        // Assert
        Assert.Equal(string.Empty, sprite.TexturePath);
        Assert.Null(sprite.Texture);
        Assert.Null(sprite.SourceRect);
        Assert.Equal(Color.White, sprite.Tint);
        Assert.Equal(Vector2.Zero, sprite.Offset);
        Assert.Equal(1.0f, sprite.Scale);
        Assert.False(sprite.FlipX);
        Assert.False(sprite.FlipY);
        Assert.Equal(0, sprite.Layer);
    }

    #endregion

    #region Texture Properties

    [Fact]
    public void TexturePath_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;

        // Act
        sprite.TexturePath = "assets/player.png";

        // Assert
        Assert.Equal("assets/player.png", sprite.TexturePath);
    }

    [Fact]
    public void Texture_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;
        var mockTexture = Substitute.For<ITexture>();

        // Act
        sprite.Texture = mockTexture;

        // Assert
        Assert.Equal(mockTexture, sprite.Texture);
    }

    [Fact]
    public void SourceRect_CanBeSet()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;

        // Act
        sprite.SourceRect = new Rectangle(0, 0, 32, 32);

        // Assert
        Assert.NotNull(sprite.SourceRect);
        Assert.Equal(new Rectangle(0, 0, 32, 32), sprite.SourceRect.Value);
    }

    [Fact]
    public void SourceRect_CanBeNull()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<SpriteComponent>(s => s.SourceRect = new Rectangle(0, 0, 32, 32));

        var sprite = entity.GetComponent<SpriteComponent>()!;

        // Act
        sprite.SourceRect = null;

        // Assert
        Assert.Null(sprite.SourceRect);
    }

    #endregion

    #region Visual Properties

    [Fact]
    public void Tint_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;

        // Act
        sprite.Tint = Color.Red;

        // Assert
        Assert.Equal(Color.Red, sprite.Tint);
    }

    [Fact]
    public void Tint_WithAlpha_Works()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity()
            .AddComponent<SpriteComponent>(s => s.Tint = new Color(255, 0, 0, 128));

        var sprite = entity.GetComponent<SpriteComponent>()!;

        // Assert
        Assert.Equal(128, sprite.Tint.A);
    }

    [Fact]
    public void Offset_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;

        // Act
        sprite.Offset = new Vector2(10, 20);

        // Assert
        Assert.Equal(new Vector2(10, 20), sprite.Offset);
    }

    [Fact]
    public void Scale_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;

        // Act
        sprite.Scale = 2.5f;

        // Assert
        Assert.Equal(2.5f, sprite.Scale);
    }

    #endregion

    #region Flip Properties

    [Fact]
    public void FlipX_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;

        // Act
        sprite.FlipX = true;

        // Assert
        Assert.True(sprite.FlipX);
    }

    [Fact]
    public void FlipY_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;

        // Act
        sprite.FlipY = true;

        // Assert
        Assert.True(sprite.FlipY);
    }

    [Fact]
    public void FlipX_CanBeToggled()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;

        // Act & Assert
        sprite.FlipX = true;
        Assert.True(sprite.FlipX);

        sprite.FlipX = false;
        Assert.False(sprite.FlipX);
    }

    [Fact]
    public void FlipY_CanBeToggled()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;

        // Act & Assert
        sprite.FlipY = true;
        Assert.True(sprite.FlipY);

        sprite.FlipY = false;
        Assert.False(sprite.FlipY);
    }

    #endregion

    #region Layer Property

    [Fact]
    public void Layer_SetAndGet_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;

        // Act
        sprite.Layer = 5;

        // Assert
        Assert.Equal(5, sprite.Layer);
    }

    [Fact]
    public void Layer_CanBeNegative()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;

        // Act
        sprite.Layer = -10;

        // Assert
        Assert.Equal(-10, sprite.Layer);
    }

    #endregion

    #region Integration Scenarios

    [Fact]
    public void SpriteComponent_CompleteSetup_WorksCorrectly()
    {
        // Arrange & Act
        var world = CreateTestWorld();
        var mockTexture = Substitute.For<ITexture>();
        
        var entity = world.CreateEntity()
            .AddComponent<SpriteComponent>(s =>
            {
                s.TexturePath = "assets/player.png";
                s.Texture = mockTexture;
                s.SourceRect = new Rectangle(0, 0, 32, 32);
                s.Tint = new Color(255, 128, 128);
                s.Offset = new Vector2(16, 16);
                s.Scale = 2.0f;
                s.FlipX = true;
                s.Layer = 10;
            });

        var sprite = entity.GetComponent<SpriteComponent>()!;

        // Assert
        Assert.Equal("assets/player.png", sprite.TexturePath);
        Assert.Equal(mockTexture, sprite.Texture);
        Assert.Equal(new Rectangle(0, 0, 32, 32), sprite.SourceRect);
        Assert.Equal(new Color(255, 128, 128), sprite.Tint);
        Assert.Equal(new Vector2(16, 16), sprite.Offset);
        Assert.Equal(2.0f, sprite.Scale);
        Assert.True(sprite.FlipX);
        Assert.Equal(10, sprite.Layer);
    }

    #endregion
}