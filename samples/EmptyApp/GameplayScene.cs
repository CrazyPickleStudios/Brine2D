using System.Drawing;
using Brine2D.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace EmptyApp;

internal sealed class GameplayScene : IScene
{
    private ITexture? _tex;
    private int _x;

    public async Task InitializeAsync(IGameContext context, CancellationToken ct)
    {
        await Task.Delay(2000, ct).ConfigureAwait(false);

        var content = context.Services.GetRequiredService<IContentManager>();
        _tex = await content.TryLoadAsync<ITexture>("Assets/player.png", ct);
    }

    public void Render(IRenderContext ctx)
    {
        ctx.Clear(Color.FromArgb(255, 30, 30, 30));
        if (_tex is not null)
            ctx.DrawTexture(_tex, new Rectangle(_x, 100, (int)_tex.Width, (int)_tex.Height));
        else
            ctx.DrawRect(new Rectangle(_x, 100, 40, 40), Color.FromArgb(255, 200, 80, 80));
        ctx.DrawRect(new Rectangle(_x, 100, 40, 40), Color.FromArgb(255, 0, 200, 255));
        ctx.Present();
    }

    public void Update(GameTime time)
    {
        _x += (int)(60 * time.DeltaSeconds);
        if (_x > 760) _x = 0;
    }
}