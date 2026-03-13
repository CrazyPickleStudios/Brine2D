using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Input;
using DependencyInjection.Options;
using DependencyInjection.Services;
using Microsoft.Extensions.Logging;

namespace DependencyInjection;

/// <summary>
///     Demonstrates dependency injection in Brine2D.
///     Framework services (Renderer, Input, Audio, Logger, etc.) are available as Scene
///     properties. Only inject your own services via the constructor.
/// </summary>
public class GameScene : Scene
{
    private readonly GameOptions _gameOptions;
    private readonly IScoreService _scoreService;

    // Only inject YOUR services; framework properties handle the rest
    public GameScene(IScoreService scoreService, GameOptions gameOptions)
    {
        _scoreService = scoreService;
        _gameOptions  = gameOptions;
    }

    protected override Task OnLoadAsync(CancellationToken cancellationToken, IProgress<float>? progress = null)
    {
        Logger.LogInformation("GameScene: OnLoad");
        Renderer.ClearColor = new Color(52, 78, 65, 255);
        _scoreService.ResetScore();
        return Task.CompletedTask;
    }

    protected override void OnEnter()
    {
        Logger.LogInformation(
            "GameScene initialized: PointsPerSecond={Points}, PlayerName={Name}",
            _gameOptions.PointsPerSecond,
            _gameOptions.PlayerName);
    }

    protected override Task OnUnloadAsync(CancellationToken cancellationToken)
    {
        Logger.LogInformation("GameScene: OnUnload - Final score: {Score}", _scoreService.GetScore());
        return Task.CompletedTask;
    }

    protected override void OnRender(GameTime gameTime)
    {
        Renderer.DrawText("DEPENDENCY INJECTION",                                          100, 100, Color.White);
        Renderer.DrawText($"Player: {_gameOptions.PlayerName}",                           100, 140, Color.LightGray);
        Renderer.DrawText($"Score: {_scoreService.GetScore():F1}",                        100, 180, Color.Yellow);
        Renderer.DrawText($"Points/sec: {_gameOptions.PointsPerSecond}",                  100, 220, Color.LightGray);
        Renderer.DrawText("Press SPACE to reset score",                                   100, 280, Color.Gray);
        Renderer.DrawText("Press ESC to quit",                                            100, 320, Color.Gray);
        Renderer.DrawText("Service Lifetime: Singleton (same instance throughout game)",  100, 380, Color.DarkGray);
    }

    protected override void OnUpdate(GameTime gameTime)
    {
        _scoreService.AddPoints(_gameOptions.PointsPerSecond * (float)gameTime.DeltaTime);

        if (Input.IsKeyPressed(Key.Space))
        {
            Logger.LogInformation("Resetting score");
            _scoreService.ResetScore();
        }

        if (Input.IsKeyPressed(Key.Escape))
        {
            Logger.LogInformation("Exiting with final score: {Score}", _scoreService.GetScore());
            Game.RequestExit();
        }
    }
}