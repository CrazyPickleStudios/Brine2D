using Brine2D.Events;
using Brine2D.Input;
using Brine2D.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Brine2D;

public static class SDL3InputServiceCollectionExtensions
{
    public static IServiceCollection AddSDL3Input(this IServiceCollection services)
    {
        // Register InputContext with proper dependencies
        services.TryAddSingleton<IInputContext>(sp => new InputContext(
            sp.GetRequiredService<ILogger<InputContext>>(),
            sp.GetRequiredService<EventBus>(),                    // Public event bus
            sp.GetRequiredKeyedService<EventBus>("SDL_Internal"), // Internal SDL event bus
            sp.GetService<ISDL3WindowProvider>()                  // Window provider from renderer
        ));
        
        services.TryAddSingleton<InputLayerManager>();
        
        return services;
    }
}   
