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
        if (services == null) throw new ArgumentNullException(nameof(services));

        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }

        // Register renderer based on backend option
        services.TryAddSingleton<IRenderer>(provider =>
        {
            var options = provider.GetRequiredService<IOptions<RenderingOptions>>();

            return options.Value.Backend switch
            {
                GraphicsBackend.GPU => new SDL3GPURenderer(
                    provider.GetRequiredService<ILogger<SDL3GPURenderer>>(),
                    provider.GetRequiredService<ILoggerFactory>(),
                    options),
                GraphicsBackend.LegacyRenderer => new SDL3Renderer(
                    provider.GetRequiredService<ILogger<SDL3Renderer>>(),
                    options),
                GraphicsBackend.Auto => new SDL3GPURenderer(
                    provider.GetRequiredService<ILogger<SDL3GPURenderer>>(),
                    provider.GetRequiredService<ILoggerFactory>(),
                    options),
                _ => throw new NotSupportedException($"Backend {options.Value.Backend} not supported")
            };
        });

        // Register texture loader
        services.TryAddSingleton<ITextureLoader>(provider =>
        {
            var renderer = provider.GetRequiredService<IRenderer>();

            // Get the internal SDL renderer handle
            nint rendererHandle = renderer switch
            {
                SDL3Renderer legacyRenderer => legacyRenderer.RendererHandle,
                SDL3GPURenderer gpuRenderer => throw new NotSupportedException("Texture loading not yet supported for GPU renderer"),
                _ => throw new NotSupportedException($"Unknown renderer type: {renderer.GetType()}")
            };

            return new SDL3TextureLoader(
                provider.GetRequiredService<ILogger<SDL3TextureLoader>>(),
                provider.GetRequiredService<ILoggerFactory>(),
                rendererHandle);
        });

        return services;
    }
}