namespace Brine2D.Rendering.SDL.PostProcessing;

/// <summary>
/// Configuration options for post-processing effects.
/// </summary>
public class PostProcessingOptions
{
    /// <summary>
    /// Enable or disable post-processing. When disabled, rendering goes directly to swapchain.
    /// Default: false
    /// </summary>
    public bool Enabled { get; set; } = false;

    /// <summary>
    /// Texture format for render targets.
    /// Default: R8G8B8A8Unorm (standard RGBA)
    /// </summary>
    public SDL3.SDL.GPUTextureFormat RenderTargetFormat { get; set; } = SDL3.SDL.GPUTextureFormat.R8G8B8A8Unorm;
}