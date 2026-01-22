using Brine2D.Rendering.TextureAtlas;
using Brine2D.Rendering.SDL.TextureAtlas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;

namespace Brine2D.Rendering.SDL;

/// <summary>
/// Extension methods for registering texture atlas services.
/// </summary>
public static class TextureAtlasServiceExtensions
{
    /// <summary>
    /// Adds texture atlas services to the dependency injection container.
    /// Call this after AddSDL3Rendering() to enable texture atlasing.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="configureOptions">Optional configuration for atlas options.</param>
    /// <returns>The service collection for chaining.</returns>
    public static IServiceCollection AddTextureAtlasing(
        this IServiceCollection services,
        Action<TextureAtlasOptions>? configureOptions = null)
    {
        // Register options
        if (configureOptions != null)
        {
            services.Configure(configureOptions);
        }
        
        // Register TextureAtlasOptions as a service for direct injection
        services.TryAddSingleton(sp => sp.GetRequiredService<IOptions<TextureAtlasOptions>>().Value);

        // Register atlas builder as transient (each build creates new atlas)
        services.TryAddTransient<ITextureAtlasBuilder, TextureAtlasBuilder>();

        return services;
    }
}