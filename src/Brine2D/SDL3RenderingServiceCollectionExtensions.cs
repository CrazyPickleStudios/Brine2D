using Brine2D.Common;
using Brine2D.Events;
using Brine2D.Rendering.SDL.PostProcessing;
using Brine2D.Common;
using Brine2D.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering.SDL;

public static class SDL3RenderingServiceCollectionExtensions
{
    /// <summary>
    /// Adds SDL3 rendering services.
    /// </summary>
    public static IServiceCollection AddSDL3Rendering(this IServiceCollection services)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Register font loader
        services.TryAddSingleton<IFontLoader, SDL3FontLoader>();

        // Register shader loader
        services.TryAddSingleton<IShaderLoader, SDL3ShaderLoader>();

        // Register renderer
        services.TryAddSingleton<IRenderer>(provider =>
        {
            var renderingOptions = provider.GetRequiredService<RenderingOptions>();
            var windowOptions = provider.GetRequiredService<WindowOptions>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var fontLoader = provider.GetService<IFontLoader>();
            var eventBus = provider.GetService<EventBus>();
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

        // Register window provider from renderer
        services.AddSingleton<ISDL3WindowProvider>(sp =>
            (ISDL3WindowProvider)sp.GetRequiredService<IRenderer>());

        // Register texture context from renderer
        services.TryAddSingleton<ITextureContext>(provider =>
            (ITextureContext)provider.GetRequiredService<IRenderer>());

        // Register texture loader
        services.TryAddSingleton<ITextureLoader, SDL3TextureLoader>();

        return services;
    }
}