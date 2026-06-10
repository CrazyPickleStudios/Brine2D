using Brine2D.Core;

namespace Brine2D.Tilemap;

public class TilemapLayer
{
    public string Name { get; set; }

    public int Width { get; set; }

    public int Height { get; set; }

    public Tile[,] Tiles { get; set; }

    /// <summary>
    /// When true, solid tiles on this layer contribute collision rects.
    /// Both this and <see cref="TileProperties.IsSolid"/> must be set for a rect to be generated.
    /// </summary>
    public bool HasCollision { get; set; }

    public byte ZOrder { get; set; }

    public float Opacity { get; set; } = 1.0f;

    public bool Visible { get; set; } = true;

    public float OffsetX { get; set; }

    public float OffsetY { get; set; }

    /// <summary>Parallax scroll factor on X. 1.0 scrolls with the camera, 0.0 is screen-fixed. Maps to Tiled's parallaxx.</summary>
    public float ParallaxX { get; set; } = 1.0f;

    /// <summary>Parallax scroll factor on Y. 1.0 scrolls with the camera, 0.0 is screen-fixed. Maps to Tiled's parallaxy.</summary>
    public float ParallaxY { get; set; } = 1.0f;

    /// <summary>Tint multiplied with each tile during rendering. Defaults to White (no tint). Alpha is pre-multiplied with <see cref="Opacity"/>.</summary>
    public Color TintColor { get; set; } = Color.White;

    /// <summary>Custom properties from Tiled. Does not include collision/hascollision, which is reflected in <see cref="HasCollision"/>.</summary>
    public Dictionary<string, string> Properties { get; set; } = new();

    public TilemapLayer(string name, int width, int height)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Width = width;
        Height = height;
        Tiles = new Tile[width, height];
    }

    public Tile GetTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return Tile.Empty;

        return Tiles[x, y];
    }

    public void SetTile(int x, int y, Tile tile)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        Tiles[x, y] = tile;
    }
}