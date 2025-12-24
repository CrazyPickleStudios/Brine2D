using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Text;

namespace Brine2D.Core
{
    /// <summary>
    /// Base class for game scenes.
    /// </summary>
    public abstract class Scene : IScene
    {
        private readonly ILogger _logger;

        /// <summary>
        /// Gets the logger for this scene.
        /// </summary>
        protected ILogger Logger => _logger;

        /// <inheritdoc/>
        public virtual string Name => GetType().Name;

        /// <inheritdoc/>
        public bool IsActive { get; private set; }
        
        protected Scene(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public virtual void Initialize()
        {
            _logger.LogDebug("Initializing scene: {SceneName}", Name);
            OnInitialize();
            IsActive = true;
        }

        /// <inheritdoc/>
        public virtual async Task LoadAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Loading scene: {SceneName}", Name);
            await OnLoadAsync(cancellationToken);
        }

        /// <inheritdoc/>
        public void Update(GameTime gameTime)
        {
            if (!IsActive) return;
            OnUpdate(gameTime);
        }

        /// <inheritdoc/>
        public void Render(GameTime gameTime)
        {
            if (!IsActive) return;
            OnRender(gameTime);
        }

        /// <inheritdoc/>
        public virtual async Task UnloadAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogInformation("Unloading scene: {SceneName}", Name);
            await OnUnloadAsync(cancellationToken);
            IsActive = false;
        }

        /// <summary>
        /// Called during initialization. Override to provide custom initialization logic.
        /// </summary>
        protected virtual void OnInitialize() { }

        /// <summary>
        /// Called during loading. Override to load resources asynchronously.
        /// </summary>
        protected virtual Task OnLoadAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Called every frame to update game logic. Override to provide custom update logic.
        /// </summary>
        protected virtual void OnUpdate(GameTime gameTime) { }

        /// <summary>
        /// Called every frame to render. Override to provide custom rendering logic.
        /// </summary>
        protected virtual void OnRender(GameTime gameTime) { }

        /// <summary>
        /// Called during unloading. Override to clean up resources.
        /// </summary>
        protected virtual Task OnUnloadAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}
