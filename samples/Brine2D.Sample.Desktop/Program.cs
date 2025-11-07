using Brine2D.Core.Hosting;
using Brine2D.Core.Math;
using Brine2D.Core.Runtime;
using Brine2D.Core.Timing;
using Brine2D.SDL.Hosting;

namespace Brine2D.Sample.Desktop;

internal class Program
{
    private static void Main(string[] args)
    {
        // Swap to SdlHost once SDL3-CS is wired. For now, it runs a minimal loop.
        IGameHost host = new SdlHost();
        host.Run(new Game1());
    }
}

file sealed class SampleGame : GameBase
{
    private double _t;

    public override void Draw(GameTime time)
    {
        // Simple color pulse using the renderer abstraction
        var intensity = (byte)(127 + 127 * Math.Sin(_t));
        Engine.Renderer.Clear(new Color(50, intensity, 200));
        Engine.Renderer.Present();
    }

    public override void Update(GameTime time)
    {
        _t += time.DeltaSeconds;
    }
}