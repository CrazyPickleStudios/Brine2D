namespace Brine2D.Engine;

public interface IGameLoop
{
    Task RunAsync(CancellationToken cancellationToken);
}