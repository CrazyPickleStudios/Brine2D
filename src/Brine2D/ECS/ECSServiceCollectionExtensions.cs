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
    /// Use this to add custom systems to the global pipelines.
    /// Built-in systems are automatically registered by <c>.UseSystems()</c>.
    /// </para>
    /// <para>
    /// Systems are executed in order of their UpdateOrder/RenderOrder properties.
    /// Systems implementing both IUpdateSystem and IRenderSystem are automatically
    /// added to both pipelines.
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Built-in systems are auto-registered by .UseSystems()
    /// builder.Services.AddBrine2D().UseSystems().UseSDL();
    /// 
    /// // Add custom systems
    /// builder.Services.ConfigureSystemPipelines(pipelines =>
    /// {
    ///     pipelines.AddSystem&lt;MyCustomAISystem&gt;();
    ///     pipelines.AddSystem&lt;MyCustomRenderSystem&gt;();
    /// });
    /// </code>
    /// </example>
    public static IServiceCollection ConfigureSystemPipelines(
        this IServiceCollection services, 
        Action<SystemPipelineBuilder> configure)
    {
        // Ensure hosted service is registered (auto-populates pipelines)
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