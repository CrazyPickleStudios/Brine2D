using Brine2D.Core;
using Brine2D.Performance;
using Microsoft.Extensions.Logging;
using System.Reflection;

namespace Brine2D.ECS.Systems;

/// <summary>
/// Manages and executes update systems in order.
/// Pure ECS - no rendering dependencies.
/// </summary>
public class UpdatePipeline
{
    private readonly List<IUpdateSystem> _systems = new();
    private readonly ILogger<UpdatePipeline>? _logger;
    private readonly ScopedProfiler? _profiler;
    private readonly ECSOptions _options;
    private bool _isSorted;
    private readonly HashSet<string> _disabledSystems = new();
    
    private readonly List<IUpdateSystem> _systemsToAdd = new();
    private readonly List<IUpdateSystem> _systemsToRemove = new();
    private bool _isExecuting = false;

    public IReadOnlyList<IUpdateSystem> Systems => _systems.AsReadOnly();

    public UpdatePipeline(ILogger<UpdatePipeline>? logger = null, ScopedProfiler? profiler = null, ECSOptions? options = null)
    {
        _logger = logger;
        _profiler = profiler;
        _options = options ?? new ECSOptions();
    }

    public UpdatePipeline AddSystem(IUpdateSystem system)
    {
        if (_isExecuting)
        {
            _systemsToAdd.Add(system);
            _logger?.LogDebug("Deferred update system addition: {SystemName}", system.Name);
        }
        else
        {
            _systems.Add(system);
            _isSorted = false;
            
            // Log if system is marked sequential
            var isSequential = system.GetType().GetCustomAttribute<SequentialAttribute>() != null;
            _logger?.LogDebug("Added update system: {SystemName} (order: {Order}, parallel: {Parallel})", 
                system.Name, system.UpdateOrder, !isSequential && _options.EnableMultiThreading);
        }
        
        return this;
    }

    public bool RemoveSystem(IUpdateSystem system)
    {
        if (_isExecuting)
        {
            _systemsToRemove.Add(system);
            _logger?.LogDebug("Deferred update system removal: {SystemName}", system.Name);
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

    /// <summary>
    /// Disables systems by name for this pipeline.
    /// Disabled systems are skipped during Execute().
    /// </summary>
    public void DisableSystems(IEnumerable<string> systemNames)
    {
        foreach (var name in systemNames)
        {
            _disabledSystems.Add(name);
        }
    }

    /// <summary>
    /// Re-enables all systems.
    /// </summary>
    public void EnableAllSystems()
    {
        _disabledSystems.Clear();
    }

    public void Execute(GameTime gameTime, IEntityWorld world)
    {
        if (!_isSorted)
        {
            _systems.Sort((a, b) => a.UpdateOrder.CompareTo(b.UpdateOrder));
            _isSorted = true;
        }

        _isExecuting = true;

        try
        {
            ApplyDeferredModifications();

            foreach (var system in _systems)
            {
                // Skip disabled systems
                if (_disabledSystems.Contains(system.Name))
                {
                    continue;
                }
                
                try
                {
                    // Profile system execution
                    if (_profiler != null)
                    {
                        using (_profiler.BeginScope($"Update/{system.Name}"))
                        {
                            system.Update(gameTime, world);
                        }
                    }
                    else
                    {
                        system.Update(gameTime, world);
                    }
                }
                catch (Exception ex)
                {
                    _logger?.LogError(ex, "Error executing update system: {SystemName}", system.Name);
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
        // Process additions
        if (_systemsToAdd.Count > 0)
        {
            foreach (var system in _systemsToAdd)
            {
                _systems.Add(system);
                _isSorted = false;
                
                // Log if system is marked sequential
                var isSequential = system.GetType().GetCustomAttribute<SequentialAttribute>() != null;
                _logger?.LogDebug("Applied deferred addition: {SystemName} (order: {Order}, parallel: {Parallel})", 
                    system.Name, system.UpdateOrder, !isSequential && _options.EnableMultiThreading);
            }
            _systemsToAdd.Clear();
        }

        // Process removals
        if (_systemsToRemove.Count > 0)
        {
            foreach (var system in _systemsToRemove)
            {
                _systems.Remove(system);
                _logger?.LogDebug("Applied deferred removal: {SystemName}", system.Name);
            }
            _systemsToRemove.Clear();
        }
    }
}