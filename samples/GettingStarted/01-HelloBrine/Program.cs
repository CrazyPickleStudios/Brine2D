using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Hosting;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.SDL;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

// Create the game application builder (similar to ASP.NET)
var builder = GameApplication.CreateBuilder(args);

// Add Brine2D with SDL backend
builder.Services
    .AddBrine2D(options =>
    {
        options.Window.Title = "Hello Brine2D";
        options.Window.Width = 1280;
        options.Window.Height = 720;
        options.Rendering.Backend = GraphicsBackend.GPU;
        options.Rendering.VSync = true;
    })
    .UseSDL(); // Activate SDL backend

// Register scene
builder.Services.AddScene<GameScene>();

// Build and run
var game = builder.Build();
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