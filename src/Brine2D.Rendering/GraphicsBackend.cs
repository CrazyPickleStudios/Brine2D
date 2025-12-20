namespace Brine2D.Rendering;

/// <summary>
/// Supported graphics backend types.
/// </summary>
public enum GraphicsBackend
{
    /// <summary>
    /// Automatically select the best backend for the platform.
    /// </summary>
    Auto,
    
    /// <summary>
    /// Legacy SDL3 renderer (software/hardware accelerated).
    /// </summary>
    LegacyRenderer,
    
    /// <summary>
    /// Modern SDL3 GPU API (Vulkan/Metal/D3D11/D3D12).
    /// </summary>
    GPU
}