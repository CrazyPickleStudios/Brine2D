using Brine2D.Window;
using SDL;
using static SDL.SDL3;

namespace Brine2D.Event.Messages.Mouse;

internal sealed record MouseMovedMessage(double X, double Y, double DX, double DY, bool IsTouch) : MouseMessage
{
    public static MouseMovedMessage FromSDL(SDL_Event e)
    {
        var win = Module.GetInstance<WindowModule>();

        double x = e.motion.x;
        double y = e.motion.y;
        double xrel = e.motion.xrel;
        double yrel = e.motion.yrel;

        (x, y) =ClampToWindow(win,  x,  y);
        (x, y) = WindowToDPICoords(win,  x,  y);
        (xrel, yrel) = WindowToDPICoords(win, xrel,  yrel);

        return new MouseMovedMessage(x, y, xrel, yrel, e.motion.which == SDL_TOUCH_MOUSEID);
    }
}