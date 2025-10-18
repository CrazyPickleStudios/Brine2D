using SDL;
using static SDL.SDL3;

namespace Brine2D;

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

        double? px = (double)e.button.x;
        double? py = (double)e.button.y;

        ClampToWindow(win, ref px, ref py);
        WindowToDPICoords(win, ref px, ref py);

        return new MouseReleasedMessage(px.Value, py.Value, button, e.button.which == SDL_TOUCH_MOUSEID, e.button.clicks);
    }

    
}