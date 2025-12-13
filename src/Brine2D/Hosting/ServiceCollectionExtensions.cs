using Brine2D.Content;
using Brine2D.Engine;
using Brine2D.Graphics.Tilemaps;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brine2D.Hosting;

/// <summary>
///     Provides extension methods for <see cref="IServiceCollection" /> to register Brine2D core services.
/// </summary>
/// <remarks>
///     This class is intended to group service registration helpers for the Brine2D framework.
/// </remarks>
public static class ServiceCollectionExtensions
{
    /// <summary>
    ///     Registers the Brine2D core services into the specified <see cref="IServiceCollection" />.
    /// </summary>
    /// <param name="services">The service collection to which Brine2D services will be added.</param>
    /// <returns>The same <see cref="IServiceCollection" /> instance to allow for fluent configuration.</returns>
    /// <remarks>
    ///     Currently a no-op placeholder.
    /// </remarks>
    public static IServiceCollection AddBrine2DCore(this IServiceCollection services)
    {
        services.AddSingleton<ISceneManager, SceneManager>();
        services.AddSingleton<IAssetLoader<Tilemap>, TilemapLoader>();

        return services;
    }
}