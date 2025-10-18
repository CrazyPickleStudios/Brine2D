using SDL;

namespace Brine2D;

internal sealed record DirectoryDroppedMessage(string Path) : Message
{
    public static DirectoryDroppedMessage FromSDL(SDL_Event e)
    {
        throw new NotImplementedException();
    }
}