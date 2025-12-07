namespace Brine2D.Abstractions;

public interface IGameContext
{
    IInput Input { get; }
    IServiceProvider Services { get; }
    IWindow Window { get; }
}