using Brine2D.Core.Hosting;
using Brine2D.SDL.Hosting;

namespace Brine2D.SDL;

public static class DesktopHost
{
    public static IGameHost CreateDefault(int width = 1280, int height = 720)
    {
        return new SdlHost();
    }
}