namespace Brine2D.Rendering;

/// <summary>
/// GPU backend driver for SDL3 rendering.
/// </summary>
public enum GPUDriver
{
    /// <summary>
    /// Automatically select the best available driver for the platform.
    /// </summary>
    Auto = 0,
    
    /// <summary>
    /// Vulkan (Windows, Linux, Android).
    /// </summary>
    Vulkan = 1,
    
    /// <summary>
    /// Direct3D 12 (Windows only).
    /// </summary>
    D3D12 = 2,
    
    /// <summary>
    /// Metal (macOS, iOS).
    /// </summary>
    Metal = 3
}