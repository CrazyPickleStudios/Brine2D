using Brine2D.Core.Input;
using Brine2D.Core.Math;
using SDL;
using static SDL.SDL3;

namespace Brine2D.SDL.Input;

internal sealed unsafe class SdlGamepads : IGamepads, IDisposable
{
    private readonly List<SdlGamepad> _pads = new();
    public event Action<int>? OnConnected;
    public event Action<int>? OnDisconnected;

    public int Count => _pads.Count;
    public bool DefaultInvertLeftX { get; set; } = false;
    public bool DefaultInvertLeftY { get; set; } = false;
    public bool DefaultInvertRightX { get; set; } = false;
    public bool DefaultInvertRightY { get; set; } = false;
    public bool DefaultRadialDeadzone { get; set; } = true;

    // Manager defaults applied to newly added pads
    public float DefaultStickDeadzone { get; set; } = 0.15f;
    public float DefaultTriggerPressThreshold { get; set; } = 0.5f;
    public float DefaultTriggerReleaseThreshold { get; set; } = 0.45f;
    public IGamepad? Primary => _pads.Count > 0 ? _pads[0] : null;

    public void BeginFrame()
    {
        for (var i = 0; i < _pads.Count; i++)
        {
            _pads[i].BeginFrame();
        }
    }

    public void Dispose()
    {
        for (var i = 0; i < _pads.Count; i++)
        {
            _pads[i].Dispose();
        }

        _pads.Clear();
    }

    public IGamepad? Get(int index)
    {
        return index >= 0 && index < _pads.Count ? _pads[index] : null;
    }

    internal static GamepadAxis MapAxis(SDL_GamepadAxis a)
    {
        return a switch
        {
            SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTX => GamepadAxis.LeftX,
            SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTY => GamepadAxis.LeftY,
            SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTX => GamepadAxis.RightX,
            SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTY => GamepadAxis.RightY,
            SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFT_TRIGGER => GamepadAxis.LeftTrigger,
            SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHT_TRIGGER => GamepadAxis.RightTrigger,
            _ => GamepadAxis.LeftX
        };
    }

    internal static GamepadButton MapButton(SDL_GamepadButton b)
    {
        return b switch
        {
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH => GamepadButton.A, // A / Cross
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST => GamepadButton.B, // B / Circle
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_WEST => GamepadButton.X, // X / Square
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_NORTH => GamepadButton.Y, // Y / Triangle
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK => GamepadButton.Back,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_START => GamepadButton.Start,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_GUIDE => GamepadButton.Guide,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_SHOULDER => GamepadButton.LeftShoulder,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER => GamepadButton.RightShoulder,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_STICK => GamepadButton.LeftStick,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_STICK => GamepadButton.RightStick,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP => GamepadButton.DPadUp,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN => GamepadButton.DPadDown,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT => GamepadButton.DPadLeft,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT => GamepadButton.DPadRight,
            _ => GamepadButton.A
        };
    }

    internal void EnumerateExisting()
    {
        // SDL3: SDL_GetGamepads returns an array you must free with SDL_free.
        var count = 0;
        var ids = SDL_GetGamepads(&count);
        if (ids == null || count <= 0)
        {
            return;
        }

        try
        {
            for (var i = 0; i < count; i++)
            {
                var gid = ids[i];
                if (gid == 0)
                {
                    continue;
                }

                if (FindByInstance(gid) != null)
                {
                    continue; // avoid duplicates if an ADDED event also fires
                }

                OnGamepadAdded(gid);
            }
        }
        finally
        {
            SDL_free(ids);
        }
    }

    internal void OnFocusGained()
    {
        foreach (var p in _pads)
        {
            p.OnFocusGained();
        }
    }

    internal void OnFocusLost()
    {
        foreach (var p in _pads)
        {
            p.OnFocusLost();
        }
    }

    internal void OnGamepadAdded(SDL_JoystickID which)
    {
        // Guard against duplicates (can happen if we enumerated and also get an ADDED event)
        if (FindByInstance(which) != null)
        {
            return;
        }

        var pad = SDL_OpenGamepad(which);
        if (pad == null)
        {
            return;
        }

        string? name = null;
        try
        {
            name = SDL_GetGamepadName(pad) ?? "Gamepad";
        }
        catch
        {
            name = "Gamepad";
        }

        var gp = new SdlGamepad(pad, which, name);

        // Apply manager defaults
        gp.StickDeadzone = DefaultStickDeadzone;
        gp.RadialDeadzone = DefaultRadialDeadzone;
        gp.InvertLeftX = DefaultInvertLeftX;
        gp.InvertLeftY = DefaultInvertLeftY;
        gp.InvertRightX = DefaultInvertRightX;
        gp.InvertRightY = DefaultInvertRightY;
        gp.TriggerPressThreshold = DefaultTriggerPressThreshold;
        gp.TriggerReleaseThreshold = DefaultTriggerReleaseThreshold;

        gp.SeedInitialState();
        _pads.Add(gp);
        OnConnected?.Invoke(_pads.Count - 1);
    }

    internal void OnGamepadAxis(SDL_JoystickID which, SDL_GamepadAxis sdlAxis, short value)
    {
        var gp = FindByInstance(which);
        if (gp == null)
        {
            return;
        }

        gp.OnAxis(MapAxis(sdlAxis), value);
    }

    internal void OnGamepadButton(SDL_JoystickID which, SDL_GamepadButton sdlButton, bool down)
    {
        var gp = FindByInstance(which);
        if (gp == null)
        {
            return;
        }

        gp.OnButton(MapButton(sdlButton), down);
    }

    internal void OnGamepadRemapped(SDL_JoystickID which)
    {
        var gp = FindByInstance(which);
        if (gp == null)
        {
            return;
        }

        gp.SeedInitialState();
    }

    internal void OnGamepadRemoved(SDL_JoystickID which)
    {
        var idx = IndexOf(which);
        if (idx < 0)
        {
            return;
        }

        _pads[idx].Dispose();
        _pads.RemoveAt(idx);
        OnDisconnected?.Invoke(idx);
    }

    private SdlGamepad? FindByInstance(SDL_JoystickID which)
    {
        var idx = IndexOf(which);
        return idx >= 0 ? _pads[idx] : null;
    }

    private int IndexOf(SDL_JoystickID which)
    {
        for (var i = 0; i < _pads.Count; i++)
        {
            if (_pads[i].InstanceId == which)
            {
                return i;
            }
        }

        return -1;
    }
}

internal sealed unsafe class SdlGamepad : IGamepad, IDisposable
{
    private readonly float[] _axes; // raw normalized: sticks -1..+1, triggers 0..1

    private readonly bool[] _down;
    private readonly bool[] _pressed;
    private readonly bool[] _released;
    private bool _ltPressedEdge, _ltReleasedEdge, _rtPressedEdge, _rtReleasedEdge;

    // Trigger edges with hysteresis
    private float _ltPrev, _rtPrev;
    private SDL_Gamepad* _pad;

    // Capability cache (lazy-probed)
    private bool? _supportsRumble;
    private bool? _supportsTriggerRumble;

    public SdlGamepad(SDL_Gamepad* pad, SDL_JoystickID id, string? name)
    {
        _pad = pad;
        InstanceId = id;
        Name = name;

        var buttons = Enum.GetValues<GamepadButton>().Length;
        _down = new bool[buttons];
        _pressed = new bool[buttons];
        _released = new bool[buttons];

        var axes = Enum.GetValues<GamepadAxis>().Length;
        _axes = new float[axes];
    }

    public SDL_JoystickID InstanceId { get; }

    // Axis inversion options
    public bool InvertLeftX { get; set; }
    public bool InvertLeftY { get; set; }
    public bool InvertRightX { get; set; }
    public bool InvertRightY { get; set; }

    public bool IsConnected => _pad != null;

    public Vector2 LeftStick
    {
        get
        {
            var v = ApplyStickDeadzone(_axes[(int)GamepadAxis.LeftX], _axes[(int)GamepadAxis.LeftY]);
            var x = InvertLeftX ? -v.X : v.X;
            var y = InvertLeftY ? -v.Y : v.Y;
            return new Vector2(x, y);
        }
    }

    public float LeftTrigger => ApplyTriggerThreshold(_axes[(int)GamepadAxis.LeftTrigger]);
    public string? Name { get; }
    public bool RadialDeadzone { get; set; } = true;

    public Vector2 RightStick
    {
        get
        {
            var v = ApplyStickDeadzone(_axes[(int)GamepadAxis.RightX], _axes[(int)GamepadAxis.RightY]);
            var x = InvertRightX ? -v.X : v.X;
            var y = InvertRightY ? -v.Y : v.Y;
            return new Vector2(x, y);
        }
    }

    public float RightTrigger => ApplyTriggerThreshold(_axes[(int)GamepadAxis.RightTrigger]);

    public float StickDeadzone { get; set; } = 0.15f;

    public bool SupportsRumble
    {
        get
        {
            if (_pad == null)
            {
                return false;
            }

            if (_supportsRumble.HasValue)
            {
                return _supportsRumble.Value;
            }

            // Probe by zeroing strengths (stops rumble, safe as a capability check)
            _supportsRumble = SDL_RumbleGamepad(_pad, 0, 0, 0);
            return _supportsRumble.Value;
        }
    }

    public bool SupportsTriggerRumble
    {
        get
        {
            if (_pad == null)
            {
                return false;
            }

            if (_supportsTriggerRumble.HasValue)
            {
                return _supportsTriggerRumble.Value;
            }

            _supportsTriggerRumble = SDL_RumbleGamepadTriggers(_pad, 0, 0, 0);
            return _supportsTriggerRumble.Value;
        }
    }

    // Trigger thresholds
    public float TriggerPressThreshold { get; set; } = 0.5f;
    public float TriggerReleaseThreshold { get; set; } = 0.45f;

    public void BeginFrame()
    {
        Array.Clear(_pressed, 0, _pressed.Length);
        Array.Clear(_released, 0, _released.Length);

        // Snapshot last-frame trigger values and reset edges
        _ltPrev = _axes[(int)GamepadAxis.LeftTrigger];
        _rtPrev = _axes[(int)GamepadAxis.RightTrigger];
        _ltPressedEdge = _ltReleasedEdge = _rtPressedEdge = _rtReleasedEdge = false;
    }

    public void Dispose()
    {
        if (_pad != null)
        {
            SDL_CloseGamepad(_pad);
            _pad = null;
        }
    }

    public float GetAxis(GamepadAxis axis)
    {
        return _axes[(int)axis];
    }

    public bool IsButtonDown(GamepadButton button)
    {
        return _down[(int)button];
    }

    public void OnAxis(GamepadAxis a, short rawValue)
    {
        // SDL3 axes are typically -32768..32767 for sticks; triggers may be unified to signed.
        float v;
        if (a == GamepadAxis.LeftTrigger || a == GamepadAxis.RightTrigger)
        {
            v = rawValue / 32767f;
            if (v < 0f)
            {
                v = 0f; // clamp signed negative to 0
            }

            if (v > 1f)
            {
                v = 1f;
            }
        }
        else
        {
            v = rawValue / 32767f;
            if (v < -1f)
            {
                v = -1f;
            }

            if (v > +1f)
            {
                v = +1f;
            }
        }

        _axes[(int)a] = v;

        // Trigger edges with hysteresis vs last-frame values
        if (a == GamepadAxis.LeftTrigger)
        {
            float prev = _ltPrev, curr = v;
            if (!_ltPressedEdge && prev < TriggerPressThreshold && curr >= TriggerPressThreshold)
            {
                _ltPressedEdge = true;
            }

            if (!_ltReleasedEdge && prev >= TriggerPressThreshold && curr < TriggerReleaseThreshold)
            {
                _ltReleasedEdge = true;
            }
        }
        else if (a == GamepadAxis.RightTrigger)
        {
            float prev = _rtPrev, curr = v;
            if (!_rtPressedEdge && prev < TriggerPressThreshold && curr >= TriggerPressThreshold)
            {
                _rtPressedEdge = true;
            }

            if (!_rtReleasedEdge && prev >= TriggerPressThreshold && curr < TriggerReleaseThreshold)
            {
                _rtReleasedEdge = true;
            }
        }
    }

    public void OnButton(GamepadButton b, bool isDown)
    {
        var i = (int)b;
        if (isDown)
        {
            if (!_down[i])
            {
                _down[i] = true;
                _pressed[i] = true;
            }
        }
        else
        {
            if (_down[i])
            {
                _down[i] = false;
                _released[i] = true;
            }
        }
    }

    public void OnFocusGained()
    {
        // nothing required right now
    }

    public void OnFocusLost()
    {
        Array.Clear(_down, 0, _down.Length);
        Array.Clear(_pressed, 0, _pressed.Length);
        Array.Clear(_released, 0, _released.Length);
        Array.Clear(_axes, 0, _axes.Length);
        // Keep capability cache; it doesn't change per focus.

        _ltPrev = _rtPrev = 0f;
        _ltPressedEdge = _ltReleasedEdge = _rtPressedEdge = _rtReleasedEdge = false;
    }

    public void StopRumble()
    {
        if (_pad == null)
        {
            return;
        }

        SDL_RumbleGamepad(_pad, 0, 0, 0);
    }

    public void StopRumbleTriggers()
    {
        if (_pad == null)
        {
            return;
        }

        SDL_RumbleGamepadTriggers(_pad, 0, 0, 0);
    }

    public bool TryRumble(ushort lowFrequency, ushort highFrequency, uint durationMs)
    {
        if (_pad == null || !SupportsRumble)
        {
            return false; // property probes and caches
        }

        return SDL_RumbleGamepad(_pad, lowFrequency, highFrequency, durationMs);
    }

    public bool TryRumbleTriggers(ushort leftTrigger, ushort rightTrigger, uint durationMs)
    {
        if (_pad == null || !SupportsTriggerRumble)
        {
            return false; // property probes and caches
        }

        return SDL_RumbleGamepadTriggers(_pad, leftTrigger, rightTrigger, durationMs);
    }

    public bool WasButtonPressed(GamepadButton button)
    {
        return _pressed[(int)button];
    }

    public bool WasButtonReleased(GamepadButton button)
    {
        return _released[(int)button];
    }

    public bool WasLeftTriggerPressed()
    {
        return _ltPressedEdge;
    }

    public bool WasLeftTriggerReleased()
    {
        return _ltReleasedEdge;
    }

    public bool WasRightTriggerPressed()
    {
        return _rtPressedEdge;
    }

    public bool WasRightTriggerReleased()
    {
        return _rtReleasedEdge;
    }

    internal void SeedInitialState()
    {
        if (_pad == null)
        {
            return;
        }

        // Seed buttons (set held state only; do not generate edges)
        var btns = new[]
        {
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_SOUTH,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_EAST,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_WEST,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_NORTH,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_BACK,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_START,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_GUIDE,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_SHOULDER,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_SHOULDER,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_LEFT_STICK,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_RIGHT_STICK,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_UP,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_DOWN,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_LEFT,
            SDL_GamepadButton.SDL_GAMEPAD_BUTTON_DPAD_RIGHT
        };
        foreach (var sb in btns)
        {
            var down = SDL_GetGamepadButton(_pad, sb);
            _down[(int)SdlGamepads.MapButton(sb)] = down;
        }

        // Seed axes
        var axes = new[]
        {
            SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTX,
            SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFTY,
            SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTX,
            SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHTY,
            SDL_GamepadAxis.SDL_GAMEPAD_AXIS_LEFT_TRIGGER,
            SDL_GamepadAxis.SDL_GAMEPAD_AXIS_RIGHT_TRIGGER
        };
        foreach (var sa in axes)
        {
            var raw = SDL_GetGamepadAxis(_pad, sa);
            OnAxis(SdlGamepads.MapAxis(sa), raw);
        }

        // Initialize last-frame trigger values to current to avoid false edges first frame
        _ltPrev = _axes[(int)GamepadAxis.LeftTrigger];
        _rtPrev = _axes[(int)GamepadAxis.RightTrigger];
        _ltPressedEdge = _ltReleasedEdge = _rtPressedEdge = _rtReleasedEdge = false;
    }

    private static float ApplyTriggerThreshold(float v)
    {
        const float thresh = 0.05f;
        return v < thresh ? 0f : v;
    }

    private Vector2 ApplyStickDeadzone(float x, float y)
    {
        if (!RadialDeadzone)
        {
            var dx = MathF.Abs(x) < StickDeadzone ? 0f : x;
            var dy = MathF.Abs(y) < StickDeadzone ? 0f : y;
            return new Vector2(dx, dy);
        }

        var len = MathF.Sqrt(x * x + y * y);
        if (len <= StickDeadzone)
        {
            return new Vector2(0, 0);
        }

        // Rescale from [deadzone..1] back to [0..1]
        var newLen = (len - StickDeadzone) / (1f - StickDeadzone);
        var scale = newLen / len;
        return new Vector2(x * scale, y * scale);
    }
}