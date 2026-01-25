namespace DependencyInjection.Services;

/// <summary>
///     Service interface for managing player score.
///     Follows ASP.NET Core convention: Interface + Implementation.
/// </summary>
public interface IScoreService
{
    /// <summary>
    ///     Adds points to the score.
    /// </summary>
    void AddPoints(float points);

    /// <summary>
    ///     Gets the high score.
    /// </summary>
    float GetHighScore();

    /// <summary>
    ///     Gets the current score.
    /// </summary>
    float GetScore();

    /// <summary>
    ///     Resets the score to zero.
    /// </summary>
    void ResetScore();
}