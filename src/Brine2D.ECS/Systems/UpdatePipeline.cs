using Brine2D.Core;
using Microsoft.Extensions.Logging;

namespace Brine2D.ECS.Systems;

/// <summary>
/// Manages and executes update systems in order.
/// Pure ECS - no rendering dependencies.
/// </summary>
public class UpdatePipeline
{
    private readonly List<IUpdateSystem> _systems = new();
    private readonly ILogger<UpdatePipeline>? _logger;
    private bool _isSorted;

    public IReadOnlyList<IUpdateSystem> Systems => _systems.AsReadOnly();

    public UpdatePipeline(ILogger<UpdatePipeline>? logger = null)
    {
        _logger = logger;
    }

    public UpdatePipeline AddSystem(IUpdateSystem system)
    {
        _systems.Add(system);
        _isSorted = false;
        _logger?.LogDebug("Added update system: {SystemName} (order: {Order})", system.Name, system.UpdateOrder);
        return this;
    }

    public bool RemoveSystem(IUpdateSystem system) => _systems.Remove(system);

    public void Clear()
    {
        _systems.Clear();
        _isSorted = false;
    }

    public void Execute(GameTime gameTime)
    {
        if (!_isSorted)
        {
            _systems.Sort((a, b) => a.UpdateOrder.CompareTo(b.UpdateOrder));
            _isSorted = true;
            
            _logger?.LogDebug("Update system execution order:");
            foreach (var system in _systems)
            {
                _logger?.LogDebug("  {Order}: {SystemName}", system.UpdateOrder, system.Name);
            }
        }

        foreach (var system in _systems)
        {
            try
            {
                system.Update(gameTime);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing update system: {SystemName}", system.Name);
            }
        }
    }
}