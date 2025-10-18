using Brine2D.Event.Messages;
using Brine2D.Input;
using SDL;

namespace Brine2D.Event.Messages.Keyboard;

internal sealed record KeyPressedMessage(Key Key, Scancode Scancode, bool IsRepeat) : Message
{
    public static KeyPressedMessage FromSDL(SDL_Event e)
    {
        KeyboardModule.GetConstant(e.key.key, out var key);
        KeyboardModule.GetConstant(e.key.scancode, out var scancode);

        return new KeyPressedMessage(key, scancode, e.key.repeat);
    }
}