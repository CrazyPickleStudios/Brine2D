using Brine2D.Tilemap;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.Tilemap;

public sealed class TilesetTests
{
    [Fact]
    public void GetTileSourceRect_EmptyTile_ReturnsZeroRect()
    {
        var tileset = new Tileset { TileWidth = 16, TileHeight = 16, Columns = 4, FirstGid = 1 };

        tileset.GetTileSourceRect(0).Should().Be((0, 0, 0, 0));
    }

    [Fact]
    public void GetTileSourceRect_FirstTile_NoSpacingOrMargin_ReturnsOrigin()
    {
        var tileset = new Tileset { TileWidth = 16, TileHeight = 16, Columns = 4, FirstGid = 1 };

        tileset.GetTileSourceRect(1).Should().Be((0, 0, 16, 16));
    }

    [Fact]
    public void GetTileSourceRect_ThirdColumn_FirstRow_NoSpacing()
    {
        var tileset = new Tileset { TileWidth = 16, TileHeight = 16, Columns = 4, FirstGid = 1 };

        // GID 3 = localId 2, column=2, row=0
        tileset.GetTileSourceRect(3).Should().Be((32, 0, 16, 16));
    }

    [Fact]
    public void GetTileSourceRect_SecondRow_NoSpacing()
    {
        var tileset = new Tileset { TileWidth = 16, TileHeight = 16, Columns = 4, FirstGid = 1 };

        // GID 5 = localId 4, column=0, row=1
        tileset.GetTileSourceRect(5).Should().Be((0, 16, 16, 16));
    }

    [Fact]
    public void GetTileSourceRect_WithSpacing_SecondTile_IncludesSpacingInXOffset()
    {
        var tileset = new Tileset { TileWidth = 16, TileHeight = 16, Columns = 4, FirstGid = 1, Spacing = 2 };

        // GID 2 = localId 1, column=1, row=0: x = 0 + 1*(16+2) = 18
        tileset.GetTileSourceRect(2).Should().Be((18, 0, 16, 16));
    }

    [Fact]
    public void GetTileSourceRect_WithSpacing_SecondRow_IncludesSpacingInYOffset()
    {
        var tileset = new Tileset { TileWidth = 16, TileHeight = 16, Columns = 4, FirstGid = 1, Spacing = 2 };

        // GID 5 = localId 4, column=0, row=1: y = 0 + 1*(16+2) = 18
        tileset.GetTileSourceRect(5).Should().Be((0, 18, 16, 16));
    }

    [Fact]
    public void GetTileSourceRect_WithMargin_FirstTile_OffsetsByMargin()
    {
        var tileset = new Tileset { TileWidth = 16, TileHeight = 16, Columns = 4, FirstGid = 1, Margin = 4 };

        // GID 1 = localId 0, column=0, row=0: x=4, y=4
        tileset.GetTileSourceRect(1).Should().Be((4, 4, 16, 16));
    }

    [Fact]
    public void GetTileSourceRect_WithMarginAndSpacing_CombinesBothCorrectly()
    {
        var tileset = new Tileset { TileWidth = 16, TileHeight = 16, Columns = 4, FirstGid = 1, Spacing = 2, Margin = 4 };

        // GID 2 = localId 1, column=1, row=0: x = 4 + 1*(16+2) = 22, y = 4
        tileset.GetTileSourceRect(2).Should().Be((22, 4, 16, 16));

        // GID 5 = localId 4, column=0, row=1: x = 4, y = 4 + 1*(16+2) = 22
        tileset.GetTileSourceRect(5).Should().Be((4, 22, 16, 16));
    }

    [Fact]
    public void GetTileSourceRect_NonOneFirstGid_OffsetsByFirstGid()
    {
        var tileset = new Tileset { TileWidth = 16, TileHeight = 16, Columns = 4, FirstGid = 9 };

        // GID 9 = localId 0: (0, 0)
        tileset.GetTileSourceRect(9).Should().Be((0, 0, 16, 16));

        // GID 10 = localId 1, column=1: (16, 0)
        tileset.GetTileSourceRect(10).Should().Be((16, 0, 16, 16));
    }

    [Fact]
    public void GetTileSourceRect_WidthAndHeight_AlwaysReturnTileDimensions()
    {
        var tileset = new Tileset { TileWidth = 24, TileHeight = 32, Columns = 4, FirstGid = 1, Spacing = 2, Margin = 4 };

        var (_, _, width, height) = tileset.GetTileSourceRect(1);

        width.Should().Be(24);
        height.Should().Be(32);
    }
}