using Brine2D.Engine;
using Brine2D.Hosting;
using Microsoft.Extensions.DependencyInjection;

namespace Brine2D.Tests.Hosting;

public sealed class GameApplicationBuilderTests
{
    private static GameApplicationBuilder Headless()
        => GameApplication.CreateBuilder().Configure(o => o.Headless = true);

    [Fact]
    public async Task Build_CalledTwice_ThrowsWithDescriptiveMessage()
    {
        var builder = Headless();
        await using var _ = builder.Build();

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains("Build() has been called", ex.Message);
    }

    [Fact]
    public async Task Configure_AfterBuild_Throws()
    {
        var builder = Headless();
        await using var _ = builder.Build();

        Assert.Throws<InvalidOperationException>(() => builder.Configure(_ => { }));
    }

    [Fact]
    public async Task AddScene_AfterBuild_Throws()
    {
        var builder = Headless();
        await using var _ = builder.Build();

        Assert.Throws<InvalidOperationException>(() => builder.AddScene<EmptyScene>());
    }

    [Fact]
    public async Task ConfigureScene_AfterBuild_Throws()
    {
        var builder = Headless();
        await using var _ = builder.Build();

        Assert.Throws<InvalidOperationException>(() => builder.ConfigureScene(_ => { }));
    }

    [Fact]
    public void AddScene_CalledTwice_RegistersTransientOnce()
    {
        var builder = Headless();

        builder.AddScene<EmptyScene>();
        builder.AddScene<EmptyScene>();

        var registrationCount = builder.Services
            .Count(d => d.ServiceType == typeof(EmptyScene));

        Assert.Equal(1, registrationCount);
    }

    [Fact]
    public void Build_SceneWithUnregisteredDependency_ThrowsWithSceneNameAndParameterType()
    {
        var builder = Headless();
        builder.AddScene<SceneWithUnregisteredDep>();

        var ex = Assert.Throws<InvalidOperationException>(() => builder.Build());

        Assert.Contains(nameof(SceneWithUnregisteredDep), ex.Message);
        Assert.Contains(nameof(IUnregisteredService),     ex.Message);
    }

    [Fact]
    public async Task Build_SceneWithRegisteredDependency_DoesNotThrow()
    {
        var builder = Headless();
        builder.Services.AddSingleton<IUnregisteredService, StubService>();
        builder.AddScene<SceneWithUnregisteredDep>();

        await using var game = builder.Build();
    }

    private sealed class EmptyScene : Scene { }

    private interface IUnregisteredService { }
    private sealed class StubService : IUnregisteredService { }

    private sealed class SceneWithUnregisteredDep : Scene
    {
        // ReSharper disable once UnusedParameter.Local
        public SceneWithUnregisteredDep(IUnregisteredService dep) { }
    }
}