using Brine2D.Event.Messages;
using SDL;

namespace Brine2D.Event.Messages.Keyboard;

internal sealed record TextInputMessage(string Text) : Message
{
    public static TextInputMessage FromSDL(SDL_Event e)
    {
        var text = e.text.GetText()!;

        return new TextInputMessage(text);
    }
}