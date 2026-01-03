using System;
using System.Collections.Generic;
using System.Text;

namespace Brine2D.Rendering;

/// <summary>
/// Configuration options for rendering.
/// </summary>
public class RenderingOptions
{
    public const string SectionName = "Rendering";

    /// <summary>
    /// Gets or sets the window title.
    /// </summary>
    public string WindowTitle { get; set; } = "Brine2D Game";

    /// <summary>
    /// Gets or sets the window width in pixels.
    /// </summary>
    public int WindowWidth { get; set; } = 1280;

    /// <summary>
    /// Gets or sets the window height in pixels.
    /// </summary>
    public int WindowHeight { get; set; } = 720;

    /// <summary>
    /// Gets or sets a value indicating whether the window starts in fullscreen mode.
    /// </summary>
    public bool Fullscreen { get; set; } = false;

    /// <summary>
    /// Gets or sets a value indicating whether VSync is enabled.
    /// </summary>
    public bool VSync { get; set; } = true;

    /// <summary>
    /// Gets or sets a value indicating whether the window is resizable.
    /// </summary>
    public bool Resizable { get; set; } = true;

    /// <summary>
    /// Gets or sets the graphics backend to use.
    /// </summary>
    /// TODO: Need to switch back to GPU once stable.
    public GraphicsBackend Backend { get; set; } = GraphicsBackend.LegacyRenderer;

    /// <summary>
    /// Gets or sets the preferred GPU driver (Vulkan, Metal, D3D11, D3D12).
    /// Null = auto-select.
    /// </summary>
    public string? PreferredGPUDriver { get; set; } = null;
}
