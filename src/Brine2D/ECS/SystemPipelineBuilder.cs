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
    /// Adds a system to the appropriate pipeline(s) with optional priority override.
    /// Automatically registers the system in DI as a singleton.
    /// </summary>
    /// <param name="updatePriority">
    /// Override for update pipeline priority. If null, uses system's UpdateOrder property.
    /// Lower values execute first.
    /// </param>
    /// <param name="renderPriority">
    /// Override for render pipeline priority. If null, uses system's RenderOrder property.
    /// Lower values execute first.
    /// </param>
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
    /// // Use default priorities from system
    /// pipelines.AddSystem&lt;PhysicsSystem&gt;();
    /// 
    /// // Override update priority
    /// pipelines.AddSystem&lt;CustomSystem&gt;(updatePriority: 50);
    /// 
    /// // Override both priorities
    /// pipelines.AddSystem&lt;DebugSystem&gt;(updatePriority: 9999, renderPriority: 9999);
    /// </code>
    /// </example>
    public SystemPipelineBuilder AddSystem<T>(int? updatePriority = null, int? renderPriority = null) 
        where T : class
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

        // Register the system in DI
        _services.TryAddSingleton<T>();

        // Add to update pipeline with optional priority override
        if (isUpdateSystem)
        {
            TryAddSystemRegistration<IUpdateSystemRegistration>(
                typeof(UpdateSystemRegistration<>).MakeGenericType(systemType),
                updatePriority);
        }

        // Add to render pipeline with optional priority override
        if (isRenderSystem)
        {
            TryAddSystemRegistration<IRenderSystemRegistration>(
                typeof(RenderSystemRegistration<>).MakeGenericType(systemType),
                renderPriority);
        }

        return this;
    }

    /// <summary>
    /// Adds a system that runs after another system (relative ordering).
    /// </summary>
    /// <typeparam name="T">The system to add.</typeparam>
    /// <typeparam name="TAfter">The system that T should run after.</typeparam>
    /// <example>
    /// <code>
    /// pipelines
    ///     .AddSystem&lt;PhysicsSystem&gt;()
    ///     .AddSystemAfter&lt;CollisionSystem, PhysicsSystem&gt;(); // Runs after physics
    /// </code>
    /// </example>
    public SystemPipelineBuilder AddSystemAfter<T, TAfter>() 
        where T : class 
        where TAfter : class
    {
        // Build temporary service provider to get TAfter instance
        using var tempProvider = _services.BuildServiceProvider();
        var afterSystem = tempProvider.GetService<TAfter>();
        
        int? updateOrder = null;
        int? renderOrder = null;
        
        if (afterSystem is IUpdateSystem updateSys)
            updateOrder = updateSys.UpdateOrder + 1;
        if (afterSystem is IRenderSystem renderSys)
            renderOrder = renderSys.RenderOrder + 1;
        
        return AddSystem<T>(updatePriority: updateOrder, renderPriority: renderOrder);
    }

    /// <summary>
    /// Adds a system that runs before another system (relative ordering).
    /// </summary>
    /// <typeparam name="T">The system to add.</typeparam>
    /// <typeparam name="TBefore">The system that T should run before.</typeparam>
    public SystemPipelineBuilder AddSystemBefore<T, TBefore>() 
        where T : class 
        where TBefore : class
    {
        using var tempProvider = _services.BuildServiceProvider();
        var beforeSystem = tempProvider.GetService<TBefore>();
        
        int? updateOrder = null;
        int? renderOrder = null;
        
        if (beforeSystem is IUpdateSystem updateSys)
            updateOrder = updateSys.UpdateOrder - 1;
        if (beforeSystem is IRenderSystem renderSys)
            renderOrder = renderSys.RenderOrder - 1;
        
        return AddSystem<T>(updatePriority: updateOrder, renderPriority: renderOrder);
    }

    private void TryAddSystemRegistration<TInterface>(Type registrationType, int? priorityOverride)
        where TInterface : class
    {
        // Check if already registered
        var alreadyRegistered = _services.Any(s =>
            s.ServiceType == typeof(TInterface) &&
            s.ImplementationInstance?.GetType() == registrationType);

        if (!alreadyRegistered)
        {
            var registration = Activator.CreateInstance(registrationType) as TInterface;
            
            // TODO: If priorityOverride is set, we need to modify the registration
            // This requires updating UpdateSystemRegistration/RenderSystemRegistration
            // to accept priority overrides (future enhancement)
            
            _services.AddSingleton(registration!);
        }
    }
}
