using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace Brine2D.Rendering;

/// <summary>
/// Configuration options for the rendering system.
/// </summary>
public class RenderingOptions
{
    /// <summary>
    /// Configuration section name for binding from JSON.
    /// </summary>
    public const string SectionName = "Rendering";

    /// <summary>
    /// Gets or sets the graphics backend to use (GPU or LegacyRenderer).
    /// </summary>
    public GraphicsBackend Backend { get; set; } = GraphicsBackend.GPU;
    
    /// <summary>
    /// Gets or sets whether VSync is enabled.
    /// </summary>
    public bool VSync { get; set; } = true;
    
    /// <summary>
    /// Gets or sets the preferred GPU driver (Vulkan, Metal, D3D11, D3D12).
    /// Null = auto-select based on platform.
    /// </summary>
    public string? PreferredGPUDriver { get; set; } = null;
    
    /// <summary>
    /// Gets or sets the target frames per second for the game loop.
    /// </summary>
    public int TargetFPS { get; set; } = 60;
    
    /// <summary>
    /// Gets or sets the clear color used when clearing the screen each frame.
    /// </summary>
    public Color ClearColor { get; set; } = Color.FromArgb(255, 52, 78, 65);
}
