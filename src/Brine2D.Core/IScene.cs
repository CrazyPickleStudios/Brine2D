using System;
using System.Collections.Generic;
using System.Text;

namespace Brine2D.Core
{
    /// <summary>
    /// Represents a game scene (like a page or controller in ASP.NET).
    /// </summary>
    public interface IScene
    {
        /// <summary>
        /// Gets the name of the scene.
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Gets a value indicating whether the scene is active.
        /// </summary>
        bool IsActive { get; }

        /// <summary>
        /// Called when the scene is first initialized.
        /// </summary>
        void Initialize();

        /// <summary>
        /// Called when the scene is loaded.
        /// </summary>
        /// <returns>A task representing the asynchronous operation.</returns>
        Task LoadAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Called every frame to update game logic.
        /// </summary>
        void Update(GameTime gameTime);

        /// <summary>
        /// Called every frame to render.
        /// </summary>
        void Render(GameTime gameTime);

        /// <summary>
        /// Called when the scene is unloaded.
        /// </summary>
        Task UnloadAsync(CancellationToken cancellationToken = default);
    }
}
