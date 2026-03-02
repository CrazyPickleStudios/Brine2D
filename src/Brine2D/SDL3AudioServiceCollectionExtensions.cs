using Brine2D.Audio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brine2D;

/// <summary>
/// Extension methods for registering SDL3 audio services.
/// </summary>
public static class SDL3AudioServiceCollectionExtensions
{
    /// <summary>
    /// Adds SDL3 audio services.
    /// </summary>
    public static IServiceCollection AddSDL3Audio(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IAudioService, AudioService>();

        return services;
    }
}
