using Brine2D.SDL.Common;
using Brine2D.Rendering.SDL.PostProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Brine2D.Events;
using Brine2D.SDL.Rendering;

namespace Brine2D.Rendering.SDL;

public static class SDL3RenderingServiceCollectionExtensions
{
    // Remove the configureOptions parameter - configuration now comes from Brine2DOptions
    public static IServiceCollection AddSDL3Rendering(this IServiceCollection services)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        services.TryAddSingleton<IFontLoader, SDL3FontLoader>();

        // Register renderer
        services.TryAddSingleton<IRenderer>(provider =>
        {
            var renderingOptions = provider.GetRequiredService<IOptions<RenderingOptions>>();
            var windowOptions = provider.GetRequiredService<IOptions<WindowOptions>>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var logger = loggerFactory.CreateLogger("SDL3Rendering");
            var fontLoader = provider.GetService<IFontLoader>();
            var eventBus = provider.GetService<EventBus>();
            var postProcessingOptions = provider.GetService<IOptions<PostProcessingOptions>>();
            var postProcessPipeline = provider.GetService<SDL3PostProcessPipeline>();

            // Warn if post-processing is enabled with legacy renderer
            if (postProcessingOptions?.Value?.Enabled == true && 
                renderingOptions.Value.Backend == GraphicsBackend.LegacyRenderer)
            {
                logger.LogWarning("Post-processing is not supported with LegacyRenderer backend.");
            }

            return renderingOptions.Value.Backend switch
            {
                GraphicsBackend.GPU => new SDL3GPURenderer(
                    provider.GetRequiredService<ILogger<SDL3GPURenderer>>(),
                    loggerFactory,
                    renderingOptions,
                    windowOptions,
                    postProcessingOptions,
                    postProcessPipeline,
                    fontLoader,
                    eventBus), 
                GraphicsBackend.LegacyRenderer => new SDL3Renderer(
                    provider.GetRequiredService<ILogger<SDL3Renderer>>(),
                    loggerFactory,
                    renderingOptions,
                    windowOptions,
                    fontLoader,
                    eventBus),
                GraphicsBackend.Auto => new SDL3GPURenderer(
                    provider.GetRequiredService<ILogger<SDL3GPURenderer>>(),
                    loggerFactory,
                    renderingOptions,
                    windowOptions,
                    postProcessingOptions,
                    postProcessPipeline,
                    fontLoader,
                    eventBus), 
                _ => throw new NotSupportedException($"Backend {renderingOptions.Value.Backend} not supported")
            };
        });

        services.AddSingleton<ISDL3WindowProvider>(sp =>
            (ISDL3WindowProvider)sp.GetRequiredService<IRenderer>());

        // Register texture context from renderer (both renderers implement ITextureContext)
        services.TryAddSingleton<ITextureContext>(provider => 
            (ITextureContext)provider.GetRequiredService<IRenderer>());

        // Simple, clean texture loader registration - no type checking, no Func<> gymnastics!
        services.TryAddSingleton<ITextureLoader, SDL3TextureLoader>();

        return services;
    }
}