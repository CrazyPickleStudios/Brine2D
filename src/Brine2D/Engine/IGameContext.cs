using Brine2D.Input;

namespace Brine2D.Engine;

public interface IGameContext
{
    IInput Input { get; }
    IServiceProvider Services { get; }
    IWindow Window { get; }
}