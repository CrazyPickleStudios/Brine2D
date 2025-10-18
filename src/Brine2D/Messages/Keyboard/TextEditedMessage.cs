using SDL;

namespace Brine2D;

internal sealed record TextEditedMessage(string Text, int Start, int Length) : Message
{
    public static TextEditedMessage FromSDL(SDL_Event e)
    {
        var text = e.edit.GetText()!;
        var start = e.edit.start;
        var length = e.edit.length;

        return new TextEditedMessage(text, start, length);
    }
}