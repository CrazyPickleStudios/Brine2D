using SDL;

namespace Brine2D;

internal sealed record MouseFocusMessage(bool Focus) : Message
{
    public static MouseFocusMessage FromSDL(SDL_Event e)
    {
        return new MouseFocusMessage(e.Type == SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER);
    }
}