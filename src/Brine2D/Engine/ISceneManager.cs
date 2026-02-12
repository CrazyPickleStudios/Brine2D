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
        Scene? CurrentScene { get; }

        /// <summary>
        /// Registers a scene type with the scene manager.
        /// </summary>
        void RegisterScene<TScene>() where TScene : Scene;

        /// <summary>
        /// Loads and activates a scene with optional transition and loading screen.
        /// </summary>
        /// <typeparam name="TScene">The scene type to load.</typeparam>
        /// <param name="transition">Optional transition effect to play during scene change.</param>
        /// <param name="cancellationToken">Optional cancellation token.</param>
        Task LoadSceneAsync<TScene>(
            ISceneTransition? transition = null,
            CancellationToken cancellationToken = default)
            where TScene : Scene;

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
            where TScene : Scene
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
        /// Loads a scene using a custom factory function.
        /// </summary>
        /// <typeparam name="TScene">The scene type to load.</typeparam>
        /// <param name="sceneFactory">Factory function to create the scene.</param>
        /// <param name="transition">Optional transition effect.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task LoadSceneAsync<TScene>(
            Func<IServiceProvider, TScene> sceneFactory,
            ISceneTransition? transition = null,
            CancellationToken cancellationToken = default)
            where TScene : Scene;

        /// <summary>
        /// Updates the current scene.
        /// </summary>
        void Update(GameTime gameTime);

        /// <summary>
        /// Renders the current scene.
        /// </summary>
        void Render(GameTime gameTime);

        /// <summary>
        /// Loads a sequence of scenes with transitions between them.
        /// </summary>
        /// <example>
        /// <code>
        /// var chain = new SceneChain()
        ///     .Then&lt;IntroScene&gt;()
        ///     .Then&lt;GameScene&gt;();
        /// 
        /// await sceneManager.LoadSceneChainAsync(chain);
        /// </code>
        /// </example>
        Task LoadSceneChainAsync(
            SceneChain chain,
            CancellationToken cancellationToken = default);
    }
}
