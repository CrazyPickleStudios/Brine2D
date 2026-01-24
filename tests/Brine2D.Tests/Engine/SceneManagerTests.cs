using System;
using System.Threading;
using System.Threading.Tasks;
using Brine2D.Core;
using Brine2D.Engine;
using Brine2D.Rendering;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Xunit;

namespace Brine2D.Tests.Engine;

public class SceneManagerTests
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<SceneManager> _logger;

    public SceneManagerTests()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ILoggerFactory, NullLoggerFactory>();
        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddTransient<TestScene>();
        services.AddTransient<AnotherTestScene>();
        services.AddTransient<OrderedTestScene>();
        services.AddTransient<ManualScene>();
        
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
        scene!.InitializeCalled.Should().BeTrue();
        scene.LoadCalled.Should().BeTrue();
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
        firstScene!.UnloadCalled.Should().BeTrue();
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
    public async Task ShouldThrowIfLoadingNonISceneType()
    {
        // Arrange
        var manager = CreateManager();

        // Act
        Func<Task> act = async () => await manager.LoadSceneAsync(typeof(string));

        // Assert
        await act.Should().ThrowAsync<ArgumentException>();
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
    public async Task ShouldCallInitializeBeforeLoad()
    {
        // Arrange
        var manager = CreateManager();
        var scene = default(OrderedTestScene);

        // Act
        await manager.LoadSceneAsync<OrderedTestScene>();
        scene = manager.CurrentScene as OrderedTestScene;

        // Assert
        scene!.InitializeCalled.Should().BeTrue();
        scene.LoadCalled.Should().BeTrue();
        scene.InitializeCalledBeforeLoad.Should().BeTrue();
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
    public async Task ShouldHandleSceneWithLoadingScreen()
    {
        // Arrange
        var manager = CreateManager();
        var loadingScreen = new TestLoadingScreen();

        // Act
        await manager.LoadSceneAsync<TestScene>(loadingScreen: loadingScreen);

        // Assert
        loadingScreen.InitializeCalled.Should().BeTrue();
        loadingScreen.LoadCalled.Should().BeTrue();
        loadingScreen.UnloadCalled.Should().BeTrue();
        manager.CurrentScene.Should().BeOfType<TestScene>();
    }

    // --- Helpers and test doubles ---

    private SceneManager CreateManager()
    {
        return new SceneManager(_logger, _serviceProvider);
    }

    public class TestScene : Scene
    {
        public bool InitializeCalled { get; private set; }
        public bool LoadCalled { get; private set; }
        public bool UnloadCalled { get; private set; }
        public bool UpdateCalled { get; private set; }
        public bool RenderCalled { get; private set; }

        public TestScene(ILogger<TestScene> logger) : base(logger) { }

        protected override Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            InitializeCalled = true;
            return Task.CompletedTask;
        }

        protected override Task OnLoadAsync(CancellationToken cancellationToken)
        {
            LoadCalled = true;
            return Task.CompletedTask;
        }

        protected override Task OnUnloadAsync(CancellationToken cancellationToken)
        {
            UnloadCalled = true;
            return Task.CompletedTask;
        }

        protected override void OnUpdate(GameTime gameTime) => UpdateCalled = true;
        protected override void OnRender(GameTime gameTime) => RenderCalled = true;
    }

    public class AnotherTestScene : Scene
    {
        public AnotherTestScene(ILogger<AnotherTestScene> logger) : base(logger) { }
    }

    public class OrderedTestScene : Scene
    {
        public bool InitializeCalled { get; private set; }
        public bool LoadCalled { get; private set; }
        public bool InitializeCalledBeforeLoad { get; private set; }

        public OrderedTestScene(ILogger<OrderedTestScene> logger) : base(logger) { }

        protected override Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            InitializeCalled = true;
            InitializeCalledBeforeLoad = !LoadCalled;
            return Task.CompletedTask;
        }

        protected override Task OnLoadAsync(CancellationToken cancellationToken)
        {
            LoadCalled = true;
            return Task.CompletedTask;
        }
    }

    public class ManualScene : Scene
    {
        public ManualScene(ILogger<ManualScene> logger) : base(logger)
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
        public float Progress => 1.0f; // Always complete for unit tests
        public float Duration => 0.01f;
        public bool IsComplete => true; // Always complete immediately

        public void Begin() => BeginCalled = true;
        public void Update(float deltaTime) => UpdateCalled = true;
        public void Render(IRenderer renderer) => RenderCalled = true;
    }

    public class TestLoadingScreen : LoadingScene
    {
        public bool InitializeCalled { get; private set; }
        public bool LoadCalled { get; private set; }
        public bool UnloadCalled { get; private set; }

        public TestLoadingScreen()
            : base(null, NullLogger.Instance) // No renderer needed for tests
        {
        }

        protected override Task OnInitializeAsync(CancellationToken cancellationToken)
        {
            InitializeCalled = true;
            return Task.CompletedTask;
        }

        protected override Task OnLoadAsync(CancellationToken cancellationToken)
        {
            LoadCalled = true;
            return Task.CompletedTask;
        }

        protected override Task OnUnloadAsync(CancellationToken cancellationToken)
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