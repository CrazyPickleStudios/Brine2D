using Brine2D.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace Brine2D.Performance;

public static class PerformanceServiceCollectionExtensions
{
    /// <summary>
    /// Adds performance monitoring services.
    /// Includes frame time tracking, FPS, memory, and system profiling.
    /// </summary>
    public static IServiceCollection AddPerformanceMonitoring(this IServiceCollection services)
    {
        services.AddSingleton<PerformanceOverlay>();
        services.AddSingleton<PerformanceMonitor>();
        services.AddSingleton<ScopedProfiler>();

        services.AddSingleton<ISceneLifecycleHook, PerformanceLifecycleHook>();

        return services;
    }
}