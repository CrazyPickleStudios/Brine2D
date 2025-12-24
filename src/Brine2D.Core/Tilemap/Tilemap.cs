using Brine2D.Core.Collision;
using System.Numerics;

namespace Brine2D.Core.Tilemap;

/// <summary>
/// Represents a tilemap with multiple layers and a tileset.
/// Pure data structure - rendering is handled by TilemapRenderer extension.
/// </summary>
public class Tilemap
{
    /// <summary>
    /// Width of each tile in pixels.
    /// </summary>
    public int TileWidth { get; set; }

    /// <summary>
    /// Height of each tile in pixels.
    /// </summary>
    public int TileHeight { get; set; }

    /// <summary>
    /// Width of the tilemap in tiles.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Height of the tilemap in tiles.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// Layers in the tilemap (background, gameplay, foreground, etc.).
    /// </summary>
    public List<TilemapLayer> Layers { get; set; } = new();

    /// <summary>
    /// Tileset used by this tilemap.
    /// </summary>
    public Tileset? Tileset { get; set; }

    public Tilemap(int tileWidth, int tileHeight, int width, int height)
    {
        TileWidth = tileWidth;
        TileHeight = tileHeight;
        Width = width;
        Height = height;
    }

    /// <summary>
    /// Gets a layer by name.
    /// </summary>
    public TilemapLayer? GetLayer(string name)
    {
        return Layers.FirstOrDefault(l => l.Name == name);
    }

    /// <summary>
    /// Adds a layer to the tilemap.
    /// </summary>
    public void AddLayer(TilemapLayer layer)
    {
        Layers.Add(layer);
        Layers = Layers.OrderBy(l => l.ZOrder).ToList();
    }

    /// <summary>
    /// Generates collision shapes for all solid tiles in the specified layer.
    /// </summary>
    public List<BoxCollider> GenerateColliders(string layerName)
    {
        var colliders = new List<BoxCollider>();
        var layer = GetLayer(layerName);

        if (layer == null || !layer.HasCollision || Tileset == null)
            return colliders;

        for (int y = 0; y < layer.Height; y++)
        {
            for (int x = 0; x < layer.Width; x++)
            {
                var tile = layer.GetTile(x, y);
                if (tile.IsEmpty) continue;

                if (Tileset.TileProperties.TryGetValue(tile.Id, out var props) && props.IsSolid)
                {
                    var worldX = x * TileWidth;
                    var worldY = y * TileHeight;

                    var collider = new BoxCollider(TileWidth, TileHeight)
                    {
                        Position = new Vector2(worldX, worldY)
                    };

                    colliders.Add(collider);
                }
            }
        }

        return colliders;
    }
}