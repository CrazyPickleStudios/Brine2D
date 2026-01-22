using Brine2D.Rendering;
using System.Diagnostics;
using System.Drawing;

namespace Brine2D.Performance;

/// <summary>
/// Renders a performance statistics overlay on screen.
/// Shows FPS, frame time, draw calls, and other metrics.
/// Renders in screen space (UI layer) so it doesn't move with the camera.
/// </summary>
public class PerformanceOverlay
{
    private readonly PerformanceMonitor _monitor;
    private readonly ScopedProfiler? _systemProfiler;
    private readonly Stopwatch _updateTimer = new();
    private bool _isVisible = true;
    private bool _showDetailedStats = false;
    private bool _showSystemProfiling = false;
    private OverlayPosition _position = OverlayPosition.TopRight;
    
    // Display update throttling
    private double _displayUpdateInterval = 0.1; // 10 Hz
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

    // Cached system profiling data (throttled updates)
    private List<(string Name, ScopedTimingData Timing)> _displayedSystems = new();
    private double _displayedTotalFrameTime = 0;
    
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
    /// Gets or sets whether to show system profiling details.
    /// </summary>
    public bool ShowSystemProfiling
    {
        get => _showSystemProfiling;
        set => _showSystemProfiling = value;
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
    /// Default is 0.1 (10 updates per second).
    /// Lower values update more frequently but use more CPU for text rendering.
    /// </summary>
    public double DisplayUpdateInterval
    {
        get => _displayUpdateInterval;
        set => _displayUpdateInterval = Math.Max(0.016, value);
    }
    
    public PerformanceOverlay(PerformanceMonitor monitor, ScopedProfiler? systemProfiler = null)
    {
        _monitor = monitor ?? throw new ArgumentNullException(nameof(monitor));
        _systemProfiler = systemProfiler;
        _updateTimer.Start();
    }
    
    /// <summary>
    /// Renders the performance overlay.
    /// Screen dimensions are obtained from the renderer.
    /// </summary>
    /// <param name="renderer">The renderer to use.</param>
    public void Render(IRenderer renderer)
    {
        if (!_isVisible)
            return;
        
        // Update displayed values at fixed interval
        if (_updateTimer.Elapsed.TotalSeconds >= _displayUpdateInterval)
        {
            UpdateDisplayedValues();
            _updateTimer.Restart();
        }

        // Disable camera to render UI in screen space
        var previousCamera = renderer.Camera;
        renderer.Camera = null;

        try
        {
            RenderOverlay(renderer, renderer.Width, renderer.Height);
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
        var bgHeight = _showDetailedStats ? 295 : 135; // Extra space for 2-line instructions
        
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
        renderer.DrawRectangleFilled(x - 5, y - 5, bgWidth, bgHeight, Color.FromArgb(180, 0, 0, 0));
        
        // FPS Stats
        var fpsColor = GetFpsColor(_displayedFPS);
        renderer.DrawText($"FPS: {_displayedFPS:F1}", x, currentY, fpsColor);
        currentY += lineHeight;
        
        renderer.DrawText($"Frame: {_displayedFrameTime:F2}ms", x, currentY, Color.White);
        currentY += lineHeight;
        
        if (_showDetailedStats)
        {
            renderer.DrawText($"Min/Max/Avg: {_displayedMinFPS:F0} / {_displayedMaxFPS:F0} / {_displayedAvgFPS:F1}", 
                x, currentY, Color.FromArgb(180, 180, 180));
            currentY += lineHeight;
            
            currentY += 5;
            
            renderer.DrawText("=== Rendering ===", x, currentY, Color.FromArgb(255, 255, 100));
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
            
            renderer.DrawText("=== Memory ===", x, currentY, Color.FromArgb(255, 255, 100));
            currentY += lineHeight;
            
            renderer.DrawText($"Total: {_displayedMemoryMB:F2} MB", x, currentY, Color.White);
            currentY += lineHeight;
            
            var gcColor = GetGCColor(_displayedGen2);
            renderer.DrawText($"GC: {_displayedGen0} / {_displayedGen1} / {_displayedGen2}", 
                x, currentY, gcColor);
            currentY += lineHeight;
        }
        
        // Instructions - split into two lines
        var instructionY = currentY + 10;
        renderer.DrawText("F1: Visibility | F3: Details", x, instructionY, Color.FromArgb(150, 150, 150));
        renderer.DrawText("F4: System Profiling", x, instructionY + 15, Color.FromArgb(150, 150, 150));
    }
    
    /// <summary>
    /// Renders a frame time graph.
    /// Graph updates every frame for smooth animation.
    /// Screen dimensions are obtained from the renderer.
    /// </summary>
    /// <param name="renderer">The renderer to use.</param>
    public void RenderFrameTimeGraph(IRenderer renderer)
    {
        if (!_isVisible || !_showDetailedStats)
            return;
        
        // CRITICAL: Temporarily disable camera for UI rendering
        var previousCamera = renderer.Camera;
        renderer.Camera = null;
        
        try
        {
            RenderGraph(renderer, renderer.Width, renderer.Height);
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
        
        renderer.DrawRectangleFilled(graphX, graphY, graphWidth, graphHeight, Color.FromArgb(180, 0, 0, 0));
        
        // Draw 60 FPS line (16.67ms)
        var targetLineY = graphY + graphHeight - (16.67f / 50f * graphHeight);
        renderer.DrawLine(graphX, targetLineY, graphX + graphWidth, targetLineY,
            Color.FromArgb(100, 0, 255, 0), 1f);
        
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
                
                var color = history[i + 1] > 16.67 ? Color.FromArgb(255, 100, 100) : Color.FromArgb(100, 255, 100);
                renderer.DrawLine(x1, y1, x2, y2, color, 2f);
            }
        }
        
        // Label
        renderer.DrawText("Frame Time (ms)", graphX, graphY - 20, Color.White);
    }
    
    /// <summary>
    /// Renders system profiling data (F4 toggle).
    /// Updates are throttled to the same rate as the main overlay.
    /// Screen dimensions are obtained from the renderer.
    /// </summary>
    public void RenderSystemProfiling(IRenderer renderer)
    {
        if (!_isVisible || !_showSystemProfiling || _systemProfiler == null)
            return;

        // Disable camera for UI rendering
        var previousCamera = renderer.Camera;
        renderer.Camera = null;

        try
        {
            RenderSystemProfilingInternal(renderer, renderer.Width, renderer.Height);
        }
        finally
        {
            renderer.Camera = previousCamera;
        }
    }

    private void RenderSystemProfilingInternal(IRenderer renderer, int screenWidth, int screenHeight)
    {
        var panelWidth = 400;
        var panelHeight = 500;
        var x = (screenWidth - panelWidth) / 2; // Center
        var y = 50;
        var lineHeight = 18;

        // Background
        renderer.DrawRectangleFilled(x - 5, y - 5, panelWidth, panelHeight, Color.FromArgb(200, 0, 0, 0));

        // Title
        renderer.DrawText("=== SYSTEM PROFILING ===", x, y, Color.FromArgb(255, 255, 100));
        y += lineHeight + 5;

        // Use cached/throttled system data
        if (_displayedSystems.Count == 0)
        {
            renderer.DrawText("No profiling data yet...", x, y, Color.FromArgb(180, 180, 180));
        }
        else
        {
            // Header
            renderer.DrawText("System", x, y, Color.FromArgb(200, 200, 200));
            renderer.DrawText("Current", x + 250, y, Color.FromArgb(200, 200, 200));
            renderer.DrawText("Avg", x + 320, y, Color.FromArgb(200, 200, 200));
            y += lineHeight;

            // Draw separator
            renderer.DrawLine(x, y, x + panelWidth - 10, y, Color.FromArgb(100, 100, 100), 1f);
            y += 5;

            // Display cached systems
            foreach (var (systemName, timing) in _displayedSystems)
            {
                var displayName = systemName.Length > 30 
                    ? systemName.Substring(0, 27) + "..." 
                    : systemName;

                var color = GetSystemTimingColor(timing.CurrentMs);

                renderer.DrawText(displayName, x, y, Color.White);
                renderer.DrawText($"{timing.CurrentMs:F2}ms", x + 250, y, color);
                renderer.DrawText($"{timing.AverageMs:F2}ms", x + 320, y, Color.FromArgb(180, 180, 180));

                y += lineHeight;

                if (y > screenHeight - 100) break;
            }

            y += 10;

            // Total frame time
            renderer.DrawText($"Total Measured: {_displayedTotalFrameTime:F2}ms", x, y, Color.FromArgb(255, 255, 100));
        }

        // Instructions - split into two lines
        var instructionY = screenHeight - 70;
        renderer.DrawText("F4: Toggle Profiling", x, instructionY, Color.FromArgb(150, 150, 150));
        renderer.DrawText("F3: Toggle Details", x, instructionY + 15, Color.FromArgb(150, 150, 150));
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

        // Update system profiling stats (always, not just when visible)
        if (_systemProfiler != null)
        {
            _displayedSystems = _systemProfiler.ScopedTimings
                .OrderByDescending(kvp => kvp.Value.CurrentMs)
                .Take(15)
                .Select(kvp => (kvp.Key, kvp.Value))
                .ToList();

            _displayedTotalFrameTime = _displayedSystems.Sum(s => s.Timing.CurrentMs);
        }
    }
    
    private Color GetFpsColor(double fps)
    {
        if (fps >= 60) return Color.FromArgb(0, 255, 0);
        if (fps >= 30) return Color.FromArgb(255, 255, 0);
        return Color.FromArgb(255, 100, 100);
    }
    
    private Color GetBatchEfficiencyColor(float efficiency)
    {
        if (efficiency >= 10) return Color.FromArgb(0, 255, 0);
        if (efficiency >= 5) return Color.FromArgb(255, 255, 0);
        return Color.FromArgb(255, 150, 0);
    }
    
    private Color GetGCColor(int gen2Collections)
    {
        if (gen2Collections > 10) return Color.FromArgb(255, 100, 100);
        return Color.White;
    }

    private Color GetSystemTimingColor(double ms)
    {
        if (ms < 1.0) return Color.FromArgb(0, 255, 0);
        if (ms < 2.0) return Color.FromArgb(150, 255, 0);
        if (ms < 5.0) return Color.FromArgb(255, 255, 0);
        if (ms < 8.0) return Color.FromArgb(255, 150, 0);
        return Color.FromArgb(255, 100, 100);
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