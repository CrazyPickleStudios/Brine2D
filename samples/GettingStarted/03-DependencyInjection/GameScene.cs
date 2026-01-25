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
    private readonly IInputService _input;

    private readonly ILogger<GameScene> _logger; // Structured logging!

    // Built-in Brine2D services (automatically registered)
    private readonly IRenderer _renderer;

    // Custom service (registered in Program.cs)
    private readonly IScoreService _scoreService;

    // Constructor injection - DI container provides all these automatically!
    public GameScene(
        IRenderer renderer,
        IInputService input,
        IGameContext gameContext,
        ILogger<GameScene> logger,
        IScoreService scoreService, // Custom service
        IOptions<GameOptions> gameOptions) // Configuration
        : base(logger)
    {
        _renderer = renderer;
        _input = input;
        _gameContext = gameContext;
        _logger = logger;
        _scoreService = scoreService;
        _gameOptions = gameOptions.Value; // Unwrap IOptions<T>

        // Log configuration on startup (structured logging!)
        _logger.LogInformation(
            "GameScene initialized with config: PointsPerSecond={Points}, PlayerName={Name}",
            _gameOptions.PointsPerSecond,
            _gameOptions.PlayerName);
    }

    protected override void OnEnter()
    {
        _logger.LogInformation("GameScene: OnEnter");

        // Reset score using our custom service
        _scoreService.ResetScore();
    }

    protected override void OnExit()
    {
        _logger.LogInformation("GameScene: OnExit - Final score: {Score}",
            _scoreService.GetScore());
    }

    protected override void OnRender(GameTime gameTime)
    {
        _renderer.DrawText("DEPENDENCY INJECTION", 100, 100, Color.White);

        _renderer.DrawText(
            $"Player: {_gameOptions.PlayerName}",
            100, 140, Color.LightGray);

        _renderer.DrawText(
            $"Score: {_scoreService.GetScore():F1}",
            100, 180, Color.Yellow);

        _renderer.DrawText(
            $"Points/sec: {_gameOptions.PointsPerSecond}",
            100, 220, Color.LightGray);

        _renderer.DrawText("Press SPACE to reset score", 100, 280, Color.Gray);
        _renderer.DrawText("Press ESC to quit", 100, 320, Color.Gray);

        // Show service lifetime info
        _renderer.DrawText(
            "Service Lifetime: Singleton (same instance throughout game)",
            100, 380, Color.DarkGray);
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        // Add points based on configuration
        _scoreService.AddPoints(_gameOptions.PointsPerSecond * (float)gameTime.DeltaTime);

        // SPACE to reset score
        if (_input.IsKeyPressed(Keys.Space))
        {
            _logger.LogInformation("Resetting score");
            _scoreService.ResetScore();
        }

        // ESC to quit
        if (_input.IsKeyPressed(Keys.Escape))
        {
            _logger.LogInformation("Exiting game with final score: {Score}",
                _scoreService.GetScore());
            _gameContext.RequestExit();
        }
    }
}