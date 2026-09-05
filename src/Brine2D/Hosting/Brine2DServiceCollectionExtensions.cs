using System.Numerics;
using Brine2D.Assets;
using Brine2D.Audio;
using Brine2D.ECS;
using Brine2D.Engine;
using Brine2D.Events;
using Brine2D.Input;
using Brine2D.Physics;
using Brine2D.Pooling;
using Brine2D.Rendering;
using Brine2D.Rendering.SDL.PostProcessing;
using Brine2D.Threading;
using Brine2D.UI;
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
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <returns>A <see cref="Brine2DBuilder" /> for chaining backend configuration.</returns>
    public static Brine2DBuilder AddBrine2D(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // TryAdd: no-ops when GameApplicationBuilder already registered these as concrete instances.
        services.TryAddSingleton<Brine2DOptions>();
        services.TryAddSingleton<AssetOptions>(sp => sp.GetRequiredService<Brine2DOptions>().Assets);
        services.TryAddSingleton<WindowOptions>(sp => sp.GetRequiredService<Brine2DOptions>().Window);
        services.TryAddSingleton<RenderingOptions>(sp => sp.GetRequiredService<Brine2DOptions>().Rendering);
        services.TryAddSingleton<ECSOptions>(sp => sp.GetRequiredService<Brine2DOptions>().ECS);
        services.TryAddSingleton<AudioOptions>(sp => sp.GetRequiredService<Brine2DOptions>().Audio);

        AddBrineCore(services);
        AddECSCore(services);
        AddBrineEngine(services);

        // Box2D ships as part of core Brine2D (not an optional assembly), so PhysicsWorld and the
        // default Box2DPhysicsSystem must always be resolvable, even when AddPhysics() is never
        // called. AddPhysics() remains available purely to configure gravity/layers/thresholds.
        services.AddPhysicsCore();

        return new Brine2DBuilder(services);
    }

    private static void AddBrineCore(IServiceCollection services)
    {
        // Both the concrete type and interface are registered: SDL3 infrastructure injects
        // EventBus directly; scenes and systems inject IEventBus for testability.
        services.TryAddSingleton<EventBus>();
        services.TryAddSingleton<IEventBus>(sp => sp.GetRequiredService<EventBus>());

        services.TryAddSingleton<IMainThreadDispatcher, MainThreadDispatcher>();

        // AssetCache is a singleton (owns the cache and native resources).
        // Each scene's DI scope gets an AssetLoader that tracks refs and
        // releases them automatically when the scope is disposed.
        services.TryAddSingleton<AssetCache>();
        services.TryAddScoped<IAssetLoader, AssetLoader>();

        services.AddObjectPooling();

        services.TryAddEnumerable(
            ServiceDescriptor.Singleton<IHostedService, Brine2DOptionsValidatorService>());
    }

    private static void AddBrineEngine(IServiceCollection services)
    {
        services.TryAddSingleton<GameEngine>();
        services.TryAddSingleton<GameLoop>();

        services.TryAddSingleton<IGameContext, GameContext>();
        services.TryAddSingleton<SceneFrameworkServices>();
        services.TryAddSingleton<SceneManager>();
        services.TryAddSingleton<ISceneManager>(sp => sp.GetRequiredService<SceneManager>());
        services.TryAddSingleton<ISceneLoop>(sp => sp.GetRequiredService<SceneManager>());
        services.TryAddSingleton<ICameraManager, CameraManager>();

        // Scene load error info: written by SceneManager, read by fallback scenes via ISceneLoadErrorInfo.
        services.TryAddSingleton<SceneLoadErrorInfo>();
        services.TryAddSingleton<ISceneLoadErrorInfo>(sp => sp.GetRequiredService<SceneLoadErrorInfo>());

        // Built-in fallback scene; replaced project-wide via builder.UseFallbackScene<T>().
        services.TryAddTransient<DefaultFallbackScene>();

        // Camera registration with ICameraManager is handled by SceneManager.SetupScene; DI factories must be side-effect-free.
        // When AddLetterbox() is configured, all game-world rendering happens in the fixed
        // design resolution (not the live window/viewport size), so the camera's viewport
        // must match the design resolution rather than the renderer/window size.
        services.TryAddScoped<ICamera>(sp =>
        {
            var designResolution = sp.GetService<LetterboxDesignResolution>();
            int w, h;
            if (designResolution != null)
            {
                (w, h) = (designResolution.Width, designResolution.Height);
            }
            else
            {
                var renderer = sp.GetService<IRenderer>();
                var windowOptions = sp.GetRequiredService<WindowOptions>();
                (w, h) = renderer is { Width: > 0, Height: > 0 }
                    ? (renderer.Width, renderer.Height)
                    : (windowOptions.Width, windowOptions.Height);
            }

            return new Camera2D(w, h) { Position = new Vector2(w / 2f, h / 2f) };
        });

        // UICanvas is on by default (no AddUICanvas() call required), matching the default
        // camera/physics precedent. Window-resize tracking is NOT automatic — opt in via
        // canvas.AutoResizeToWindow = true (see UICanvas.AutoResizeToWindow).
        services.TryAddScoped<UICanvas>(sp =>
        {
            var renderer = sp.GetService<IRenderer>();
            var windowOptions = sp.GetRequiredService<WindowOptions>();
            var (w, h) = renderer is { Width: > 0, Height: > 0 }
                ? (renderer.Width, renderer.Height)
                : (windowOptions.Width, windowOptions.Height);

            var input = sp.GetRequiredService<IInputContext>();
            var eventBus = sp.GetService<IEventBus>();
            var canvas = eventBus != null
                ? new UICanvas(input, eventBus)
                : new UICanvas(input);
            canvas.ScreenSize = new Vector2(w, h);
            return canvas;
        });
    }

    /// <summary>
    ///     Adds core ECS infrastructure.
    ///     Each scene gets its own <see cref="IEntityWorld"/> instance (scoped, one per scene lifetime).
    /// </summary>
    private static void AddECSCore(IServiceCollection services)
    {
        // IActivator wraps ActivatorUtilities so EntityWorld doesn't hold a direct IServiceProvider.
        services.TryAddScoped<IActivator>(sp => new ServiceProviderActivator(sp));
        services.TryAddScoped<IEntityWorld, EntityWorld>();
    }
}