using Brine2D.Core;

namespace Brine2D.Core.Performance;

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
    
    public void PreUpdate(GameTime gameTime)
    {
        // Begin frame timing at the very start
        _monitor.BeginFrame();
    }
    
    public void PostUpdate(GameTime gameTime)
    {
        // Nothing needed here
    }
    
    public void PreRender(GameTime gameTime)
    {
        // Nothing needed here
    }
    
    public void PostRender(GameTime gameTime)
    {
        // End frame timing at the very end
        _monitor.EndFrame();
    }
}