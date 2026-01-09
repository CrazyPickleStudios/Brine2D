using System.Diagnostics;

namespace Brine2D.Core.Performance;

/// <summary>
/// Monitors game performance metrics like FPS, frame time, and memory usage.
/// Provides data for performance overlays and profiling.
/// </summary>
public class PerformanceMonitor
{
    private readonly Stopwatch _frameTimer = new();
    private readonly Queue<double> _frameTimeHistory = new(60); // Last 60 frames
    private double _totalFrameTime = 0;
    private int _frameCount = 0;
    
    // FPS tracking
    private double _currentFps = 0;
    private double _minFps = double.MaxValue;
    private double _maxFps = 0;
    private double _avgFps = 0;
    
    // Frame time tracking
    private double _currentFrameTime = 0; // milliseconds
    private double _minFrameTime = double.MaxValue;
    private double _maxFrameTime = 0;
    
    // Stats tracking
    private int _drawCalls = 0;
    private int _entityCount = 0;
    private int _spriteCount = 0;
    private int _culledSprites = 0;
    private int _batchCount = 0;
    
    // Memory tracking
    private long _totalMemory = 0;
    private int _gen0Collections = 0;
    private int _gen1Collections = 0;
    private int _gen2Collections = 0;
    
    /// <summary>
    /// Gets the current frames per second.
    /// </summary>
    public double CurrentFPS => _currentFps;
    
    /// <summary>
    /// Gets the minimum FPS recorded.
    /// </summary>
    public double MinFPS => _minFps;
    
    /// <summary>
    /// Gets the maximum FPS recorded.
    /// </summary>
    public double MaxFPS => _maxFps;
    
    /// <summary>
    /// Gets the average FPS.
    /// </summary>
    public double AverageFPS => _avgFps;
    
    /// <summary>
    /// Gets the current frame time in milliseconds.
    /// </summary>
    public double CurrentFrameTime => _currentFrameTime;
    
    /// <summary>
    /// Gets the minimum frame time in milliseconds.
    /// </summary>
    public double MinFrameTime => _minFrameTime;
    
    /// <summary>
    /// Gets the maximum frame time in milliseconds.
    /// </summary>
    public double MaxFrameTime => _maxFrameTime;
    
    /// <summary>
    /// Gets the number of draw calls in the last frame.
    /// </summary>
    public int DrawCalls => _drawCalls;
    
    /// <summary>
    /// Gets the total number of entities.
    /// </summary>
    public int EntityCount => _entityCount;
    
    /// <summary>
    /// Gets the number of sprites rendered.
    /// </summary>
    public int SpriteCount => _spriteCount;
    
    /// <summary>
    /// Gets the number of sprites culled (not rendered).
    /// </summary>
    public int CulledSprites => _culledSprites;
    
    /// <summary>
    /// Gets the number of batches used.
    /// </summary>
    public int BatchCount => _batchCount;
    
    /// <summary>
    /// Gets the batch efficiency (sprites per batch).
    /// </summary>
    public float BatchEfficiency => _batchCount > 0 ? (float)_spriteCount / _batchCount : 0;
    
    /// <summary>
    /// Gets the total managed memory in megabytes.
    /// </summary>
    public double TotalMemoryMB => _totalMemory / 1024.0 / 1024.0;
    
    /// <summary>
    /// Gets the number of Gen 0 garbage collections.
    /// </summary>
    public int Gen0Collections => _gen0Collections;
    
    /// <summary>
    /// Gets the number of Gen 1 garbage collections.
    /// </summary>
    public int Gen1Collections => _gen1Collections;
    
    /// <summary>
    /// Gets the number of Gen 2 garbage collections.
    /// </summary>
    public int Gen2Collections => _gen2Collections;
    
    /// <summary>
    /// Gets the frame time history for the last 60 frames.
    /// </summary>
    public IReadOnlyCollection<double> FrameTimeHistory => _frameTimeHistory;
    
    /// <summary>
    /// Begins timing a new frame.
    /// </summary>
    public void BeginFrame()
    {
        _frameTimer.Restart();
    }
    
    /// <summary>
    /// Ends frame timing and updates statistics.
    /// </summary>
    public void EndFrame()
    {
        _frameTimer.Stop();
        
        // Calculate frame time in milliseconds
        _currentFrameTime = _frameTimer.Elapsed.TotalMilliseconds;
        
        // Update frame time history (rolling window of 60 frames)
        if (_frameTimeHistory.Count >= 60)
        {
            var oldestFrame = _frameTimeHistory.Dequeue();
            _totalFrameTime -= oldestFrame;
        }
        
        _frameTimeHistory.Enqueue(_currentFrameTime);
        _totalFrameTime += _currentFrameTime;
        _frameCount++;
        
        // Calculate FPS
        _currentFps = 1000.0 / _currentFrameTime;
        
        // Update min/max FPS (ignore first 60 frames for warmup)
        if (_frameCount > 60)
        {
            _minFps = Math.Min(_minFps, _currentFps);
            _maxFps = Math.Max(_maxFps, _currentFps);
            _minFrameTime = Math.Min(_minFrameTime, _currentFrameTime);
            _maxFrameTime = Math.Max(_maxFrameTime, _currentFrameTime);
        }
        
        // Calculate average FPS (from frame time history)
        if (_frameTimeHistory.Count > 0)
        {
            var avgFrameTime = _totalFrameTime / _frameTimeHistory.Count;
            _avgFps = 1000.0 / avgFrameTime;
        }
        
        // Update memory stats
        UpdateMemoryStats();
    }
    
    /// <summary>
    /// Updates rendering statistics for the current frame.
    /// </summary>
    public void UpdateRenderStats(int drawCalls, int entityCount, int spriteCount, int culledSprites, int batchCount)
    {
        _drawCalls = drawCalls;
        _entityCount = entityCount;
        _spriteCount = spriteCount;
        _culledSprites = culledSprites;
        _batchCount = batchCount;
    }
    
    /// <summary>
    /// Resets all statistics (useful for benchmarking).
    /// </summary>
    public void Reset()
    {
        _frameTimeHistory.Clear();
        _totalFrameTime = 0;
        _frameCount = 0;
        _currentFps = 0;
        _minFps = double.MaxValue;
        _maxFps = 0;
        _avgFps = 0;
        _currentFrameTime = 0;
        _minFrameTime = double.MaxValue;
        _maxFrameTime = 0;
        _drawCalls = 0;
        _entityCount = 0;
        _spriteCount = 0;
        _culledSprites = 0;
        _batchCount = 0;
    }
    
    private void UpdateMemoryStats()
    {
        _totalMemory = GC.GetTotalMemory(false);
        _gen0Collections = GC.CollectionCount(0);
        _gen1Collections = GC.CollectionCount(1);
        _gen2Collections = GC.CollectionCount(2);
    }
}