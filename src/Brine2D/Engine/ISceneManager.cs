namespace Brine2D.Engine;

/// <summary>
/// Manages scene lifetime, transitions, and loading screens.
/// Inject this to trigger scene navigation from within your game code.
/// </summary>
/// <example>
/// <code>
/// public class MainMenuScene : Scene
/// {
///     private readonly ISceneManager _scenes;
///
///     public MainMenuScene(ISceneManager scenes) => _scenes = scenes;
///
///     protected override void OnEnter()
///     {
///         // Navigate on button press
///         World.CreateEntity("StartButton")
///             .AddBehavior&lt;ButtonBehavior&gt;(b =>
///                 b.OnClick = () => _scenes.LoadSceneAsync&lt;GameScene&gt;());
///     }
/// }
/// </code>
/// </example>
public interface ISceneManager
{
    /// <summary>
    /// Gets the currently active scene, or null if no scene is loaded.
    /// </summary>
    Scene? CurrentScene { get; }

    /// <summary>
    /// Loads a scene by type with an optional transition.
    /// </summary>
    Task LoadSceneAsync<TScene>(
        ISceneTransition? transition = null,
        CancellationToken cancellationToken = default)
        where TScene : Scene;

    /// <summary>
    /// Loads a scene with a loading screen displayed during the transition.
    /// </summary>
    Task LoadSceneAsync<TScene, TLoadingScene>(
        ISceneTransition? transition = null,
        CancellationToken cancellationToken = default)
        where TScene : Scene
        where TLoadingScene : LoadingScene;

    /// <summary>
    /// Loads a scene using a factory function.
    /// Use when you need to pass runtime data that DI alone cannot provide
    /// (e.g., level number, save data, session info).
    /// </summary>
    Task LoadSceneAsync<TScene>(
        Func<IServiceProvider, TScene> sceneFactory,
        ISceneTransition? transition = null,
        LoadingScene? loadingScreen = null,
        CancellationToken cancellationToken = default)
        where TScene : Scene;
}