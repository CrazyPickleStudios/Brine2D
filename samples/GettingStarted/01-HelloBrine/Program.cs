using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Hosting;
using Brine2D.Input;

var builder = GameApplication.CreateBuilder(args);

builder.Configure(options =>
{
    options.Window.Title = "01 - Hello Brine";
    options.Window.Width = 1280;
    options.Window.Height = 720;
});

builder.AddScene<GameScene>();

await using var game = builder.Build();
await game.RunAsync<GameScene>();

// Scene definition
public class GameScene : Scene
{
    private readonly IInputContext _input;
    private readonly IGameContext _gameContext;

    public GameScene(
        IInputContext input,
        IGameContext gameContext)
    {
        _input = input;
        _gameContext = gameContext;
    }

    protected override void OnRender(GameTime gameTime)
    {
        Renderer.DrawText("Hello, Brine2D!", 100, 100, Color.White);
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        if (_input.IsKeyPressed(Key.Escape))
        {
            _gameContext.RequestExit();
        }
    }
}