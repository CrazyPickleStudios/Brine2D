using Brine2D.Events;
using Brine2D.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Brine2D;

public static class SDL3EventPumpServiceCollectionExtensions
{
    public static IServiceCollection AddSDL3EventPump(this IServiceCollection services)
    {
        // Register internal event bus (for SDL internals)
        services.AddKeyedSingleton<EventBus>("SDL_Internal", (sp, _) =>
            new EventBus(sp.GetService<ILogger<EventBus>>()));
        
        // Register SDL3EventPump
        services.TryAddSingleton<SDL3EventPump>(sp => new SDL3EventPump(
            sp.GetRequiredService<ILogger<SDL3EventPump>>(),
            sp.GetRequiredService<EventBus>(),
            sp.GetRequiredKeyedService<EventBus>("SDL_Internal"),
            sp.GetRequiredService<IHostApplicationLifetime>()
        ));
        
        // Register as IEventPump
        services.TryAddSingleton<IEventPump>(sp => 
            sp.GetRequiredService<SDL3EventPump>());
        
        return services;
    }
}