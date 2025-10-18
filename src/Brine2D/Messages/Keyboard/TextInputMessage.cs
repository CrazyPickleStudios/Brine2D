using SDL;

namespace Brine2D;

internal sealed record TextInputMessage(string Text) : Message
{
    public static TextInputMessage FromSDL(SDL_Event e)
    {
        var text = e.text.GetText()!;

        return new TextInputMessage(text);
    }
}