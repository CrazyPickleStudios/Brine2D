using System;
using Brine2D.Rendering.PostProcessing;
using Brine2D.Rendering.SDL.PostProcessing;
using Brine2D.Rendering.SDL.PostProcessing.Effects;
using Brine2D.SDL.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering.SDL;

public static class PostProcessingServiceCollectionExtensions
{
    /// <summary>
    /// Adds SDL3 post-processing support to the rendering pipeline.
    /// Call this after AddSDL3Rendering() to enable post-process effects.
    /// </summary>
    public static IServiceCollection AddPostProcessing(this IServiceCollection services, Action<PostProcessingOptions>? configure = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Configure options
        if (configure != null)
        {
            services.Configure(configure);
        }

        // Register SDL3-specific pipeline as singleton
        services.TryAddSingleton<SDL3PostProcessPipeline>();

        // Also register as base type for generic access
        services.TryAddSingleton<PostProcessPipeline>(sp => sp.GetRequiredService<SDL3PostProcessPipeline>());

        return services;
    }

    /// <summary>
    /// Add a pass-through effect to the pipeline for testing.
    /// This effect does nothing but copy source to target.
    /// </summary>
    public static IServiceCollection AddPassThroughEffect(this IServiceCollection services, int width = 1280, int height = 720)
    {
        services.AddSingleton<IPostProcessEffect>(provider =>
        {
            var logger = provider.GetService<ILogger<PassThroughEffect>>();
            var effect = new PassThroughEffect(width, height, logger);
            
            // Auto-register with pipeline if available
            var pipeline = provider.GetService<SDL3PostProcessPipeline>();
            pipeline?.AddEffect(effect);
            
            return effect;
        });

        return services;
    }

    /// <summary>
    /// Add a grayscale effect to the pipeline.
    /// Converts the rendered image to black and white using luminance calculation.
    /// </summary>
    public static IServiceCollection AddGrayscaleEffect(this IServiceCollection services, int width = 1280, int height = 720, float intensity = 1.0f)
    {
        // Register the effect itself as a singleton
        services.TryAddSingleton<GrayscaleEffect>(provider =>
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var logger = provider.GetService<ILogger<GrayscaleEffect>>();
            var renderer = provider.GetRequiredService<IRenderer>();
            
            // Get the GPU device handle from renderer
            if (renderer is not SDL3GPURenderer gpuRenderer)
            {
                throw new InvalidOperationException("Grayscale effect requires SDL3GPURenderer");
            }

            return new GrayscaleEffect(gpuRenderer.Device, width, height, loggerFactory, logger)
            {
                Intensity = intensity
            };
    });

        return services;
    }

    /// <summary>
    /// Add a blur effect to the pipeline.
    /// Performs two-pass Gaussian blur (horizontal + vertical).
    /// </summary>
    public static IServiceCollection AddBlurEffect(this IServiceCollection services, int width = 1280, int height = 720, float blurRadius = 2.0f)
    {
        services.AddSingleton<IPostProcessEffect>(provider =>
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var logger = provider.GetService<ILogger<BlurEffect>>();
            var renderer = provider.GetRequiredService<IRenderer>();

            if (renderer is not SDL3GPURenderer gpuRenderer)
            {
                throw new InvalidOperationException("Blur effect requires SDL3GPURenderer");
            }

            return new BlurEffect(gpuRenderer.Device, width, height, loggerFactory, logger)
            {
                BlurRadius = blurRadius
            };
        });

        return services;
    }
}