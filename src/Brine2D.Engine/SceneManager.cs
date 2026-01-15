using Brine2D.Core;
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
        private bool _isTransitioning;

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

        public Task LoadSceneAsync<TScene>(CancellationToken cancellationToken = default) where TScene : IScene
        {
            return LoadSceneAsync(typeof(TScene), null, null, cancellationToken);
        }
        
        public Task LoadSceneAsync<TScene>(ISceneTransition transition, CancellationToken cancellationToken = default) 
            where TScene : IScene
        {
            return LoadSceneAsync(typeof(TScene), null, transition, cancellationToken);
        }
        
        public Task LoadSceneAsync<TScene>(LoadingScene? loadingScreen = null, ISceneTransition? transition = null, 
                                          CancellationToken cancellationToken = default) 
            where TScene : IScene
        {
            return LoadSceneAsync(typeof(TScene), loadingScreen, transition, cancellationToken);
        }

        public Task LoadSceneAsync(Type sceneType, CancellationToken cancellationToken = default)
        {
            return LoadSceneAsync(sceneType, null, null, cancellationToken);
        }
        
        public Task LoadSceneAsync(Type sceneType, ISceneTransition transition, CancellationToken cancellationToken = default)
        {
            return LoadSceneAsync(sceneType, null, transition, cancellationToken);
        }

        public async Task LoadSceneAsync(Type sceneType, LoadingScene? loadingScreen = null, 
                                        ISceneTransition? transition = null, 
                                        CancellationToken cancellationToken = default)
        {
            if (!typeof(IScene).IsAssignableFrom(sceneType))
            {
                throw new ArgumentException($"Type {sceneType.Name} does not implement IScene", nameof(sceneType));
            }

            _logger.LogInformation("Loading scene: {SceneType} (Transition: {HasTransition}, LoadingScreen: {HasLoading})", 
                sceneType.Name, transition != null, loadingScreen != null);

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

            // Show loading screen if provided
            if (loadingScreen != null)
            {
                _activeLoadingScreen = loadingScreen;
                _activeLoadingScreen.Initialize();
                await _activeLoadingScreen.LoadAsync(cancellationToken);
                _logger.LogDebug("Loading screen initialized");
            }

            // Unload current scene
            if (CurrentScene != null)
            {
                _logger.LogDebug("Unloading current scene: {SceneName}", CurrentScene.Name);
                await CurrentScene.UnloadAsync(cancellationToken);
            }

            // Update loading progress
            _activeLoadingScreen?.UpdateProgress(0.3f, "Creating scene...");

            // Create new scene instance from DI
            var scene = (IScene)_serviceProvider.GetRequiredService(sceneType);
            _logger.LogDebug("Scene instance created from DI");

            // Update loading progress
            _activeLoadingScreen?.UpdateProgress(0.5f, "Initializing...");

            // Initialize
            scene.Initialize();
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

        public void Update(GameTime gameTime)
        {
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
            
            // Only execute hooks if scene allows it
            if (CurrentScene == null || CurrentScene.EnableLifecycleHooks)
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
            
            // Scene update (always runs)
            CurrentScene?.Update(gameTime);
            
            // Only execute hooks if scene allows it
            if (CurrentScene == null || CurrentScene.EnableLifecycleHooks)
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
            // Only do automatic frame management if scene allows it
            if (CurrentScene == null || CurrentScene.EnableAutomaticFrameManagement)
            {
                if (_renderer != null)
                {
                    _renderer.BeginFrame();
                }
            }
            
            // Render loading screen if active (takes over entire render)
            if (_activeLoadingScreen != null)
            {
                _activeLoadingScreen.Render(gameTime);
            }
            else
            {
                // Normal scene rendering
                if (CurrentScene == null || CurrentScene.EnableLifecycleHooks)
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
                CurrentScene?.Render(gameTime);
                
                if (CurrentScene == null || CurrentScene.EnableLifecycleHooks)
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
            
            // Only do automatic frame management if scene allows it
            if (CurrentScene == null || CurrentScene.EnableAutomaticFrameManagement)
            {
                if (_renderer != null)
                {
                    _renderer.EndFrame();
                }
            }
        }
    }
}