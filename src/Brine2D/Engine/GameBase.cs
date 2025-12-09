using System;
using System.Threading;
using System.Threading.Tasks;

namespace Brine2D.Engine
{
    /// <summary>
    /// Backend-agnostic base game that delegates Update/Render to a scene manager
    /// and supports non-blocking scene transitions with an optional loading scene.
    /// </summary>
    public class GameBase : IGame
    {
        protected ISceneManager Scenes { get; }
        protected IGameContext? Context { get; private set; }

        /// <summary>
        /// Creates a new GameBase with a scene manager.
        /// </summary>
        public GameBase(ISceneManager scenes)
        {
            Scenes = scenes ?? throw new ArgumentNullException(nameof(scenes));
        }

        // Protected helpers for derived games
        protected void SetInitialScene(IScene scene)
        {
            if (scene is null) throw new ArgumentNullException(nameof(scene));
            Scenes.SetInitialScene(scene);
        }

        protected void SetLoadingScene(IScene loadingScene)
        {
            if (loadingScene is null) throw new ArgumentNullException(nameof(loadingScene));
            Scenes.SetLoading(loadingScene);
        }

        protected void LoadSceneAsync(Func<IScene> sceneFactory, CancellationToken ct = default)
        {
            if (sceneFactory is null) throw new ArgumentNullException(nameof(sceneFactory));
            Scenes.LoadSceneAsync(sceneFactory, ct);
        }

        protected void SetSceneAsync(IScene scene, CancellationToken ct = default)
        {
            if (scene is null) throw new ArgumentNullException(nameof(scene));
            Scenes.SetSceneAsync(scene, ct);
        }

        // IGame implementation
        public virtual Task Initialize(IGameContext context)
        {
            Context = context ?? throw new ArgumentNullException(nameof(context));
            return Task.CompletedTask;
        }

        public void Update(GameTime time)
        {
            Scenes.Update(time);
        }

        public void Render(IRenderContext ctx)
        {
            Scenes.Render(ctx);
        }
    }
}