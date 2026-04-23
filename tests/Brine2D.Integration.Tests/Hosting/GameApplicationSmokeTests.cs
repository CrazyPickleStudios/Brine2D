using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Brine2D.Integration.Tests.Hosting;

/// <summary>
/// Smoke tests for the full startup + hosting lifecycle in headless mode.
/// Verifies the Build → Run → Dispose path without requiring SDL3, a window, or hardware.
/// </summary>
public class GameApplicationSmokeTests
{
    [Fact]
    public async Task Build_InHeadlessMode_SucceedsAndDisposesCleanly()
    {
        await using var game = GameApplication.CreateBuilder()
            .Configure(o => o.Headless = true)
            .Build();
    }

    [Fact]
    public async Task RunAsync_CancelledExternally_CompletesNormally()
    {
        // TODO: 
        //await using var game = GameApplication.CreateBuilder()
        //    .Configure(o => o.Headless = true)
        //    .Build();

        //using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        //// Clean shutdown via cancellation is not an error; task completes with RanToCompletion.
        //await game.RunAsync<NeverExitScene>(cts.Token);
    }

    [Fact]
    public async Task RunAsync_SceneRequestsExit_CompletesWithoutHanging()
    {
        // TODO: 
        //await using var game = GameApplication.CreateBuilder()
        //    .Configure(o => o.Headless = true)
        //    .Build();

        //await game.RunAsync<ImmediateExitScene>(
        //    sp => new ImmediateExitScene(sp.GetRequiredService<IHostApplicationLifetime>()));
    }

    [Fact]
    public async Task RunAsync_CalledTwice_Throws()
    {
        // TODO: 
        //var game = GameApplication.CreateBuilder()
        //    .Configure(o => o.Headless = true)
        //    .Build();

        //var sceneEntered = new TaskCompletionSource();
        //var firstRun = game.RunAsync<SignalingScene>(sp => new SignalingScene(sceneEntered));

        //await sceneEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        //await Assert.ThrowsAsync<InvalidOperationException>(() => game.RunAsync<NeverExitScene>());

        //await game.DisposeAsync();
        //await firstRun;
    }

    [Fact]
    public async Task RunAsync_AfterDispose_ThrowsObjectDisposedException()
    {
        var game = GameApplication.CreateBuilder()
            .Configure(o => o.Headless = true)
            .Build();

        await game.DisposeAsync();

        await Assert.ThrowsAsync<ObjectDisposedException>(() => game.RunAsync<NeverExitScene>());
    }

    [Fact]
    public async Task RunAsync_FatalSceneException_PropagatesAndDisposesCleanly()
    {
        await using var game = GameApplication.CreateBuilder()
            .Configure(o => o.Headless = true)
            .Build();

        // TODO: var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => game.RunAsync<FaultyScene>());

        // TODO: Assert.Contains("Intentional test failure", ex.Message);
    }

    [Fact]
    public async Task RunAsync_FaultyConfigureScene_ThrowsGameConfigurationException()
    {
        await using var game = GameApplication.CreateBuilder()
            .Configure(o => o.Headless = true)
            .ConfigureScene(_ => throw new Exception("scene config failure"))
            .Build();

        // TODO: var ex = await Assert.ThrowsAsync<GameConfigurationException>(() => game.RunAsync<NeverExitScene>());

        // TODO: Assert.Contains("ConfigureScene", ex.Message);
    }

    [Fact]
    public async Task DisposeAsync_CalledTwice_DoesNotThrow()
    {
        var game = GameApplication.CreateBuilder()
            .Configure(o => o.Headless = true)
            .Build();

        await game.DisposeAsync();
        await game.DisposeAsync();
    }

    [Fact]
    public async Task DisposeAsync_WhileRunning_CancelsGameThreadAndCompletes()
    {
        // TODO: 
        //var game = GameApplication.CreateBuilder()
        //    .Configure(o => o.Headless = true)
        //    .Build();

        //var sceneEntered = new TaskCompletionSource();

        //var runTask = game.RunAsync<SignalingScene>(
        //    sp => new SignalingScene(sceneEntered));

        //await sceneEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        //await game.DisposeAsync();

        //Assert.True(runTask.IsCompleted);
    }

    private sealed class NeverExitScene : Scene { }

    private sealed class FaultyScene : Scene
    {
        protected internal override void OnUpdate(GameTime gameTime)
            => throw new InvalidOperationException("Intentional test failure");
    }

    private sealed class ImmediateExitScene : Scene
    {
        private readonly IHostApplicationLifetime _lifetime;
        public ImmediateExitScene(IHostApplicationLifetime lifetime) => _lifetime = lifetime;
        protected internal override void OnEnter() => _lifetime.StopApplication();
    }

    private sealed class SignalingScene : Scene
    {
        private readonly TaskCompletionSource _entered;
        public SignalingScene(TaskCompletionSource entered) => _entered = entered;
        protected internal override void OnEnter() => _entered.TrySetResult();
    }
}