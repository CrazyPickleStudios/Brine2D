namespace Brine2D;

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