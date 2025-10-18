using SDL;
using static SDL.SDL3;

namespace Brine2D;

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

        double? px = (double)e.button.x;
        double? py = (double)e.button.y;

        ClampToWindow(win, ref px, ref py);
        WindowToDPICoords(win, ref px, ref py);
        
        return new MousePressedMessage(px.Value, py.Value, button, e.button.which == SDL_TOUCH_MOUSEID, e.button.clicks);
    }
}