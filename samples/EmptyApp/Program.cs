using Brine2D.Desktop;
using Microsoft.Extensions.Hosting;

namespace EmptyApp;

internal class Program
{
    private static async Task Main(string[] args)
    {
        var host = DesktopHostBuilder.CreateWithScene<LoadingScene, GameplayScene>(opts =>
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

        await host.RunAsync();
    }
}