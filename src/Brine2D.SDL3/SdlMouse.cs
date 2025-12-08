using Brine2D.Input;
using SDL3;

namespace Brine2D.SDL3;

public sealed class SdlMouse : IMouse
{
    private readonly HashSet<MouseButton> _current = [];
    private readonly HashSet<MouseButton> _previous = [];

    public float WheelX { get; private set; }

    public float WheelY { get; private set; }

    public float X { get; private set; }

    public float Y { get; private set; }

    public bool IsDown(MouseButton button)
    {
        return _current.Contains(button);
    }

    public bool WasPressed(MouseButton button)
    {
        return _current.Contains(button) && !_previous.Contains(button);
    }

    public bool WasReleased(MouseButton button)
    {
        return !_current.Contains(button) && _previous.Contains(button);
    }

    internal void BeginFrame()
    {
        WheelX = 0;
        WheelY = 0;
    }

    internal void EndFrame()
    {
        _previous.Clear();

        foreach (var b in _current)
        {
            _previous.Add(b);
        }
    }

    internal void OnMouseButtonDown(uint sdlButton)
    {
        if (TryConvertButton(sdlButton, out var button))
        {
            _current.Add(button);
        }
    }

    internal void OnMouseButtonUp(uint sdlButton)
    {
        if (TryConvertButton(sdlButton, out var button))
        {
            _current.Remove(button);
        }
    }

    internal void OnMouseMotion(float x, float y)
    {
        X = x;
        Y = y;
    }

    internal void OnMouseWheel(float wheelX, float wheelY)
    {
        WheelX += wheelX;
        WheelY += wheelY;
    }

    private static bool TryConvertButton(uint sdlButton, out MouseButton button)
    {
        button = sdlButton switch
        {
            SDL.ButtonLeft => MouseButton.Left,
            SDL.ButtonRight => MouseButton.Right,
            SDL.ButtonMiddle => MouseButton.Middle,
            SDL.ButtonX1 => MouseButton.X1,
            SDL.ButtonX2 => MouseButton.X2,
            _ => default
        };

        return sdlButton is SDL.ButtonLeft or SDL.ButtonRight or SDL.ButtonMiddle or SDL.ButtonX1 or SDL.ButtonX2;
    }
}