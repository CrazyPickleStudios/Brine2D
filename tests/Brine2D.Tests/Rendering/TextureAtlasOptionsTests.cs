using Brine2D.Rendering;
using Brine2D.Rendering.TextureAtlas;

namespace Brine2D.Tests.Rendering;

public class TextureAtlasOptionsTests
{
    [Fact]
    public void Defaults_AreCorrect()
    {
        var options = new TextureAtlasOptions();

        Assert.Equal(2048, options.MaxAtlasWidth);
        Assert.Equal(2048, options.MaxAtlasHeight);
        Assert.Equal(2, options.Padding);
        Assert.True(options.UsePowerOfTwo);
        Assert.Equal(TextureScaleMode.Nearest, options.DefaultScaleMode);
        Assert.Equal(0, options.Extrude);
    }

    [Fact]
    public void Extrude_CanBeSet()
    {
        var options = new TextureAtlasOptions { Extrude = 2 };

        Assert.Equal(2, options.Extrude);
    }

    [Fact]
    public void Extrude_DefaultIsZero()
    {
        var options = new TextureAtlasOptions();

        Assert.Equal(0, options.Extrude);
    }

    [Fact]
    public void Padding_CanBeChanged()
    {
        var options = new TextureAtlasOptions { Padding = 4 };

        Assert.Equal(4, options.Padding);
    }

    [Fact]
    public void UsePowerOfTwo_CanBeDisabled()
    {
        var options = new TextureAtlasOptions { UsePowerOfTwo = false };

        Assert.False(options.UsePowerOfTwo);
    }

    [Fact]
    public void DefaultScaleMode_CanBeLinear()
    {
        var options = new TextureAtlasOptions { DefaultScaleMode = TextureScaleMode.Linear };

        Assert.Equal(TextureScaleMode.Linear, options.DefaultScaleMode);
    }
}
