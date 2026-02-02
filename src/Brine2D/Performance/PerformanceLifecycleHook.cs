using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Engine;

namespace Brine2D.Performance;

/// <summary>
/// Lifecycle hook that tracks performance metrics automatically.
/// Integrates with the scene lifecycle to measure frame times.
/// </summary>
public class PerformanceLifecycleHook : ISceneLifecycleHook
{
    private readonly PerformanceMonitor _monitor;
    
    public int Order => int.MinValue; // Run first to capture full frame time
    
    public PerformanceLifecycleHook(PerformanceMonitor monitor)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
    }
    
    public void PreUpdate(GameTime gameTime, IEntityWorld world)
    {
        // Begin frame timing at the very start
        _monitor.BeginFrame();
    }
    
    public void PostUpdate(GameTime gameTime, IEntityWorld world)
    {
        // Nothing needed here
    }
    
    public void PreRender(GameTime gameTime, IEntityWorld world)
    {
        // Nothing needed here
    }
    
    public void PostRender(GameTime gameTime, IEntityWorld world)
    {
        // End frame timing at the very end
        _monitor.EndFrame();
    }
}