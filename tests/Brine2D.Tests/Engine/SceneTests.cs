using Brine2D.Engine;
using Brine2D.Core;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Brine2D.Tests.Engine;

public class SceneTests
{
    [Fact]
    public async Task SceneManager_ShouldLoadScene()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<ISceneManager, SceneManager>();
        services.AddTransient<TestScene>();
        var provider = services.BuildServiceProvider();
        var sceneManager = provider.GetRequiredService<ISceneManager>();

        // Act
        await sceneManager.LoadSceneAsync<TestScene>();

        // Assert
        sceneManager.CurrentScene.Should().BeOfType<TestScene>();
        sceneManager.CurrentScene.Should().NotBeNull();
    }

    [Fact]
    public async Task Scene_ShouldCallLifecycleHooks()
    {
        // Arrange
        var logger = NullLogger<LifecycleTestScene>.Instance;
        var scene = new LifecycleTestScene(logger);

        // Act - Follow correct lifecycle order
        await scene.InitializeAsync();
        await scene.LoadAsync();
        
        scene.Update(new GameTime());
        scene.Render(new GameTime());

        await scene.UnloadAsync();

        // Assert
        scene.LoadCalled.Should().BeTrue();
        scene.UpdateCalled.Should().BeTrue();
        scene.RenderCalled.Should().BeTrue();
        scene.UnloadCalled.Should().BeTrue();
    }
}

// Minimal scene for DI/manager test
public class TestScene : Scene
{
    public TestScene(ILogger<TestScene> logger) : base(logger) { }
}

// For lifecycle test
public class LifecycleTestScene : Scene
{
    public bool LoadCalled { get; private set; }
    public bool UpdateCalled { get; private set; }
    public bool RenderCalled { get; private set; }
    public bool UnloadCalled { get; private set; }

    public LifecycleTestScene(ILogger<LifecycleTestScene> logger) : base(logger) { }

    protected override Task OnLoadAsync(CancellationToken cancellationToken)
    {
        LoadCalled = true;
        return Task.CompletedTask;
    }

    protected override void OnUpdate(GameTime gameTime)
        => UpdateCalled = true;

    protected override void OnRender(GameTime gameTime)
        => RenderCalled = true;

    protected override Task OnUnloadAsync(CancellationToken cancellationToken)
    {
        UnloadCalled = true;
        return Task.CompletedTask;
    }
}