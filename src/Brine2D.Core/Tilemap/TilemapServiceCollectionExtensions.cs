using Microsoft.Extensions.DependencyInjection;

namespace Brine2D.Core.Tilemap;

/// <summary>
/// Extension methods for registering tilemap services.
/// </summary>
public static class TilemapServiceCollectionExtensions
{
    /// <summary>
    /// Adds tilemap loading services to the service collection.
    /// </summary>
    public static IServiceCollection AddTilemapServices(this IServiceCollection services)
    {
        services.AddSingleton<ITilemapLoader, TmjLoader>();
        
        return services;
    }
}