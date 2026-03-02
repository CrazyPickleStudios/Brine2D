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
using Brine2D.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Brine2D.Engine;

/// <summary>
///     Manages scene lifetime: loading, transitions, loading screens, and frame-boundary deferral.
/// </summary>
internal sealed class SceneManager : ISceneManager, IAsyncDisposable
{
    // Must match the key used by CameraSystem and ICameraManager.GetCamera("main").
    private const string MainCameraKey = "main";

    private readonly int _loadingScreenMinimumDisplayMs;
    private readonly ILogger<SceneManager> _logger;
    private readonly HashSet<Type> _registeredScenes;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly SceneWorldConfiguration? _sceneWorldConfig;
    private readonly ILoggerFactory _loggerFactory;
    private readonly IRenderer _renderer;
    private readonly IInputContext _inputContext;
    private readonly IAudioService _audioService;
    private readonly IGameContext _gameContext;
    private readonly ICameraManager _cameraManager;

    private LoadingScene? _activeLoadingScreen;
    private ISceneTransition? _activeTransition;
    private IServiceScope? _currentSceneScope;
    private LoadingScene? _deferredLoadingScreen;
    private Func<IServiceProvider, Scene>? _deferredSceneFactory;
    private Type? _deferredSceneType;
    private ISceneTransition? _deferredTransition;

    private bool _isProcessingFrame;
    private bool _isTransitioning;
    private bool _isLoadingScene;

    private readonly HashSet<Type> _warnedUnregisteredScenes = [];
    private readonly CancellationTokenSource _disposeCts = new();
    private int _disposed;

    public SceneManager(
        ILogger<SceneManager> logger,
        IServiceScopeFactory scopeFactory,
        IRenderer renderer,
        IMainThreadDispatcher mainThreadDispatcher,
        ILoggerFactory loggerFactory,
        IInputContext inputContext,
        IAudioService audioService,
        IGameContext gameContext,
        ICameraManager cameraManager,
        SceneWorldConfiguration? sceneWorldConfig = null,
        RegisteredSceneRegistry? sceneRegistry = null,
        Brine2DOptions? options = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _renderer = renderer ?? throw new ArgumentNullException(nameof(renderer));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));
        _registeredScenes = sceneRegistry != null
            ? new HashSet<Type>(sceneRegistry.SceneTypes)
            : [];
        _loadingScreenMinimumDisplayMs = options?.LoadingScreenMinimumDisplayMs ?? 200;
        _loggerFactory = loggerFactory ?? throw new ArgumentNullException(nameof(loggerFactory));
        _inputContext = inputContext ?? throw new ArgumentNullException(nameof(inputContext));
        _audioService = audioService ?? throw new ArgumentNullException(nameof(audioService));
        _gameContext = gameContext ?? throw new ArgumentNullException(nameof(gameContext));
        _cameraManager = cameraManager ?? throw new ArgumentNullException(nameof(cameraManager));
        _sceneWorldConfig = sceneWorldConfig;
    }

    public Scene? CurrentScene { get; private set; }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        _disposeCts.Cancel();

        if (_isLoadingScene)
        {
            _logger.LogWarning(
                "DisposeAsync called during an active scene transition; " +
                "the transition will not complete and scene cleanup may be partial");
        }

        if (_activeLoadingScreen != null)
        {
            try { await _activeLoadingScreen.OnUnloadAsync(CancellationToken.None); }
            catch (Exception ex) { _logger.LogError(ex, "Error unloading loading screen during dispose"); }
            _activeLoadingScreen = null;
        }

        var scene = CurrentScene;
        if (scene is not null and not LoadingScene)
        {
            try
            {
                scene.OnExit();
                await scene.OnUnloadAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error unloading scene {SceneName} during dispose", scene.GetType().Name);
            }
        }

        _currentSceneScope?.Dispose();
        _disposeCts.Dispose();
    }

    public Task LoadSceneAsync<TScene>(
        ISceneTransition? transition = null,
        CancellationToken cancellationToken = default)
        where TScene : Scene
        => LoadSceneAsync(typeof(TScene), transition, null, cancellationToken);

    public Task LoadSceneAsync<TScene, TLoadingScene>(
        ISceneTransition? transition = null,
        CancellationToken cancellationToken = default)
        where TScene : Scene
        where TLoadingScene : LoadingScene
    {
        using var tempScope = _scopeFactory.CreateScope();
        var loadingScreen = tempScope.ServiceProvider.GetService<TLoadingScene>()
                            ?? ActivatorUtilities.CreateInstance<TLoadingScene>(tempScope.ServiceProvider);

        SetupLoadingScene(loadingScreen);

        return LoadSceneAsync(typeof(TScene), transition, loadingScreen, cancellationToken);
    }

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
    ///     Loads a scene using a custom factory function.
    ///     Use when runtime data needs to be passed to the initial scene that DI alone cannot provide.
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
        if (currentScene == null)
            return;

        currentScene.OnUpdate(gameTime);
        currentScene.World.Update(gameTime);
    }

    public void Render(GameTime gameTime)
    {
        var currentScene = CurrentScene;

        if (currentScene != null && currentScene.EnableAutomaticFrameManagement)
            _renderer.BeginFrame();

        if (_activeLoadingScreen != null)
        {
            _activeLoadingScreen.OnRender(gameTime);
        }
        else if (currentScene != null)
        {
            currentScene.World.Render(_renderer);
            currentScene.OnRender(gameTime);
        }

        if (_activeTransition != null)
            _activeTransition.Render(_renderer);

        if (currentScene != null && currentScene.EnableAutomaticFrameManagement)
        {
            _renderer.ApplyPostProcessing();
            _renderer.EndFrame();
        }
    }

    /// <summary>Called by GameLoop to mark the start of frame processing; defers scene transitions until <see cref="ProcessDeferredTransitionsAsync"/>.</summary>
    internal void BeginFrame() => _isProcessingFrame = true;

    /// <summary>Processes deferred scene transitions at frame boundaries. Called by GameLoop after Update and Render complete.</summary>
    internal Task ProcessDeferredTransitionsAsync(CancellationToken ct)
    {
        _isProcessingFrame = false;

        if (_deferredSceneFactory == null && _deferredSceneType == null)
            return Task.CompletedTask;

        return _deferredSceneFactory != null
            ? ProcessDeferredFactoryAsync(ct)
            : ProcessDeferredTypeAsync(ct);
    }

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

    private async Task LoadSceneInternalCoreAsync(
        Func<IServiceScope, Scene> resolveScene,
        string logName,
        ISceneTransition? transition,
        LoadingScene? loadingScreen,
        CancellationToken cancellationToken)
    {
        if (_isLoadingScene)
        {
            _logger.LogWarning(
                "LoadScene({Scene}) ignored; a scene load is already in progress. " +
                "Call LoadSceneAsync from within a frame (OnUpdate/OnEnter) to use deferred queuing.",
                logName);
            return;
        }

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCts.Token);
        var ct = linkedCts.Token;

        _isLoadingScene = true;

        try
        {
            _logger.LogInformation("Loading scene: {SceneName}", logName);

            if (transition != null)
            {
                _isTransitioning = true;
                _activeTransition = transition;
                _activeTransition.Begin();
            }

            var oldScene = CurrentScene;

            if (loadingScreen != null)
            {
                _activeLoadingScreen = loadingScreen;
                CurrentScene = _activeLoadingScreen;
                await _activeLoadingScreen.OnLoadAsync(ct);
            }
            else
            {
                CurrentScene = null;
            }

            if (oldScene != null)
            {
                _logger.LogDebug("Exiting current scene {SceneName}", oldScene.GetType().Name);
                oldScene.OnExit();
                await oldScene.OnUnloadAsync(ct);
            }

            _activeLoadingScreen?.UpdateProgress(0.3f, "Creating scene...");

            _logger.LogDebug("Creating new scene service scope");
            _currentSceneScope?.Dispose();
            _currentSceneScope = _scopeFactory.CreateScope();

            var scene = resolveScene(_currentSceneScope);

            var sceneType = scene.GetType();
            if (!_registeredScenes.Contains(sceneType) && _warnedUnregisteredScenes.Add(sceneType))
            {
                _logger.LogWarning(
                    "Scene {SceneName} was not registered via builder.AddScene<T>(). " +
                    "Consider registering it for automatic dependency validation at startup.",
                    sceneType.Name);
            }

            SetupScene(scene, _currentSceneScope);

            _activeLoadingScreen?.UpdateProgress(0.5f, "Loading assets...");

            await scene.OnLoadAsync(ct);
            scene.OnEnter();

            _activeLoadingScreen?.UpdateProgress(1.0f, "Ready!");

            if (_activeLoadingScreen != null && _loadingScreenMinimumDisplayMs > 0)
                await Task.Delay(_loadingScreenMinimumDisplayMs, ct);

            if (_activeLoadingScreen != null)
                await _activeLoadingScreen.OnUnloadAsync(ct);

            // Swap CurrentScene and _activeLoadingScreen together on the game thread so Update/Render never observe a partial transition.
            await _mainThreadDispatcher.RunOnMainThreadAsync(() =>
            {
                CurrentScene = scene;
                _activeLoadingScreen = null;
                _logger.LogInformation("Scene loaded successfully: {SceneName}", scene.Name);
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to load scene: {SceneName}", logName);

            await _mainThreadDispatcher.RunOnMainThreadAsync(() =>
            {
                _activeLoadingScreen = null;
                CurrentScene = null;
            }, CancellationToken.None);

            _currentSceneScope?.Dispose();
            _currentSceneScope = null;
            throw;
        }
        finally
        {
            await _mainThreadDispatcher.RunOnMainThreadAsync(() =>
            {
                _isLoadingScene = false;
                _isTransitioning = false;
                _activeTransition = null;
            }, CancellationToken.None);
        }
    }

    private async Task ProcessDeferredFactoryAsync(CancellationToken ct)
    {
        _logger.LogDebug("Processing deferred scene transition using factory");
        try
        {
            await LoadSceneInternalFactoryAsync(_deferredSceneFactory!, _deferredTransition, _deferredLoadingScreen, ct);
        }
        finally
        {
            _deferredSceneFactory = null;
            _deferredSceneType = null;
            _deferredTransition = null;
            _deferredLoadingScreen = null;
        }
    }

    private async Task ProcessDeferredTypeAsync(CancellationToken ct)
    {
        _logger.LogDebug("Processing deferred scene transition to {SceneName}", _deferredSceneType!.Name);
        try
        {
            await LoadSceneInternalAsync(_deferredSceneType, _deferredTransition, _deferredLoadingScreen, ct);
        }
        finally
        {
            _deferredSceneType = null;
            _deferredTransition = null;
            _deferredLoadingScreen = null;
        }
    }

    private void SetupLoadingScene(LoadingScene loadingScene)
    {
        loadingScene.Logger = _loggerFactory.CreateLogger(loadingScene.GetType());
        loadingScene.Renderer = _renderer;
        loadingScene.Input = _inputContext;
        loadingScene.Audio = _audioService;
        loadingScene.Game = _gameContext;
    }

    /// <remarks>
    ///     IMPORTANT: New default systems must be added here; they are not discovered automatically.
    ///     Disable per-scene via <c>world.GetSystem&lt;T&gt;()!.IsEnabled = false</c> in <c>OnEnter()</c>,
    ///     or project-wide via <c>builder.ConfigureScene(...)</c>.
    /// </remarks>
    private void SetupScene(Scene scene, IServiceScope scope)
    {
        scene.Logger = _loggerFactory.CreateLogger(scene.GetType());

        var world = scope.ServiceProvider.GetRequiredService<IEntityWorld>();

        world.AddSystem<SpriteRenderingSystem>();
        world.AddSystem<ParticleSystem>();
        world.AddSystem<VelocitySystem>();
        world.AddSystem<CollisionDetectionSystem>();
        world.AddSystem<AudioSystem>();
        world.AddSystem<CameraSystem>();
        world.AddSystem<DebugRenderer>(debug => debug.IsEnabled = false);

        _sceneWorldConfig?.Apply(world);

        _logger.LogDebug("Default engine systems added to world for scene: {SceneName}", scene.GetType().Name);

        var camera = scope.ServiceProvider.GetRequiredService<ICamera>();
        _cameraManager.RegisterCamera(MainCameraKey, camera);
        if (camera is ITrackableCamera trackable)
            trackable.TrackRegistration(_cameraManager, MainCameraKey);
        _cameraManager.MainCamera = camera;

        scene.World = world;
        scene.Renderer = _renderer;
        scene.Input = _inputContext;
        scene.Audio = _audioService;
        scene.Game = _gameContext;
    }
}