using Brine2D.Graphics;
using SDL3;

namespace Brine2D.SDL3;

internal sealed class SdlTexture : ITexture
{
    public IntPtr Handle { get; }
    public float Width { get; }
    public float Height { get; }

    public SdlTexture(IntPtr handle, float width, float height)
    {
        Handle = handle;
        Width = width;
        Height = height;
    }

    public void Dispose()
    {
        if (Handle != IntPtr.Zero)
        {
            SDL.DestroyTexture(Handle);
        }
    }
}