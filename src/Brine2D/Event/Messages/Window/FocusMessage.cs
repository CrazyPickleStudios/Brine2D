using Brine2D.Event.Messages;
using SDL;

namespace Brine2D.Event.Messages.Window;

internal sealed record FocusMessage(bool Focus) : Message
{
    public static FocusMessage FromSDL(SDL_Event e)
    {
        return new FocusMessage(e.Type == SDL_EventType.SDL_EVENT_WINDOW_FOCUS_GAINED);
    }
}