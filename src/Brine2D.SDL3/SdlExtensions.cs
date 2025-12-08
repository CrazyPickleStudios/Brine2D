using System.Runtime.InteropServices;
using SDL3;

namespace Brine2D.SDL3;

internal static class SdlExtensions
{
    public static string GetText(this SDL.TextInputEvent ev)
    {
        return ev.Text.GetUtf8String();
    }

    public static string GetText(this SDL.TextEditingEvent ev)
    {
        return ev.Text.GetUtf8String();
    }

    private static string GetUtf8String(this IntPtr ptr)
    {
        if (ptr == IntPtr.Zero)
        {
            return string.Empty;
        }

        return Marshal.PtrToStringUTF8(ptr) ?? string.Empty;
    }
}