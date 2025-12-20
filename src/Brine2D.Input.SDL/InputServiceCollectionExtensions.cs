using Brine2D.Core.Input;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brine2D.Input.SDL;

public static class InputServiceCollectionExtensions
{
    public static IServiceCollection AddSDL3Input(this IServiceCollection services)
    {
        if (services == null) throw new ArgumentNullException(nameof(services));

        services.TryAddSingleton<IInputService, SDL3InputService>();

        return services;
    }
}