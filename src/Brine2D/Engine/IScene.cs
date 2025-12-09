namespace Brine2D.Engine;

public interface IScene
{
    Task InitializeAsync(IGameContext context, CancellationToken ct);
    void Update(GameTime time);
    void Render(IRenderContext ctx);
}