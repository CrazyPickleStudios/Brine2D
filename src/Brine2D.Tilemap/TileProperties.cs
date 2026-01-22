namespace Brine2D.Tilemap;

/// <summary>
/// Properties and metadata for a specific tile ID in the tileset.
/// </summary>
public class TileProperties
{
    /// <summary>
    /// Tile ID this property set applies to.
    /// </summary>
    public int TileId { get; set; }

    /// <summary>
    /// Whether this tile is solid (blocks movement).
    /// </summary>
    public bool IsSolid { get; set; }

    /// <summary>
    /// Whether this tile is a one-way platform (can jump through from below).
    /// </summary>
    public bool IsOneWayPlatform { get; set; }

    /// <summary>
    /// Custom properties from Tiled (e.g., "damage", "slippery", etc.).
    /// </summary>
    public Dictionary<string, string> CustomProperties { get; set; } = new();

    public TileProperties(int tileId)
    {
        TileId = tileId;
    }
}