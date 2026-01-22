using Brine2D.Performance;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace Brine2D.ECS.Systems;

/// <summary>
/// Manages and executes render systems in order.
/// Lives in Brine2D.Rendering.ECS because it depends on IRenderer.
/// </summary>
public class RenderPipeline
{
    private readonly List<IRenderSystem> _systems = new();
    private readonly ILogger<RenderPipeline>? _logger;
    private readonly ScopedProfiler? _profiler;
    private bool _isSorted;

    private readonly HashSet<string> _disabledSystems = new();

    public IReadOnlyList<IRenderSystem> Systems => _systems.AsReadOnly();

    public RenderPipeline(ILogger<RenderPipeline>? logger = null, ScopedProfiler? profiler = null)
    {
        _logger = logger;
        _profiler = profiler;
    }

    public RenderPipeline AddSystem(IRenderSystem system)
    {
        _systems.Add(system);
        _isSorted = false;
        _logger?.LogDebug("Added render system: {SystemName} (order: {Order})", system.Name, system.RenderOrder);
        return this;
    }

    public bool RemoveSystem(IRenderSystem system) => _systems.Remove(system);

    public void Clear()
    {
        _systems.Clear();
        _isSorted = false;
    }

    public void DisableSystems(IEnumerable<string> systemNames)
    {
        foreach (var name in systemNames)
        {
            _disabledSystems.Add(name);
        }
    }

    public void EnableAllSystems()
    {
        _disabledSystems.Clear();
    }

    public void Execute(IRenderer renderer)
    {
        if (!_isSorted)
        {
            _systems.Sort((a, b) => a.RenderOrder.CompareTo(b.RenderOrder));
            _isSorted = true;
            
            _logger?.LogDebug("Render system execution order:");
            foreach (var system in _systems)
            {
                _logger?.LogDebug("  {Order}: {SystemName}", system.RenderOrder, system.Name);
            }
        }

        foreach (var system in _systems)
        {
            // Skip disabled systems
            if (_disabledSystems.Contains(system.Name))
                continue;

            try
            {
                // Profile system execution
                if (_profiler != null)
                {
                    using (_profiler.BeginScope($"Render/{system.Name}"))
                    {
                        system.Render(renderer);
                    }
                }
                else
                {
                    system.Render(renderer);
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing render system: {SystemName}", system.Name);
            }
        }
    }
}