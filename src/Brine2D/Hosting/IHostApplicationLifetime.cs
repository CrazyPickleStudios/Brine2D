namespace Brine2D.Hosting;

/// <summary>
/// Manages application and window lifecycle events.
/// Similar to ASP.NET's IHostApplicationLifetime pattern.
/// </summary>
public interface IHostApplicationLifetime
{
    /// <summary>
    /// Gets whether an exit has been requested.
    /// </summary>
    bool IsExitRequested { get; }

    /// <summary>
    /// Requests the application to exit gracefully.
    /// </summary>
    void RequestExit();
}