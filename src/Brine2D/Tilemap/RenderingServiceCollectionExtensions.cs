using Brine2D.Tilemap;
using Microsoft.Extensions.DependencyInjection;

namespace Brine2D.Rendering;

public static class RenderingServiceCollectionExtensions
{
    public static IServiceCollection AddTilemapRenderer(this IServiceCollection services)
    {
        services.AddTransient<TilemapRenderer>();
        
        return services;
    }
}