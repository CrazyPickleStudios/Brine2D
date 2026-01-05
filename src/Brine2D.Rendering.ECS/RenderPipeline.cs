using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace Brine2D.Rendering.ECS;

/// <summary>
/// Manages and executes render systems in order.
/// Lives in Brine2D.Rendering.ECS because it depends on IRenderer.
/// </summary>
public class RenderPipeline
{
    private readonly List<IRenderSystem> _systems = new();
    private readonly ILogger<RenderPipeline>? _logger;
    private bool _isSorted;

    public IReadOnlyList<IRenderSystem> Systems => _systems.AsReadOnly();

    public RenderPipeline(ILogger<RenderPipeline>? logger = null)
    {
        _logger = logger;
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
            try
            {
                system.Render(renderer);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "Error executing render system: {SystemName}", system.Name);
            }
        }
    }
}