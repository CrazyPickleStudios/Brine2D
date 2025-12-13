using System.Drawing;
using System.Text;
using System.Text.Json;
using System.Xml.Linq;
using Brine2D.Content;

namespace Brine2D.Graphics.Tilemaps;

public sealed class TiledJsonLoader : IAssetLoader<Tilemap>
{
    private readonly IContentManager _content;

    public TiledJsonLoader(IContentManager content)
    {
        _content = content;
    }

    public async Task<Tilemap> LoadAsync(string path, CancellationToken ct = default)
    {
        var json = await File.ReadAllTextAsync(path, Encoding.UTF8, ct).ConfigureAwait(false);
        var baseDir = Path.GetDirectoryName(path) ?? string.Empty;
        return await ParseAsync(json, baseDir, ct).ConfigureAwait(false);
    }

    public async Task<Tilemap> LoadAsync(Stream stream, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
        var json = Encoding.UTF8.GetString(ms.ToArray());
        return await ParseAsync(json, string.Empty, ct).ConfigureAwait(false);
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

    private async Task<Tilemap> ParseAsync(string json, string baseDir, CancellationToken ct)
    {
        var map = JsonSerializer.Deserialize<TiledMapDto>(json) ??
                  throw new InvalidOperationException("Invalid Tiled JSON.");
        if (!string.Equals(map.orientation, "orthogonal", StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException($"Unsupported map orientation: {map.orientation}");
        }

        var tilesets = new List<TilesetRef>(map.tilesets.Length);
        foreach (var ts in map.tilesets)
        {
            TilesetData data;

            if (!string.IsNullOrWhiteSpace(ts.source))
            {
                var tsxPath = string.IsNullOrEmpty(baseDir) ? ts.source : Path.Combine(baseDir, ts.source);
                data = LoadTilesetFromTsx(tsxPath);
                data.firstgid = ts.firstgid;
            }
            else
            {
                if (string.IsNullOrWhiteSpace(ts.image))
                {
                    throw new InvalidOperationException("Inline tileset missing image.");
                }

                data = new TilesetData
                {
                    image = ResolveAssetPath(baseDir, ts.image, _content),
                    tilewidth = ts.tilewidth,
                    tileheight = ts.tileheight,
                    columns = ts.columns,
                    firstgid = ts.firstgid
                };
            }

            var texture = await _content.LoadAsync<ITexture>(data.image, ct).ConfigureAwait(false);
            var regions = BuildGridRegions(data.columns, data.tilewidth, data.tileheight, texture);
            var tileset = new Tileset(texture, regions);
            tilesets.Add(new TilesetRef(data.firstgid, tileset));
        }

        var layers = new List<TileLayer>();
        foreach (var layer in map.layers)
        {
            if (!string.Equals(layer.type, "tilelayer", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            var gids = layer.data ?? Array.Empty<int>();
            var tl = new TileLayer(layer.name ?? "Layer", gids)
            {
                Visible = layer.visible,
                Opacity = (float)layer.opacity
            };
            layers.Add(tl);
        }

        return new Tilemap(map.width, map.height, map.tilewidth, map.tileheight, tilesets, layers);
    }

    private sealed class LayerDto
    {
        public int[]? data { get; set; }
        public string? name { get; set; }
        public double opacity { get; } = 1.0;
        public string type { get; } = "tilelayer";
        public bool visible { get; } = true;
    }

    private sealed class TiledMapDto
    {
        public int height { get; set; }
        public LayerDto[] layers { get; set; } = Array.Empty<LayerDto>();
        public string orientation { get; set; } = "orthogonal";
        public int tileheight { get; set; }
        public TilesetDto[] tilesets { get; set; } = Array.Empty<TilesetDto>();
        public int tilewidth { get; set; }
        public int width { get; set; }
    }

    private sealed class TilesetData
    {
        public int columns;
        public int firstgid;
        public string image = string.Empty;
        public int tileheight;
        public int tilewidth;
    }

    private sealed class TilesetDto
    {
        public int columns { get; set; }
        public int firstgid { get; set; }
        public string image { get; } = string.Empty;
        public string? source { get; set; }
        public int tileheight { get; set; }
        public int tilewidth { get; set; }
    }
}