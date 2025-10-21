using Brine2D.Window;
using SDL;
using static SDL.SDL3;

namespace Brine2D.Event.Messages.Mouse;

internal sealed record MousePressedMessage(double X, double Y, double Button, bool IsTouch, double Presses) : MouseMessage
{
    public static MousePressedMessage FromSDL(SDL_Event e)
    {
        var win = Module.GetInstance<WindowModule>();
        int button = e.button.button;

        button = button switch
        {
            SDL_BUTTON_RIGHT => 2,
            SDL_BUTTON_MIDDLE => 3,
            _ => button
        };

        double px = e.button.x;
        double py = e.button.y;

        (px, py) = ClampToWindow(win, px, py);
        (px, py) = WindowToDPICoords(win, px, py);
        
        return new MousePressedMessage(px, py, button, e.button.which == SDL_TOUCH_MOUSEID, e.button.clicks);
    }
}