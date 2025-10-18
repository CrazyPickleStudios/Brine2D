using Brine2D.Event.Messages;
using Brine2D.Window;

namespace Brine2D.Event.Messages.Mouse;

internal abstract record MouseMessage : Message
{
    protected static void ClampToWindow(WindowModule? window, ref double? x, ref double? y)
    {
        if (window != null)
        {
            window.ClampPositionInWindow(ref x, ref y);
        }
    }

    protected static void WindowToDPICoords(WindowModule? window, ref double? x, ref double? y)
    {
        if (window != null)
        {
            window.WindowToDPICoords(ref x, ref y);
        }
    }
}