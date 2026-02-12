using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Systems;
using Brine2D.Input;
using Brine2D.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine
{
    /// <summary>
    /// Default implementation of scene management.
    /// Executes lifecycle hooks (like ECS pipelines) automatically - no manual calls needed!
    /// Handles frame management (Clear/BeginFrame/EndFrame) automatically.
    /// Supports scene transitions and loading screens.
    /// Defers scene transitions to frame boundaries for safety.
    /// Scenes can opt-out of automatic behavior for advanced control.
    /// </summary>
    internal sealed class SceneManager : ISceneManager
    {
        private readonly ILogger<SceneManager> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, Type> _registeredScenes;
        private readonly List<ISceneLifecycleHook> _hooks;
        private readonly IRenderer? _renderer;

        private ISceneTransition? _activeTransition;
        private LoadingScene? _activeLoadingScreen;
        private bool _isTransitioning = false;

        private Scene? _currentScene;
        private IServiceScope? _currentSceneScope; 

        // Deferred transition support (like EntityWorld pattern)
        private bool _isProcessingFrame = false;
        private Type? _deferredSceneType;
        private ISceneTransition? _deferredTransition;
        private LoadingScene? _deferredLoadingScreen;

        // Factory-based scene loading support
        private Func<IServiceProvider, Scene>? _deferredSceneFactory;

        public Scene? CurrentScene
        {
            get => _currentScene;
            private set => _currentScene = value;
        }

        public SceneManager(
            ILogger<SceneManager> logger,
            IServiceProvider serviceProvider,
            IEnumerable<ISceneLifecycleHook>? hooks = null,
            IRenderer? renderer = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _registeredScenes = new Dictionary<Type, Type>();
            _renderer = renderer;

            // Sort hooks by order
            _hooks = hooks?.OrderBy(h => h.Order).ToList() ?? new List<ISceneLifecycleHook>();

            if (_hooks.Any())
            {
                _logger.LogDebug("SceneManager initialized with {Count} lifecycle hooks", _hooks.Count);
                foreach (var hook in _hooks)
                {
                    _logger.LogDebug("  - {HookType} (order: {Order})",
                        hook.GetType().Name, hook.Order);
                }
            }

            if (_renderer != null)
            {
                _logger.LogDebug("SceneManager will handle automatic frame management");
            }
        }

        public void RegisterScene<TScene>() where TScene : Scene
        {
            var sceneType = typeof(TScene);
            _registeredScenes[sceneType] = sceneType;
            _logger.LogDebug("Registered scene: {SceneType}", sceneType.Name);
        }

        /// <summary>
        /// Called by GameLoop to indicate frame processing has started.
        /// Scene transitions will be deferred until ProcessDeferredTransitionsAsync().
        /// </summary>
        internal void BeginFrame()
        {
            _isProcessingFrame = true;
        }

        /// <summary>
        /// Processes deferred scene transitions at frame boundaries.
        /// Called by GameLoop after Update() and Render() complete.
        /// </summary>
        internal async Task ProcessDeferredTransitionsAsync(CancellationToken ct)
        {
            _isProcessingFrame = false;

            // Handle factory-based deferred load
            if (_deferredSceneFactory != null)
            {
                _logger.LogDebug("Processing deferred scene transition using factory");

                await LoadSceneInternalFactoryAsync(_deferredSceneFactory, _deferredTransition, _deferredLoadingScreen, ct);

                _deferredSceneFactory = null;
                _deferredSceneType = null;
                _deferredTransition = null;
                _deferredLoadingScreen = null;
            }
            // Handle type-based deferred load
            else if (_deferredSceneType != null)
            {
                _logger.LogDebug("Processing deferred scene transition to {SceneName}", _deferredSceneType.Name);

                await LoadSceneInternalAsync(_deferredSceneType, _deferredTransition, _deferredLoadingScreen, ct);

                _deferredSceneType = null;
                _deferredTransition = null;
                _deferredLoadingScreen = null;
            }
        }

        // Generic method (most common use case)
        public Task LoadSceneAsync<TScene>(
            ISceneTransition? transition = null,
            CancellationToken cancellationToken = default)
            where TScene : Scene
        {
            return LoadSceneAsync(typeof(TScene), transition, null, cancellationToken);
        }

        // Generic with loading screen
        public Task LoadSceneAsync<TScene, TLoadingScene>(
            ISceneTransition? transition = null,
            CancellationToken cancellationToken = default)
            where TScene : Scene
            where TLoadingScene : LoadingScene
        {
            var loadingScreen = _serviceProvider.GetService<TLoadingScene>();

            if (loadingScreen == null)
            {
                // Fallback: create with Activator if not registered
                loadingScreen = Activator.CreateInstance<TLoadingScene>();
            }

            if (loadingScreen != null)
            {
                var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
                loadingScreen.Logger = loggerFactory.CreateLogger(typeof(TLoadingScene));
                loadingScreen.Renderer = _serviceProvider.GetRequiredService<IRenderer>();
            }

            return LoadSceneAsync(typeof(TScene), transition, loadingScreen, cancellationToken);
        }

        // Type-based method (implementation with deferment support)
        public async Task LoadSceneAsync(
            Type sceneType,
            ISceneTransition? transition = null,
            LoadingScene? loadingScreen = null,
            CancellationToken cancellationToken = default)
        {
            if (!typeof(Scene).IsAssignableFrom(sceneType))
            {
                throw new ArgumentException($"Type {sceneType.Name} does not implement IScene", nameof(sceneType));
            }

            // Defer if called during frame processing
            if (_isProcessingFrame)
            {
                _deferredSceneType = sceneType;
                _deferredTransition = transition;
                _deferredLoadingScreen = loadingScreen;
                return;
            }

            // Safe to load immediately (called outside frame processing)
            await LoadSceneInternalAsync(sceneType, transition, loadingScreen, cancellationToken);
        }

        /// <summary>
        /// Loads a scene using a custom factory function.
        /// This allows passing runtime data to scenes that DI alone cannot provide.
        /// </summary>
        /// <typeparam name="TScene">The scene type to load.</typeparam>
        /// <param name="sceneFactory">Factory function to create the scene.</param>
        /// <param name="transition">Optional transition effect.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <example>
        /// <code>
        /// // Pass level number to scene
        /// var levelNumber = 5;
        /// await sceneManager.LoadSceneAsync&lt;GameScene&gt;(sp => 
        /// {
        ///     var renderer = sp.GetRequiredService&lt;IRenderer&gt;();
        ///     var input = sp.GetRequiredService&lt;IInputService&gt;();
        ///     var logger = sp.GetRequiredService&lt;ILogger&lt;GameScene&gt;&gt;();
        ///     return new GameScene(renderer, input, logger, levelNumber);
        /// });
        /// </code>
        /// </example>
        public async Task LoadSceneAsync<TScene>(
            Func<IServiceProvider, TScene> sceneFactory,
            ISceneTransition? transition = null,
            CancellationToken cancellationToken = default)
            where TScene : Scene
        {
            if (sceneFactory == null)
                throw new ArgumentNullException(nameof(sceneFactory));

            // Defer if called during frame processing
            if (_isProcessingFrame)
            {
                // Store the factory for deferred execution
                _deferredSceneFactory = sp => sceneFactory(sp);
                _deferredSceneType = typeof(TScene);
                _deferredTransition = transition;
                _deferredLoadingScreen = null;
                return;
            }

            // Safe to load immediately
            await LoadSceneInternalFactoryAsync(sp => sceneFactory(sp), transition, null, cancellationToken);
        }

        /// <summary>
        /// Internal method that actually performs the scene load (type-based).
        /// </summary>
        private async Task LoadSceneInternalAsync(
            Type sceneType,
            ISceneTransition? transition,
            LoadingScene? loadingScreen,
            CancellationToken cancellationToken)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            try
            {
                _logger.LogInformation("Loading scene: {SceneName}", sceneType.Name);

                // Start transition
                if (transition != null)
                {
                    _activeTransition = transition;
                    _activeTransition.Begin();
                }

                // Load and set loading screen as current scene FIRST
                if (loadingScreen != null)
                {
                    _activeLoadingScreen = loadingScreen;
                    await _activeLoadingScreen.OnLoadAsync(cancellationToken);

                    // Make loading screen the current scene
                    var oldScene = CurrentScene;
                    CurrentScene = _activeLoadingScreen;

                    // Unload old scene in background
                    if (oldScene != null)
                    {
                        _logger.LogDebug("Exiting current scene {SceneName}", oldScene.GetType().Name);
                        oldScene.OnExit();
                        
                        if (oldScene is Scene oldConcreteScene)
                        {
                            RemoveSceneSystemConfiguration(oldConcreteScene);
                        }
                        
                        await oldScene.OnUnloadAsync(cancellationToken);
                    }
                }
                else
                {
                    // No loading screen - just unload old scene
                    var oldScene = CurrentScene;
                    CurrentScene = null;
                    if (oldScene != null)
                    {
                        _logger.LogDebug("Exiting current scene {SceneName}", oldScene.GetType().Name);
                        oldScene.OnExit();
                        
                        if (oldScene is Scene oldConcreteScene)
                        {
                            RemoveSceneSystemConfiguration(oldConcreteScene);
                        }
                        
                        await oldScene.OnUnloadAsync(cancellationToken);
                    }
                }

                _activeLoadingScreen?.UpdateProgress(0.3f, "Creating scene...");

                // Create new scene
                var scene = (Scene)_serviceProvider.GetRequiredService(sceneType);

                if (scene is Scene concreteScene)
                {
                    // Set logger specific to this scene type
                    var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
                    concreteScene.Logger = loggerFactory.CreateLogger(sceneType);

                    // Set entity world (scoped per scene)
                    concreteScene.World = _serviceProvider.GetRequiredService<IEntityWorld>();

                    // Set renderer
                    concreteScene.Renderer = _serviceProvider.GetRequiredService<IRenderer>();

                    // Initialize scene-specific systems
                    concreteScene.InitializeSystems(_serviceProvider, _logger);
                    ApplySceneSystemConfiguration(concreteScene);
                }

                _activeLoadingScreen?.UpdateProgress(0.5f, "Loading assets...");

                // Load scene (loading screen renders during this)
                await scene.OnLoadAsync(cancellationToken);

                // Add
                scene.OnEnter();

                _activeLoadingScreen?.UpdateProgress(1.0f, "Ready!");

                if (_activeLoadingScreen != null)
                {
                    await Task.Delay(200, cancellationToken);
                }

                // Swap to loaded scene
                CurrentScene = scene;

                _logger.LogInformation("Scene loaded successfully: {SceneName}", scene.Name);

                // Clean up loading screen
                if (_activeLoadingScreen != null)
                {
                    await _activeLoadingScreen.OnUnloadAsync(cancellationToken);
                    _activeLoadingScreen = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load scene: {SceneName}", sceneType.Name);
                throw;
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        /// <summary>
        /// Internal method for factory-based scene loading (non-generic for deferred execution).
        /// </summary>
        private async Task LoadSceneInternalFactoryAsync(
            Func<IServiceProvider, Scene> sceneFactory,
            ISceneTransition? transition,
            LoadingScene? loadingScreen,
            CancellationToken cancellationToken)
        {
            if (_isTransitioning) return;
            _isTransitioning = true;

            _logger.LogInformation("Loading scene using factory");

            try
            {
                // START TRANSITION
                if (transition != null)
                {
                    _activeTransition = transition;
                    _activeTransition.Begin();
                }

                // UNLOAD CURRENT SCENE
                if (CurrentScene != null)
                {
                    _logger.LogDebug("Exiting current scene {SceneName}", CurrentScene.GetType().Name);
                    CurrentScene.OnExit(); 

                    if (CurrentScene is Scene oldConcreteScene)
                    {
                        RemoveSceneSystemConfiguration(oldConcreteScene);
                    }

                    await CurrentScene.OnUnloadAsync(cancellationToken);
                    CurrentScene = null;
                }

                // Dispose old scene scope
                _currentSceneScope?.Dispose();
                _currentSceneScope = null;

                // LOAD NEW SCENE
                _logger.LogDebug("Creating new scene scope");
                _currentSceneScope = _serviceProvider.CreateScope();

                // Create scene using factory
                var scene = sceneFactory(_currentSceneScope.ServiceProvider);
        
                if (scene == null)
                {
                    throw new InvalidOperationException("Scene factory returned null");
                }

                // Initialize concrete scene if applicable
                if (scene is Scene concreteScene)
                {
                    var loggerFactory = _currentSceneScope.ServiceProvider.GetRequiredService<ILoggerFactory>();
                    concreteScene.Logger = loggerFactory.CreateLogger(scene.GetType());

                    concreteScene.World = _currentSceneScope.ServiceProvider.GetRequiredService<IEntityWorld>();
                    concreteScene.Renderer = _currentSceneScope.ServiceProvider.GetRequiredService<IRenderer>();

                    concreteScene.InitializeSystems(_currentSceneScope.ServiceProvider, _logger);
                    ApplySceneSystemConfiguration(concreteScene);
                }

                // Load scene
                _logger.LogDebug("Loading scene {SceneName}", scene.GetType().Name);
                await scene.OnLoadAsync(cancellationToken);

                CurrentScene = scene;

                _logger.LogInformation("Scene {SceneName} loaded successfully", scene.GetType().Name);
        
                // Transition will complete via Update() calls in the game loop
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load scene using factory");
                throw;
            }
            finally
            {
                _isTransitioning = false;
            }
        }

        public void Update(GameTime gameTime)
        {
            // Update active transition (if any)
            if (_activeTransition != null)
            {
                _activeTransition.Update(gameTime);

                // Clear transition when complete so scene can start updating
                if (_activeTransition.IsComplete)
                {
                    _activeTransition = null;
                    _isTransitioning = false;
                    _logger.LogDebug("Transition completed");
                }

                // If transition is still active, don't update scene yet
                if (_activeTransition != null)
                {
                    return;
                }
            }

            // Update loading screen (if any)
            if (_activeLoadingScreen != null)
            {
                _activeLoadingScreen.OnUpdate(gameTime);
                return;
            }

            var currentScene = CurrentScene;
            if (currentScene == null) return;

            var world = currentScene.World;

            if (currentScene.EnableLifecycleHooks)
            {
                // Pre-update hooks
                foreach (var hook in _hooks)
                {
                    try
                    {
                        hook.PreUpdate(gameTime, world);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in PreUpdate hook: {HookType}", hook.GetType().Name);
                    }
                }
            }

            currentScene.OnUpdate(gameTime);

            if (currentScene.EnableLifecycleHooks)
            {
                world.Update(gameTime);

                // Post-update hooks
                foreach (var hook in _hooks)
                {
                    try
                    {
                        hook.PostUpdate(gameTime, world);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in PostUpdate hook: {HookType}", hook.GetType().Name);
                    }
                }
            }
        }

        public void Render(GameTime gameTime)
        {
            var currentScene = CurrentScene;

            // Automatic frame management (default)
            if (currentScene != null && currentScene.EnableAutomaticFrameManagement && _renderer != null)
            {
                _renderer.BeginFrame();
            }

            // Render loading screen if active (takes over entire render)
            if (_activeLoadingScreen != null)
            {
                _activeLoadingScreen.OnRender(gameTime);
            }
            else if (currentScene != null)
            {
                var world = currentScene.World;

                if (currentScene.EnableLifecycleHooks)
                {
                    // Pre-render hooks (ECS rendering systems)
                    foreach (var hook in _hooks)
                    {
                        try
                        {
                            hook.PreRender(gameTime, world);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in PreRender hook: {HookType}", hook.GetType().Name);
                        }
                    }
                }

                // World render (OOP components)
                if (_renderer != null && currentScene.EnableLifecycleHooks)
                {
                    world.Render(_renderer);
                }

                // Scene render (UI, overlays, custom rendering)
                currentScene.OnRender(gameTime);

                if (currentScene.EnableLifecycleHooks)
                {
                    // Post-render hooks (debug overlays, etc.)
                    foreach (var hook in _hooks)
                    {
                        try
                        {
                            hook.PostRender(gameTime, world);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in PostRender hook: {HookType}", hook.GetType().Name);
                        }
                    }
                }
            }

            // Render transition overlay on top of everything
            if (_activeTransition != null && _renderer != null)
            {
                _activeTransition.Render(_renderer);
            }

            // Automatic frame management (default)
            if (currentScene != null && currentScene.EnableAutomaticFrameManagement && _renderer != null)
            {
                _renderer.ApplyPostProcessing();
                _renderer.EndFrame();
            }
        }

        private void ApplySceneSystemConfiguration(Scene scene)
        {
            if (scene.SystemConfigurator == null) return;

            var updatePipeline = _serviceProvider.GetService<UpdatePipeline>();
            var renderPipeline = _serviceProvider.GetService<RenderPipeline>();

            // Add scene-specific systems
            foreach (var system in scene.SystemConfigurator.SceneSystems)
            {
                if (system is IUpdateSystem updateSystem)
                {
                    updatePipeline?.AddSystem(updateSystem);
                    _logger.LogDebug("Added scene-specific update system: {SystemName}", updateSystem.Name);
                }
                else if (system is IRenderSystem renderSystem)
                {
                    renderPipeline?.AddSystem(renderSystem);
                    _logger.LogDebug("Added scene-specific render system: {SystemName}", renderSystem.GetType().Name);
                }
            }

            // Disable global systems for this scene
            if (scene.SystemConfigurator.DisabledSystemNames.Count > 0)
            {
                updatePipeline?.DisableSystems(scene.SystemConfigurator.DisabledSystemNames);
                renderPipeline?.DisableSystems(scene.SystemConfigurator.DisabledSystemNames);
                _logger.LogDebug("Disabled {Count} systems for scene '{SceneName}'",
                    scene.SystemConfigurator.DisabledSystemNames.Count, scene.Name);
            }
        }

        private void RemoveSceneSystemConfiguration(Scene scene)
        {
            if (scene.SystemConfigurator == null) return;

            var updatePipeline = _serviceProvider.GetService<UpdatePipeline>();
            var renderPipeline = _serviceProvider.GetService<RenderPipeline>();

            // Remove scene-specific systems
            foreach (var system in scene.SystemConfigurator.SceneSystems)
            {
                if (system is IUpdateSystem updateSystem)
                {
                    updatePipeline?.RemoveSystem(updateSystem);
                }
                else if (system is IRenderSystem renderSystem)
                {
                    renderPipeline?.RemoveSystem(renderSystem);
                }
            }

            // Re-enable all systems
            updatePipeline?.EnableAllSystems();
            renderPipeline?.EnableAllSystems();

            _logger.LogDebug("Removed scene-specific systems for '{SceneName}'", scene.Name);
        }

        public async Task LoadSceneChainAsync(
            SceneChain chain,
            CancellationToken cancellationToken = default)
        {
            if (chain == null)
                throw new ArgumentNullException(nameof(chain));
            
            foreach (var (sceneType, transition) in chain.Scenes)
            {
                await LoadSceneAsync(sceneType, transition, null, cancellationToken);
                
                // Wait for scene to signal completion (optional - implement if needed)
                // This would require scenes to have a "completion" event/task
            }
        }
    }
}