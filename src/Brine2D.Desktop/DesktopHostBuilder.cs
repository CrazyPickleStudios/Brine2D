using Brine2D.Abstractions;
using Brine2D.Hosting;
using Brine2D.Options;
using Brine2D.SDL3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Brine2D.Desktop;

public static class DesktopHostBuilder
{
    public static IHostBuilder CreateDefault<TGame>
    (
        Action<WindowOptions>? configureWindow = null,
        Action<LoopOptions>? configureLoop = null
    )
        where TGame : class, IGame
    {
        var builder = Host.CreateDefaultBuilder();

        builder.ConfigureServices((ctx, services) =>
        {
            services.AddLogging(logging =>
            {
                logging.ClearProviders();
                logging.AddConsole();
                logging.SetMinimumLevel(LogLevel.Information);
            });

            services.AddBrine2DCore()
                .AddBrine2DSdl3();

            services.AddOptions<LoopOptions>();

            if (configureWindow is not null)
            {
                services.Configure(configureWindow);
            }

            if (configureLoop is not null)
            {
                services.Configure(configureLoop);
            }

            services.AddSingleton<IGameContext>
            (sp =>
                new DesktopGameContext(
                    sp,
                    sp.GetRequiredService<IWindow>(),
                    sp.GetRequiredService<IInput>())
            );

            services.AddHostedService<HostedGame>();
            services.AddSingleton<IGame, TGame>();
        });

        return builder;
    }
}