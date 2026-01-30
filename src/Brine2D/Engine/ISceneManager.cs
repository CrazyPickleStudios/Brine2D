using Brine2D.Core;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace Brine2D.Engine
{
    /// <summary>
    /// Manages game scenes and transitions.
    /// </summary>
    public interface ISceneManager
    {
        /// <summary>
        /// Gets the currently active scene.
        /// </summary>
        IScene? CurrentScene { get; }

        /// <summary>
        /// Registers a scene type with the scene manager.
        /// </summary>
        void RegisterScene<TScene>() where TScene : IScene;

        /// <summary>
        /// Loads and activates a scene with optional transition and loading screen.
        /// </summary>
        /// <typeparam name="TScene">The scene type to load.</typeparam>
        /// <param name="transition">Optional transition effect to play during scene change.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        Task LoadSceneAsync<TScene>(
            ISceneTransition? transition = null,
            CancellationToken cancellationToken = default) 
            where TScene : IScene;

        /// <summary>
        /// Loads and activates a scene with optional transition and loading screen.
        /// </summary>
        /// <typeparam name="TScene">The scene type to load.</typeparam>
        /// <typeparam name="TLoadingScene">The loading screen type to display while loading.</typeparam>
        /// <param name="transition">Optional transition effect to play during scene change.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        Task LoadSceneAsync<TScene, TLoadingScene>(
            ISceneTransition? transition = null,
            CancellationToken cancellationToken = default) 
            where TScene : IScene
            where TLoadingScene : LoadingScene;

        /// <summary>
        /// Loads and activates a scene by type with optional transition and loading screen.
        /// </summary>
        /// <param name="sceneType">The scene type to load.</param>
        /// <param name="transition">Optional transition effect to play during scene change.</param>
        /// <param name="loadingScreen">Optional loading screen to display while loading.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        Task LoadSceneAsync(
            Type sceneType,
            ISceneTransition? transition = null,
            LoadingScene? loadingScreen = null,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the current scene.
        /// </summary>
        void Update(GameTime gameTime);

        /// <summary>
        /// Renders the current scene.
        /// </summary>
        void Render(GameTime gameTime);
    }
}
