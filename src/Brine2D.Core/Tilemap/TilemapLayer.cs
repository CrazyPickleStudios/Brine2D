namespace Brine2D.Core.Tilemap;

/// <summary>
/// Represents a single layer in a tilemap.
/// </summary>
public class TilemapLayer
{
    /// <summary>
    /// Layer name (e.g., "background", "gameplay", "foreground").
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Width of the layer in tiles.
    /// </summary>
    public int Width { get; set; }

    /// <summary>
    /// Height of the layer in tiles.
    /// </summary>
    public int Height { get; set; }

    /// <summary>
    /// 2D array of tiles [x, y].
    /// </summary>
    public Tile[,] Tiles { get; set; }

    /// <summary>
    /// Whether this layer generates collision.
    /// </summary>
    public bool HasCollision { get; set; }

    /// <summary>
    /// Rendering order (lower renders first/behind).
    /// </summary>
    public int ZOrder { get; set; }

    /// <summary>
    /// Layer opacity (0.0 = invisible, 1.0 = fully visible).
    /// </summary>
    public float Opacity { get; set; } = 1.0f;

    /// <summary>
    /// Whether this layer is visible.
    /// </summary>
    public bool Visible { get; set; } = true;

    public TilemapLayer(string name, int width, int height)
    {
        Name = name ?? throw new ArgumentNullException(nameof(name));
        Width = width;
        Height = height;
        Tiles = new Tile[width, height];
    }

    /// <summary>
    /// Gets a tile at the specified position.
    /// </summary>
    public Tile GetTile(int x, int y)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return Tile.Empty;

        return Tiles[x, y];
    }

    /// <summary>
    /// Sets a tile at the specified position.
    /// </summary>
    public void SetTile(int x, int y, Tile tile)
    {
        if (x < 0 || x >= Width || y < 0 || y >= Height)
            return;

        Tiles[x, y] = tile;
    }
}