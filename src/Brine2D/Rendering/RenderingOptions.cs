using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace Brine2D.Rendering;

/// <summary>
/// Configuration options for the rendering system.
/// </summary>
public sealed class RenderingOptions
{
    /// <summary>
    /// Synchronizes frame rate with the display refresh rate. Default: <see langword="true"/>.
    /// When disabled, use <see cref="TargetFPS"/> to cap the frame rate manually.
    /// </summary>
    public bool VSync { get; set; } = true;

    /// <summary>
    /// Preferred GPU driver backend.
    /// If null or Auto, SDL3 will automatically select the best driver for the platform.
    /// </summary>
    public GPUDriver PreferredGPUDriver { get; set; } = GPUDriver.Auto;

    /// <summary>
    /// Gets or sets the target frames per second for the game loop.
    /// 0 = unlimited (use VSync or monitor refresh rate).
    /// </summary>
    [Range(0, 240, ErrorMessage = "TargetFPS must be between 0 (uncapped) and 240")]
    public int TargetFPS { get; set; } = 0;

    /// <summary>
    /// Gets or sets the maximum delta time in milliseconds applied per frame.
    /// Prevents runaway physics or update logic after a pause, debugger break, or frame spike.
    /// Default: 100ms. Set higher for games with heavy simulation; lower for tighter physics.
    /// </summary>
    [Range(1, 1000, ErrorMessage = "MaxDeltaTimeMs must be between 1 and 1000.")]
    public int MaxDeltaTimeMs { get; set; } = 100;

    /// <summary>
    /// Gets or sets the number of vertices that can be staged per GPU transfer pass.
    /// </summary>
    /// <remarks>
    /// This value sizes the GPU vertex buffer and per-frame transfer buffers (3 in flight).
    /// Each vertex is 32 bytes, so memory cost is approximately <c>MaxVerticesPerFrame × 32 × 4</c> bytes
    /// (1 vertex buffer + 3 transfer buffers). The default of 50,000 uses ~6.4 MB total.
    /// When this limit is reached mid-frame, the renderer automatically flushes pending draw calls
    /// to the GPU and reuses the buffer space — no vertices are dropped.
    /// Larger values reduce the number of mid-frame flush passes at the cost of more GPU memory.
    /// </remarks>
    [Range(1000, 1_000_000, ErrorMessage = "MaxVerticesPerFrame must be between 1,000 and 1,000,000")]
    public int MaxVerticesPerFrame { get; set; } = 50_000;

    public Color ClearColor { get; set; } = Color.FromArgb(255, 52, 78, 65);
}