namespace Brine2D.Engine;

/// <summary>
/// Provides progress reporting during scene loading.
/// </summary>
public interface ILoadingProgress
{
    /// <summary>
    /// Reports loading progress.
    /// </summary>
    /// <param name="progress">Progress value between 0.0 and 1.0.</param>
    /// <param name="message">Optional status message.</param>
    void Report(float progress, string? message = null);
}