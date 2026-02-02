namespace Brine2D.Rendering;

/// <summary>
/// Configuration options for the game window.
/// </summary>
public class WindowOptions
{
    /// <summary>
    /// Configuration section name for binding from JSON.
    /// </summary>
    public const string SectionName = "Window";
    
    /// <summary>
    /// Gets or sets the window title displayed in the title bar.
    /// </summary>
    public string Title { get; set; } = "Brine2D Game";
    
    /// <summary>
    /// Gets or sets the initial window width in pixels.
    /// </summary>
    public int Width { get; set; } = 1280;
    
    /// <summary>
    /// Gets or sets the initial window height in pixels.
    /// </summary>
    public int Height { get; set; } = 720;
    
    /// <summary>
    /// Gets or sets whether the window starts in fullscreen mode.
    /// </summary>
    public bool Fullscreen { get; set; } = false;
    
    /// <summary>
    /// Gets or sets whether the window is resizable by the user.
    /// </summary>
    public bool Resizable { get; set; } = true;
    
    /// <summary>
    /// Gets or sets whether the window starts maximized.
    /// </summary>
    public bool Maximized { get; set; } = false;
    
    /// <summary>
    /// Gets or sets whether the window has window decorations (border, title bar).
    /// </summary>
    public bool Borderless { get; set; } = false;
}