namespace Brine2D.Engine;

/// <summary>
/// Provides read access to the most recent scene load failure.
/// Inject this into a fallback scene to display error details.
/// Populated by the framework before the fallback scene is loaded.
/// </summary>
public interface ISceneLoadErrorInfo
{
    /// <summary>Gets the name of the scene that failed to load, or <see langword="null"/> if no failure has occurred yet.</summary>
    string? FailedSceneName { get; }

    /// <summary>Gets the exception that caused the failure, or <see langword="null"/> if no failure has occurred yet.</summary>
    Exception? Exception { get; }
}

internal sealed class SceneLoadErrorInfo : ISceneLoadErrorInfo
{
    public string? FailedSceneName { get; private set; }
    public Exception? Exception { get; private set; }

    internal void Set(SceneLoadFailedEventArgs args)
    {
        FailedSceneName = args.SceneName;
        Exception = args.Exception;
    }
}