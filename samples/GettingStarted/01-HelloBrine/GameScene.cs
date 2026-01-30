using System.Drawing;
using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace HelloBrine;

/// <summary>
/// Your first Brine2D scene!
/// Scenes are like Controllers in ASP.NET - they handle game logic and rendering.
/// </summary>
public class GameScene : Scene
{
    private readonly IGameContext _gameContext;
    private readonly IInputContext _input;

    // Constructor uses Dependency Injection (just like ASP.NET Core!)
    // Services are automatically injected by the DI container
    public GameScene
    (
        IInputContext input,
        IGameContext gameContext)
    {
        _input = input;
        _gameContext = gameContext;
    }
    
    // OnUpdate is called every frame for game logic (input, physics, AI)
    // Think of it like processing a request in ASP.NET
    protected override void OnUpdate(GameTime gameTime)
    {
        // Exit when ESC is pressed
        if (_input.IsKeyPressed(Key.Escape))
        {
            _gameContext.RequestExit();
        }
    }

    // OnRender is called every frame for drawing
    // Separate from OnUpdate for clarity (game logic vs presentation)
    protected override void OnRender(GameTime gameTime)
    {
        // Draw "Hello, Brine2D!" text at position (100, 100)
        Renderer.DrawText("Hello, Brine2D!", 100, 100, Color.White);
        
        // Show exit instructions
        Renderer.DrawText("Press ESC to exit", 100, 140, Color.LightGray);
    }
}