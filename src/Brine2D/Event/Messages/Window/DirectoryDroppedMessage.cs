using Brine2D.Event.Messages;
using SDL;

namespace Brine2D.Event.Messages.Window;

internal sealed record DirectoryDroppedMessage(string Path) : Message
{
    public static DirectoryDroppedMessage FromSDL(SDL_Event e)
    {
        throw new NotImplementedException();
    }
}