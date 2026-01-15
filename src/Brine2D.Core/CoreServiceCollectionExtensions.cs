using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Brine2D.Core;

public static class CoreServiceCollectionExtensions
{
    public static IServiceCollection AddBrineCore(this IServiceCollection services)
    {
        // Register public event bus
        services.TryAddSingleton<EventBus>(sp => 
            new EventBus(sp.GetService<ILogger<EventBus>>()));
        
        return services;
    }
}