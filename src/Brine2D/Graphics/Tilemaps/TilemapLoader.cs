using Brine2D.Content;

namespace Brine2D.Graphics.Tilemaps;

public sealed class TilemapLoader : IAssetLoader<Tilemap>
{
    private readonly TiledJsonLoader _json;
    private readonly TiledXmlLoader _xml;

    public TilemapLoader(IContentManager content)
    {
        _json = new TiledJsonLoader(content);
        _xml = new TiledXmlLoader(content);
    }

    public Task<Tilemap> LoadAsync(string path, CancellationToken ct = default)
    {
        var ext = Path.GetExtension(path);

        if (ext.Equals(".tmj", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".json", StringComparison.OrdinalIgnoreCase))
        {
            return _json.LoadAsync(path, ct);
        }

        if (ext.Equals(".tmx", StringComparison.OrdinalIgnoreCase) ||
            ext.Equals(".xml", StringComparison.OrdinalIgnoreCase))
        {
            return _xml.LoadAsync(path, ct);
        }

        return LoadByStreamAsync(path, ct);
    }

    public async Task<Tilemap> LoadAsync(Stream stream, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();

        await stream.CopyToAsync(ms, ct).ConfigureAwait(false);

        ms.Position = 0;

        int first;

        do
        {
            first = ms.ReadByte();
        } while (first >= 0 && char.IsWhiteSpace((char)first));

        ms.Position = 0;

        return first is '{' or '['
            ? await _json.LoadAsync(ms, ct).ConfigureAwait(false)
            : await _xml.LoadAsync(ms, ct).ConfigureAwait(false);
    }

    private async Task<Tilemap> LoadByStreamAsync(string path, CancellationToken ct)
    {
        await using var fs = File.OpenRead(path);

        return await LoadAsync(fs, ct).ConfigureAwait(false);
    }
}