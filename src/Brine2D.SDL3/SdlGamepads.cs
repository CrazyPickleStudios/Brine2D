using Brine2D.Input;
using SDL3;

namespace Brine2D.SDL3;

internal sealed class SdlGamepads : IGamepads, IDisposable
{
    private readonly List<SdlGamepad> _pads = [];

    public IReadOnlyList<IGamepad> Pads => _pads;
    
    internal void EndFrame()
    {
        foreach (var p in _pads) p.EndFrame();
    }

    internal void OnDeviceAdded(uint which)
    {
        var handle = SDL.OpenGamepad(which);

        if (handle != IntPtr.Zero)
        {
            var pad = new SdlGamepad(which, handle);
            _pads.Add(pad);
        }
    }

    internal void OnDeviceRemoved(uint which)
    {
        var idx = _pads.FindIndex(p => p.Index == which);
        if (idx >= 0)
        {
            _pads[idx].Dispose();
            _pads.RemoveAt(idx);
        }
    }

    internal void OnAxisMotion(uint which, SDL.GamepadAxis axis, short value)
    {
        var pad = Get(which);
        pad?.OnAxisMotion(axis, value);
    }

    internal void OnButtonDown(uint which, SDL.GamepadButton button)
    {
        var pad = Get(which);
        pad?.OnButtonDown(button);
    }

    internal void OnButtonUp(uint which, SDL.GamepadButton button)
    {
        var pad = Get(which);
        pad?.OnButtonUp(button);
    }

    private SdlGamepad? Get(uint which) => _pads.Find(p => p.Index == which);

    public void Dispose()
    {
        foreach (var p in _pads) p.Dispose();
        _pads.Clear();
    }
}