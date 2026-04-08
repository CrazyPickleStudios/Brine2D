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
    /// When <see langword="null"/>, the swapchain format detected at initialization is used.
    /// </summary>
    public SDL3.SDL.GPUTextureFormat? RenderTargetFormat { get; set; }
}