using Brine2D.Core.Tilemap;
using Microsoft.Extensions.DependencyInjection;

namespace Brine2D.Rendering;

/// <summary>
/// Extension methods for registering rendering services.
/// </summary>
public static class RenderingServiceCollectionExtensions
{
    /// <summary>
    /// Adds tilemap renderer to the service collection.
    /// </summary>
    public static IServiceCollection AddTilemapRenderer(this IServiceCollection services)
    {
        services.AddTransient<TilemapRenderer>();
        
        return services;
    }
}