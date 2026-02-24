using Brine2D.Audio;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Hosting;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.Systems.Audio;
using Brine2D.Systems.Collision;
using Brine2D.Systems.Physics;
using Brine2D.Systems.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine
{
    /// <summary>
    /// Manages scene lifetime: loading, transitions, loading screens, and frame-boundary deferral.
    /// </summary>
    internal sealed class SceneManager : ISceneManager
    {
        private readonly ILogger<SceneManager> _logger;
        private readonly IServiceProvider _serviceProvider;
        private readonly IRenderer? _renderer;
        private readonly int _loadingScreenMinimumDisplayMs;

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

        private readonly HashSet<Type> _registeredScenes;

        public Scene? CurrentScene
        {
            get => _currentScene;
            private set => _currentScene = value;
        }

        public SceneManager(
            ILogger<SceneManager> logger,
            IServiceProvider serviceProvider,
            IRenderer? renderer = null,
            HashSet<Type>? registeredScenes = null,
            Brine2DOptions? options = null)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _renderer = renderer;
            _registeredScenes = registeredScenes ?? new HashSet<Type>();
            _loadingScreenMinimumDisplayMs = options?.LoadingScreenMinimumDisplayMs ?? 200;

            if (_renderer != null)
                _logger.LogDebug("SceneManager will handle automatic frame management");
        }

        /// <summary>
        /// Called by GameLoop to indicate frame processing has started.
        /// Scene transitions will be deferred until ProcessDeferredTransitionsAsync().
        /// </summary>
        internal void BeginFrame() => _isProcessingFrame = true;

        /// <summary>
        /// Processes deferred scene transitions at frame boundaries.
        /// Called by GameLoop after Update() and Render() complete.
        /// </summary>
        internal Task ProcessDeferredTransitionsAsync(CancellationToken ct)
        {
            _isProcessingFrame = false;

            // Fast path; no allocation when nothing is deferred (the common case, every frame)
            if (_deferredSceneFactory == null && _deferredSceneType == null)
                return Task.CompletedTask;

            return _deferredSceneFactory != null
                ? ProcessDeferredFactoryAsync(ct)
                : ProcessDeferredTypeAsync(ct);
        }

        private async Task ProcessDeferredFactoryAsync(CancellationToken ct)
        {
            _logger.LogDebug("Processing deferred scene transition using factory");
            await LoadSceneInternalFactoryAsync(_deferredSceneFactory!, _deferredTransition, _deferredLoadingScreen, ct);
            _deferredSceneFactory = null;
            _deferredSceneType = null;
            _deferredTransition = null;
            _deferredLoadingScreen = null;
        }

        private async Task ProcessDeferredTypeAsync(CancellationToken ct)
        {
            _logger.LogDebug("Processing deferred scene transition to {SceneName}", _deferredSceneType!.Name);
            await LoadSceneInternalAsync(_deferredSceneType, _deferredTransition, _deferredLoadingScreen, ct);
            _deferredSceneType = null;
            _deferredTransition = null;
            _deferredLoadingScreen = null;
        }

        // Generic method (most common use case)
        public Task LoadSceneAsync<TScene>(
            ISceneTransition? transition = null,
            CancellationToken cancellationToken = default)
            where TScene : Scene
            => LoadSceneAsync(typeof(TScene), transition, null, cancellationToken);

        // Generic with loading screen; fully sets up the loading screen before handing off
        public Task LoadSceneAsync<TScene, TLoadingScene>(
            ISceneTransition? transition = null,
            CancellationToken cancellationToken = default)
            where TScene : Scene
            where TLoadingScene : LoadingScene
        {
            var loadingScreen = _serviceProvider.GetService<TLoadingScene>()
                ?? ActivatorUtilities.CreateInstance<TLoadingScene>(_serviceProvider);

            SetupLoadingScene(loadingScreen);

            return LoadSceneAsync(typeof(TScene), transition, loadingScreen, cancellationToken);
        }

        // Type-based method (implementation with deferral support)
        public async Task LoadSceneAsync(
            Type sceneType,
            ISceneTransition? transition = null,
            LoadingScene? loadingScreen = null,
            CancellationToken cancellationToken = default)
        {
            if (!typeof(Scene).IsAssignableFrom(sceneType))
                throw new ArgumentException($"Type {sceneType.Name} does not inherit from Scene", nameof(sceneType));

            if (_isProcessingFrame)
            {
                _deferredSceneType = sceneType;
                _deferredTransition = transition;
                _deferredLoadingScreen = loadingScreen;
                return;
            }

            await LoadSceneInternalAsync(sceneType, transition, loadingScreen, cancellationToken);
        }

        /// <summary>
        /// Loads a scene using a custom factory function.
        /// Use when runtime data needs to be passed to the initial scene that DI alone cannot provide.
        /// </summary>
        public async Task LoadSceneAsync<TScene>(
            Func<IServiceProvider, TScene> sceneFactory,
            ISceneTransition? transition = null,
            LoadingScene? loadingScreen = null,
            CancellationToken cancellationToken = default)
            where TScene : Scene
        {
            ArgumentNullException.ThrowIfNull(sceneFactory);

            if (_isProcessingFrame)
            {
                _deferredSceneFactory = sp => sceneFactory(sp);
                _deferredSceneType = typeof(TScene);
                _deferredTransition = transition;
                _deferredLoadingScreen = loadingScreen;
                return;
            }

            await LoadSceneInternalFactoryAsync(sp => sceneFactory(sp), transition, loadingScreen, cancellationToken);
        }

        // -----------------------------------------------------------------------------------------
        // Setup helpers
        // -----------------------------------------------------------------------------------------

        /// <summary>
        /// Assigns framework properties to a loading screen.
        /// World is intentionally not set; loading screens have no ECS world.
        /// Previously only Logger and Renderer were set here, leaving Audio/Input/Game null
        /// and causing NREs in any LoadingScene override that accessed those properties.
        /// </summary>
        private void SetupLoadingScene(LoadingScene loadingScene)
        {
            var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            loadingScene.Logger = loggerFactory.CreateLogger(loadingScene.GetType());
            loadingScene.Renderer = _serviceProvider.GetRequiredService<IRenderer>();
            loadingScene.Input = _serviceProvider.GetRequiredService<IInputContext>();
            loadingScene.Audio = _serviceProvider.GetRequiredService<AudioService>();
            loadingScene.Game = _serviceProvider.GetRequiredService<IGameContext>();
        }

        /// <summary>
        /// Shared scene setup applied to every scene regardless of how it was created.
        /// Assigns all framework-injected properties and adds default world systems.
        /// </summary>
        private void SetupScene(Scene scene, IServiceScope scope)
        {
            var loggerFactory = _serviceProvider.GetRequiredService<ILoggerFactory>();
            scene.Logger = loggerFactory.CreateLogger(scene.GetType());

            var world = scope.ServiceProvider.GetRequiredService<IEntityWorld>();

            // Default systems; disable any via World.GetSystem<T>()!.IsEnabled = false in OnEnter()
            world.AddSystem<SpriteRenderingSystem>();
            world.AddSystem<ParticleSystem>();
            world.AddSystem<VelocitySystem>();
            world.AddSystem<CollisionDetectionSystem>();
            world.AddSystem<AudioSystem>();
            world.AddSystem<CameraSystem>();
            world.AddSystem<DebugRenderer>(debug => debug.IsEnabled = false);

            // Apply project-level scene configuration (registered via builder.ConfigureScene())
            _serviceProvider.GetService<SceneWorldConfiguration>()?.Apply(world);

            _logger.LogDebug("Default engine systems added to world for scene: {SceneName}", scene.GetType().Name);

            scene.World = world;
            scene.Renderer = _serviceProvider.GetRequiredService<IRenderer>();
            scene.Input = _serviceProvider.GetRequiredService<IInputContext>();
            scene.Audio = _serviceProvider.GetRequiredService<AudioService>();
            scene.Game = _serviceProvider.GetRequiredService<IGameContext>();
        }

        // -----------------------------------------------------------------------------------------
        // Internal load paths; thin wrappers over the shared core
        // -----------------------------------------------------------------------------------------

        // Resolves the scene from DI/ActivatorUtilities, then delegates to the core
        private Task LoadSceneInternalAsync(
            Type sceneType,
            ISceneTransition? transition,
            LoadingScene? loadingScreen,
            CancellationToken cancellationToken)
            => LoadSceneInternalCoreAsync(
                scope =>
                {
                    var scene = scope.ServiceProvider.GetService(sceneType) as Scene;
                    if (scene != null)
                    {
                        _logger.LogDebug("Loaded registered scene from DI: {SceneName}", sceneType.Name);
                        return scene;
                    }

                    scene = (Scene)ActivatorUtilities.CreateInstance(scope.ServiceProvider, sceneType);
                    _logger.LogDebug("Created unregistered scene via ActivatorUtilities: {SceneName}", sceneType.Name);
                    return scene;
                },
                sceneType.Name,
                transition, loadingScreen, cancellationToken);

        // Creates the scene via caller-supplied factory, then delegates to the core
        private Task LoadSceneInternalFactoryAsync(
            Func<IServiceProvider, Scene> sceneFactory,
            ISceneTransition? transition,
            LoadingScene? loadingScreen,
            CancellationToken cancellationToken)
            => LoadSceneInternalCoreAsync(
                scope => sceneFactory(scope.ServiceProvider)
                    ?? throw new InvalidOperationException("Scene factory returned null"),
                "factory",
                transition, loadingScreen, cancellationToken);

        /// <summary>
        /// Core scene loading logic shared by all load paths.
        /// Handles the transition, loading screen, old-scene teardown, scope creation,
        /// scene setup, lifecycle calls, and configurable minimum display delay.
        /// </summary>
        private async Task LoadSceneInternalCoreAsync(
            Func<IServiceScope, Scene> resolveScene,
            string logName,
            ISceneTransition? transition,
            LoadingScene? loadingScreen,
            CancellationToken cancellationToken)
        {
            if (_isTransitioning)
            {
                // A transition is already running (e.g., a LoadSceneAsync call arrived from an
                // async callback outside the frame boundary). Silently dropping it would cause
                // hard-to-diagnose bugs, so warn loudly instead.
                _logger.LogWarning(
                    "LoadScene({Scene}) ignored; a scene transition is already in progress. " +
                    "Call LoadSceneAsync from within a frame (OnUpdate/OnEnter) to use deferred queuing.",
                    logName);
                return;
            }

            _isTransitioning = true;

            try
            {
                _logger.LogInformation("Loading scene: {SceneName}", logName);

                // Begin visual transition effect (if any)
                if (transition != null)
                {
                    _activeTransition = transition;
                    _activeTransition.Begin();
                }

                // Swap in the loading screen and tear down the outgoing scene
                var oldScene = CurrentScene;

                if (loadingScreen != null)
                {
                    _activeLoadingScreen = loadingScreen;
                    await _activeLoadingScreen.OnLoadAsync(cancellationToken);
                    CurrentScene = _activeLoadingScreen;
                }
                else
                {
                    CurrentScene = null;
                }

                if (oldScene != null)
                {
                    _logger.LogDebug("Exiting current scene {SceneName}", oldScene.GetType().Name);
                    oldScene.OnExit();
                    await oldScene.OnUnloadAsync(cancellationToken);
                }

                _activeLoadingScreen?.UpdateProgress(0.3f, "Creating scene...");

                // Create a fresh DI scope for the new scene
                _logger.LogDebug("Creating new scene service scope");
                _currentSceneScope?.Dispose();
                _currentSceneScope = _serviceProvider.CreateScope();

                var scene = resolveScene(_currentSceneScope);

                var sceneType = scene.GetType();
                if (!_registeredScenes.Contains(sceneType))
                {
                    _logger.LogWarning(
                        "Scene {SceneName} was not registered via builder.AddScene<T>(). " +
                        "Consider registering it for automatic dependency validation at startup.",
                        sceneType.Name);
                }

                SetupScene(scene, _currentSceneScope);

                _activeLoadingScreen?.UpdateProgress(0.5f, "Loading assets...");

                await scene.OnLoadAsync(cancellationToken);
                scene.OnEnter();

                _activeLoadingScreen?.UpdateProgress(1.0f, "Ready!");

                // Hold the loading screen visible for the configured minimum duration so it
                // doesn't flash imperceptibly on fast loads. Configurable via
                // builder.Configure(o => o.LoadingScreenMinimumDisplayMs = 0) to disable.
                if (_activeLoadingScreen != null && _loadingScreenMinimumDisplayMs > 0)
                    await Task.Delay(_loadingScreenMinimumDisplayMs, cancellationToken);

                CurrentScene = scene;
                _logger.LogInformation("Scene loaded successfully: {SceneName}", scene.Name);

                if (_activeLoadingScreen != null)
                {
                    await _activeLoadingScreen.OnUnloadAsync(cancellationToken);
                    _activeLoadingScreen = null;
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to load scene: {SceneName}", logName);
                throw;
            }
            finally
            {
                _isTransitioning = false;
                _activeTransition = null;
            }
        }

        // -----------------------------------------------------------------------------------------
        // Per-frame update and render
        // -----------------------------------------------------------------------------------------

        public void Update(GameTime gameTime)
        {
            if (_activeTransition != null)
            {
                _activeTransition.Update(gameTime);

                if (_activeTransition.IsComplete)
                {
                    _activeTransition = null;
                    _isTransitioning = false;
                    _logger.LogDebug("Transition completed");
                }

                if (_activeTransition != null)
                    return;
            }

            if (_activeLoadingScreen != null)
            {
                _activeLoadingScreen.OnUpdate(gameTime);
                return;
            }

            var currentScene = CurrentScene;
            if (currentScene == null) return;

            currentScene.OnUpdate(gameTime);
            currentScene.World.Update(gameTime);
        }

        public void Render(GameTime gameTime)
        {
            var currentScene = CurrentScene;

            if (currentScene != null && currentScene.EnableAutomaticFrameManagement && _renderer != null)
                _renderer.BeginFrame();

            if (_activeLoadingScreen != null)
            {
                _activeLoadingScreen.OnRender(gameTime);
            }
            else if (currentScene != null)
            {
                if (_renderer != null)
                    currentScene.World.Render(_renderer);

                currentScene.OnRender(gameTime);
            }

            if (_activeTransition != null && _renderer != null)
                _activeTransition.Render(_renderer);

            if (currentScene != null && currentScene.EnableAutomaticFrameManagement && _renderer != null)
            {
                _renderer.ApplyPostProcessing();
                _renderer.EndFrame();
            }
        }
    }
}