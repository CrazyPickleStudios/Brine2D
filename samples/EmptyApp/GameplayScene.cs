using System.Drawing;
using Brine2D.Engine;
using Microsoft.Extensions.DependencyInjection;

namespace EmptyApp;

internal sealed class GameplayScene : IScene
{
    private IMusic _music = null!;
    private ITexture? _tex;
    private int _x;

    public async Task InitializeAsync(IGameContext context, CancellationToken ct)
    {
        // Simulate a long loading time.
        await Task.Delay(2000, ct).ConfigureAwait(false);

        var content = context.Services.GetRequiredService<IContentManager>();
        
        _tex = await content.TryLoadAsync<ITexture>("Assets/logo.png", ct);

        // TODO: The NuGet for SDL3-CS is pending a new release for audio to work. -RP
        //var audio = context.Services.GetRequiredService<IAudio>();
        //_music = await content.LoadAsync<IMusic>("Assets/music.mp3", ct);
        // audio.PlayMusic(_music);
    }

    public void Render(IRenderContext ctx)
    {
        ctx.Clear(Color.FromArgb(255, 30, 30, 30));

        if (_tex is not null)
        {
            ctx.DrawTexture(_tex, new Rectangle(_x, 100, 64, 64));
        }
        else
        {
            ctx.DrawRect(new Rectangle(_x, 100, 64, 64), Color.FromArgb(255, 0, 200, 255));
        }

        
        ctx.Present();
    }

    public void Update(GameTime time)
    {
        _x += (int)(60 * time.DeltaSeconds);

        if (_x > 760)
        {
            _x = 0;
        }
    }
}