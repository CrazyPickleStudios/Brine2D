using Brine2D.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brine2D.Hosting;

/// <summary>
/// Extension methods for <see cref="GameApplicationBuilder"/>.
/// </summary>
public static class GameApplicationBuilderExtensions
{
    /// <summary>
    /// Registers a scene directly on the builder (shortcut for builder.Services.AddScene).
    /// </summary>
    /// <typeparam name="TScene">The scene type to register.</typeparam>
    /// <param name="builder">The game application builder.</param>
    /// <returns>The builder for chaining.</returns>
    public static GameApplicationBuilder AddScene<TScene>(this GameApplicationBuilder builder)
        where TScene : Scene
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));

        builder.Services.AddScene<TScene>();
        return builder;
    }

    /// <summary>
    /// Configures scenes using a fluent builder.
    /// </summary>
    /// <param name="builder">The game application builder.</param>
    /// <param name="configure">Action to configure scenes.</param>
    /// <returns>The builder for chaining.</returns>
    /// <example>
    /// <code>
    /// builder.AddScenes(scenes => scenes
    ///     .Add&lt;MenuScene&gt;()
    ///     .Add&lt;GameScene&gt;()
    ///     .Add&lt;SettingsScene&gt;());
    /// </code>
    /// </example>
    public static GameApplicationBuilder AddScenes(
        this GameApplicationBuilder builder,
        Action<SceneBuilder> configure)
    {
        if (builder == null)
            throw new ArgumentNullException(nameof(builder));
        if (configure == null)
            throw new ArgumentNullException(nameof(configure));

        var sceneBuilder = new SceneBuilder(builder.Services);
        configure(sceneBuilder);

        return builder;
    }
}