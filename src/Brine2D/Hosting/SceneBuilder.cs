using Brine2D.Engine;

namespace Brine2D.Hosting;

/// <summary>
/// Fluent builder for registering scenes.
/// </summary>
/// <remarks>
/// Scene registration is optional. Scenes can be loaded without explicit registration,
/// but registering scenes is recommended for:
/// - Self-documentation (shows available scenes in Program.cs)
/// - Startup validation (ensures scenes can be resolved)
/// - Slightly better performance (DI container caches type info)
/// </remarks>
public sealed class SceneBuilder
{
    private readonly GameApplicationBuilder _appBuilder;

    internal SceneBuilder(GameApplicationBuilder appBuilder)
    {
        _appBuilder = appBuilder ?? throw new ArgumentNullException(nameof(appBuilder));
    }

    /// <summary>
    /// Registers a scene, including startup-time dependency validation.
    /// </summary>
    public SceneBuilder Add<TScene>() where TScene : Scene
    {
        _appBuilder.AddScene<TScene>();
        return this;
    }

    /// <summary>
    /// Registers multiple scenes by type.
    /// </summary>
    public SceneBuilder AddRange(params ReadOnlySpan<Type> sceneTypes)
    {
        foreach (var sceneType in sceneTypes)
        {
            ArgumentNullException.ThrowIfNull(sceneType, nameof(sceneTypes));
            if (!typeof(Scene).IsAssignableFrom(sceneType))
                throw new ArgumentException($"Type {sceneType.Name} does not inherit from Scene", nameof(sceneTypes));
        }

        foreach (var sceneType in sceneTypes)
            _appBuilder.AddScene(sceneType, skipTypeCheck: true);

        return this;
    }
}