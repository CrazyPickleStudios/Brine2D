using Brine2D.Audio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brine2D.SDL.Audio;

/// <summary>
///     Provides extension methods for configuring SDL3 audio services in an <see cref="IServiceCollection"/>.
/// </summary>
public static class AudioServiceCollectionExtensions
{
    /// <summary>
    ///     Adds SDL3_mixer audio services to the service collection.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/> so that additional calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    /// <remarks>
    ///     This method registers the SDL3-based implementation of <see cref="IAudioService"/> as a singleton.
    ///     If an audio service is already registered, this method will not replace it.
    /// </remarks>
    public static IServiceCollection AddSDL3Audio(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IAudioService, SDL3AudioService>();

        return services;
    }
}