using Brine2D.Graphics;

namespace Brine2D.Engine;

public interface IGame
{
    Task Initialize(IGameContext context);
    void Update(GameTime time);
    void Render(IRenderContext ctx);
}