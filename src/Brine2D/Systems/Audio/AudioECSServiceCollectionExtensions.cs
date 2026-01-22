using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brine2D.Systems.Audio;

/// <summary>
/// Extension methods for registering ECS audio systems.
/// </summary>
public static class AudioECSServiceCollectionExtensions
{
    /// <summary>
    /// Adds ECS audio systems (bridge between ECS and Audio).
    /// </summary>
    public static IServiceCollection AddECSAudio(this IServiceCollection services)
    {
        services.TryAddSingleton<AudioSystem>();
        
        return services;
    }
}