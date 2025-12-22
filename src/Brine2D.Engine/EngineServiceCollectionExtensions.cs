using Brine2D.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brine2D.Engine;

/// <summary>
/// Extension methods for registering engine services.
/// </summary>
public static class EngineServiceCollectionExtensions
{
    /// <summary>
    /// Adds core Brine2D engine services to the service collection.
    /// </summary>
    public static IServiceCollection AddBrineEngine(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        // Register core engine services
        services.TryAddSingleton<IGameEngine, GameEngine>();
        services.TryAddSingleton<IGameLoop, GameLoop>();
        services.TryAddSingleton<IGameContext, GameContext>();
        services.TryAddSingleton<ISceneManager, SceneManager>();

        return services;
    }

    /// <summary>
    /// Registers a scene in the service collection.
    /// </summary>
    public static IServiceCollection AddScene<TScene>(this IServiceCollection services)
        where TScene : class, IScene
    {
        services.TryAddTransient<TScene>();
        return services;
    }
}
