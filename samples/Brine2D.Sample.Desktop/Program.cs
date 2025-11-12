using Brine2D.SDL;

namespace Brine2D.Sample.Desktop;

internal class Program
{
    private static void Main(string[] args)
    {
        var host = DesktopHost.CreateDefault();
        host.Run(new MyGame());
    }
}