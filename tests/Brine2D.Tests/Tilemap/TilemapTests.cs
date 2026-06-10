using Brine2D.Core;
using Brine2D.Tilemap;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.Tilemap;

public sealed class TilemapTests
{
    private static Tileset MakeTileset(int firstGid = 1, bool makeSolid = true)
    {
        var ts = new Tileset
        {
            FirstGid = firstGid,
            TileWidth = 16,
            TileHeight = 16,
            Columns = 4,
            Rows = 4
        };

        if (makeSolid)
            ts.TileProperties[firstGid] = new TileProperties(firstGid) { IsSolid = true };

        return ts;
    }

    private static TilemapLayer MakeCollisionLayer(string name, int w, int h, float offsetX = 0f, float offsetY = 0f)
    {
        var layer = new TilemapLayer(name, w, h)
        {
            HasCollision = true,
            OffsetX = offsetX,
            OffsetY = offsetY
        };
        return layer;
    }

    [Fact]
    public void GenerateCollisionRects_NoOffset_RectOriginIsAtTileGridPosition()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        var ts = MakeTileset(firstGid: 1, makeSolid: true);
        map.AddTileset(ts);

        var layer = MakeCollisionLayer("col", 4, 4);
        layer.SetTile(1, 2, new Tile(1));
        map.AddLayer(layer);

        var rects = map.GenerateCollisionRects("col");

        rects.Should().HaveCount(1);
        rects[0].X.Should().Be(1 * 16);
        rects[0].Y.Should().Be(2 * 16);
        rects[0].Width.Should().Be(16);
        rects[0].Height.Should().Be(16);
    }

    [Fact]
    public void GenerateCollisionRects_WithPositiveOffset_RectIsShiftedByLayerOffset()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        var ts = MakeTileset(firstGid: 1, makeSolid: true);
        map.AddTileset(ts);

        var layer = MakeCollisionLayer("col", 4, 4, offsetX: 32f, offsetY: 48f);
        layer.SetTile(0, 0, new Tile(1));
        map.AddLayer(layer);

        var rects = map.GenerateCollisionRects("col");

        rects.Should().HaveCount(1);
        rects[0].X.Should().Be(32f);
        rects[0].Y.Should().Be(48f);
    }

    [Fact]
    public void GenerateCollisionRects_WithNegativeOffset_RectIsShiftedNegatively()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        var ts = MakeTileset(firstGid: 1, makeSolid: true);
        map.AddTileset(ts);

        var layer = MakeCollisionLayer("col", 4, 4, offsetX: -8f, offsetY: -4f);
        layer.SetTile(1, 1, new Tile(1));
        map.AddLayer(layer);

        var rects = map.GenerateCollisionRects("col");

        rects.Should().HaveCount(1);
        rects[0].X.Should().Be(1 * 16 + (-8f));
        rects[0].Y.Should().Be(1 * 16 + (-4f));
    }

    [Fact]
    public void GenerateCollisionRects_TileNotSolid_ReturnsEmpty()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        var ts = MakeTileset(firstGid: 1, makeSolid: false);
        map.AddTileset(ts);

        var layer = MakeCollisionLayer("col", 4, 4);
        layer.SetTile(0, 0, new Tile(1));
        map.AddLayer(layer);

        map.GenerateCollisionRects("col").Should().BeEmpty();
    }

    [Fact]
    public void GenerateCollisionRects_LayerHasCollisionFalse_ReturnsEmpty()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        var ts = MakeTileset(firstGid: 1, makeSolid: true);
        map.AddTileset(ts);

        var layer = new TilemapLayer("col", 4, 4) { HasCollision = false };
        layer.SetTile(0, 0, new Tile(1));
        map.AddLayer(layer);

        map.GenerateCollisionRects("col").Should().BeEmpty();
    }

    [Fact]
    public void MergeCollisionRects_ThreeAdjacentTilesInSameRow_ProducesOneWideRect()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 5, 1);
        map.AddTileset(MakeTileset(firstGid: 1, makeSolid: true));
        var layer = MakeCollisionLayer("col", 5, 1);
        layer.SetTile(1, 0, new Tile(1));
        layer.SetTile(2, 0, new Tile(1));
        layer.SetTile(3, 0, new Tile(1));
        map.AddLayer(layer);

        var rects = map.MergeCollisionRects("col");

        rects.Should().HaveCount(1);
        rects[0].X.Should().Be(1 * 16);
        rects[0].Width.Should().Be(3 * 16);
        rects[0].Height.Should().Be(16);
    }

    [Fact]
    public void MergeCollisionRects_TwoSeparateRunsInSameRow_ProducesTwoRects()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 5, 1);
        map.AddTileset(MakeTileset(firstGid: 1, makeSolid: true));
        var layer = MakeCollisionLayer("col", 5, 1);
        layer.SetTile(0, 0, new Tile(1));
        layer.SetTile(1, 0, new Tile(1));
        layer.SetTile(3, 0, new Tile(1));
        layer.SetTile(4, 0, new Tile(1));
        map.AddLayer(layer);

        var rects = map.MergeCollisionRects("col");

        rects.Should().HaveCount(2);
        rects[0].X.Should().Be(0);
        rects[0].Width.Should().Be(2 * 16);
        rects[1].X.Should().Be(3 * 16);
        rects[1].Width.Should().Be(2 * 16);
    }

    [Fact]
    public void MergeCollisionRects_TilesInSeparateRows_SameColumnRange_ProducesOneRect()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 3, 2);
        map.AddTileset(MakeTileset(firstGid: 1, makeSolid: true));
        var layer = MakeCollisionLayer("col", 3, 2);
        layer.SetTile(0, 0, new Tile(1));
        layer.SetTile(1, 0, new Tile(1));
        layer.SetTile(0, 1, new Tile(1));
        layer.SetTile(1, 1, new Tile(1));
        map.AddLayer(layer);

        var rects = map.MergeCollisionRects("col");

        rects.Should().HaveCount(1);
        rects[0].X.Should().Be(0);
        rects[0].Y.Should().Be(0);
        rects[0].Width.Should().Be(2 * 16);
        rects[0].Height.Should().Be(2 * 16);
    }

    [Fact]
    public void MergeCollisionRects_SingleSolidTile_ProducesOneTileRect()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.AddTileset(MakeTileset(firstGid: 1, makeSolid: true));
        var layer = MakeCollisionLayer("col", 4, 4);
        layer.SetTile(2, 1, new Tile(1));
        map.AddLayer(layer);

        var rects = map.MergeCollisionRects("col");

        rects.Should().HaveCount(1);
        rects[0].X.Should().Be(2 * 16);
        rects[0].Y.Should().Be(1 * 16);
        rects[0].Width.Should().Be(16);
        rects[0].Height.Should().Be(16);
    }

    [Fact]
    public void MergeCollisionRects_NoSolidTiles_ReturnsEmpty()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.AddTileset(MakeTileset(firstGid: 1, makeSolid: false));
        var layer = MakeCollisionLayer("col", 4, 4);
        layer.SetTile(0, 0, new Tile(1));
        map.AddLayer(layer);

        map.MergeCollisionRects("col").Should().BeEmpty();
    }

    [Fact]
    public void MergeCollisionRects_LayerHasCollisionFalse_ReturnsEmpty()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.AddTileset(MakeTileset(firstGid: 1, makeSolid: true));
        var layer = new TilemapLayer("col", 4, 4) { HasCollision = false };
        layer.SetTile(0, 0, new Tile(1));
        map.AddLayer(layer);

        map.MergeCollisionRects("col").Should().BeEmpty();
    }

    [Fact]
    public void MergeCollisionRects_WithLayerOffset_RectsAreShifted()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 3, 1);
        map.AddTileset(MakeTileset(firstGid: 1, makeSolid: true));
        var layer = MakeCollisionLayer("col", 3, 1, offsetX: 8f, offsetY: 4f);
        layer.SetTile(0, 0, new Tile(1));
        layer.SetTile(1, 0, new Tile(1));
        map.AddLayer(layer);

        var rects = map.MergeCollisionRects("col");

        rects.Should().HaveCount(1);
        rects[0].X.Should().Be(8f);
        rects[0].Y.Should().Be(4f);
        rects[0].Width.Should().Be(2 * 16);
    }

    [Fact]
    public void GetObjectsByType_MatchingType_ReturnsMatchingObjects()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.ObjectLayers["Entities"] =
        [
            new TilemapObject { Name = "enemy1", Type = "Enemy" },
            new TilemapObject { Name = "spawn",  Type = "SpawnPoint" },
            new TilemapObject { Name = "enemy2", Type = "Enemy" },
        ];

        var result = map.GetObjectsByType("Entities", "Enemy");

        result.Should().HaveCount(2);
        result.Select(o => o.Name).Should().BeEquivalentTo(["enemy1", "enemy2"]);
    }

    [Fact]
    public void GetObjectsByType_TypeMatchIsCaseInsensitive()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.ObjectLayers["Entities"] =
        [
            new TilemapObject { Name = "e", Type = "Enemy" },
        ];

        map.GetObjectsByType("Entities", "enemy").Should().HaveCount(1);
        map.GetObjectsByType("Entities", "ENEMY").Should().HaveCount(1);
    }

    [Fact]
    public void GetObjectsByType_NoMatchingType_ReturnsEmpty()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.ObjectLayers["Entities"] =
        [
            new TilemapObject { Name = "item", Type = "Item" },
        ];

        map.GetObjectsByType("Entities", "Enemy").Should().BeEmpty();
    }

    [Fact]
    public void GetObjectsByType_UnknownLayer_ReturnsEmpty()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);

        map.GetObjectsByType("NonExistent", "Enemy").Should().BeEmpty();
    }

    [Fact]
    public void MergeCollisionRects_VerticalWall_FourTilesTall_ProducesOneRect()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 1, 4);
        map.AddTileset(MakeTileset(firstGid: 1, makeSolid: true));
        var layer = MakeCollisionLayer("col", 1, 4);
        layer.SetTile(0, 0, new Tile(1));
        layer.SetTile(0, 1, new Tile(1));
        layer.SetTile(0, 2, new Tile(1));
        layer.SetTile(0, 3, new Tile(1));
        map.AddLayer(layer);

        var rects = map.MergeCollisionRects("col");

        rects.Should().HaveCount(1);
        rects[0].X.Should().Be(0);
        rects[0].Y.Should().Be(0);
        rects[0].Width.Should().Be(16);
        rects[0].Height.Should().Be(4 * 16);
    }

    [Fact]
    public void MergeCollisionRects_DifferentWidthStripsInAdjacentRows_NotMergedVertically()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 3, 2);
        map.AddTileset(MakeTileset(firstGid: 1, makeSolid: true));
        var layer = MakeCollisionLayer("col", 3, 2);
        layer.SetTile(0, 0, new Tile(1));
        layer.SetTile(1, 0, new Tile(1));
        layer.SetTile(0, 1, new Tile(1));
        map.AddLayer(layer);

        var rects = map.MergeCollisionRects("col");

        rects.Should().HaveCount(2);
        rects.Should().Contain(r => r.Width == 2 * 16 && r.Y == 0);
        rects.Should().Contain(r => r.Width == 16 && r.Y == 16);
    }

    [Fact]
    public void MergeCollisionRects_NonAdjacentRows_SameWidth_NotMergedVertically()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 1, 3);
        map.AddTileset(MakeTileset(firstGid: 1, makeSolid: true));
        var layer = MakeCollisionLayer("col", 1, 3);
        layer.SetTile(0, 0, new Tile(1));
        layer.SetTile(0, 2, new Tile(1));
        map.AddLayer(layer);

        var rects = map.MergeCollisionRects("col");

        rects.Should().HaveCount(2);
        rects[0].Y.Should().Be(0);
        rects[1].Y.Should().Be(2 * 16);
    }

    [Fact]
    public void GetObjectByName_ObjectExistsInOneLayer_ReturnsIt()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.ObjectLayers["Entities"] =
        [
            new TilemapObject { Name = "PlayerSpawn", Type = "SpawnPoint" },
        ];

        var result = map.GetObjectByName("PlayerSpawn");

        result.Should().NotBeNull();
        result!.Name.Should().Be("PlayerSpawn");
    }

    [Fact]
    public void GetObjectByName_ObjectExistsInSecondLayer_ReturnsIt()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.ObjectLayers["Decorations"] =
        [
            new TilemapObject { Name = "Tree1" },
        ];
        map.ObjectLayers["Entities"] =
        [
            new TilemapObject { Name = "PlayerSpawn" },
        ];

        var result = map.GetObjectByName("PlayerSpawn");

        result.Should().NotBeNull();
        result!.Name.Should().Be("PlayerSpawn");
    }

    [Fact]
    public void GetObjectByName_NoMatch_ReturnsNull()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.ObjectLayers["Entities"] =
        [
            new TilemapObject { Name = "Enemy1" },
        ];

        map.GetObjectByName("PlayerSpawn").Should().BeNull();
    }

    [Fact]
    public void GetObjectByName_EmptyMap_ReturnsNull()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);

        map.GetObjectByName("anything").Should().BeNull();
    }

    [Fact]
    public void GetObjectByName_NameMatchIsCaseSensitive()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.ObjectLayers["Entities"] =
        [
            new TilemapObject { Name = "PlayerSpawn" },
        ];

        map.GetObjectByName("playerspawn").Should().BeNull();
        map.GetObjectByName("PlayerSpawn").Should().NotBeNull();
    }

    [Fact]
    public void GenerateOneWayPlatformRects_OneWayTile_ProducesRect()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        var ts = new Tileset { FirstGid = 1, TileWidth = 16, TileHeight = 16, Columns = 4, Rows = 4 };
        ts.TileProperties[1] = new TileProperties(1) { IsOneWayPlatform = true };
        map.AddTileset(ts);

        var layer = MakeCollisionLayer("col", 4, 4);
        layer.SetTile(2, 1, new Tile(1));
        map.AddLayer(layer);

        var rects = map.GenerateOneWayPlatformRects("col");

        rects.Should().HaveCount(1);
        rects[0].X.Should().Be(2 * 16);
        rects[0].Y.Should().Be(1 * 16);
        rects[0].Width.Should().Be(16);
        rects[0].Height.Should().Be(16);
    }

    [Fact]
    public void GenerateOneWayPlatformRects_SolidTile_IsNotIncluded()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        var ts = new Tileset { FirstGid = 1, TileWidth = 16, TileHeight = 16, Columns = 4, Rows = 4 };
        ts.TileProperties[1] = new TileProperties(1) { IsSolid = true };
        map.AddTileset(ts);

        var layer = MakeCollisionLayer("col", 4, 4);
        layer.SetTile(0, 0, new Tile(1));
        map.AddLayer(layer);

        map.GenerateOneWayPlatformRects("col").Should().BeEmpty();
    }

    [Fact]
    public void GenerateOneWayPlatformRects_LayerHasCollisionFalse_ReturnsEmpty()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        var ts = new Tileset { FirstGid = 1, TileWidth = 16, TileHeight = 16, Columns = 4, Rows = 4 };
        ts.TileProperties[1] = new TileProperties(1) { IsOneWayPlatform = true };
        map.AddTileset(ts);

        var layer = new TilemapLayer("col", 4, 4) { HasCollision = false };
        layer.SetTile(0, 0, new Tile(1));
        map.AddLayer(layer);

        map.GenerateOneWayPlatformRects("col").Should().BeEmpty();
    }

    [Fact]
    public void GenerateOneWayPlatformRects_WithOffset_RectIsShifted()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        var ts = new Tileset { FirstGid = 1, TileWidth = 16, TileHeight = 16, Columns = 4, Rows = 4 };
        ts.TileProperties[1] = new TileProperties(1) { IsOneWayPlatform = true };
        map.AddTileset(ts);

        var layer = MakeCollisionLayer("col", 4, 4, offsetX: 8f, offsetY: 4f);
        layer.SetTile(0, 0, new Tile(1));
        map.AddLayer(layer);

        var rects = map.GenerateOneWayPlatformRects("col");

        rects.Should().HaveCount(1);
        rects[0].X.Should().Be(8f);
        rects[0].Y.Should().Be(4f);
    }

    [Fact]
    public void MergeOneWayPlatformRects_ThreeAdjacentTiles_ProducesOneWideRect()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 5, 1);
        var ts = new Tileset { FirstGid = 1, TileWidth = 16, TileHeight = 16, Columns = 5, Rows = 1 };
        ts.TileProperties[1] = new TileProperties(1) { IsOneWayPlatform = true };
        map.AddTileset(ts);

        var layer = MakeCollisionLayer("col", 5, 1);
        layer.SetTile(1, 0, new Tile(1));
        layer.SetTile(2, 0, new Tile(1));
        layer.SetTile(3, 0, new Tile(1));
        map.AddLayer(layer);

        var rects = map.MergeOneWayPlatformRects("col");

        rects.Should().HaveCount(1);
        rects[0].X.Should().Be(1 * 16);
        rects[0].Width.Should().Be(3 * 16);
        rects[0].Height.Should().Be(16);
    }

    [Fact]
    public void MergeOneWayPlatformRects_LayerHasCollisionFalse_ReturnsEmpty()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        var ts = new Tileset { FirstGid = 1, TileWidth = 16, TileHeight = 16, Columns = 4, Rows = 4 };
        ts.TileProperties[1] = new TileProperties(1) { IsOneWayPlatform = true };
        map.AddTileset(ts);

        var layer = new TilemapLayer("col", 4, 4) { HasCollision = false };
        layer.SetTile(0, 0, new Tile(1));
        map.AddLayer(layer);

        map.MergeOneWayPlatformRects("col").Should().BeEmpty();
    }

    [Fact]
    public void ResolveTilesetByName_ExistingName_ReturnsTileset()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        var ts = new Tileset { FirstGid = 1, Name = "Terrain" };
        map.AddTileset(ts);

        map.ResolveTilesetByName("Terrain").Should().BeSameAs(ts);
    }

    [Fact]
    public void ResolveTilesetByName_UnknownName_ReturnsNull()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.AddTileset(new Tileset { FirstGid = 1, Name = "Terrain" });

        map.ResolveTilesetByName("Other").Should().BeNull();
    }

    [Fact]
    public void ResolveTilesetByName_MatchIsCaseSensitive()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.AddTileset(new Tileset { FirstGid = 1, Name = "Terrain" });

        map.ResolveTilesetByName("terrain").Should().BeNull();
    }

    [Fact]
    public void ResolveTilesetByName_MultipleTilesets_ReturnsCorrectOne()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        var ts1 = new Tileset { FirstGid = 1, Name = "Terrain" };
        var ts2 = new Tileset { FirstGid = 17, Name = "Entities" };
        map.AddTileset(ts1);
        map.AddTileset(ts2);

        map.ResolveTilesetByName("Entities").Should().BeSameAs(ts2);
        map.ResolveTilesetByName("Terrain").Should().BeSameAs(ts1);
    }

    [Fact]
    public void GetObjectById_ExistingId_ReturnsObject()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.ObjectLayers["Layer1"] =
        [
            new TilemapObject { Id = 42, Name = "spawn" },
            new TilemapObject { Id = 7, Name = "door" },
        ];

        map.GetObjectById(42)!.Name.Should().Be("spawn");
        map.GetObjectById(7)!.Name.Should().Be("door");
    }

    [Fact]
    public void GetObjectById_MissingId_ReturnsNull()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.ObjectLayers["Layer1"] = [new TilemapObject { Id = 1, Name = "a" }];

        map.GetObjectById(99).Should().BeNull();
    }

    [Fact]
    public void GetObjectById_NoObjectLayers_ReturnsNull()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);

        map.GetObjectById(1).Should().BeNull();
    }

    [Fact]
    public void GetObjectById_ObjectInSecondLayer_ReturnsCorrectObject()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.ObjectLayers["Layer1"] = [new TilemapObject { Id = 1, Name = "a" }];
        map.ObjectLayers["Layer2"] = [new TilemapObject { Id = 2, Name = "b" }];

        map.GetObjectById(2)!.Name.Should().Be("b");
    }

    [Fact]
    public void GetObjectsByType_AllLayers_ReturnsMatchingObjectsFromAllLayers()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.ObjectLayers["Layer1"] =
        [
            new TilemapObject { Id = 1, Name = "enemy1", Type = "Enemy" },
            new TilemapObject { Id = 2, Name = "spawn", Type = "SpawnPoint" },
        ];
        map.ObjectLayers["Layer2"] =
        [
            new TilemapObject { Id = 3, Name = "enemy2", Type = "Enemy" },
        ];

        var enemies = map.GetObjectsByType("Enemy");

        enemies.Should().HaveCount(2);
        enemies.Select(o => o.Name).Should().BeEquivalentTo(["enemy1", "enemy2"]);
    }

    [Fact]
    public void GetObjectsByType_AllLayers_TypeMatchIsCaseInsensitive()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.ObjectLayers["Layer1"] = [new TilemapObject { Id = 1, Name = "e", Type = "enemy" }];

        map.GetObjectsByType("ENEMY").Should().HaveCount(1);
        map.GetObjectsByType("Enemy").Should().HaveCount(1);
    }

    [Fact]
    public void GetObjectsByType_AllLayers_NoMatch_ReturnsEmptyList()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.ObjectLayers["Layer1"] = [new TilemapObject { Id = 1, Name = "a", Type = "Trigger" }];

        map.GetObjectsByType("Enemy").Should().BeEmpty();
    }

    [Fact]
    public void GetObjectsByType_AllLayers_NoObjectLayers_ReturnsEmptyList()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);

        map.GetObjectsByType("Enemy").Should().BeEmpty();
    }

    [Fact]
    public void GetAllLayers_NoMatch_ReturnsEmptyList()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.AddLayer(new TilemapLayer("Ground", 4, 4));

        map.GetAllLayers("Other").Should().BeEmpty();
    }

    [Fact]
    public void GetAllLayers_SingleMatch_ReturnsSingleLayer()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        map.AddLayer(new TilemapLayer("Ground", 4, 4));
        map.AddLayer(new TilemapLayer("Foreground", 4, 4) { ZOrder = 1 });

        var result = map.GetAllLayers("Ground");

        result.Should().HaveCount(1);
        result[0].Name.Should().Be("Ground");
    }

    [Fact]
    public void GetAllLayers_DuplicateNames_ReturnsAllMatches()
    {
        var map = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        var layer1 = new TilemapLayer("Decor", 4, 4) { ZOrder = 0 };
        var layer2 = new TilemapLayer("Decor", 4, 4) { ZOrder = 1 };
        map.AddLayer(layer1);
        map.AddLayer(layer2);

        var result = map.GetAllLayers("Decor");

        result.Should().HaveCount(2);
    }
}
