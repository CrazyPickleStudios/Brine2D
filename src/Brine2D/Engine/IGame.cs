namespace Brine2D.Engine;

public interface IGame
{
    void Initialize(IGameContext context);
    void Render(IRenderContext ctx);
    void Update(GameTime time);
}