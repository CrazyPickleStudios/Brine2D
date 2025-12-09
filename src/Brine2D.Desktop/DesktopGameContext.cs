using Brine2D.Engine;
using Brine2D.Input;

namespace Brine2D.Desktop;

/// <summary>
///     Desktop-specific implementation of <see cref="IGameContext" /> providing access to
///     application services, the main window, and input handling for desktop platforms.
/// </summary>
public sealed class DesktopGameContext : IGameContext
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DesktopGameContext" /> class.
    /// </summary>
    /// <param name="services">The service provider used to resolve application services.</param>
    /// <param name="window">The primary desktop window instance.</param>
    /// <param name="input">The input subsystem responsible for handling user input.</param>
    public DesktopGameContext(IServiceProvider services, IWindow window, IInput input)
    {
        // Store references to the provided platform-specific components.
        Services = services;
        Window = window;
        Input = input;
    }

    /// <summary>
    ///     Gets the input subsystem for handling desktop input devices.
    /// </summary>
    public IInput Input { get; }

    /// <summary>
    ///     Gets the service provider for resolving dependencies and services.
    /// </summary>
    public IServiceProvider Services { get; }

    /// <summary>
    ///     Gets the main desktop window associated with the game context.
    /// </summary>
    public IWindow Window { get; }
}