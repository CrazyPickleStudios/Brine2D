using System.ComponentModel.DataAnnotations;

namespace Brine2D.Rendering;

/// <summary>
/// Configuration options for the game window.
/// </summary>
public sealed class WindowOptions
{
    /// <summary>
    /// Gets or sets the window title. Defaults to "Brine2D Game".
    /// </summary>
    [Required]
    [StringLength(100, ErrorMessage = "Title cannot exceed 100 characters")]
    public string Title { get; set; } = "Brine2D Game";

    /// <summary>
    /// Gets or sets the window width in pixels. Defaults to 1280.
    /// Must be between 1 and 7680 pixels.
    /// </summary>
    [Range(1, 7680, ErrorMessage = "Width must be between 1 and 7680")]
    public int Width { get; set; } = 1280;

    /// <summary>
    /// Gets or sets the window height in pixels. Defaults to 720.
    /// Must be between 1 and 4320 pixels.
    /// </summary>
    [Range(1, 4320, ErrorMessage = "Height must be between 1 and 4320")]
    public int Height { get; set; } = 720;

    /// <summary>
    /// Gets or sets whether the window starts in fullscreen mode.
    /// </summary>
    public bool Fullscreen { get; set; }

    /// <summary>
    /// Gets or sets whether the window is resizable by the user.
    /// </summary>
    public bool Resizable { get; set; } = true;

    /// <summary>
    /// Gets or sets whether the window starts maximized.
    /// </summary>
    public bool Maximized { get; set; }

    /// <summary>
    /// Gets or sets whether the window has window decorations (border, title bar).
    /// </summary>
    public bool Borderless { get; set; }
}