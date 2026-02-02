using Brine2D.ECS.Systems;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Brine2D.ECS;

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
    /// Automatically registers the system in DI as a singleton.
    /// </summary>
    /// <remarks>
    /// Systems are automatically routed to the correct pipeline:
    /// <list type="bullet">
    /// <item><description>IUpdateSystem only → Update pipeline</description></item>
    /// <item><description>IRenderSystem only → Render pipeline</description></item>
    /// <item><description>Both interfaces → Both pipelines</description></item>
    /// </list>
    /// </remarks>
    /// <exception cref="InvalidOperationException">
    /// Thrown if the type doesn't implement IUpdateSystem or IRenderSystem.
    /// </exception>
    /// <example>
    /// <code>
    /// pipelines.AddSystem&lt;VelocitySystem&gt;();        // Auto-registers + adds to pipeline
    /// pipelines.AddSystem&lt;SpriteRenderingSystem&gt;(); // Auto-registers + adds to pipeline
    /// pipelines.AddSystem&lt;ParticleSystem&gt;();        // Auto-registers + adds to both pipelines
    /// </code>
    /// </example>
    public SystemPipelineBuilder AddSystem<T>() where T : class
    {
        var systemType = typeof(T);
        
        var isUpdateSystem = typeof(IUpdateSystem).IsAssignableFrom(systemType);
        var isRenderSystem = typeof(IRenderSystem).IsAssignableFrom(systemType);

        if (!isUpdateSystem && !isRenderSystem)
        {
            throw new InvalidOperationException(
                $"Type '{systemType.Name}' must implement IUpdateSystem, IRenderSystem, or both. " +
                $"Systems must inherit from one of these interfaces to be added to pipelines.");
        }

        // STEP 1: Register the system in DI (automatic - user doesn't think about this!)
        _services.TryAddSingleton<T>();

        // STEP 2: Add to update pipeline if applicable
        if (isUpdateSystem)
        {
            var registrationType = typeof(UpdateSystemRegistration<>).MakeGenericType(systemType);
            var registration = Activator.CreateInstance(registrationType) as IUpdateSystemRegistration;
            _services.AddSingleton(registration!);
        }

        // STEP 3: Add to render pipeline if applicable
        if (isRenderSystem)
        {
            var registrationType = typeof(RenderSystemRegistration<>).MakeGenericType(systemType);
            var registration = Activator.CreateInstance(registrationType) as IRenderSystemRegistration;
            _services.AddSingleton(registration!);
        }

        return this;
    }
}
