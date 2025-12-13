using Brine2D.Graphics;

namespace Brine2D.Engine;

/// <summary>
///     Base implementation of <see cref="IGame" /> that delegates rendering and updates to an <see cref="ISceneManager" />
///     .
///     Provides helper methods to set and load scenes and stores a reference to the <see cref="IGameContext" />.
/// </summary>
public class GameBase : IGame
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="GameBase" /> class.
    /// </summary>
    /// <param name="scenes">The scene manager responsible for managing scenes.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="scenes" /> is null.</exception>
    public GameBase(ISceneManager scenes)
    {
        Scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));
    }

    /// <summary>
    ///     Gets the current game context after initialization.
    /// </summary>
    protected IGameContext? Context { get; private set; }

    /// <summary>
    ///     Gets the scene manager used to render and update scenes.
    /// </summary>
    protected ISceneManager Scenes { get; }

    /// <summary>
    ///     Initializes the game with the provided context.
    /// </summary>
    /// <param name="context">The game context.</param>
    /// <returns>A completed task.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="context" /> is null.</exception>
    public virtual Task Initialize(IGameContext context)
    {
        Context = context ?? throw new ArgumentNullException(nameof(context));

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Renders the current scene using the provided render context.
    /// </summary>
    /// <param name="ctx">The render context.</param>
    public void Render(IRenderContext ctx)
    {
        Scenes.Render(ctx);
    }

    /// <summary>
    ///     Updates the current scene using the provided game time.
    /// </summary>
    /// <param name="time">The current game time.</param>
    public void Update(GameTime time)
    {
        Scenes.Update(time);
    }

    /// <summary>
    ///     Asynchronously loads a new scene created by the provided factory.
    /// </summary>
    /// <param name="sceneFactory">Factory function to create the scene instance.</param>
    /// <param name="ct">An optional cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="sceneFactory" /> is null.</exception>
    protected void LoadSceneAsync(Func<IScene> sceneFactory, CancellationToken ct = default)
    {
        if (sceneFactory is null)
        {
            throw new ArgumentNullException(nameof(sceneFactory));
        }

        Scenes.LoadSceneAsync(sceneFactory, ct);
    }

    /// <summary>
    ///     Sets the initial scene to be displayed when the game starts.
    /// </summary>
    /// <param name="scene">The initial scene.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="scene" /> is null.</exception>
    protected void SetInitialScene(IScene scene)
    {
        if (scene is null)
        {
            throw new ArgumentNullException(nameof(scene));
        }

        Scenes.SetInitialScene(scene);
    }

    /// <summary>
    ///     Sets the scene shown while other scenes are loading.
    /// </summary>
    /// <param name="loadingScene">The loading scene.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="loadingScene" /> is null.</exception>
    protected void SetLoadingScene(IScene loadingScene)
    {
        if (loadingScene is null)
        {
            throw new ArgumentNullException(nameof(loadingScene));
        }

        Scenes.SetLoading(loadingScene);
    }

    /// <summary>
    ///     Asynchronously sets the active scene.
    /// </summary>
    /// <param name="scene">The scene to set as active.</param>
    /// <param name="ct">An optional cancellation token.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="scene" /> is null.</exception>
    protected void SetSceneAsync(IScene scene, CancellationToken ct = default)
    {
        if (scene is null)
        {
            throw new ArgumentNullException(nameof(scene));
        }

        Scenes.SetSceneAsync(scene, ct);
    }
}