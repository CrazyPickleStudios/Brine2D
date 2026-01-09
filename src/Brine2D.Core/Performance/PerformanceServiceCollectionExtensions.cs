using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brine2D.Core.Performance;

/// <summary>
/// Extension methods for registering performance monitoring services.
/// </summary>
public static class PerformanceServiceCollectionExtensions
{
    /// <summary>
    /// Adds performance monitoring services to the application.
    /// Automatically tracks FPS, frame time via lifecycle hooks.
    /// </summary>
    public static IServiceCollection AddPerformanceMonitoring(this IServiceCollection services)
    {
        // Register monitor
        services.TryAddSingleton<PerformanceMonitor>();
        
        // Register lifecycle hook for automatic frame timing
        services.AddSingleton<ISceneLifecycleHook, PerformanceLifecycleHook>();
        
        return services;
    }
}