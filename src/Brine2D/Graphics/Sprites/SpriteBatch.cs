using System.Drawing;

namespace Brine2D.Graphics.Sprites;

public sealed class SpriteBatch
{
    private readonly List<Item> _items = [];

    public void Clear()
    {
        _items.Clear();
    }

    public void Draw
    (
        ITexture texture,
        RectangleF src,
        RectangleF dst,
        Color? tint = null,
        float rotation = 0f,
        PointF? origin = null
    )
    {
        _items.Add(new Item(texture, src, dst, tint ?? Color.White, rotation, origin ?? PointF.Empty));
    }

    public void Draw
    (
        SpriteAtlas atlas,
        string name,
        RectangleF dst,
        Color? tint = null,
        float rotation = 0f,
        PointF? origin = null
    )
    {
        if (!atlas.TryGetRegion(name, out var src))
        {
            return;
        }

        Draw(atlas.Texture, src, dst, tint, rotation, origin);
    }

    public void Flush(IRenderContext ctx)
    {
        var byTexture = new Dictionary<ITexture, List<Item>>();

        foreach (var item in _items)
        {
            if (!byTexture.TryGetValue(item.Texture, out var list))
            {
                list = [];
                byTexture[item.Texture] = list;
            }

            list.Add(item);
        }

        foreach (var kv in byTexture)
        {
            var list = kv.Value;

            for (var i = 0; i < list.Count; i++)
            {
                var it = list[i];

                ctx.DrawTexture(it.Texture, it.Dst, it.Src, it.Tint, it.Rotation, it.Origin);
            }
        }

        _items.Clear();
    }

    private readonly struct Item
    {
        public readonly ITexture Texture;
        public readonly RectangleF Src;
        public readonly RectangleF Dst;
        public readonly Color Tint;
        public readonly float Rotation;
        public readonly PointF Origin;

        public Item(ITexture texture, RectangleF src, RectangleF dst, Color tint, float rotation, PointF origin)
        {
            Texture = texture;
            Src = src;
            Dst = dst;
            Tint = tint;
            Rotation = rotation;
            Origin = origin;
        }
    }
}