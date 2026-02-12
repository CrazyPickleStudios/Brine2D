using Brine2D.Hosting;
using Brine2D.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine;

/// <summary>
/// Extension methods for registering engine services.
/// </summary>
public static class EngineServiceCollectionExtensions
{
    /// <summary>
    /// Adds core Brine2D engine services to the service collection.
    /// </summary>
    internal static IServiceCollection AddBrineEngine(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // Register core engine services
        services.TryAddSingleton<GameEngine>();
        
        services.TryAddSingleton<GameLoop>(sp => new GameLoop(
            sp.GetRequiredService<ILogger<GameLoop>>(),
            sp.GetRequiredService<ILoggerFactory>(),
            sp.GetRequiredService<IGameContext>(),
            sp.GetRequiredService<ISceneManager>(),
            sp.GetRequiredService<IInputContext>(),
            sp.GetRequiredService<IHostApplicationLifetime>(),
            sp.GetService<InputLayerManager>(),
            sp.GetService<IEventPump>()
        ));
        
        services.TryAddSingleton<IGameContext, GameContext>();
        services.TryAddSingleton<ISceneManager, SceneManager>();

        return services;
    }

    /// <summary>
    /// Registers a scene in the service collection.
    /// </summary>
    /// <typeparam name="TScene">The scene type to register.</typeparam>
    /// <param name="services">The service collection.</param>
    /// <returns>The service collection for chaining.</returns>
    /// <remarks>
    /// Scenes are registered as transient services, meaning a new instance
    /// is created each time the scene is loaded. This ensures clean state
    /// between scene transitions.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddScene&lt;MenuScene&gt;();
    /// builder.Services.AddScene&lt;GameScene&gt;();
    /// builder.Services.AddScene&lt;SettingsScene&gt;();
    /// </code>
    /// </example>
    public static IServiceCollection AddScene<TScene>(this IServiceCollection services)
        where TScene : Scene
    {
        services.TryAddTransient<TScene>();
        return services;
    }
}
