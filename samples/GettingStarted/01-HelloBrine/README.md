# 01 - Hello Brine

Your first Brine2D application. This sample covers the minimal setup to open a game window and render text.

## What You'll Learn

- GameApplication setup (similar to WebApplication in ASP.NET)
- Scene creation and lifecycle
- Basic rendering and input

## The Code

### Program.cs

~~~csharp
using Brine2D.Hosting;

var builder = GameApplication.CreateBuilder(args);

builder.Configure(options =>
{
    options.Window.Title  = "01 - Hello Brine";
    options.Window.Width  = 1280;
    options.Window.Height = 720;
});

builder.AddScene<GameScene>();

await using var game = builder.Build();
await game.RunAsync<GameScene>();
~~~

### GameScene.cs

Framework services are available as properties on `Scene`: `Renderer`, `Input`, `Audio`, `World`, `Logger`, `Game`. No constructor needed for any of them.

~~~csharp
using Brine2D.Core;
using Brine2D.Engine;

public class GameScene : Scene
{
    protected override void OnEnter()
    {
        Renderer.ClearColor = Color.CornflowerBlue;
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        if (Input.IsKeyPressed(Key.Escape))
            Environment.Exit(0);
    }

    protected override void OnRender(GameTime gameTime)
    {
        Renderer.DrawText("Hello, Brine2D!", 100, 100, Color.White);
        Renderer.DrawText("Press ESC to exit", 100, 140, Color.LightGray);
    }
}
~~~

If your scene needs your own services, inject only those:

~~~csharp
public class GameScene : Scene
{
    private readonly IScoreService _scores;

    public GameScene(IScoreService scores)
    {
        _scores = scores;
    }
}
~~~

## ASP.NET Parallels

| ASP.NET Core | Brine2D |
|---|---|
| `WebApplication.CreateBuilder()` | `GameApplication.CreateBuilder()` |
| `builder.Services.AddControllers()` | `builder.Services.AddBrine2D()` |
| `app.MapControllers()` | `builder.AddScene<T>()` |
| `app.Run()` | `await game.RunAsync<T>()` |
| `ControllerBase` properties | `Scene` properties (`Renderer`, `Input`, `Audio`) |
| `ILogger<T>` | `ILogger<T>` (same interface, same DI container) |

## Run It

~~~sh
dotnet run
~~~

You should see a blue window with "Hello, Brine2D!" text. Press ESC to exit.

## What's Next?

- **02-SceneBasics** - Scene lifecycle and transitions
- **03-DependencyInjection** - Custom services and configuration
- **04-InputAndText** - Keyboard, mouse, and text rendering