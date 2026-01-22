using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brine2D.Performance;

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
        // Explicitly register with both dependencies
        services.TryAddSingleton<PerformanceOverlay>(sp =>
        {
            var monitor = sp.GetRequiredService<PerformanceMonitor>();
            var profiler = sp.GetService<ScopedProfiler>(); // Optional
            return new PerformanceOverlay(monitor, profiler);
        });
        return services;
    }
}