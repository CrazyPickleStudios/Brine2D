using Brine2D.Common;
using Brine2D.Events;
using Brine2D.Rendering;
using Brine2D.Rendering.SDL.PostProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Brine2D;

/// <summary>
/// Extension methods for registering SDL3 rendering services.
/// </summary>
public static class SDL3RenderingServiceCollectionExtensions
{
    /// <summary>
    /// Adds SDL3 rendering services.
    /// </summary>
    public static IServiceCollection AddSDL3Rendering(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.TryAddSingleton<IFontLoader, SDL3FontLoader>();
        services.TryAddSingleton<IShaderLoader, SDL3ShaderLoader>();

        services.TryAddSingleton<IRenderer>(provider =>
        {
            var renderingOptions = provider.GetRequiredService<RenderingOptions>();
            var windowOptions = provider.GetRequiredService<WindowOptions>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var fontLoader = provider.GetService<IFontLoader>();
            var eventBus = provider.GetService<IEventBus>();
            var postProcessingOptions = provider.GetService<PostProcessingOptions>();
            var postProcessPipeline = provider.GetService<SDL3PostProcessPipeline>();

            return new SDL3Renderer(
                provider.GetRequiredService<ILogger<SDL3Renderer>>(),
                loggerFactory,
                renderingOptions,
                windowOptions,
                postProcessingOptions,
                postProcessPipeline,
                fontLoader,
                eventBus);
        });

        // Resolved by casting the already-registered IRenderer singleton — no separate instance.
        services.TryAddSingleton<ISDL3WindowProvider>(sp =>
            (ISDL3WindowProvider)sp.GetRequiredService<IRenderer>());

        services.TryAddSingleton<ITextureContext>(sp =>
            (ITextureContext)sp.GetRequiredService<IRenderer>());

        services.TryAddSingleton<ITextureLoader, SDL3TextureLoader>();

        return services;
    }
}