using Brine2D.Assets;
using Brine2D.Audio;
using Brine2D.ECS;
using Brine2D.Engine;
using Brine2D.Events;
using Brine2D.Input;
using Brine2D.Pooling;
using Brine2D.Rendering;
using Brine2D.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Brine2D.Hosting;

/// <summary>
///     Extension methods for configuring Brine2D services in an <see cref="IServiceCollection" />.
///     Follows ASP.NET Core naming convention (e.g., EntityFrameworkServiceCollectionExtensions).
/// </summary>
public static class Brine2DServiceCollectionExtensions
{
    /// <summary>
    ///     Adds Brine2D core engine services.
    ///     Singleton services (shared across scenes):
    ///     - IGameContext, GameEngine, GameLoop, SceneManager, EventBus, IMainThreadDispatcher
    ///     - WindowOptions, RenderingOptions, ECSOptions, AudioOptions
    ///     Scoped services (new instance per scene, like ASP.NET request scope):
    ///     - IEntityWorld - Each scene gets its own world, auto-disposed on scene unload
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>A <see cref="Brine2DBuilder" /> for chaining backend configuration.</returns>
    public static Brine2DBuilder AddBrine2D(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // If GameApplicationBuilder.Build() ran first it already registered all options as
        // concrete instances; these TryAdd calls are all no-ops in that case.
        // For standalone usage (no GameApplicationBuilder) each sub-option is registered
        // independently via a factory so a partial manual registration can never leave
        // sibling sub-options unresolvable.
        services.TryAddSingleton<Brine2DOptions>();
        services.TryAddSingleton<WindowOptions>(sp => sp.GetRequiredService<Brine2DOptions>().Window);
        services.TryAddSingleton<RenderingOptions>(sp => sp.GetRequiredService<Brine2DOptions>().Rendering);
        services.TryAddSingleton<ECSOptions>(sp => sp.GetRequiredService<Brine2DOptions>().ECS);
        services.TryAddSingleton<AudioOptions>(sp => sp.GetRequiredService<Brine2DOptions>().Audio);

        // Register core services
        AddBrineCore(services);
        AddECSCore(services);
        AddBrineEngine(services);

        return new Brine2DBuilder(services);
    }

    /// <summary>
    ///     Adds core Brine2D services (event bus, threading, object pooling, assets).
    /// </summary>
    private static void AddBrineCore(IServiceCollection services)
    {
        // Event bus for pub/sub messaging
        services.TryAddSingleton<EventBus>(sp =>
            new EventBus(sp.GetService<ILogger<EventBus>>()));

        // Main thread dispatcher (required for SDL3 GPU operations)
        services.TryAddSingleton<IMainThreadDispatcher, MainThreadDispatcher>();

        // Asset loading
        services.TryAddSingleton<IAssetLoader, AssetLoader>();

        // Object pooling (required by ParticleSystem and other systems)
        services.AddObjectPooling();
    }

    /// <summary>
    ///     Adds core Brine2D engine services (game loop, scene manager, context).
    /// </summary>
    private static void AddBrineEngine(IServiceCollection services)
    {
        services.TryAddSingleton<GameEngine>();

        services.TryAddSingleton<GameLoop>(sp => new GameLoop(
            sp.GetRequiredService<ILogger<GameLoop>>(),
            sp.GetRequiredService<ILoggerFactory>(),
            sp.GetRequiredService<IGameContext>(),
            sp.GetRequiredService<SceneManager>(),
            sp.GetRequiredService<IInputContext>(),
            sp.GetRequiredService<IHostApplicationLifetime>(),
            sp.GetService<InputLayerManager>(),
            sp.GetService<IEventPump>()
        ));

        services.TryAddSingleton<IGameContext, GameContext>();

        services.TryAddSingleton<SceneManager>();
        services.TryAddSingleton<ISceneManager>(sp => sp.GetRequiredService<SceneManager>());

        // Required by CameraSystem; always a default scene system.
        services.TryAddSingleton<ICameraManager, CameraManager>();

        // Required by rendering systems; scene-scoped, one camera per scene.
        // HeadlessRenderer reports Width/Height = 0 (no window), so we explicitly
        // check for a positive size rather than a non-null renderer. This is the
        // canonical fix for the old nullable-renderer pattern where the null check
        // was originally used to detect headless mode.
        services.TryAddScoped<ICamera>(sp =>
        {
            var renderer = sp.GetService<IRenderer>();
            var (w, h) = renderer is { Width: > 0, Height: > 0 }
                ? (renderer.Width, renderer.Height)
                : (1280, 720);

            var camera = new Camera2D(w, h);

            var cameraManager = sp.GetService<ICameraManager>();
            if (cameraManager != null)
            {
                cameraManager.RegisterCamera("main", camera);
                camera.TrackRegistration(cameraManager, "main"); // auto-unregisters on scope disposal
                cameraManager.MainCamera = camera;
            }

            return camera;
        });
    }

    /// <summary>
    ///     Adds core ECS infrastructure.
    ///     Each scene gets its own IEntityWorld instance (scoped, like ASP.NET request scope).
    /// </summary>
    private static void AddECSCore(IServiceCollection services)
    {
        // Scene-scoped EntityWorld (new instance per scene, auto-disposed on scope disposal)
        services.TryAddScoped<IEntityWorld>(sp =>
        {
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var ecsOptions = sp.GetService<ECSOptions>();

            return new EntityWorld(
                sp,
                loggerFactory,
                ecsOptions);
        });
    }
}