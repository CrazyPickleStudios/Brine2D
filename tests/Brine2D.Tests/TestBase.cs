using Brine2D.ECS;
using Brine2D.Engine;
using Brine2D.Rendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Brine2D.Tests;

public abstract class TestBase
{
    protected EntityWorld CreateTestWorld(Action<ECSOptions>? configure = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        
        // Configure ECS options with defaults
        services.Configure<ECSOptions>(options =>
        {
            configure?.Invoke(options);
        });
        
        var serviceProvider = services.BuildServiceProvider();
        
        var world = new EntityWorld(
            serviceProvider,
            serviceProvider.GetService<ILoggerFactory>(),
            serviceProvider.GetService<IOptions<ECSOptions>>()
        );
        
        // Ensure the world is initialized (if it has an initialization method)
        // world.Initialize(); // Uncomment if EntityWorld has this
        
        return world;
    }
    
    protected IServiceProvider CreateTestServiceProvider(Action<IServiceCollection>? configureServices = null)
    {
        var services = new ServiceCollection();
        services.AddLogging();
        
        configureServices?.Invoke(services);
        
        return services.BuildServiceProvider();
    }
    
    /// <summary>
    /// Creates a scene with framework properties initialized for testing.
    /// Requires [assembly: InternalsVisibleTo("Brine2D.Tests")] in Brine2D assembly.
    /// </summary>
    protected T CreateTestScene<T>() where T : Scene, new()
    {
        var scene = new T();
        
        scene.Logger = NullLogger<T>.Instance;
        scene.World = CreateTestWorld();
        scene.Renderer = Substitute.For<IRenderer>();
        
        return scene;
    }
}