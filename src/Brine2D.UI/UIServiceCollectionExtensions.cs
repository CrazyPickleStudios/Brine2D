using Microsoft.Extensions.DependencyInjection;

namespace Brine2D.UI;

/// <summary>
/// Extension methods for registering UI services.
/// </summary>
public static class UIServiceCollectionExtensions
{
    /// <summary>
    /// Adds UI canvas to the service collection.
    /// </summary>
    public static IServiceCollection AddUICanvas(this IServiceCollection services)
    {
        services.AddScoped<UICanvas>();
        
        return services;
    }
}