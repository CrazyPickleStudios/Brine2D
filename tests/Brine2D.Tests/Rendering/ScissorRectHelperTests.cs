using Brine2D.Core;
using Brine2D.Rendering;

namespace Brine2D.Tests.Rendering;

public class ScissorRectHelperTests
{
    [Fact]
    public void Intersect_BothNull_ReturnsNull()
    {
        var result = ScissorRectHelper.Intersect(null, null);

        Assert.Null(result);
    }

    [Fact]
    public void Intersect_IncomingNull_ReturnsCurrent()
    {
        var current = new Rectangle(10, 20, 100, 50);

        var result = ScissorRectHelper.Intersect(current, null);

        Assert.Equal(current, result);
    }

    [Fact]
    public void Intersect_CurrentNull_ReturnsIncoming()
    {
        var incoming = new Rectangle(10, 20, 100, 50);

        var result = ScissorRectHelper.Intersect(null, incoming);

        Assert.Equal(incoming, result);
    }

    [Fact]
    public void Intersect_OverlappingRects_ReturnsIntersection()
    {
        var current = new Rectangle(0, 0, 100, 100);
        var incoming = new Rectangle(50, 50, 100, 100);

        var result = ScissorRectHelper.Intersect(current, incoming);

        Assert.NotNull(result);
        Assert.Equal(50, result.Value.X);
        Assert.Equal(50, result.Value.Y);
        Assert.Equal(50, result.Value.Width);
        Assert.Equal(50, result.Value.Height);
    }

    [Fact]
    public void Intersect_IncomingInsideCurrent_ReturnsIncoming()
    {
        var current = new Rectangle(0, 0, 200, 200);
        var incoming = new Rectangle(50, 50, 40, 40);

        var result = ScissorRectHelper.Intersect(current, incoming);

        Assert.NotNull(result);
        Assert.Equal(50, result.Value.X);
        Assert.Equal(50, result.Value.Y);
        Assert.Equal(40, result.Value.Width);
        Assert.Equal(40, result.Value.Height);
    }

    [Fact]
    public void Intersect_NonOverlapping_ReturnsZeroAreaAtIncomingPosition()
    {
        var current = new Rectangle(0, 0, 50, 50);
        var incoming = new Rectangle(200, 200, 50, 50);

        var result = ScissorRectHelper.Intersect(current, incoming);

        Assert.NotNull(result);
        Assert.Equal(200, result.Value.X);
        Assert.Equal(200, result.Value.Y);
        Assert.Equal(0, result.Value.Width);
        Assert.Equal(0, result.Value.Height);
    }

    [Fact]
    public void Intersect_AdjacentEdges_ReturnsZeroAreaAtIncomingPosition()
    {
        var current = new Rectangle(0, 0, 100, 100);
        var incoming = new Rectangle(100, 0, 100, 100);

        var result = ScissorRectHelper.Intersect(current, incoming);

        Assert.NotNull(result);
        Assert.Equal(0, result.Value.Width * result.Value.Height);
    }

    [Fact]
    public void Intersect_PartialOverlap_ClipsCorrectly()
    {
        var current = new Rectangle(10, 10, 80, 80);
        var incoming = new Rectangle(0, 0, 50, 50);

        var result = ScissorRectHelper.Intersect(current, incoming);

        Assert.NotNull(result);
        Assert.Equal(10, result.Value.X);
        Assert.Equal(10, result.Value.Y);
        Assert.Equal(40, result.Value.Width);
        Assert.Equal(40, result.Value.Height);
    }
}