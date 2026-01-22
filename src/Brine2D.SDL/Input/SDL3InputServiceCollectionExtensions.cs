using Brine2D.Events;
using Brine2D.Input;
using Brine2D.SDL.Common;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Brine2D.SDL.Input;

public static class SDL3InputServiceCollectionExtensions
{
    public static IServiceCollection AddSDL3Input(this IServiceCollection services)
    {
        services.TryAddSingleton<IInputService>(sp => new SDL3InputService(
            sp.GetRequiredService<ILogger<SDL3InputService>>(),
            sp.GetRequiredService<EventBus>(),                       // Public event bus
            sp.GetRequiredKeyedService<EventBus>("SDL_Internal"),    // Internal event bus
            sp.GetService<ISDL3WindowProvider>()                     // Window provider (optional)
        ));

        return services;
    }
}