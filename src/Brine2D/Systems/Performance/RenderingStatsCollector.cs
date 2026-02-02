using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Performance;
using Brine2D.Systems.Rendering;
using Brine2D.Engine;

namespace Brine2D.Systems.Performance;

/// <summary>
/// Collects rendering statistics from ECS systems and updates the performance monitor.
/// </summary>
public class RenderingStatsCollector : ISceneLifecycleHook
{
    private readonly PerformanceMonitor _monitor;
    private readonly SpriteRenderingSystem? _spriteSystem;
    
    public int Order => int.MaxValue; // Run last to collect final stats
    
    public RenderingStatsCollector(
        PerformanceMonitor monitor,
        SpriteRenderingSystem? spriteSystem = null)
    {
        _monitor = monitor;
        _spriteSystem = spriteSystem;
    }
    
    public void PreUpdate(GameTime gameTime, IEntityWorld world) 
    { 
        // Nothing needed
    }
    
    public void PostUpdate(GameTime gameTime, IEntityWorld world) 
    { 
        // Nothing needed
    }
    
    public void PreRender(GameTime gameTime, IEntityWorld world) 
    { 
        // Nothing needed
    }
    
    public void PostRender(GameTime gameTime, IEntityWorld world)
    {
        var entityCount = world.Entities.Count();
        
        int renderedSprites = 0;
        int totalSprites = 0;
        int culledSprites = 0;
        int batchCount = 0;
        
        if (_spriteSystem != null)
        {
            totalSprites = _spriteSystem.GetTotalSpriteCount();
            var (rendered, batches) = _spriteSystem.GetBatchStats();
            renderedSprites = rendered;
            batchCount = batches;
            culledSprites = totalSprites - renderedSprites;
        }
        
        // Update monitor
        _monitor.UpdateRenderStats(
            drawCalls: batchCount,
            entityCount: entityCount,
            spriteCount: renderedSprites,
            culledSprites: culledSprites,
            batchCount: batchCount);
    }
}