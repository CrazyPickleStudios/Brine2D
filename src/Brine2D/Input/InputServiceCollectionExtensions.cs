using Microsoft.Extensions.DependencyInjection;

namespace Brine2D.Input;

public static class InputServiceCollectionExtensions
{
    public static IServiceCollection AddInputLayerManager(this IServiceCollection services)
    {
        services.AddSingleton<InputLayerManager>();
        return services;
    }
}