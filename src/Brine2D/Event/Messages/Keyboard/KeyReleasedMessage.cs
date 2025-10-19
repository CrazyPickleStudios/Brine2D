using Brine2D.Event.Messages;
using Brine2D.Keyboard;
using SDL;

namespace Brine2D.Event.Messages.Keyboard;

internal sealed record KeyReleasedMessage(Key Key, Scancode Scancode) : Message
{
    public static KeyReleasedMessage FromSDL(SDL_Event e)
    {
        KeyboardModule.GetConstant(e.key.key, out var key);
        KeyboardModule.GetConstant(e.key.scancode, out var scancode);

        return new KeyReleasedMessage(key, scancode);
    }
}