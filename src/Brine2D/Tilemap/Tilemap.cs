using Brine2D.Core;
using System.Numerics;

namespace Brine2D.Tilemap;

public class Tilemap
{
    public int TileWidth { get; set; }
    public int TileHeight { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
    public List<TilemapLayer> Layers { get; set; } = new();

    /// <summary>All tilesets used by this map, sorted by <see cref="Tileset.FirstGid"/> ascending.</summary>
    public List<Tileset> Tilesets { get; private set; } = new();

    /// <summary>Objects from all object layers, keyed by layer name.</summary>
    public Dictionary<string, List<TilemapObject>> ObjectLayers { get; set; } = new();

    /// <summary>Custom properties per object layer, keyed by layer name. Duplicate layer names merge with last-write-wins per key.</summary>
    public Dictionary<string, Dictionary<string, string>> ObjectLayerProperties { get; set; } = new();

    /// <summary>Map-level custom properties from Tiled (e.g. "music", "level_name").</summary>
    public Dictionary<string, string> Properties { get; set; } = new();

    /// <summary>Background color from Tiled's Map Properties. Defaults to <see cref="Color.Transparent"/>.</summary>
    public Color BackgroundColor { get; set; }

    /// <summary>
    /// Image layers in document order. Not rendered automatically — load and draw them via
    /// <see cref="TilemapImageLayer.ImagePath"/> and the layer's parallax/offset properties.
    /// </summary>
    public List<TilemapImageLayer> ImageLayers { get; private set; } = new();

    /// <summary>Effective visibility per object layer, accounting for parent group visibility. A missing key means visible; false means the layer or an ancestor was hidden.</summary>
    public Dictionary<string, bool> ObjectLayerVisibility { get; set; } = new();

    public Tileset? Tileset => Tilesets.Count > 0 ? Tilesets[0] : null;

    public Tileset? ResolveTilesetByName(string name) =>
        Tilesets.FirstOrDefault(t => t.Name == name);

    public Tilemap(int tileWidth, int tileHeight, int width, int height)
    {
        TileWidth = tileWidth;
        TileHeight = tileHeight;
        Width = width;
        Height = height;
    }

    /// <summary>Returns the tileset that owns the given GID, or null. Tilesets must be sorted by FirstGid ascending (guaranteed by <see cref="AddTileset"/>).</summary>
    public Tileset? ResolveTileset(int gid)
    {
        if (gid == 0) return null;

        Tileset? result = null;
        foreach (var ts in Tilesets)
        {
            if (ts.FirstGid <= gid)
                result = ts;
            else
                break;
        }

        return result;
    }

    /// <summary>Adds a tileset and keeps the list sorted by FirstGid.</summary>
    public void AddTileset(Tileset tileset)
    {
        Tilesets.Add(tileset);
        Tilesets = Tilesets.OrderBy(t => t.FirstGid).ToList();
    }

    public TilemapLayer? GetLayer(string name) =>
        Layers.FirstOrDefault(l => l.Name == name);

    /// <summary>Returns every layer with the given name. Unlike <see cref="GetLayer"/>, this returns all matches so duplicate-named layers are all accessible.</summary>
    public List<TilemapLayer> GetAllLayers(string name) =>
        Layers.Where(l => l.Name == name).ToList();

    public void AddLayer(TilemapLayer layer)
    {
        Layers.Add(layer);
        Layers = Layers.OrderBy(l => l.ZOrder).ToList();
    }

    /// <summary>Adds an image layer and keeps the list sorted by ZOrder.</summary>
    public void AddImageLayer(TilemapImageLayer imageLayer)
    {
        ImageLayers.Add(imageLayer);
        ImageLayers = ImageLayers.OrderBy(l => l.ZOrder).ToList();
    }

    public TilemapImageLayer? GetImageLayer(string name) =>
        ImageLayers.FirstOrDefault(l => l.Name == name);

    public bool GetObjectLayerVisibility(string layerName) =>
        !ObjectLayerVisibility.TryGetValue(layerName, out var v) || v;

    public List<TilemapObject> GetObjects(string layerName) =>
        ObjectLayers.TryGetValue(layerName, out var objects) ? objects : [];

    public TilemapObject? GetObject(string layerName, string objectName) =>
        GetObjects(layerName).FirstOrDefault(o => o.Name == objectName);

    /// <summary>Searches all object layers for the object with the given ID. IDs are unique per map in Tiled.</summary>
    public TilemapObject? GetObjectById(int id)
    {
        foreach (var objects in ObjectLayers.Values)
        {
            foreach (var obj in objects)
            {
                if (obj.Id == id)
                    return obj;
            }
        }

        return null;
    }

    public TilemapObject? GetObjectByName(string objectName)
    {
        foreach (var objects in ObjectLayers.Values)
        {
            foreach (var obj in objects)
            {
                if (obj.Name == objectName)
                    return obj;
            }
        }

        return null;
    }

    public Dictionary<string, string> GetObjectLayerProperties(string layerName) =>
        ObjectLayerProperties.TryGetValue(layerName, out var props) ? props : [];

    public List<TilemapObject> GetObjectsByType(string layerName, string type) =>
        GetObjects(layerName)
            .Where(o => string.Equals(o.Type, type, StringComparison.OrdinalIgnoreCase))
            .ToList();

    public List<TilemapObject> GetObjectsByType(string type) =>
        ObjectLayers.Values
            .SelectMany(objects => objects)
            .Where(o => string.Equals(o.Type, type, StringComparison.OrdinalIgnoreCase))
            .ToList();

    /// <summary>
    /// Returns world-space rects for all solid tiles on the layer. Requires both
    /// <see cref="TilemapLayer.HasCollision"/> and <see cref="TileProperties.IsSolid"/>.
    /// Parallax is not applied — collision layers should use the default parallax of 1.0.
    /// </summary>
    public List<Rectangle> GenerateCollisionRects(string layerName)
    {
        var rects = new List<Rectangle>();
        var layer = GetLayer(layerName);

        if (layer == null || !layer.HasCollision || Tilesets.Count == 0)
            return rects;

        for (int y = 0; y < layer.Height; y++)
        {
            for (int x = 0; x < layer.Width; x++)
            {
                var tile = layer.GetTile(x, y);
                if (tile.IsEmpty) continue;

                var tileset = ResolveTileset(tile.Id);
                if (tileset == null) continue;

                if (tileset.TileProperties.TryGetValue(tile.Id, out var props) && props.IsSolid)
                {
                    rects.Add(new Rectangle(
                        x * TileWidth + layer.OffsetX,
                        y * TileHeight + layer.OffsetY,
                        TileWidth,
                        TileHeight));
                }
            }
        }

        return rects;
    }

    public (int tileX, int tileY) WorldToTile(float worldX, float worldY) =>
        ((int)MathF.Floor(worldX / TileWidth), (int)MathF.Floor(worldY / TileHeight));

    /// <summary>
    /// Converts a world position to tile coordinates, accounting for the layer's pixel offset.
    /// Does not account for parallax — use the camera overload for parallax layers.
    /// </summary>
    public (int tileX, int tileY) WorldToTile(float worldX, float worldY, TilemapLayer layer) =>
        ((int)MathF.Floor((worldX - layer.OffsetX) / TileWidth),
         (int)MathF.Floor((worldY - layer.OffsetY) / TileHeight));

    /// <summary>
    /// Converts a world position to tile coordinates, accounting for layer offset and parallax.
    /// Exact inverse of the renderer's tile placement.
    /// </summary>
    public (int tileX, int tileY) WorldToTile(float worldX, float worldY, TilemapLayer layer, Vector2 cameraPosition)
    {
        var effectiveX = worldX - layer.OffsetX - cameraPosition.X * (1f - layer.ParallaxX);
        var effectiveY = worldY - layer.OffsetY - cameraPosition.Y * (1f - layer.ParallaxY);
        return ((int)MathF.Floor(effectiveX / TileWidth),
                (int)MathF.Floor(effectiveY / TileHeight));
    }

    public (float worldX, float worldY) TileToWorld(int tileX, int tileY) =>
        (tileX * TileWidth, tileY * TileHeight);

    /// <summary>
    /// Converts tile coordinates to world-space, accounting for the layer's pixel offset.
    /// Does not account for parallax — use the camera overload for parallax layers.
    /// </summary>
    public (float worldX, float worldY) TileToWorld(int tileX, int tileY, TilemapLayer layer) =>
        (tileX * TileWidth + layer.OffsetX,
         tileY * TileHeight + layer.OffsetY);

    /// <summary>Converts tile coordinates to world-space, accounting for layer offset and parallax. Inverse of <see cref="WorldToTile(float,float,TilemapLayer,System.Numerics.Vector2)"/>.</summary>
    public (float worldX, float worldY) TileToWorld(int tileX, int tileY, TilemapLayer layer, Vector2 cameraPosition) =>
        (tileX * TileWidth + layer.OffsetX + cameraPosition.X * (1f - layer.ParallaxX),
         tileY * TileHeight + layer.OffsetY + cameraPosition.Y * (1f - layer.ParallaxY));

    /// <summary>
    /// Like <see cref="GenerateCollisionRects"/> but merges adjacent solid tiles into larger
    /// rectangles (greedy horizontal then vertical) to avoid seams in the physics solver.
    /// </summary>
    public List<Rectangle> MergeCollisionRects(string layerName)
    {
        var layer = GetLayer(layerName);
        if (layer == null || !layer.HasCollision || Tilesets.Count == 0)
            return [];

        return MergeRects(layer, IsSolidTile);
    }

    private List<Rectangle> MergeRects(TilemapLayer layer, Func<TilemapLayer, int, int, bool> tileTest)
    {
        // Pass 1: horizontal merge — one rect per contiguous run in each row.
        var horizontalStrips = new List<Rectangle>();

        for (int y = 0; y < layer.Height; y++)
        {
            int x = 0;
            while (x < layer.Width)
            {
                if (!tileTest(layer, x, y))
                {
                    x++;
                    continue;
                }

                int startX = x;
                while (x < layer.Width && tileTest(layer, x, y))
                    x++;

                horizontalStrips.Add(new Rectangle(
                    startX * TileWidth + layer.OffsetX,
                    y * TileHeight + layer.OffsetY,
                    (x - startX) * TileWidth,
                    TileHeight));
            }
        }

        // Pass 2: vertical merge — combine strips with the same X/Width that are adjacent in Y.
        horizontalStrips.Sort((a, b) =>
        {
            int cmpX = a.X.CompareTo(b.X);
            if (cmpX != 0) return cmpX;
            int cmpW = a.Width.CompareTo(b.Width);
            if (cmpW != 0) return cmpW;
            return a.Y.CompareTo(b.Y);
        });

        var merged = new List<Rectangle>();
        int i = 0;
        while (i < horizontalStrips.Count)
        {
            var current = horizontalStrips[i];
            int j = i + 1;

            while (j < horizontalStrips.Count)
            {
                var next = horizontalStrips[j];
                if (next.X != current.X || next.Width != current.Width)
                    break;
                if (next.Y != current.Y + current.Height)
                    break;

                current = new Rectangle(current.X, current.Y, current.Width, current.Height + next.Height);
                j++;
            }

            merged.Add(current);
            i = j;
        }

        return merged;
    }

    private bool IsSolidTile(TilemapLayer layer, int x, int y)
    {
        var tile = layer.GetTile(x, y);
        if (tile.IsEmpty) return false;
        var tileset = ResolveTileset(tile.Id);
        if (tileset == null) return false;
        return tileset.TileProperties.TryGetValue(tile.Id, out var props) && props.IsSolid;
    }

    private bool IsOneWayPlatformTile(TilemapLayer layer, int x, int y)
    {
        var tile = layer.GetTile(x, y);
        if (tile.IsEmpty) return false;
        var tileset = ResolveTileset(tile.Id);
        if (tileset == null) return false;
        return tileset.TileProperties.TryGetValue(tile.Id, out var props) && props.IsOneWayPlatform;
    }

    /// <summary>
    /// Returns world-space rects for all one-way platform tiles on the layer. Requires both
    /// <see cref="TilemapLayer.HasCollision"/> and <see cref="TileProperties.IsOneWayPlatform"/>.
    /// Parallax is not applied.
    /// </summary>
    public List<Rectangle> GenerateOneWayPlatformRects(string layerName)
    {
        var rects = new List<Rectangle>();
        var layer = GetLayer(layerName);

        if (layer == null || !layer.HasCollision || Tilesets.Count == 0)
            return rects;

        for (int y = 0; y < layer.Height; y++)
        {
            for (int x = 0; x < layer.Width; x++)
            {
                if (!IsOneWayPlatformTile(layer, x, y)) continue;

                rects.Add(new Rectangle(
                    x * TileWidth + layer.OffsetX,
                    y * TileHeight + layer.OffsetY,
                    TileWidth,
                    TileHeight));
            }
        }

        return rects;
    }

    /// <summary>Like <see cref="GenerateOneWayPlatformRects"/> but merges adjacent tiles into larger rectangles.</summary>
    public List<Rectangle> MergeOneWayPlatformRects(string layerName)
    {
        var layer = GetLayer(layerName);
        if (layer == null || !layer.HasCollision || Tilesets.Count == 0)
            return [];

        return MergeRects(layer, IsOneWayPlatformTile);
    }
}