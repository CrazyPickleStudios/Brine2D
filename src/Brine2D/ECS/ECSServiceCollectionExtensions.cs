using Brine2D.Performance;
using Brine2D.Pooling;
using Brine2D.ECS.Serialization;
using Brine2D.ECS.Systems;
using Brine2D.Engine;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;

namespace Brine2D.ECS;

/// <summary>
/// Extension methods for configuring ECS system pipelines.
/// </summary>
public static class ECSServiceCollectionExtensions
{
    /// <summary>
    /// Configures the update and render pipelines with custom ECS systems.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This method is additive and can be called multiple times to register systems
    /// from different configuration locations. Systems are automatically deduplicated
    /// by type - adding the same system multiple times will only register it once.
    /// </para>
    /// <para>
    /// Built-in systems are automatically registered when calling <c>.UseSystems()</c>.
    /// Use this method to add ONLY your custom systems.
    /// </para>
    /// <para>
    /// Systems are executed in order of their <see cref="IUpdateSystem.UpdateOrder"/> 
    /// or <see cref="IRenderSystem.RenderOrder"/> properties, not the order they are added.
    /// Systems implementing both <see cref="IUpdateSystem"/> and <see cref="IRenderSystem"/> 
    /// are automatically added to both pipelines.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Step 1: Built-in systems are auto-registered by .UseSystems()
    /// builder.Services
    ///     .AddBrine2D()
    ///     .UseSystems()  // Registers SpriteRenderingSystem, ParticleSystem, etc.
    ///     .UseSDL();
    /// 
    /// // Step 2: Add ONLY your custom systems
    /// builder.Services.ConfigureSystemPipelines(pipelines =>
    /// {
    ///     pipelines.AddSystem&lt;MyCustomAISystem&gt;();
    ///     pipelines.AddSystem&lt;MyCustomPhysicsSystem&gt;();
    /// });
    /// 
    /// // Optional: Can be called multiple times (additive)
    /// builder.Services.ConfigureSystemPipelines(pipelines =>
    /// {
    ///     pipelines.AddSystem&lt;MyDebugSystem&gt;();
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection ConfigureSystemPipelines(
        this IServiceCollection services, 
        Action<SystemPipelineBuilder> configure)
    {
        // Ensure hosted service is registered (idempotent - won't duplicate)
        services.AddHostedService<SystemPipelineHostedService>();
        
        var builder = new SystemPipelineBuilder(services);
        configure(builder);
        return services;
    }

    /// <summary>
    /// Adds ECS lifecycle hook for automatic update pipeline execution.
    /// Called automatically when systems are registered.
    /// </summary>
    public static IServiceCollection AddECSLifecycleHook(this IServiceCollection services)
    {
        services.AddSingleton<ISceneLifecycleHook, ECSLifecycleHook>();
        return services;
    }
}