using Brine2D.ECS;
using Brine2D.Engine;
using Brine2D.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;

namespace Brine2D.Tests;

public abstract class TestBase
{
    protected IEntityWorld CreateTestWorld(Action<ECSOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();

        var serviceProvider = services.BuildServiceProvider();

        var options = new ECSOptions();
        configure?.Invoke(options);

        return new EntityWorld(
            new ServiceProviderActivator(serviceProvider),
            serviceProvider.GetService<ILoggerFactory>(),
            options);
    }

    protected IServiceProvider CreateTestServiceProvider(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        configureServices?.Invoke(services);
        return services.BuildServiceProvider();
    }

    protected T CreateTestScene<T>() where T : Scene, new()
    {
        var scene = new T();
        scene.Logger = Microsoft.Extensions.Logging.Abstractions.NullLogger<T>.Instance;
        scene.World = CreateTestWorld();
        scene.Renderer = Substitute.For<IRenderer>();
        return scene;
    }
}