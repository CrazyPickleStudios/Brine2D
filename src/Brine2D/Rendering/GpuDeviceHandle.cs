namespace Brine2D.Rendering;

/// <summary>
/// Shared, mutable reference to the GPU device pointer.
/// The renderer creates one instance and passes it to every texture it allocates.
/// When the renderer destroys the device, it calls <see cref="Invalidate"/>,
/// and all textures that still reference this handle see <see cref="nint.Zero"/>
/// instead of a dangling pointer.
/// </summary>
internal sealed class GpuDeviceHandle
{
    private nint _handle;

    public nint Handle => Volatile.Read(ref _handle);

    public GpuDeviceHandle(nint handle) => _handle = handle;

    public void Invalidate() => Volatile.Write(ref _handle, nint.Zero);
}