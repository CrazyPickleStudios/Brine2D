using Brine2D.Engine;

namespace Brine2D.Abstractions;

public interface IGame
{
    void Initialize(IGameContext context);
    void Render(IRenderContext ctx);
    void Update(GameTime time);
}