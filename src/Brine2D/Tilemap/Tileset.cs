namespace Brine2D.Tilemap;

public class Tileset
{
    public string Name { get; set; } = string.Empty;

    public string ImagePath { get; set; } = string.Empty;

    public int TileWidth { get; set; }

    public int TileHeight { get; set; }

    /// <summary>Number of tile columns in the sheet. 0 for image-collection tilesets (not supported).</summary>
    public int Columns { get; set; }

    public int Rows { get; set; }

    /// <summary>First GID — the offset Tiled applies when this tileset appears in a multi-tileset map.</summary>
    public int FirstGid { get; set; } = 1;

    /// <summary>Pixel gap between adjacent tiles in the sheet. Prevents texture bleeding on non-power-of-two tiles.</summary>
    public int Spacing { get; set; }

    /// <summary>Pixel inset from the image edge to the first tile.</summary>
    public int Margin { get; set; }

    /// <summary>Per-tile properties keyed by GID. Only tiles with properties in Tiled are present.</summary>
    public Dictionary<int, TileProperties> TileProperties { get; set; } = new();

    /// <summary>Tile animations keyed by GID. Only tiles with animations in Tiled are present.</summary>
    public Dictionary<int, TileAnimation> Animations { get; set; } = new();

    public Dictionary<string, string> CustomProperties { get; set; } = new();

    /// <summary>Returns the source rect for a GID in the tileset texture, accounting for spacing and margin. Returns a zero rect for GID 0 or image-collection tilesets.</summary>
    public (int x, int y, int width, int height) GetTileSourceRect(int tileId)
    {
        if (tileId == 0) return (0, 0, 0, 0);
        if (Columns == 0) return (0, 0, 0, 0);

        var localId = tileId - FirstGid;
        var column = localId % Columns;
        var row = localId / Columns;

        return (
            Margin + column * (TileWidth + Spacing),
            Margin + row * (TileHeight + Spacing),
            TileWidth,
            TileHeight
        );
    }
}