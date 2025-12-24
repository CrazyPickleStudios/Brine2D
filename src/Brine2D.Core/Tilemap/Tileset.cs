namespace Brine2D.Core.Tilemap;

/// <summary>
/// Represents a tileset (sprite sheet) used by a tilemap.
/// Pure data - no rendering dependencies.
/// </summary>
public class Tileset
{
    /// <summary>
    /// Path to the tileset image file.
    /// </summary>
    public string ImagePath { get; set; } = string.Empty;

    /// <summary>
    /// Width of each tile in pixels.
    /// </summary>
    public int TileWidth { get; set; }

    /// <summary>
    /// Height of each tile in pixels.
    /// </summary>
    public int TileHeight { get; set; }

    /// <summary>
    /// Number of tile columns in the tileset image.
    /// </summary>
    public int Columns { get; set; }

    /// <summary>
    /// Number of tile rows in the tileset image.
    /// </summary>
    public int Rows { get; set; }

    /// <summary>
    /// First tile ID (GID) - used by Tiled for multiple tilesets.
    /// </summary>
    public int FirstGid { get; set; } = 1;

    /// <summary>
    /// Properties for specific tiles (indexed by tile ID).
    /// </summary>
    public Dictionary<int, TileProperties> TileProperties { get; set; } = new();

    /// <summary>
    /// Gets the source rectangle for a tile ID in the tileset texture.
    /// </summary>
    public (int x, int y, int width, int height) GetTileSourceRect(int tileId)
    {
        if (tileId == 0) return (0, 0, 0, 0); // Empty tile

        // Adjust for firstgid (Tiled uses GID, we use local IDs)
        var localId = tileId - FirstGid;
        
        var column = localId % Columns;
        var row = localId / Columns;

        return (
            column * TileWidth,
            row * TileHeight,
            TileWidth,
            TileHeight
        );
    }
}