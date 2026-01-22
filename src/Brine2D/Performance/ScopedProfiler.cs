using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace Brine2D.Performance;

/// <summary>
/// Profiles individual scoped execution times.
/// Thread-safe for concurrent scoped execution (future-proofing).
/// </summary>
public class ScopedProfiler
{
    private readonly ConcurrentDictionary<string, ScopedTimingData> _scopeTimings = new();
    private readonly int _historySize = 60; // Last 60 frames

    /// <summary>
    /// Gets all scoped timing data.
    /// </summary>
    public IReadOnlyDictionary<string, ScopedTimingData> ScopedTimings => _scopeTimings;

    /// <summary>
    /// Begins timing a scope.
    /// </summary>
    /// <param name="scopeName">Name of the scope being profiled.</param>
    /// <returns>A disposable scope that automatically ends timing.</returns>
    public IDisposable BeginScope(string scopeName)
    {
        var data = _scopeTimings.GetOrAdd(scopeName, _ => new ScopedTimingData(_historySize));
        return new TimingScope(data);
    }

    /// <summary>
    /// Resets all profiling data.
    /// </summary>
    public void Reset()
    {
        _scopeTimings.Clear();
    }

    /// <summary>
    /// Gets timing data for a specific scope.
    /// </summary>
    public ScopedTimingData? GetScopeTiming(string scopeName)
    {
        return _scopeTimings.TryGetValue(scopeName, out var data) ? data : null;
    }

    private class TimingScope : IDisposable
    {
        private readonly ScopedTimingData _data;
        private readonly Stopwatch _stopwatch;

        public TimingScope(ScopedTimingData data)
        {
            _data = data;
            _stopwatch = Stopwatch.StartNew();
        }

        public void Dispose()
        {
            _stopwatch.Stop();
            _data.RecordFrame(_stopwatch.Elapsed.TotalMilliseconds);
        }
    }
}

/// <summary>
/// Stores timing data for a single scope.
/// </summary>
public class ScopedTimingData
{
    private readonly Queue<double> _frameHistory;
    private readonly int _maxHistorySize;
    private double _totalTime = 0;
    private double _currentMs = 0;
    private double _minMs = double.MaxValue;
    private double _maxMs = 0;
    private double _avgMs = 0;
    private int _frameCount = 0;

    public double CurrentMs => _currentMs;
    public double MinMs => _minMs == double.MaxValue ? 0 : _minMs;
    public double MaxMs => _maxMs;
    public double AverageMs => _avgMs;
    public IReadOnlyCollection<double> FrameHistory => _frameHistory;

    public ScopedTimingData(int historySize = 60)
    {
        _maxHistorySize = historySize;
        _frameHistory = new Queue<double>(historySize);
    }

    internal void RecordFrame(double milliseconds)
    {
        _currentMs = milliseconds;
        _frameCount++;

        // Update history
        if (_frameHistory.Count >= _maxHistorySize)
        {
            var oldest = _frameHistory.Dequeue();
            _totalTime -= oldest;
        }

        _frameHistory.Enqueue(milliseconds);
        _totalTime += milliseconds;

        // Update min/max (after warmup)
        if (_frameCount > 60)
        {
            _minMs = Math.Min(_minMs, milliseconds);
            _maxMs = Math.Max(_maxMs, milliseconds);
        }

        // Calculate average
        if (_frameHistory.Count > 0)
        {
            _avgMs = _totalTime / _frameHistory.Count;
        }
    }
}
