using Brine2D.Rendering;

namespace Brine2D.Tests.Rendering;

public class RenderingOptionsTests
{
    [Fact]
    public void PixelSnapping_DefaultIsTrue()
    {
        var options = new RenderingOptions();

        Assert.True(options.PixelSnapping);
    }

    [Fact]
    public void PixelSnapping_CanBeDisabled()
    {
        var options = new RenderingOptions { PixelSnapping = false };

        Assert.False(options.PixelSnapping);
    }

    [Fact]
    public void MsaaSamples_DefaultIsOne()
    {
        var options = new RenderingOptions();

        Assert.Equal(1, options.MsaaSamples);
    }

    [Fact]
    public void MsaaSamples_CanBeSetToTwo()
    {
        var options = new RenderingOptions { MsaaSamples = 2 };

        Assert.Equal(2, options.MsaaSamples);
    }

    [Fact]
    public void MaxVerticesPerFrame_DefaultIs50000()
    {
        var options = new RenderingOptions();

        Assert.Equal(50_000, options.MaxVerticesPerFrame);
    }

    [Fact]
    public void MaxParticlesPerFrame_DefaultIs20000()
    {
        var options = new RenderingOptions();

        Assert.Equal(20_000, options.MaxParticlesPerFrame);
    }

    [Fact]
    public void VSync_DefaultIsTrue()
    {
        var options = new RenderingOptions();

        Assert.True(options.VSync);
    }
}
