using Brine2D.Core;
using Brine2D.ECS.Serialization;
using Brine2D.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Brine2D.ECS;

/// <summary>
/// Post-build configuration to populate update and render pipelines.
/// This is registered as a hosted service to run automatically.
/// </summary>
internal class SystemPipelineHostedService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public SystemPipelineHostedService(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public Task StartAsync(CancellationToken cancellationToken)
    {
        // Configure pipelines when the host starts
        SystemPipelineConfigurator.ConfigureUpdatePipeline(_serviceProvider);
        SystemPipelineConfigurator.ConfigureRenderPipeline(_serviceProvider);
        return Task.CompletedTask;
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}

public static class ECSServiceCollectionExtensions
{
    /// <summary>
    /// Adds the object-based ECS system to the service collection.
    /// Automatically registers lifecycle hook for pipeline execution.
    /// </summary>
    public static IServiceCollection AddObjectECS(this IServiceCollection services)
    {
        services.TryAddSingleton<IEntityWorld, EntityWorld>();
        services.TryAddSingleton<PrefabLibrary>();
        services.TryAddSingleton<EntitySerializer>();
        services.TryAddSingleton<EventBus>();
        
        // Add UpdatePipeline
        services.TryAddSingleton<UpdatePipeline>();
        
        // Register pure ECS systems
        services.TryAddSingleton<VelocitySystem>();
        services.TryAddSingleton<PhysicsSystem>();
        services.TryAddSingleton<AISystem>();
        
        // Register lifecycle hook for automatic pipeline execution
        services.AddSingleton<ISceneLifecycleHook, ECSLifecycleHook>();
        
        // Register hosted service to auto-configure pipelines
        services.AddHostedService<SystemPipelineHostedService>();
        
        return services;
    }

    /// <summary>
    /// Configures the update and render pipelines with ECS systems.
    /// Systems are executed in order of their UpdateOrder/RenderOrder properties.
    /// </summary>
    /// <example>
    /// <code>
    /// builder.Services.ConfigureSystemPipelines(pipelines =>
    /// {
    ///     pipelines.AddSystem&lt;PlayerControllerSystem&gt;();
    ///     pipelines.AddSystem&lt;AISystem&gt;();
    ///     pipelines.AddSystem&lt;SpriteRenderingSystem&gt;();
    ///     pipelines.AddSystem&lt;ParticleSystem&gt;(); // Both update and render
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection ConfigureSystemPipelines(
        this IServiceCollection services, 
        Action<SystemPipelineBuilder> configure)
    {
        var builder = new SystemPipelineBuilder(services);
        configure(builder);
        return services;
    }
}

/// <summary>
/// Builder for configuring update and render system pipelines.
/// </summary>
public class SystemPipelineBuilder
{
    private readonly IServiceCollection _services;

    internal SystemPipelineBuilder(IServiceCollection services)
    {
        _services = services;
    }

    /// <summary>
    /// Adds a system to the appropriate pipeline(s).
    /// - If the system implements only IUpdateSystem, adds to update pipeline.
    /// - If the system implements IRenderSystem (from Brine2D.Rendering.ECS), adds to render pipeline.
    /// - If the system implements both, automatically adds to BOTH pipelines.
    /// Uses reflection to detect IRenderSystem without requiring a direct reference.
    /// </summary>
    /// <example>
    /// <code>
    /// pipelines.AddSystem&lt;VelocitySystem&gt;();        // Update only
    /// pipelines.AddSystem&lt;SpriteRenderingSystem&gt;(); // Render only
    /// pipelines.AddSystem&lt;ParticleSystem&gt;();        // Both (auto-detected)
    /// </code>
    /// </example>
    public SystemPipelineBuilder AddSystem<T>() where T : class
    {
        var systemType = typeof(T);
        var isUpdateSystem = typeof(IUpdateSystem).IsAssignableFrom(systemType);
        
        // Check for IRenderSystem by name (no compile-time dependency)
        var isRenderSystem = systemType.GetInterfaces()
            .Any(i => i.Name == "IRenderSystem" && 
                     i.Namespace == "Brine2D.Rendering.ECS");

        if (!isUpdateSystem && !isRenderSystem)
        {
            throw new InvalidOperationException(
                $"Type {systemType.Name} must implement IUpdateSystem, IRenderSystem, or both.");
        }

        // Register the system itself
        _services.TryAddSingleton<T>();

        // Add to update pipeline if applicable
        if (isUpdateSystem)
        {
            // Create registration using reflection to avoid constraint issues
            var registrationType = typeof(UpdateSystemRegistration<>).MakeGenericType(systemType);
            var registration = Activator.CreateInstance(registrationType) as IUpdateSystemRegistration;
            _services.AddSingleton(registration!);
        }

        // Add to render pipeline if applicable
        if (isRenderSystem)
        {
            var registrationType = typeof(RenderSystemRegistration<>).MakeGenericType(systemType);
            var registration = Activator.CreateInstance(registrationType) as IRenderSystemRegistration;
            _services.AddSingleton(registration!);
        }

        return this;
    }
}

// Registration helpers for deferred system resolution
internal interface IUpdateSystemRegistration
{
    IUpdateSystem Resolve(IServiceProvider serviceProvider);
}

internal interface IRenderSystemRegistration
{
    object Resolve(IServiceProvider serviceProvider); // Returns object to avoid rendering dependency
}

/// <summary>
/// Registration for update systems (IUpdateSystem is known in Brine2D.ECS).
/// </summary>
internal class UpdateSystemRegistration<T> : IUpdateSystemRegistration 
    where T : IUpdateSystem
{
    public IUpdateSystem Resolve(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<T>();
    }
}

/// <summary>
/// Registration for render systems (IRenderSystem is in Brine2D.Rendering.ECS).
/// Uses 'class' constraint because IRenderSystem is not available in this package.
/// Type checking happens via reflection in AddSystem().
/// </summary>
internal class RenderSystemRegistration<T> : IRenderSystemRegistration 
    where T : class
{
    public object Resolve(IServiceProvider serviceProvider)
    {
        return serviceProvider.GetRequiredService<T>();
    }
}

/// <summary>
/// Post-build configuration to populate update and render pipelines.
/// Called after the service provider is built.
/// </summary>
public static class SystemPipelineConfigurator
{
    public static void ConfigureUpdatePipeline(IServiceProvider serviceProvider)
    {
        var pipeline = serviceProvider.GetRequiredService<UpdatePipeline>();
        var registrations = serviceProvider.GetServices<IUpdateSystemRegistration>();

        foreach (var registration in registrations)
        {
            var system = registration.Resolve(serviceProvider);
            pipeline.AddSystem(system);
        }
    }

    public static void ConfigureRenderPipeline(IServiceProvider serviceProvider)
    {
        // Try to get RenderPipeline (optional dependency from Brine2D.Rendering.ECS)
        var renderPipelineType = AppDomain.CurrentDomain.GetAssemblies()
            .SelectMany(a => a.GetTypes())
            .FirstOrDefault(t => t.Name == "RenderPipeline" && t.Namespace == "Brine2D.Rendering.ECS");

        if (renderPipelineType == null) return;

        var pipeline = serviceProvider.GetService(renderPipelineType);
        if (pipeline == null) return;

        var registrations = serviceProvider.GetServices<IRenderSystemRegistration>();
        var addSystemMethod = renderPipelineType.GetMethod("AddSystem");

        if (addSystemMethod == null) return;

        foreach (var registration in registrations)
        {
            var system = registration.Resolve(serviceProvider);
            addSystemMethod.Invoke(pipeline, new[] { system });
        }
    }
}