using SDL;

using static SDL.SDL3;

namespace Brine2D;

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

            normalizedToDPICoords(win, ref touchinfo.x, ref touchinfo.y);
            normalizedToDPICoords(win, ref touchinfo.dx, ref touchinfo.dy);
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

    static void normalizedToDPICoords(WindowModule? window, ref double x, ref double y)
    {
        double? w = 1.0, h = 1.0;

        if (window != null)
        {
            w = window.GetWidth();
            h = window.GetHeight();
            window.WindowToDPICoords(ref w, ref h);
        }

        if (x != null)
            x = ((x) * w.Value);
        if (y != null)
            y = ((y) * h.Value);
    }
}