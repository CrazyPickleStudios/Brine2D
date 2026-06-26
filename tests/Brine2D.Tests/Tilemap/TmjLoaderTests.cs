using System.Text.Json;
using Brine2D.Tilemap;
using FluentAssertions;
using Xunit;

namespace Brine2D.Tests.Tilemap;

public sealed class TmjLoaderTests : IDisposable
{
    private readonly string _tempDir;

    public TmjLoaderTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"Brine2D_TmjLoader_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    private string WriteFile(string relativePath, string contents)
    {
        var fullPath = Path.Combine(_tempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
        File.WriteAllText(fullPath, contents);
        return fullPath;
    }

    #region Error Handling

    [Fact]
    public async Task LoadAsync_MapFileNotFound_ThrowsFileNotFoundException()
    {
        var loader = new TmjLoader();
        var act = () => loader.LoadAsync(Path.Combine(_tempDir, "missing.tmj"));

        await act.Should().ThrowAsync<FileNotFoundException>();
    }

    [Fact]
    public async Task LoadAsync_ExternalTsj_MissingFile_ThrowsFileNotFoundException()
    {
        var mapPath = WriteFile("map.tmj", ExternalTilesetMapJson("tileset.tsj"));
        var loader = new TmjLoader();
        var act = () => loader.LoadAsync(mapPath);

        var ex = (await act.Should().ThrowAsync<FileNotFoundException>()).Which;
        ex.FileName.Should().EndWith("tileset.tsj");
    }

    #endregion

    #region Infinite Map Guard

    [Fact]
    public async Task LoadAsync_InfiniteMapLayer_ThrowsNotSupportedWithHelpfulMessage()
    {
        var mapPath = WriteFile("map.tmj", MapWithInfiniteLayerJson("Ground"));
        var act = () => new TmjLoader().LoadAsync(mapPath);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*infinite*")
            .WithMessage("*Infinite*");
    }

    [Fact]
    public async Task LoadAsync_InfiniteMapLayer_ErrorMessageContainsLayerName()
    {
        var mapPath = WriteFile("map.tmj", MapWithChunkLayerNoInfiniteFlagJson("MyLayer"));
        var act = () => new TmjLoader().LoadAsync(mapPath);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*MyLayer*");
    }

    [Fact]
    public async Task LoadAsync_MapLevelInfiniteFlag_ThrowsNotSupportedEvenWithNoChunkData()
    {
        var mapPath = WriteFile("map.tmj", MapWithInfiniteMapFlagNoChunksJson());
        var act = () => new TmjLoader().LoadAsync(mapPath);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*infinite*");
    }

    #endregion

    #region Unsupported Layer Types

    [Fact]
    public async Task LoadAsync_ImageLayer_IsSkippedWithoutThrowing()
    {
        var mapPath = WriteFile("map.tmj", MapWithImageLayerJson("Background"));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ImageLayer_DoesNotAddToObjectLayers()
    {
        var mapPath = WriteFile("map.tmj", MapWithImageLayerJson("Background"));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_UnknownLayerType_LogsWarningWithLayerNameAndType()
    {
        var logger = new CapturingLogger<TmjLoader>();
        var mapPath = WriteFile("map.tmj", MapWithUnknownLayerTypeJson("BadLayer", "customlayer"));
        await new TmjLoader(logger).LoadAsync(mapPath);

        logger.Warnings.Should().ContainMatch("*BadLayer*");
        logger.Warnings.Should().ContainMatch("*customlayer*");
    }

    [Fact]
    public async Task LoadAsync_ImageLayerAlongsideTileLayer_TileLayerIsStillLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithImageLayerAndTileLayerJson());
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers.Should().HaveCount(1);
        tilemap.Layers[0].Name.Should().Be("Ground");
    }

    #endregion

    #region Embedded Tileset (regression)

    [Fact]
    public async Task LoadAsync_EmbeddedTileset_LoadsTilesetDimensions()
    {
        var mapPath = WriteFile("map.tmj", EmbeddedTilesetMapJson(
            """{"firstgid":1,"image":"tiles.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":8}"""));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Tileset.Should().NotBeNull();
        tilemap.Tileset!.TileWidth.Should().Be(16);
        tilemap.Tileset.TileHeight.Should().Be(16);
        tilemap.Tileset.Columns.Should().Be(4);
        tilemap.Tileset.Rows.Should().Be(2);
        tilemap.Tileset.FirstGid.Should().Be(1);
    }

    [Fact]
    public async Task LoadAsync_EmbeddedTileset_ResolvesImagePathRelativeToMap()
    {
        var mapPath = WriteFile("map.tmj", EmbeddedTilesetMapJson(
            """{"firstgid":1,"image":"tiles.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":8}"""));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        var expected = Path.GetFullPath(Path.Combine(_tempDir, "tiles.png"));
        tilemap.Tileset!.ImagePath.Should().Be(expected);
    }

    #endregion

    #region External .tsj Loading

    [Fact]
    public async Task LoadAsync_ExternalTsj_LoadsTilesetDimensions()
    {
        WriteFile("tileset.tsj",
            """{"columns":4,"image":"tiles.png","tilewidth":16,"tileheight":16,"tilecount":8}""");
        var mapPath = WriteFile("map.tmj", ExternalTilesetMapJson("tileset.tsj"));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Tileset.Should().NotBeNull();
        tilemap.Tileset!.TileWidth.Should().Be(16);
        tilemap.Tileset.TileHeight.Should().Be(16);
        tilemap.Tileset.Columns.Should().Be(4);
        tilemap.Tileset.Rows.Should().Be(2);
    }

    [Fact]
    public async Task LoadAsync_ExternalTsj_UsesFirstGidFromMapReference()
    {
        WriteFile("tileset.tsj",
            """{"columns":4,"image":"tiles.png","tilewidth":16,"tileheight":16,"tilecount":8}""");
        var mapPath = WriteFile("map.tmj", ExternalTilesetMapJson("tileset.tsj", firstGid: 5));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Tileset!.FirstGid.Should().Be(5);
    }

    [Fact]
    public async Task LoadAsync_ExternalTsj_InSameDirectory_ResolvesImagePathRelativeToTsjFile()
    {
        WriteFile("tileset.tsj",
            """{"columns":4,"image":"tiles.png","tilewidth":16,"tileheight":16,"tilecount":8}""");
        var mapPath = WriteFile("map.tmj", ExternalTilesetMapJson("tileset.tsj"));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        var expected = Path.GetFullPath(Path.Combine(_tempDir, "tiles.png"));
        tilemap.Tileset!.ImagePath.Should().Be(expected);
    }

    [Fact]
    public async Task LoadAsync_ExternalTsj_InSubdirectory_ResolvesImagePathRelativeToTsjFile()
    {
        WriteFile("tilesets/tileset.tsj",
            """{"columns":4,"image":"tiles.png","tilewidth":16,"tileheight":16,"tilecount":8}""");
        var mapPath = WriteFile("map.tmj", ExternalTilesetMapJson("tilesets/tileset.tsj"));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        var expected = Path.GetFullPath(Path.Combine(_tempDir, "tilesets", "tiles.png"));
        tilemap.Tileset!.ImagePath.Should().Be(expected);
    }

    [Fact]
    public async Task LoadAsync_ExternalTsj_ImagePathWithDotDot_NormalizesCorrectly()
    {
        WriteFile("tilesets/tileset.tsj",
            """{"columns":4,"image":"../images/tiles.png","tilewidth":16,"tileheight":16,"tilecount":8}""");
        var mapPath = WriteFile("map.tmj", ExternalTilesetMapJson("tilesets/tileset.tsj"));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        var expected = Path.GetFullPath(Path.Combine(_tempDir, "images", "tiles.png"));
        tilemap.Tileset!.ImagePath.Should().Be(expected);
    }

    [Fact]
    public async Task LoadAsync_ExternalTsj_WithStringTileProperty_LoadsCustomPropertyFromTsj()
    {
        WriteFile("tileset.tsj", """
            {
                "columns": 2, "image": "tiles.png", "tilewidth": 16, "tileheight": 16, "tilecount": 2,
                "tiles": [{ "id": 0, "properties": [{ "name": "surface", "type": "string", "value": "grass" }] }]
            }
            """);
        var mapPath = WriteFile("map.tmj", ExternalTilesetMapJson("tileset.tsj"));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Tileset!.TileProperties.Should().ContainKey(1);
        tilemap.Tileset.TileProperties[1].CustomProperties.Should().ContainKey("surface")
            .WhoseValue.Should().Be("grass");
    }

    [Fact]
    public async Task LoadAsync_EmbeddedTileset_WithName_LoadsTilesetName()
    {
        var mapPath = WriteFile("map.tmj", EmbeddedTilesetMapJson(
            """{"firstgid":1,"name":"Terrain","image":"tiles.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":8}"""));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Tileset!.Name.Should().Be("Terrain");
    }

    [Fact]
    public async Task LoadAsync_EmbeddedTileset_WithoutName_TilesetNameIsEmpty()
    {
        var mapPath = WriteFile("map.tmj", EmbeddedTilesetMapJson(
            """{"firstgid":1,"image":"tiles.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":8}"""));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Tileset!.Name.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_EmbeddedTileset_WithCustomProperties_LoadsTilesetCustomProperties()
    {
        var mapPath = WriteFile("map.tmj", EmbeddedTilesetMapJson(
            """{"firstgid":1,"image":"tiles.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":8,"properties":[{"name":"physics_material","type":"string","value":"stone"}]}"""));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Tileset!.CustomProperties.Should().ContainKey("physics_material")
            .WhoseValue.Should().Be("stone");
    }

    [Fact]
    public async Task LoadAsync_EmbeddedTileset_MultipleCustomProperties_AllLoaded()
    {
        var mapPath = WriteFile("map.tmj", EmbeddedTilesetMapJson(
            """{"firstgid":1,"image":"tiles.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":8,"properties":[{"name":"physics_material","type":"string","value":"stone"},{"name":"sound_group","type":"string","value":"hard_surface"}]}"""));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Tileset!.CustomProperties.Should().ContainKey("physics_material");
        tilemap.Tileset.CustomProperties.Should().ContainKey("sound_group")
            .WhoseValue.Should().Be("hard_surface");
    }

    [Fact]
    public async Task LoadAsync_ExternalTsj_WithName_LoadsTilesetName()
    {
        WriteFile("tileset.tsj",
            """{"name":"Entities","columns":4,"image":"tiles.png","tilewidth":16,"tileheight":16,"tilecount":8}""");
        var mapPath = WriteFile("map.tmj", ExternalTilesetMapJson("tileset.tsj"));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Tileset!.Name.Should().Be("Entities");
    }

    [Fact]
    public async Task LoadAsync_ExternalTsj_WithTilesetCustomProperty_LoadsIt()
    {
        WriteFile("tileset.tsj", """
            {
                "name":"Props","columns":2,"image":"tiles.png","tilewidth":16,"tileheight":16,"tilecount":2,
                "properties":[{"name":"layer_mask","type":"string","value":"foreground"}]
            }
            """);
        var mapPath = WriteFile("map.tmj", ExternalTilesetMapJson("tileset.tsj"));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Tileset!.CustomProperties.Should().ContainKey("layer_mask")
            .WhoseValue.Should().Be("foreground");
    }

    [Fact]
    public async Task ResolveTilesetByName_AfterLoad_ReturnsMatchingTileset()
    {
        var mapPath = WriteFile("map.tmj", EmbeddedTilesetMapJson(
            """{"firstgid":1,"name":"Terrain","image":"tiles.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":8}"""));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.ResolveTilesetByName("Terrain").Should().NotBeNull();
        tilemap.ResolveTilesetByName("Other").Should().BeNull();
    }

    #endregion

    #region Multi-Tileset (Bug #1)

    [Fact]
    public async Task LoadAsync_MultiTileset_LoadsAllTilesets()
    {
        var mapPath = WriteFile("map.tmj", MultiTilesetMapJson());
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Tilesets.Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadAsync_MultiTileset_TilesetsHaveCorrectFirstGids()
    {
        var mapPath = WriteFile("map.tmj", MultiTilesetMapJson());
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Tilesets[0].FirstGid.Should().Be(1);
        tilemap.Tilesets[1].FirstGid.Should().Be(9);
    }

    [Fact]
    public async Task ResolveTileset_ReturnsFirstTileset_ForGidInFirstRange()
    {
        var mapPath = WriteFile("map.tmj", MultiTilesetMapJson());
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.ResolveTileset(1)!.FirstGid.Should().Be(1);
        tilemap.ResolveTileset(8)!.FirstGid.Should().Be(1);
    }

    [Fact]
    public async Task ResolveTileset_ReturnsSecondTileset_ForGidInSecondRange()
    {
        var mapPath = WriteFile("map.tmj", MultiTilesetMapJson());
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.ResolveTileset(9)!.FirstGid.Should().Be(9);
        tilemap.ResolveTileset(16)!.FirstGid.Should().Be(9);
    }

    [Fact]
    public void ResolveTileset_ReturnsNull_ForGidZero()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        tilemap.AddTileset(new Tileset { FirstGid = 1 });

        tilemap.ResolveTileset(0).Should().BeNull();
    }

    #endregion

    #region Layer Opacity (Bug #2)

    [Fact]
    public async Task LoadAsync_LayerOpacity_IsLoadedFromJson()
    {
        var mapPath = WriteFile("map.tmj", MapWithLayerOpacityJson(0.5f));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Layers[0].Opacity.Should().BeApproximately(0.5f, 0.001f);
    }

    [Fact]
    public async Task LoadAsync_LayerOpacity_DefaultsToOne_WhenNotSpecified()
    {
        var mapPath = WriteFile("map.tmj", EmbeddedTilesetMapJson(
            """{"firstgid":1,"image":"tiles.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":8}"""));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Layers[0].Opacity.Should().Be(1.0f);
    }

    #endregion

    #region Diagonal Flip (Bug #3)

    [Fact]
    public async Task LoadAsync_DiagonalFlipBit_IsStoredOnTile()
    {
        // GID 1 with FlippedDiagonallyFlag (0x20000000) set
        const int gidWithDiagonal = 1 | unchecked((int)0x20000000);
        var mapPath = WriteFile("map.tmj", MapWithSingleTileGidJson(gidWithDiagonal));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        var tile = tilemap.Layers[0].GetTile(0, 0);
        tile.FlipDiagonal.Should().BeTrue();
        tile.Id.Should().Be(1);
    }

    [Fact]
    public async Task LoadAsync_HorizontalAndDiagonalFlip_BothStored()
    {
        const int gid = 1 | unchecked((int)0x80000000) | unchecked((int)0x20000000);
        var mapPath = WriteFile("map.tmj", MapWithSingleTileGidJson(gid));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        var tile = tilemap.Layers[0].GetTile(0, 0);
        tile.FlipHorizontal.Should().BeTrue();
        tile.FlipDiagonal.Should().BeTrue();
        tile.FlipVertical.Should().BeFalse();
    }

    #endregion

    #region JsonElement Property Parsing (Bug #6)

    [Fact]
    public async Task LoadAsync_BoolTileProperty_ParsesWithoutThrowing()
    {
        WriteFile("tileset.tsj", """
            {
                "columns": 2, "image": "tiles.png", "tilewidth": 16, "tileheight": 16, "tilecount": 2,
                "tiles": [{ "id": 0, "properties": [{ "name": "solid", "type": "bool", "value": true }] }]
            }
            """);
        var mapPath = WriteFile("map.tmj", ExternalTilesetMapJson("tileset.tsj"));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Tileset!.TileProperties[1].IsSolid.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_FalseBoolTileProperty_ParsedCorrectly()
    {
        WriteFile("tileset.tsj", """
            {
                "columns": 2, "image": "tiles.png", "tilewidth": 16, "tileheight": 16, "tilecount": 2,
                "tiles": [{ "id": 0, "properties": [{ "name": "solid", "type": "bool", "value": false }] }]
            }
            """);
        var mapPath = WriteFile("map.tmj", ExternalTilesetMapJson("tileset.tsj"));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Tileset!.TileProperties[1].IsSolid.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_LayerCollisionBoolProperty_ParsedFromJsonElement()
    {
        var mapPath = WriteFile("map.tmj", MapWithLayerBoolPropertyJson("collision", true));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Layers[0].HasCollision.Should().BeTrue();
    }

    #endregion

    #region Layer Offset (Bug #7)

    [Fact]
    public async Task LoadAsync_LayerOffset_IsLoadedFromJson()
    {
        var mapPath = WriteFile("map.tmj", MapWithLayerOffsetJson(32f, 64f));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Layers[0].OffsetX.Should().BeApproximately(32f, 0.001f);
        tilemap.Layers[0].OffsetY.Should().BeApproximately(64f, 0.001f);
    }

    [Fact]
    public async Task LoadAsync_LayerOffset_DefaultsToZero_WhenNotSpecified()
    {
        var mapPath = WriteFile("map.tmj", EmbeddedTilesetMapJson(
            """{"firstgid":1,"image":"tiles.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":8}"""));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Layers[0].OffsetX.Should().Be(0f);
        tilemap.Layers[0].OffsetY.Should().Be(0f);
    }

    #endregion

    #region Tileset Spacing and Margin

    [Fact]
    public async Task LoadAsync_TilesetSpacing_IsLoadedFromJson()
    {
        var mapPath = WriteFile("map.tmj", EmbeddedTilesetWithSpacingMarginJson(spacing: 2, margin: 0));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Tileset!.Spacing.Should().Be(2);
    }

    [Fact]
    public async Task LoadAsync_TilesetMargin_IsLoadedFromJson()
    {
        var mapPath = WriteFile("map.tmj", EmbeddedTilesetWithSpacingMarginJson(spacing: 0, margin: 4));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Tileset!.Margin.Should().Be(4);
    }

    [Fact]
    public async Task LoadAsync_TilesetSpacingAndMargin_DefaultToZero_WhenNotSpecified()
    {
        var mapPath = WriteFile("map.tmj", EmbeddedTilesetMapJson(
            """{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}"""));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Tileset!.Spacing.Should().Be(0);
        tilemap.Tileset.Margin.Should().Be(0);
    }

    [Fact]
    public async Task LoadAsync_ExternalTsj_SpacingAndMargin_AreLoadedFromTsj()
    {
        WriteFile("tileset.tsj",
            """{"columns":4,"image":"tiles.png","tilewidth":16,"tileheight":16,"tilecount":16,"spacing":2,"margin":4}""");
        var mapPath = WriteFile("map.tmj", ExternalTilesetMapJson("tileset.tsj"));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Tileset!.Spacing.Should().Be(2);
        tilemap.Tileset.Margin.Should().Be(4);
    }

    #endregion

    #region Object Layers

    [Fact]
    public async Task LoadAsync_ObjectLayer_IsAddedToObjectLayers()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Entities",
            """{"id":1,"name":"player","type":"Player","x":32.0,"y":64.0,"width":16.0,"height":16.0,"visible":true}"""));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.ObjectLayers.Should().ContainKey("Entities");
    }

    [Fact]
    public async Task LoadAsync_ObjectLayer_DoesNotAddToTileLayers()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Entities",
            """{"id":1,"name":"player","type":"Player","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true}"""));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.Layers.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ObjectLayer_LoadsObjectNameAndType()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Entities",
            """{"id":1,"name":"spawn1","type":"SpawnPoint","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true}"""));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        var obj = tilemap.ObjectLayers["Entities"][0];
        obj.Name.Should().Be("spawn1");
        obj.Type.Should().Be("SpawnPoint");
    }

    [Fact]
    public async Task LoadAsync_ObjectLayer_Tiled19ClassField_IsReadAsType()
    {
        // Tiled 1.9+ exports "class" instead of "type" for the user-defined object class.
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Entities",
            """{"id":1,"name":"enemy","class":"Goblin","x":0.0,"y":0.0,"width":16.0,"height":16.0,"visible":true}"""));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.ObjectLayers["Entities"][0].Type.Should().Be("Goblin");
    }

    [Fact]
    public async Task LoadAsync_ObjectLayer_Tiled19BothFields_TypeTakesPrecedence()
    {
        // When both "type" and "class" are present, the "type" value wins.
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Entities",
            """{"id":1,"name":"x","type":"Legacy","class":"New","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true}"""));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.ObjectLayers["Entities"][0].Type.Should().Be("Legacy");
    }

    [Fact]
    public async Task LoadAsync_ObjectLayer_Tiled19ClassField_GetObjectsByType_Works()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Entities",
            """{"id":1,"name":"boss","class":"Dragon","x":0.0,"y":0.0,"width":32.0,"height":32.0,"visible":true}"""));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.GetObjectsByType("Dragon").Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadAsync_ObjectLayer_LoadsObjectPositionAndSize()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Entities",
            """{"id":1,"name":"box","type":"","x":32.0,"y":64.0,"width":16.0,"height":32.0,"visible":true}"""));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        var obj = tilemap.ObjectLayers["Entities"][0];
        obj.X.Should().BeApproximately(32f, 0.001f);
        obj.Y.Should().BeApproximately(64f, 0.001f);
        obj.Width.Should().BeApproximately(16f, 0.001f);
        obj.Height.Should().BeApproximately(32f, 0.001f);
    }

    [Fact]
    public async Task LoadAsync_ObjectLayer_LoadsObjectId()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Entities",
            """{"id":42,"name":"thing","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true}"""));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.ObjectLayers["Entities"][0].Id.Should().Be(42);
    }

    [Fact]
    public async Task LoadAsync_ObjectLayer_LoadsObjectCustomProperties()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Entities", """
            {"id":1,"name":"spawn1","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true,
             "properties":[{"name":"team","type":"string","value":"red"}]}
            """));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.ObjectLayers["Entities"][0].CustomProperties
            .Should().ContainKey("team").WhoseValue.Should().Be("red");
    }

    [Fact]
    public async Task LoadAsync_ObjectLayer_MultipleObjects_AllLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Entities", """
            {"id":1,"name":"a","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true},
            {"id":2,"name":"b","type":"","x":16.0,"y":0.0,"width":0.0,"height":0.0,"visible":true}
            """));
        var loader = new TmjLoader();

        var tilemap = await loader.LoadAsync(mapPath);

        tilemap.ObjectLayers["Entities"].Should().HaveCount(2);
    }

    [Fact]
    public void GetObjects_UnknownLayerName_ReturnsEmptyList()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);

        tilemap.GetObjects("NonExistent").Should().BeEmpty();
    }

    [Fact]
    public void GetObject_MatchingName_ReturnsObject()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        tilemap.ObjectLayers["Layer"] = [new TilemapObject { Name = "target", Type = "Trigger" }];

        tilemap.GetObject("Layer", "target")!.Type.Should().Be("Trigger");
    }

    [Fact]
    public void GetObject_UnknownObjectName_ReturnsNull()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);
        tilemap.ObjectLayers["Layer"] = [new TilemapObject { Name = "thing" }];

        tilemap.GetObject("Layer", "missing").Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_ObjectLayer_LayerCustomProperties_AreLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerWithPropertiesJson("Entities",
            """{"name":"zone","type":"string","value":"boss"}""",
            """{"id":1,"name":"guard","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.GetObjectLayerProperties("Entities")
            .Should().ContainKey("zone").WhoseValue.Should().Be("boss");
    }

    [Fact]
    public async Task LoadAsync_ObjectLayer_MultipleLayerCustomProperties_AllLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerWithPropertiesJson("Entities",
            """{"name":"zone","type":"string","value":"boss"},{"name":"music","type":"string","value":"boss.ogg"}""",
            """{"id":1,"name":"guard","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        var props = tilemap.GetObjectLayerProperties("Entities");
        props.Should().ContainKey("zone").WhoseValue.Should().Be("boss");
        props.Should().ContainKey("music").WhoseValue.Should().Be("boss.ogg");
    }

    [Fact]
    public async Task LoadAsync_ObjectLayer_NoLayerProperties_ObjectLayerPropertiesIsAbsent()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Entities",
            """{"id":1,"name":"guard","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayerProperties.Should().NotContainKey("Entities");
    }

    [Fact]
    public void GetObjectLayerProperties_UnknownLayerName_ReturnsEmptyDictionary()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);

        tilemap.GetObjectLayerProperties("NonExistent").Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_ObjectLayer_GetObjectsByType_ReturnsMatchingObjects()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Entities", """
            {"id":1,"name":"enemy1","type":"Enemy","x":0.0,"y":0.0,"width":16.0,"height":16.0,"visible":true},
            {"id":2,"name":"spawn","type":"SpawnPoint","x":16.0,"y":0.0,"width":0.0,"height":0.0,"visible":true},
            {"id":3,"name":"enemy2","type":"Enemy","x":32.0,"y":0.0,"width":16.0,"height":16.0,"visible":true}
            """));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        var enemies = tilemap.GetObjectsByType("Entities", "Enemy");
        enemies.Should().HaveCount(2);
        enemies.Select(o => o.Name).Should().BeEquivalentTo(["enemy1", "enemy2"]);
    }

    [Fact]
    public async Task LoadAsync_TileObject_Y_IsNormalizedToTopLeft()
    {
        // Tiled tile objects place Y at the bottom-left of the tile.
        // The loader must subtract the object height to normalize to top-left.
        // Object: gid=1 (tile), x=32, y=80, width=16, height=16
        // Expected normalized Y = 80 - 16 = 64.
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Tiles",
            """{"id":1,"name":"block","type":"","x":32.0,"y":80.0,"width":16.0,"height":16.0,"gid":1,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        var obj = tilemap.ObjectLayers["Tiles"][0];
        obj.X.Should().BeApproximately(32f, 0.001f);
        obj.Y.Should().BeApproximately(64f, 0.001f);
    }

    [Fact]
    public async Task LoadAsync_TileObject_Y_NormalizationUsesObjectHeight()
    {
        // A 32-tall tile object: normalized Y = 128 - 32 = 96.
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Tiles",
            """{"id":1,"name":"tall","type":"","x":0.0,"y":128.0,"width":32.0,"height":32.0,"gid":1,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Tiles"][0].Y.Should().BeApproximately(96f, 0.001f);
    }

    [Fact]
    public async Task LoadAsync_NonTileObject_Y_IsNotAdjusted()
    {
        // Rectangle objects use top-left origin — Y must not be adjusted.
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Zones",
            """{"id":1,"name":"zone","type":"","x":0.0,"y":64.0,"width":32.0,"height":32.0,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Zones"][0].Y.Should().BeApproximately(64f, 0.001f);
    }

    [Fact]
    public async Task LoadAsync_TileObject_Y_WithLayerOffset_BothAdjustmentsApplied()
    {
        // Layer has offsetY=16. Tile object y=80, height=16.
        // Expected: (80 - 16) + 16 = 80.
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerWithOffsetJson("Tiles", 0f, 16f,
            """{"id":1,"name":"block","type":"","x":0.0,"y":80.0,"width":16.0,"height":16.0,"gid":1,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Tiles"][0].Y.Should().BeApproximately(80f, 0.001f);
    }

    [Fact]
    public async Task LoadAsync_InvisibleObjectLayer_ObjectsAreStillLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Spawners",
            """{"id":1,"name":"enemy","type":"Enemy","x":0.0,"y":0.0,"width":16.0,"height":16.0,"visible":false}""",
            layerVisible: false));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers.Should().ContainKey("Spawners");
        tilemap.ObjectLayers["Spawners"].Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadAsync_InvisibleObjectLayer_ObjectVisibilityFlagIsPreserved()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Spawners",
            """{"id":1,"name":"enemy","type":"Enemy","x":0.0,"y":0.0,"width":16.0,"height":16.0,"visible":false}""",
            layerVisible: false));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Spawners"][0].Visible.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_InvisibleObjectLayer_InsideVisibleGroup_ObjectsAreStillLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerInGroupJson("Debug",
            """{"id":1,"name":"marker","type":"Marker","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true}""",
            layerVisible: false));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers.Should().ContainKey("Debug");
        tilemap.ObjectLayers["Debug"].Should().HaveCount(1);
    }

    #endregion

    #region Tile Data Encoding

    [Fact]
    public async Task LoadAsync_Base64Encoding_LoadsTilesCorrectly()
    {
        var bytes = new byte[16];
        BitConverter.GetBytes(1).CopyTo(bytes, 0);
        BitConverter.GetBytes(2).CopyTo(bytes, 4);
        BitConverter.GetBytes(3).CopyTo(bytes, 8);
        BitConverter.GetBytes(0).CopyTo(bytes, 12);
        var b64 = Convert.ToBase64String(bytes);

        var mapPath = WriteFile("map.tmj", MapWithEncodedTileDataJson($"\"{b64}\"", encoding: "base64"));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].GetTile(0, 0).Id.Should().Be(1);
        tilemap.Layers[0].GetTile(1, 0).Id.Should().Be(2);
        tilemap.Layers[0].GetTile(0, 1).Id.Should().Be(3);
        tilemap.Layers[0].GetTile(1, 1).IsEmpty.Should().BeTrue();
    }

    [Theory]
    [InlineData("lzma")]
    public async Task LoadAsync_UnsupportedCompressedBase64_ThrowsNotSupportedWithHelpfulMessage(string compression)
    {
        var mapPath = WriteFile("map.tmj", MapWithEncodedTileDataJson("\"AAAA\"", encoding: "base64", compression: compression));

        var act = () => new TmjLoader().LoadAsync(mapPath);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage($"*base64+{compression}*");
    }

    [Fact]
    public async Task LoadAsync_Base64ZlibEncoding_LoadsTilesCorrectly()
    {
        var raw = BuildRawTileBytes(1, 2, 3, 0);
        var b64 = CompressBase64Zlib(raw);
        var mapPath = WriteFile("map.tmj", MapWithEncodedTileDataJson($"\"{b64}\"", encoding: "base64", compression: "zlib"));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].GetTile(0, 0).Id.Should().Be(1);
        tilemap.Layers[0].GetTile(1, 0).Id.Should().Be(2);
        tilemap.Layers[0].GetTile(0, 1).Id.Should().Be(3);
        tilemap.Layers[0].GetTile(1, 1).IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_Base64GzipEncoding_LoadsTilesCorrectly()
    {
        var raw = BuildRawTileBytes(1, 2, 3, 0);
        var b64 = CompressBase64Gzip(raw);
        var mapPath = WriteFile("map.tmj", MapWithEncodedTileDataJson($"\"{b64}\"", encoding: "base64", compression: "gzip"));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].GetTile(0, 0).Id.Should().Be(1);
        tilemap.Layers[0].GetTile(1, 0).Id.Should().Be(2);
        tilemap.Layers[0].GetTile(0, 1).Id.Should().Be(3);
        tilemap.Layers[0].GetTile(1, 1).IsEmpty.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_Base64ZstdEncoding_LoadsTilesCorrectly()
    {
        var raw = BuildRawTileBytes(1, 2, 3, 0);
        var b64 = CompressBase64Zstd(raw);
        var mapPath = WriteFile("map.tmj", MapWithEncodedTileDataJson($"\"{b64}\"", encoding: "base64", compression: "zstd"));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].GetTile(0, 0).Id.Should().Be(1);
        tilemap.Layers[0].GetTile(1, 0).Id.Should().Be(2);
        tilemap.Layers[0].GetTile(0, 1).Id.Should().Be(3);
        tilemap.Layers[0].GetTile(1, 1).IsEmpty.Should().BeTrue();
    }

    #endregion

    #region Parallax

    [Fact]
    public async Task LoadAsync_LayerParallax_IsStoredOnLayer()
    {
        var mapPath = WriteFile("map.tmj", MapWithLayerParallaxJson(parallaxX: 0.5f, parallaxY: 0.25f));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].ParallaxX.Should().BeApproximately(0.5f, 0.001f);
        tilemap.Layers[0].ParallaxY.Should().BeApproximately(0.25f, 0.001f);
    }

    [Fact]
    public async Task LoadAsync_LayerParallax_DefaultsToOne_WhenNotSpecified()
    {
        var mapPath = WriteFile("map.tmj", EmbeddedTilesetMapJson(
            """{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].ParallaxX.Should().Be(1.0f);
        tilemap.Layers[0].ParallaxY.Should().Be(1.0f);
    }

    #endregion

    #region Object Rotation

    [Fact]
    public async Task LoadAsync_ObjectLayer_LoadsObjectRotation()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Spawns",
            """{"id":1,"name":"cannon","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"rotation":45.0,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Spawns"][0].Rotation.Should().BeApproximately(45f, 0.001f);
    }

    [Fact]
    public async Task LoadAsync_ObjectLayer_ObjectRotationAbsent_DefaultsToZero()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Spawns",
            """{"id":1,"name":"spawn","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Spawns"][0].Rotation.Should().Be(0f);
    }

    #endregion

    #region Group Layers

    [Fact]
    public async Task LoadAsync_GroupLayer_TileLayerInsideGroup_IsLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithGroupLayerJson(
            """{ "name": "Ground", "type": "tilelayer", "width": 2, "height": 1, "data": [1, 2], "visible": true, "opacity": 1.0 }"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers.Should().HaveCount(1);
        tilemap.Layers[0].Name.Should().Be("Ground");
    }

    [Fact]
    public async Task LoadAsync_GroupLayer_TilesInsideGroup_AreAccessible()
    {
        var mapPath = WriteFile("map.tmj", MapWithGroupLayerJson(
            """{ "name": "Ground", "type": "tilelayer", "width": 2, "height": 1, "data": [1, 2], "visible": true, "opacity": 1.0 }"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].GetTile(0, 0).Id.Should().Be(1);
        tilemap.Layers[0].GetTile(1, 0).Id.Should().Be(2);
    }

    [Fact]
    public async Task LoadAsync_GroupLayer_ObjectGroupInsideGroup_IsLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithGroupObjectLayerJson("Entities",
            """{"id":1,"name":"player","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers.Should().ContainKey("Entities");
        tilemap.ObjectLayers["Entities"].Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadAsync_GroupLayer_NestedGroups_AreFlattened()
    {
        var mapPath = WriteFile("map.tmj", MapWithNestedGroupLayerJson());

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers.Should().HaveCount(1);
        tilemap.Layers[0].Name.Should().Be("Ground");
    }

    [Fact]
    public async Task LoadAsync_GroupLayer_DoesNotAddGroupAsLayer()
    {
        var mapPath = WriteFile("map.tmj", MapWithGroupLayerJson(
            """{ "name": "Ground", "type": "tilelayer", "width": 2, "height": 1, "data": [1, 2], "visible": true, "opacity": 1.0 }"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers.Should().NotContain(l => l.Name == "Group1");
    }

    [Fact]
    public async Task LoadAsync_InvisibleGroup_ObjectLayerInsideGroup_ObjectsAreStillLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithInvisibleGroupObjectLayerJson("Spawns",
            """{"id":1,"name":"player","type":"","x":10.0,"y":20.0,"width":0.0,"height":0.0,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers.Should().ContainKey("Spawns");
        tilemap.ObjectLayers["Spawns"].Should().HaveCount(1);
    }

    [Fact]
    public async Task LoadAsync_VisibleGroup_ObjectLayerInsideGroup_IsLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithGroupObjectLayerJson("Entities",
            """{"id":1,"name":"player","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers.Should().ContainKey("Entities");
    }

    [Fact]
    public async Task LoadAsync_GroupWithOffset_ObjectPositionHasGroupOffsetBaked()
    {
        var mapPath = WriteFile("map.tmj", MapWithGroupObjectLayerWithOffsetJson("Layer",
            objectsJson: """{"id":1,"name":"npc","type":"","x":10.0,"y":20.0,"width":0.0,"height":0.0,"visible":true}""",
            groupOffsetX: 32f, groupOffsetY: 16f));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        var obj = tilemap.ObjectLayers["Layer"][0];
        obj.X.Should().BeApproximately(42f, 0.001f);
        obj.Y.Should().BeApproximately(36f, 0.001f);
    }

    #endregion

    #region Map Properties

    [Fact]
    public async Task LoadAsync_MapProperties_StringProperty_IsLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithMapPropertiesJson(
            """{"name":"music","type":"string","value":"theme.ogg"}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Properties.Should().ContainKey("music").WhoseValue.Should().Be("theme.ogg");
    }

    [Fact]
    public async Task LoadAsync_MapProperties_MultipleProperties_AllLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithMapPropertiesJson(
            """{"name":"music","type":"string","value":"theme.ogg"},{"name":"level_name","type":"string","value":"World 1-1"}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Properties.Should().ContainKey("music");
        tilemap.Properties.Should().ContainKey("level_name").WhoseValue.Should().Be("World 1-1");
    }

    [Fact]
    public async Task LoadAsync_MapProperties_WhenAbsent_PropertiesIsEmpty()
    {
        var mapPath = WriteFile("map.tmj", EmbeddedTilesetMapJson(
            """{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Properties.Should().BeEmpty();
    }

    [Fact]
    public async Task LoadAsync_MapProperties_NonStringProperty_IsStoredAsString()
    {
        var mapPath = WriteFile("map.tmj", MapWithMapPropertiesJson(
            """{"name":"lives","type":"int","value":3}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Properties.Should().ContainKey("lives").WhoseValue.Should().Be("3");
    }

    #endregion

    #region Image Collection Tileset

    [Fact]
    public void GetTileSourceRect_WhenColumnsIsZero_ReturnsZeroRect()
    {
        var tileset = new Tileset { FirstGid = 1, TileWidth = 16, TileHeight = 16, Columns = 0 };

        var rect = tileset.GetTileSourceRect(1);

        rect.Should().Be((0, 0, 0, 0));
    }

    [Fact]
    public void GetTileSourceRect_WhenColumnsIsZero_DoesNotThrow()
    {
        var tileset = new Tileset { FirstGid = 1, TileWidth = 16, TileHeight = 16, Columns = 0 };

        var act = () => tileset.GetTileSourceRect(1);

        act.Should().NotThrow();
    }

    #endregion

    #region Duplicate Object Layer Name

    [Fact]
    public async Task LoadAsync_DuplicateObjectLayerName_MergesObjects()
    {
        var mapPath = WriteFile("map.tmj", MapWithDuplicateObjectLayersJson());

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers.Should().ContainKey("Entities");
        tilemap.ObjectLayers["Entities"].Should().HaveCount(2);
    }

    [Fact]
    public async Task LoadAsync_DuplicateObjectLayerName_PreservesObjectsFromBothLayers()
    {
        var mapPath = WriteFile("map.tmj", MapWithDuplicateObjectLayersJson());

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        var names = tilemap.ObjectLayers["Entities"].Select(o => o.Name).ToList();
        names.Should().Contain("a");
        names.Should().Contain("b");
    }

    #endregion

    #region Object Shape

    [Fact]
    public async Task LoadAsync_RectangleObject_ShapeIsRectangle()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            """{"id":1,"name":"box","type":"","x":0.0,"y":0.0,"width":32.0,"height":16.0,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Layer"][0].Shape.Should().Be(TilemapObjectShape.Rectangle);
    }

    [Fact]
    public async Task LoadAsync_RectangleObject_PointsIsNull()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            """{"id":1,"name":"box","type":"","x":0.0,"y":0.0,"width":16.0,"height":16.0,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Layer"][0].Points.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_RectangleObject_GidIsNull()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            """{"id":1,"name":"box","type":"","x":0.0,"y":0.0,"width":16.0,"height":16.0,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Layer"][0].Gid.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_PointObject_ShapeIsPoint()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            """{"id":1,"name":"spawn","type":"","x":32.0,"y":64.0,"width":0.0,"height":0.0,"point":true,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Layer"][0].Shape.Should().Be(TilemapObjectShape.Point);
    }

    [Fact]
    public async Task LoadAsync_EllipseObject_ShapeIsEllipse()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            """{"id":1,"name":"area","type":"","x":0.0,"y":0.0,"width":32.0,"height":16.0,"ellipse":true,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Layer"][0].Shape.Should().Be(TilemapObjectShape.Ellipse);
    }

    [Fact]
    public async Task LoadAsync_PolygonObject_ShapeIsPolygon()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            """{"id":1,"name":"wall","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"polygon":[{"x":0,"y":0},{"x":16,"y":0},{"x":0,"y":16}],"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Layer"][0].Shape.Should().Be(TilemapObjectShape.Polygon);
    }

    [Fact]
    public async Task LoadAsync_PolygonObject_PointsAreLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            """{"id":1,"name":"wall","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"polygon":[{"x":0,"y":0},{"x":16,"y":0},{"x":0,"y":16}],"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        var points = tilemap.ObjectLayers["Layer"][0].Points;
        points.Should().NotBeNull();
        points!.Should().HaveCount(3);
        points[0].Should().Be((0f, 0f));
        points[1].Should().Be((16f, 0f));
        points[2].Should().Be((0f, 16f));
    }

    [Fact]
    public async Task LoadAsync_PolylineObject_ShapeIsPolyline()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            """{"id":1,"name":"path","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"polyline":[{"x":0,"y":0},{"x":32,"y":0}],"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Layer"][0].Shape.Should().Be(TilemapObjectShape.Polyline);
    }

    [Fact]
    public async Task LoadAsync_PolylineObject_PointsAreLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            """{"id":1,"name":"path","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"polyline":[{"x":0,"y":0},{"x":32,"y":0}],"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        var points = tilemap.ObjectLayers["Layer"][0].Points;
        points.Should().NotBeNull();
        points!.Should().HaveCount(2);
        points[0].Should().Be((0f, 0f));
        points[1].Should().Be((32f, 0f));
    }

    [Fact]
    public async Task LoadAsync_TileObject_ShapeIsTile()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            """{"id":1,"name":"crate","type":"","x":32.0,"y":64.0,"width":16.0,"height":16.0,"gid":1,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Layer"][0].Shape.Should().Be(TilemapObjectShape.Tile);
    }

    [Fact]
    public async Task LoadAsync_TileObject_GidIsLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            """{"id":1,"name":"crate","type":"","x":32.0,"y":64.0,"width":16.0,"height":16.0,"gid":5,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Layer"][0].Gid.Should().Be(5);
    }

    [Fact]
    public async Task LoadAsync_TileObject_HFlippedGid_StripsFlipBitAndSetsFlipHorizontal()
    {
        // 0x80000005 = GID 5 with horizontal-flip bit set
        var rawGid = (long)0x80000005L;
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            $$"""{"id":1,"name":"crate","type":"","x":0,"y":0,"width":16,"height":16,"gid":{{rawGid}},"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);
        var obj = tilemap.ObjectLayers["Layer"][0];

        obj.Gid.Should().Be(5);
        obj.FlipHorizontal.Should().BeTrue();
        obj.FlipVertical.Should().BeFalse();
        obj.FlipDiagonal.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_TileObject_VFlippedGid_StripsFlipBitAndSetsFlipVertical()
    {
        // 0x40000005 = GID 5 with vertical-flip bit set
        var rawGid = (long)0x40000005L;
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            $$"""{"id":1,"name":"crate","type":"","x":0,"y":0,"width":16,"height":16,"gid":{{rawGid}},"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);
        var obj = tilemap.ObjectLayers["Layer"][0];

        obj.Gid.Should().Be(5);
        obj.FlipHorizontal.Should().BeFalse();
        obj.FlipVertical.Should().BeTrue();
        obj.FlipDiagonal.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_TileObject_AllFlipBitsSet_StripsAllBitsAndSetsAllFlipProps()
    {
        // 0xE0000005 = GID 5 with H + V + D flip bits set
        var rawGid = (long)0xE0000005L;
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            $$"""{"id":1,"name":"crate","type":"","x":0,"y":0,"width":16,"height":16,"gid":{{rawGid}},"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);
        var obj = tilemap.ObjectLayers["Layer"][0];

        obj.Gid.Should().Be(5);
        obj.FlipHorizontal.Should().BeTrue();
        obj.FlipVertical.Should().BeTrue();
        obj.FlipDiagonal.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_TileObject_NoFlipBits_FlipPropsAreFalse()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            """{"id":1,"name":"crate","type":"","x":0,"y":0,"width":16,"height":16,"gid":3,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);
        var obj = tilemap.ObjectLayers["Layer"][0];

        obj.Gid.Should().Be(3);
        obj.FlipHorizontal.Should().BeFalse();
        obj.FlipVertical.Should().BeFalse();
        obj.FlipDiagonal.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_TextObject_ShapeIsText()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            """{"id":1,"name":"label","type":"","x":0.0,"y":0.0,"width":100.0,"height":20.0,"visible":true,"text":{"text":"Hello world","fontsize":16}}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Layer"][0].Shape.Should().Be(TilemapObjectShape.Text);
    }

    [Fact]
    public async Task LoadAsync_TextObject_TextContentIsPopulated()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            """{"id":1,"name":"label","type":"","x":0.0,"y":0.0,"width":100.0,"height":20.0,"visible":true,"text":{"text":"Hello world","fontsize":16}}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Layer"][0].TextContent.Should().Be("Hello world");
    }

    [Fact]
    public async Task LoadAsync_TextObject_PointsIsNull()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            """{"id":1,"name":"label","type":"","x":0.0,"y":0.0,"width":100.0,"height":20.0,"visible":true,"text":{"text":"Hi"}}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Layer"][0].Points.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_TextObject_GidIsNull()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            """{"id":1,"name":"label","type":"","x":0.0,"y":0.0,"width":100.0,"height":20.0,"visible":true,"text":{"text":"Hi"}}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Layer"][0].Gid.Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_NonTextObject_TextContentIsNull()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Layer",
            """{"id":1,"name":"box","type":"","x":0.0,"y":0.0,"width":32.0,"height":16.0,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers["Layer"][0].TextContent.Should().BeNull();
    }

    #endregion

    #region WorldToTile

    [Fact]
    public void WorldToTile_OriginPosition_ReturnsTileZeroZero()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 10, 10);

        tilemap.WorldToTile(0f, 0f).Should().Be((0, 0));
    }

    [Fact]
    public void WorldToTile_ExactTileBoundary_ReturnsCorrectTile()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 10, 10);

        tilemap.WorldToTile(32f, 48f).Should().Be((2, 3));
    }

    [Fact]
    public void WorldToTile_InsideTile_ReturnsTileContainingPosition()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 10, 10);

        tilemap.WorldToTile(17f, 33f).Should().Be((1, 2));
    }

    [Fact]
    public void WorldToTile_OnePixelBeforeBoundary_ReturnsCurrentTile()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 10, 10);

        tilemap.WorldToTile(31.9f, 15.9f).Should().Be((1, 0));
    }

    [Fact]
    public void WorldToTile_NegativeCoordinate_ReturnsNegativeTileIndex()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 10, 10);

        var (tileX, _) = tilemap.WorldToTile(-1f, 0f);
        tileX.Should().Be(-1);
    }

    [Fact]
    public void WorldToTile_WithLayerOffset_AccountsForOffset()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 10, 10);
        var layer = new TilemapLayer("Ground", 10, 10) { OffsetX = 32f, OffsetY = 16f };

        // world (48, 32) minus offset (32, 16) = (16, 16) → tile (1, 1)
        tilemap.WorldToTile(48f, 32f, layer).Should().Be((1, 1));
    }

    [Fact]
    public void WorldToTile_WithLayerOffsetZero_SameAsNoLayerOverload()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 10, 10);
        var layer = new TilemapLayer("Ground", 10, 10) { OffsetX = 0f, OffsetY = 0f };

        tilemap.WorldToTile(48f, 32f, layer).Should().Be(tilemap.WorldToTile(48f, 32f));
    }

    [Fact]
    public void WorldToTile_WithParallaxOneOne_SameAsLayerOverloadWithoutCamera()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 10, 10);
        var layer = new TilemapLayer("Ground", 10, 10) { ParallaxX = 1f, ParallaxY = 1f };
        var camera = new System.Numerics.Vector2(100f, 80f);

        tilemap.WorldToTile(48f, 32f, layer, camera).Should().Be(tilemap.WorldToTile(48f, 32f, layer));
    }

    [Fact]
    public void WorldToTile_WithParallaxHalf_AccountsForCameraOffset()
    {
        // Parallax 0.5: renderer shifts the layer origin by camera * (1 - 0.5) = camera * 0.5.
        // world (48,32), camera (64,32), parallax (0.5, 0.5)
        // effectiveX = 48 - 0 - 64*(1-0.5) = 48 - 32 = 16 → tile 1
        // effectiveY = 32 - 0 - 32*(1-0.5) = 32 - 16 = 16 → tile 1
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 10, 10);
        var layer = new TilemapLayer("BG", 10, 10) { ParallaxX = 0.5f, ParallaxY = 0.5f };
        var camera = new System.Numerics.Vector2(64f, 32f);

        tilemap.WorldToTile(48f, 32f, layer, camera).Should().Be((1, 1));
    }

    [Fact]
    public void WorldToTile_WithParallaxZero_FullCameraCompensation()
    {
        // Parallax 0: layer is fixed to screen, renderer shift = camera * (1 - 0) = camera.
        // effectiveX = 48 - 0 - 32*(1-0) = 48 - 32 = 16 → tile 1
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 10, 10);
        var layer = new TilemapLayer("Fixed", 10, 10) { ParallaxX = 0f, ParallaxY = 0f };
        var camera = new System.Numerics.Vector2(32f, 0f);

        var (tileX, _) = tilemap.WorldToTile(48f, 0f, layer, camera);
        tileX.Should().Be(1);
    }

    #endregion

    #region TileToWorld

    [Fact]
    public void TileToWorld_OriginTile_ReturnsZeroZero()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 10, 10);

        tilemap.TileToWorld(0, 0).Should().Be((0f, 0f));
    }

    [Fact]
    public void TileToWorld_NonOriginTile_ReturnsTopLeftCorner()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 10, 10);

        tilemap.TileToWorld(2, 3).Should().Be((32f, 48f));
    }

    [Fact]
    public void TileToWorld_IsInverseOfWorldToTile_AtTileBoundary()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 10, 10);

        var (wx, wy) = tilemap.TileToWorld(3, 5);
        tilemap.WorldToTile(wx, wy).Should().Be((3, 5));
    }

    [Fact]
    public void TileToWorld_WithLayerOffset_AccountsForOffset()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 10, 10);
        var layer = new TilemapLayer("Ground", 10, 10) { OffsetX = 32f, OffsetY = 16f };

        tilemap.TileToWorld(1, 1, layer).Should().Be((48f, 32f));
    }

    [Fact]
    public void TileToWorld_WithLayerOffset_IsInverseOfWorldToTileWithLayer()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 10, 10);
        var layer = new TilemapLayer("Ground", 10, 10) { OffsetX = 20f, OffsetY = 12f };

        var (wx, wy) = tilemap.TileToWorld(4, 2, layer);
        tilemap.WorldToTile(wx, wy, layer).Should().Be((4, 2));
    }

    [Fact]
    public void TileToWorld_WithParallaxAndCamera_IsInverseOfWorldToTileWithCamera()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 10, 10);
        var layer = new TilemapLayer("BG", 10, 10) { OffsetX = 8f, OffsetY = 4f, ParallaxX = 0.5f, ParallaxY = 0.5f };
        var camera = new System.Numerics.Vector2(64f, 32f);

        var (wx, wy) = tilemap.TileToWorld(3, 1, layer, camera);
        tilemap.WorldToTile(wx, wy, layer, camera).Should().Be((3, 1));
    }

    [Fact]
    public void TileToWorld_WithParallaxOneOne_SameAsLayerOverloadWithoutCamera()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 10, 10);
        var layer = new TilemapLayer("Ground", 10, 10) { OffsetX = 0f, OffsetY = 0f, ParallaxX = 1f, ParallaxY = 1f };
        var camera = new System.Numerics.Vector2(100f, 80f);

        tilemap.TileToWorld(2, 3, layer, camera).Should().Be(tilemap.TileToWorld(2, 3, layer));
    }

    #endregion

    #region Orientation Guard

    [Fact]
    public async Task LoadAsync_OrthogonalMap_DoesNotThrow()
    {
        var mapPath = WriteFile("map.tmj", MapWithOrientationJson("orthogonal"));
        var act = () => new TmjLoader().LoadAsync(mapPath);

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("isometric")]
    [InlineData("staggered")]
    [InlineData("hexagonal")]
    public async Task LoadAsync_NonOrthogonalMap_ThrowsNotSupported(string orientation)
    {
        var mapPath = WriteFile("map.tmj", MapWithOrientationJson(orientation));
        var act = () => new TmjLoader().LoadAsync(mapPath);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage($"*\"{orientation}\"*");
    }

    #endregion

    #region Render Order Guard

    [Fact]
    public async Task LoadAsync_RightDownRenderOrder_DoesNotThrow()
    {
        var mapPath = WriteFile("map.tmj", MapWithRenderOrderJson("right-down"));
        var act = () => new TmjLoader().LoadAsync(mapPath);

        await act.Should().NotThrowAsync();
    }

    [Fact]
    public async Task LoadAsync_MissingRenderOrder_DoesNotThrow()
    {
        var mapPath = WriteFile("map.tmj", MinimalMapJson());
        var act = () => new TmjLoader().LoadAsync(mapPath);

        await act.Should().NotThrowAsync();
    }

    [Theory]
    [InlineData("right-up")]
    [InlineData("left-down")]
    [InlineData("left-up")]
    public async Task LoadAsync_UnsupportedRenderOrder_ThrowsNotSupported(string renderOrder)
    {
        var mapPath = WriteFile("map.tmj", MapWithRenderOrderJson(renderOrder));
        var act = () => new TmjLoader().LoadAsync(mapPath);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage($"*\"{renderOrder}\"*");
    }

    [Fact]
    public async Task LoadAsync_UnsupportedRenderOrder_ErrorMessageContainsRightDown()
    {
        var mapPath = WriteFile("map.tmj", MapWithRenderOrderJson("right-up"));
        var act = () => new TmjLoader().LoadAsync(mapPath);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*right-down*");
    }

    #endregion

    #region Layer Custom Properties

    [Fact]
    public async Task LoadAsync_LayerCustomProperties_StringProperty_IsLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithLayerStringPropertyJson("zone_type", "water"));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].Properties.Should().ContainKey("zone_type").WhoseValue.Should().Be("water");
    }

    [Fact]
    public async Task LoadAsync_LayerCustomProperties_MultipleProperties_AllLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithLayerMultipleStringPropertiesJson());
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].Properties.Should().ContainKey("zone_type").WhoseValue.Should().Be("water");
        tilemap.Layers[0].Properties.Should().ContainKey("music_track").WhoseValue.Should().Be("ocean.ogg");
    }

    [Fact]
    public async Task LoadAsync_LayerCustomProperties_CollisionProp_IsNotStoredInProperties()
    {
        var mapPath = WriteFile("map.tmj", MapWithLayerBoolPropertyJson("collision", true));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].Properties.Should().NotContainKey("collision");
        tilemap.Layers[0].HasCollision.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_LayerCustomProperties_WhenNoProperties_DictionaryIsEmpty()
    {
        var mapPath = WriteFile("map.tmj", EmbeddedTilesetMapJson(
            """{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}"""));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].Properties.Should().BeEmpty();
    }

    #endregion

    #region Layer Tint Color

    [Fact]
    public async Task LoadAsync_LayerTintColor_RgbHex_IsLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithLayerTintColorJson("#ff0000"));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].TintColor.R.Should().Be(255);
        tilemap.Layers[0].TintColor.G.Should().Be(0);
        tilemap.Layers[0].TintColor.B.Should().Be(0);
    }

    [Fact]
    public async Task LoadAsync_LayerTintColor_ArgbHex_IsLoadedWithAlpha()
    {
        var mapPath = WriteFile("map.tmj", MapWithLayerTintColorJson("#80ff0000"));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].TintColor.A.Should().Be(0x80);
        tilemap.Layers[0].TintColor.R.Should().Be(255);
        tilemap.Layers[0].TintColor.G.Should().Be(0);
        tilemap.Layers[0].TintColor.B.Should().Be(0);
    }

    [Fact]
    public async Task LoadAsync_LayerTintColor_WhenAbsent_DefaultsToWhite()
    {
        var mapPath = WriteFile("map.tmj", EmbeddedTilesetMapJson(
            """{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}"""));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].TintColor.R.Should().Be(255);
        tilemap.Layers[0].TintColor.G.Should().Be(255);
        tilemap.Layers[0].TintColor.B.Should().Be(255);
        tilemap.Layers[0].TintColor.A.Should().Be(255);
    }

    #endregion

    #region Group Layer Property Inheritance

    [Fact]
    public async Task LoadAsync_GroupLayer_InheritedVisibility_FalseGroupHidesChild()
    {
        var mapPath = WriteFile("map.tmj", MapWithGroupPropertiesJson(
            groupVisible: false, groupOpacity: 1f, groupTintColor: null, groupOffsetX: 0, groupOffsetY: 0,
            groupParallaxX: 1f, groupParallaxY: 1f,
            layerVisible: true, layerOpacity: 1f));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].Visible.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_GroupLayer_InheritedVisibility_TrueGroupPreservesChildTrue()
    {
        var mapPath = WriteFile("map.tmj", MapWithGroupPropertiesJson(
            groupVisible: true, groupOpacity: 1f, groupTintColor: null, groupOffsetX: 0, groupOffsetY: 0,
            groupParallaxX: 1f, groupParallaxY: 1f,
            layerVisible: true, layerOpacity: 1f));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].Visible.Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_GroupLayer_InheritedOpacity_IsMultiplied()
    {
        var mapPath = WriteFile("map.tmj", MapWithGroupPropertiesJson(
            groupVisible: true, groupOpacity: 0.5f, groupTintColor: null, groupOffsetX: 0, groupOffsetY: 0,
            groupParallaxX: 1f, groupParallaxY: 1f,
            layerVisible: true, layerOpacity: 0.5f));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].Opacity.Should().BeApproximately(0.25f, 0.001f);
    }

    [Fact]
    public async Task LoadAsync_GroupLayer_InheritedOffset_IsAdded()
    {
        var mapPath = WriteFile("map.tmj", MapWithGroupPropertiesJson(
            groupVisible: true, groupOpacity: 1f, groupTintColor: null, groupOffsetX: 16, groupOffsetY: 32,
            groupParallaxX: 1f, groupParallaxY: 1f,
            layerVisible: true, layerOpacity: 1f, layerOffsetX: 8, layerOffsetY: 4));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].OffsetX.Should().BeApproximately(24f, 0.001f);
        tilemap.Layers[0].OffsetY.Should().BeApproximately(36f, 0.001f);
    }

    [Fact]
    public async Task LoadAsync_GroupLayer_InheritedParallax_IsMultiplied()
    {
        var mapPath = WriteFile("map.tmj", MapWithGroupPropertiesJson(
            groupVisible: true, groupOpacity: 1f, groupTintColor: null, groupOffsetX: 0, groupOffsetY: 0,
            groupParallaxX: 0.5f, groupParallaxY: 0.25f,
            layerVisible: true, layerOpacity: 1f, layerParallaxX: 0.8f, layerParallaxY: 0.4f));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].ParallaxX.Should().BeApproximately(0.4f, 0.001f);
        tilemap.Layers[0].ParallaxY.Should().BeApproximately(0.1f, 0.001f);
    }

    [Fact]
    public async Task LoadAsync_GroupLayer_InheritedTintColor_IsColorMultiplied()
    {
        // Group tint #ff8080 (R=255, G=128, B=128) * layer tint white = #ff8080
        var mapPath = WriteFile("map.tmj", MapWithGroupPropertiesJson(
            groupVisible: true, groupOpacity: 1f, groupTintColor: "#ff8080", groupOffsetX: 0, groupOffsetY: 0,
            groupParallaxX: 1f, groupParallaxY: 1f,
            layerVisible: true, layerOpacity: 1f));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].TintColor.R.Should().Be(255);
        tilemap.Layers[0].TintColor.G.Should().Be(128);
        tilemap.Layers[0].TintColor.B.Should().Be(128);
    }

    [Fact]
    public async Task LoadAsync_GroupLayer_NestedGroups_OpacityIsAccumulatedCorrectly()
    {
        var mapPath = WriteFile("map.tmj", MapWithNestedGroupOpacityJson(outerOpacity: 0.5f, innerOpacity: 0.5f, layerOpacity: 1f));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers[0].Opacity.Should().BeApproximately(0.25f, 0.001f);
    }

    #endregion

    #region Image Collection Tileset Guard

    [Fact]
    public async Task LoadAsync_ImageCollectionTileset_Embedded_ThrowsNotSupported()
    {
        var mapPath = WriteFile("map.tmj", MapWithImageCollectionTilesetJson());

        var act = () => new TmjLoader().LoadAsync(mapPath);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*image-collection*");
    }

    [Fact]
    public async Task LoadAsync_ImageCollectionTileset_External_ThrowsNotSupported()
    {
        WriteFile("collection.tsj", """{"columns":0,"tilewidth":16,"tileheight":16,"tilecount":2,"tiles":[{"id":0},{"id":1}]}""");
        var mapPath = WriteFile("map.tmj", ExternalTilesetMapJson("collection.tsj"));

        var act = () => new TmjLoader().LoadAsync(mapPath);

        await act.Should().ThrowAsync<NotSupportedException>()
            .WithMessage("*image-collection*");
    }

    #endregion

    #region Typed Property Helpers

    [Fact]
    public void Get_StringProperty_ReturnsValue()
    {
        var props = new Dictionary<string, string> { ["name"] = "hero" };

        props.Get<string>("name").Should().Be("hero");
    }

    [Fact]
    public void Get_IntProperty_ReturnsValue()
    {
        var props = new Dictionary<string, string> { ["damage"] = "42" };

        props.Get<int>("damage").Should().Be(42);
    }

    [Fact]
    public void Get_FloatProperty_ReturnsValue()
    {
        var props = new Dictionary<string, string> { ["speed"] = "1.5" };

        props.Get<float>("speed").Should().BeApproximately(1.5f, 0.001f);
    }

    [Fact]
    public void Get_DoubleProperty_ReturnsValue()
    {
        var props = new Dictionary<string, string> { ["ratio"] = "0.333" };

        props.Get<double>("ratio").Should().BeApproximately(0.333, 0.001);
    }

    [Fact]
    public void Get_BoolProperty_True_ReturnsTrue()
    {
        var props = new Dictionary<string, string> { ["solid"] = "true" };

        props.Get<bool>("solid").Should().BeTrue();
    }

    [Fact]
    public void Get_BoolProperty_False_ReturnsFalse()
    {
        var props = new Dictionary<string, string> { ["solid"] = "false" };

        props.Get<bool>("solid").Should().BeFalse();
    }

    [Fact]
    public void Get_MissingKey_ReturnsDefault()
    {
        var props = new Dictionary<string, string>();

        props.Get<int>("missing").Should().Be(0);
        props.Get<float>("missing").Should().Be(0f);
        props.Get<string>("missing").Should().BeNull();
        props.Get<bool>("missing").Should().BeFalse();
    }

    [Fact]
    public void Get_MissingKey_ReturnsProvidedDefault()
    {
        var props = new Dictionary<string, string>();

        props.Get("missing", 99).Should().Be(99);
        props.Get("missing", "fallback").Should().Be("fallback");
    }

    [Fact]
    public void Get_UnparsableInt_ReturnsDefault()
    {
        var props = new Dictionary<string, string> { ["val"] = "notanumber" };

        props.Get<int>("val", -1).Should().Be(-1);
    }

    [Fact]
    public void TryGet_ExistingInt_ReturnsTrueAndValue()
    {
        var props = new Dictionary<string, string> { ["count"] = "7" };

        props.TryGet<int>("count", out var val).Should().BeTrue();
        val.Should().Be(7);
    }

    [Fact]
    public void TryGet_MissingKey_ReturnsFalse()
    {
        var props = new Dictionary<string, string>();

        props.TryGet<int>("missing", out _).Should().BeFalse();
    }

    [Fact]
    public void TryGet_UnparsableFloat_ReturnsFalse()
    {
        var props = new Dictionary<string, string> { ["x"] = "bad" };

        props.TryGet<float>("x", out _).Should().BeFalse();
    }

    [Fact]
    public void Get_IntProperty_InvariantCulture_ParsesCorrectly()
    {
        var props = new Dictionary<string, string> { ["v"] = "1000" };

        props.Get<int>("v").Should().Be(1000);
    }

    [Fact]
    public void Get_FloatProperty_InvariantCulture_UsesDecimalPoint()
    {
        var props = new Dictionary<string, string> { ["v"] = "3.14" };

        props.Get<float>("v").Should().BeApproximately(3.14f, 0.001f);
    }

    #endregion

    #region Tile Animations

    [Fact]
    public async Task LoadAsync_AnimatedTile_AnimationIsStoredOnTileset()
    {
        var mapPath = WriteFile("map.tmj", MapWithAnimatedTilesetJson(
            ownerLocalId: 0, frame0LocalId: 0, frame0DurationMs: 200, frame1LocalId: 1, frame1DurationMs: 200));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Tileset!.Animations.Should().ContainKey(1);
    }

    [Fact]
    public async Task LoadAsync_AnimatedTile_FramesHaveCorrectGids()
    {
        var mapPath = WriteFile("map.tmj", MapWithAnimatedTilesetJson(
            ownerLocalId: 0, frame0LocalId: 0, frame0DurationMs: 200, frame1LocalId: 1, frame1DurationMs: 300));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        var animation = tilemap.Tileset!.Animations[1];
        animation.Frames.Should().HaveCount(2);
        animation.Frames[0].Gid.Should().Be(1);
        animation.Frames[1].Gid.Should().Be(2);
    }

    [Fact]
    public async Task LoadAsync_AnimatedTile_FramesHaveCorrectDurations()
    {
        var mapPath = WriteFile("map.tmj", MapWithAnimatedTilesetJson(
            ownerLocalId: 0, frame0LocalId: 0, frame0DurationMs: 150, frame1LocalId: 1, frame1DurationMs: 250));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        var animation = tilemap.Tileset!.Animations[1];
        animation.Frames[0].DurationMs.Should().Be(150);
        animation.Frames[1].DurationMs.Should().Be(250);
    }

    [Fact]
    public async Task LoadAsync_AnimatedTile_TotalDurationIsSumOfFrameDurations()
    {
        var mapPath = WriteFile("map.tmj", MapWithAnimatedTilesetJson(
            ownerLocalId: 0, frame0LocalId: 0, frame0DurationMs: 200, frame1LocalId: 1, frame1DurationMs: 300));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Tileset!.Animations[1].TotalDurationMs.Should().Be(500);
    }

    [Fact]
    public async Task LoadAsync_NoAnimations_AnimationsDictionaryIsEmpty()
    {
        var mapPath = WriteFile("map.tmj", EmbeddedTilesetMapJson(
            """{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":2,"tilecount":4}"""));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Tileset!.Animations.Should().BeEmpty();
    }

    #endregion

    #region Image Layers

    [Fact]
    public async Task LoadAsync_ImageLayer_IsAddedToImageLayers()
    {
        var mapPath = WriteFile("map.tmj", MapWithImageLayerJson("Background"));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ImageLayers.Should().HaveCount(1);
        tilemap.ImageLayers[0].Name.Should().Be("Background");
    }

    [Fact]
    public async Task LoadAsync_ImageLayer_ImagePathIsResolvedAbsolute()
    {
        var mapPath = WriteFile("map.tmj", MapWithImageLayerJson("Background"));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ImageLayers[0].ImagePath.Should().Be(
            Path.GetFullPath(Path.Combine(_tempDir, "background.png")));
    }

    [Fact]
    public async Task LoadAsync_ImageLayer_VisibilityIsPreserved()
    {
        var mapPath = WriteFile("map.tmj", MapWithImageLayerVisibilityJson("Sky", visible: false));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ImageLayers[0].Visible.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ImageLayer_OpacityIsPreserved()
    {
        var mapPath = WriteFile("map.tmj", MapWithImageLayerOpacityJson("Sky", opacity: 0.5f));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ImageLayers[0].Opacity.Should().BeApproximately(0.5f, 0.001f);
    }

    [Fact]
    public async Task LoadAsync_ImageLayer_ParallaxIsPreserved()
    {
        var mapPath = WriteFile("map.tmj", MapWithImageLayerParallaxJson("Sky", parallaxX: 0.3f, parallaxY: 0.2f));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ImageLayers[0].ParallaxX.Should().BeApproximately(0.3f, 0.001f);
        tilemap.ImageLayers[0].ParallaxY.Should().BeApproximately(0.2f, 0.001f);
    }

    [Fact]
    public async Task LoadAsync_ImageLayer_CustomProperties_AreLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithImageLayerWithPropertyJson("Sky", "scroll_speed", "2.5"));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ImageLayers[0].Properties.Should().ContainKey("scroll_speed").WhoseValue.Should().Be("2.5");
    }

    [Fact]
    public async Task GetImageLayer_ExistingName_ReturnsLayer()
    {
        var mapPath = WriteFile("map.tmj", MapWithImageLayerJson("Background"));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.GetImageLayer("Background").Should().NotBeNull();
        tilemap.GetImageLayer("Other").Should().BeNull();
    }

    [Fact]
    public async Task LoadAsync_ImageLayerInsideInvisibleGroup_InheritsGroupVisibility()
    {
        var mapPath = WriteFile("map.tmj", MapWithImageLayerInsideInvisibleGroupJson("Sky"));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ImageLayers.Should().HaveCount(1);
        tilemap.ImageLayers[0].Visible.Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ImageLayerAlongsideTileLayer_BothAreLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithImageLayerAndTileLayerJson());
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.Layers.Should().HaveCount(1);
        tilemap.ImageLayers.Should().HaveCount(1);
    }

    #endregion

    #region Object Layer Visibility

    [Fact]
    public async Task LoadAsync_VisibleObjectLayer_VisibilityIsTrue()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Spawns",
            """{"id":1,"name":"p","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true}""",
            layerVisible: true));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.GetObjectLayerVisibility("Spawns").Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_InvisibleObjectLayer_VisibilityIsFalse()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Spawns",
            """{"id":1,"name":"p","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true}""",
            layerVisible: false));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.GetObjectLayerVisibility("Spawns").Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ObjectLayerInsideInvisibleGroup_VisibilityIsFalse()
    {
        var mapPath = WriteFile("map.tmj", MapWithInvisibleGroupObjectLayerJson("Spawns",
            """{"id":1,"name":"p","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.GetObjectLayerVisibility("Spawns").Should().BeFalse();
    }

    [Fact]
    public async Task LoadAsync_ObjectLayerInsideVisibleGroup_VisibilityIsTrue()
    {
        var mapPath = WriteFile("map.tmj", MapWithGroupObjectLayerJson("Entities",
            """{"id":1,"name":"p","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true}"""));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.GetObjectLayerVisibility("Entities").Should().BeTrue();
    }

    [Fact]
    public void GetObjectLayerVisibility_UnknownLayer_ReturnsTrue()
    {
        var tilemap = new Brine2D.Tilemap.Tilemap(16, 16, 4, 4);

        tilemap.GetObjectLayerVisibility("NonExistent").Should().BeTrue();
    }

    [Fact]
    public async Task LoadAsync_ObjectLayerVisibility_ObjectsAreStillLoadedWhenInvisible()
    {
        var mapPath = WriteFile("map.tmj", MapWithObjectLayerJson("Spawns",
            """{"id":1,"name":"enemy","type":"Enemy","x":0.0,"y":0.0,"width":16.0,"height":16.0,"visible":true}""",
            layerVisible: false));

        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.ObjectLayers.Should().ContainKey("Spawns");
        tilemap.ObjectLayers["Spawns"].Should().HaveCount(1);
        tilemap.GetObjectLayerVisibility("Spawns").Should().BeFalse();
    }

    #endregion

    #region Map Background Color

    [Fact]
    public async Task LoadAsync_BackgroundColor_RgbHex_IsLoaded()
    {
        var mapPath = WriteFile("map.tmj", MapWithBackgroundColorJson("#336699"));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.BackgroundColor.R.Should().Be(0x33);
        tilemap.BackgroundColor.G.Should().Be(0x66);
        tilemap.BackgroundColor.B.Should().Be(0x99);
    }

    [Fact]
    public async Task LoadAsync_BackgroundColor_ArgbHex_IsLoadedWithAlpha()
    {
        var mapPath = WriteFile("map.tmj", MapWithBackgroundColorJson("#80336699"));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.BackgroundColor.A.Should().Be(0x80);
        tilemap.BackgroundColor.R.Should().Be(0x33);
        tilemap.BackgroundColor.G.Should().Be(0x66);
        tilemap.BackgroundColor.B.Should().Be(0x99);
    }

    [Fact]
    public async Task LoadAsync_BackgroundColor_WhenAbsent_IsTransparent()
    {
        var mapPath = WriteFile("map.tmj", EmbeddedTilesetMapJson(
            """{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}"""));
        var tilemap = await new TmjLoader().LoadAsync(mapPath);

        tilemap.BackgroundColor.Should().Be(default(Brine2D.Core.Color));
    }

    #endregion

    #region Helpers

    private static string MapWithInfiniteLayerJson(string layerName) => $$"""
        {
            "width": 16, "height": 16, "tilewidth": 16, "tileheight": 16, "infinite": true,
            "layers": [{
                "name": "{{layerName}}", "type": "tilelayer", "width": 16, "height": 16,
                "encoding": "base64", "compression": "zlib",
                "chunks": [{"x":0,"y":0,"width":16,"height":16,"data":"eJxjYBgFgx8AAIAAAg=="}],
                "visible": true, "opacity": 1.0
            }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    private static string MapWithChunkLayerNoInfiniteFlagJson(string layerName) => $$"""
        {
            "width": 16, "height": 16, "tilewidth": 16, "tileheight": 16,
            "layers": [{
                "name": "{{layerName}}", "type": "tilelayer", "width": 16, "height": 16,
                "encoding": "base64", "compression": "zlib",
                "chunks": [{"x":0,"y":0,"width":16,"height":16,"data":"eJxjYBgFgx8AAIAAAg=="}],
                "visible": true, "opacity": 1.0
            }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    private static string MapWithInfiniteMapFlagNoChunksJson() => """
        {
            "width": 4, "height": 4, "tilewidth": 16, "tileheight": 16, "infinite": true,
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 4, "height": 4,
                         "data": [], "visible": true, "opacity": 1.0 }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    private static string MapWithDuplicateObjectLayersJson() => """
                                                                {
                                                                    "width": 4, "height": 4, "tilewidth": 16, "tileheight": 16,
                                                                    "layers": [
                                                                        { "name": "Entities", "type": "objectgroup", "visible": true, "opacity": 1.0,
                                                                          "objects": [{"id":1,"name":"a","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true}] },
                                                                        { "name": "Entities", "type": "objectgroup", "visible": true, "opacity": 1.0,
                                                                          "objects": [{"id":2,"name":"b","type":"","x":0.0,"y":0.0,"width":0.0,"height":0.0,"visible":true}] }
                                                                    ],
                                                                    "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
                                                                }
                                                                """;

    private static string MapWithEncodedTileDataJson(string dataJson, string? encoding = null, string? compression = null)
    {
        var encodingField = encoding != null ? $",\"encoding\":\"{encoding}\"" : string.Empty;
        var compressionField = compression != null ? $",\"compression\":\"{compression}\"" : string.Empty;
        return $$"""
                 {
                     "width": 2, "height": 2, "tilewidth": 16, "tileheight": 16,
                     "layers": [{ "name": "Ground", "type": "tilelayer", "width": 2, "height": 2,
                                  "data": {{dataJson}}{{encodingField}}{{compressionField}}, "visible": true, "opacity": 1.0 }],
                     "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":2,"tilecount":4}]
                 }
                 """;
    }

    private static string MapWithLayerParallaxJson(float parallaxX, float parallaxY) => $$"""
          {
              "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
              "layers": [{ "name": "Ground", "type": "tilelayer", "width": 1, "height": 1,
                           "data": [1], "visible": true, "opacity": 1.0,
                           "parallaxx": {{parallaxX.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}},
                           "parallaxy": {{parallaxY.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}} }],
              "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}]
          }
          """;

    private static string EmbeddedTilesetMapJson(string tilesetJson) => $$"""
        {
            "width": 2, "height": 2, "tilewidth": 16, "tileheight": 16,
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 2, "height": 2,
                         "data": [1, 2, 1, 2], "visible": true, "opacity": 1.0 }],
            "tilesets": [{{tilesetJson}}]
        }
        """;

    private static string ExternalTilesetMapJson(string source, int firstGid = 1) => $$"""
        {
            "width": 2, "height": 2, "tilewidth": 16, "tileheight": 16,
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 2, "height": 2,
                         "data": [1, 2, 1, 2], "visible": true, "opacity": 1.0 }],
            "tilesets": [{ "firstgid": {{firstGid}}, "source": "{{source}}" }]
        }
        """;

    private static string MultiTilesetMapJson() => """
        {
            "width": 2, "height": 1, "tilewidth": 16, "tileheight": 16,
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 2, "height": 1,
                         "data": [1, 9], "visible": true, "opacity": 1.0 }],
            "tilesets": [
                {"firstgid":1, "image":"tiles1.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":8},
                {"firstgid":9, "image":"tiles2.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":8}
            ]
        }
        """;

    private static string MapWithLayerOpacityJson(float opacity) => $$"""
        {
            "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 1, "height": 1,
                         "data": [1], "visible": true, "opacity": {{opacity.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}} }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}]
        }
        """;

    private static string MapWithSingleTileGidJson(int gid) => $$"""
        {
            "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 1, "height": 1,
                         "data": [{{gid}}], "visible": true, "opacity": 1.0 }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}]
        }
        """;

    private static string MapWithLayerBoolPropertyJson(string propName, bool value) => $$"""
        {
            "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 1, "height": 1,
                         "data": [1], "visible": true, "opacity": 1.0,
                         "properties": [{"name":"{{propName}}","type":"bool","value":{{(value ? "true" : "false")}}}]
            }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}]
        }
        """;

    private static string MapWithLayerOffsetJson(float offsetX, float offsetY) => $$"""
        {
            "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 1, "height": 1,
                         "data": [1], "visible": true, "opacity": 1.0,
                         "offsetx": {{offsetX.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}},
                         "offsety": {{offsetY.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}} }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}]
        }
        """;

    private static string EmbeddedTilesetWithSpacingMarginJson(int spacing, int margin) => $$"""
        {
            "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 1, "height": 1,
                         "data": [1], "visible": true, "opacity": 1.0 }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16,
                          "spacing":{{spacing}},"margin":{{margin}}}]
        }
        """;

    private static string MapWithObjectLayerWithPropertiesJson(string layerName, string propertiesJson, string objectsJson) => $$"""
        {
            "width": 4, "height": 4, "tilewidth": 16, "tileheight": 16,
            "layers": [
                { "name": "{{layerName}}", "type": "objectgroup", "visible": true, "opacity": 1.0,
                  "properties": [{{propertiesJson}}],
                  "objects": [{{objectsJson}}] }
            ],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    private static string MapWithObjectLayerJson(string layerName, string objectsJson) => $$"""
        {
            "width": 4, "height": 4, "tilewidth": 16, "tileheight": 16,
            "layers": [
                { "name": "{{layerName}}", "type": "objectgroup", "visible": true, "opacity": 1.0,
                  "objects": [{{objectsJson}}] }
            ],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    private static string MapWithObjectLayerJson(string layerName, string objectsJson, bool layerVisible) => $$"""
        {
            "width": 4, "height": 4, "tilewidth": 16, "tileheight": 16,
            "layers": [
                { "name": "{{layerName}}", "type": "objectgroup", "visible": {{(layerVisible ? "true" : "false")}}, "opacity": 1.0,
                  "objects": [{{objectsJson}}] }
            ],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    private static string MapWithObjectLayerInGroupJson(string layerName, string objectsJson, bool layerVisible) => $$"""
        {
            "width": 4, "height": 4, "tilewidth": 16, "tileheight": 16,
            "layers": [{
                "name": "Group1", "type": "group", "visible": true, "opacity": 1.0,
                "layers": [{
                    "name": "{{layerName}}", "type": "objectgroup", "visible": {{(layerVisible ? "true" : "false")}}, "opacity": 1.0,
                    "objects": [{{objectsJson}}]
                }]
            }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    private static string MapWithObjectLayerWithOffsetJson(string layerName, float offsetX, float offsetY, string objectsJson) => $$"""
        {
            "width": 4, "height": 4, "tilewidth": 16, "tileheight": 16,
            "layers": [
                { "name": "{{layerName}}", "type": "objectgroup", "visible": true, "opacity": 1.0,
                  "offsetx": {{offsetX.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}},
                  "offsety": {{offsetY.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}},
                  "objects": [{{objectsJson}}] }
            ],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    private static string MapWithGroupLayerJson(string innerLayerJson) => $$"""
        {
            "width": 2, "height": 1, "tilewidth": 16, "tileheight": 16,
            "layers": [
                {
                    "name": "Group1",
                    "type": "group",
                    "visible": true,
                    "opacity": 1.0,
                    "layers": [{{innerLayerJson}}]
                }
            ],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":2,"tilecount":4}]
        }
        """;

    private static string MapWithNestedGroupLayerJson() => """
        {
            "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
            "layers": [
                {
                    "name": "OuterGroup",
                    "type": "group",
                    "visible": true,
                    "opacity": 1.0,
                    "layers": [
                        {
                            "name": "InnerGroup",
                            "type": "group",
                            "visible": true,
                            "opacity": 1.0,
                            "layers": [
                                { "name": "Ground", "type": "tilelayer", "width": 1, "height": 1,
                                  "data": [1], "visible": true, "opacity": 1.0 }
                            ]
                        }
                    ]
                }
            ],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}]
        }
        """;

    private static string MapWithMapPropertiesJson(string propertiesJson) => $$"""
        {
            "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
            "properties": [{{propertiesJson}}],
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 1, "height": 1,
                         "data": [1], "visible": true, "opacity": 1.0 }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}]
        }
        """;

    private static string MapWithOrientationJson(string orientation) => $$"""
        {
            "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
            "orientation": "{{orientation}}",
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 1, "height": 1,
                         "data": [1], "visible": true, "opacity": 1.0 }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}]
        }
        """;

    private static string MapWithRenderOrderJson(string renderOrder) => $$"""
        {
            "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
            "renderorder": "{{renderOrder}}",
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 1, "height": 1,
                         "data": [1], "visible": true, "opacity": 1.0 }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}]
        }
        """;

    private static string MinimalMapJson() => """
        {
            "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 1, "height": 1,
                         "data": [1], "visible": true, "opacity": 1.0 }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}]
        }
        """;

    private static string MapWithLayerStringPropertyJson(string name, string value) => $$"""
        {
            "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 1, "height": 1,
                         "data": [1], "visible": true, "opacity": 1.0,
                         "properties": [{"name":"{{name}}","type":"string","value":"{{value}}"}] }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}]
        }
        """;

    private static string MapWithLayerMultipleStringPropertiesJson() => """
        {
            "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 1, "height": 1,
                         "data": [1], "visible": true, "opacity": 1.0,
                         "properties": [
                             {"name":"zone_type","type":"string","value":"water"},
                             {"name":"music_track","type":"string","value":"ocean.ogg"}
                         ] }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}]
        }
        """;

    private static string MapWithLayerTintColorJson(string tintColor) => $$"""
        {
            "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 1, "height": 1,
                         "data": [1], "visible": true, "opacity": 1.0,
                         "tintcolor": "{{tintColor}}" }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}]
        }
        """;

    [Fact]
    public void TilemapRenderer_Dispose_DoesNotThrow()
    {
        var renderer = new Brine2D.Rendering.TilemapRenderer();
        var act = () => renderer.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void TilemapRenderer_DoubleDispose_DoesNotThrow()
    {
        var renderer = new Brine2D.Rendering.TilemapRenderer();
        renderer.Dispose();
        var act = () => renderer.Dispose();

        act.Should().NotThrow();
    }

    [Fact]
    public void TilemapRenderer_ImplementsIDisposable()
    {
        typeof(Brine2D.Rendering.TilemapRenderer)
            .Should().Implement<IDisposable>();
    }

    private static byte[] BuildRawTileBytes(params int[] gids)
    {
        var bytes = new byte[gids.Length * 4];
        for (int i = 0; i < gids.Length; i++)
            BitConverter.GetBytes(gids[i]).CopyTo(bytes, i * 4);
        return bytes;
    }

    private static string CompressBase64Zlib(byte[] raw)
    {
        using var output = new System.IO.MemoryStream();
        using (var zlib = new System.IO.Compression.ZLibStream(output, System.IO.Compression.CompressionMode.Compress))
            zlib.Write(raw, 0, raw.Length);
        return Convert.ToBase64String(output.ToArray());
    }

    private static string CompressBase64Gzip(byte[] raw)
    {
        using var output = new System.IO.MemoryStream();
        using (var gzip = new System.IO.Compression.GZipStream(output, System.IO.Compression.CompressionMode.Compress))
            gzip.Write(raw, 0, raw.Length);
        return Convert.ToBase64String(output.ToArray());
    }

    private static string CompressBase64Zstd(byte[] raw)
    {
        using var compressor = new ZstdSharp.Compressor();
        var compressed = compressor.Wrap(raw).ToArray();
        return Convert.ToBase64String(compressed);
    }

    private static string MapWithGroupPropertiesJson(
        bool groupVisible, float groupOpacity, string? groupTintColor,
        float groupOffsetX, float groupOffsetY, float groupParallaxX, float groupParallaxY,
        bool layerVisible, float layerOpacity,
        float layerOffsetX = 0f, float layerOffsetY = 0f,
        float layerParallaxX = 1f, float layerParallaxY = 1f)
    {
        var tintField = groupTintColor != null ? $",\"tintcolor\":\"{groupTintColor}\"" : string.Empty;
        return $$"""
            {
                "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
                "layers": [{
                    "name": "Group1", "type": "group",
                    "visible": {{(groupVisible ? "true" : "false")}},
                    "opacity": {{groupOpacity.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}},
                    "offsetx": {{groupOffsetX.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}},
                    "offsety": {{groupOffsetY.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}},
                    "parallaxx": {{groupParallaxX.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}},
                    "parallaxy": {{groupParallaxY.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}}{{tintField}},
                    "layers": [{
                        "name": "Ground", "type": "tilelayer", "width": 1, "height": 1, "data": [1],
                        "visible": {{(layerVisible ? "true" : "false")}},
                        "opacity": {{layerOpacity.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}},
                        "offsetx": {{layerOffsetX.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}},
                        "offsety": {{layerOffsetY.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}},
                        "parallaxx": {{layerParallaxX.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}},
                        "parallaxy": {{layerParallaxY.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}}
                    }]
                }],
                "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}]
            }
            """;
    }

    private static string MapWithNestedGroupOpacityJson(float outerOpacity, float innerOpacity, float layerOpacity) => $$"""
        {
            "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
            "layers": [{
                "name": "Outer", "type": "group", "visible": true,
                "opacity": {{outerOpacity.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}},
                "layers": [{
                    "name": "Inner", "type": "group", "visible": true,
                    "opacity": {{innerOpacity.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}},
                    "layers": [{
                        "name": "Ground", "type": "tilelayer", "width": 1, "height": 1, "data": [1],
                        "visible": true,
                        "opacity": {{layerOpacity.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}}
                    }]
                }]
            }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}]
        }
        """;

    private static string MapWithImageCollectionTilesetJson() => """
        {
            "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 1, "height": 1, "data": [1],
                         "visible": true, "opacity": 1.0 }],
            "tilesets": [{"firstgid":1,"tilewidth":16,"tileheight":16,"columns":0,"tilecount":2,
                          "tiles":[{"id":0},{"id":1}]}]
        }
        """;

    private static string MapWithGroupObjectLayerJson(string layerName, string objectsJson) => $$"""
        {
            "width": 4, "height": 4, "tilewidth": 16, "tileheight": 16,
            "layers": [{
                "name": "Group1", "type": "group", "visible": true, "opacity": 1.0,
                "layers": [{
                    "name": "{{layerName}}", "type": "objectgroup", "visible": true, "opacity": 1.0,
                    "objects": [{{objectsJson}}]
                }]
            }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    private static string MapWithInvisibleGroupObjectLayerJson(string layerName, string objectsJson) => $$"""
        {
            "width": 4, "height": 4, "tilewidth": 16, "tileheight": 16,
            "layers": [{
                "name": "HiddenGroup", "type": "group", "visible": false, "opacity": 1.0,
                "layers": [{
                    "name": "{{layerName}}", "type": "objectgroup", "visible": true, "opacity": 1.0,
                    "objects": [{{objectsJson}}]
                }]
            }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    private static string MapWithGroupObjectLayerWithOffsetJson(string layerName, string objectsJson, float groupOffsetX, float groupOffsetY) => $$"""
        {
            "width": 4, "height": 4, "tilewidth": 16, "tileheight": 16,
            "layers": [{
                "name": "Group1", "type": "group", "visible": true, "opacity": 1.0,
                "offsetx": {{groupOffsetX.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}},
                "offsety": {{groupOffsetY.ToString("F1", System.Globalization.CultureInfo.InvariantCulture)}},
                "layers": [{
                    "name": "{{layerName}}", "type": "objectgroup", "visible": true, "opacity": 1.0,
                    "objects": [{{objectsJson}}]
                }]
            }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    private static string MapWithAnimatedTilesetJson(int ownerLocalId, int frame0LocalId, int frame0DurationMs, int frame1LocalId, int frame1DurationMs) => $$"""
        {
            "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 1, "height": 1,
                         "data": [{{ownerLocalId + 1}}], "visible": true, "opacity": 1.0 }],
            "tilesets": [{
                "firstgid": 1, "image": "t.png", "tilewidth": 16, "tileheight": 16,
                "columns": 4, "tilecount": 8,
                "tiles": [{
                    "id": {{ownerLocalId}},
                    "animation": [
                        { "tileid": {{frame0LocalId}}, "duration": {{frame0DurationMs}} },
                        { "tileid": {{frame1LocalId}}, "duration": {{frame1DurationMs}} }
                    ]
                }]
            }]
        }
        """;

    private static string MapWithImageLayerJson(string layerName) => $$"""
        {
            "width": 4, "height": 4, "tilewidth": 16, "tileheight": 16,
            "layers": [{
                "name": "{{layerName}}", "type": "imagelayer", "visible": true, "opacity": 1.0,
                "image": "background.png", "x": 0, "y": 0
            }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    private static string MapWithImageLayerVisibilityJson(string layerName, bool visible) => $$"""
        {
            "width": 4, "height": 4, "tilewidth": 16, "tileheight": 16,
            "layers": [{
                "name": "{{layerName}}", "type": "imagelayer", "visible": {{(visible ? "true" : "false")}}, "opacity": 1.0,
                "image": "background.png", "x": 0, "y": 0
            }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    private static string MapWithImageLayerOpacityJson(string layerName, float opacity) => $$"""
        {
            "width": 4, "height": 4, "tilewidth": 16, "tileheight": 16,
            "layers": [{
                "name": "{{layerName}}", "type": "imagelayer", "visible": true,
                "opacity": {{opacity.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}},
                "image": "background.png", "x": 0, "y": 0
            }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    private static string MapWithImageLayerParallaxJson(string layerName, float parallaxX, float parallaxY) => $$"""
        {
            "width": 4, "height": 4, "tilewidth": 16, "tileheight": 16,
            "layers": [{
                "name": "{{layerName}}", "type": "imagelayer", "visible": true, "opacity": 1.0,
                "parallaxx": {{parallaxX.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}},
                "parallaxy": {{parallaxY.ToString("F2", System.Globalization.CultureInfo.InvariantCulture)}},
                "image": "background.png", "x": 0, "y": 0
            }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    private static string MapWithImageLayerWithPropertyJson(string layerName, string propName, string propValue) => $$"""
        {
            "width": 4, "height": 4, "tilewidth": 16, "tileheight": 16,
            "layers": [{
                "name": "{{layerName}}", "type": "imagelayer", "visible": true, "opacity": 1.0,
                "image": "background.png", "x": 0, "y": 0,
                "properties": [{"name":"{{propName}}","type":"string","value":"{{propValue}}"}]
            }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    private static string MapWithImageLayerInsideInvisibleGroupJson(string layerName) => $$"""
        {
            "width": 4, "height": 4, "tilewidth": 16, "tileheight": 16,
            "layers": [{
                "name": "HiddenGroup", "type": "group", "visible": false, "opacity": 1.0,
                "layers": [{
                    "name": "{{layerName}}", "type": "imagelayer", "visible": true, "opacity": 1.0,
                    "image": "background.png", "x": 0, "y": 0
                }]
            }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    private static string MapWithImageLayerAndTileLayerJson() => """
        {
            "width": 4, "height": 4, "tilewidth": 16, "tileheight": 16,
            "layers": [
                { "name": "Background", "type": "imagelayer", "visible": true, "opacity": 1.0,
                  "image": "background.png", "x": 0, "y": 0 },
                { "name": "Ground", "type": "tilelayer", "width": 4, "height": 4,
                  "data": [1,0,0,0, 0,0,0,0, 0,0,0,0, 0,0,0,0], "visible": true, "opacity": 1.0 }
            ],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    private static string MapWithBackgroundColorJson(string color) => $$"""
        {
            "width": 1, "height": 1, "tilewidth": 16, "tileheight": 16,
            "backgroundcolor": "{{color}}",
            "layers": [{ "name": "Ground", "type": "tilelayer", "width": 1, "height": 1,
                         "data": [1], "visible": true, "opacity": 1.0 }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":1,"tilecount":1}]
        }
        """;

    private static string MapWithUnknownLayerTypeJson(string layerName, string layerType) => $$"""
        {
            "width": 4, "height": 4, "tilewidth": 16, "tileheight": 16,
            "layers": [{
                "name": "{{layerName}}", "type": "{{layerType}}", "visible": true, "opacity": 1.0
            }],
            "tilesets": [{"firstgid":1,"image":"t.png","tilewidth":16,"tileheight":16,"columns":4,"tilecount":16}]
        }
        """;

    #endregion
}

file sealed class CapturingLogger<T> : Microsoft.Extensions.Logging.ILogger<T>
{
    public List<string> Warnings { get; } = new();

    public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
    public bool IsEnabled(Microsoft.Extensions.Logging.LogLevel logLevel) => true;

    public void Log<TState>(
        Microsoft.Extensions.Logging.LogLevel logLevel,
        Microsoft.Extensions.Logging.EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        if (logLevel == Microsoft.Extensions.Logging.LogLevel.Warning)
            Warnings.Add(formatter(state, exception));
    }
}