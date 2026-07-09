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
        Assert.Equal(Vector2.One, sprite.Scale);
        Assert.False(sprite.FlipX);
        Assert.False(sprite.FlipY);
        Assert.Equal(0, sprite.Layer);
        Assert.Equal(0, sprite.OrderInLayer);
        Assert.Equal(BlendMode.Alpha, sprite.BlendMode);
        Assert.Equal(TextureScaleMode.Nearest, sprite.TextureScaleMode);
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
        sprite.Scale = new Vector2(2.5f, 3f);

        // Assert
        Assert.Equal(new Vector2(2.5f, 3f), sprite.Scale);
    }

    [Fact]
    public void Scale_NonUniform_WorksCorrectly()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;

        // Act — squash on Y, stretch on X
        sprite.Scale = new Vector2(2f, 0.5f);

        // Assert
        Assert.Equal(2f, sprite.Scale.X);
        Assert.Equal(0.5f, sprite.Scale.Y);
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
    public void Layer_CanBeMaxValue()
    {
        // Arrange
        var world = CreateTestWorld();
        var entity = world.CreateEntity();
        entity.AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;

        // Act
        sprite.Layer = byte.MaxValue;

        // Assert
        Assert.Equal(byte.MaxValue, sprite.Layer);
    }

    #endregion

    #region BlendMode, OrderInLayer, and TextureScaleMode Properties

    [Fact]
    public void BlendMode_DefaultIsAlpha()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity().AddComponent<SpriteComponent>();
        Assert.Equal(BlendMode.Alpha, entity.GetComponent<SpriteComponent>()!.BlendMode);
    }

    [Fact]
    public void BlendMode_SetAndGet_WorksCorrectly()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity().AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;

        sprite.BlendMode = BlendMode.Additive;

        Assert.Equal(BlendMode.Additive, sprite.BlendMode);
    }

    [Fact]
    public void OrderInLayer_DefaultIsZero()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity().AddComponent<SpriteComponent>();
        Assert.Equal(0, entity.GetComponent<SpriteComponent>()!.OrderInLayer);
    }

    [Fact]
    public void OrderInLayer_SetAndGet_WorksCorrectly()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity().AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;

        sprite.OrderInLayer = -5;
        Assert.Equal(-5, sprite.OrderInLayer);

        sprite.OrderInLayer = 100;
        Assert.Equal(100, sprite.OrderInLayer);
    }

    [Fact]
    public void TextureScaleMode_DefaultIsNearest()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity().AddComponent<SpriteComponent>();
        Assert.Equal(TextureScaleMode.Nearest, entity.GetComponent<SpriteComponent>()!.TextureScaleMode);
    }

    [Fact]
    public void TextureScaleMode_SetAndGet_WorksCorrectly()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity().AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;

        sprite.TextureScaleMode = TextureScaleMode.Linear;

        Assert.Equal(TextureScaleMode.Linear, sprite.TextureScaleMode);
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
                s.Scale = new Vector2(2.0f, 2.0f);
                s.FlipX = true;
                s.Layer = 10;
                s.OrderInLayer = 3;
                s.BlendMode = BlendMode.Additive;
            });

        var sprite = entity.GetComponent<SpriteComponent>()!;

        // Assert
        Assert.Equal("assets/player.png", sprite.TexturePath);
        Assert.Equal(mockTexture, sprite.Texture);
        Assert.Equal(new Rectangle(0, 0, 32, 32), sprite.SourceRect);
        Assert.Equal(new Color(255, 128, 128), sprite.Tint);
        Assert.Equal(new Vector2(16, 16), sprite.Offset);
        Assert.Equal(new Vector2(2.0f, 2.0f), sprite.Scale);
        Assert.True(sprite.FlipX);
        Assert.Equal(10, sprite.Layer);
        Assert.Equal(3, sprite.OrderInLayer);
        Assert.Equal(BlendMode.Additive, sprite.BlendMode);
    }

    #endregion

    #region Material Property

    [Fact]
    public void Material_DefaultIsNull()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity().AddComponent<SpriteComponent>();
        Assert.Null(entity.GetComponent<SpriteComponent>()!.Material);
    }

    [Fact]
    public void Material_SetAndGet_WorksCorrectly()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity().AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;
        var material = Substitute.For<IMaterial>();
        material.Name.Returns("TestMaterial");

        sprite.Material = material;

        Assert.Same(material, sprite.Material);
        Assert.Equal("TestMaterial", sprite.Material!.Name);
    }

    [Fact]
    public void Material_CanBeSetToNull()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity().AddComponent<SpriteComponent>();
        var sprite = entity.GetComponent<SpriteComponent>()!;
        sprite.Material = Substitute.For<IMaterial>();

        sprite.Material = null;

        Assert.Null(sprite.Material);
    }

    #endregion

    #region CrossFadeGhosts and Material defaults

    [Fact]
    public void Constructor_CrossFadeGhosts_DefaultsToEmpty()
    {
        var world = CreateTestWorld();
        var entity = world.CreateEntity().AddComponent<SpriteComponent>();
        Assert.Empty(entity.GetComponent<SpriteComponent>()!.CrossFadeGhosts);
    }

    #endregion
}
