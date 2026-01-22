using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Brine2D.Core;
using Brine2D.Engine.Systems;

namespace Brine2D.Engine
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
        
        /// <summary>
        /// Set to false to disable automatic lifecycle hook execution (ECS pipelines, etc.).
        /// Use this when you want complete manual control over system execution.
        /// Default: true (hooks execute automatically - recommended for most users).
        /// </summary>
        public virtual bool EnableLifecycleHooks => true;
        
        /// <summary>
        /// Set to false to handle frame management manually (Clear/BeginFrame/EndFrame).
        /// Use this when you need custom render targets, multi-pass rendering, or post-processing.
        /// Default: true (automatic frame management - recommended for most users).
        /// </summary>
        public virtual bool EnableAutomaticFrameManagement => true;
        
        protected Scene(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        /// <inheritdoc/>
        public virtual async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            _logger.LogDebug("Initializing scene: {SceneName}", Name);
            await OnInitializeAsync(cancellationToken);
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
        /// This is for setup and configuration tasks (NOT asset loading - use OnLoadAsync for that).
        /// </summary>
        /// <remarks>
        /// Initialize is for fast setup: configuring state, creating entities (without assets), registering handlers.
        /// For loading textures, sounds, or other assets, use <see cref="OnLoadAsync"/> instead.
        /// </remarks>
        protected virtual Task OnInitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;

        /// <summary>
        /// Called during loading. Override to load resources asynchronously.
        /// This is where you load textures, sounds, build atlases, and create GPU resources.
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

        private SceneSystemConfigurator? _systemConfigurator;

        /// <summary>
        /// Override to configure scene-specific systems.
        /// Called automatically when scene loads, before OnInitializeAsync.
        /// Global systems (registered in Program.cs) run by default.
        /// </summary>
        /// <example>
        /// <code>
        /// protected override void ConfigureSystems(ISystemConfigurator systems)
        /// {
        ///     // Add scene-specific system
        ///     systems.AddUpdateSystem&lt;BenchmarkSystem&gt;();
        ///     
        ///     // Disable global system for this scene
        ///     systems.DisableSystem&lt;VelocitySystem&gt;();
        /// }
        /// </code>
        /// </example>
        protected virtual void ConfigureSystems(ISystemConfigurator systems)
        {
            // Default: no scene-specific configuration
        }
        
        /// <summary>
        /// Gets the scene's system configurator (if configured).
        /// </summary>
        internal SceneSystemConfigurator? SystemConfigurator => _systemConfigurator;
        
        /// <summary>
        /// Initializes scene-specific systems before OnInitializeAsync.
        /// Called by SceneManager during scene loading.
        /// </summary>
        internal void InitializeSystems(IServiceProvider services, ILogger logger)
        {
            _systemConfigurator = new SceneSystemConfigurator(
                services, 
                services.GetService<ILoggerFactory>()?.CreateLogger<SceneSystemConfigurator>());
            
            try
            {
                ConfigureSystems(_systemConfigurator);
                
                // Log configuration summary
                if (_systemConfigurator.SceneSystems.Count > 0 || 
                    _systemConfigurator.DisabledSystemNames.Count > 0)
                {
                    logger.LogInformation(
                        "Scene '{SceneName}' configured: {SystemCount} scene-specific systems, {DisabledCount} disabled systems",
                        Name,
                        _systemConfigurator.SceneSystems.Count,
                        _systemConfigurator.DisabledSystemNames.Count);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to configure systems for scene '{SceneName}'", Name);
                throw;
            }
        }
    }
}
