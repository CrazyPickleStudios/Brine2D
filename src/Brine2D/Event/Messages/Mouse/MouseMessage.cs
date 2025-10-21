using Brine2D.Event.Messages;
using Brine2D.Window;

namespace Brine2D.Event.Messages.Mouse;

internal abstract record MouseMessage : Message
{
    protected static (double x, double y) ClampToWindow(WindowModule? window, double x, double y)
    {
        return window?.ClampPositionInWindow(x, y) ?? (x, y);
    }

    protected static (double x, double y) WindowToDPICoords(WindowModule? window, double x, double y)
    {
        return window?.WindowToDPICoords(x, y) ?? (x, y);
    }
}