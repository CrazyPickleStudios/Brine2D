using Brine2D.Event.Messages;
using SDL;

namespace Brine2D.Event.Messages.Window;

internal sealed record MouseFocusMessage(bool Focus) : Message
{
    public static MouseFocusMessage FromSDL(SDL_Event e)
    {
        return new MouseFocusMessage(e.Type == SDL_EventType.SDL_EVENT_WINDOW_MOUSE_ENTER);
    }
}