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
    public async Task RunAsync_CancelledExternally_ThrowsOperationCanceledException()
    {
        await using var game = GameApplication.CreateBuilder()
            .Configure(o => o.Headless = true)
            .Build();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => game.RunAsync<NeverExitScene>(cts.Token));
    }

    [Fact]
    public async Task RunAsync_SceneRequestsExit_CompletesWithoutHanging()
    {
        await using var game = GameApplication.CreateBuilder()
            .Configure(o => o.Headless = true)
            .Build();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => game.RunAsync<ImmediateExitScene>(
                sp => new ImmediateExitScene(sp.GetRequiredService<IHostApplicationLifetime>())));
    }

    [Fact]
    public async Task DisposeAsync_WhileRunning_CancelsGameThreadAndCompletes()
    {
        var game = GameApplication.CreateBuilder()
            .Configure(o => o.Headless = true)
            .Build();

        var sceneEntered = new TaskCompletionSource();

        var runTask = game.RunAsync<SignalingScene>(
            sp => new SignalingScene(sceneEntered));

        await sceneEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));

        await game.DisposeAsync();

        Assert.True(runTask.IsCompleted);
    }

    private sealed class NeverExitScene : Scene { }

    // Uses the factory overload because IHostApplicationLifetime is registered by the
    // host after ValidateSceneDependencies runs and won't survive the startup check.
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