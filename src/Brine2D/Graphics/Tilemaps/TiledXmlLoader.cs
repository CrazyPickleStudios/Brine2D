using System.Drawing;
using System.Xml.Linq;
using Brine2D.Content;

namespace Brine2D.Graphics.Tilemaps;

/// <summary>
///     Loads Tiled TMX (XML) maps into a Tilemap, supporting external TSX tilesets and inline tilesets.
///     Supports orthogonal maps. Layer data is read from CSV-encoded <data>.
/// </summary>
public sealed class TiledXmlLoader : IAssetLoader<Tilemap>
{
    private readonly IContentManager _content;

    public TiledXmlLoader(IContentManager content)
    {
        _content = content;
    }

    public Task<Tilemap> LoadAsync(string path, CancellationToken ct = default)
    {
        var doc = XDocument.Load(path);
        var baseDir = Path.GetDirectoryName(path) ?? string.Empty;
        return ParseAsync(doc, baseDir, ct);
    }

    public async Task<Tilemap> LoadAsync(Stream stream, CancellationToken ct = default)
    {
        var doc = XDocument.Load(stream);

        return await ParseAsync(doc, string.Empty, ct).ConfigureAwait(false);
    }

    private static List<RectangleF> BuildGridRegions(int columns, int tileWidth, int tileHeight, ITexture texture)
    {
        if (columns <= 0)
        {
            columns = Math.Max(1, (int)(texture.Width / tileWidth));
        }

        var rows = Math.Max(1, (int)(texture.Height / tileHeight));
        var regions = new List<RectangleF>(columns * rows);

        for (var y = 0; y < rows; y++)
        {
            for (var x = 0; x < columns; x++)
            {
                regions.Add(new RectangleF(x * tileWidth, y * tileHeight, tileWidth, tileHeight));
            }
        }

        return regions;
    }

    private static int[] ParseCsvGids(string csv, int expectedCount)
    {
        var parts = csv.Split(new[] { ',', '\n', '\r', '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
        var gids = new int[parts.Length];

        for (var i = 0; i < parts.Length; i++)
        {
            gids[i] = int.TryParse(parts[i], out var v) ? v : 0;
        }

        return gids;
    }

    private static string ResolveAssetPath(string baseDir, string path, IContentManager content)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return path;
        }

        if (Path.IsPathRooted(path))
        {
            return path;
        }

        if (!string.IsNullOrEmpty(baseDir))
        {
            return Path.Combine(baseDir, path);
        }

        return Path.Combine(content.ContentRoot, path);
    }

    private TilesetData LoadTilesetFromTsx(string tsxPath)
    {
        var doc = XDocument.Load(tsxPath);
        var tsNode = doc.Root ?? throw new InvalidOperationException("Invalid TSX: missing root.");

        var tilewidth = (int?)tsNode.Attribute("tilewidth") ??
                        throw new InvalidOperationException("TSX missing tilewidth.");
        var tileheight = (int?)tsNode.Attribute("tileheight") ??
                         throw new InvalidOperationException("TSX missing tileheight.");
        var columns = (int?)tsNode.Attribute("columns") ?? 0;

        var imageNode = tsNode.Element("image") ?? throw new InvalidOperationException("TSX missing image element.");
        var imageSource = (string?)imageNode.Attribute("source") ??
                          throw new InvalidOperationException("TSX image missing source.");

        var tsxDir = Path.GetDirectoryName(tsxPath) ?? string.Empty;
        var imagePath = ResolveAssetPath(tsxDir, imageSource, _content);

        return new TilesetData
        {
            image = imagePath,
            tilewidth = tilewidth,
            tileheight = tileheight,
            columns = columns
        };
    }

    private async Task<Tilemap> ParseAsync(XDocument doc, string baseDir, CancellationToken ct)
    {
        var map = doc.Root ?? throw new InvalidOperationException("Invalid TMX: missing <map> root.");

        var orientation = (string?)map.Attribute("orientation") ?? "orthogonal";
        if (!string.Equals(orientation, "orthogonal", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Unsupported map orientation: {orientation}");
        }

        var mapWidthTiles = (int?)map.Attribute("width") ??
                            throw new InvalidOperationException("TMX missing map width.");
        var mapHeightTiles = (int?)map.Attribute("height") ??
                             throw new InvalidOperationException("TMX missing map height.");
        var tileWidth = (int?)map.Attribute("tilewidth") ??
                        throw new InvalidOperationException("TMX missing tilewidth.");
        var tileHeight = (int?)map.Attribute("tileheight") ??
                         throw new InvalidOperationException("TMX missing tileheight.");

        var tilesets = new List<TilesetRef>();
        foreach (var tsNode in map.Elements("tileset"))
        {
            var firstgid = (int?)tsNode.Attribute("firstgid") ?? 1;

            TilesetData data;

            var source = (string?)tsNode.Attribute("source");
            if (!string.IsNullOrWhiteSpace(source))
            {
                var tsxPath = string.IsNullOrEmpty(baseDir) ? source : Path.Combine(baseDir, source);
                data = LoadTilesetFromTsx(tsxPath);
                data.firstgid = firstgid;
            }
            else
            {
                var imageNode = tsNode.Element("image") ??
                                throw new InvalidOperationException("Inline tileset missing <image>.");
                var imageSource = (string?)imageNode.Attribute("source") ??
                                  throw new InvalidOperationException("Tileset image missing source.");
                var tw = (int?)tsNode.Attribute("tilewidth") ?? tileWidth;
                var th = (int?)tsNode.Attribute("tileheight") ?? tileHeight;

                var columns = (int?)tsNode.Attribute("columns") ?? 0;

                data = new TilesetData
                {
                    image = ResolveAssetPath(baseDir, imageSource, _content),
                    tilewidth = tw,
                    tileheight = th,
                    columns = columns,
                    firstgid = firstgid
                };
            }

            var texture = await _content.LoadAsync<ITexture>(data.image, ct).ConfigureAwait(false);
            var regions = BuildGridRegions(data.columns, data.tilewidth, data.tileheight, texture);
            var tileset = new Tileset(texture, regions);
            tilesets.Add(new TilesetRef(data.firstgid, tileset));
        }

        var layers = new List<TileLayer>();
        foreach (var layerNode in map.Elements("layer"))
        {
            var name = (string?)layerNode.Attribute("name") ?? "Layer";
            var visible = ((int?)layerNode.Attribute("visible") ?? 1) != 0;
            var opacity = (float?)layerNode.Attribute("opacity") ?? 1.0f;

            var dataNode = layerNode.Element("data") ??
                           throw new InvalidOperationException($"Layer '{name}' missing <data>.");
            var encoding = (string?)dataNode.Attribute("encoding") ?? "csv";

            int[] gids;

            if (string.Equals(encoding, "csv", StringComparison.OrdinalIgnoreCase))
            {
                var csv = dataNode.Value ?? string.Empty;
                gids = ParseCsvGids(csv, mapWidthTiles * mapHeightTiles);
            }
            else
            {
                throw new NotSupportedException($"Unsupported TMX data encoding: {encoding}");
            }

            var tl = new TileLayer(name, gids)
            {
                Visible = visible,
                Opacity = opacity
            };

            layers.Add(tl);
        }

        return new Tilemap(mapWidthTiles, mapHeightTiles, tileWidth, tileHeight, tilesets, layers);
    }

    private sealed class TilesetData
    {
        public int columns;
        public int firstgid;
        public string image = string.Empty;
        public int tileheight;
        public int tilewidth;
    }
}