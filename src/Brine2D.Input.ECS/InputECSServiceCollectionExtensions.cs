using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brine2D.Input.ECS;

/// <summary>
/// Extension methods for registering ECS input systems.
/// </summary>
public static class InputECSServiceCollectionExtensions
{
    /// <summary>
    /// Adds ECS input systems (bridge between ECS and Input).
    /// </summary>
    public static IServiceCollection AddECSInput(this IServiceCollection services)
    {
        // Register input systems
        services.TryAddSingleton<PlayerControllerSystem>();
        
        return services;
    }
}