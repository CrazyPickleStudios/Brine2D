using System.Drawing;

namespace Brine2D.Engine;

public interface IRenderContext
{
    void Clear(Color color);
    void DrawRect(Rectangle rect, Color color);
    void Present();
}