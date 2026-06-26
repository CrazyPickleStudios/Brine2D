using Brine2D.Events;
using Brine2D.Input;
using Microsoft.Extensions.DependencyInjection;

namespace Brine2D.UI;

/// <summary>
/// Extension methods for registering UI services.
/// </summary>
public static class UIServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="UICanvas"/> to the service collection.
    /// When <see cref="IEventBus"/> is registered, the canvas subscribes to
    /// <see cref="WindowResizedEvent"/> automatically so <see cref="UICanvas.ScreenSize"/>
    /// stays in sync without any manual wiring.
    /// </summary>
    public static IServiceCollection AddUICanvas(this IServiceCollection services)
    {
        services.AddScoped<UICanvas>(sp =>
        {
            var input = sp.GetRequiredService<IInputContext>();
            var eventBus = sp.GetService<IEventBus>();
            return eventBus != null
                ? new UICanvas(input, eventBus)
                : new UICanvas(input);
        });

        return services;
    }
}