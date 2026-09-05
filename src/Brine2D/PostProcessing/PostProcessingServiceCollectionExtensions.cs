using System;
using Brine2D.Rendering.PostProcessing;
using Brine2D.Rendering.SDL.PostProcessing;
using Brine2D.Rendering.SDL.PostProcessing.Effects;
using Brine2D.Rendering;
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

        if (configure != null)
        {
            services.AddSingleton<Action<PostProcessingOptions>>(configure);
        }

        services.TryAddSingleton(sp =>
        {
            var options = new PostProcessingOptions();
            foreach (var action in sp.GetServices<Action<PostProcessingOptions>>())
            {
                action(options);
            }
            return options;
        });

        services.TryAddSingleton<SDL3PostProcessPipeline>();
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
            return new PassThroughEffect(width, height, logger);
        });

        return services;
    }

    /// <summary>
    /// Add a grayscale effect to the pipeline.
    /// Converts the rendered image to black and white using luminance calculation.
    /// </summary>
    public static IServiceCollection AddGrayscaleEffect(this IServiceCollection services, int width = 1280, int height = 720, float intensity = 1.0f)
    {
        services.AddSingleton<IPostProcessEffect>(provider =>
        {
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var logger = provider.GetService<ILogger<GrayscaleEffect>>();
            var renderer = provider.GetRequiredService<IRenderer>();
            
            if (renderer is not SDL3Renderer gpuRenderer)
            {
                throw new InvalidOperationException("Grayscale effect requires SDL3GPURenderer");
            }

            var format = ResolveColorTargetFormat(provider);

            return new GrayscaleEffect(gpuRenderer.GpuDevice!, width, height, format, loggerFactory, logger)
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

            if (renderer is not SDL3Renderer gpuRenderer)
            {
                throw new InvalidOperationException("Blur effect requires SDL3GPURenderer");
            }

            var format = ResolveColorTargetFormat(provider);

            return new BlurEffect(gpuRenderer.GpuDevice!, width, height, format, loggerFactory, logger)
            {
                BlurRadius = blurRadius
            };
        });

        return services;
    }

    /// <summary>
    /// Adds a fixed-aspect-ratio/letterbox effect to the pipeline. The main render target
    /// (and all game-world rendering) is fixed at <paramref name="designWidth"/> x
    /// <paramref name="designHeight"/> regardless of window size; this effect scales that
    /// output to fit the actual window, centered, filling the remainder with
    /// <paramref name="barColor"/>. Always runs last in the pipeline.
    /// </summary>
    /// <remarks>
    /// Requires <see cref="AddPostProcessing"/> to have been called first. When this effect
    /// is registered, the render target manager sizes the main/ping-pong render targets to
    /// the design resolution instead of the live viewport, so window resizes no longer
    /// recreate them.
    /// </remarks>
    public static IServiceCollection AddLetterbox(this IServiceCollection services, int designWidth, int designHeight, Brine2D.Core.Color? barColor = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        services.AddSingleton<IPostProcessEffect>(provider =>
        {
            var logger = provider.GetService<ILogger<LetterboxEffect>>();
            var effect = new LetterboxEffect(designWidth, designHeight, logger);
            if (barColor.HasValue)
                effect.BarColor = barColor.Value;
            return effect;
        });

        services.TryAddSingleton(new Brine2D.Rendering.SDL.PostProcessing.LetterboxDesignResolution(designWidth, designHeight));

        return services;
    }

    private static SDL3.SDL.GPUTextureFormat ResolveColorTargetFormat(IServiceProvider provider)
    {
        var options = provider.GetService<PostProcessingOptions>();
        if (options?.RenderTargetFormat is { } explicitFormat)
            return explicitFormat;

        var renderer = provider.GetRequiredService<IRenderer>();
        if (renderer is SDL3Renderer gpuRenderer)
            return gpuRenderer.SwapchainFormat;

        return SDL3.SDL.GPUTextureFormat.B8G8R8A8Unorm;
    }
}