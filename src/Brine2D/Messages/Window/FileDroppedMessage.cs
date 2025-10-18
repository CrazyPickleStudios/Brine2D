using SDL;

namespace Brine2D;

internal sealed record FileDroppedMessage(DroppedFile File) : Message
{
    public static FileDroppedMessage FromSDL(SDL_Event e)
    {
        throw new NotImplementedException();
    }
}