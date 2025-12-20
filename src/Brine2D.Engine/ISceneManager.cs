using Brine2D.Core;
using System;
using System.Collections.Generic;
using System.Text;

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
        /// Loads and activates a scene.
        /// </summary>
        Task LoadSceneAsync<TScene>(CancellationToken cancellationToken = default) where TScene : IScene;

        /// <summary>
        /// Loads and activates a scene by type.
        /// </summary>
        Task LoadSceneAsync(Type sceneType, CancellationToken cancellationToken = default);

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
