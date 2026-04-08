using Brine2D.Rendering;

namespace Brine2D.Tests.Rendering;

public class GpuDeviceHandleTests
{
    [Fact]
    public void Handle_ReturnsConstructorValue()
    {
        var handle = new GpuDeviceHandle((nint)0xDEAD);

        Assert.Equal((nint)0xDEAD, handle.Handle);
    }

    [Fact]
    public void Invalidate_SetsHandleToZero()
    {
        var handle = new GpuDeviceHandle((nint)0xBEEF);

        handle.Invalidate();

        Assert.Equal(nint.Zero, handle.Handle);
    }

    [Fact]
    public void Invalidate_IsIdempotent()
    {
        var handle = new GpuDeviceHandle((nint)0xCAFE);

        handle.Invalidate();
        handle.Invalidate();

        Assert.Equal(nint.Zero, handle.Handle);
    }

    [Fact]
    public void Handle_AfterInvalidate_RemainsZero()
    {
        var handle = new GpuDeviceHandle((nint)42);
        Assert.NotEqual(nint.Zero, handle.Handle);

        handle.Invalidate();

        Assert.Equal(nint.Zero, handle.Handle);
        Assert.Equal(nint.Zero, handle.Handle);
    }
}