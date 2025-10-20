using SDL;
using static SDL.SDL3;

namespace Brine2D.Keyboard;

/// <summary>
///     Provides an interface to the user's keyboard.
/// </summary>
public unsafe sealed class KeyboardModule : Module
{
    internal KeyboardModule()
    {
        key_repeat = false;
    }

    bool key_repeat;
    
    private static Dictionary<Key, SDL_Keycode> keyToSDLKey = new()
    {
        { Key.KEY_UNKNOWN, SDL_Keycode.SDLK_UNKNOWN },

    { Key.KEY_CAPSLOCK, SDL_Keycode.SDLK_CAPSLOCK },

    { Key.KEY_F1, SDL_Keycode.SDLK_F1 },
    { Key.KEY_F2, SDL_Keycode.SDLK_F2 },
    { Key.KEY_F3, SDL_Keycode.SDLK_F3 },
    { Key.KEY_F4, SDL_Keycode.SDLK_F4 },
    { Key.KEY_F5, SDL_Keycode.SDLK_F5 },
    { Key.KEY_F6, SDL_Keycode.SDLK_F6 },
    { Key.KEY_F7, SDL_Keycode.SDLK_F7 },
    { Key.KEY_F8, SDL_Keycode.SDLK_F8 },
    { Key.KEY_F9, SDL_Keycode.SDLK_F9 },
    { Key.KEY_F10, SDL_Keycode.SDLK_F10 },
    { Key.KEY_F11, SDL_Keycode.SDLK_F11 },
    { Key.KEY_F12, SDL_Keycode.SDLK_F12 },

    { Key.KEY_PRINTSCREEN, SDL_Keycode.SDLK_PRINTSCREEN },
    { Key.KEY_SCROLLLOCK, SDL_Keycode.SDLK_SCROLLLOCK },
    { Key.KEY_PAUSE, SDL_Keycode.SDLK_PAUSE },
    { Key.KEY_INSERT, SDL_Keycode.SDLK_INSERT },
    { Key.KEY_HOME, SDL_Keycode.SDLK_HOME },
    { Key.KEY_PAGEUP, SDL_Keycode.SDLK_PAGEUP },
    { Key.KEY_DELETE, SDL_Keycode.SDLK_DELETE },
    { Key.KEY_END, SDL_Keycode.SDLK_END },
    { Key.KEY_PAGEDOWN, SDL_Keycode.SDLK_PAGEDOWN },
    { Key.KEY_RIGHT, SDL_Keycode.SDLK_RIGHT },
    { Key.KEY_LEFT, SDL_Keycode.SDLK_LEFT },
    { Key.KEY_DOWN, SDL_Keycode.SDLK_DOWN },
    { Key.KEY_UP, SDL_Keycode.SDLK_UP },

    { Key.KEY_NUMLOCKCLEAR, SDL_Keycode.SDLK_NUMLOCKCLEAR },
    { Key.KEY_KP_DIVIDE, SDL_Keycode.SDLK_KP_DIVIDE },
    { Key.KEY_KP_MULTIPLY, SDL_Keycode.SDLK_KP_MULTIPLY },
    { Key.KEY_KP_MINUS, SDL_Keycode.SDLK_KP_MINUS },
    { Key.KEY_KP_PLUS, SDL_Keycode.SDLK_KP_PLUS },
    { Key.KEY_KP_ENTER, SDL_Keycode.SDLK_KP_ENTER },
    { Key.KEY_KP_0, SDL_Keycode.SDLK_KP_0 },
    { Key.KEY_KP_1, SDL_Keycode.SDLK_KP_1 },
    { Key.KEY_KP_2, SDL_Keycode.SDLK_KP_2 },
    { Key.KEY_KP_3, SDL_Keycode.SDLK_KP_3 },
    { Key.KEY_KP_4, SDL_Keycode.SDLK_KP_4 },
    { Key.KEY_KP_5, SDL_Keycode.SDLK_KP_5 },
    { Key.KEY_KP_6, SDL_Keycode.SDLK_KP_6 },
    { Key.KEY_KP_7, SDL_Keycode.SDLK_KP_7 },
    { Key.KEY_KP_8, SDL_Keycode.SDLK_KP_8 },
    { Key.KEY_KP_9, SDL_Keycode.SDLK_KP_9 },
    { Key.KEY_KP_PERIOD, SDL_Keycode.SDLK_KP_PERIOD },
    { Key.KEY_KP_COMMA, SDL_Keycode.SDLK_KP_COMMA },
    { Key.KEY_KP_EQUALS, SDL_Keycode.SDLK_KP_EQUALS },

    { Key.KEY_APPLICATION, SDL_Keycode.SDLK_APPLICATION },
    { Key.KEY_POWER, SDL_Keycode.SDLK_POWER },
    { Key.KEY_F13, SDL_Keycode.SDLK_F13 },
    { Key.KEY_F14, SDL_Keycode.SDLK_F14 },
    { Key.KEY_F15, SDL_Keycode.SDLK_F15 },
    { Key.KEY_F16, SDL_Keycode.SDLK_F16 },
    { Key.KEY_F17, SDL_Keycode.SDLK_F17 },
    { Key.KEY_F18, SDL_Keycode.SDLK_F18 },
    { Key.KEY_F19, SDL_Keycode.SDLK_F19 },
    { Key.KEY_F20, SDL_Keycode.SDLK_F20 },
    { Key.KEY_F21, SDL_Keycode.SDLK_F21 },
    { Key.KEY_F22, SDL_Keycode.SDLK_F22 },
    { Key.KEY_F23, SDL_Keycode.SDLK_F23 },
    { Key.KEY_F24, SDL_Keycode.SDLK_F24 },
    { Key.KEY_EXECUTE, SDL_Keycode.SDLK_EXECUTE },
    { Key.KEY_HELP, SDL_Keycode.SDLK_HELP },
    { Key.KEY_MENU, SDL_Keycode.SDLK_MENU },
    { Key.KEY_SELECT, SDL_Keycode.SDLK_SELECT },
    { Key.KEY_STOP, SDL_Keycode.SDLK_STOP },
    { Key.KEY_AGAIN, SDL_Keycode.SDLK_AGAIN },
    { Key.KEY_UNDO, SDL_Keycode.SDLK_UNDO },
    { Key.KEY_CUT, SDL_Keycode.SDLK_CUT },
    { Key.KEY_COPY, SDL_Keycode.SDLK_COPY },
    { Key.KEY_PASTE, SDL_Keycode.SDLK_PASTE },
    { Key.KEY_FIND, SDL_Keycode.SDLK_FIND },
    { Key.KEY_MUTE, SDL_Keycode.SDLK_MUTE },
    { Key.KEY_VOLUMEUP, SDL_Keycode.SDLK_VOLUMEUP },
    { Key.KEY_VOLUMEDOWN, SDL_Keycode.SDLK_VOLUMEDOWN },

    { Key.KEY_ALTERASE, SDL_Keycode.SDLK_ALTERASE },
    { Key.KEY_SYSREQ, SDL_Keycode.SDLK_SYSREQ },
    { Key.KEY_CANCEL, SDL_Keycode.SDLK_CANCEL },
    { Key.KEY_CLEAR, SDL_Keycode.SDLK_CLEAR },
    { Key.KEY_PRIOR, SDL_Keycode.SDLK_PRIOR },
    { Key.KEY_RETURN2, SDL_Keycode.SDLK_RETURN2 },
    { Key.KEY_SEPARATOR, SDL_Keycode.SDLK_SEPARATOR },
    { Key.KEY_OUT, SDL_Keycode.SDLK_OUT },
    { Key.KEY_OPER, SDL_Keycode.SDLK_OPER },
    { Key.KEY_CLEARAGAIN, SDL_Keycode.SDLK_CLEARAGAIN },

    { Key.KEY_THOUSANDSSEPARATOR, SDL_Keycode.SDLK_THOUSANDSSEPARATOR },
    { Key.KEY_DECIMALSEPARATOR, SDL_Keycode.SDLK_DECIMALSEPARATOR },
    { Key.KEY_CURRENCYUNIT, SDL_Keycode.SDLK_CURRENCYUNIT },
    { Key.KEY_CURRENCYSUBUNIT, SDL_Keycode.SDLK_CURRENCYSUBUNIT },

    { Key.KEY_LCTRL, SDL_Keycode.SDLK_LCTRL },
    { Key.KEY_LSHIFT, SDL_Keycode.SDLK_LSHIFT },
    { Key.KEY_LALT, SDL_Keycode.SDLK_LALT },
    { Key.KEY_LGUI, SDL_Keycode.SDLK_LGUI },
    { Key.KEY_RCTRL, SDL_Keycode.SDLK_RCTRL },
    { Key.KEY_RSHIFT, SDL_Keycode.SDLK_RSHIFT },
    { Key.KEY_RALT, SDL_Keycode.SDLK_RALT },
    { Key.KEY_RGUI, SDL_Keycode.SDLK_RGUI },

    { Key.KEY_MODE, SDL_Keycode.SDLK_MODE },

    { Key.KEY_AUDIONEXT, SDL_Keycode.SDLK_MEDIA_NEXT_TRACK },
    { Key.KEY_AUDIOPREV, SDL_Keycode.SDLK_MEDIA_PREVIOUS_TRACK },
    { Key.KEY_AUDIOSTOP, SDL_Keycode.SDLK_MEDIA_STOP },
    { Key.KEY_AUDIOPLAY, SDL_Keycode.SDLK_MEDIA_PLAY },
    { Key.KEY_AUDIOMUTE, SDL_Keycode.SDLK_MUTE },
    { Key.KEY_MEDIASELECT, SDL_Keycode.SDLK_MEDIA_SELECT },
    { Key.KEY_APP_SEARCH, SDL_Keycode.SDLK_AC_SEARCH },
    { Key.KEY_APP_HOME, SDL_Keycode.SDLK_AC_HOME },
    { Key.KEY_APP_BACK, SDL_Keycode.SDLK_AC_BACK },
    { Key.KEY_APP_FORWARD, SDL_Keycode.SDLK_AC_FORWARD },
    { Key.KEY_APP_STOP, SDL_Keycode.SDLK_AC_STOP },
    { Key.KEY_APP_REFRESH, SDL_Keycode.SDLK_AC_REFRESH },
    { Key.KEY_APP_BOOKMARKS, SDL_Keycode.SDLK_AC_BOOKMARKS },

    { Key.KEY_EJECT, SDL_Keycode.SDLK_MEDIA_EJECT },
    { Key.KEY_SLEEP, SDL_Keycode.SDLK_SLEEP },
    };

    private static Dictionary<SDL_Keycode, Key> sdlKeyToKey = new();

    private static readonly EnumMap<Scancode, SDL_Scancode>.Entry[] scancodeEntries =
    [
        new(Scancode.SCANCODE_UNKNOWN, SDL_Scancode.SDL_SCANCODE_UNKNOWN),

        new(Scancode.SCANCODE_A, SDL_Scancode.SDL_SCANCODE_A),
        new(Scancode.SCANCODE_B, SDL_Scancode.SDL_SCANCODE_B),
        new(Scancode.SCANCODE_C, SDL_Scancode.SDL_SCANCODE_C),
        new(Scancode.SCANCODE_D, SDL_Scancode.SDL_SCANCODE_D),
        new(Scancode.SCANCODE_E, SDL_Scancode.SDL_SCANCODE_E),
        new(Scancode.SCANCODE_F, SDL_Scancode.SDL_SCANCODE_F),
        new(Scancode.SCANCODE_G, SDL_Scancode.SDL_SCANCODE_G),
        new(Scancode.SCANCODE_H, SDL_Scancode.SDL_SCANCODE_H),
        new(Scancode.SCANCODE_I, SDL_Scancode.SDL_SCANCODE_I),
        new(Scancode.SCANCODE_J, SDL_Scancode.SDL_SCANCODE_J),
        new(Scancode.SCANCODE_K, SDL_Scancode.SDL_SCANCODE_K),
        new(Scancode.SCANCODE_L, SDL_Scancode.SDL_SCANCODE_L),
        new(Scancode.SCANCODE_M, SDL_Scancode.SDL_SCANCODE_M),
        new(Scancode.SCANCODE_N, SDL_Scancode.SDL_SCANCODE_N),
        new(Scancode.SCANCODE_O, SDL_Scancode.SDL_SCANCODE_O),
        new(Scancode.SCANCODE_P, SDL_Scancode.SDL_SCANCODE_P),
        new(Scancode.SCANCODE_Q, SDL_Scancode.SDL_SCANCODE_Q),
        new(Scancode.SCANCODE_R, SDL_Scancode.SDL_SCANCODE_R),
        new(Scancode.SCANCODE_S, SDL_Scancode.SDL_SCANCODE_S),
        new(Scancode.SCANCODE_T, SDL_Scancode.SDL_SCANCODE_T),
        new(Scancode.SCANCODE_U, SDL_Scancode.SDL_SCANCODE_U),
        new(Scancode.SCANCODE_V, SDL_Scancode.SDL_SCANCODE_V),
        new(Scancode.SCANCODE_W, SDL_Scancode.SDL_SCANCODE_W),
        new(Scancode.SCANCODE_X, SDL_Scancode.SDL_SCANCODE_X),
        new(Scancode.SCANCODE_Y, SDL_Scancode.SDL_SCANCODE_Y),
        new(Scancode.SCANCODE_Z, SDL_Scancode.SDL_SCANCODE_Z),

        new(Scancode.SCANCODE_1, SDL_Scancode.SDL_SCANCODE_1),
        new(Scancode.SCANCODE_2, SDL_Scancode.SDL_SCANCODE_2),
        new(Scancode.SCANCODE_3, SDL_Scancode.SDL_SCANCODE_3),
        new(Scancode.SCANCODE_4, SDL_Scancode.SDL_SCANCODE_4),
        new(Scancode.SCANCODE_5, SDL_Scancode.SDL_SCANCODE_5),
        new(Scancode.SCANCODE_6, SDL_Scancode.SDL_SCANCODE_6),
        new(Scancode.SCANCODE_7, SDL_Scancode.SDL_SCANCODE_7),
        new(Scancode.SCANCODE_8, SDL_Scancode.SDL_SCANCODE_8),
        new(Scancode.SCANCODE_9, SDL_Scancode.SDL_SCANCODE_9),
        new(Scancode.SCANCODE_0, SDL_Scancode.SDL_SCANCODE_0),

        new(Scancode.SCANCODE_RETURN, SDL_Scancode.SDL_SCANCODE_RETURN),
        new(Scancode.SCANCODE_ESCAPE, SDL_Scancode.SDL_SCANCODE_ESCAPE),
        new(Scancode.SCANCODE_BACKSPACE, SDL_Scancode.SDL_SCANCODE_BACKSPACE),
        new(Scancode.SCANCODE_TAB, SDL_Scancode.SDL_SCANCODE_TAB),
        new(Scancode.SCANCODE_SPACE, SDL_Scancode.SDL_SCANCODE_SPACE),

        new(Scancode.SCANCODE_MINUS, SDL_Scancode.SDL_SCANCODE_MINUS),
        new(Scancode.SCANCODE_EQUALS, SDL_Scancode.SDL_SCANCODE_EQUALS),
        new(Scancode.SCANCODE_LEFTBRACKET, SDL_Scancode.SDL_SCANCODE_LEFTBRACKET),
        new(Scancode.SCANCODE_RIGHTBRACKET, SDL_Scancode.SDL_SCANCODE_RIGHTBRACKET),
        new(Scancode.SCANCODE_BACKSLASH, SDL_Scancode.SDL_SCANCODE_BACKSLASH),
        new(Scancode.SCANCODE_NONUSHASH, SDL_Scancode.SDL_SCANCODE_NONUSHASH),
        new(Scancode.SCANCODE_SEMICOLON, SDL_Scancode.SDL_SCANCODE_SEMICOLON),
        new(Scancode.SCANCODE_APOSTROPHE, SDL_Scancode.SDL_SCANCODE_APOSTROPHE),
        new(Scancode.SCANCODE_GRAVE, SDL_Scancode.SDL_SCANCODE_GRAVE),
        new(Scancode.SCANCODE_COMMA, SDL_Scancode.SDL_SCANCODE_COMMA),
        new(Scancode.SCANCODE_PERIOD, SDL_Scancode.SDL_SCANCODE_PERIOD),
        new(Scancode.SCANCODE_SLASH, SDL_Scancode.SDL_SCANCODE_SLASH),

        new(Scancode.SCANCODE_CAPSLOCK, SDL_Scancode.SDL_SCANCODE_CAPSLOCK),

        new(Scancode.SCANCODE_F1, SDL_Scancode.SDL_SCANCODE_F1),
        new(Scancode.SCANCODE_F2, SDL_Scancode.SDL_SCANCODE_F2),
        new(Scancode.SCANCODE_F3, SDL_Scancode.SDL_SCANCODE_F3),
        new(Scancode.SCANCODE_F4, SDL_Scancode.SDL_SCANCODE_F4),
        new(Scancode.SCANCODE_F5, SDL_Scancode.SDL_SCANCODE_F5),
        new(Scancode.SCANCODE_F6, SDL_Scancode.SDL_SCANCODE_F6),
        new(Scancode.SCANCODE_F7, SDL_Scancode.SDL_SCANCODE_F7),
        new(Scancode.SCANCODE_F8, SDL_Scancode.SDL_SCANCODE_F8),
        new(Scancode.SCANCODE_F9, SDL_Scancode.SDL_SCANCODE_F9),
        new(Scancode.SCANCODE_F10, SDL_Scancode.SDL_SCANCODE_F10),
        new(Scancode.SCANCODE_F11, SDL_Scancode.SDL_SCANCODE_F11),
        new(Scancode.SCANCODE_F12, SDL_Scancode.SDL_SCANCODE_F12),

        new(Scancode.SCANCODE_PRINTSCREEN, SDL_Scancode.SDL_SCANCODE_PRINTSCREEN),
        new(Scancode.SCANCODE_SCROLLLOCK, SDL_Scancode.SDL_SCANCODE_SCROLLLOCK),
        new(Scancode.SCANCODE_PAUSE, SDL_Scancode.SDL_SCANCODE_PAUSE),
        new(Scancode.SCANCODE_INSERT, SDL_Scancode.SDL_SCANCODE_INSERT),
        new(Scancode.SCANCODE_HOME, SDL_Scancode.SDL_SCANCODE_HOME),
        new(Scancode.SCANCODE_PAGEUP, SDL_Scancode.SDL_SCANCODE_PAGEUP),
        new(Scancode.SCANCODE_DELETE, SDL_Scancode.SDL_SCANCODE_DELETE),
        new(Scancode.SCANCODE_END, SDL_Scancode.SDL_SCANCODE_END),
        new(Scancode.SCANCODE_PAGEDOWN, SDL_Scancode.SDL_SCANCODE_PAGEDOWN),
        new(Scancode.SCANCODE_RIGHT, SDL_Scancode.SDL_SCANCODE_RIGHT),
        new(Scancode.SCANCODE_LEFT, SDL_Scancode.SDL_SCANCODE_LEFT),
        new(Scancode.SCANCODE_DOWN, SDL_Scancode.SDL_SCANCODE_DOWN),
        new(Scancode.SCANCODE_UP, SDL_Scancode.SDL_SCANCODE_UP),

        new(Scancode.SCANCODE_NUMLOCKCLEAR, SDL_Scancode.SDL_SCANCODE_NUMLOCKCLEAR),
        new(Scancode.SCANCODE_KP_DIVIDE, SDL_Scancode.SDL_SCANCODE_KP_DIVIDE),
        new(Scancode.SCANCODE_KP_MULTIPLY, SDL_Scancode.SDL_SCANCODE_KP_MULTIPLY),
        new(Scancode.SCANCODE_KP_MINUS, SDL_Scancode.SDL_SCANCODE_KP_MINUS),
        new(Scancode.SCANCODE_KP_PLUS, SDL_Scancode.SDL_SCANCODE_KP_PLUS),
        new(Scancode.SCANCODE_KP_ENTER, SDL_Scancode.SDL_SCANCODE_KP_ENTER),
        new(Scancode.SCANCODE_KP_1, SDL_Scancode.SDL_SCANCODE_KP_1),
        new(Scancode.SCANCODE_KP_2, SDL_Scancode.SDL_SCANCODE_KP_2),
        new(Scancode.SCANCODE_KP_3, SDL_Scancode.SDL_SCANCODE_KP_3),
        new(Scancode.SCANCODE_KP_4, SDL_Scancode.SDL_SCANCODE_KP_4),
        new(Scancode.SCANCODE_KP_5, SDL_Scancode.SDL_SCANCODE_KP_5),
        new(Scancode.SCANCODE_KP_6, SDL_Scancode.SDL_SCANCODE_KP_6),
        new(Scancode.SCANCODE_KP_7, SDL_Scancode.SDL_SCANCODE_KP_7),
        new(Scancode.SCANCODE_KP_8, SDL_Scancode.SDL_SCANCODE_KP_8),
        new(Scancode.SCANCODE_KP_9, SDL_Scancode.SDL_SCANCODE_KP_9),
        new(Scancode.SCANCODE_KP_0, SDL_Scancode.SDL_SCANCODE_KP_0),
        new(Scancode.SCANCODE_KP_PERIOD, SDL_Scancode.SDL_SCANCODE_KP_PERIOD),

        new(Scancode.SCANCODE_NONUSBACKSLASH, SDL_Scancode.SDL_SCANCODE_NONUSBACKSLASH),
        new(Scancode.SCANCODE_APPLICATION, SDL_Scancode.SDL_SCANCODE_APPLICATION),
        new(Scancode.SCANCODE_POWER, SDL_Scancode.SDL_SCANCODE_POWER),
        new(Scancode.SCANCODE_KP_EQUALS, SDL_Scancode.SDL_SCANCODE_KP_EQUALS),
        new(Scancode.SCANCODE_F13, SDL_Scancode.SDL_SCANCODE_F13),
        new(Scancode.SCANCODE_F14, SDL_Scancode.SDL_SCANCODE_F14),
        new(Scancode.SCANCODE_F15, SDL_Scancode.SDL_SCANCODE_F15),
        new(Scancode.SCANCODE_F16, SDL_Scancode.SDL_SCANCODE_F16),
        new(Scancode.SCANCODE_F17, SDL_Scancode.SDL_SCANCODE_F17),
        new(Scancode.SCANCODE_F18, SDL_Scancode.SDL_SCANCODE_F18),
        new(Scancode.SCANCODE_F19, SDL_Scancode.SDL_SCANCODE_F19),
        new(Scancode.SCANCODE_F20, SDL_Scancode.SDL_SCANCODE_F20),
        new(Scancode.SCANCODE_F21, SDL_Scancode.SDL_SCANCODE_F21),
        new(Scancode.SCANCODE_F22, SDL_Scancode.SDL_SCANCODE_F22),
        new(Scancode.SCANCODE_F23, SDL_Scancode.SDL_SCANCODE_F23),
        new(Scancode.SCANCODE_F24, SDL_Scancode.SDL_SCANCODE_F24),
        new(Scancode.SCANCODE_EXECUTE, SDL_Scancode.SDL_SCANCODE_EXECUTE),
        new(Scancode.SCANCODE_HELP, SDL_Scancode.SDL_SCANCODE_HELP),
        new(Scancode.SCANCODE_MENU, SDL_Scancode.SDL_SCANCODE_MENU),
        new(Scancode.SCANCODE_SELECT, SDL_Scancode.SDL_SCANCODE_SELECT),
        new(Scancode.SCANCODE_STOP, SDL_Scancode.SDL_SCANCODE_STOP),
        new(Scancode.SCANCODE_AGAIN, SDL_Scancode.SDL_SCANCODE_AGAIN),
        new(Scancode.SCANCODE_UNDO, SDL_Scancode.SDL_SCANCODE_UNDO),
        new(Scancode.SCANCODE_CUT, SDL_Scancode.SDL_SCANCODE_CUT),
        new(Scancode.SCANCODE_COPY, SDL_Scancode.SDL_SCANCODE_COPY),
        new(Scancode.SCANCODE_PASTE, SDL_Scancode.SDL_SCANCODE_PASTE),
        new(Scancode.SCANCODE_FIND, SDL_Scancode.SDL_SCANCODE_FIND),
        new(Scancode.SCANCODE_MUTE, SDL_Scancode.SDL_SCANCODE_MUTE),
        new(Scancode.SCANCODE_VOLUMEUP, SDL_Scancode.SDL_SCANCODE_VOLUMEUP),
        new(Scancode.SCANCODE_VOLUMEDOWN, SDL_Scancode.SDL_SCANCODE_VOLUMEDOWN),
        new(Scancode.SCANCODE_KP_COMMA, SDL_Scancode.SDL_SCANCODE_KP_COMMA),
        new(Scancode.SCANCODE_KP_EQUALSAS400, SDL_Scancode.SDL_SCANCODE_KP_EQUALSAS400),

        new(Scancode.SCANCODE_INTERNATIONAL1, SDL_Scancode.SDL_SCANCODE_INTERNATIONAL1),
        new(Scancode.SCANCODE_INTERNATIONAL2, SDL_Scancode.SDL_SCANCODE_INTERNATIONAL2),
        new(Scancode.SCANCODE_INTERNATIONAL3, SDL_Scancode.SDL_SCANCODE_INTERNATIONAL3),
        new(Scancode.SCANCODE_INTERNATIONAL4, SDL_Scancode.SDL_SCANCODE_INTERNATIONAL4),
        new(Scancode.SCANCODE_INTERNATIONAL5, SDL_Scancode.SDL_SCANCODE_INTERNATIONAL5),
        new(Scancode.SCANCODE_INTERNATIONAL6, SDL_Scancode.SDL_SCANCODE_INTERNATIONAL6),
        new(Scancode.SCANCODE_INTERNATIONAL7, SDL_Scancode.SDL_SCANCODE_INTERNATIONAL7),
        new(Scancode.SCANCODE_INTERNATIONAL8, SDL_Scancode.SDL_SCANCODE_INTERNATIONAL8),
        new(Scancode.SCANCODE_INTERNATIONAL9, SDL_Scancode.SDL_SCANCODE_INTERNATIONAL9),
        new(Scancode.SCANCODE_LANG1, SDL_Scancode.SDL_SCANCODE_LANG1),
        new(Scancode.SCANCODE_LANG2, SDL_Scancode.SDL_SCANCODE_LANG2),
        new(Scancode.SCANCODE_LANG3, SDL_Scancode.SDL_SCANCODE_LANG3),
        new(Scancode.SCANCODE_LANG4, SDL_Scancode.SDL_SCANCODE_LANG4),
        new(Scancode.SCANCODE_LANG5, SDL_Scancode.SDL_SCANCODE_LANG5),
        new(Scancode.SCANCODE_LANG6, SDL_Scancode.SDL_SCANCODE_LANG6),
        new(Scancode.SCANCODE_LANG7, SDL_Scancode.SDL_SCANCODE_LANG7),
        new(Scancode.SCANCODE_LANG8, SDL_Scancode.SDL_SCANCODE_LANG8),
        new(Scancode.SCANCODE_LANG9, SDL_Scancode.SDL_SCANCODE_LANG9),

        new(Scancode.SCANCODE_ALTERASE, SDL_Scancode.SDL_SCANCODE_ALTERASE),
        new(Scancode.SCANCODE_SYSREQ, SDL_Scancode.SDL_SCANCODE_SYSREQ),
        new(Scancode.SCANCODE_CANCEL, SDL_Scancode.SDL_SCANCODE_CANCEL),
        new(Scancode.SCANCODE_CLEAR, SDL_Scancode.SDL_SCANCODE_CLEAR),
        new(Scancode.SCANCODE_PRIOR, SDL_Scancode.SDL_SCANCODE_PRIOR),
        new(Scancode.SCANCODE_RETURN2, SDL_Scancode.SDL_SCANCODE_RETURN2),
        new(Scancode.SCANCODE_SEPARATOR, SDL_Scancode.SDL_SCANCODE_SEPARATOR),
        new(Scancode.SCANCODE_OUT, SDL_Scancode.SDL_SCANCODE_OUT),
        new(Scancode.SCANCODE_OPER, SDL_Scancode.SDL_SCANCODE_OPER),
        new(Scancode.SCANCODE_CLEARAGAIN, SDL_Scancode.SDL_SCANCODE_CLEARAGAIN),
        new(Scancode.SCANCODE_CRSEL, SDL_Scancode.SDL_SCANCODE_CRSEL),
        new(Scancode.SCANCODE_EXSEL, SDL_Scancode.SDL_SCANCODE_EXSEL),

        new(Scancode.SCANCODE_KP_00, SDL_Scancode.SDL_SCANCODE_KP_00),
        new(Scancode.SCANCODE_KP_000, SDL_Scancode.SDL_SCANCODE_KP_000),
        new(Scancode.SCANCODE_THOUSANDSSEPARATOR, SDL_Scancode.SDL_SCANCODE_THOUSANDSSEPARATOR),
        new(Scancode.SCANCODE_DECIMALSEPARATOR, SDL_Scancode.SDL_SCANCODE_DECIMALSEPARATOR),
        new(Scancode.SCANCODE_CURRENCYUNIT, SDL_Scancode.SDL_SCANCODE_CURRENCYUNIT),
        new(Scancode.SCANCODE_CURRENCYSUBUNIT, SDL_Scancode.SDL_SCANCODE_CURRENCYSUBUNIT),
        new(Scancode.SCANCODE_KP_LEFTPAREN, SDL_Scancode.SDL_SCANCODE_KP_LEFTPAREN),
        new(Scancode.SCANCODE_KP_RIGHTPAREN, SDL_Scancode.SDL_SCANCODE_KP_RIGHTPAREN),
        new(Scancode.SCANCODE_KP_LEFTBRACE, SDL_Scancode.SDL_SCANCODE_KP_LEFTBRACE),
        new(Scancode.SCANCODE_KP_RIGHTBRACE, SDL_Scancode.SDL_SCANCODE_KP_RIGHTBRACE),
        new(Scancode.SCANCODE_KP_TAB, SDL_Scancode.SDL_SCANCODE_KP_TAB),
        new(Scancode.SCANCODE_KP_BACKSPACE, SDL_Scancode.SDL_SCANCODE_KP_BACKSPACE),
        new(Scancode.SCANCODE_KP_A, SDL_Scancode.SDL_SCANCODE_KP_A),
        new(Scancode.SCANCODE_KP_B, SDL_Scancode.SDL_SCANCODE_KP_B),
        new(Scancode.SCANCODE_KP_C, SDL_Scancode.SDL_SCANCODE_KP_C),
        new(Scancode.SCANCODE_KP_D, SDL_Scancode.SDL_SCANCODE_KP_D),
        new(Scancode.SCANCODE_KP_E, SDL_Scancode.SDL_SCANCODE_KP_E),
        new(Scancode.SCANCODE_KP_F, SDL_Scancode.SDL_SCANCODE_KP_F),
        new(Scancode.SCANCODE_KP_XOR, SDL_Scancode.SDL_SCANCODE_KP_XOR),
        new(Scancode.SCANCODE_KP_POWER, SDL_Scancode.SDL_SCANCODE_KP_POWER),
        new(Scancode.SCANCODE_KP_PERCENT, SDL_Scancode.SDL_SCANCODE_KP_PERCENT),
        new(Scancode.SCANCODE_KP_LESS, SDL_Scancode.SDL_SCANCODE_KP_LESS),
        new(Scancode.SCANCODE_KP_GREATER, SDL_Scancode.SDL_SCANCODE_KP_GREATER),
        new(Scancode.SCANCODE_KP_AMPERSAND, SDL_Scancode.SDL_SCANCODE_KP_AMPERSAND),
        new(Scancode.SCANCODE_KP_DBLAMPERSAND, SDL_Scancode.SDL_SCANCODE_KP_DBLAMPERSAND),
        new(Scancode.SCANCODE_KP_VERTICALBAR, SDL_Scancode.SDL_SCANCODE_KP_VERTICALBAR),
        new(Scancode.SCANCODE_KP_DBLVERTICALBAR, SDL_Scancode.SDL_SCANCODE_KP_DBLVERTICALBAR),
        new(Scancode.SCANCODE_KP_COLON, SDL_Scancode.SDL_SCANCODE_KP_COLON),
        new(Scancode.SCANCODE_KP_HASH, SDL_Scancode.SDL_SCANCODE_KP_HASH),
        new(Scancode.SCANCODE_KP_SPACE, SDL_Scancode.SDL_SCANCODE_KP_SPACE),
        new(Scancode.SCANCODE_KP_AT, SDL_Scancode.SDL_SCANCODE_KP_AT),
        new(Scancode.SCANCODE_KP_EXCLAM, SDL_Scancode.SDL_SCANCODE_KP_EXCLAM),
        new(Scancode.SCANCODE_KP_MEMSTORE, SDL_Scancode.SDL_SCANCODE_KP_MEMSTORE),
        new(Scancode.SCANCODE_KP_MEMRECALL, SDL_Scancode.SDL_SCANCODE_KP_MEMRECALL),
        new(Scancode.SCANCODE_KP_MEMCLEAR, SDL_Scancode.SDL_SCANCODE_KP_MEMCLEAR),
        new(Scancode.SCANCODE_KP_MEMADD, SDL_Scancode.SDL_SCANCODE_KP_MEMADD),
        new(Scancode.SCANCODE_KP_MEMSUBTRACT, SDL_Scancode.SDL_SCANCODE_KP_MEMSUBTRACT),
        new(Scancode.SCANCODE_KP_MEMMULTIPLY, SDL_Scancode.SDL_SCANCODE_KP_MEMMULTIPLY),
        new(Scancode.SCANCODE_KP_MEMDIVIDE, SDL_Scancode.SDL_SCANCODE_KP_MEMDIVIDE),
        new(Scancode.SCANCODE_KP_PLUSMINUS, SDL_Scancode.SDL_SCANCODE_KP_PLUSMINUS),
        new(Scancode.SCANCODE_KP_CLEAR, SDL_Scancode.SDL_SCANCODE_KP_CLEAR),
        new(Scancode.SCANCODE_KP_CLEARENTRY, SDL_Scancode.SDL_SCANCODE_KP_CLEARENTRY),
        new(Scancode.SCANCODE_KP_BINARY, SDL_Scancode.SDL_SCANCODE_KP_BINARY),
        new(Scancode.SCANCODE_KP_OCTAL, SDL_Scancode.SDL_SCANCODE_KP_OCTAL),
        new(Scancode.SCANCODE_KP_DECIMAL, SDL_Scancode.SDL_SCANCODE_KP_DECIMAL),
        new(Scancode.SCANCODE_KP_HEXADECIMAL, SDL_Scancode.SDL_SCANCODE_KP_HEXADECIMAL),

        new(Scancode.SCANCODE_LCTRL, SDL_Scancode.SDL_SCANCODE_LCTRL),
        new(Scancode.SCANCODE_LSHIFT, SDL_Scancode.SDL_SCANCODE_LSHIFT),
        new(Scancode.SCANCODE_LALT, SDL_Scancode.SDL_SCANCODE_LALT),
        new(Scancode.SCANCODE_LGUI, SDL_Scancode.SDL_SCANCODE_LGUI),
        new(Scancode.SCANCODE_RCTRL, SDL_Scancode.SDL_SCANCODE_RCTRL),
        new(Scancode.SCANCODE_RSHIFT, SDL_Scancode.SDL_SCANCODE_RSHIFT),
        new(Scancode.SCANCODE_RALT, SDL_Scancode.SDL_SCANCODE_RALT),
        new(Scancode.SCANCODE_RGUI, SDL_Scancode.SDL_SCANCODE_RGUI),

        new(Scancode.SCANCODE_MODE, SDL_Scancode.SDL_SCANCODE_MODE),

        new(Scancode.SCANCODE_AUDIONEXT, SDL_Scancode.SDL_SCANCODE_MEDIA_NEXT_TRACK),
        new(Scancode.SCANCODE_AUDIOPREV, SDL_Scancode.SDL_SCANCODE_MEDIA_PREVIOUS_TRACK),
        new(Scancode.SCANCODE_AUDIOSTOP, SDL_Scancode.SDL_SCANCODE_MEDIA_STOP),
        new(Scancode.SCANCODE_AUDIOPLAY, SDL_Scancode.SDL_SCANCODE_MEDIA_PLAY),
        new(Scancode.SCANCODE_AUDIOMUTE, SDL_Scancode.SDL_SCANCODE_MUTE),
        new(Scancode.SCANCODE_MEDIASELECT, SDL_Scancode.SDL_SCANCODE_MEDIA_SELECT),
        new(Scancode.SCANCODE_AC_SEARCH, SDL_Scancode.SDL_SCANCODE_AC_SEARCH),
        new(Scancode.SCANCODE_AC_HOME, SDL_Scancode.SDL_SCANCODE_AC_HOME),
        new(Scancode.SCANCODE_AC_BACK, SDL_Scancode.SDL_SCANCODE_AC_BACK),
        new(Scancode.SCANCODE_AC_FORWARD, SDL_Scancode.SDL_SCANCODE_AC_FORWARD),
        new(Scancode.SCANCODE_AC_STOP, SDL_Scancode.SDL_SCANCODE_AC_STOP),
        new(Scancode.SCANCODE_AC_REFRESH, SDL_Scancode.SDL_SCANCODE_AC_REFRESH),
        new(Scancode.SCANCODE_AC_BOOKMARKS, SDL_Scancode.SDL_SCANCODE_AC_BOOKMARKS),

        new(Scancode.SCANCODE_EJECT, SDL_Scancode.SDL_SCANCODE_MEDIA_EJECT),
        new(Scancode.SCANCODE_SLEEP, SDL_Scancode.SDL_SCANCODE_SLEEP)
    ];

    private static readonly EnumMap<Scancode, SDL_Scancode> scancodes =
        new(scancodeEntries, (int)SDL_Scancode.SDL_SCANCODE_COUNT);
    
    /// <summary>
    ///     <para>Gets the key corresponding to the given hardware scancode.</para>
    ///     <para>
    ///         Unlike key constants, Scancodes are keyboard layout-independent. For example the scancode "w" will be generated
    ///         if the key in the same place as the "w" key on an American keyboard is pressed, no matter what the key is
    ///         labelled or what the user's operating system settings are.
    ///     </para>
    ///     <para>Scancodes are useful for creating default controls that have the same physical locations on all systems.</para>
    /// </summary>
    /// <param name="scancode">The scancode to get the key from.</param>
    /// <returns>
    ///     The key corresponding to the given scancode, or "unknown" if the scancode doesn't map to a KeyConstant on the
    ///     current system.
    /// </returns>
    public Key GetKeyFromScancode(Scancode scancode)
    {
        scancodes.TryGetValue(scancode, out var sdlscancode);

        var sdlKey = SDL_GetKeyFromScancode(sdlscancode, SDL_Keymod.SDL_KMOD_NONE, false);

        GetConstant(sdlKey, out var key);
        return key;
    }

    static bool GetConstant(Scancode @in, out SDL_Scancode @out)
    {
        return scancodes.TryGetValue(@in, out @out);
    }

    internal static bool GetConstant(SDL_Scancode @in, out Scancode @out)
    {
        return scancodes.TryGetValue(@in, out @out);
    }

    static bool GetConstant(Key @in, out SDL_Keycode @out)
    {
        if (((int)@in & KeyUtil.KEY_SCANCODE_MASK) != 0)
        {
            if (keyToSDLKey.TryGetValue(@in, out @out))
                return true;
        }
        else
        {
            // All other keys use the same value as their ASCII character representation
            @out = (SDL_Keycode)@in;
            return true;
        }

        @out = default;
        return false;
    }

    internal static bool GetConstant(SDL_Keycode @in, out Key @out)
    {
#if LOVE_ANDROID
        // TODO: Can this be done more cleanly?
        if (in == SDLK_AC_BACK)
        {
            @out = KEY_ESCAPE;
            return true;
        }
#endif

        if (((int)@in & SDLK_SCANCODE_MASK) != 0)
        {
            if (sdlKeyToKey.Count == 0)
            {
                foreach (var kvp in keyToSDLKey)
                    sdlKeyToKey[kvp.Value] = kvp.Key;
            }

            if (sdlKeyToKey.TryGetValue(@in, out @out))
                return true;
        }
        else
        {
            // All other keys use the same value as their ASCII character representation
            @out = (Key)@in;
            return true;
        }

        @out = default;
        return false;
    }

    /// <summary>
    ///     <para>Gets the hardware scancode corresponding to the given key.</para>
    ///     <para>
    ///         Unlike key constants, Scancodes are keyboard layout-independent. For example the scancode "w" will be
    ///         generated if the key in the same place as the "w" key on an American keyboard is pressed, no matter what the
    ///         key is labelled or what the user's operating system settings are.
    ///     </para>
    ///     <para>Scancodes are useful for creating default controls that have the same physical locations on all systems.</para>
    /// </summary>
    /// <param name="key">The key to get the scancode from.</param>
    /// <returns>
    ///     The scancode corresponding to the given key, or "unknown" if the given key has no known physical representation on
    ///     the current system.
    /// </returns>
    public unsafe Scancode GetScancodeFromKey(Key key)
    {
        // Default to unknown
        var scancode = Scancode.SCANCODE_UNKNOWN;

        // Try to get the SDL_Keycode for the given Key
        if (GetConstant(key, out SDL_Keycode sdlkey))
        {
            // Get the SDL_Scancode from the SDL_Keycode
            var sdlscancode = SDL_GetScancodeFromKey(sdlkey, null);

            // Try to get the mapped Scancode from the EnumMap
            scancodes.TryGetValue(sdlscancode, out scancode);
        }

        return scancode;
    }

    /// <summary>
    ///     <para>Gets whether key repeat is enabled.</para>
    /// </summary>
    /// <returns>
    ///     Whether key repeat is enabled.
    /// </returns>
    public bool HasKeyRepeat()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>Gets whether screen keyboard is supported.</para>
    /// </summary>
    /// <returns>
    ///     Whether screen keyboard is supported.
    /// </returns>
    public bool HasScreenKeyboard()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>Gets whether text input events are enabled.</para>
    /// </summary>
    /// <returns>
    ///     Whether text input events are enabled.
    /// </returns>
    public bool HasTextInput()
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>Checks whether a certain key is down. Not to be confused with Game.KeyPressed or Game.KeyReleased.</para>
    /// </summary>
    /// <param name="key">The key to check.</param>
    /// <returns>
    ///     True if the key is down, false if not.
    /// </returns>
    public bool IsDown(Key key)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>Checks whether a certain key is down. Not to be confused with Game.KeyPressed or Game.KeyReleased.</para>
    /// </summary>
    /// <param name="key">A key to check.</param>
    /// <param name="keys">Additional keys to check.</param>
    /// <returns>
    ///     True if any supplied key is down, false if not.
    /// </returns>
    public bool IsDown(Key key, params Key[] keys)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>Checks whether a certain key is down. Not to be confused with Game.KeyPressed or Game.KeyReleased.</para>
    /// </summary>
    /// <param name="keys">Enumerable of keys to check.</param>
    /// <returns>
    ///     True if any supplied key is down, false if not.
    /// </returns>
    public bool IsDown(IEnumerable<Key> keys)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>
    ///         Checks whether the specified Scancodes are pressed. Not to be confused with Game.KeyPressed or
    ///         Game.KeyReleased.
    ///     </para>
    ///     <para>
    ///         Unlike regular KeyConstants, Scancodes are keyboard layout-independent. The scancode "w" is used if the key
    ///         in the same place as the "w" key on an American keyboard is pressed, no matter what the key is labelled or what
    ///         the user's operating system settings are.
    ///     </para>
    /// </summary>
    /// <param name="scancode">A Scancode to check.</param>
    /// <param name="scancodes">Additional Scancodes to check.</param>
    /// <returns>
    ///     True if any supplied Scancode is down, false if not.
    /// </returns>
    public bool IsScancodeDown(Scancode scancode, params Scancode[] scancodes)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>
    ///         Checks whether the specified Scancodes are pressed. Not to be confused with Game.KeyPressed or
    ///         Game.KeyReleased.
    ///     </para>
    ///     <para>
    ///         Unlike regular KeyConstants, Scancodes are keyboard layout-independent. The scancode "w" is used if the key
    ///         in the same place as the "w" key on an American keyboard is pressed, no matter what the key is labelled or what
    ///         the user's operating system settings are.
    ///     </para>
    /// </summary>
    /// <param name="scancodes">Enumerable of Scancodes to check.</param>
    /// <returns>
    ///     True if any supplied Scancode is down, false if not.
    /// </returns>
    public bool IsScancodeDown(IEnumerable<Scancode> scancodes)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     Enables or disables key repeat for Game.KeyPressed. It is disabled by default.
    /// </summary>
    /// <remarks>
    ///     The interval between repeats depends on the user's system settings. This function doesn't affect whether
    ///     Game.TextInput is called multiple times while a key is held down.
    /// </remarks>
    /// <param name="enable">Whether repeat keypress events should be enabled when a key is held down.</param>
    public void SetKeyRepeat(bool enable)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>
    ///         Enables or disables text input events. It is enabled by default on Windows, Mac, and Linux, and disabled by
    ///         default on iOS and Android.
    ///     </para>
    ///     <para>On touch devices, this shows the system's native on-screen keyboard when it's enabled.</para>
    /// </summary>
    /// <param name="enable">Whether text input events should be enabled.</param>
    public void SetTextInput(bool enable)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    ///     <para>
    ///         Enables or disables text input events. It is enabled by default on Windows, Mac, and Linux, and disabled by
    ///         default on iOS and Android.
    ///     </para>
    ///     <para>On touch devices, this shows the system's native on-screen keyboard when it's enabled.</para>
    /// </summary>
    /// <param name="enable">Whether text input events should be enabled.</param>
    /// <param name="x">Text rectangle x position.</param>
    /// <param name="y">Text rectangle y position.</param>
    /// <param name="w">Text rectangle width.</param>
    /// <param name="h">Text rectangle height.</param>
    public void SetTextInput(bool enable, double x, double y, double w, double h)
    {
        throw new NotImplementedException();
    }
}