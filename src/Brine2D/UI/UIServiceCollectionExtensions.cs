using Brine2D.Events;
using Brine2D.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brine2D.UI;

/// <summary>
/// Extension methods for registering UI services.
/// </summary>
public static class UIServiceCollectionExtensions
{
    /// <summary>
    /// Adds <see cref="UICanvas"/> to the service collection. Kept for backward compatibility;
    /// <see cref="UICanvas"/> is now registered by default via <c>AddBrine2D()</c>, so calling
    /// this is only necessary when <c>AddBrine2D()</c> was not used. Uses <see cref="IServiceCollectionServiceExtensions.TryAddScoped{TService}(IServiceCollection, Func{IServiceProvider, TService})"/>
    /// so it never overrides an existing registration. Window-resize tracking is NOT automatic —
    /// opt in explicitly via <see cref="UICanvas.AutoResizeToWindow"/>.
    /// </summary>
    public static IServiceCollection AddUICanvas(this IServiceCollection services)
    {
        services.TryAddScoped<UICanvas>(sp =>
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
