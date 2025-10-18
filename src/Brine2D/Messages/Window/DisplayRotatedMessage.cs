using SDL;
using static SDL.SDL3;

namespace Brine2D;

internal sealed record DisplayRotatedMessage(int Index, DisplayOrientation Orientation) : Message
{
    public static unsafe DisplayRotatedMessage FromSDL(SDL_Event e)
    {
        DisplayOrientation orientation;

        switch ((SDL_DisplayOrientation)e.display.data1)
        {
            case SDL_DisplayOrientation.SDL_ORIENTATION_UNKNOWN:
            default:
                orientation = DisplayOrientation.Unknown;
                break;
            case SDL_DisplayOrientation.SDL_ORIENTATION_LANDSCAPE:
                orientation = DisplayOrientation.Landscape;
                break;
            case SDL_DisplayOrientation.SDL_ORIENTATION_LANDSCAPE_FLIPPED:
                orientation = DisplayOrientation.Landscapeflipped;
                break;
            case SDL_DisplayOrientation.SDL_ORIENTATION_PORTRAIT:
                orientation = DisplayOrientation.Portrait;
                break;
            case SDL_DisplayOrientation.SDL_ORIENTATION_PORTRAIT_FLIPPED:
                orientation = DisplayOrientation.Portraitflipped;
                break;
        }

        var count = 0;
        var displayIndex = 0;
        var displays = SDL_GetDisplays(&count);

        for (var i = 0; i < count; i++)
        {
            if (displays[i] == e.display.displayID)
            {
                displayIndex = i;
                break;
            }
        }

        SDL_free(displays);

        return new DisplayRotatedMessage(displayIndex + 1, orientation);
    }
}