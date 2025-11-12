using System;
using SDL;
using static SDL.SDL3;
using static SDL.SDL3_ttf;

namespace Brine2D.SDL.Content.Loaders;

public sealed unsafe class TtfFont : IDisposable
{
    internal TTF_Font* Ptr { get; }
    public int PointSize { get; }
    public string Name { get; }

    internal TtfFont(TTF_Font* ptr, int ptSize, string name)
    {
        Ptr = ptr;
        PointSize = ptSize;
        Name = name;
    }

    public void Dispose()
    {
        if (Ptr != null)
        {
            TTF_CloseFont(Ptr);
        }
    }
}