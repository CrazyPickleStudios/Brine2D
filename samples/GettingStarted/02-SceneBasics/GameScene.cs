using System.Drawing;
using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.Logging;

namespace SceneBasics;

/// <summary>
/// Game scene demonstrating scene lifecycle and transitions.
/// Press ESC to return to menu (demonstrates scene transitions).
/// </summary>
public class GameScene : Scene
{
    private readonly IRenderer _renderer;
    private readonly IInputService _input;
    private readonly ISceneManager _sceneManager;
    private readonly IGameContext _gameContext;
    private int _score = 0;
    private float _elapsedTime = 0f;

    public GameScene(
        IRenderer renderer,
        IInputService input,
        ISceneManager sceneManager,
        IGameContext gameContext,
        ILogger<GameScene> logger) : base(logger)
    {
        _renderer = renderer;
        _input = input;
        _sceneManager = sceneManager;
        _gameContext = gameContext;
    }

    // OnLoad: Called when scene loads - initialize state
    // Each time you transition to GameScene, a NEW instance is created
    protected override Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("GameScene: OnLoad - Game starting");
        _renderer.ClearColor = Color.FromArgb(255, 52, 78, 65); // Dirty brine
        
        // Initialize state (fresh scene every time)
        _score = 0;
        _elapsedTime = 0f;
        
        return Task.CompletedTask;
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        // Update game logic
        _elapsedTime += (float)gameTime.DeltaTime;
        _score = (int)_elapsedTime; // Score increases over time

        // ESC returns to menu
        if (_input.IsKeyPressed(Keys.Escape))
        {
            Logger.LogInformation("GameScene: Returning to menu");
            _sceneManager.LoadSceneAsync<MenuScene>();
        }
        
        // Q quits game
        if (_input.IsKeyPressed(Keys.Q))
        {
            Logger.LogInformation("GameScene: Quitting game");
            _gameContext.RequestExit();
        }
    }

    protected override void OnRender(GameTime gameTime)
    {
        _renderer.DrawText("GAME SCENE", 100, 100, Color.White);
        _renderer.DrawText($"Score: {_score}", 100, 140, Color.Yellow);
        _renderer.DrawText($"Time: {_elapsedTime:F1}s", 100, 180, Color.LightGray);
        _renderer.DrawText("Press ESC to return to menu", 100, 220, Color.Gray);
        _renderer.DrawText("Press Q to quit", 100, 260, Color.Gray);
    }

    // OnUnload: Called when scene unloads - cleanup resources
    protected override Task OnUnloadAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("GameScene: OnUnload - Final score was {Score}", _score);
        // Cleanup resources here (if needed)
        return Task.CompletedTask;
    }
}