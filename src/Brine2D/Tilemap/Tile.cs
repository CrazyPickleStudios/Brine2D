namespace Brine2D.Tilemap;

/// <summary>
/// Represents a single tile in a tilemap.
/// </summary>
public struct Tile
{
    /// <summary>
    /// Tile ID from the tileset (0 = empty/no tile).
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Whether this tile is flipped horizontally.
    /// </summary>
    public bool FlipHorizontal { get; set; }

    /// <summary>
    /// Whether this tile is flipped vertically.
    /// </summary>
    public bool FlipVertical { get; set; }

    /// <summary>
    /// Whether this tile is flipped diagonally.
    /// </summary>
    public bool FlipDiagonal { get; set; }

    public Tile(int id, bool flipH = false, bool flipV = false, bool flipD = false)
    {
        Id = id;
        FlipHorizontal = flipH;
        FlipVertical = flipV;
        FlipDiagonal = flipD;
    }

    public static Tile Empty => new Tile(0);
    public bool IsEmpty => Id == 0;
}