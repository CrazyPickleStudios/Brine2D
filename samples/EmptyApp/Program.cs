using System.Drawing;
using Brine2D.Abstractions;
using Brine2D.Desktop;
using Brine2D.Engine;
using Microsoft.Extensions.Hosting;

namespace EmptyApp;

internal class Program
{
    private static void Main(string[] args)
    {
        var host = DesktopHostBuilder.CreateDefault<DemoGame>(opts =>
            {
                opts.Title = "Brine2D Demo";
                opts.Width = 800;
                opts.Height = 600;
                opts.VSync = true;
            },
            loop =>
            {
                loop.UseFixedStep = true;
                loop.FixedStepSeconds = 1.0 / 60.0;
                loop.MaxFps = null;
            }).Build();

        host.Run();
    }
}

internal sealed class DemoGame : IGame
{
    private int _x;

    public void Initialize(IGameContext context)
    {
        context.Window.Show();
    }

    public void Render(IRenderContext ctx)
    {
        ctx.Clear(Color.FromArgb(255, 30, 30, 30));
        ctx.DrawRect(new Rectangle(_x, 100, 40, 40), Color.FromArgb(255, 0, 200, 255));
        ctx.Present();
    }

    public void Update(GameTime time)
    {
        _x += (int)(60 * time.DeltaSeconds);
        if (_x > 760)
        {
            _x = 0;
        }
    }
}