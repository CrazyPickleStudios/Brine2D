using Brine2D.Input;
using SDL3;

namespace Brine2D.SDL3;

internal sealed class SdlGamepad : IGamepad, IDisposable
{
    private readonly Dictionary<GamepadAxis, float> _axes = new()
    {
        { GamepadAxis.LeftX, 0f }, { GamepadAxis.LeftY, 0f },
        { GamepadAxis.RightX, 0f }, { GamepadAxis.RightY, 0f },
        { GamepadAxis.LeftTrigger, 0f }, { GamepadAxis.RightTrigger, 0f }
    };

    private readonly HashSet<GamepadButton> _current = [];
    private readonly IntPtr _handle;
    private readonly HashSet<GamepadButton> _previous = [];

    public SdlGamepad(uint index, IntPtr handle)
    {
        Index = index;
        _handle = handle;
    }

    public uint Index { get; }

    int IGamepad.Index => (int)Index;

    public float Axis(GamepadAxis axis)
    {
        return _axes.GetValueOrDefault(axis, 0f);
    }

    public void Dispose()
    {
        if (_handle != IntPtr.Zero)
        {
            SDL.CloseGamepad(_handle);
        }
    }

    public bool IsDown(GamepadButton button)
    {
        return _current.Contains(button);
    }

    public bool WasPressed(GamepadButton button)
    {
        return _current.Contains(button) && !_previous.Contains(button);
    }

    public bool WasReleased(GamepadButton button)
    {
        return !_current.Contains(button) && _previous.Contains(button);
    }

    internal void EndFrame()
    {
        _previous.Clear();

        foreach (var b in _current)
        {
            _previous.Add(b);
        }
    }

    internal void OnAxisMotion(SDL.GamepadAxis axis, short value)
    {
        var mapped = MapAxis(axis);
        float norm;

        if (axis is SDL.GamepadAxis.LeftTrigger or SDL.GamepadAxis.RightTrigger)
        {
            norm = Math.Clamp(value / 32767f, 0f, 1f);
        }
        else
        {
            norm = value >= 0
                ? value / 32767f
                : value / 32768f;

            const float deadzone = 0.05f;

            if (Math.Abs(norm) < deadzone)
            {
                norm = 0f;
            }
        }

        _axes[mapped] = norm;
    }

    internal void OnButtonDown(SDL.GamepadButton button)
    {
        if (TryMapButton(button, out var mapped))
        {
            _current.Add(mapped);
        }
    }

    internal void OnButtonUp(SDL.GamepadButton button)
    {
        if (TryMapButton(button, out var mapped))
        {
            _current.Remove(mapped);
        }
    }

    private static GamepadAxis MapAxis(SDL.GamepadAxis axis)
    {
        return axis switch
        {
            SDL.GamepadAxis.LeftX => GamepadAxis.LeftX,
            SDL.GamepadAxis.LeftY => GamepadAxis.LeftY,
            SDL.GamepadAxis.RightX => GamepadAxis.RightX,
            SDL.GamepadAxis.RightY => GamepadAxis.RightY,
            SDL.GamepadAxis.LeftTrigger => GamepadAxis.LeftTrigger,
            SDL.GamepadAxis.RightTrigger => GamepadAxis.RightTrigger,
            _ => GamepadAxis.LeftX
        };
    }

    private static bool TryMapButton(SDL.GamepadButton button, out GamepadButton mapped)
    {
        mapped = button switch
        {
            SDL.GamepadButton.South => GamepadButton.A,
            SDL.GamepadButton.East => GamepadButton.B,
            SDL.GamepadButton.West => GamepadButton.X,
            SDL.GamepadButton.North => GamepadButton.Y,
            SDL.GamepadButton.Back => GamepadButton.Back,
            SDL.GamepadButton.Start => GamepadButton.Start,
            SDL.GamepadButton.Guide => GamepadButton.Guide,
            SDL.GamepadButton.LeftShoulder => GamepadButton.LeftShoulder,
            SDL.GamepadButton.RightShoulder => GamepadButton.RightShoulder,
            SDL.GamepadButton.LeftStick => GamepadButton.LeftStick,
            SDL.GamepadButton.RightStick => GamepadButton.RightStick,
            SDL.GamepadButton.DPadUp => GamepadButton.DpadUp,
            SDL.GamepadButton.DPadDown => GamepadButton.DpadDown,
            SDL.GamepadButton.DPadLeft => GamepadButton.DpadLeft,
            SDL.GamepadButton.DPadRight => GamepadButton.DpadRight,
            _ => default
        };

        return mapped != default || button == SDL.GamepadButton.DPadRight;
    }
}