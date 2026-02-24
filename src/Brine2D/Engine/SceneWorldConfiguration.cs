using Brine2D.ECS;

namespace Brine2D.Engine;

/// <summary>
/// Carries the per-scene world configuration delegate registered via
/// <see cref="Hosting.GameApplicationBuilder.ConfigureScene"/>.
/// Applied by <see cref="SceneManager"/> after default systems are added,
/// allowing project-wide system configuration without subclassing the manager.
/// </summary>
internal sealed class SceneWorldConfiguration
{
    private readonly Action<IEntityWorld>? _configure;

    internal SceneWorldConfiguration(Action<IEntityWorld>? configure)
    {
        _configure = configure;
    }

    internal void Apply(IEntityWorld world) => _configure?.Invoke(world);
}