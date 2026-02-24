using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brine2D.Engine;

/// <summary>
/// Extension methods for registering scene services.
/// </summary>
public static class EngineServiceCollectionExtensions
{
    /// <summary>
    /// Registers a scene in the service collection as a transient service.
    /// A new instance is created each time the scene is loaded, ensuring clean state.
    /// </summary>
    /// <remarks>
    /// Prefer <see cref="Hosting.GameApplicationBuilder.AddScene{T}"/> for startup-time
    /// dependency validation. Use this overload for dynamic registration scenarios.
    /// </remarks>
    public static IServiceCollection AddScene<TScene>(this IServiceCollection services)
        where TScene : Scene
    {
        services.TryAddTransient<TScene>();
        return services;
    }
}