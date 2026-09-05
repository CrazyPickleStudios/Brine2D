using Brine2D.Core;
using Brine2D.Rendering.SDL.PostProcessing.Effects;

namespace Brine2D.Tests.PostProcessing.Effects;

public class LetterboxEffectTests
{
    [Fact]
    public void Constructor_WithNonPositiveDesignWidth_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LetterboxEffect(0, 720));
    }

    [Fact]
    public void Constructor_WithNonPositiveDesignHeight_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => new LetterboxEffect(1280, -1));
    }

    [Fact]
    public void Constructor_SetsDefaults()
    {
        var effect = new LetterboxEffect(1280, 720);

        Assert.Equal("Letterbox", effect.Name);
        Assert.True(effect.Enabled);
        Assert.Equal(int.MaxValue, effect.Order);
        Assert.Equal(new Color(0, 0, 0, 255), effect.BarColor);
    }

    [Fact]
    public void ComputeFitRect_WithDegenerateSwapchain_ReturnsZeroRect()
    {
        var effect = new LetterboxEffect(1280, 720);

        var rect = effect.ComputeFitRect(0, 720);

        Assert.Equal((0, 0, 0, 0), rect);
    }

    [Fact]
    public void ComputeFitRect_WithMatchingAspectRatio_FillsWindowExactly()
    {
        var effect = new LetterboxEffect(1280, 720);

        var (x, y, width, height) = effect.ComputeFitRect(1920, 1080);

        Assert.Equal(0, x);
        Assert.Equal(0, y);
        Assert.Equal(1920, width);
        Assert.Equal(1080, height);
    }

    [Fact]
    public void ComputeFitRect_WithWiderWindow_PillarboxesLeftAndRight()
    {
        // Design is 4:3, window is much wider (16:9) -> fit to height, bars on left/right.
        var effect = new LetterboxEffect(800, 600);

        var (x, y, width, height) = effect.ComputeFitRect(1920, 1080);

        Assert.Equal(1080, height);
        Assert.Equal(1440, width); // 1080 * (800/600) = 1440
        Assert.Equal(0, y);
        Assert.Equal((1920 - 1440) / 2, x);
        Assert.True(x > 0);
    }

    [Fact]
    public void ComputeFitRect_WithTallerWindow_LetterboxesTopAndBottom()
    {
        // Design is 16:9, window is much taller (portrait) -> fit to width, bars on top/bottom.
        var effect = new LetterboxEffect(1280, 720);

        var (x, y, width, height) = effect.ComputeFitRect(1080, 1920);

        Assert.Equal(1080, width);
        Assert.Equal(608, height); // round(1080 / (1280/720)) = 607.5 -> 608
        Assert.Equal(0, x);
        Assert.Equal((1920 - 608) / 2, y);
        Assert.True(y > 0);
    }

    [Fact]
    public void ComputeFitRect_ResultIsAlwaysCenteredAndWithinBounds()
    {
        var effect = new LetterboxEffect(1280, 720);

        var (x, y, width, height) = effect.ComputeFitRect(1000, 2000);

        Assert.InRange(width, 1, 1000);
        Assert.InRange(height, 1, 2000);
        Assert.Equal(x, (1000 - width) / 2);
        Assert.Equal(y, (2000 - height) / 2);
    }

    [Fact]
    public void SetDimensions_UpdatesFitRectComputationAtRuntime()
    {
        var effect = new LetterboxEffect(1280, 720);
        effect.SetDimensions(1920, 1080);

        // ComputeFitRect takes explicit dimensions, but SetDimensions is what Apply()
        // uses internally, so this test only validates it doesn't throw and dimensions
        // are independently queryable via ComputeFitRect with the same values.
        var rect = effect.ComputeFitRect(1920, 1080);

        Assert.Equal(1920, rect.Width);
        Assert.Equal(1080, rect.Height);
    }

    [Fact]
    public void BarColor_CanBeCustomized()
    {
        var effect = new LetterboxEffect(1280, 720)
        {
            BarColor = new Color(255, 0, 0, 255)
        };

        Assert.Equal(new Color(255, 0, 0, 255), effect.BarColor);
    }
}
