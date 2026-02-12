using Brine2D.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brine2D.Hosting;

/// <summary>
/// Builder for configuring scenes.
/// </summary>
public class SceneBuilder
{
    private readonly IServiceCollection _services;

    internal SceneBuilder(IServiceCollection services)
    {
        _services = services ?? throw new ArgumentNullException(nameof(services));
    }

    /// <summary>
    /// Registers a scene in the service collection.
    /// </summary>
    /// <typeparam name="TScene">The scene type to register.</typeparam>
    /// <returns>The scene builder for chaining.</returns>
    /// <remarks>
    /// Scenes are registered as transient services, meaning a new instance
    /// is created each time the scene is loaded.
    /// </remarks>
    public SceneBuilder Add<TScene>() where TScene : Scene
    {
        _services.TryAddTransient<TScene>();
        return this;
    }

    /// <summary>
    /// Registers multiple scenes by type.
    /// </summary>
    /// <param name="sceneTypes">Scene types to register.</param>
    /// <returns>The scene builder for chaining.</returns>
    public SceneBuilder AddRange(params Type[] sceneTypes)
    {
        foreach (var sceneType in sceneTypes)
        {
            if (!typeof(Scene).IsAssignableFrom(sceneType))
            {
                throw new ArgumentException(
                    $"Type {sceneType.Name} does not implement IScene",
                    nameof(sceneTypes));
            }

            _services.TryAdd(new ServiceDescriptor(sceneType, sceneType, ServiceLifetime.Transient));
        }

        return this;
    }
}