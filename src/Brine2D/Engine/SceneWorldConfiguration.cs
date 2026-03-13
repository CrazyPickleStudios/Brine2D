using System.Collections.Frozen;
using Brine2D.ECS;

namespace Brine2D.Engine;

/// <summary>
/// Carries the per-scene world configuration delegate registered via
/// <see cref="Hosting.GameApplicationBuilder.ConfigureScene"/>, the set of default systems
/// excluded via <see cref="Hosting.GameApplicationBuilder.ExcludeDefaultSystem{T}"/>, and any
/// additional default systems added via <see cref="Hosting.GameApplicationBuilder.AddDefaultSystem{T}()"/>.
/// Applied by <see cref="SceneManager"/> after the built-in system inclusion check.
/// </summary>
internal sealed class SceneWorldConfiguration
{
    private readonly Action<IEntityWorld>? _configure;
    private readonly FrozenSet<Type> _excludedSystems;
    private readonly (Type Type, Action<IEntityWorld> Register)[] _additionalSystems;

    internal SceneWorldConfiguration(
        Action<IEntityWorld>? configure,
        FrozenSet<Type>? excludedSystems = null,
        (Type Type, Action<IEntityWorld> Register)[]? additionalSystems = null)
    {
        _configure = configure;
        _excludedSystems = excludedSystems ?? FrozenSet.Create<Type>();
        _additionalSystems = additionalSystems ?? [];
    }

    internal bool IsExcluded(Type systemType) => _excludedSystems.Contains(systemType);

    internal void Apply(IEntityWorld world)
    {
        foreach (var (type, register) in _additionalSystems)
        {
            if (!IsExcluded(type))
                register(world);
        }
        _configure?.Invoke(world);
    }
}