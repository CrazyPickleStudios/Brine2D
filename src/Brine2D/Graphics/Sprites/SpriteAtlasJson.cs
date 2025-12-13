using Brine2D.Engine;
using System.Drawing;
using System.Text.Json;

namespace Brine2D.Graphics.Sprites;

public sealed class SpriteAtlasJson
{
    public string Texture { get; set; } = string.Empty;
    public Dictionary<string, RectDto> Sprites { get; set; } = new();

    public sealed class RectDto
    {
        public int x { get; set; }
        public int y { get; set; }
        public int w { get; set; }
        public int h { get; set; }

        public RectangleF ToRect() => new RectangleF(x, y, w, h);
    }

    public static SpriteAtlasJson Parse(string json) =>
        JsonSerializer.Deserialize<SpriteAtlasJson>(json) ?? new SpriteAtlasJson();
}