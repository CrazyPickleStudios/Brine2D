using Brine2D.Core.Performance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brine2D.Rendering.Performance;

/// <summary>
/// Extension methods for registering performance overlay (rendering).
/// </summary>
public static class PerformanceRenderingExtensions
{
    /// <summary>
    /// Adds performance overlay rendering.
    /// Requires AddPerformanceMonitoring() to be called first.
    /// </summary>
    public static IServiceCollection AddPerformanceOverlay(this IServiceCollection services)
    {
        services.TryAddSingleton<PerformanceOverlay>();
        return services;
    }
}