namespace Brine2D.Core;

/// <summary>
/// Manages application and window lifecycle events.
/// Similar to ASP.NET's IHostApplicationLifetime pattern.
/// </summary>
public interface IApplicationLifetime
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