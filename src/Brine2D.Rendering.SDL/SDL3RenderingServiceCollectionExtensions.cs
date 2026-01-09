using Brine2D.SDL.Common;
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

        services.TryAddSingleton<IRenderer>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<RenderingOptions>>();
            var logger = provider.GetRequiredService<ILogger<SDL3Renderer>>();
            var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

            var fontLoader = provider.GetService<IFontLoader>();

            return options.Value.Backend switch
            {
                GraphicsBackend.GPU => new SDL3GPURenderer(
                    provider.GetRequiredService<ILogger<SDL3GPURenderer>>(),
                    loggerFactory,
                    options),
                GraphicsBackend.LegacyRenderer => new SDL3Renderer(
                    logger,
                    loggerFactory,
                    options,
                    fontLoader),
                GraphicsBackend.Auto => new SDL3GPURenderer(
                    provider.GetRequiredService<ILogger<SDL3GPURenderer>>(),
                    loggerFactory,
                    options),
                _ => throw new NotSupportedException($"Backend {options.Value.Backend} not supported")
            };
        });

        services.AddSingleton<ISDL3WindowProvider>(sp =>
            (SDL3Renderer)sp.GetRequiredService<IRenderer>());

        services.TryAddSingleton<ITextureLoader>(provider =>
        {
            return new SDL3TextureLoader(
                provider.GetRequiredService<ILogger<SDL3TextureLoader>>(),
                provider.GetRequiredService<ILoggerFactory>(),
                () =>
                {
                    // Get renderer FRESH each time (inside the lambda)
                    var renderer = provider.GetRequiredService<IRenderer>();
                    return renderer switch
                    {
                        SDL3Renderer legacyRenderer => legacyRenderer.RendererHandle,
                        SDL3GPURenderer gpuRenderer => throw new NotSupportedException(
                            "Texture loading not yet supported for GPU renderer"),
                        _ => throw new NotSupportedException($"Unknown renderer type: {renderer.GetType()}")
                    };
                });
        });

        return services;
    }
}