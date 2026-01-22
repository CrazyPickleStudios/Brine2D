using Brine2D.Performance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Brine2D.Rendering;
using Brine2D.Engine;
using Brine2D.ECS.Systems;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// Extension methods for registering ECS rendering systems.
/// </summary>
public static class RenderingECSServiceCollectionExtensions
{
    /// <summary>
    /// Adds ECS rendering systems (bridge between ECS and Rendering).
    /// Automatically creates a main camera using the window dimensions from RenderingOptions.
    /// To use a custom camera, register ICamera before calling this method.
    /// </summary>
    public static IServiceCollection AddECSRendering(this IServiceCollection services)
    {
        // Register camera manager
        services.TryAddSingleton<ICameraManager, CameraManager>();

        // Create main camera using window dimensions from RenderingOptions
        // TryAddSingleton won't replace if user already registered ICamera
        services.TryAddSingleton<ICamera>(sp =>
        {
            var cameraManager = sp.GetRequiredService<ICameraManager>();
            
            // Get window dimensions from rendering configuration
            var renderingOptions = sp.GetRequiredService<IOptions<RenderingOptions>>().Value;
            
            // Create camera that matches the actual window size
            var mainCamera = new Camera2D(renderingOptions.WindowWidth, renderingOptions.WindowHeight);
            cameraManager.RegisterCamera("main", mainCamera);
            cameraManager.MainCamera = mainCamera;
            
            return mainCamera;
        });

        // Register rendering systems
        services.TryAddSingleton<SpriteRenderingSystem>();
        services.TryAddSingleton<CameraSystem>();
        services.TryAddSingleton<ParticleSystem>();
        services.TryAddSingleton<DebugRenderer>();
        
        // Add RenderPipeline
        services.AddSingleton(sp =>
        {
            var logger = sp.GetService<ILogger<RenderPipeline>>();
            var profiler = sp.GetService<ScopedProfiler>();
            return new RenderPipeline(logger, profiler);
        });
        
        // Register lifecycle hook for automatic render pipeline execution
        services.AddSingleton<ISceneLifecycleHook, ECSRenderHook>();
        
        return services;
    }
}