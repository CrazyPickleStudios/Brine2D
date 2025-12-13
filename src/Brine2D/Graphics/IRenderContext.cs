using Brine2D.Engine;
using System.Drawing;

namespace Brine2D.Graphics;

public enum FlipMode
{
    None = 0,
    Horizontal = 1,
    Vertical = 2,
    HorizontalVertical = 3
}

public interface IRenderContext : IDisposable
{
    void Clear(Color color);
    void DrawRect(RectangleF rect, Color color);

    void DrawTexture(ITexture texture, RectangleF dest, RectangleF? src = null, Color? tint = null);

    void DrawTexture
    (
        ITexture texture,
        RectangleF dest,
        RectangleF? src,
        Color? tint,
        float rotationDegrees,
        PointF? origin,
        FlipMode flip = FlipMode.None
    );

    void Present();
}