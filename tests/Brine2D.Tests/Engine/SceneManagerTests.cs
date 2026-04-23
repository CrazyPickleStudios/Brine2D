using System.Collections.Frozen;
using Brine2D.Audio;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Engine;
using Brine2D.Hosting;
using Brine2D.Input;
using Brine2D.Rendering;
using Brine2D.Systems.Audio;
using Brine2D.Systems.Physics;
using Brine2D.Systems.Rendering;
using Brine2D.Threading;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;

namespace Brine2D.Tests.Engine;

public class SceneManagerTests : TestBase
{
    [Fact]
    public async Task LoadInitialScene_SetsCurrentScene_AndCallsLifecycleInOrder()
    {
        await using var manager = CreateManager(CreateScopeFactory(typeof(TrackingScene)));

        await manager.LoadInitialSceneAsync<TrackingScene>(null, CancellationToken.None);

        var scene = Assert.IsType<TrackingScene>(manager.CurrentScene);
        Assert.True(scene.LoadCalled);
        Assert.True(scene.EnterCalled);
        Assert.True(scene.LoadCalledBeforeEnter);
    }

    [Fact]
    public async Task LoadScene_ExitsAndUnloadsOldScene_WhenTransitioning()
    {
        await using var manager = CreateManager(CreateScopeFactory(typeof(TrackingScene), typeof(SimpleScene)));

        await manager.LoadInitialSceneAsync<TrackingScene>(null, CancellationToken.None);
        var first = Assert.IsType<TrackingScene>(manager.CurrentScene);

        await manager.LoadInitialSceneAsync<SimpleScene>(null, CancellationToken.None);

        Assert.True(first.ExitCalled);
        Assert.True(first.UnloadCalled);
        Assert.IsType<SimpleScene>(manager.CurrentScene);
    }

    [Fact]
    public async Task LoadScene_CalledFromOnEnter_QueuesPendingTransition_ThatExecutesNextFrame()
    {
        await using var manager = CreateManager(CreateScopeFactory(typeof(SimpleScene)));
        var sceneLoop = (ISceneLoop)manager;

        await manager.LoadInitialSceneAsync<TransitionOnEnterScene>(
            _ => new TransitionOnEnterScene(manager),
            CancellationToken.None);

        // Simulate the next game loop frame — the pending transition queued from OnEnter should fire.
        sceneLoop.BeginFrame();
        sceneLoop.ProcessDeferredTransitions(CancellationToken.None);

        var pendingLoad = sceneLoop.ActiveLoadTask;
        Assert.NotNull(pendingLoad);
        await pendingLoad;

        Assert.IsType<SimpleScene>(manager.CurrentScene);
    }

    [Fact]
    public async Task SceneLoadFailed_FiresEvent_AndAutoLoadsFallback_WhenNoHandlerQueuesRecovery()
    {
        var fallback = new FallbackSceneConfiguration(typeof(SimpleScene));
        await using var manager = CreateManager(
            CreateScopeFactory(typeof(ThrowingScene), typeof(SimpleScene)),
            fallback: fallback);

        SceneLoadFailedEventArgs? capturedArgs = null;
        manager.SceneLoadFailed += (_, args) => capturedArgs = args;

        await Assert.ThrowsAnyAsync<Exception>(() =>
            manager.LoadInitialSceneAsync<ThrowingScene>(null, CancellationToken.None));

        var sceneLoop = (ISceneLoop)manager;
        sceneLoop.BeginFrame();
        sceneLoop.RaiseSceneLoadFailedIfPending();
        sceneLoop.ProcessDeferredTransitions(CancellationToken.None);

        var fallbackLoad = sceneLoop.ActiveLoadTask;
        Assert.NotNull(fallbackLoad);
        await fallbackLoad;

        Assert.NotNull(capturedArgs);
        Assert.IsType<SimpleScene>(manager.CurrentScene);
    }

    [Fact]
    public async Task SceneLoadFailed_RecoveryHandlerQueuesScene_FallbackIsNotLoaded()
    {
        var fallback = new FallbackSceneConfiguration(typeof(SimpleScene));
        await using var manager = CreateManager(
            CreateScopeFactory(typeof(ThrowingScene), typeof(RecoveryScene), typeof(SimpleScene)),
            fallback: fallback);

        manager.SceneLoadFailed += (_, _) => manager.LoadScene<RecoveryScene>();

        await Assert.ThrowsAnyAsync<Exception>(() =>
            manager.LoadInitialSceneAsync<ThrowingScene>(null, CancellationToken.None));

        var sceneLoop = (ISceneLoop)manager;
        sceneLoop.BeginFrame();
        sceneLoop.RaiseSceneLoadFailedIfPending();
        sceneLoop.ProcessDeferredTransitions(CancellationToken.None);

        var recoveryLoad = sceneLoop.ActiveLoadTask;
        Assert.NotNull(recoveryLoad);
        await recoveryLoad;

        Assert.IsType<RecoveryScene>(manager.CurrentScene);
    }

    private SceneManager CreateManager(
        IServiceScopeFactory scopeFactory,
        IMainThreadDispatcher? dispatcher = null,
        FallbackSceneConfiguration? fallback = null,
        SceneWorldConfiguration? worldConfig = null) =>
        new(
            logger: NullLogger<SceneManager>.Instance,
            scopeFactory: scopeFactory,
            mainThreadDispatcher: dispatcher ?? CreateInlineDispatcher(),
            services: new SceneFrameworkServices(
            NullLoggerFactory.Instance,
            Substitute.For<IRenderer>(),
            Substitute.For<IInputContext>(),
            Substitute.For<IAudioPlayer>(),
            Substitute.For<IGameContext>()),
            cameraManager: Substitute.For<ICameraManager>(),
            options: new Brine2DOptions { LoadingScreenMinimumDisplayMs = 0 },
            sceneWorldConfig: worldConfig ?? ExcludeAllDefaultSystems(),
            fallbackSceneConfig: fallback);

    private static IMainThreadDispatcher CreateInlineDispatcher()
    {
        var dispatcher = Substitute.For<IMainThreadDispatcher>();
        dispatcher
            .RunOnMainThreadAsync(Arg.Any<Action>(), Arg.Any<CancellationToken>())
            .Returns(ci => { ci.Arg<Action>()(); return Task.CompletedTask; });
        return dispatcher;
    }

    private static IServiceScopeFactory CreateScopeFactory(params Type[] sceneTypes)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IEntityWorld>(_ => Substitute.For<IEntityWorld>());
        services.AddScoped<ICamera>(_ => Substitute.For<ICamera>());
        foreach (var type in sceneTypes)
            services.AddTransient(type);
        return services.BuildServiceProvider().GetRequiredService<IServiceScopeFactory>();
    }

    private static SceneWorldConfiguration ExcludeAllDefaultSystems() => new(
        configure: null,
        excludedSystems: new[]
        {
            typeof(SpriteRenderingSystem),
            typeof(ParticleSystem),
            typeof(Box2DPhysicsSystem),
            typeof(AudioSystem),
            typeof(CameraSystem),
            typeof(DebugRenderer),
        }.ToFrozenSet());

    private sealed class SimpleScene : Scene { }

    private sealed class RecoveryScene : Scene { }

    private sealed class TrackingScene : Scene
    {
        public bool LoadCalled { get; private set; }
        public bool EnterCalled { get; private set; }
        public bool ExitCalled { get; private set; }
        public bool UnloadCalled { get; private set; }
        public bool LoadCalledBeforeEnter { get; private set; }

        protected internal override Task OnLoadAsync(CancellationToken ct, IProgress<float>? progress = null)
        {
            LoadCalled = true;
            LoadCalledBeforeEnter = !EnterCalled;
            return Task.CompletedTask;
        }

        protected internal override void OnEnter() => EnterCalled = true;
        protected internal override void OnExit() => ExitCalled = true;

        protected internal override Task OnUnloadAsync(CancellationToken ct)
        {
            UnloadCalled = true;
            return Task.CompletedTask;
        }
    }

    private sealed class ThrowingScene : Scene
    {
        protected internal override Task OnLoadAsync(CancellationToken ct, IProgress<float>? progress = null) =>
            Task.FromException(new InvalidOperationException("Deliberate test failure"));
    }

    private sealed class TransitionOnEnterScene(ISceneManager sceneManager) : Scene
    {
        protected internal override void OnEnter() => sceneManager.LoadScene<SimpleScene>();
    }
}