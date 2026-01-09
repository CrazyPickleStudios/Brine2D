using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.ObjectPool;

namespace Brine2D.Core.Pooling;

/// <summary>
/// Extension methods for registering object pooling services.
/// Called automatically by AddObjectECS() - users don't need to call this directly.
/// </summary>
public static class PoolingServiceCollectionExtensions
{
    /// <summary>
    /// Adds object pooling infrastructure to the service collection.
    /// Registers the ObjectPoolProvider singleton.
    /// </summary>
    public static IServiceCollection AddObjectPooling(this IServiceCollection services)
    {
        // Register the default ObjectPoolProvider singleton
        services.TryAddSingleton<ObjectPoolProvider, DefaultObjectPoolProvider>();
        
        return services;
    }
}