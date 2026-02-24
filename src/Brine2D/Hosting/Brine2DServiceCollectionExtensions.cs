using Brine2D.Assets;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Engine;
using Brine2D.Events;
using Brine2D.Input;
using Brine2D.Pooling;
using Brine2D.Rendering;
using Brine2D.Threading;
using Brine2D.Systems.Audio;
using Brine2D.Systems.Input;
using Brine2D.Systems.Physics;
using Brine2D.Systems.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Brine2D.Hosting;

/// <summary>
/// Extension methods for configuring Brine2D services in an <see cref="IServiceCollection"/>.
/// Follows ASP.NET Core naming convention (e.g., EntityFrameworkServiceCollectionExtensions).
/// </summary>
public static class Brine2DServiceCollectionExtensions
{
    /// <summary>
    /// Adds Brine2D core engine services.
    /// 
    /// Singleton services (shared across scenes):
    /// - IGameContext, GameEngine, GameLoop, SceneManager, EventBus, IMainThreadDispatcher
    /// 
    /// Scoped services (new instance per scene, like ASP.NET request scope):
    /// - IEntityWorld - Each scene gets its own world, auto-disposed on scene unload
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>A <see cref="Brine2DBuilder"/> for chaining backend configuration.</returns>
    public static Brine2DBuilder AddBrine2D(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Options should already be registered by GameApplicationBuilder.Build()
        // If not registered, create defaults (for standalone usage without GameApplicationBuilder)
        var existingOptions = services.Any(d => d.ServiceType == typeof(Brine2DOptions));
        if (!existingOptions)
        {
            var options = new Brine2DOptions();
            services.TryAddSingleton(options);
            services.TryAddSingleton(options.Window);
            services.TryAddSingleton(options.Rendering);
            services.TryAddSingleton(options.ECS);
        }

        // Register core services
        AddBrineCore(services);
        AddECSCore(services);
        AddBrineEngine(services);

        return new Brine2DBuilder(services);
    }

    /// <summary>
    /// Adds core Brine2D services (event bus, threading, object pooling, assets).
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
    /// Adds core ECS infrastructure.
    /// Each scene gets its own IEntityWorld instance (scoped, like ASP.NET request scope).
    /// </summary>
    private static void AddECSCore(IServiceCollection services)
    {
        // Scene-scoped EntityWorld (new instance per scene, auto-disposed on scope disposal)
        services.TryAddScoped<IEntityWorld>(sp =>
        {
            var loggerFactory = sp.GetService<ILoggerFactory>();
            var ecsOptions = sp.GetService<ECSOptions>();

            return new EntityWorld(
                sp, // Scoped service provider (scene-specific)
                loggerFactory,
                ecsOptions);
        });
    }

    /// <summary>
    /// Adds core Brine2D engine services (game loop, scene manager, context).
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
        // Falls back to 1280x720 in headless mode.
        services.TryAddScoped<ICamera>(sp =>
        {
            var renderer = sp.GetService<IRenderer>();
            var camera = renderer != null
                ? new Camera2D(renderer.Width, renderer.Height)
                : new Camera2D(1280, 720);

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
    /// Reserved for optional system-level service registration.
    /// Camera infrastructure (<see cref="ICameraManager"/>, <see cref="ICamera"/>) is
    /// registered automatically by <see cref="AddBrine2D"/>; no call to this method required.
    /// </summary>
    public static Brine2DBuilder UseSystems(this Brine2DBuilder builder)
    {
        if (builder == null) throw new ArgumentNullException(nameof(builder));
        return builder;
    }
}