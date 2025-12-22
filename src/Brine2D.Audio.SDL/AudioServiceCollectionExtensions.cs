using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brine2D.Audio.SDL;

public static class AudioServiceCollectionExtensions
{
    /// <summary>
    /// Adds SDL3_mixer audio services.
    /// </summary>
    public static IServiceCollection AddSDL3Audio(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.TryAddSingleton<IAudioService, SDL3AudioService>();

        return services;
    }
}