using Brine2D.Core;

namespace Brine2D.Engine;

/// <summary>
/// Manages scene lifetime, transitions, and loading screens.
/// Inject this to trigger scene navigation from within your game code.
/// </summary>
/// <remarks>
/// <b>Single deferred slot:</b> when a <c>LoadScene</c> call is made during <c>Update</c>,
/// <c>OnEnter</c>, or while a load is already in flight, it is queued for execution at the end
/// of the current frame. Only <b>one</b> transition can be queued per frame — a second call
/// throws <see cref="InvalidOperationException"/> to prevent silently losing an in-flight request.
/// If multiple systems might independently request a transition in the same frame, coordinate them
/// externally to ensure only one fires per frame.
/// </remarks>
public interface ISceneManager
{
    /// <summary>Gets the currently active scene, or null if no scene is loaded.</summary>
    Scene? CurrentScene { get; }

    /// <summary>
    /// Raised when a scene load fails. Subscribe to load a fallback or error scene.
    /// The handler is invoked on the main thread after <c>BeginFrame</c>, so calling
    /// <see cref="LoadScene{TScene}()"/> from within the handler will defer correctly.
    /// </summary>
    event EventHandler<SceneLoadFailedEventArgs>? SceneLoadFailed;

    /// <summary>
    /// Requests a scene transition. Fire-and-forget: the load runs in the background and the
    /// returned task does not represent load completion. React to the transition in the target
    /// scene's <see cref="Scene.OnEnter"/> or handle failures via <see cref="SceneLoadFailed"/>.
    /// </summary>
    void LoadScene<TScene>(
        ISceneTransition? transition = null,
        CancellationToken cancellationToken = default)
        where TScene : Scene;

    /// <summary>
    /// Requests a scene transition with a loading screen displayed during the load.
    /// </summary>
    void LoadScene<TScene, TLoadingScene>(
        ISceneTransition? transition = null,
        CancellationToken cancellationToken = default)
        where TScene : Scene
        where TLoadingScene : LoadingScene;

    /// <summary>
    /// Requests a scene transition by runtime <see cref="Type"/>.
    /// Use for data-driven flows where the scene type is not known at compile time.
    /// </summary>
    void LoadScene(
        Type sceneType,
        ISceneTransition? transition = null,
        LoadingScene? loadingScreen = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Requests a scene transition using a factory function.
    /// Use when you need to pass runtime data that DI alone cannot provide.
    /// </summary>
    void LoadScene<TScene>(
        Func<IServiceProvider, TScene> sceneFactory,
        ISceneTransition? transition = null,
        LoadingScene? loadingScreen = null,
        CancellationToken cancellationToken = default)
        where TScene : Scene;
}