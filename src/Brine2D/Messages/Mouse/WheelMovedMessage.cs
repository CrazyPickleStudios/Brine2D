using SDL;

namespace Brine2D;

internal sealed record WheelMovedMessage(double X, double Y) : Message
{
    public static WheelMovedMessage? FromSDL(SDL_Event e)
    {
        if (e.Type != SDL_EventType.SDL_EVENT_MOUSE_WHEEL)
            return null;

        double x = e.wheel.x;
        double y = e.wheel.y;
        return new WheelMovedMessage(x, y);
    }
}