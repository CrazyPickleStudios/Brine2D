using System.Drawing;
using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Input;
using Brine2D.Rendering;
using DependencyInjection.Options;
using DependencyInjection.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace DependencyInjection;

/// <summary>
///     Demonstrates dependency injection in Brine2D.
///     All services are automatically injected via constructor - just like ASP.NET Core!
/// </summary>
public class GameScene : Scene
{
    private readonly IGameContext _gameContext;

    // Configuration options (bound from gamesettings.json)
    private readonly GameOptions _gameOptions;
    private readonly IInputContext _input;
    
    // Custom service (registered in Program.cs)
    private readonly IScoreService _scoreService;

    // Constructor injection - DI container provides all these automatically!
    public GameScene(
        IInputContext input,
        IGameContext gameContext,
        IScoreService scoreService, // Custom service
        IOptions<GameOptions> gameOptions) // Configuration
    {
        _input = input;
        _gameContext = gameContext;
        _scoreService = scoreService;
        _gameOptions = gameOptions.Value; // Unwrap IOptions<T>

        // Log configuration on startup (structured logging!)
        Logger.LogInformation(
            "GameScene initialized with config: PointsPerSecond={Points}, PlayerName={Name}",
            _gameOptions.PointsPerSecond,
            _gameOptions.PlayerName);
    }

    // OnLoad: Called when scene loads - initialize state
    protected override Task OnLoadAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("GameScene: OnLoad");
        Renderer.ClearColor = Color.FromArgb(255, 52, 78, 65); // Dirty brine

        // Reset score using our custom service
        _scoreService.ResetScore();

        return Task.CompletedTask;
    }

    // OnUnload: Called when scene unloads - cleanup
    protected override Task OnUnloadAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("GameScene: OnUnload - Final score: {Score}",
            _scoreService.GetScore());
        return Task.CompletedTask;
    }

    protected override void OnRender(GameTime gameTime)
    {
        Renderer.DrawText("DEPENDENCY INJECTION", 100, 100, Color.White);

        Renderer.DrawText(
            $"Player: {_gameOptions.PlayerName}",
            100, 140, Color.LightGray);

        Renderer.DrawText(
            $"Score: {_scoreService.GetScore():F1}",
            100, 180, Color.Yellow);

        Renderer.DrawText(
            $"Points/sec: {_gameOptions.PointsPerSecond}",
            100, 220, Color.LightGray);

        Renderer.DrawText("Press SPACE to reset score", 100, 280, Color.Gray);
        Renderer.DrawText("Press ESC to quit", 100, 320, Color.Gray);

        // Show service lifetime info
        Renderer.DrawText(
            "Service Lifetime: Singleton (same instance throughout game)",
            100, 380, Color.DarkGray);
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        // Add points based on configuration
        _scoreService.AddPoints(_gameOptions.PointsPerSecond * (float)gameTime.DeltaTime);

        // SPACE to reset score
        if (_input.IsKeyPressed(Key.Space))
        {
            Logger.LogInformation("Resetting score");
            _scoreService.ResetScore();
        }

        // ESC to quit
        if (_input.IsKeyPressed(Key.Escape))
        {
            Logger.LogInformation("Exiting game with final score: {Score}",
                _scoreService.GetScore());
            _gameContext.RequestExit();
        }
    }
}