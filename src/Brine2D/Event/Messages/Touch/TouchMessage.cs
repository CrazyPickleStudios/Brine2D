using Brine2D.Event.Messages;
using Brine2D.Touch;
using Brine2D.Window;
using SDL;

using static SDL.SDL3;

namespace Brine2D.Event.Messages.Touch;

internal abstract record TouchMessage(object Id, double X, double Y, double DX, double DY, double Pressure) : Message
{
    public static TouchMessage FromSDL(SDL_Event e)
    {
        TouchInfo touchinfo = new();
        touchinfo.id = e.tfinger.fingerID;
        touchinfo.x = e.tfinger.x;
        touchinfo.y = e.tfinger.y;
        touchinfo.dx = e.tfinger.dx;
        touchinfo.dy = e.tfinger.dy;
        touchinfo.pressure = e.tfinger.pressure;
        // TODO: touchinfo.deviceType = TouchManager.GetDeviceType(SDL_GetTouchDeviceType(e.tfinger.touchID));
        touchinfo.mouse = e.tfinger.touchID == SDL_MOUSE_TOUCHID;

        // SDL's coords are normalized to [0, 1], but we want screen coords for direct touches.
        if (touchinfo.deviceType == DeviceType.DEVICE_TOUCHSCREEN)
        {
            var win = Module.GetInstance<WindowModule>();

            (touchinfo.x, touchinfo.y) = normalizedToDPICoords(win, touchinfo.x, touchinfo.y);
            (touchinfo.dx, touchinfo.dy) = normalizedToDPICoords(win, touchinfo.dx, touchinfo.dy);
        }

        var touchmodule = Module.GetInstance<TouchModule>();

        if (touchmodule != null)
        {
            touchmodule.OnEvent(e.Type, touchinfo);
        }
        
        if (e.Type == SDL_EventType.SDL_EVENT_FINGER_DOWN)
            return new TouchPressedMessage(touchinfo.id, touchinfo.x, touchinfo.y, touchinfo.dx, touchinfo.dy, touchinfo.pressure);
        else if (e.Type == SDL_EventType.SDL_EVENT_FINGER_UP || e.Type == SDL_EventType.SDL_EVENT_FINGER_CANCELED)
            return new TouchReleasedMessage(touchinfo.id, touchinfo.x, touchinfo.y, touchinfo.dx, touchinfo.dy, touchinfo.pressure);
        else
            return new TouchMovedMessage(touchinfo.id, touchinfo.x, touchinfo.y, touchinfo.dx, touchinfo.dy, touchinfo.pressure);
        
    }

    static (double x, double y) normalizedToDPICoords(WindowModule? window, double x, double y)
    {
        // default normalized->dpi scale is 1.0 (no-op)
        double w = 1.0, h = 1.0;

        if (window != null)
        {
            // get window size in window coords, then convert those to DPI units
            w = window.GetWidth();
            h = window.GetHeight();
            (w, h) = window.WindowToDPICoords(w, h);
        }

        x = x * w;
        y = y * h;

        return (x, y);
    }
}