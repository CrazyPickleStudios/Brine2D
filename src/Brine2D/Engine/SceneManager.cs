using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.ECS.Systems;
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
    /// Scenes can opt-out of automatic behavior for advanced control.
    /// </summary>
    public class SceneManager : ISceneManager
    {
        private readonly ILogger<SceneManager> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, Type> _registeredScenes;
        private readonly List<ISceneLifecycleHook> _hooks;
        private readonly IRenderer? _renderer;
        
        private ISceneTransition? _activeTransition;
        private LoadingScene? _activeLoadingScreen;
        private bool _isTransitioning = false;

        public IScene? CurrentScene { get; private set; }

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

        public void RegisterScene<TScene>() where TScene : IScene
        {
            var sceneType = typeof(TScene);
            _registeredScenes[sceneType] = sceneType;
            _logger.LogDebug("Registered scene: {SceneType}", sceneType.Name);
        }

        // Generic method (most common use case)
        public Task LoadSceneAsync<TScene>(
            ISceneTransition? transition = null,
            CancellationToken cancellationToken = default) 
            where TScene : IScene
        {
            return LoadSceneAsync(typeof(TScene), transition, null, cancellationToken);
        }

        // Generic with loading screen
        public Task LoadSceneAsync<TScene, TLoadingScene>(
            ISceneTransition? transition = null,
            CancellationToken cancellationToken = default)
            where TScene : IScene
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
                // Note: Don't set World - LoadingScene doesn't use entities
            }
            
            return LoadSceneAsync(typeof(TScene), transition, loadingScreen, cancellationToken);
        }

        // Type-based method (implementation)
        public async Task LoadSceneAsync(
            Type sceneType,
            ISceneTransition? transition = null,
            LoadingScene? loadingScreen = null,
            CancellationToken cancellationToken = default)
        {
            // Guard against overlapping transitions
            if (_isTransitioning)
            {
                _logger.LogWarning("Scene transition already in progress, ignoring request to load {SceneName}", sceneType.Name);
                return;
            }

            if (!typeof(IScene).IsAssignableFrom(sceneType))
            {
                throw new ArgumentException($"Type {sceneType.Name} does not implement IScene", nameof(sceneType));
            }

            _isTransitioning = true;

            try
            {
                _logger.LogInformation("Loading scene: {SceneType} (Transition: {HasTransition}, LoadingScreen: {HasLoading})", 
                    sceneType.Name, transition != null, loadingScreen != null);

                // Start transition if provided
                if (transition != null)
                {
                    _activeTransition = transition;
                    _activeTransition.Begin();
                    // Don't set _isTransitioning here - already set above
                    
                    _logger.LogDebug("Transition started (duration: {Duration}s)", _activeTransition.Duration);
                    
                    // Capture reference for first wait loop
                    var capturedTransition = _activeTransition;
                    
                    // Wait for transition to reach midpoint (fade out complete)
                    while (capturedTransition.Progress < 0.5f && !cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(16, cancellationToken); // ~60 FPS
                    }
                }

                // Show loading screen if provided
                if (loadingScreen != null)
                {
                    _activeLoadingScreen = loadingScreen;
                    await _activeLoadingScreen.InitializeAsync(cancellationToken);
                    await _activeLoadingScreen.LoadAsync(cancellationToken);
                    _logger.LogDebug("Loading screen initialized");
                }

                var oldScene = CurrentScene;
                CurrentScene = null; // This stops SceneManager.Update() from calling it

                // Unload old scene (now it won't receive any more updates)
                if (oldScene != null)
                {
                    _logger.LogDebug("Unloading scene: {SceneName}", oldScene.Name);
                    
                    // Remove scene-specific systems
                    if (oldScene is Scene)
                    {
                        RemoveSceneSystemConfiguration((Scene)oldScene);
                    }
                    
                    await oldScene.UnloadAsync(cancellationToken);
                }

                // Update loading progress
                _activeLoadingScreen?.UpdateProgress(0.3f, "Creating scene...");

                // Create new scene instance from DI
                var scene = (IScene)_serviceProvider.GetRequiredService(sceneType);
                _logger.LogDebug("Scene instance created from DI");

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

                // Update loading progress
                _activeLoadingScreen?.UpdateProgress(0.5f, "Initializing...");

                // Initialize
                await scene.InitializeAsync(cancellationToken);
                _logger.LogDebug("Scene initialized");
                
                // Update loading progress
                _activeLoadingScreen?.UpdateProgress(0.7f, "Loading assets...");

                // Load
                await scene.LoadAsync(cancellationToken);
                _logger.LogDebug("Scene assets loaded");

                // Update loading progress
                _activeLoadingScreen?.UpdateProgress(1.0f, "Ready!");
                
                // Small delay to show 100%
                if (_activeLoadingScreen != null)
                {
                    await Task.Delay(200, cancellationToken);
                }

                // Set as current scene (scene is now active)
                CurrentScene = scene;

                // Log scene configuration
                if (!scene.EnableLifecycleHooks)
                {
                    _logger.LogInformation("Scene {SceneName} has lifecycle hooks DISABLED (manual control)", scene.Name);
                }
                if (!scene.EnableAutomaticFrameManagement)
                {
                    _logger.LogInformation("Scene {SceneName} has automatic frame management DISABLED (manual control)", scene.Name);
                }
                
                _logger.LogInformation("Scene loaded: {SceneName}", scene.Name);

                // Clean up loading screen
                if (_activeLoadingScreen != null)
                {
                    await _activeLoadingScreen.UnloadAsync(cancellationToken);
                    _activeLoadingScreen = null;
                    _logger.LogDebug("Loading screen unloaded");
                }

                // Finish transition if active
                if (_activeTransition != null)
                {
                    _logger.LogDebug("Waiting for transition to complete (fade in)...");
                    
                    var capturedTransition = _activeTransition;
                    
                    // Wait for fade in to complete
                    while (!capturedTransition.IsComplete && !cancellationToken.IsCancellationRequested)
                    {
                        await Task.Delay(16, cancellationToken);
                    }
                    
                    _activeTransition = null;
                    _isTransitioning = false;
                    _logger.LogDebug("Transition complete");
                }
            }
            finally
            {
                _isTransitioning = false;
                _logger.LogDebug("Scene transition complete, flag cleared");
            }
        }

        private async Task<T> LoadSceneInternalAsync<T>(ISceneTransition? transition, CancellationToken cancellationToken) 
            where T : Scene
        {
            // Start transition if provided
            if (transition != null)
            {
                _activeTransition = transition;
                _activeTransition.Begin();
                _isTransitioning = true;
                
                _logger.LogDebug("Transition started (duration: {Duration}s)", _activeTransition.Duration);
                
                // Capture reference for first wait loop
                var capturedTransition = _activeTransition;
                
                // Wait for transition to reach midpoint (fade out complete)
                while (capturedTransition.Progress < 0.5f && !cancellationToken.IsCancellationRequested)
                {
                    await Task.Delay(16, cancellationToken); // ~60 FPS
                }
            }

            // Create new scene instance from DI
            var scene = _serviceProvider.GetRequiredService<T>();
            
            // Initialize scene-specific systems (scene is already Scene type)
            scene.InitializeSystems(_serviceProvider, _logger);
            ApplySceneSystemConfiguration(scene);

            // Initialize
            await scene.InitializeAsync(cancellationToken);
            _logger.LogDebug("Scene initialized");
            
            // Update loading progress
            _activeLoadingScreen?.UpdateProgress(0.7f, "Loading assets...");

            // Load
            await scene.LoadAsync(cancellationToken);
            _logger.LogDebug("Scene assets loaded");

            return scene;
        }

        public void Update(GameTime gameTime)
        {
            var currentScene = CurrentScene;

            // Update transition
            if (_activeTransition != null)
            {
                _activeTransition.Update((float)gameTime.DeltaTime);
            }
            
            // Update loading screen
            if (_activeLoadingScreen != null)
            {
                _activeLoadingScreen.Update(gameTime);
                return; // Don't update current scene while loading
            }
            
            // Only execute hooks if there IS a scene
            if (currentScene != null && currentScene.EnableLifecycleHooks)
            {
                // Pre-update hooks (input layers, camera setup, etc.)
                foreach (var hook in _hooks)
                {
                    try
                    {
                        hook.PreUpdate(gameTime);
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, "Error in PreUpdate hook: {HookType}", hook.GetType().Name);
                    }
                }
            }
            
            // Scene update
            currentScene?.Update(gameTime);
            
            if (currentScene is Scene scene)
            {
                scene.EntityWorld.Update(gameTime);
            }
            
            // Only execute hooks if there IS a scene
            if (currentScene != null && currentScene.EnableLifecycleHooks)
            {
                // Post-update hooks (ECS systems, physics, AI, etc.)
                foreach (var hook in _hooks)
                {
                    try
                    {
                        hook.PostUpdate(gameTime);
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
            
            // Always do frame management if renderer exists
            if (_renderer != null)
            {
                _renderer.BeginFrame();
            }
            
            // Render loading screen if active (takes over entire render)
            if (_activeLoadingScreen != null)
            {
                _activeLoadingScreen.Render(gameTime);
            }
            else if (currentScene != null) // Only render scene if it exists
            {
                // Normal scene rendering
                if (currentScene.EnableLifecycleHooks)
                {
                    // Pre-render hooks (ECS rendering, sprites, particles, etc.)
                    foreach (var hook in _hooks)
                    {
                        try
                        {
                            hook.PreRender(gameTime);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error in PreRender hook: {HookType}", hook.GetType().Name);
                        }
                    }
                }
                
                // Scene render (UI, debug overlays)
                currentScene.Render(gameTime);
                
                if (currentScene.EnableLifecycleHooks)
                {
                    // Post-render hooks (debug overlays, final UI chrome, etc.)
                    foreach (var hook in _hooks)
                    {
                        try
                        {
                            hook.PostRender(gameTime);
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
            
            // Always end frame if renderer exists
            if (_renderer != null)
            {
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
    }
}