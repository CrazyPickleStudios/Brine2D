using System.ComponentModel.DataAnnotations;
using System.Drawing;

namespace Brine2D.Rendering;

/// <summary>
/// Configuration options for the rendering system.
/// </summary>
public sealed class RenderingOptions
{
    /// <summary>
    /// Gets or sets whether VSync is enabled.
    /// When true, frame rate is synchronized with display refresh rate.
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
    /// Gets or sets the clear color used when clearing the screen each frame.
    /// </summary>
    public Color ClearColor { get; set; } = Color.FromArgb(255, 52, 78, 65);
}