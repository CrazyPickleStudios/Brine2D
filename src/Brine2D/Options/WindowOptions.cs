namespace Brine2D.Options;

/// <summary>
///     Represents configuration options for the application window.
/// </summary>
/// <remarks>
///     Use this to configure the initial window size, title, and VSync behavior.
/// </remarks>
public class WindowOptions
{
    /// <summary>
    ///     Gets or sets the window height in pixels.
    /// </summary>
    /// <value>Default is 720.</value>
    public int Height { get; set; } = 720;

    /// <summary>
    ///     Gets or sets the window title text.
    /// </summary>
    /// <value>Default is "Brine2D".</value>
    public string Title { get; set; } = "Brine2D";

    /// <summary>
    ///     Gets or sets a value indicating whether vertical synchronization is enabled.
    /// </summary>
    /// <value>Default is <see langword="true" />.</value>
    public bool VSync { get; set; } = true;

    /// <summary>
    ///     Gets or sets the window width in pixels.
    /// </summary>
    /// <value>Default is 1280.</value>
    public int Width { get; set; } = 1280;
}