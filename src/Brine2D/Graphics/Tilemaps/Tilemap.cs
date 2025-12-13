namespace Brine2D.Graphics.Tilemaps;

public sealed class Tilemap : IDisposable
{
    public Tilemap(int mapWidthTiles, int mapHeightTiles, int tileWidth, int tileHeight,
        IReadOnlyList<TilesetRef> tilesets, IReadOnlyList<TileLayer> layers)
    {
        MapWidthTiles = mapWidthTiles;
        MapHeightTiles = mapHeightTiles;
        TileWidth = tileWidth;
        TileHeight = tileHeight;
        Tilesets = tilesets ?? throw new ArgumentNullException(nameof(tilesets));
        Layers = layers ?? throw new ArgumentNullException(nameof(layers));
    }

    public IReadOnlyList<TileLayer> Layers { get; }
    public int MapHeightTiles { get; }
    public int MapWidthTiles { get; }
    public int TileHeight { get; }
    public IReadOnlyList<TilesetRef> Tilesets { get; }
    public int TileWidth { get; }

    public void Dispose()
    {
        foreach (var ts in Tilesets)
        {
            ts.Tileset.Dispose();
        }
    }
}

public sealed class TilesetRef
{
    public TilesetRef(int firstGid, Tileset tileset)
    {
        FirstGid = firstGid;
        Tileset = tileset ?? throw new ArgumentNullException(nameof(tileset));
    }

    public int FirstGid { get; }
    public Tileset Tileset { get; }
}

public sealed class TileLayer
{
    public TileLayer(string name, int[] gids)
    {
        Name = name;
        GIds = gids ?? throw new ArgumentNullException(nameof(gids));
    }

    public int[] GIds { get; }
    public string Name { get; }
    public float Opacity { get; set; } = 1f;
    public bool Visible { get; set; } = true;
}