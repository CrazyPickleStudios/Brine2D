using Brine2D.Core.Performance;
using System.Diagnostics;

namespace Brine2D.Rendering.Performance;

/// <summary>
/// Renders a performance statistics overlay on screen.
/// Shows FPS, frame time, draw calls, and other metrics.
/// Renders in screen space (UI layer) so it doesn't move with the camera.
/// </summary>
public class PerformanceOverlay
{
    private readonly PerformanceMonitor _monitor;
    private readonly Stopwatch _updateTimer = new();
    private bool _isVisible = true;
    private bool _showDetailedStats = false;
    private OverlayPosition _position = OverlayPosition.TopRight;
    
    // Display update throttling
    private double _displayUpdateInterval = 0.25;
    private double _displayedFPS = 0;
    private double _displayedFrameTime = 0;
    private double _displayedMinFPS = 0;
    private double _displayedMaxFPS = 0;
    private double _displayedAvgFPS = 0;
    private int _displayedDrawCalls = 0;
    private int _displayedEntityCount = 0;
    private int _displayedSpriteCount = 0;
    private int _displayedCulledSprites = 0;
    private int _displayedBatchCount = 0;
    private float _displayedBatchEfficiency = 0;
    private double _displayedMemoryMB = 0;
    private int _displayedGen0 = 0;
    private int _displayedGen1 = 0;
    private int _displayedGen2 = 0;
    
    /// <summary>
    /// Gets or sets whether the overlay is visible.
    /// </summary>
    public bool IsVisible
    {
        get => _isVisible;
        set => _isVisible = value;
    }
    
    /// <summary>
    /// Gets or sets whether to show detailed statistics.
    /// </summary>
    public bool ShowDetailedStats
    {
        get => _showDetailedStats;
        set => _showDetailedStats = value;
    }
    
    /// <summary>
    /// Gets or sets the overlay position on screen.
    /// </summary>
    public OverlayPosition Position
    {
        get => _position;
        set => _position = value;
    }
    
    /// <summary>
    /// Gets or sets the display update interval in seconds.
    /// Default is 0.25 (4 updates per second).
    /// Lower values update more frequently but use more CPU for text rendering.
    /// </summary>
    public double DisplayUpdateInterval
    {
        get => _displayUpdateInterval;
        set => _displayUpdateInterval = Math.Max(0.016, value);
    }
    
    public PerformanceOverlay(PerformanceMonitor monitor)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _updateTimer.Start();
    }
    
    /// <summary>
    /// Renders the performance overlay.
    /// </summary>
    /// <param name="renderer">The renderer to use.</param>
    /// <param name="screenWidth">Screen width for right-aligned positioning (default: 1280).</param>
    /// <param name="screenHeight">Screen height (default: 720).</param>
    public void Render(IRenderer renderer, int screenWidth = 1280, int screenHeight = 720)
    {
        if (!_isVisible)
            return;
        
        // Update displayed values at fixed interval
        if (_updateTimer.Elapsed.TotalSeconds >= _displayUpdateInterval)
        {
            UpdateDisplayedValues();
            _updateTimer.Restart();
        }
        
        // CRITICAL: Temporarily disable camera for UI rendering
        var previousCamera = renderer.Camera;
        renderer.Camera = null; // Render in screen space!
        
        try
        {
            RenderOverlay(renderer, screenWidth, screenHeight);
        }
        finally
        {
            // Restore camera
            renderer.Camera = previousCamera;
        }
    }
    
    private void RenderOverlay(IRenderer renderer, int screenWidth, int screenHeight)
    {
        var lineHeight = 20;
        var bgWidth = _showDetailedStats ? 350 : 250;
        var bgHeight = _showDetailedStats ? 280 : 120;
        
        // Calculate position
        int x, y;
        switch (_position)
        {
            case OverlayPosition.TopRight:
                x = screenWidth - bgWidth - 10;
                y = 10;
                break;
            case OverlayPosition.TopLeft:
                x = 10;
                y = 10;
                break;
            case OverlayPosition.BottomLeft:
                x = 10;
                y = screenHeight - bgHeight - 10;
                break;
            case OverlayPosition.BottomRight:
                x = screenWidth - bgWidth - 10;
                y = screenHeight - bgHeight - 10;
                break;
            default:
                x = 10;
                y = 10;
                break;
        }
        
        var currentY = y;
        
        // Draw semi-transparent background
        renderer.DrawRectangleFilled(x - 5, y - 5, bgWidth, bgHeight, new Color(0, 0, 0, 180));
        
        // FPS Stats
        var fpsColor = GetFpsColor(_displayedFPS);
        renderer.DrawText($"FPS: {_displayedFPS:F1}", x, currentY, fpsColor);
        currentY += lineHeight;
        
        renderer.DrawText($"Frame: {_displayedFrameTime:F2}ms", x, currentY, Color.White);
        currentY += lineHeight;
        
        if (_showDetailedStats)
        {
            renderer.DrawText($"Min/Max/Avg: {_displayedMinFPS:F0} / {_displayedMaxFPS:F0} / {_displayedAvgFPS:F1}", 
                x, currentY, new Color(180, 180, 180));
            currentY += lineHeight;
            
            currentY += 5;
            
            renderer.DrawText("=== Rendering ===", x, currentY, new Color(255, 255, 100));
            currentY += lineHeight;
            
            renderer.DrawText($"Draw Calls: {_displayedDrawCalls}", x, currentY, Color.White);
            currentY += lineHeight;
            
            renderer.DrawText($"Entities: {_displayedEntityCount}", x, currentY, Color.White);
            currentY += lineHeight;
            
            renderer.DrawText($"Sprites: {_displayedSpriteCount} ({_displayedCulledSprites} culled)", x, currentY, Color.White);
            currentY += lineHeight;
            
            var batchEfficiencyColor = GetBatchEfficiencyColor(_displayedBatchEfficiency);
            renderer.DrawText($"Batches: {_displayedBatchCount} ({_displayedBatchEfficiency:F1}x)", 
                x, currentY, batchEfficiencyColor);
            currentY += lineHeight;
            
            currentY += 5;
            
            renderer.DrawText("=== Memory ===", x, currentY, new Color(255, 255, 100));
            currentY += lineHeight;
            
            renderer.DrawText($"Total: {_displayedMemoryMB:F2} MB", x, currentY, Color.White);
            currentY += lineHeight;
            
            var gcColor = GetGCColor(_displayedGen2);
            renderer.DrawText($"GC: {_displayedGen0} / {_displayedGen1} / {_displayedGen2}", 
                x, currentY, gcColor);
            currentY += lineHeight;
        }
        
        var instructionY = currentY + 10;
        renderer.DrawText("F3: Toggle Details", x, instructionY, new Color(150, 150, 150));
    }
    
    /// <summary>
    /// Renders a frame time graph.
    /// Graph updates every frame for smooth animation.
    /// </summary>
    /// <param name="renderer">The renderer to use.</param>
    /// <param name="screenWidth">Screen width for positioning (default: 1280).</param>
    /// <param name="screenHeight">Screen height (default: 720).</param>
    public void RenderFrameTimeGraph(IRenderer renderer, int screenWidth = 1280, int screenHeight = 720)
    {
        if (!_isVisible || !_showDetailedStats)
            return;
        
        // CRITICAL: Temporarily disable camera for UI rendering
        var previousCamera = renderer.Camera;
        renderer.Camera = null;
        
        try
        {
            RenderGraph(renderer, screenWidth, screenHeight);
        }
        finally
        {
            renderer.Camera = previousCamera;
        }
    }
    
    private void RenderGraph(IRenderer renderer, int screenWidth, int screenHeight)
    {
        var graphWidth = 200;
        var graphHeight = 100;
        
        int graphX, graphY;
        switch (_position)
        {
            case OverlayPosition.TopRight:
                graphX = screenWidth - graphWidth - 10;
                graphY = 300;
                break;
            case OverlayPosition.TopLeft:
                graphX = 360;
                graphY = 10;
                break;
            case OverlayPosition.BottomLeft:
                graphX = 360;
                graphY = screenHeight - graphHeight - 10;
                break;
            case OverlayPosition.BottomRight:
                graphX = screenWidth - graphWidth - 10;
                graphY = screenHeight - graphHeight - 120;
                break;
            default:
                graphX = 360;
                graphY = 10;
                break;
        }
        
        renderer.DrawRectangleFilled(graphX, graphY, graphWidth, graphHeight, new Color(0, 0, 0, 180));
        
        // Draw 60 FPS line (16.67ms)
        var targetLineY = graphY + graphHeight - (16.67f / 50f * graphHeight);
        renderer.DrawLine(graphX, targetLineY, graphX + graphWidth, targetLineY, 
            new Color(0, 255, 0, 100), 1f);
        
        // Draw frame time history (updates every frame for smooth animation)
        var history = _monitor.FrameTimeHistory.ToArray();
        if (history.Length > 1)
        {
            var step = graphWidth / (float)history.Length;
            
            for (int i = 0; i < history.Length - 1; i++)
            {
                var x1 = graphX + (i * step);
                var x2 = graphX + ((i + 1) * step);
                
                // Clamp to 50ms max for graph scale
                var y1 = graphY + graphHeight - (float)(Math.Min(history[i], 50) / 50.0 * graphHeight);
                var y2 = graphY + graphHeight - (float)(Math.Min(history[i + 1], 50) / 50.0 * graphHeight);
                
                var color = history[i + 1] > 16.67 ? new Color(255, 100, 100) : new Color(100, 255, 100);
                renderer.DrawLine(x1, y1, x2, y2, color, 2f);
            }
        }
        
        // Label
        renderer.DrawText("Frame Time (ms)", graphX, graphY - 20, Color.White);
    }
    
    /// <summary>
    /// Updates the displayed values from the monitor.
    /// Called at a fixed interval (not every frame).
    /// </summary>
    private void UpdateDisplayedValues()
    {
        _displayedFPS = _monitor.CurrentFPS;
        _displayedFrameTime = _monitor.CurrentFrameTime;
        _displayedMinFPS = _monitor.MinFPS;
        _displayedMaxFPS = _monitor.MaxFPS;
        _displayedAvgFPS = _monitor.AverageFPS;
        _displayedDrawCalls = _monitor.DrawCalls;
        _displayedEntityCount = _monitor.EntityCount;
        _displayedSpriteCount = _monitor.SpriteCount;
        _displayedCulledSprites = _monitor.CulledSprites;
        _displayedBatchCount = _monitor.BatchCount;
        _displayedBatchEfficiency = _monitor.BatchEfficiency;
        _displayedMemoryMB = _monitor.TotalMemoryMB;
        _displayedGen0 = _monitor.Gen0Collections;
        _displayedGen1 = _monitor.Gen1Collections;
        _displayedGen2 = _monitor.Gen2Collections;
    }
    
    private Color GetFpsColor(double fps)
    {
        if (fps >= 60) return new Color(0, 255, 0);
        if (fps >= 30) return new Color(255, 255, 0);
        return new Color(255, 100, 100);
    }
    
    private Color GetBatchEfficiencyColor(float efficiency)
    {
        if (efficiency >= 10) return new Color(0, 255, 0);
        if (efficiency >= 5) return new Color(255, 255, 0);
        return new Color(255, 150, 0);
    }
    
    private Color GetGCColor(int gen2Collections)
    {
        if (gen2Collections > 10) return new Color(255, 100, 100);
        return Color.White;
    }
}

/// <summary>
/// Position options for the performance overlay.
/// </summary>
public enum OverlayPosition
{
    TopLeft,
    TopRight,
    BottomLeft,
    BottomRight
}