using Brine2D.Common;
using Brine2D.Events;
using Brine2D.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Brine2D;

/// <summary>
/// Extension methods for registering SDL3 input services.
/// </summary>
public static class SDL3InputServiceCollectionExtensions
{
    /// <summary>
    /// Adds SDL3 input services.
    /// </summary>
    public static IServiceCollection AddSDL3Input(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IInputContext>(sp => new InputContext(
            sp.GetRequiredService<ILogger<InputContext>>(),
            sp.GetRequiredService<IEventBus>(),                        // Public event bus
            sp.GetRequiredKeyedService<IEventBus>("SDL_Internal"),     // Internal SDL event bus
            sp.GetService<ISDL3WindowProvider>()                       // Window provider from renderer
        ));

        services.TryAddSingleton<InputLayerManager>();

        return services;
    }
}
