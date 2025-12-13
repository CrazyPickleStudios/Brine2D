using Brine2D.Engine;
using System.Drawing;

namespace Brine2D.Graphics.Sprites;

public sealed class SpriteAtlas : IDisposable
{
    public SpriteAtlas(ITexture texture, IReadOnlyDictionary<string, RectangleF> regions)
    {
        Texture = texture ?? throw new ArgumentNullException(nameof(texture));
        Regions = regions ?? throw new ArgumentNullException(nameof(regions));
    }

    public IReadOnlyDictionary<string, RectangleF> Regions { get; }
    public ITexture Texture { get; }

    public void Dispose()
    {
        Texture?.Dispose();
    }

    public bool TryGetRegion(string name, out RectangleF rect)
    {
        return Regions.TryGetValue(name, out rect);
    }
}