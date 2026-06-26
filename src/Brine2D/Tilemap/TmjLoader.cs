using System.IO.Compression;
using System.Text.Json;
using Brine2D.Core;
using Microsoft.Extensions.Logging;
using ZstdSharp;

namespace Brine2D.Tilemap;

/// <summary>
/// Loads Tiled JSON (.tmj) maps. Supports embedded and external (.tsj) tilesets,
/// CSV and base64 tile data (uncompressed, zlib, gzip, zstd), and group layers at any depth.
/// </summary>
public class TmjLoader : ITilemapLoader
{
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    private readonly ILogger<TmjLoader>? _logger;

    public TmjLoader(ILogger<TmjLoader>? logger = null)
    {
        _logger = logger;
    }

    public async Task<Tilemap> LoadAsync(string path, CancellationToken cancellationToken = default)
    {
        if (!File.Exists(path))
            throw new FileNotFoundException($"Tilemap file not found: {path}", path);

        _logger?.LogInformation("Loading tilemap from: {Path}", path);

        var json = await File.ReadAllTextAsync(path, cancellationToken);
        var tmjMap = JsonSerializer.Deserialize<TmjMap>(json, JsonOptions);

        if (tmjMap == null)
            throw new InvalidOperationException($"Failed to parse tilemap file: {path}");

        if (!string.Equals(tmjMap.Orientation, "orthogonal", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(
                $"Tilemap \"{path}\" uses \"{tmjMap.Orientation}\" orientation. " +
                $"Only orthogonal maps are supported.");

        if (!string.IsNullOrEmpty(tmjMap.RenderOrder) &&
            !string.Equals(tmjMap.RenderOrder, "right-down", StringComparison.OrdinalIgnoreCase))
            throw new NotSupportedException(
                $"Tilemap \"{path}\" uses \"{tmjMap.RenderOrder}\" tile render order. " +
                "Only 'right-down' is supported. In Tiled: Map > Map Properties > Tile Render Order.");

        if (tmjMap.Infinite)
            throw new NotSupportedException(
                $"Tilemap \"{path}\" is an infinite map. " +
                "Only fixed-size maps are supported. In Tiled: Map > Map Properties, uncheck 'Infinite'.");

        var tilemap = new Tilemap(tmjMap.TileWidth, tmjMap.TileHeight, tmjMap.Width, tmjMap.Height);

        if (tmjMap.Properties != null)
        {
            foreach (var prop in tmjMap.Properties)
                tilemap.Properties[prop.Name] = ReadString(prop);
        }

        if (!string.IsNullOrEmpty(tmjMap.BackgroundColor))
            tilemap.BackgroundColor = ParseTiledColor(tmjMap.BackgroundColor);

        foreach (var tmjTileset in tmjMap.Tilesets)
            tilemap.AddTileset(await LoadTilesetAsync(tmjTileset, path, cancellationToken));

        var mapDirectory = Path.GetDirectoryName(path) ?? string.Empty;

        byte zOrder = 0;
        foreach (var (tmjLayer, groupState) in FlattenLayers(tmjMap.Layers))
        {
            if (tmjLayer.Type == "tilelayer")
                tilemap.AddLayer(LoadLayer(tmjLayer, zOrder++, groupState));
            else if (tmjLayer.Type == "objectgroup")
                LoadObjectLayer(tmjLayer, tilemap, groupState);
            else if (tmjLayer.Type == "imagelayer")
                tilemap.AddImageLayer(LoadImageLayer(tmjLayer, zOrder++, groupState, mapDirectory));
            else
                _logger?.LogWarning(
                    "Tilemap layer \"{LayerName}\" has unsupported type \"{LayerType}\" and will be skipped.",
                    tmjLayer.Name, tmjLayer.Type);
        }

        _logger?.LogInformation(
            "Tilemap loaded: {Width}x{Height} tiles, {LayerCount} layers, {TilesetCount} tilesets, {ObjectLayerCount} object layers, {ImageLayerCount} image layers",
            tilemap.Width, tilemap.Height, tilemap.Layers.Count, tilemap.Tilesets.Count, tilemap.ObjectLayers.Count, tilemap.ImageLayers.Count);

        return tilemap;
    }

    /// <summary>
    /// Yields all tile and object layers in document order, regardless of group nesting depth.
    /// Group visibility, opacity, tint, offset, and parallax are composed into the state
    /// passed down to each leaf layer.
    /// </summary>
    private static IEnumerable<(TmjLayer Layer, GroupState Inherited)> FlattenLayers(
        IEnumerable<TmjLayer> layers,
        GroupState? inherited = null)
    {
        inherited ??= GroupState.Default;

        foreach (var layer in layers)
        {
            if (layer.Type == "group" && layer.Layers != null)
            {
                var composed = inherited.Compose(layer);
                foreach (var child in FlattenLayers(layer.Layers, composed))
                    yield return child;
            }
            else
            {
                yield return (layer, inherited);
            }
        }
    }

    private async Task<Tileset> LoadTilesetAsync(TmjTileset tmjTileset, string mapPath, CancellationToken cancellationToken)
    {
        var mapDirectory = Path.GetDirectoryName(mapPath) ?? string.Empty;

        if (!string.IsNullOrEmpty(tmjTileset.Source))
        {
            var tsjPath = Path.GetFullPath(Path.Combine(mapDirectory, tmjTileset.Source));

            if (!File.Exists(tsjPath))
                throw new FileNotFoundException($"External tileset file not found: {tsjPath}", tsjPath);

            _logger?.LogInformation("Loading external tileset from: {Path}", tsjPath);

            var tsjJson = await File.ReadAllTextAsync(tsjPath, cancellationToken);
            var tsjData = JsonSerializer.Deserialize<TmjTileset>(tsjJson, JsonOptions)
                ?? throw new InvalidOperationException($"Failed to parse external tileset file: {tsjPath}");

            // firstgid lives in the map reference, not the .tsj file.
            // Image paths inside a .tsj are relative to the .tsj file's own directory.
            tsjData.FirstGid = tmjTileset.FirstGid;
            return BuildTileset(tsjData, Path.GetDirectoryName(tsjPath) ?? string.Empty);
        }

        return BuildTileset(tmjTileset, mapDirectory);
    }

    private static Tileset BuildTileset(TmjTileset tmjTileset, string baseDirectory)
    {
        if (string.IsNullOrEmpty(tmjTileset.Image) && tmjTileset.Columns == 0)
            throw new NotSupportedException(
                $"Tileset with firstgid={tmjTileset.FirstGid} is an image-collection tileset " +
                "(separate image per tile). Only single-image tilesets are supported. " +
                "In Tiled, open the tileset and use Tileset > Convert Tileset to convert it to a single image.");

        var tileset = new Tileset
        {
            FirstGid = tmjTileset.FirstGid,
            Name = tmjTileset.Name ?? string.Empty,
            TileWidth = tmjTileset.TileWidth,
            TileHeight = tmjTileset.TileHeight,
            Columns = tmjTileset.Columns,
            Rows = tmjTileset.Columns > 0 ? tmjTileset.TileCount / tmjTileset.Columns : 0,
            Spacing = tmjTileset.Spacing,
            Margin = tmjTileset.Margin
        };

        if (!string.IsNullOrEmpty(tmjTileset.Image))
            tileset.ImagePath = Path.GetFullPath(Path.Combine(baseDirectory, tmjTileset.Image));

        if (tmjTileset.Properties != null)
        {
            foreach (var prop in tmjTileset.Properties)
                tileset.CustomProperties[prop.Name] = ReadString(prop);
        }

        if (tmjTileset.Tiles == null)
            return tileset;

        foreach (var tmjTile in tmjTileset.Tiles)
        {
            var tileId = tmjTile.Id + tmjTileset.FirstGid;
            var props = new TileProperties(tileId);

            if (tmjTile.Properties != null)
            {
                foreach (var prop in tmjTile.Properties)
                {
                    switch (prop.Name.ToLowerInvariant())
                    {
                        case "solid":
                        case "issolid":
                            props.IsSolid = ReadBool(prop);
                            break;
                        case "onewayplatform":
                        case "isonewayplatform":
                            props.IsOneWayPlatform = ReadBool(prop);
                            break;
                        default:
                            props.CustomProperties[prop.Name] = ReadString(prop);
                            break;
                    }
                }
            }

            tileset.TileProperties[tileId] = props;

            if (tmjTile.Animation != null && tmjTile.Animation.Count > 0)
            {
                var frames = tmjTile.Animation
                    .Select(f => new TileAnimationFrame(f.TileId + tmjTileset.FirstGid, f.Duration))
                    .ToList();
                tileset.Animations[tileId] = new TileAnimation(tileId, frames);
            }
        }

        return tileset;
    }

    private static void LoadObjectLayer(TmjLayer tmjLayer, Tilemap tilemap, GroupState groupState)
    {
        var objects = new List<TilemapObject>();
        var offsetX = groupState.OffsetX + tmjLayer.OffsetX;
        var offsetY = groupState.OffsetY + tmjLayer.OffsetY;
        var effectiveVisible = groupState.Visible && tmjLayer.Visible;
        tilemap.ObjectLayerVisibility[tmjLayer.Name] = effectiveVisible;

        foreach (var tmjObj in tmjLayer.Objects ?? [])
        {
            bool flipH = false, flipV = false, flipD = false;
            int? cleanGid = null;
            if (tmjObj.Gid.HasValue)
            {
                const long kFlipH = 0x80000000L;
                const long kFlipV = 0x40000000L;
                const long kFlipD = 0x20000000L;
                var rawGid = tmjObj.Gid.Value;
                flipH = (rawGid & kFlipH) != 0;
                flipV = (rawGid & kFlipV) != 0;
                flipD = (rawGid & kFlipD) != 0;
                cleanGid = (int)(rawGid & ~(kFlipH | kFlipV | kFlipD));
            }

            var obj = new TilemapObject
            {
                Id = tmjObj.Id,
                Name = tmjObj.Name,
                // Tiled 1.9+ renamed the "type" user field to "class". Fall back to Class when Type is empty.
                Type = !string.IsNullOrEmpty(tmjObj.Type) ? tmjObj.Type : (tmjObj.Class ?? string.Empty),
                X = tmjObj.X + offsetX,
                // Tiled places tile object Y at the bottom-left of the tile; normalize to top-left.
                Y = tmjObj.Y + offsetY - (cleanGid.HasValue ? tmjObj.Height : 0),
                Width = tmjObj.Width,
                Height = tmjObj.Height,
                Rotation = tmjObj.Rotation,
                Visible = tmjObj.Visible,
                Gid = cleanGid,
                FlipHorizontal = flipH,
                FlipVertical = flipV,
                FlipDiagonal = flipD,
                Shape = tmjObj.Point ? TilemapObjectShape.Point
                    : tmjObj.Ellipse ? TilemapObjectShape.Ellipse
                    : tmjObj.Polygon != null ? TilemapObjectShape.Polygon
                    : tmjObj.Polyline != null ? TilemapObjectShape.Polyline
                    : cleanGid.HasValue ? TilemapObjectShape.Tile
                    : tmjObj.Text != null ? TilemapObjectShape.Text
                    : TilemapObjectShape.Rectangle,
                Points = tmjObj.Polygon?.Select(p => (p.X, p.Y)).ToList()
                         ?? tmjObj.Polyline?.Select(p => (p.X, p.Y)).ToList(),
                TextContent = tmjObj.Text?.Text,
            };

            if (tmjObj.Properties != null)
            {
                foreach (var prop in tmjObj.Properties)
                    obj.CustomProperties[prop.Name] = ReadString(prop);
            }

            objects.Add(obj);
        }

        // Two object layers with the same name are valid in Tiled. Merge rather than overwrite
        // so no objects are silently lost.
        if (tilemap.ObjectLayers.TryGetValue(tmjLayer.Name, out var existing))
            existing.AddRange(objects);
        else
            tilemap.ObjectLayers[tmjLayer.Name] = objects;

        if (tmjLayer.Properties != null && tmjLayer.Properties.Count > 0)
        {
            if (!tilemap.ObjectLayerProperties.TryGetValue(tmjLayer.Name, out var layerProps))
            {
                layerProps = new Dictionary<string, string>();
                tilemap.ObjectLayerProperties[tmjLayer.Name] = layerProps;
            }

            foreach (var prop in tmjLayer.Properties)
                layerProps[prop.Name] = ReadString(prop);
        }
    }

    private static TilemapImageLayer LoadImageLayer(TmjLayer tmjLayer, byte zOrder, GroupState groupState, string mapDirectory)
    {
        var layerTint = string.IsNullOrEmpty(tmjLayer.TintColor)
            ? Color.White
            : ParseTiledColor(tmjLayer.TintColor);

        var imageLayer = new TilemapImageLayer
        {
            Name = tmjLayer.Name,
            ZOrder = zOrder,
            Visible = groupState.Visible && tmjLayer.Visible,
            Opacity = groupState.Opacity * tmjLayer.Opacity,
            OffsetX = groupState.OffsetX + tmjLayer.OffsetX,
            OffsetY = groupState.OffsetY + tmjLayer.OffsetY,
            ParallaxX = groupState.ParallaxX * tmjLayer.ParallaxX,
            ParallaxY = groupState.ParallaxY * tmjLayer.ParallaxY,
            TintColor = MultiplyColors(groupState.TintColor, layerTint),
            ImagePath = string.IsNullOrEmpty(tmjLayer.Image)
                ? string.Empty
                : Path.GetFullPath(Path.Combine(mapDirectory, tmjLayer.Image)),
        };

        if (tmjLayer.Properties != null)
        {
            foreach (var prop in tmjLayer.Properties)
                imageLayer.Properties[prop.Name] = ReadString(prop);
        }

        return imageLayer;
    }

    private static TilemapLayer LoadLayer(TmjLayer tmjLayer, byte zOrder, GroupState groupState)
    {
        var layerTint = string.IsNullOrEmpty(tmjLayer.TintColor)
            ? Color.White
            : ParseTiledColor(tmjLayer.TintColor);

        var layer = new TilemapLayer(tmjLayer.Name, tmjLayer.Width, tmjLayer.Height)
        {
            ZOrder = zOrder,
            Visible = groupState.Visible && tmjLayer.Visible,
            Opacity = groupState.Opacity * tmjLayer.Opacity,
            OffsetX = groupState.OffsetX + tmjLayer.OffsetX,
            OffsetY = groupState.OffsetY + tmjLayer.OffsetY,
            ParallaxX = groupState.ParallaxX * tmjLayer.ParallaxX,
            ParallaxY = groupState.ParallaxY * tmjLayer.ParallaxY,
            TintColor = MultiplyColors(groupState.TintColor, layerTint),
        };

        if (tmjLayer.Properties != null)
        {
            foreach (var prop in tmjLayer.Properties)
            {
                var nameLower = prop.Name.ToLowerInvariant();
                if (nameLower is "collision" or "hascollision")
                {
                    layer.HasCollision = ReadBool(prop);
                }
                else
                {
                    layer.Properties[prop.Name] = ReadString(prop);
                }
            }
        }

        var gids = DecodeTileData(tmjLayer);

        for (int y = 0; y < tmjLayer.Height; y++)
        {
            for (int x = 0; x < tmjLayer.Width; x++)
            {
                int index = y * tmjLayer.Width + x;
                if (index >= gids.Count)
                    continue;

                int gid = gids[index];

                const uint FlippedHorizontallyFlag = 0x80000000;
                const uint FlippedVerticallyFlag = 0x40000000;
                const uint FlippedDiagonallyFlag = 0x20000000;

                bool flipH = ((uint)gid & FlippedHorizontallyFlag) != 0;
                bool flipV = ((uint)gid & FlippedVerticallyFlag) != 0;
                bool flipD = ((uint)gid & FlippedDiagonallyFlag) != 0;

                gid = unchecked((int)((uint)gid & ~(FlippedHorizontallyFlag | FlippedVerticallyFlag | FlippedDiagonallyFlag)));

                layer.SetTile(x, y, new Tile(gid, flipH, flipV, flipD));
            }
        }

        return layer;
    }

    /// <summary>Decodes tile GIDs from a layer. Handles CSV arrays and base64 with optional zlib, gzip, or zstd compression.</summary>
    private static List<int> DecodeTileData(TmjLayer layer)
    {
        if (layer.Chunks.HasValue && layer.Chunks.Value.ValueKind == JsonValueKind.Array)
            throw new NotSupportedException(
                $"Tilemap layer \"{layer.Name}\" uses infinite/chunk mode. " +
                "Only fixed-size maps are supported. In Tiled: Map > Map Properties, uncheck 'Infinite'.");

        if (layer.Data.ValueKind == JsonValueKind.Array)
        {
            var result = new List<int>(layer.Width * layer.Height);
            foreach (var element in layer.Data.EnumerateArray())
                result.Add(element.GetInt32());
            return result;
        }

        if (layer.Data.ValueKind == JsonValueKind.String)
        {
            var compression = layer.Compression?.ToLowerInvariant();

            return compression switch
            {
                null or "" => DecodeBase64TileData(layer.Data.GetString()!),
                "zlib" => DecodeBase64ZlibTileData(layer.Data.GetString()!),
                "gzip" => DecodeBase64GzipTileData(layer.Data.GetString()!),
                "zstd" => DecodeBase64ZstdTileData(layer.Data.GetString()!),
                _ => throw new NotSupportedException(
                    $"Tilemap layer \"{layer.Name}\" uses base64+{compression} encoding. " +
                    $"Only CSV, base64, base64+zlib, base64+gzip, and base64+zstd are supported.")
            };
        }

        return [];
    }

    private static List<int> DecodeBase64TileData(string base64)
    {
        var bytes = Convert.FromBase64String(base64);
        var gids = new List<int>(bytes.Length / 4);

        for (int i = 0; i < bytes.Length; i += 4)
            gids.Add(BitConverter.ToInt32(bytes, i));

        return gids;
    }

    private static List<int> DecodeBase64GzipTileData(string base64)
    {
        var compressed = Convert.FromBase64String(base64);
        using var input = new MemoryStream(compressed);
        using var gzip = new GZipStream(input, CompressionMode.Decompress);
        return ReadDecompressedGids(gzip);
    }

    private static List<int> DecodeBase64ZstdTileData(string base64)
    {
        var compressed = Convert.FromBase64String(base64);
        using var decompressor = new Decompressor();
        var decompressed = decompressor.Unwrap(compressed);
        var gids = new List<int>(decompressed.Length / 4);
        for (int i = 0; i < decompressed.Length; i += 4)
            gids.Add(BitConverter.ToInt32(decompressed[i..]));
        return gids;
    }

    private static List<int> DecodeBase64ZlibTileData(string base64)
    {
        var compressed = Convert.FromBase64String(base64);
        using var input = new MemoryStream(compressed);
        using var zlib = new ZLibStream(input, CompressionMode.Decompress);
        return ReadDecompressedGids(zlib);
    }

    private static List<int> ReadDecompressedGids(Stream stream)
    {
        var gids = new List<int>();
        var buf = new byte[4];
        while (stream.Read(buf, 0, 4) == 4)
            gids.Add(BitConverter.ToInt32(buf, 0));
        return gids;
    }

    /// <summary>Parses a Tiled color string (#RRGGBB or #AARRGGBB). Returns White on failure.</summary>
    internal static Color ParseTiledColor(string hex)
    {
        var s = hex.TrimStart('#');
        if (s.Length == 6 && uint.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var rgb))
            return new Color((byte)(rgb >> 16), (byte)(rgb >> 8 & 0xFF), (byte)(rgb & 0xFF));

        if (s.Length == 8 && uint.TryParse(s, System.Globalization.NumberStyles.HexNumber, null, out var argb))
            return new Color((byte)(argb >> 16 & 0xFF), (byte)(argb >> 8 & 0xFF), (byte)(argb & 0xFF), (byte)(argb >> 24));

        return Color.White;
    }

    private static bool ReadBool(TmjProperty prop)
    {
        if (prop.Value.ValueKind == JsonValueKind.True) return true;
        if (prop.Value.ValueKind == JsonValueKind.False) return false;
        if (prop.Value.ValueKind == JsonValueKind.String)
            return bool.Parse(prop.Value.GetString()!);
        return false;
    }

    private static string ReadString(TmjProperty prop)
    {
        return prop.Value.ValueKind == JsonValueKind.String
            ? prop.Value.GetString() ?? string.Empty
            : prop.Value.ToString();
    }

    private static Color MultiplyColors(Color a, Color b) =>
        new Color(
            (byte)(a.R * b.R / 255),
            (byte)(a.G * b.G / 255),
            (byte)(a.B * b.B / 255),
            (byte)(a.A * b.A / 255));

    private sealed class GroupState
    {
        public static readonly GroupState Default = new();

        public bool Visible { get; private set; } = true;
        public float Opacity { get; private set; } = 1f;
        public Color TintColor { get; private set; } = Color.White;
        public float OffsetX { get; private set; }
        public float OffsetY { get; private set; }
        public float ParallaxX { get; private set; } = 1f;
        public float ParallaxY { get; private set; } = 1f;

        public GroupState Compose(TmjLayer group)
        {
            var groupTint = string.IsNullOrEmpty(group.TintColor)
                ? Color.White
                : ParseTiledColor(group.TintColor);

            return new GroupState
            {
                Visible = Visible && group.Visible,
                Opacity = Opacity * group.Opacity,
                TintColor = MultiplyColors(TintColor, groupTint),
                OffsetX = OffsetX + group.OffsetX,
                OffsetY = OffsetY + group.OffsetY,
                ParallaxX = ParallaxX * group.ParallaxX,
                ParallaxY = ParallaxY * group.ParallaxY,
            };
        }
    }
}