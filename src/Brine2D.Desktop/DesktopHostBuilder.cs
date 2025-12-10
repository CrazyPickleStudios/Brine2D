using Brine2D.Engine;
using Brine2D.Hosting;
using Brine2D.Input;
using Brine2D.Options;
using Brine2D.SDL3;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Brine2D.Desktop;

public static class DesktopHostBuilder
{
    public static IHostBuilder CreateWithGame<TGame>
    (
        Action<WindowOptions>? configureWindow = null,
        Action<LoopOptions>? configureLoop = null
    )
        where TGame : class, IGame
    {
        return CreateInternal(configureWindow, configureLoop, services =>
        {
            services.AddHostedService<HostedGame>();
            services.AddSingleton<IGame, TGame>();
        });
    }

    public static IHostBuilder CreateWithScene<TInitialScene>
    (
        Action<WindowOptions>? configureWindow = null,
        Action<LoopOptions>? configureLoop = null
    )
        where TInitialScene : class, IScene
    {
        return CreateInternal(configureWindow, configureLoop, services =>
        {
            services.AddSingleton<ISceneManager, SceneManager>();
            services.AddTransient<TInitialScene>();

            services.AddSingleton<IGame>(sp =>
            {
                var scenes = sp.GetRequiredService<ISceneManager>();
                var game = new GameBase(scenes);

                var initial = sp.GetRequiredService<TInitialScene>();
                scenes.SetInitialScene(initial);

                return game;
            });

            services.AddHostedService<HostedGame>();
        });
    }

    public static IHostBuilder CreateWithScene<TLoadingScene, TInitialScene>
    (
        Action<WindowOptions>? configureWindow = null,
        Action<LoopOptions>? configureLoop = null
    )
        where TLoadingScene : class, IScene
        where TInitialScene : class, IScene
    {
        return CreateInternal(configureWindow, configureLoop, services =>
        {
            services.AddSingleton<ISceneManager, SceneManager>();
            services.AddTransient<TLoadingScene>();
            services.AddTransient<TInitialScene>();

            services.AddSingleton<IGame>(sp =>
            {
                var scenes = sp.GetRequiredService<ISceneManager>();
                var game = new GameBase(scenes);

                var loading = sp.GetRequiredService<TLoadingScene>();
                var initial = sp.GetRequiredService<TInitialScene>();

                scenes.SetLoading(loading);
                scenes.SetInitialScene(loading);
                scenes.LoadSceneAsync(() => initial);

                return game;
            });

            services.AddHostedService<HostedGame>();
        });
    }

    private static void ConfigureLogging(IServiceCollection services)
    {
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddConsole();
            logging.SetMinimumLevel(LogLevel.Information);
        });
    }

    private static IHostBuilder CreateInternal
    (
        Action<WindowOptions>? configureWindow,
        Action<LoopOptions>? configureLoop,
        Action<IServiceCollection> registerScenesAndGame
    )
    {
        var builder = Host.CreateDefaultBuilder();

        builder.ConfigureServices((ctx, services) =>
        {
            ConfigureLogging(services);

            services.AddBrine2DCore().AddBrine2DSdl3();

            services.AddOptions<LoopOptions>();

            if (configureWindow is not null)
            {
                services.Configure(configureWindow);
            }

            if (configureLoop is not null)
            {
                services.Configure(configureLoop);
            }

            services.AddSingleton<IGameContext>(sp =>
                new DesktopGameContext(
                    sp,
                    sp.GetRequiredService<IWindow>(),
                    sp.GetRequiredService<IInput>()));

            registerScenesAndGame(services);
        });

        return builder;
    }
}