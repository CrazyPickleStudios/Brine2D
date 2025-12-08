using Brine2D.Input;

namespace Brine2D.SDL3;

internal sealed class SdlTouch : ITouch
{
    private readonly Dictionary<long, TouchPoint> _current = new();
    private readonly Dictionary<long, TouchPoint> _previous = new();

    public IReadOnlyList<TouchPoint> Points => _current.Values.ToList();

    public bool IsDown(long fingerId)
    {
        return _current.ContainsKey(fingerId);
    }

    public bool WasPressed(long fingerId)
    {
        return _current.ContainsKey(fingerId) && !_previous.ContainsKey(fingerId);
    }

    public bool WasReleased(long fingerId)
    {
        return !_current.ContainsKey(fingerId) && _previous.ContainsKey(fingerId);
    }

    internal void BeginFrame()
    {
        // No-op for now; No per-frame accumulators for touch currently. -RP
    }

    internal void EndFrame()
    {
        _previous.Clear();
        foreach (var kv in _current)
        {
            _previous[kv.Key] = kv.Value;
        }
    }

    internal void OnFingerDown(ulong fingerId, float x, float y)
    {
        var id = unchecked((long)fingerId);
        _current[id] = new TouchPoint(id, x, y);
    }

    internal void OnFingerMotion(ulong fingerId, float x, float y)
    {
        var id = unchecked((long)fingerId);
        _current[id] = new TouchPoint(id, x, y);
    }

    internal void OnFingerUp(ulong fingerId, float x, float y)
    {
        var id = unchecked((long)fingerId);
        _current.Remove(id);
    }
}