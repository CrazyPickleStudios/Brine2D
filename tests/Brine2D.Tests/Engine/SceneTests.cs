using Brine2D.Engine;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Rendering;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Brine2D.Tests.Engine;

public class SceneTests : TestBase
{
    [Fact]
    public async Task SceneManager_ShouldLoadScene()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IEntityWorld, EntityWorld>();
        services.Configure<ECSOptions>(options => { });
        
        var mockRenderer = Substitute.For<IRenderer>();
        services.AddSingleton(mockRenderer);
        
        services.AddSingleton<SceneManager>();
        services.AddSingleton<ISceneManager>(sp => sp.GetRequiredService<SceneManager>());
        services.AddTransient<TestScene>();
        
        var provider = services.BuildServiceProvider();
        
        using var scope = provider.CreateScope();
        var sceneManager = scope.ServiceProvider.GetRequiredService<ISceneManager>();

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
        var scene = CreateTestScene<LifecycleTestScene>();

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
}

// For lifecycle test
public class LifecycleTestScene : Scene
{
    public bool LoadCalled { get; private set; }
    public bool UpdateCalled { get; private set; }
    public bool RenderCalled { get; private set; }
    public bool UnloadCalled { get; private set; }
    
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