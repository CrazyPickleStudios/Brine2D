using Brine2D.Events;
using Brine2D.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Brine2D;

/// <summary>
/// Extension methods for registering SDL3 event pump services.
/// </summary>
public static class SDL3EventPumpServiceCollectionExtensions
{
    /// <summary>
    /// Adds the SDL3 event pump and its internal event bus to the service collection.
    /// </summary>
    public static IServiceCollection AddSDL3EventPump(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Internal event bus scoped to SDL3 internals (key/mouse/gamepad routing).
        // Keyed so it never collides with the public IEventBus singleton.
        // Typed as IEventBus — SDL3EventPump only calls Publish(), which is on the interface.
        services.TryAddKeyedSingleton<IEventBus>("SDL_Internal", (sp, _) =>
            new EventBus(sp.GetService<ILogger<EventBus>>()));

        services.TryAddSingleton<SDL3EventPump>(sp => new SDL3EventPump(
            sp.GetRequiredService<ILogger<SDL3EventPump>>(),
            sp.GetRequiredService<IEventBus>(),
            sp.GetRequiredKeyedService<IEventBus>("SDL_Internal"),
            sp.GetRequiredService<IHostApplicationLifetime>()
        ));

        services.TryAddSingleton<IEventPump>(sp =>
            sp.GetRequiredService<SDL3EventPump>());

        return services;
    }
}