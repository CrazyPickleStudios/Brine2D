using System.Drawing;

namespace Brine2D.Engine;

public interface IRenderContext : IDisposable
{
    void Clear(Color color);
    void DrawRect(Rectangle rect, Color color);
    void DrawTexture(ITexture texture, Rectangle dest, Rectangle? src = null, Color? tint = null);
    void Present();
}