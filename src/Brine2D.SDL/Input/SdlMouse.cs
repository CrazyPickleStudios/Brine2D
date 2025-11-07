using System.Globalization;
using Brine2D.Core.Input;
using Brine2D.Core.Math;
using SDL;
using static SDL.SDL3;

namespace Brine2D.SDL.Input;

internal sealed unsafe class SdlMouse : IMouse, IDisposable
{
    // Cursor cache (store as nint; cast when calling SDL)
    private readonly Dictionary<MouseCursor, nint> _cursorCache = new();
    private readonly Dictionary<string, nint> _customCursorCache = new(StringComparer.OrdinalIgnoreCase);

    private readonly HashSet<MouseButton> _down = new();

    // Drag tracking per button: start point + active flag
    private readonly Dictionary<MouseButton, (float sx, float sy, bool active)> _drag = new();
    private readonly HashSet<MouseButton> _pressed = new();
    private readonly HashSet<MouseButton> _released = new();

    private nint _activeCustomCursor;
    private bool _activityThisFrame;
    private bool _cursorHiddenByAuto;
    private float _fDx, _fDy; // filtered deltas

    // Cursor visibility stack (to avoid flicker with nested calls)
    private int _hideStack;
    private double _lastActivitySeconds;
    private float _wheelX, _wheelY;
    private SDL_Window* _window;

    // State

    // Events (optional, light)
    public event Action<MouseButton>? OnClick;
    public event Action<MouseButton>? OnDoubleClick;
    public event Action<MouseButton>? OnDragEnd;
    public event Action<MouseButton>? OnDragStart;
    public event Action<MouseButton, byte>? OnMultiClick;
    public bool ApplySensitivityInRelativeOnly { get; set; } = true;

    // Auto-capture while dragging
    public bool AutoCaptureDuringDrag { get; set; } = false;
    public int AutoConfinePadding { get; set; }

    // Confine helpers
    public bool AutoConfineToWindowRect { get; set; }

    // Auto-hide after inactivity (seconds). 0/negative disables.
    public double AutoHideCursorAfterSeconds { get; set; } = 0.0;
    public bool CenterWarpOnRelativeEnable { get; set; } = true;
    public float DeltaX { get; private set; }

    public float DeltaY { get; private set; }

    // Expose masks as properties
    public uint DownMask => ToMask(_down);
    public float DragThresholdPixels { get; set; } = 4f;
    public bool EnableSmoothing { get; set; } = false;
    public float FilteredDeltaX => EnableSmoothing ? _fDx : DeltaX;
    public float FilteredDeltaY => EnableSmoothing ? _fDy : DeltaY;
    public bool IgnorePenMouseEvents { get; set; } = false;
    public bool IgnoreTouchMouseEvents { get; set; } = false;

    public bool InvertWheelX { get; set; }
    public bool InvertWheelY { get; set; }
    public bool IsInWindow { get; private set; }

    public bool IsMouseCaptured
        => _window != null && SDL_GetWindowMouseGrab(_window);

    public bool IsRelativeMouseModeEnabled
        => _window != null && SDL_GetWindowRelativeMouseMode(_window);

    // Last click info
    public MouseButton? LastClickButton { get; private set; }
    public byte LastClickCount { get; private set; }
    public double LastClickTimeSeconds { get; private set; }

    // Device and window tracking
    public SDL_MouseID LastDeviceId { get; private set; }
    public uint PressedMask => ToMask(_pressed);

    // Relative-mode polish
    public float RelativeSensitivity { get; set; } = 1.0f;

    public uint ReleasedMask => ToMask(_released);

    // 0..1; closer to 1 means heavier smoothing
    public float SmoothingFactor { get; set; } = 0.5f;

    // Wheel helpers
    public float WheelScale { get; set; } = 1.0f;
    public float WheelX => _wheelX * WheelScale;
    public bool WheelXScrolled => Math.Abs(_wheelX) > float.Epsilon;
    public float WheelY => _wheelY * WheelScale;
    public bool WheelYScrolled => Math.Abs(_wheelY) > float.Epsilon;

    // IMouse: position/deltas
    public float X { get; private set; }

    public float Y { get; private set; }

    // Options/filters
    internal bool DebounceDoubleClick { get; set; } = true;

    public void CaptureMouse(bool enabled)
    {
        SDL_CaptureMouse(enabled);
    }

    // Convenience to center the cursor in the window
    public void CenterCursor()
    {
        if (_window == null)
        {
            return;
        }

        int lw, lh;
        SDL_GetWindowSize(_window, &lw, &lh);
        SDL_WarpMouseInWindow(_window, lw / 2, lh / 2);
    }

    public void Dispose()
    {
        foreach (var kv in _cursorCache)
        {
            if (kv.Value != 0)
            {
                SDL_DestroyCursor((SDL_Cursor*)kv.Value);
            }
        }

        _cursorCache.Clear();

        foreach (var kv in _customCursorCache)
        {
            if (kv.Value != 0)
            {
                SDL_DestroyCursor((SDL_Cursor*)kv.Value);
            }
        }

        _customCursorCache.Clear();

        if (_activeCustomCursor != 0)
        {
            SDL_DestroyCursor((SDL_Cursor*)_activeCustomCursor);
            _activeCustomCursor = 0;
        }
    }

    public (float dx, float dy) GetDragDelta(MouseButton b)
    {
        if (_drag.TryGetValue(b, out var d))
        {
            return (X - d.sx, Y - d.sy);
        }

        return (0, 0);
    }

    public (float sx, float sy)? GetDragStart(MouseButton b)
    {
        return _drag.TryGetValue(b, out var d) ? (d.sx, d.sy) : null;
    }

    public (float nx, float ny) GetNormalizedPosition()
    {
        if (_window == null)
        {
            return (0, 0);
        }

        int lw, lh;
        SDL_GetWindowSize(_window, &lw, &lh);
        if (lw <= 0 || lh <= 0)
        {
            return (0, 0);
        }

        return (X / lw, Y / lh);
    }

    // HiDPI helpers
    public (int px, int py) GetPixelPosition()
    {
        if (_window == null)
        {
            return ((int)MathF.Round(X), (int)MathF.Round(Y));
        }

        int lw, lh, pw, ph;
        SDL_GetWindowSize(_window, &lw, &lh);
        SDL_GetWindowSizeInPixels(_window, &pw, &ph);
        if (lw <= 0 || lh <= 0)
        {
            return (0, 0);
        }

        var sx = (float)pw / lw;
        var sy = (float)ph / lh;
        return ((int)MathF.Round(X * sx), (int)MathF.Round(Y * sy));
    }

    // IMouse: button queries
    public bool IsButtonDown(MouseButton button)
    {
        return _down.Contains(button);
    }

    public bool IsDragging(MouseButton b)
    {
        return _drag.TryGetValue(b, out var d) && d.active;
    }

    public void PopHideCursor()
    {
        if (_hideStack == 0)
        {
            return;
        }

        _hideStack--;
        if (_hideStack == 0)
        {
            SDL_ShowCursor();
        }
    }

    public void PushHideCursor()
    {
        _hideStack++;
        if (_hideStack == 1)
        {
            SDL_HideCursor();
        }
    }

    // Reset all mouse state (useful on scene reset or forced cleanup)
    public void Reset()
    {
        DeltaX = DeltaY = 0f;
        _fDx = _fDy = 0f;
        _wheelX = _wheelY = 0f;
        _down.Clear();
        _pressed.Clear();
        _released.Clear();
        _drag.Clear();
        LastClickButton = null;
        LastClickCount = 0;
        LastClickTimeSeconds = 0.0;
        _activityThisFrame = false;
    }

    // Toggle auto-confine and apply immediately
    public void SetAutoConfineToWindow(bool enable, int padding = 0)
    {
        AutoConfineToWindowRect = enable;
        AutoConfinePadding = padding;
        if (_window != null)
        {
            if (enable)
            {
                OnWindowResized(); // applies rect + grab
            }
            else
            {
                SDL_SetWindowMouseRect(_window, null);
                SDL_SetWindowMouseGrab(_window, false);
            }
        }
    }

    public void SetConfinedToWindow(bool enabled)
    {
        if (_window != null)
        {
            SDL_SetWindowMouseGrab(_window, enabled);
        }
    }

    public void SetConfineRect(Rectangle? rect)
    {
        if (_window == null)
        {
            return;
        }

        if (rect is Rectangle r)
        {
            SDL_Rect sdlRect;
            sdlRect.x = r.X;
            sdlRect.y = r.Y;
            sdlRect.w = r.Width;
            sdlRect.h = r.Height;
            SDL_SetWindowMouseRect(_window, &sdlRect);
        }
        else
        {
            SDL_SetWindowMouseRect(_window, null);
        }
    }

    public void SetCursor(MouseCursor cursor)
    {
        var c = GetOrCreateCursor(cursor);
        SDL_SetCursor(c);
    }

    public void SetCursorPosition(int x, int y)
    {
        if (_window != null)
        {
            SDL_WarpMouseInWindow(_window, x, y);
        }
    }

    // IMouse: visibility/relative/capture/confine/cursor shape
    public void SetCursorVisible(bool visible)
    {
        // Explicit calls override auto-hide state.
        if (visible)
        {
            SDL_ShowCursor();
            _cursorHiddenByAuto = false;
            _hideStack = 0;
        }
        else
        {
            SDL_HideCursor();
            _cursorHiddenByAuto = false;
            _hideStack = Math.Max(_hideStack, 1);
        }
    }

    public void SetDoubleClickRadius(float pixels)
    {
        SDL_SetHint("SDL_MOUSE_DOUBLE_CLICK_RADIUS", pixels.ToString(CultureInfo.InvariantCulture));
    }

    // Double-click settings via SDL hints
    public void SetDoubleClickTime(uint milliseconds)
    {
        SDL_SetHint("SDL_MOUSE_DOUBLE_CLICK_TIME", milliseconds.ToString());
    }

    // Optional hint: click-through focuses window and delivers the click to your app
    public void SetFocusClickThrough(bool enabled)
    {
        SDL_SetHint("SDL_MOUSE_FOCUS_CLICKTHROUGH", enabled ? "1" : "0");
    }

    public void SetRelativeMouseMode(bool enabled)
    {
        if (_window == null)
        {
            return;
        }

        SDL_SetWindowRelativeMouseMode(_window, enabled);
        if (enabled && CenterWarpOnRelativeEnable)
        {
            int lw, lh;
            SDL_GetWindowSize(_window, &lw, &lh);
            SDL_WarpMouseInWindow(_window, lw / 2, lh / 2);
        }
    }

    // Snapshot of current state (consumers can read outside the input loop safely).
    public MouseSnapshot Snapshot()
    {
        return new MouseSnapshot
        {
            X = X,
            Y = Y,
            DeltaX = DeltaX,
            DeltaY = DeltaY,
            FilteredDeltaX = FilteredDeltaX,
            FilteredDeltaY = FilteredDeltaY,
            WheelX = WheelX,
            WheelY = WheelY,
            DownMask = ToMask(_down),
            PressedMask = ToMask(_pressed),
            ReleasedMask = ToMask(_released),
            IsInWindow = IsInWindow,
            IsRelativeMode = IsRelativeMouseModeEnabled
        };
    }

    // Custom cursor support (from SDL_Surface*)
    // Provide a cache key if you want to reuse later by name.
    public bool TrySetCursorFromSurface(nint surface, int hotX, int hotY, string? cacheKey = null)
    {
        if (surface == 0)
        {
            return false;
        }

        var cur = SDL_CreateColorCursor((SDL_Surface*)surface, hotX, hotY);
        if (cur == null)
        {
            return false;
        }

        SDL_SetCursor(cur);

        if (!string.IsNullOrEmpty(cacheKey))
        {
            if (_customCursorCache.TryGetValue(cacheKey!, out var old) && old != 0)
            {
                SDL_DestroyCursor((SDL_Cursor*)old);
            }

            _customCursorCache[cacheKey!] = (nint)cur;
        }
        else
        {
            if (_activeCustomCursor != 0)
            {
                SDL_DestroyCursor((SDL_Cursor*)_activeCustomCursor);
            }

            _activeCustomCursor = (nint)cur;
        }

        return true;
    }

    public bool WasButtonPressed(MouseButton button)
    {
        return _pressed.Contains(button);
    }

    public bool WasButtonReleased(MouseButton button)
    {
        return _released.Contains(button);
    }

    // Host wiring
    internal void AttachWindow(SDL_Window* window)
    {
        _window = window;
    }

    internal void BeginFrame()
    {
        DeltaX = 0f;
        DeltaY = 0f;
        _fDx = 0f;
        _fDy = 0f;
        _wheelX = 0f;
        _wheelY = 0f;
        _pressed.Clear();
        _released.Clear();
        _activityThisFrame = false;
    }

    internal void OnButtonDown(MouseButton b, byte clicks, SDL_MouseID which)
    {
        if ((IgnoreTouchMouseEvents && which == SDL_TOUCH_MOUSEID) ||
            (IgnorePenMouseEvents && which == SDL_PEN_MOUSEID))
        {
            return;
        }

        LastDeviceId = which;

        var added = _down.Add(b);
        if (added)
        {
            if (!(DebounceDoubleClick && clicks > 1))
            {
                _pressed.Add(b);
                if (clicks == 1)
                {
                    OnClick?.Invoke(b);
                }
            }

            if (clicks == 2)
            {
                OnDoubleClick?.Invoke(b);
            }

            // record drag start
            _drag[b] = (X, Y, false);

            if (AutoCaptureDuringDrag)
            {
                SDL_CaptureMouse(true);
            }
        }

        LastClickButton = b;
        LastClickCount = clicks;
        LastClickTimeSeconds = SDL_GetTicks() / 1000.0; // set timestamp (seconds)
        OnMultiClick?.Invoke(b, clicks);

        _activityThisFrame = true;
    }

    internal void OnButtonUp(MouseButton b, SDL_MouseID which)
    {
        if ((IgnoreTouchMouseEvents && which == SDL_TOUCH_MOUSEID) ||
            (IgnorePenMouseEvents && which == SDL_PEN_MOUSEID))
        {
            return;
        }

        LastDeviceId = which;

        if (_down.Remove(b))
        {
            _released.Add(b);
        }

        if (_drag.Remove(b, out var d) && d.active)
        {
            OnDragEnd?.Invoke(b);
        }

        if (AutoCaptureDuringDrag && _down.Count == 0)
        {
            SDL_CaptureMouse(false);
        }

        _activityThisFrame = true;
    }

    internal void OnCaptureLost()
    {
        _down.Clear();
        _pressed.Clear();
        _released.Clear();
        _drag.Clear();
    }

    internal void OnEnterWindow()
    {
        IsInWindow = true;
    }

    internal void OnFocusGained()
    {
        // Reset per-frame edges/deltas for a clean start
        DeltaX = 0f;
        DeltaY = 0f;
        _wheelX = 0f;
        _wheelY = 0f;
        _activityThisFrame = false;

        // Re-apply confine rect if requested
        if (AutoConfineToWindowRect)
        {
            OnWindowResized();
        }

        // Optional recentre if using relative mode and configured
        if (CenterWarpOnRelativeEnable && IsRelativeMouseModeEnabled && _window != null)
        {
            int lw, lh;
            SDL_GetWindowSize(_window, &lw, &lh);
            SDL_WarpMouseInWindow(_window, lw / 2, lh / 2);
        }
    }

    internal void OnFocusLost()
    {
        OnCaptureLost();
        if (IsMouseCaptured)
        {
            SDL_CaptureMouse(false);
        }
    }

    internal void OnLeaveWindow()
    {
        IsInWindow = false;
    }

    // Event handlers (pass SDL_MouseID which)
    internal void OnMotion(float x, float y, float dx, float dy, SDL_MouseID which)
    {
        if ((IgnoreTouchMouseEvents && which == SDL_TOUCH_MOUSEID) ||
            (IgnorePenMouseEvents && which == SDL_PEN_MOUSEID))
        {
            return;
        }

        LastDeviceId = which;

        // Apply sensitivity (optionally only in relative mode)
        var applySens = !ApplySensitivityInRelativeOnly || IsRelativeMouseModeEnabled;
        var sdx = applySens ? dx * RelativeSensitivity : dx;
        var sdy = applySens ? dy * RelativeSensitivity : dy;

        // Update position and deltas
        X = x;
        Y = y;
        DeltaX += sdx;
        DeltaY += sdy;

        if (EnableSmoothing)
        {
            var a = Math.Clamp(SmoothingFactor, 0f, 1f);
            _fDx = _fDx * a + sdx * (1f - a);
            _fDy = _fDy * a + sdy * (1f - a);
        }

        // Drag activation check
        if (_drag.Count != 0)
        {
            var thr2 = DragThresholdPixels * DragThresholdPixels;
            foreach (var b in _down)
            {
                if (_drag.TryGetValue(b, out var d) && !d.active)
                {
                    var ddx = X - d.sx;
                    var ddy = Y - d.sy;
                    if (ddx * ddx + ddy * ddy >= thr2)
                    {
                        _drag[b] = (d.sx, d.sy, true);
                        OnDragStart?.Invoke(b);
                    }
                }
            }
        }

        _activityThisFrame = true;
    }

    internal void OnWheel(float wx, float wy, SDL_MouseID which)
    {
        if ((IgnoreTouchMouseEvents && which == SDL_TOUCH_MOUSEID) ||
            (IgnorePenMouseEvents && which == SDL_PEN_MOUSEID))
        {
            return;
        }

        LastDeviceId = which;
        _wheelX += InvertWheelX ? -wx : wx;
        _wheelY += InvertWheelY ? -wy : wy;
        _activityThisFrame = true;
    }

    internal void OnWindowResized()
    {
        if (_window == null)
        {
            return;
        }

        if (AutoConfineToWindowRect)
        {
            int w, h;
            SDL_GetWindowSize(_window, &w, &h);
            var pad = AutoConfinePadding;
            SDL_Rect rect;
            rect.x = pad;
            rect.y = pad;
            rect.w = Math.Max(0, w - pad * 2);
            rect.h = Math.Max(0, h - pad * 2);
            SDL_SetWindowMouseRect(_window, &rect);
            SDL_SetWindowMouseGrab(_window, true);
        }
    }

    // Called once per frame (after PumpEvents) with total seconds
    internal void Update(double totalSeconds)
    {
        if (_activityThisFrame)
        {
            _lastActivitySeconds = totalSeconds;

            // If the cursor was auto-hidden, show it again on activity
            if (_cursorHiddenByAuto && _hideStack == 0)
            {
                SDL_ShowCursor();
                _cursorHiddenByAuto = false;
            }
        }

        if (AutoHideCursorAfterSeconds > 0)
        {
            var inactive = totalSeconds - _lastActivitySeconds;
            if (inactive >= AutoHideCursorAfterSeconds && !_cursorHiddenByAuto)
            {
                // Only hide if not explicitly hidden by the stack.
                if (_hideStack == 0)
                {
                    SDL_HideCursor();
                    _cursorHiddenByAuto = true;
                }
            }
        }
    }

    private static uint ToMask(HashSet<MouseButton> set)
    {
        uint m = 0;
        foreach (var b in set)
        {
            var bit = b switch
            {
                MouseButton.Left => 0,
                MouseButton.Right => 1,
                MouseButton.Middle => 2,
                MouseButton.X1 => 3,
                MouseButton.X2 => 4,
                _ => 0
            };
            m |= 1u << bit;
        }

        return m;
    }

    private SDL_Cursor* GetOrCreateCursor(MouseCursor cursor)
    {
        if (_cursorCache.TryGetValue(cursor, out var cur) && cur != 0)
        {
            return (SDL_Cursor*)cur;
        }

        var sys = cursor switch
        {
            MouseCursor.Default or MouseCursor.Arrow => SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT,
            MouseCursor.IBeam => SDL_SystemCursor.SDL_SYSTEM_CURSOR_TEXT,
            MouseCursor.Crosshair => SDL_SystemCursor.SDL_SYSTEM_CURSOR_CROSSHAIR,
            MouseCursor.Hand => SDL_SystemCursor.SDL_SYSTEM_CURSOR_POINTER,
            MouseCursor.ResizeNS => SDL_SystemCursor.SDL_SYSTEM_CURSOR_NS_RESIZE,
            MouseCursor.ResizeWE => SDL_SystemCursor.SDL_SYSTEM_CURSOR_EW_RESIZE,
            MouseCursor.ResizeNWSE => SDL_SystemCursor.SDL_SYSTEM_CURSOR_NWSE_RESIZE,
            MouseCursor.ResizeNESW => SDL_SystemCursor.SDL_SYSTEM_CURSOR_NESW_RESIZE,
            MouseCursor.ResizeAll => SDL_SystemCursor.SDL_SYSTEM_CURSOR_MOVE,
            MouseCursor.NotAllowed => SDL_SystemCursor.SDL_SYSTEM_CURSOR_NOT_ALLOWED,
            MouseCursor.Wait => SDL_SystemCursor.SDL_SYSTEM_CURSOR_WAIT,
            _ => SDL_SystemCursor.SDL_SYSTEM_CURSOR_DEFAULT
        };

        var created = SDL_CreateSystemCursor(sys);
        _cursorCache[cursor] = (nint)created;
        return created;
    }
}

internal struct MouseSnapshot
{
    public float X, Y;
    public float DeltaX, DeltaY;
    public float FilteredDeltaX, FilteredDeltaY;
    public float WheelX, WheelY;
    public uint DownMask, PressedMask, ReleasedMask;
    public bool IsInWindow;
    public bool IsRelativeMode;
}