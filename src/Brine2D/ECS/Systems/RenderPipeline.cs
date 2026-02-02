using Brine2D.Performance;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace Brine2D.ECS.Systems;

/// <summary>
/// Manages and executes render systems in order.
/// </summary>
public class RenderPipeline
{
    private readonly List<IRenderSystem> _systems = new();
    private readonly ILogger<RenderPipeline>? _logger;
    private readonly ScopedProfiler? _profiler;
    private bool _isSorted;
    private readonly HashSet<string> _disabledSystems = new();
    
    private readonly List<IRenderSystem> _systemsToAdd = new();
    private readonly List<IRenderSystem> _systemsToRemove = new();
    private bool _isExecuting = false;

    public IReadOnlyList<IRenderSystem> Systems => _systems.AsReadOnly();

    public RenderPipeline(ILogger<RenderPipeline>? logger = null, ScopedProfiler? profiler = null)
    {
        _logger = logger;
        _profiler = profiler;
    }

    public RenderPipeline AddSystem(IRenderSystem system)
    {
        if (_isExecuting)
        {
            _systemsToAdd.Add(system);
            _logger?.LogDebug("Deferred render system addition: {SystemType}", system.GetType().Name);
        }
        else
        {
            _systems.Add(system);
            _isSorted = false;
            _logger?.LogDebug("Added render system: {SystemType} (order: {Order})", 
                system.GetType().Name, system.RenderOrder);
        }
        
        return this;
    }

    public bool RemoveSystem(IRenderSystem system)
    {
        if (_isExecuting)
        {
            _systemsToRemove.Add(system);
            _logger?.LogDebug("Deferred render system removal: {SystemType}", system.GetType().Name);
            return true;
        }
        else
        {
            return _systems.Remove(system);
        }
    }

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

    public void Execute(IRenderer renderer, IEntityWorld world)
    {
        if (!_isSorted)
        {
            _systems.Sort((a, b) => a.RenderOrder.CompareTo(b.RenderOrder));
            _isSorted = true;
        }

        _isExecuting = true;

        try
        {
            ApplyDeferredModifications();

            foreach (var system in _systems)
            {
                var systemName = system.GetType().Name;
                
                if (_disabledSystems.Contains(systemName))
                {
                    continue;
                }
                
                try
                {
                    if (_profiler != null)
                    {
                        using (_profiler.BeginScope($"Render/{systemName}"))
                        {
                            system.Render(renderer, world);
                        }
                    }
                    else
                    {
                        system.Render(renderer, world);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error executing render system: {SystemType}", systemName);
                }
            }
        }
        finally
        {
            _isExecuting = false;
            ApplyDeferredModifications();
        }
    }

    private void ApplyDeferredModifications()
    {
        if (_systemsToAdd.Count > 0)
        {
            foreach (var system in _systemsToAdd)
            {
                _systems.Add(system);
                _isSorted = false;
                _logger?.LogDebug("Applied deferred addition: {SystemType} (order: {Order})", 
                    system.GetType().Name, system.RenderOrder);
            }
            _systemsToAdd.Clear();
        }

        if (_systemsToRemove.Count > 0)
        {
            foreach (var system in _systemsToRemove)
            {
                _systems.Remove(system);
                _logger?.LogDebug("Applied deferred removal: {SystemType}", system.GetType().Name);
            }
            _systemsToRemove.Clear();
        }
    }
}