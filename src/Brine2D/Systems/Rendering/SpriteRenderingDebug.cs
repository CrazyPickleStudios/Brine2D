using Brine2D.Core;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace Brine2D.Systems.Rendering;

/// <summary>
/// Debug overlay for sprite rendering performance statistics.
/// Shows batching efficiency and culling effectiveness.
/// </summary>
public static class SpriteRenderingDebug
{
    /// <summary>
    /// Draws sprite rendering performance stats on screen.
    /// </summary>
    public static void DrawStats(
        IRenderer renderer, 
        SpriteRenderingSystem spriteSystem,
        int totalEntities,
        int x = 10, 
        int y = 10)
    {
        var (spriteCount, drawCalls) = spriteSystem.GetBatchStats();
        var culled = totalEntities - spriteCount;
        var batchingEfficiency = drawCalls > 0 ? (float)spriteCount / drawCalls : 0;

        renderer.DrawText($"Sprites: {spriteCount} / {totalEntities}", x, y, Color.White);
        renderer.DrawText($"Draw Calls: {drawCalls}", x, y + 20, Color.Yellow);
        renderer.DrawText($"Culled: {culled}", x, y + 40, Color.Green);
        renderer.DrawText($"Batch Efficiency: {batchingEfficiency:F1}x", x, y + 60, 
            batchingEfficiency > 5 ? Color.Green : Color.Yellow);
    }
}