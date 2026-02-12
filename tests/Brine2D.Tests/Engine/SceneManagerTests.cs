using System;
using System.Threading;
using System.Threading.Tasks;
using Brine2D.Core;
using Brine2D.ECS;
using Brine2D.Engine;
using Brine2D.Rendering;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Brine2D.Tests.Engine;

public class SceneManagerTests : TestBase
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SceneManager> _logger;
    private readonly IRenderer _mockRenderer;

    public SceneManagerTests()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        
        services.AddScoped<IEntityWorld, EntityWorld>();
        services.Configure<ECSOptions>(options => { });
        
        _mockRenderer = Substitute.For<IRenderer>();
        services.AddSingleton(_mockRenderer);
        
        // Register test scenes
        services.AddTransient<TestScene>();
        services.AddTransient<AnotherTestScene>();
        services.AddTransient<OrderedTestScene>();
        services.AddTransient<ManualScene>();
        services.AddTransient<TestLoadingScreen>();
        
        _serviceProvider = services.BuildServiceProvider();
        _logger = NullLogger<SceneManager>.Instance;
    }

    [Fact]
    public async Task ShouldSetCurrentSceneAfterLoading()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        await manager.LoadSceneAsync<TestScene>();

        // Assert
        manager.CurrentScene.Should().NotBeNull();
        manager.CurrentScene.Should().BeOfType<TestScene>();
    }

    [Fact]
    public async Task ShouldLoadSceneAndCallOnLoad()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        await manager.LoadSceneAsync<TestScene>();
        var scene = manager.CurrentScene as TestScene;

        // Assert
        scene.Should().NotBeNull();
        scene!.LoadCalled.Should().BeTrue("OnLoadAsync should have been called");
        scene.EnterCalled.Should().BeTrue("OnEnter should have been called");
    }

    [Fact]
    public async Task ShouldUnloadSceneAndCallOnUnload()
    {
        // Arrange
        var manager = CreateManager();
        await manager.LoadSceneAsync<TestScene>();
        var firstScene = manager.CurrentScene as TestScene;

        // Act - Loading a new scene should unload the current one
        await manager.LoadSceneAsync<AnotherTestScene>();

        // Assert
        firstScene!.ExitCalled.Should().BeTrue("OnExit should have been called");
        firstScene.UnloadCalled.Should().BeTrue("OnUnloadAsync should have been called");
        manager.CurrentScene.Should().BeOfType<AnotherTestScene>();
    }

    [Fact]
    public async Task ShouldUpdateAndRenderActiveScene()
    {
        // Arrange
        var manager = CreateManager();
        await manager.LoadSceneAsync<TestScene>();
        var scene = manager.CurrentScene as TestScene;

        // Act
        manager.Update(new GameTime());
        manager.Render(new GameTime());

        // Assert
        scene!.UpdateCalled.Should().BeTrue();
        scene.RenderCalled.Should().BeTrue();
    }

    [Fact]
    public async Task ShouldHandleSceneTransitions()
    {
        // Arrange
        var manager = CreateManager();
        await manager.LoadSceneAsync<TestScene>();
        var firstScene = manager.CurrentScene;

        // Act
        await manager.LoadSceneAsync<AnotherTestScene>();
        var secondScene = manager.CurrentScene;

        // Assert
        firstScene.Should().NotBe(secondScene);
        secondScene.Should().BeOfType<AnotherTestScene>();
    }

    [Fact]
    public async Task ShouldHandleMultipleConsecutiveSceneLoads()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        await manager.LoadSceneAsync<TestScene>();
        await manager.LoadSceneAsync<AnotherTestScene>();
        await manager.LoadSceneAsync<TestScene>();

        // Assert
        manager.CurrentScene.Should().BeOfType<TestScene>();
    }

    [Fact]
    public async Task ShouldNotUpdateOrRenderBeforeSceneIsLoaded()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        manager.Update(new GameTime());
        manager.Render(new GameTime());

        // Assert
        manager.CurrentScene.Should().BeNull();
    }

    [Fact]
    public async Task ShouldCallOnEnterAfterLoad()
    {
        // Arrange
        var manager = CreateManager();
        var scene = default(OrderedTestScene);

        // Act
        await manager.LoadSceneAsync<OrderedTestScene>();
        scene = manager.CurrentScene as OrderedTestScene;

        // Assert
        scene!.LoadCalled.Should().BeTrue("OnLoadAsync should have been called");
        scene.EnterCalled.Should().BeTrue("OnEnter should have been called");
        scene.LoadCalledBeforeEnter.Should().BeTrue("OnLoadAsync should be called before OnEnter");
    }

    [Fact]
    public async Task ShouldHandleSceneWithTransition()
    {
        // Arrange
        var manager = CreateManager();
        var transition = new TestTransition();

        // Act
        await manager.LoadSceneAsync<TestScene>(transition);

        // Assert
        transition.BeginCalled.Should().BeTrue();
        manager.CurrentScene.Should().BeOfType<TestScene>();
    }

    [Fact]
    public async Task ShouldHandleSceneWithLoadingScreenGeneric()
    {
        // Arrange
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddScoped<IEntityWorld, EntityWorld>();
        services.Configure<ECSOptions>(options => { });
        services.AddSingleton(_mockRenderer);
        services.AddTransient<TestScene>();
        services.AddTransient<TestLoadingScreen>(); 
        
        var serviceProvider = services.BuildServiceProvider();
        var manager = new SceneManager(
            NullLogger<SceneManager>.Instance,
            serviceProvider,
            renderer: _mockRenderer
        );

        // Act - Use generic overload
        await manager.LoadSceneAsync<TestScene, TestLoadingScreen>();

        // Assert
        manager.CurrentScene.Should().BeOfType<TestScene>();
    }

    // --- Helpers and test doubles ---

    private SceneManager CreateManager()
    {
        return new SceneManager(_logger, _serviceProvider, renderer: _mockRenderer);
    }

    public class TestScene : Scene
    {
        public bool LoadCalled { get; private set; }
        public bool EnterCalled { get; private set; }
        public bool UnloadCalled { get; private set; }
        public bool ExitCalled { get; private set; }
        public bool UpdateCalled { get; private set; }
        public bool RenderCalled { get; private set; }

        public TestScene() { }

        protected internal override Task OnLoadAsync(CancellationToken cancellationToken)
        {
            LoadCalled = true;
            return Task.CompletedTask;
        }

        protected internal override void OnEnter()
        {
            EnterCalled = true;
        }

        protected internal override Task OnUnloadAsync(CancellationToken cancellationToken)
        {
            UnloadCalled = true;
            return Task.CompletedTask;
        }

        protected internal override void OnExit()
        {
            ExitCalled = true;
        }

        protected internal override void OnUpdate(GameTime gameTime) => UpdateCalled = true;
        protected internal override void OnRender(GameTime gameTime) => RenderCalled = true;
    }

    public class AnotherTestScene : Scene
    {
        public AnotherTestScene() { }
    }

    public class OrderedTestScene : Scene
    {
        public bool LoadCalled { get; private set; }
        public bool EnterCalled { get; private set; }
        public bool LoadCalledBeforeEnter { get; private set; }

        public OrderedTestScene() { }

        protected internal override Task OnLoadAsync(CancellationToken cancellationToken)
        {
            LoadCalled = true;
            LoadCalledBeforeEnter = !EnterCalled;
            return Task.CompletedTask;
        }

        protected internal override void OnEnter()
        {
            EnterCalled = true;
        }
    }

    public class ManualScene : Scene
    {
        public ManualScene()
        {
            EnableLifecycleHooks = false;
            EnableAutomaticFrameManagement = false;
        }
    }

    public class TestTransition : ISceneTransition
    {
        public bool BeginCalled { get; private set; }
        public bool UpdateCalled { get; private set; }
        public bool RenderCalled { get; private set; }
        public float Progress => 1.0f;
        public float Duration => 0.01f;
        public bool IsComplete => true;

        public void Begin() => BeginCalled = true;
        public void Update(GameTime gameTime) => UpdateCalled = true;
        public void Render(IRenderer? renderer) => RenderCalled = true;
    }

    public class TestLoadingScreen : LoadingScene
    {
        public bool LoadCalled { get; private set; }
        public bool EnterCalled { get; private set; }
        public bool UnloadCalled { get; private set; }

        public TestLoadingScreen() { }

        protected internal override Task OnLoadAsync(CancellationToken cancellationToken)
        {
            LoadCalled = true;
            return Task.CompletedTask;
        }

        protected internal override void OnEnter()
        {
            EnterCalled = true;
        }

        protected internal override Task OnUnloadAsync(CancellationToken cancellationToken)
        {
            UnloadCalled = true;
            return Task.CompletedTask;
        }

        protected override void OnRenderLoading(GameTime gameTime)
        {
            // Override to do nothing for tests
        }
    }
}