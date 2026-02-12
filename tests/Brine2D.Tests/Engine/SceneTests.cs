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
    public async Task Scene_ShouldCallLifecycleHooks_ThroughSceneManager()
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
        services.AddTransient<LifecycleTestScene>();
        services.AddTransient<TestScene>(); // Add second scene for unloading first
        
        var provider = services.BuildServiceProvider();
        
        using var scope = provider.CreateScope();
        var sceneManager = scope.ServiceProvider.GetRequiredService<SceneManager>();

        // Act - Load scene (triggers OnLoadAsync and OnEnter)
        await sceneManager.LoadSceneAsync<LifecycleTestScene>();
        
        var scene = sceneManager.CurrentScene as LifecycleTestScene;
        
        // Simulate game loop (SceneManager calls these internally)
        sceneManager.Update(new GameTime());
        sceneManager.Render(new GameTime());
        
        // Unload scene by loading another scene (triggers OnExit and OnUnloadAsync)
        await sceneManager.LoadSceneAsync<TestScene>();

        // Assert
        scene.Should().NotBeNull();
        scene!.LoadCalled.Should().BeTrue("OnLoadAsync should have been called");
        scene.EnterCalled.Should().BeTrue("OnEnter should have been called");
        scene.UpdateCalled.Should().BeTrue("OnUpdate should have been called");
        scene.RenderCalled.Should().BeTrue("OnRender should have been called");
        scene.ExitCalled.Should().BeTrue("OnExit should have been called");
        scene.UnloadCalled.Should().BeTrue("OnUnloadAsync should have been called");
    }

    [Fact]
    public void Scene_ShouldHaveCorrectDefaultName()
    {
        // Arrange
        var scene = new TestableScene();

        // Act
        var name = scene.Name;

        // Assert
        name.Should().Be("TestableScene");
    }

    [Fact]
    public void Scene_ShouldRespectSceneAttribute()
    {
        // Arrange
        var scene = new AttributedScene();

        // Act
        var name = scene.Name;

        // Assert
        name.Should().Be("Custom Scene Name");
    }
}

// Minimal scene for DI/manager test
public class TestScene : Scene
{
}

// Scene with custom attribute
[Scene(Name = "Custom Scene Name")]
public class AttributedScene : Scene
{
}

// For lifecycle test - tracks all lifecycle calls
public class LifecycleTestScene : Scene
{
    public bool LoadCalled { get; private set; }
    public bool EnterCalled { get; private set; }
    public bool UpdateCalled { get; private set; }
    public bool RenderCalled { get; private set; }
    public bool ExitCalled { get; private set; }
    public bool UnloadCalled { get; private set; }
    
    protected internal override Task OnLoadAsync(CancellationToken cancellationToken)
    {
        LoadCalled = true;
        return Task.CompletedTask;
    }

    protected internal override void OnEnter()
    {
        EnterCalled = true;
    }

    protected internal override void OnUpdate(GameTime gameTime)
    {
        UpdateCalled = true;
    }

    protected internal override void OnRender(GameTime gameTime)
    {
        RenderCalled = true;
    }

    protected internal override void OnExit()
    {
        ExitCalled = true;
    }

    protected internal override Task OnUnloadAsync(CancellationToken cancellationToken)
    {
        UnloadCalled = true;
        return Task.CompletedTask;
    }
}

// Testable scene that exposes protected methods for unit testing
public class TestableScene : Scene
{
    // Public wrappers for testing
    public new string Name => base.Name;
    
    public Task CallOnLoadAsync(CancellationToken ct = default) 
        => OnLoadAsync(ct);
    
    public void CallOnEnter() 
        => OnEnter();
    
    public void CallOnUpdate(GameTime gameTime) 
        => OnUpdate(gameTime);
    
    public void CallOnRender(GameTime gameTime) 
        => OnRender(gameTime);
    
    public void CallOnExit() 
        => OnExit();
    
    public Task CallOnUnloadAsync(CancellationToken ct = default) 
        => OnUnloadAsync(ct);
}