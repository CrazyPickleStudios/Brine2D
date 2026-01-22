using Brine2D.ECS.Systems;
using Brine2D.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine.Systems;

/// <summary>
/// Manages scene-specific system configuration.
/// Tracks systems added/disabled for a particular scene.
/// </summary>
internal class SceneSystemConfigurator : ISystemConfigurator
{
    private readonly IServiceProvider _services;
    private readonly ILogger<SceneSystemConfigurator>? _logger;
    private readonly List<object> _sceneSystems = new();
    private readonly HashSet<string> _disabledSystemNames = new();

    public IReadOnlyList<object> SceneSystems => _sceneSystems;
    public IReadOnlySet<string> DisabledSystemNames => _disabledSystemNames;

    public SceneSystemConfigurator(IServiceProvider services, ILogger<SceneSystemConfigurator>? logger = null)
    {
        _services = services;
        _logger = logger;
    }

    public void AddUpdateSystem<T>() where T : class, IUpdateSystem
    {
        try
        {
            var system = ActivatorUtilities.CreateInstance<T>(_services);
            _sceneSystems.Add(system);
            _logger?.LogDebug("Added scene-specific update system: {SystemName}", system.Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create scene-specific update system: {SystemType}", typeof(T).Name);
            throw;
        }
    }

    public void AddRenderSystem<T>() where T : class, IRenderSystem
    {
        try
        {
            var system = ActivatorUtilities.CreateInstance<T>(_services);
            _sceneSystems.Add(system);
            _logger?.LogDebug("Added scene-specific render system: {SystemName}", system.GetType().Name);
        }
        catch (Exception ex)
        {
            _logger?.LogError(ex, "Failed to create scene-specific render system: {SystemType}", typeof(T).Name);
            throw;
        }
    }

    public void DisableSystem<T>() where T : class, IUpdateSystem
    {
        var systemName = typeof(T).Name;
        _disabledSystemNames.Add(systemName);
        _logger?.LogDebug("Disabled system for this scene: {SystemName}", systemName);
    }

    public void DisableSystem(string systemName)
    {
        _disabledSystemNames.Add(systemName);
        _logger?.LogDebug("Disabled system for this scene: {SystemName}", systemName);
    }
}