using Brine2D.Desktop;
using EmptyApp;
using Microsoft.Extensions.Hosting;

var host = DesktopHostBuilder.CreateWithScene<LoadingScene, GameplayScene>(
        opts =>
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
        }
    )
    .UseContentRoot("Assets")
    .Build();

await host.RunAsync();