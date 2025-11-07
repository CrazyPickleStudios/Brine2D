namespace Brine2D.Core.Hosting;

/// <summary>
///     Represents an abstraction of a platform window used for rendering and user interaction.
/// </summary>
/// <remarks>
///     Implementations should report client-area dimensions in pixels. Values may change at runtime
///     due to user-initiated resizes or system DPI/scaling changes. The interface is read-only for
///     sizing but allows changing the window title.
/// </remarks>
public interface IWindow
{
    /// <summary>
    ///     Gets the current client-area height, in pixels.
    /// </summary>
    /// <remarks>
    ///     This value may change after a resize event. Consumers should query it when needed rather than caching indefinitely.
    /// </remarks>
    int Height { get; }

    /// <summary>
    ///     Gets a value indicating whether the window is in the process of closing.
    /// </summary>
    /// <remarks>
    ///     When true, the main loop should begin shutdown and release resources promptly. Implementations typically
    ///     set this to true after receiving a close request from the OS or user.
    /// </remarks>
    bool IsClosing { get; }

    /// <summary>
    ///     Gets or sets the window title displayed by the operating system.
    /// </summary>
    /// <remarks>
    ///     Setting the title should be a non-blocking operation in implementations. If the window is closing,
    ///     implementations may ignore changes to this property.
    /// </remarks>
    string Title { get; set; }

    /// <summary>
    ///     Gets the current client-area width, in pixels.
    /// </summary>
    /// <remarks>
    ///     This value may change after a resize event. Consumers should query it when needed rather than caching indefinitely.
    /// </remarks>
    int Width { get; }
}