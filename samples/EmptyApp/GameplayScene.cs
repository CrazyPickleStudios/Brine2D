using System.Drawing;
using Brine2D.Audio;
using Brine2D.Content;
using Brine2D.Engine;
using Brine2D.Graphics;
using Brine2D.Graphics.Tilemaps;
using Microsoft.Extensions.DependencyInjection;

namespace EmptyApp;

internal sealed class GameplayScene : IScene
{
    private readonly IContentManager _content;
    //private readonly IAudio _audio;
    private Tilemap? _map;
    private readonly TilemapRenderer _renderer = new TilemapRenderer();
    private int _camX;
    //private IMusic _backgroundMusic;

    public GameplayScene(IContentManager content)//, IAudio audio)
    {
        _content = content;
        //_audio = audio;
    }

    public async Task InitializeAsync(IGameContext context, CancellationToken ct)
    {
        _map = await _content.LoadAsync<Tilemap>("untitled.tmj", ct);
        //_backgroundMusic = await _content.LoadAsync<IMusic>("Assets/music.mp3");
        //_audio.PlayMusic(_backgroundMusic);
    }

    public void Render(IRenderContext ctx)
    {
        ctx.Clear(Color.FromArgb(255,30, 30, 30));

        if (_map is not null)
        {
            var viewport = new RectangleF(_camX, 0, 800, 600);
            _renderer.Draw(ctx, _map, viewport);
        }

        ctx.Present();
    }

    public void Update(GameTime time)
    {
        //_camX += (int)(100 * time.DeltaSeconds);
    }
}