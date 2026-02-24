using Brine2D.Audio;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brine2D;

public static class SDL3AudioServiceCollectionExtensions
{
    public static IServiceCollection AddSDL3Audio(this IServiceCollection services)
    {
        services.TryAddSingleton<AudioService>();
        
        return services;
    }
}
