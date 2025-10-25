using Brine2D.Event.Messages;
using Brine2D.Filesystem;
using SDL;

namespace Brine2D.Event.Messages.Window;

internal sealed record FileDroppedMessage(DroppedFile File) : Message
{
    public static FileDroppedMessage FromSDL(SDL_Event e)
    {
        throw new NotImplementedException();
    }
}