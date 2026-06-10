using Microsoft.Extensions.DependencyInjection;

namespace Brine2D.Tilemap;

public static class TilemapServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ITilemapLoader"/>, <see cref="TilemapAnimator"/>, and
    /// <see cref="TilemapSystem"/>. Add <c>TilemapSystem</c> to a scene world via
    /// <c>world.AddSystem&lt;TilemapSystem&gt;()</c>.
    /// </summary>
    public static IServiceCollection AddTilemapServices(this IServiceCollection services)
    {
        services.AddSingleton<ITilemapLoader, TmjLoader>();
        services.AddTransient<TilemapAnimator>();
        services.AddTransient<TilemapSystem>();

        return services;
    }
}
