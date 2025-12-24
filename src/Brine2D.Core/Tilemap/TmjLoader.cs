using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace Brine2D.Core.Tilemap;

/// <summary>
/// Loads tilemaps from Tiled JSON (.tmj) format.
/// </summary>
public class TmjLoader : ITilemapLoader
{
    private readonly ILogger<TmjLoader>? _logger;

    // Allow null for cases where logging isn't needed
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
        var tmjMap = JsonSerializer.Deserialize<TmjMap>(json, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        });

        if (tmjMap == null)
            throw new InvalidOperationException($"Failed to parse tilemap file: {path}");

        var tilemap = new Tilemap(tmjMap.TileWidth, tmjMap.TileHeight, tmjMap.Width, tmjMap.Height);

        // Load tileset
        if (tmjMap.Tilesets.Count > 0)
        {
            var tmjTileset = tmjMap.Tilesets[0];
            tilemap.Tileset = LoadTileset(tmjTileset, path);
        }

        // Load layers
        int zOrder = 0;
        foreach (var tmjLayer in tmjMap.Layers.Where(l => l.Type == "tilelayer"))
        {
            var layer = LoadLayer(tmjLayer, zOrder++);
            tilemap.AddLayer(layer);
        }

        _logger?.LogInformation("Tilemap loaded: {Width}x{Height} tiles, {LayerCount} layers",
            tilemap.Width, tilemap.Height, tilemap.Layers.Count);

        return tilemap;
    }

    private Tileset LoadTileset(TmjTileset tmjTileset, string mapPath)
    {
        var tileset = new Tileset
        {
            FirstGid = tmjTileset.FirstGid,
            TileWidth = tmjTileset.TileWidth,
            TileHeight = tmjTileset.TileHeight,
            Columns = tmjTileset.Columns,
            Rows = tmjTileset.TileCount / tmjTileset.Columns
        };

        // Resolve tileset image path (relative to map file)
        if (!string.IsNullOrEmpty(tmjTileset.Image))
        {
            var mapDirectory = Path.GetDirectoryName(mapPath) ?? string.Empty;
            tileset.ImagePath = Path.Combine(mapDirectory, tmjTileset.Image);
        }

        // Load tile properties
        if (tmjTileset.Tiles != null)
        {
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
                                props.IsSolid = Convert.ToBoolean(prop.Value);
                                break;
                            case "onewayplatform":
                            case "isonewayplatform":
                                props.IsOneWayPlatform = Convert.ToBoolean(prop.Value);
                                break;
                            default:
                                props.CustomProperties[prop.Name] = prop.Value?.ToString() ?? string.Empty;
                                break;
                        }
                    }
                }

                tileset.TileProperties[tileId] = props;
            }
        }

        return tileset;
    }

    private TilemapLayer LoadLayer(TmjLayer tmjLayer, int zOrder)
    {
        var layer = new TilemapLayer(tmjLayer.Name, tmjLayer.Width, tmjLayer.Height)
        {
            ZOrder = zOrder,
            Visible = tmjLayer.Visible,
            Opacity = tmjLayer.Opacity
        };

        // Check for collision property
        if (tmjLayer.Properties != null)
        {
            var collisionProp = tmjLayer.Properties.FirstOrDefault(p =>
                p.Name.Equals("collision", StringComparison.OrdinalIgnoreCase) ||
                p.Name.Equals("hascollision", StringComparison.OrdinalIgnoreCase));

            if (collisionProp != null)
            {
                layer.HasCollision = Convert.ToBoolean(collisionProp.Value);
            }
        }

        // Convert flat array to 2D tile array
        for (int y = 0; y < tmjLayer.Height; y++)
        {
            for (int x = 0; x < tmjLayer.Width; x++)
            {
                int index = y * tmjLayer.Width + x;
                if (index < tmjLayer.Data.Count)
                {
                    int gid = tmjLayer.Data[index];

                    // Extract flip flags (Tiled encodes these in high bits)
                    const uint FLIPPED_HORIZONTALLY_FLAG = 0x80000000;
                    const uint FLIPPED_VERTICALLY_FLAG = 0x40000000;
                    const uint FLIPPED_DIAGONALLY_FLAG = 0x20000000;

                    bool flipH = ((uint)gid & FLIPPED_HORIZONTALLY_FLAG) != 0;
                    bool flipV = ((uint)gid & FLIPPED_VERTICALLY_FLAG) != 0;

                    // Clear flags to get actual tile ID (use unchecked for bit operations)
                    gid = unchecked((int)((uint)gid & ~(FLIPPED_HORIZONTALLY_FLAG | FLIPPED_VERTICALLY_FLAG | FLIPPED_DIAGONALLY_FLAG)));

                    layer.SetTile(x, y, new Tile(gid, flipH, flipV));
                }
            }
        }

        return layer;
    }
}