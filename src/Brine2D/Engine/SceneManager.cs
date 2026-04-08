using System.Collections.Concurrent;
using System.Diagnostics;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Hosting;
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
internal sealed class SceneManager : ISceneManager, ISceneLoop, IAsyncDisposable
{
    private const float ProgressComplete = 1.0f;
    private const float ProgressSceneCreated = 0.3f;
    private const float ProgressAssetsLoading = 0.5f;

    /// <remarks>
    ///     Add new default systems here; they are not discovered automatically.
    ///     Exclude project-wide via <c>builder.ExcludeDefaultSystem&lt;T&gt;()</c>.
    ///     Disable per-scene via <c>world.GetSystem&lt;T&gt;()!.IsEnabled = false</c> in <c>OnEnter()</c>,
    ///     or configure project-wide via <c>builder.ConfigureScene(...)</c>.
    ///     Registration order here has no effect on execution order;
    ///     that is governed by each system's <c>UpdateOrder</c> and <c>RenderOrder</c> properties.
    /// </remarks>
    private static readonly (Type Type, Action<IEntityWorld> Register)[] DefaultSystems =
    [
        (typeof(SpriteRenderingSystem), static w => w.AddSystem<SpriteRenderingSystem>()),
        (typeof(ParticleSystem),        static w => w.AddSystem<ParticleSystem>()),
        (typeof(VelocitySystem),        static w => w.AddSystem<VelocitySystem>()),
        (typeof(CollisionDetectionSystem), static w => w.AddSystem<CollisionDetectionSystem>()),
        (typeof(AudioSystem),           static w => w.AddSystem<AudioSystem>()),
        (typeof(CameraSystem),          static w => w.AddSystem<CameraSystem>()),
        (typeof(DebugRenderer),         static w => w.AddSystem<DebugRenderer>(static d => d.IsEnabled = false)),
    ];

    private readonly int _loadingScreenMinimumDisplayMs;
    private readonly TimeSpan _shutdownTimeout;
    private readonly ILogger<SceneManager> _logger;
    private readonly HashSet<Type> _registeredScenes;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IMainThreadDispatcher _mainThreadDispatcher;
    private readonly SceneWorldConfiguration? _sceneWorldConfig;
    private readonly SceneFrameworkServices _services;
    private readonly ICameraManager _cameraManager;
    private readonly SceneLoadErrorInfo? _sceneLoadErrorInfo;
    private readonly FallbackSceneConfiguration? _fallbackSceneConfig;

    private LoadingScene? _activeLoadingScreen;
    private ISceneTransition? _activeTransition;
    private IServiceScope? _currentSceneScope;

    // Read and written exclusively on the main thread: set in BeginFrame() and
    // ProcessDeferredTransitions(), read in LoadSceneCore overloads.
    // While a load is in flight, ProcessDeferredTransitions returns early (guarded by
    // _isLoadingScene), keeping this flag true for the full duration of the load.
    private bool _isDeferringTransitions;

    // Read and written exclusively on the main thread: set in BeginSceneLoad(),
    // cleared in the finally block of LoadSceneInternalCoreAsync via RunOnMainThreadAsync.
    // Read in DisposeAsync for a diagnostic log; staleness there is harmless.
    private bool _isLoadingScene;

    // Set in the catch block of LoadSceneInternalCoreAsync; consumed and cleared by RaiseSceneLoadFailedIfPending.
    // Written on the background load thread; read on the main thread after the pending task is observed as faulted —
    // task completion provides the happens-before guarantee. volatile makes the intent explicit.
    private volatile SceneLoadFailedEventArgs? _pendingSceneLoadFailure;

    // Tracks the in-flight load task so DisposeAsync can await its cancellation and GameLoop
    // can detect faults via ActiveLoadTask. Set on the main thread before any async work begins;
    // read on the game thread during disposal and fault detection.
    private Task? _currentLoadTask;

    // Bounded by the number of distinct scene types; typically a small set, so no cap is needed.
    private readonly ConcurrentDictionary<Type, byte> _warnedUnregisteredScenes = new();
    private readonly CancellationTokenSource _disposeCts = new();
    private int _disposed;

    // Pending cross-frame scene transition. Null when no transition is queued.
    private DeferredTransitionRequest? _pendingTransition;

    // Written and read exclusively on the main thread: assigned synchronously before the first
    // await in LoadSceneInternalCoreAsync, or inside RunOnMainThreadAsync lambdas. Read during
    // Update and Render which run on the same thread.
    private Scene? _currentScene;

    public SceneManager(
        ILogger<SceneManager> logger,
        IServiceScopeFactory scopeFactory,
        IMainThreadDispatcher mainThreadDispatcher,
        SceneFrameworkServices services,
        ICameraManager cameraManager,
        Brine2DOptions options,
        SceneLoadErrorInfo? sceneLoadErrorInfo = null,
        SceneWorldConfiguration? sceneWorldConfig = null,
        RegisteredSceneRegistry? sceneRegistry = null,
        FallbackSceneConfiguration? fallbackSceneConfig = null)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _scopeFactory = scopeFactory ?? throw new ArgumentNullException(nameof(scopeFactory));
        _mainThreadDispatcher = mainThreadDispatcher ?? throw new ArgumentNullException(nameof(mainThreadDispatcher));
        _services = services ?? throw new ArgumentNullException(nameof(services));
        _registeredScenes = sceneRegistry != null
            ? new HashSet<Type>(sceneRegistry.SceneTypes)
            : [];
        _cameraManager = cameraManager ?? throw new ArgumentNullException(nameof(cameraManager));
        _sceneLoadErrorInfo = sceneLoadErrorInfo;
        _sceneWorldConfig = sceneWorldConfig;
        _fallbackSceneConfig = fallbackSceneConfig;

        ArgumentNullException.ThrowIfNull(options);
        _loadingScreenMinimumDisplayMs = options.LoadingScreenMinimumDisplayMs;
        _shutdownTimeout = options.ShutdownTimeout;
    }

    public Scene? CurrentScene
    {
        get => _currentScene;
        private set => _currentScene = value;
    }

    public Task? ActiveLoadTask => _currentLoadTask;

    public TimeSpan ShutdownTimeout => _shutdownTimeout;

    public event EventHandler<SceneLoadFailedEventArgs>? SceneLoadFailed;

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) == 1)
            return;

        _disposeCts.Cancel();

        if (_isLoadingScene)
            _logger.LogDebug("DisposeAsync called during an active scene transition; awaiting cancellation...");

        // Await the cancelled load so its catch/finally blocks finish before we touch shared state.
        // RunOnMainThreadAsync executes inline after SignalShutdown (called by GameLoop before disposal),
        // so the background task can complete without needing the main thread queue to be drained.
        var pendingLoad = _currentLoadTask;
        if (pendingLoad is { IsCompleted: false })
        {
            try { await pendingLoad.WaitAsync(_shutdownTimeout); }
            catch (Exception ex) { _logger.LogDebug(ex, "Exception while awaiting in-flight scene load during dispose"); }
        }

        if (_activeLoadingScreen != null)
        {
            try { await TeardownLoadingScreenAsync(_activeLoadingScreen); }
            catch (Exception ex) { _logger.LogError(ex, "Error unloading loading screen during dispose"); }
            _activeLoadingScreen = null;
        }

        var scene = CurrentScene;
        if (scene is not null)
        {
            try { await TeardownSceneAsync(scene, wasEntered: true); }
            catch (Exception ex) { _logger.LogError(ex, "Error unloading scene {SceneName} during dispose", scene.GetType().Name); }
        }

        _currentSceneScope?.Dispose();
        _disposeCts.Dispose();
    }

    public void LoadScene<TScene>(
        ISceneTransition? transition = null,
        CancellationToken cancellationToken = default)
        where TScene : Scene
        => LoadSceneCore(typeof(TScene), transition, null, cancellationToken);

    public void LoadScene(
        Type sceneType,
        ISceneTransition? transition = null,
        LoadingScene? loadingScreen = null,
        CancellationToken cancellationToken = default)
    {
        if (!typeof(Scene).IsAssignableFrom(sceneType))
            throw new ArgumentException($"Type {sceneType.Name} does not inherit from Scene", nameof(sceneType));

        LoadSceneCore(sceneType, transition, loadingScreen, cancellationToken);
    }

    private void LoadSceneCore(
        Type sceneType,
        ISceneTransition? transition,
        LoadingScene? loadingScreen,
        CancellationToken cancellationToken)
    {
        if (_isDeferringTransitions || _isLoadingScene)
        {
            SetPendingTransition(sceneType.Name, new DeferredTransitionRequest(
                SceneType: sceneType,
                SceneFactory: null,
                LoadingScreenType: null,
                LoadingScreen: loadingScreen,
                Transition: transition,
                CancellationToken: cancellationToken));
            return;
        }

        // Task is tracked via _currentLoadTask; GameLoop observes faults through ActiveLoadTask.
        _ = LoadSceneInternalAsync(sceneType, transition, loadingScreen, null, cancellationToken);
    }

    public void LoadScene<TScene>(
        Func<IServiceProvider, TScene> sceneFactory,
        ISceneTransition? transition = null,
        LoadingScene? loadingScreen = null,
        CancellationToken cancellationToken = default)
        where TScene : Scene
    {
        ArgumentNullException.ThrowIfNull(sceneFactory);

        if (_isDeferringTransitions || _isLoadingScene)
        {
            SetPendingTransition(typeof(TScene).Name, new DeferredTransitionRequest(
                SceneType: null,
                SceneFactory: sp => sceneFactory(sp),
                LoadingScreenType: null,
                LoadingScreen: loadingScreen,
                Transition: transition,
                CancellationToken: cancellationToken));
            return;
        }

        // Task is tracked via _currentLoadTask; GameLoop observes faults through ActiveLoadTask.
        _ = LoadSceneInternalFactoryAsync(sp => sceneFactory(sp), transition, loadingScreen, cancellationToken);
    }

    /// <summary>
    /// Blocking initial scene load used exclusively by <see cref="GameApplication"/> before the game loop starts.
    /// Not part of the public <see cref="ISceneManager"/> contract.
    /// </summary>
    internal Task LoadInitialSceneAsync<TScene>(
        Func<IServiceProvider, TScene>? sceneFactory,
        CancellationToken cancellationToken)
        where TScene : Scene
    {
        return sceneFactory is null
            ? LoadSceneInternalAsync(typeof(TScene), null, null, null, cancellationToken)
            : LoadSceneInternalFactoryAsync(sp => sceneFactory(sp), null, null, cancellationToken);
    }

    public void FixedUpdate(GameTime fixedTime)
    {
        if (_activeTransition != null && !_activeTransition.IsComplete)
            return;

        if (_activeLoadingScreen != null)
            return;

        var currentScene = CurrentScene;
        if (currentScene == null)
            return;

        currentScene.OnFixedUpdate(fixedTime);
        currentScene.World.FixedUpdate(fixedTime);
    }

    public void Update(GameTime gameTime)
    {
        if (_activeTransition != null)
        {
            _activeTransition.Update(gameTime);

            if (_activeTransition.IsComplete)
            {
                _activeTransition = null;
                _logger.LogDebug("Transition completed");
            }
            else
            {
                _activeLoadingScreen?.OnUpdate(gameTime);
                return;
            }
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
        if (_activeLoadingScreen == null && currentScene == null && _activeTransition == null)
            return;

        var renderer = _services.Renderer;
        renderer.BeginFrame();

        // Loading screens replace the scene visually. When a transition is active alongside
        // a loading screen, only the loading screen renders beneath the transition overlay.
        if (_activeLoadingScreen != null)
            _activeLoadingScreen.OnRender(gameTime);
        else if (currentScene != null)
        {
            currentScene.World.Render(renderer);
            currentScene.OnRender(gameTime);
        }

        _activeTransition?.Render(renderer);

        renderer.ApplyPostProcessing();
        renderer.EndFrame();
    }

    /// <summary>
    /// Called by GameLoop at the start of each frame. Enables transition deferral for the duration of
    /// Update/Render and for any background scene load already in flight — see <see cref="_isDeferringTransitions"/>.
    /// </summary>
    public void BeginFrame() => _isDeferringTransitions = true;

    /// <summary>
    /// Resets deferral and fires any queued scene transition. Called by GameLoop after Update and Render.
    /// The active load task is exposed via <see cref="ActiveLoadTask"/> so GameLoop can track it for
    /// faulted-load detection independently of this method's invocation.
    /// </summary>
    /// <remarks>
    /// Any transition queued while a load is in flight is intentionally abandoned on shutdown.
    /// <see cref="DisposeAsync"/> awaits the in-flight load's cancellation, and the game loop stops
    /// calling this method once it exits — so the queued request simply goes unprocessed.
    /// </remarks>
    public void ProcessDeferredTransitions(CancellationToken ct)
    {
        if (_isLoadingScene)
            return;

        _isDeferringTransitions = false;

        if (!_pendingTransition.HasValue)
            return;

        var req = _pendingTransition.Value;
        _pendingTransition = null;

        if (req.SceneFactory != null)
            _ = ProcessDeferredFactoryAsync(req, ct);
        else
            _ = ProcessDeferredTypeAsync(req, ct);
    }

    /// <summary>
    /// Fires <see cref="SceneLoadFailed"/> if a failure was recorded by the last load attempt.
    /// Called by GameLoop immediately after <see cref="BeginFrame"/> so that any handler calling
    /// <see cref="LoadScene{TScene}()"/> will defer correctly and be tracked as the next pending transition.
    /// If no handler queues a recovery transition, the registered fallback scene is loaded automatically.
    /// </summary>
    public void RaiseSceneLoadFailedIfPending()
    {
        var args = _pendingSceneLoadFailure;
        _pendingSceneLoadFailure = null;
        if (args == null)
            return;

        try
        {
            SceneLoadFailed?.Invoke(this, args);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unhandled exception in SceneLoadFailed handler");
        }

        if (!_pendingTransition.HasValue)
        {
            if (_fallbackSceneConfig != null)
            {
                _sceneLoadErrorInfo?.Set(args);
                _logger.LogWarning(
                    "No recovery transition queued after SceneLoadFailed; loading fallback scene {FallbackType}",
                    _fallbackSceneConfig.FallbackSceneType.Name);
                LoadScene(_fallbackSceneConfig.FallbackSceneType);
            }
            else
            {
                _logger.LogWarning(
                    "Scene '{SceneName}' failed to load with no recovery transition queued and no fallback scene configured. The engine will continue with no active scene.",
                    args.SceneName);
            }
        }
    }

    private void SetPendingTransition(string newSceneName, DeferredTransitionRequest req)
    {
        if (_pendingTransition.HasValue)
        {
            var queued = _pendingTransition.Value.SceneType?.Name ?? "factory";
            throw new InvalidOperationException(
                $"LoadScene('{newSceneName}') called while a transition to '{queued}' is already pending. " +
                "Only one deferred transition is allowed per frame. " +
                "Coordinate competing transition sources to ensure only one fires per frame.");
        }

        _pendingTransition = req;
    }

    private async Task RunWithLoadingScreenAsync(
        LoadingScene? screen,
        string logName,
        CancellationToken ct,
        Func<Task> body)
    {
        var loadingScreenEntered = false;

        try
        {
            if (screen != null)
            {
                _activeLoadingScreen = screen;
                await screen.OnLoadAsync(ct);
                await _mainThreadDispatcher.RunOnMainThreadAsync(screen.OnEnter, CancellationToken.None);
                loadingScreenEntered = true;
            }

            await body();

            if (_activeLoadingScreen != null)
            {
                if (_loadingScreenMinimumDisplayMs > 0)
                    await Task.Delay(_loadingScreenMinimumDisplayMs, ct);

                var screenToTeardown = _activeLoadingScreen;
                await _mainThreadDispatcher.RunOnMainThreadAsync(() => _activeLoadingScreen = null, CancellationToken.None);
                await TeardownLoadingScreenAsync(screenToTeardown, wasEntered: true, ct);
            }
        }
        catch
        {
            var screenToClean = _activeLoadingScreen;
            if (screenToClean != null)
            {
                try { await TeardownLoadingScreenAsync(screenToClean, wasEntered: loadingScreenEntered); }
                catch (Exception ex) { _logger.LogError(ex, "Error cleaning up loading screen during failed load: {SceneName}", logName); }
            }
            throw;
        }
    }

    private Task LoadSceneInternalAsync(
        Type sceneType,
        ISceneTransition? transition,
        LoadingScene? loadingScreen,
        Type? loadingScreenType,
        CancellationToken cancellationToken)
    {
        BeginSceneLoad();
        var task = LoadSceneInternalCoreAsync(
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
            transition, loadingScreen, loadingScreenType, cancellationToken);
        _currentLoadTask = task;
        return task;
    }

    private Task LoadSceneInternalFactoryAsync(
        Func<IServiceProvider, Scene> sceneFactory,
        ISceneTransition? transition,
        LoadingScene? loadingScreen,
        CancellationToken cancellationToken)
    {
        BeginSceneLoad();
        var task = LoadSceneInternalCoreAsync(
            scope => sceneFactory(scope.ServiceProvider)
                     ?? throw new InvalidOperationException("Scene factory returned null"),
            "factory",
            transition, loadingScreen, null, cancellationToken);
        _currentLoadTask = task;
        return task;
    }

    public void LoadScene<TScene, TLoadingScene>(
        ISceneTransition? transition = null,
        CancellationToken cancellationToken = default)
        where TScene : Scene
        where TLoadingScene : LoadingScene
    {
        if (_isDeferringTransitions || _isLoadingScene)
        {
            SetPendingTransition(typeof(TScene).Name, new DeferredTransitionRequest(
                SceneType: typeof(TScene),
                SceneFactory: null,
                LoadingScreenType: typeof(TLoadingScene),
                LoadingScreen: null,
                Transition: transition,
                CancellationToken: cancellationToken));
            return;
        }

        // Task is tracked via _currentLoadTask; GameLoop observes faults through ActiveLoadTask.
        _ = LoadSceneInternalAsync(typeof(TScene), transition, null, typeof(TLoadingScene), cancellationToken);
    }

    private async Task LoadSceneInternalCoreAsync(
        Func<IServiceScope, Scene> resolveScene,
        string logName,
        ISceneTransition? transition,
        LoadingScene? loadingScreen,
        Type? loadingScreenType,
        CancellationToken cancellationToken)
    {
        Debug.Assert(_isLoadingScene, $"Callers must invoke {nameof(BeginSceneLoad)} before calling this method.");

        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _disposeCts.Token);
        var ct = linkedCts.Token;

        Scene? newScene = null;
        var sceneEntered = false;

        // Owns the DI scope for a loading screen resolved by type. Null when the caller
        // already provided a fully constructed LoadingScene instance.
        IServiceScope? loadingScreenScope = null;

        // Track the outgoing scene and its DI scope so we can tear it down AFTER
        // the incoming scene's assets are loaded. This keeps shared assets' ref counts
        // above zero during the transition, preventing unnecessary unload/reload cycles.
        Scene? previousScene = null;
        IServiceScope? previousSceneScope = null;

        try
        {
            _logger.LogInformation("Loading scene: {SceneName}", logName);

            if (transition != null)
            {
                _activeTransition = transition;
                _activeTransition.Begin();
            }

            previousScene = CurrentScene;
            CurrentScene = null;

            // Resolve the loading screen from its type here, inside the try block, so any
            // DI or activation failure is caught and reported via SceneLoadFailed rather than
            // silently lost in a fire-and-forget outer wrapper.
            if (loadingScreen == null && loadingScreenType != null)
            {
                loadingScreenScope = _scopeFactory.CreateScope();
                loadingScreen = (LoadingScene)(
                    loadingScreenScope.ServiceProvider.GetService(loadingScreenType)
                    ?? ActivatorUtilities.CreateInstance(loadingScreenScope.ServiceProvider, loadingScreenType));
                SetupLoadingScene(loadingScreen);
            }

            await RunWithLoadingScreenAsync(loadingScreen, logName, ct, async () =>
            {
                _activeLoadingScreen?.UpdateProgress(ProgressSceneCreated, "Creating scene...");

                _logger.LogDebug("Creating new scene service scope");
                previousSceneScope = _currentSceneScope;
                _currentSceneScope = _scopeFactory.CreateScope();

                newScene = resolveScene(_currentSceneScope);

                var sceneType = newScene.GetType();
                if (!_registeredScenes.Contains(sceneType) && _warnedUnregisteredScenes.TryAdd(sceneType, 0))
                {
                    _logger.LogWarning(
                        "Scene {SceneName} was not registered via builder.AddScene<T>(). " +
                        "Consider registering it for automatic dependency validation at startup.",
                        sceneType.Name);
                }

                SetupScene(newScene, _currentSceneScope);

                _activeLoadingScreen?.UpdateProgress(ProgressAssetsLoading, "Loading assets...");

                var activeScreen = _activeLoadingScreen;
                var sceneProgress = activeScreen != null
                    ? new Progress<float>(p => activeScreen.UpdateProgress(
                        ProgressAssetsLoading + Math.Clamp(p, 0f, 1f) * (ProgressComplete - ProgressAssetsLoading)))
                    : null;

                // Load incoming scene assets BEFORE tearing down the outgoing scene.
                // Shared assets between the two scenes keep their ref counts > 0,
                // avoiding unnecessary unload/reload cycles during the transition.
                await newScene.OnLoadAsync(ct, sceneProgress);

                if (previousScene != null)
                {
                    _logger.LogDebug("Tearing down previous scene: {SceneName}", previousScene.GetType().Name);
                    await TeardownSceneAsync(previousScene, wasEntered: true, ct);
                    previousScene = null;
                }

                previousSceneScope?.Dispose();
                previousSceneScope = null;

                _activeLoadingScreen?.UpdateProgress(ProgressComplete, "Ready!");
            });

            var scope = _currentSceneScope!;
            await _mainThreadDispatcher.RunOnMainThreadAsync(() =>
            {
                RegisterCameraForScene(scope);
                CurrentScene = newScene;
                try
                {
                    newScene!.OnEnter();
                    sceneEntered = true;
                    _logger.LogInformation("Scene loaded successfully: {SceneName}", newScene.Name);
                }
                catch
                {
                    _cameraManager.RemoveCamera(ICameraManager.MainCameraName);
                    throw;
                }
            }, CancellationToken.None);
        }
        catch (Exception ex)
        {
            // Distinguish deliberate cancellations from genuine load failures. Cancellations
            // triggered by the caller's token or by disposal are expected and must not surface
            // as SceneLoadFailed — that event is reserved for unexpected errors.
            var isCancellation = ex is OperationCanceledException
                && (cancellationToken.IsCancellationRequested || _disposeCts.IsCancellationRequested);

            if (isCancellation)
                _logger.LogDebug("Scene load cancelled: {SceneName}", logName);
            else
                _logger.LogError(ex, "Failed to load scene: {SceneName}", logName);

            if (newScene != null)
            {
                // Use CancellationToken.None for error-path cleanup so it always completes.
                try { await TeardownSceneAsync(newScene, sceneEntered); }
                catch (Exception cleanupEx)
                {
                    _logger.LogError(cleanupEx, "Error cleaning up partially-loaded scene: {SceneName}", logName);
                }
            }

            if (previousScene != null)
            {
                try { await TeardownSceneAsync(previousScene, wasEntered: true); }
                catch (Exception teardownEx)
                {
                    _logger.LogError(teardownEx, "Error tearing down previous scene during failed load: {SceneName}",
                        previousScene.GetType().Name);
                }
            }

            await _mainThreadDispatcher.RunOnMainThreadAsync(() =>
            {
                _activeLoadingScreen = null;
                _activeTransition = null;
                CurrentScene = null;
            }, CancellationToken.None);

            _currentSceneScope?.Dispose();
            _currentSceneScope = null;
            previousSceneScope?.Dispose();

            if (!isCancellation)
                _pendingSceneLoadFailure = new SceneLoadFailedEventArgs(logName, ex);
            throw;
        }
        finally
        {
            // Dispose the loading screen scope after the loading screen has been fully torn down
            // (OnExit + OnUnloadAsync are called in both the success and catch paths above).
            loadingScreenScope?.Dispose();

            await _mainThreadDispatcher.RunOnMainThreadAsync(() => { _isLoadingScene = false; },
                CancellationToken.None);
        }
    }

    private void BeginSceneLoad()
    {
        if (_isLoadingScene)
            throw new InvalidOperationException(
                $"Cannot start a new scene load while one is already in progress. " +
                $"Under normal usage this guard is unreachable: all {nameof(LoadScene)} overloads " +
                "defer automatically when a load is in flight.");
        _isLoadingScene = true;
    }

    private async Task ProcessDeferredFactoryAsync(DeferredTransitionRequest req, CancellationToken loopToken)
    {
        _logger.LogDebug("Processing deferred scene transition using factory");
        Task? loadTask = null;
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(req.CancellationToken, loopToken);
            loadTask = LoadSceneInternalFactoryAsync(req.SceneFactory!, req.Transition, req.LoadingScreen, linkedCts.Token);
            await loadTask;
        }
        catch (OperationCanceledException) when (loopToken.IsCancellationRequested) { }
        // Guard against double-reporting: LoadSceneInternalCoreAsync sets _pendingSceneLoadFailure before
        // rethrowing, and GameLoop's fault-detection path raises SceneLoadFailed from there.
        // loadTask is captured locally so the check is stable regardless of _currentLoadTask reassignment.
        catch (Exception ex) when (loadTask?.IsFaulted != true)
        {
            _logger.LogError(ex, "Unhandled exception in deferred factory scene transition");
        }
    }

    private async Task ProcessDeferredTypeAsync(DeferredTransitionRequest req, CancellationToken loopToken)
    {
        _logger.LogDebug("Processing deferred scene transition to {SceneName}", req.SceneType!.Name);
        Task? loadTask = null;
        try
        {
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(req.CancellationToken, loopToken);
            loadTask = LoadSceneInternalAsync(req.SceneType!, req.Transition, req.LoadingScreen, req.LoadingScreenType, linkedCts.Token);
            await loadTask;
        }
        catch (OperationCanceledException) when (loopToken.IsCancellationRequested) { }
        // Guard against double-reporting: LoadSceneInternalCoreAsync sets _pendingSceneLoadFailure before
        // rethrowing, and GameLoop's fault-detection path raises SceneLoadFailed from there.
        // loadTask is captured locally so the check is stable regardless of _currentLoadTask reassignment.
        catch (Exception ex) when (loadTask?.IsFaulted != true)
        {
            _logger.LogError(ex, "Unhandled exception in deferred scene transition to {SceneName}", req.SceneType!.Name);
        }
    }

    private async Task TeardownSceneAsync(SceneBase scene, bool wasEntered, CancellationToken ct = default)
    {
        if (wasEntered)
            await _mainThreadDispatcher.RunOnMainThreadAsync(() =>
            {
                scene.OnExit();
                scene.BeginUnload();
            }, CancellationToken.None);
        else
            scene.BeginUnload();

        await scene.OnUnloadAsync(ct);
    }

    private async Task TeardownLoadingScreenAsync(LoadingScene screen, bool wasEntered = true, CancellationToken ct = default)
    {
        await _mainThreadDispatcher.RunOnMainThreadAsync(() =>
        {
            if (wasEntered)
                screen.OnExit();
            screen.BeginUnload();
        }, CancellationToken.None);

        await screen.OnUnloadAsync(ct);
    }

    private void RegisterCameraForScene(IServiceScope scope)
    {
        var camera = scope.ServiceProvider.GetRequiredService<ICamera>();
        _cameraManager.RegisterCamera(ICameraManager.MainCameraName, camera);
    }

    private void SetupSceneBase(SceneBase scene)
    {
        scene.Logger = _services.LoggerFactory.CreateLogger(scene.GetType());
        scene.Renderer = _services.Renderer;
        scene.Input = _services.InputContext;
        scene.Audio = _services.AudioPlayer;
        scene.Game = _services.GameContext;
    }

    private void SetupLoadingScene(LoadingScene loadingScene) => SetupSceneBase(loadingScene);

    private void SetupScene(Scene scene, IServiceScope scope)
    {
        SetupSceneBase(scene);

        var world = scope.ServiceProvider.GetRequiredService<IEntityWorld>();

        foreach (var (type, register) in DefaultSystems)
        {
            if (_sceneWorldConfig?.IsExcluded(type) != true)
                register(world);
        }

        _sceneWorldConfig?.Apply(world);

        _logger.LogDebug("Default engine systems added to world for scene: {SceneName}", scene.GetType().Name);

        scene.World = world;
    }

    private readonly record struct DeferredTransitionRequest(
        Type? SceneType,
        Func<IServiceProvider, Scene>? SceneFactory,
        Type? LoadingScreenType,
        LoadingScene? LoadingScreen,
        ISceneTransition? Transition,
        CancellationToken CancellationToken);
}