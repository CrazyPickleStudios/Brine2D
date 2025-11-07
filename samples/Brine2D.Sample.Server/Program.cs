using Brine2D.Core.Hosting;
using Brine2D.Core.Runtime;
using Brine2D.Core.Timing;

namespace Brine2D.Sample.Server;

internal static class Program
{
    public static void Main()
    {
        IGameHost host = new NullHost();
        host.Run(new ServerGame());
    }
}

file sealed class ServerGame : GameBase
{
    private int _ticks;

    public override void Update(GameTime time)
    {
        _ticks++;
        if (_ticks % 60 == 0)
        {
            Console.WriteLine($"[Server] tick={_ticks} t={time.TotalSeconds:F2}");
        }
    }
}