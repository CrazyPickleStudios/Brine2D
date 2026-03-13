namespace Brine2D.Engine;

/// <summary>
/// Event arguments for <see cref="ISceneManager.SceneLoadFailed"/>.
/// </summary>
public sealed class SceneLoadFailedEventArgs(string sceneName, Exception exception) : EventArgs
{
    /// <summary>Gets the name of the scene that failed to load.</summary>
    public string SceneName { get; } = sceneName;

    /// <summary>Gets the exception that caused the failure.</summary>
    public Exception Exception { get; } = exception;
}