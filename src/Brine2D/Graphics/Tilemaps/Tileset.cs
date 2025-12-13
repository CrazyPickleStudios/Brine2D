using System.Drawing;

namespace Brine2D.Graphics.Tilemaps;

public sealed class Tileset : IDisposable
{
    public Tileset(ITexture texture, IReadOnlyList<RectangleF> tileRegions)
    {
        Texture = texture ?? throw new ArgumentNullException(nameof(texture));
        TileRegions = tileRegions ?? throw new ArgumentNullException(nameof(tileRegions));
    }

    public ITexture Texture { get; }
    public IReadOnlyList<RectangleF> TileRegions { get; }

    public static Tileset FromGrid(ITexture texture, int tileWidth, int tileHeight)
    {
        if (texture is null)
        {
            throw new ArgumentNullException(nameof(texture));
        }

        if (tileWidth <= 0 || tileHeight <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(tileWidth));
        }

        var cols = (int)(texture.Width / tileWidth);
        var rows = (int)(texture.Height / tileHeight);
        var regions = new List<RectangleF>(cols * rows);

        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < cols; x++)
            {
                regions.Add(new RectangleF(x * tileWidth, y * tileHeight, tileWidth, tileHeight));
            }
        }

        return new Tileset(texture, regions);
    }

    public void Dispose()
    {
        Texture.Dispose();
    }
}