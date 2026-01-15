using Brine2D.SDL.Common;
using Brine2D.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Brine2D.Rendering.SDL;

public static class SDL3RenderingServiceCollectionExtensions
{
    public static IServiceCollection AddSDL3Rendering(
        this IServiceCollection services,
        Action<RenderingOptions>? configureOptions = null)
    {
        if (services == null)
        {
            throw new ArgumentNullException(nameof(services));
        }

        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        services.TryAddSingleton<IFontLoader, SDL3FontLoader>();

        // Register renderer
        services.TryAddSingleton<IRenderer>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<RenderingOptions>>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
            var fontLoader = provider.GetService<IFontLoader>();  // Get font loader
            var eventBus = provider.GetService<EventBus>();  // Get public EventBus

            return options.Value.Backend switch
            {
                GraphicsBackend.GPU => new SDL3GPURenderer(
                    provider.GetRequiredService<ILogger<SDL3GPURenderer>>(),
                    loggerFactory,
                    options,
                    fontLoader,
                    eventBus), 
                GraphicsBackend.LegacyRenderer => new SDL3Renderer(
                    provider.GetRequiredService<ILogger<SDL3Renderer>>(),
                    loggerFactory,
                    options,
                    fontLoader,
                    eventBus),
                GraphicsBackend.Auto => new SDL3GPURenderer(
                    provider.GetRequiredService<ILogger<SDL3GPURenderer>>(),
                    loggerFactory,
                    options,
                    fontLoader,
                    eventBus), 
                _ => throw new NotSupportedException($"Backend {options.Value.Backend} not supported")
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