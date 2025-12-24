using Microsoft.Extensions.DependencyInjection;

namespace Brine2D.Core.Collision;

/// <summary>
/// Extension methods for registering collision services.
/// </summary>
public static class CollisionServiceCollectionExtensions
{
    /// <summary>
    /// Adds collision system to the service collection.
    /// </summary>
    public static IServiceCollection AddCollisionSystem(this IServiceCollection services)
    {
        // Scoped - one per scene/request
        services.AddScoped<CollisionSystem>();
        
        return services;
    }
}