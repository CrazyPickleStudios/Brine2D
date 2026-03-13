using Brine2D.Common;
using Brine2D.Rendering;
using Brine2D.Rendering.SDL.PostProcessing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

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

        // Hosted service: SDL.Init() runs in StartAsync (before any SDL usage).
        // SDL.Quit() runs in Dispose() (LIFO, after all SDL-dependent singletons).
        services.TryAddEnumerable(ServiceDescriptor.Singleton<IHostedService, SDL3Lifecycle>());

        services.TryAddSingleton<IFontLoader, SDL3FontLoader>();
        services.TryAddSingleton<IShaderLoader, SDL3ShaderLoader>();

        // SDL3Renderer's optional ctor parameters use default values; the DI container
        // injects registered services or falls back to null for unregistered ones.
        services.TryAddSingleton<IRenderer, SDL3Renderer>();

        services.TryAddSingleton<ISDL3WindowProvider>(sp =>
            (ISDL3WindowProvider)sp.GetRequiredService<IRenderer>());

        services.TryAddSingleton<ITextureContext>(sp =>
            (ITextureContext)sp.GetRequiredService<IRenderer>());

        services.TryAddSingleton<ITextureLoader, SDL3TextureLoader>();

        return services;
    }
}