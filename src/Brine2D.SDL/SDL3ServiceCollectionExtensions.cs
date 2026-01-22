using Brine2D.Events;
using Brine2D.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Brine2D.SDL.Common;

public static class SDL3ServiceCollectionExtensions
{
    public static IServiceCollection AddSDL3ApplicationLifetime(this IServiceCollection services)
    {
        // Register internal event bus (keyed service for SDL internals)
        services.AddKeyedSingleton<EventBus>("SDL_Internal", (sp, _) =>
            new EventBus(sp.GetService<ILogger<EventBus>>()));
        
        // Register SDL3EventPump
        services.TryAddSingleton<SDL3EventPump>(sp => new SDL3EventPump(
            sp.GetRequiredService<ILogger<SDL3EventPump>>(),
            sp.GetRequiredService<EventBus>(),                 
            sp.GetRequiredKeyedService<EventBus>("SDL_Internal")     
        ));
        
        // Register as IApplicationLifetime
        services.TryAddSingleton<IHostApplicationLifetime>(sp => 
            sp.GetRequiredService<SDL3EventPump>());
        
        services.TryAddSingleton<IEventPump>(sp => 
            sp.GetRequiredService<SDL3EventPump>());
        
        return services;
    }
}