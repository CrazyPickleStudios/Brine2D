using Brine2D.Engine;
using Brine2D.Graphics;
using System.Drawing;

namespace EmptyApp;

internal sealed class LoadingScene : IScene
{
    private double _t;

    public Task InitializeAsync(IGameContext context, CancellationToken ct)
    {
        return Task.CompletedTask;
    }

    public void Render(IRenderContext ctx)
    {
        ctx.Clear(Color.FromArgb(255, 20, 20, 20));

        var cx = 400;
        var cy = 300;
        var r = 40;
        var x = cx + (int)(Math.Cos(_t * 6) * r);
        var y = cy + (int)(Math.Sin(_t * 6) * r);

        ctx.DrawRect(new RectangleF(x - 10, y - 10, 20, 20), Color.FromArgb(255, 0, 200, 255));
        ctx.Present();
    }

    public void Update(GameTime time)
    {
        _t += time.DeltaSeconds;
    }
}