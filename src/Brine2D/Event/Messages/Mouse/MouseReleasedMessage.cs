using Brine2D.Window;
using SDL;
using static SDL.SDL3;

namespace Brine2D.Event.Messages.Mouse;

internal sealed record MouseReleasedMessage(double X, double Y, double Button, bool IsTouch, double Presses) : MouseMessage
{
    public static MouseReleasedMessage FromSDL(SDL_Event e)
    {
        var win = Module.GetInstance<WindowModule>();
        int button = e.button.button;

        switch (button)
        {
            case SDL_BUTTON_RIGHT:
                button = 2;
                break;
            case SDL_BUTTON_MIDDLE:
                button = 3;
                break;
        }

        double px = e.button.x;
        double py = e.button.y;

        (px, py) = ClampToWindow(win, px, py);
        (px, py) = WindowToDPICoords(win, px, py);

        return new MouseReleasedMessage(px, py, button, e.button.which == SDL_TOUCH_MOUSEID, e.button.clicks);
    }

    
}