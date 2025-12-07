using Brine2D.Abstractions;

namespace Brine2D.Desktop;

public sealed class DesktopGameContext : IGameContext
{
    public DesktopGameContext(IServiceProvider services, IWindow window, IInput input)
    {
        Services = services;
        Window = window;
        Input = input;
    }

    public IInput Input { get; }

    public IServiceProvider Services { get; }
    public IWindow Window { get; }
}