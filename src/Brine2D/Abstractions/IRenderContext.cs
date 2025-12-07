using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Brine2D.Abstractions
{
    public interface IRenderContext
    {
        void Clear(Color color);
        void DrawRect(Rectangle rect, Color color);
        void Present();
    }
}
