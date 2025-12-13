using System.Drawing;
using System.Text;
using Brine2D.Content;
using Brine2D.Engine;
using Brine2D.Graphics;
using Brine2D.Graphics.Sprites;

namespace Brine2D.SDL3;

internal sealed class SdlSpriteAtlasLoader : IAssetLoader<SpriteAtlas>
{
    private readonly IContentManager _content;

    public SdlSpriteAtlasLoader(IContentManager content)
    {
        _content = content;
    }

    public async Task<SpriteAtlas> LoadAsync(string path, CancellationToken ct = default)
    {
        var json = await File.ReadAllTextAsync(path, Encoding.UTF8, ct).ConfigureAwait(false);
        var dto = SpriteAtlasJson.Parse(json);

        var texture = await _content.LoadAsync<ITexture>(dto.Texture, ct).ConfigureAwait(false);

        var regions = new Dictionary<string, RectangleF>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in dto.Sprites)
        {
            regions[kv.Key] = kv.Value.ToRect();
        }

        return new SpriteAtlas(texture, regions);
    }

    public async Task<SpriteAtlas> LoadAsync(Stream stream, CancellationToken ct = default)
    {
        using var ms = new MemoryStream();
        await stream.CopyToAsync(ms, ct).ConfigureAwait(false);
        var json = Encoding.UTF8.GetString(ms.ToArray());

        var dto = SpriteAtlasJson.Parse(json);
        var texture = await _content.LoadAsync<ITexture>(dto.Texture, ct).ConfigureAwait(false);

        var regions = new Dictionary<string, RectangleF>(StringComparer.OrdinalIgnoreCase);
        foreach (var kv in dto.Sprites)
        {
            regions[kv.Key] = kv.Value.ToRect();
        }

        return new SpriteAtlas(texture, regions);
    }
}