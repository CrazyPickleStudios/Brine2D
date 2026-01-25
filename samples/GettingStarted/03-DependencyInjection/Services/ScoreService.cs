using Microsoft.Extensions.Logging;

namespace DependencyInjection.Services;

/// <summary>
///     Implementation of score management service.
///     Registered as Singleton - one instance for entire game lifetime.
/// </summary>
public class ScoreService : IScoreService
{
    private readonly ILogger<ScoreService> _logger;
    private float _currentScore;
    private float _highScore;

    // Services can inject other services!
    public ScoreService(ILogger<ScoreService> logger)
    {
        _logger = logger;
        _logger.LogInformation("ScoreService created (Singleton)");
    }

    public void AddPoints(float points)
    {
        _currentScore += points;

        // Update high score
        if (_currentScore > _highScore)
        {
            _highScore = _currentScore;
            _logger.LogInformation("New high score: {HighScore:F1}", _highScore);
        }
    }

    public float GetHighScore()
    {
        return _highScore;
    }

    public float GetScore()
    {
        return _currentScore;
    }

    public void ResetScore()
    {
        _logger.LogDebug("Score reset from {OldScore:F1} to 0", _currentScore);
        _currentScore = 0;
    }
}