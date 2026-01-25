# 01 - Hello Brine

Your first Brine2D application! This sample demonstrates the minimal setup required to create a game window and render text.

## What You'll Learn

- ✅ GameApplication setup (similar to WebApplication in ASP.NET)
- ✅ Service registration with `AddBrine2D()`
- ✅ Scene creation and lifecycle
- ✅ Basic text rendering
- ✅ Input handling (ESC to exit)

## The Code

### Program.cs - Setup

~~~csharp
var builder = GameApplication.CreateBuilder(args);

// Add Brine2D with sensible defaults
builder.Services.AddBrine2D(options =>
{
    options.WindowTitle = "01 - Hello Brine";
    options.WindowWidth = 1280;
    options.WindowHeight = 720;
});

builder.Services.AddScene<GameScene>();

var game = builder.Build();
await game.RunAsync<GameScene>();
~~~

**Key Points:**
- `GameApplication.CreateBuilder()` - Just like ASP.NET's `WebApplication.CreateBuilder()`
- `AddBrine2D()` - Registers all essential services (rendering, input, audio, core)
- `AddScene<T>()` - Registers your game scene (like adding a Controller)
- `RunAsync<T>()` - Starts the game loop with the specified scene

### GameScene.cs - Your Game

~~~csharp
public class GameScene : Scene
{
    private readonly IRenderer _renderer;
    private readonly IInputService _input;
    private readonly IGameContext _gameContext;

    // Constructor injection - DI provides services automatically!
    public GameScene(
        IRenderer renderer,
        IInputService input,
        IGameContext gameContext,
        ILogger<GameScene> logger) : base(logger)
    {
        _renderer = renderer;
        _input = input;
        _gameContext = gameContext;
    }

    protected override void OnRender(GameTime gameTime)
    {
        _renderer.DrawText("Hello, Brine2D!", 100, 100, Color.White);
        _renderer.DrawText("Press ESC to exit", 100, 140, Color.LightGray);
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        if (_input.IsKeyPressed(Keys.Escape))
            _gameContext.RequestExit();
    }
}
~~~

## ASP.NET Parallels

| ASP.NET Core | Brine2D |
|--------------|---------|
| `WebApplication.CreateBuilder()` | `GameApplication.CreateBuilder()` |
| `builder.Services.AddControllers()` | `builder.Services.AddBrine2D()` |
| `app.MapControllers()` | `builder.Services.AddScene<T>()` |
| `app.Run()` | `await game.RunAsync<T>()` |
| Constructor injection | ✅ Same pattern! |
| `ILogger<T>` | ✅ Same! |

## Run It

~~~sh
dotnet run
~~~

You should see a window with "Hello, Brine2D!" text. Press ESC to exit.

## What's Next?

- **02-SceneBasics** - Learn about scene lifecycle and transitions
- **03-DependencyInjection** - Custom services and configuration
- **04-InputAndText** - Keyboard, mouse, and text rendering

---

**Welcome to Brine2D!**