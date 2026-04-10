using Brine2D.Rendering.TextureAtlas;
using Brine2D.Rendering.SDL.TextureAtlas;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

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
        services.TryAddSingleton(_ =>
        {
            var options = new TextureAtlasOptions();
            configureOptions?.Invoke(options);
            return options;
        });

        // Register atlas builder as transient (each build creates new atlas)
        services.TryAddTransient<ITextureAtlasBuilder, TextureAtlasBuilder>();

        return services;
    }
}