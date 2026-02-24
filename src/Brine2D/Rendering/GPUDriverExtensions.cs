namespace Brine2D.Rendering;

internal static class GPUDriverExtensions
{
    /// <summary>
    /// Converts GPUDriver enum to SDL3 driver name string.
    /// </summary>
    public static string? ToSDL3DriverName(this GPUDriver driver)
    {
        return driver switch
        {
            GPUDriver.Vulkan => "vulkan",
            GPUDriver.D3D12 => "d3d12",
            GPUDriver.Metal => "metal",
            GPUDriver.Auto => null, // Let SDL3 choose
            _ => null
        };
    }
}